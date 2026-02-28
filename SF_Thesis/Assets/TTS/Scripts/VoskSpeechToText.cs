using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Vosk;

public class VoskSpeechToText : MonoBehaviour
{
	[Tooltip("Location of the model, relative to the Streaming Assets folder.")]
	public string ModelPath = "vosk-model-small-en-us-0.15.zip";

	[Tooltip("The source of the microphone input.")]

	public VoiceProcessor VoiceProcessor;
	[Tooltip("The Max number of alternatives that will be processed.")]
	public int MaxAlternatives = 3;

	[Tooltip("How long should we record before restarting?")]
	public float MaxRecordLength = 5;

	[Tooltip("Should the recognizer start when the application is launched?")]
	public bool AutoStart = true;

	[Tooltip("The phrases that will be detected. If left empty, all words will be detected.")]
	public List<string> KeyPhrases = new List<string>();

	private Task _workerTask;
	private readonly object _recognizerLock = new object();

	//Cached version of the Vosk Model.
	private Model _model;

	//Cached version of the Vosk recognizer.
	private VoskRecognizer _recognizer;

	//Conditional flag to see if a recognizer has already been created.
	//TODO: Allow for runtime changes to the recognizer.
	private bool _recognizerReady;

	//Holds all of the audio data until the user stops talking.
	private readonly List<short> _buffer = new List<short>();

	//Called when the the state of the controller changes.
	public Action<string> OnStatusUpdated;

	//Called after the user is done speaking and vosk processes the audio.
	public Action<string> OnTranscriptionResult;

	//The absolute path to the decompressed model folder.
	private string _decompressedModelPath;

	//A string that contains the keywords in Json Array format
	private string _grammar = "";

	//Flag that is used to wait for the model file to decompress successfully.
	private bool _isDecompressing;

	//Flag that is used to wait for the the script to start successfully.
	private bool _isInitializing;

	//Flag that is used to check if Vosk was started.
	private bool _didInit;

	//Threading Logic

	// Flag to signal we are ending
	private bool _running;

	//Thread safe queue of microphone data.
	private readonly ConcurrentQueue<short[]> _threadedBufferQueue = new ConcurrentQueue<short[]>();

	//Thread safe queue of resuts
	private readonly ConcurrentQueue<string> _threadedResultQueue = new ConcurrentQueue<string>();



	static readonly ProfilerMarker voskRecognizerCreateMarker = new ProfilerMarker("VoskRecognizer.Create");
	static readonly ProfilerMarker voskRecognizerReadMarker = new ProfilerMarker("VoskRecognizer.AcceptWaveform");

	//If Auto start is enabled, starts vosk speech to text.
	void Start()
	{
		if (AutoStart)
		{
			// Start VoiceProcessor recording immediately for monster listening
			VoiceProcessor.StartRecording(sampleRate: 16000, frameSize: 512, autoDetect: false);
			// Initialize Vosk but don’t start STT until push-to-talk
			StartVoskStt(startMicrophone: false);
		}
	}

	/// <summary>
	/// Start Vosk Speech to text
	/// </summary>
	/// <param name="keyPhrases">A list of keywords/phrases. Keywords need to exist in the models dictionary, so some words like "webview" are better detected as two more common words "web view".</param>
	/// <param name="modelPath">The path to the model folder relative to StreamingAssets. If the path has a .zip ending, it will be decompressed into the application data persistent folder.</param>
	/// <param name="startMicrophone">"Should the microphone after vosk initializes?</param>
	/// <param name="maxAlternatives">The maximum number of alternative phrases detected</param>
	public void StartVoskStt(List<string> keyPhrases = null, string modelPath = default, bool startMicrophone = false, int maxAlternatives = 3)
	{
		if (_isInitializing)
		{
			Debug.LogError("Initializing in progress!");
			return;
		}
		if (_didInit)
		{
			Debug.LogError("Vosk has already been initialized!");
			return;
		}

		if (!string.IsNullOrEmpty(modelPath))
		{
			ModelPath = modelPath;
		}


		KeyPhrases.Clear();

		// 🔑 BUILD GRAMMAR FROM REGISTRIES
		BuildKeyPhrasesFromDoors(); 
		BuildKeyPhrasesFromBridges();
		BuildKeyPhrasesForFlashlight();
		BuildKeyPhrasesForInfoMenu();
		KeyPhrases.Add("information");


		// Optional override (debug / test)
		if (keyPhrases != null && keyPhrases.Count > 0)
			KeyPhrases = keyPhrases;

		MaxAlternatives = maxAlternatives;
		StartCoroutine(DoStartVoskStt(startMicrophone));
	}
	public void SetKeyPhrasesRuntime(List<string> phrases)
	{
		if (phrases == null) phrases = new List<string>();

		KeyPhrases.Clear();
		for (int i = 0; i < phrases.Count; i++)
		{
			string p = phrases[i];
			if (string.IsNullOrWhiteSpace(p)) continue;
			KeyPhrases.Add(p.Trim().ToLower());
		}

		// force recognizer to rebuild grammar next time we start recording
		_recognizerReady = false;
	}

	private void BuildKeyPhrasesForInfoMenu() //Method g. b. ChatGPT
	{
		// entry / exit
		KeyPhrases.Add("information");
		KeyPhrases.Add("info");
		KeyPhrases.Add("menu");
		KeyPhrases.Add("help");

		KeyPhrases.Add("back");
		KeyPhrases.Add("exit");
		KeyPhrases.Add("cancel");
		KeyPhrases.Add("close menu");

		// top-level
		KeyPhrases.Add("goal");
		KeyPhrases.Add("doors");
		KeyPhrases.Add("bridges");
		KeyPhrases.Add("objectives");
		KeyPhrases.Add("more");

	}

	private void BuildKeyPhrasesFromDoors() //Method g. b. ChatGPT
	{

		foreach (var door in DoorRegistry.GetAllDoors())
		{
			if (door == null) continue;

			string normalized = DoorRegistry.NormalizeDoorId(door.doorId); // e.g. "N-45"
			if (string.IsNullOrEmpty(normalized)) continue;

			string[] parts = normalized.Split('-');
			if (parts.Length != 2) continue;

			string letter = parts[0].ToLower(); // "n"
			string numberDigits = parts[1];     // "45"

			string spokenDigits = NumberWordParser.ToWords(numberDigits); // "forty five"

			// --- OPEN ---
			KeyPhrases.Add($"open door {letter} {numberDigits}");
			KeyPhrases.Add($"open door {letter} {spokenDigits}");
			KeyPhrases.Add($"open {letter} {numberDigits}");
			KeyPhrases.Add($"open {letter} {spokenDigits}");

			// --- CLOSE ---
			KeyPhrases.Add($"close door {letter} {numberDigits}");
			KeyPhrases.Add($"close door {letter} {spokenDigits}");
			KeyPhrases.Add($"close {letter} {numberDigits}");
			KeyPhrases.Add($"close {letter} {spokenDigits}");

			// --- LOCK VARIANTS ---
			//KeyPhrases.Add($"unlock door {letter} {spokenDigits}");
			//KeyPhrases.Add($"shut door {letter} {spokenDigits}");

		}

		Debug.Log($"[Vosk] Door grammar built: {KeyPhrases.Count} phrases");
	}

	private void BuildKeyPhrasesFromBridges() //Method g. b. ChatGPT
	{

		foreach (var bridge in BridgeRegistry.GetAllBridges())
		{
			if (bridge == null) continue;

			string normalized = BridgeRegistry.NormalizeBridgeId(bridge.bridgeId); // e.g. "B-35"
			if (string.IsNullOrEmpty(normalized)) continue;

			string[] parts = normalized.Split('-');
			if (parts.Length != 2) continue;

			string letter = parts[0].ToLower(); // "b"
			string numberDigits = parts[1];     // "35"

			// If your project uses a different method name, change this line accordingly:
			string spokenDigits = NumberWordParser.ToWords(numberDigits); // "thirty five"

			// --- EXTEND ---
			KeyPhrases.Add($"extend bridge {letter} {numberDigits}");
			KeyPhrases.Add($"extend bridge {letter} {spokenDigits}");
			KeyPhrases.Add($"extend {letter} {numberDigits}");
			KeyPhrases.Add($"extend {letter} {spokenDigits}");

			// --- RETRACT ---
			KeyPhrases.Add($"retract bridge {letter} {numberDigits}");
			KeyPhrases.Add($"retract bridge {letter} {spokenDigits}");
			KeyPhrases.Add($"retract {letter} {numberDigits}");
			KeyPhrases.Add($"retract {letter} {spokenDigits}");

			// --- OPEN (alias for extend) ---
			KeyPhrases.Add($"open bridge {letter} {numberDigits}");
			KeyPhrases.Add($"open bridge {letter} {spokenDigits}");
			KeyPhrases.Add($"open {letter} {numberDigits}");
			KeyPhrases.Add($"open {letter} {spokenDigits}");

			// --- CLOSE (alias for retract) ---
			KeyPhrases.Add($"close bridge {letter} {numberDigits}");
			KeyPhrases.Add($"close bridge {letter} {spokenDigits}");
			KeyPhrases.Add($"close {letter} {numberDigits}");
			KeyPhrases.Add($"close {letter} {spokenDigits}");

			// --- Activate (alias for extend) ---
			KeyPhrases.Add($"activate bridge {letter} {numberDigits}");
			KeyPhrases.Add($"activate bridge {letter} {spokenDigits}");
			KeyPhrases.Add($"activate {letter} {numberDigits}");
			KeyPhrases.Add($"activate {letter} {spokenDigits}");

			// --- Deactivate (alias for retract) ---
			KeyPhrases.Add($"deactivate bridge {letter} {numberDigits}");
			KeyPhrases.Add($"deactivate bridge {letter} {spokenDigits}");
			KeyPhrases.Add($"deactivate {letter} {numberDigits}");
			KeyPhrases.Add($"deactivate {letter} {spokenDigits}");


			// Optional: other natural variants
			// KeyPhrases.Add($"deploy bridge {letter} {spokenDigits}");
			// KeyPhrases.Add($"fold bridge {letter} {spokenDigits}");
		}

		Debug.Log($"[Vosk] Bridge grammar built: {KeyPhrases.Count} phrases");
	}
	private void BuildKeyPhrasesForFlashlight()
	{
		// Toggle
		KeyPhrases.Add("light");
		KeyPhrases.Add("flashlight");

		// Explicit on/off (optional but nice)
		KeyPhrases.Add("light on");
		KeyPhrases.Add("light off");
		KeyPhrases.Add("flashlight on");
		KeyPhrases.Add("flashlight off");
	}



	//Decompress model, load settings, start Vosk and optionally start the microphone
	private IEnumerator DoStartVoskStt(bool startMicrophone)
	{
		_isInitializing = true;
		yield return WaitForMicrophoneInput();

		yield return Decompress();

		OnStatusUpdated?.Invoke("Loading Model from: " + _decompressedModelPath);
		//Vosk.Vosk.SetLogLevel(0);
		_model = new Model(_decompressedModelPath);

		yield return null;

		OnStatusUpdated?.Invoke("Initialized");
		VoiceProcessor.OnFrameCapturedEnhanced += VoiceProcessorOnOnFrameCaptured;
		VoiceProcessor.OnRecordingStop += VoiceProcessorOnOnRecordingStop;

		if (startMicrophone)
			VoiceProcessor.StartRecording();

		_isInitializing = false;
		_didInit = true;

		//ToggleRecording();
	}

	//Translates the KeyPhraseses into a json array and appends the `[unk]` keyword at the end to tell vosk to filter other phrases.
	private void UpdateGrammar()
	{
		if (KeyPhrases.Count == 0)
		{
			_grammar = "";
			return;
		}

		JSONArray keywords = new JSONArray();
		foreach (string keyphrase in KeyPhrases)
		{
			keywords.Add(new JSONString(keyphrase.ToLower()));
		}

		keywords.Add(new JSONString("[unk]"));

		_grammar = keywords.ToString();
	}

	//Decompress the model zip file or return the location of the decompressed files.
	private IEnumerator Decompress()
	{
		if (!Path.HasExtension(ModelPath)
			|| Directory.Exists(
				Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(ModelPath))))
		{
			OnStatusUpdated?.Invoke("Using existing decompressed model.");
			_decompressedModelPath =
				Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(ModelPath));
			Debug.Log(_decompressedModelPath);

			yield break;
		}

		OnStatusUpdated?.Invoke("Decompressing model...");
		string dataPath = Path.Combine(Application.streamingAssetsPath, ModelPath);

		Stream dataStream;
		// Read data from the streaming assets path. You cannot access the streaming assets directly on Android.
		if (dataPath.Contains("://"))
		{
			UnityWebRequest www = UnityWebRequest.Get(dataPath);
			www.SendWebRequest();
			while (!www.isDone)
			{
				yield return null;
			}
			dataStream = new MemoryStream(www.downloadHandler.data);
		}
		// Read the file directly on valid platforms.
		else
		{
			dataStream = File.OpenRead(dataPath);
		}

		//Read the Zip File
		var zipFile = ZipFile.Read(dataStream);

		//Listen for the zip file to complete extraction
		zipFile.ExtractProgress += ZipFileOnExtractProgress;

		//Update status text
		OnStatusUpdated?.Invoke("Reading Zip file");

		//Start Extraction
		zipFile.ExtractAll(Application.persistentDataPath);

		//Wait until it's complete
		while (_isDecompressing == false)
		{
			yield return null;
		}
		//Override path given in ZipFileOnExtractProgress to prevent crash
		_decompressedModelPath = Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(ModelPath));

		//Update status text
		OnStatusUpdated?.Invoke("Decompressing complete!");
		//Wait a second in case we need to initialize another object.
		yield return new WaitForSeconds(1);
		//Dispose the zipfile reader.
		zipFile.Dispose();
	}

	///The function that is called when the zip file extraction process is updated.
	private void ZipFileOnExtractProgress(object sender, ExtractProgressEventArgs e)
	{
		if (e.EventType == ZipProgressEventType.Extracting_AfterExtractAll)
		{
			_isDecompressing = true;
			_decompressedModelPath = e.ExtractLocation;
		}
	}

	//Wait until microphones are initialized
	private IEnumerator WaitForMicrophoneInput()
{
    // Request permission where applicable
    yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

    float timeout = 6f;
    float t = 0f;

    while (Microphone.devices.Length <= 0 && t < timeout)
    {
        t += Time.unscaledDeltaTime;
        yield return null;
    }

    if (Microphone.devices.Length <= 0)
    {
        Debug.LogError("[Vosk] No microphone devices found (permission denied or no device). Init aborted.");
        OnStatusUpdated?.Invoke("No microphone device / permission denied.");
        _isInitializing = false;
        yield break;
    }

    // IMPORTANT: refresh device list AFTER permission
    if (VoiceProcessor != null)
        VoiceProcessor.UpdateDevices();
}


	public void StartRecording()
	{
		if (_running) return;
		if (!_didInit || _model == null) { Debug.LogWarning("Vosk not initialized yet"); return; }

		_running = true;
		_buffer.Clear();

		if (!VoiceProcessor.IsRecording)
			VoiceProcessor.StartRecording();

		// Don't start another worker if the previous one hasn't fully ended
		if (_workerTask == null || _workerTask.IsCompleted)
			_workerTask = Task.Run(ThreadedWork);
	}

	public async void StopRecordingAndProcess()
	{
		if (!_running) return;

		_running = false;

		// Wait for worker to exit loop so it's not inside AcceptWaveform
		if (_workerTask != null)
		{
			try { await _workerTask; }
			catch (Exception e) { Debug.LogException(e); }
		}

		// Now it's safe to touch recognizer
		lock (_recognizerLock)
		{
			if (_recognizer != null)
			{
				var finalResult = _recognizer.FinalResult();
				if (!string.IsNullOrWhiteSpace(finalResult))
					_threadedResultQueue.Enqueue(finalResult);

				_recognizer.Reset();
			}
		}
	}


	//Calls the On Phrase Recognized event on the Unity Thread
	void Update()
	{
		if (_threadedResultQueue.TryDequeue(out string voiceResult))
		{
			OnTranscriptionResult?.Invoke(voiceResult);
		}
	}

	//Callback from the voice processor when new audio is detected
	private void VoiceProcessorOnOnFrameCaptured(short[] samples)
	{
		if (_running)
			_threadedBufferQueue.Enqueue(samples);
	}

	//Callback from the voice processor when recording stops
	private void VoiceProcessorOnOnRecordingStop()
	{
		Debug.Log("Stopped");
	}

	//Feeds the autio logic into the vosk recorgnizer
	private async Task ThreadedWork()
	{
		voskRecognizerCreateMarker.Begin();
		if (!_recognizerReady)
		{
			UpdateGrammar();

			//Only detect defined keywords if they are specified.
			if (string.IsNullOrEmpty(_grammar))
			{
				_recognizer = new VoskRecognizer(_model, 16000.0f);
			}
			else
			{
				_recognizer = new VoskRecognizer(_model, 16000.0f, _grammar);
			}

			_recognizer.SetMaxAlternatives(MaxAlternatives);
			//_recognizer.SetWords(true);
			_recognizerReady = true;

			Debug.Log("Recognizer ready");
		}

		voskRecognizerCreateMarker.End();

		voskRecognizerReadMarker.Begin();

		while (_running)
		{
			if (_threadedBufferQueue.TryDequeue(out short[] voiceResult))
			{
				lock (_recognizerLock)
				{
					_recognizer.AcceptWaveform(voiceResult, voiceResult.Length);
				}
			}
			else
			{
				await Task.Delay(10); // shorter delay reduces "stop latency"
			}
		}

		voskRecognizerReadMarker.End();
	}
	public void RebuildDefaultGrammar()
	{
		KeyPhrases.Clear();

		BuildKeyPhrasesFromDoors();
		BuildKeyPhrasesFromBridges();
		BuildKeyPhrasesForFlashlight();
		BuildKeyPhrasesForInfoMenu();
		

		_recognizerReady = false;
	}





}
