using UnityEngine;
using System.Text.RegularExpressions;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class VoskDialogText : MonoBehaviour
{
    public VoskSpeechToText VoskSpeechToText;
    public TextMeshProUGUI DialogText;
    [Tooltip("Seconds before the dialog clears")]
    public float ClearDelay = 5f;

    private Coroutine _clearCoroutine;

    [Header("Voice Door Range")]
    [SerializeField] private Transform playerTransform;  
    [SerializeField] private float voiceDoorRange = 2.5f; 
    [SerializeField] private bool planarDistanceOnly = true; //ignore height difference

    [Header("Typewriter")]
    [SerializeField] private bool typewriterEnabled = true;
    [SerializeField] private float charsPerSecond = 35f;
    [SerializeField] private float punctuationExtraDelay = 0.06f; // adds a small pause on .,!?

    [Header("Typewriter SFX")]
    [SerializeField] private AudioSource typeSfxSource; 
    [SerializeField] private AudioClip typeSfxClip;      
    [SerializeField] private float typeSfxVolume = 0.6f;
    [SerializeField] private float typeSfxMinInterval = 0.03f; 
    [Header("Flashlight")]
    [SerializeField] private GameObject flashlightRoot; 
    [SerializeField] private Light flashlightLight;    
    [Header("Info Menu")]
    [SerializeField] private bool enableInfoMenu = true;
    [SerializeField] private bool infoMenuActive = false;
    [Header("Info Menu - SFX")]
    [SerializeField] private AudioSource uiBeepSource;  
    [SerializeField] private AudioClip menuEnterBeep;
    [SerializeField] private AudioClip menuSelectBeep;
    [SerializeField] private AudioClip menuBackBeep;
    [SerializeField] private AudioClip menuErrorBeep;
    [SerializeField, Range(0f, 1f)] private float menuBeepVolume = 0.8f;

    [Header("Info Menu - Timeout")]
    [SerializeField] private float menuTimeoutSeconds = 12f; 
    [Header("Info Menu Text Timeout Bar")]
    [SerializeField] private bool showTextTimeoutBar = true;

    [SerializeField] private string timeBlockFull = "■";
    [SerializeField] private string timeBlockEmpty = "-";

    [SerializeField] private string timeBarPrefix = "TIME  ";

    private float _menuTimeoutRemaining;

    private Coroutine _menuTimeoutCoroutine;


    // menu stack (keys)
    private readonly Stack<string> _menuStack = new Stack<string>();

    // TMP alignment restore
    private TextAlignmentOptions _prevAlignment;
    private bool _alignmentSaved = false;


    private Coroutine _typeCoroutine;
    private float _nextSfxTime;


    // 🔍 Regex patterns for English voice commands
    Regex hello_regex = new Regex(@"\b(hi|hello|hey)\b", RegexOptions.IgnoreCase);
    Regex openDoor_regex = new Regex(@"\b(open|unlock).*(door)\b", RegexOptions.IgnoreCase);
    Regex closeDoor_regex = new Regex(@"\b(close|shut).*(door)\b", RegexOptions.IgnoreCase);
    Regex flashlightOn_regex = new Regex(@"\b(flashlight|light).*(on)\b", RegexOptions.IgnoreCase);
    Regex flashlightOff_regex = new Regex(@"\b(flashlight|light).*(off|of)\b", RegexOptions.IgnoreCase);
    Regex howru_regex = new Regex(@"\b(how).(are).(you)\b", RegexOptions.IgnoreCase);
    Regex help_regex = new Regex(@"\b(help|assist|guide)\b", RegexOptions.IgnoreCase);
    Regex infoMenuOpen_regex = new Regex(@"\b(information|info|menu|help)\b", RegexOptions.IgnoreCase);
    Regex back_regex = new Regex(@"\b(back)\b", RegexOptions.IgnoreCase);
    Regex exit_regex = new Regex(@"\b(exit)\b", RegexOptions.IgnoreCase);



    // "extend bridge B thirty two"
    // "extend bee thirty two"
    // "extend be thirty two"
    // "extend b 32"
    Regex extendBridge_regex = new Regex(
        @"\b(extend|open|deploy|activate)\s*(bridge\s*)?(?<id>(b|bee|be)\s+((\d+)|([a-z]+(\s+[a-z]+)*)))\b",
        RegexOptions.IgnoreCase
    );

    // "retract bridge B thirty two", "close bee thirty two", etc.
    Regex retractBridge_regex = new Regex(
        @"\b(retract|close|deactivate|pull\s*back)\s*(bridge\s*)?(?<id>(b|bee|be)\s+((\d+)|([a-z]+(\s+[a-z]+)*)))\b",
        RegexOptions.IgnoreCase
    );


    // "open door A forty five", "open a 45", "unlock door a45"
    Regex openDoorId_regex = new Regex(
        @"\b(open|unlock)\s*(door\s*)?(?<id>(a|ay|en|n)\s+((\d+)|([a-z]+(\s+[a-z]+)*)))\b",
        RegexOptions.IgnoreCase
    );

    // "close door A forty five", "shut a 45"
    Regex closeDoorId_regex = new Regex(
        @"\b(close|shut)\s*(door\s*)?(?<id>(a|ay|en|n)\s+((\d+)|([a-z]+(\s+[a-z]+)*)))\b",
        RegexOptions.IgnoreCase
    );
    Regex flashlightToggle_regex = new Regex(@"\b(light|flashlight)\b", RegexOptions.IgnoreCase);







    void Awake()
    {
        VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
        BuildInfoMenu();

    }

    void OnDestroy()
    {
        if (VoskSpeechToText != null)
            VoskSpeechToText.OnTranscriptionResult -= OnTranscriptionResult;
    }

    
    void Say(string response)
    {
        Debug.Log("AI says: " + response);
    }

    void AddResponse(string response)
    {
        
        if (infoMenuActive) return;
        Say(response);

        if (DialogText == null) return;

        // Stop previous coroutines (typing + clear)
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        if (_clearCoroutine != null) StopCoroutine(_clearCoroutine);

        if (!typewriterEnabled)
        {
            DialogText.text = response + "\n";
            _clearCoroutine = StartCoroutine(ClearAfterDelayRealtime());
            return;
        }

        _typeCoroutine = StartCoroutine(TypeRoutine(response));
    }


    private IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSeconds(ClearDelay);

        if (infoMenuActive) 
        {
            _clearCoroutine = null;
            yield break;
        }

        if (DialogText != null)
            DialogText.text = "";

        _clearCoroutine = null;
    }

    private IEnumerator ClearAfterDelayRealtime()
    {
        yield return new WaitForSecondsRealtime(ClearDelay);

        if (infoMenuActive) 
        {
            _clearCoroutine = null;
            yield break;
        }

        if (DialogText != null)
            DialogText.text = "";

        _clearCoroutine = null;
    }



    private void OnTranscriptionResult(string obj)
    {
        Debug.Log("Recognized: " + obj);
        var result = new RecognitionResult(obj);

        foreach (RecognizedPhrase p in result.Phrases)
        {
            string text = p.Text.ToLower();
            // --- INFO MENU ---
            if (enableInfoMenu) //INFO MENU snippet generated with ChatGPT
            {
                // open menu (only from gameplay mode)
                if (!infoMenuActive && infoMenuOpen_regex.IsMatch(text))
                {
                    EnterMenu();
                    return;
                }

                // if menu is active, hijack input
                if (infoMenuActive)
                {
                    ResetMenuTimeout();

                    if (exit_regex.IsMatch(text))
                    {
                        PlayMenuBeep(menuBackBeep);
                        ExitMenu("Closing information.");
                        return;
                    }

                    if (back_regex.IsMatch(text))
                    {
                        PlayMenuBeep(menuBackBeep);

                        if (_menuStack.Count > 1) _menuStack.Pop();
                        UpdateMenuText();
                        return;
                    }

                    // option selection
                    string currentKey = _menuStack.Peek();
                    if (_menu.TryGetValue(currentKey, out var node) && node.type == MenuNodeType.Submenu)
                    {
                        // because grammar only contains current options, this will be clean
                        if (node.next != null && node.next.TryGetValue(text, out var nextKey) && _menu.TryGetValue(nextKey, out var nextNode))
                        {
                            if (nextNode.type == MenuNodeType.Submenu)
                            {
                                PlayMenuBeep(menuSelectBeep);
                                _menuStack.Push(nextKey);
                                UpdateMenuText();
                                return;
                            }
                            else // Final
                            {
                                // leave menu, answer
                                PlayMenuBeep(menuSelectBeep);
                                string answer = nextNode.finalAnswer != null ? nextNode.finalAnswer() : "...";
                                ExitMenu(answer);
                                return;
                            }
                        }
                        else
                        {
                            PlayMenuBeep(menuErrorBeep);
                            UpdateMenuText();
                            return;
                        }
                    }

                    
                    UpdateMenuText();
                    return;
                }
            }



           
            if (TryHandleBridgeExtend(text)) return;
            if (TryHandleBridgeRetract(text)) return;

            
            if (TryHandleDoorOpen(text)) return;
            if (TryHandleDoorClose(text)) return;

            
            if (hello_regex.IsMatch(text))
            {
                AddResponse("Hello");
                return;
            }

            if (flashlightOn_regex.IsMatch(text))
            {
                SetFlashlight(true);
                return;
            }

            if (flashlightOff_regex.IsMatch(text))
            {
                SetFlashlight(false);
                return;
            }

            
            if (flashlightToggle_regex.IsMatch(text))
            {
                ToggleFlashlight();
                return;
            }


            if (help_regex.IsMatch(text))
            {
                AddResponse("No one can help you now...");
                return;
            }

            if (howru_regex.IsMatch(text))
            {
                AddResponse("Doing fine, what about you?");
                return;
            }
        }

        // If no match found
        AddResponse("I didn't quite get that, please repeat");
    }

    

    private bool TryHandleBridgeExtend(string lowerText)
    {
        var m = extendBridge_regex.Match(lowerText);
        if (!m.Success) return false;

        string raw = m.Groups["id"].Value.Trim();   // e.g. "bee thirty two"

        // 1) Normalize "bee"/"be" → "b"
        raw = Regex.Replace(raw, @"^(bee|be)\b", "b", RegexOptions.IgnoreCase);

        // 2) Split: first token = letter, rest = number words
        string[] parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false; // need at least "b something"

        string letter = parts[0]; // "b"
        string numberPart = string.Join(" ", parts, 1, parts.Length - 1); // "thirty two" / "32"

        // 3) Try parse number words → int
        string finalId;
        if (NumberWordParser.TryParseNumberWords(numberPart, out int num))
        {
            // build B-32, B-15, etc.
            finalId = $"{letter}-{num}";
        }
        else
        {
            // fallback: maybe Vosk already gave digits ("b 32")
            finalId = raw;
        }

        string normalized = BridgeRegistry.NormalizeBridgeId(finalId);

        if (BridgeRegistry.TryGetBridge(normalized, out var bridge))
        {
            bridge.Extend();
            AddResponse($"Extending bridge {normalized}...");
        }
        else
        {
            AddResponse($"I don't see any bridge named {normalized}.");
        }

        return true;
    }
    private bool IsPlayerCloseEnoughToDoor(DoorSlideUp door) //method generated with ChatGPT
    {
        if (door == null) return false;

        // Auto-find if not assigned (safe fallback)
        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform == null) return false;

        Vector3 a = playerTransform.position;
        Vector3 b = door.transform.position;

        if (planarDistanceOnly)
        {
            a.y = 0f;
            b.y = 0f;
        }

        return Vector3.Distance(a, b) <= voiceDoorRange;
    }

    private void SetFlashlight(bool on)
    {
        if (flashlightRoot != null)
        {
            flashlightRoot.SetActive(on);
            AddResponse(on ? "Light on." : "Light off.");
            return;
        }

        if (flashlightLight != null)
        {
            flashlightLight.enabled = on;
            AddResponse(on ? "Light on." : "Light off.");
            return;
        }

        AddResponse("No flashlight assigned.");
    }

    private void ToggleFlashlight()
    {
        if (flashlightRoot != null)
        {
            SetFlashlight(!flashlightRoot.activeSelf);
            return;
        }

        if (flashlightLight != null)
        {
            SetFlashlight(!flashlightLight.enabled);
            return;
        }

        AddResponse("No flashlight assigned.");
    }


    private bool TryHandleBridgeRetract(string lowerText)
    {
        var m = retractBridge_regex.Match(lowerText);
        if (!m.Success) return false;

        string raw = m.Groups["id"].Value.Trim();

        raw = Regex.Replace(raw, @"^(bee|be)\b", "b", RegexOptions.IgnoreCase);

        string[] parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        string letter = parts[0];
        string numberPart = string.Join(" ", parts, 1, parts.Length - 1);

        string finalId;
        if (NumberWordParser.TryParseNumberWords(numberPart, out int num))
        {
            finalId = $"{letter}-{num}";
        }
        else
        {
            finalId = raw;
        }

        string normalized = BridgeRegistry.NormalizeBridgeId(finalId);

        if (BridgeRegistry.TryGetBridge(normalized, out var bridge))
        {
            bridge.Retract();
            AddResponse($"Retracting bridge {normalized}...");
        }
        else
        {
            AddResponse($"There is no bridge {normalized} to retract.");
        }

        return true;
    }
    private bool TryHandleDoorOpen(string lowerText)
    {
        var m = openDoorId_regex.Match(lowerText);
        if (!m.Success) return false;

        string raw = m.Groups["id"].Value.Trim(); // "a forty five"

        // Normalize spoken letter
        raw = Regex.Replace(raw, @"^(ay|en)\b", "N", RegexOptions.IgnoreCase);

        string[] parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        string letter = parts[0]; // "a"
        string numberPart = string.Join(" ", parts, 1, parts.Length - 1);

        string finalId;
        if (NumberWordParser.TryParseNumberWords(numberPart, out int num))
            finalId = $"{letter}-{num}";
        else
            finalId = raw;

        string normalized = DoorRegistry.NormalizeDoorId(finalId);

        if (DoorRegistry.TryGetDoor(normalized, out var door))
        {
            if (door.IsLocked)
            {
                AddResponse($"Door {normalized} is locked.");
                return true;
            }

            if (!IsPlayerCloseEnoughToDoor(door))
            {
                AddResponse($"You are too far from door {normalized}.");
                return true;
            }

            door.OpenDoor();
            AddResponse($"Opening door {normalized}...");
        }
        else
        {
            AddResponse($"I can't access door {normalized}.");
        }

        return true;
    }
    private bool TryHandleDoorClose(string lowerText)
    {
        var m = closeDoorId_regex.Match(lowerText);
        if (!m.Success) return false;

        string raw = m.Groups["id"].Value.Trim();

        raw = Regex.Replace(raw, @"^(ay|en)\b", "N", RegexOptions.IgnoreCase);

        string[] parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        string letter = parts[0];
        string numberPart = string.Join(" ", parts, 1, parts.Length - 1);

        string finalId;
        if (NumberWordParser.TryParseNumberWords(numberPart, out int num))
            finalId = $"{letter}-{num}";
        else
            finalId = raw;

        string normalized = DoorRegistry.NormalizeDoorId(finalId);

        if (DoorRegistry.TryGetDoor(normalized, out var door))
        {
            if (door.IsLocked)
            {
                AddResponse($"Door {normalized} is locked.");
                return true;
            }

            if (!IsPlayerCloseEnoughToDoor(door))
            {
                AddResponse($"You are too far from door {normalized}.");
                return true;
            }

            door.CloseDoor();
            AddResponse($"Closing door {normalized}...");

        }
        else
        {
            AddResponse($"I can't access door {normalized} to close.");
        }

        return true;
    }
    private IEnumerator TypeRoutine(string response)
    {
        DialogText.text = ""; // start empty

        float secondsPerChar = (charsPerSecond <= 0.01f) ? 0.01f : (1f / charsPerSecond);
        _nextSfxTime = 0f;

        for (int i = 0; i < response.Length; i++)
        {
            char c = response[i];
            DialogText.text += c;

            
            TryPlayTypeSfx(c);

            
            float delay = secondsPerChar;

            // small extra pause on punctuation (for some *style*)
            if (c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':')
                delay += punctuationExtraDelay;

            yield return new WaitForSecondsRealtime(delay);
        }

        DialogText.text += "\n";

        _typeCoroutine = null;

        
        _clearCoroutine = StartCoroutine(ClearAfterDelayRealtime());
    }

    private void TryPlayTypeSfx(char c)
    {
        if (typeSfxClip == null) return;

        // Don’t tick on whitespace
        if (char.IsWhiteSpace(c)) return;

        // throttle
        float now = Time.unscaledTime;
        if (now < _nextSfxTime) return;
        _nextSfxTime = now + typeSfxMinInterval;

        // If no AudioSource assigned, create/play via PlayClipAtPoint (not ideal but works)
        if (typeSfxSource != null)
        {
            typeSfxSource.PlayOneShot(typeSfxClip, typeSfxVolume);
        }
        else
        {
            // fallback: lightweight one-shot at camera (optional)
            AudioSource.PlayClipAtPoint(typeSfxClip, Camera.main ? Camera.main.transform.position : Vector3.zero, typeSfxVolume);
        }
    }
    public void ForceDialog(string message, bool clearPrevious = true) //method generated with ChatGPT
    {
        if (DialogText == null) return;

        // Stop current typing + clear timer
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        if (_clearCoroutine != null) StopCoroutine(_clearCoroutine);

        if (clearPrevious)
            DialogText.text = "";

        // Use the same pipeline as voice responses
        AddResponse(message);
    }
    private enum MenuNodeType { Submenu, Final }

    private struct MenuNode
    {
        public string title;
        public MenuNodeType type;
        public List<string> options;                    // words shown on screen
        public Dictionary<string, string> next;         // option -> node key
        public Func<string> finalAnswer;                // for Final nodes
    }

    private Dictionary<string, MenuNode> _menu;
    private void BuildInfoMenu() //method generated with ChatGPT
    {
        _menu = new Dictionary<string, MenuNode>(StringComparer.OrdinalIgnoreCase);

        _menu["root"] = new MenuNode
        {
            title = "INFORMATION",
            type = MenuNodeType.Submenu,
            options = new List<string> { "goal", "light","doors", "tutorials", "more"},
            next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "goal", "goal" },
                { "light", "light" },
                { "doors", "doors" },
                { "tutorials", "tutorials" },
                { "more", "more" }
            }
        };

        _menu["goal"] = new MenuNode
        {
            title = "GOAL  ▶",
            type = MenuNodeType.Final,
            finalAnswer = () =>
            {
                var mm = MissionManager.Instance;
                if (mm == null)
                    return "No mission system detected.";

                var step = mm.GetCurrentStep();
                if (step == null)
                    return "No active objective.";

                // If objectiveText is empty, show something sensible.
                if (string.IsNullOrWhiteSpace(step.objectiveText))
                    return "Objective text is missing.";

                // Optional: include mission/step id for debugging (remove if you want)
                // return $"{mm.CurrentMission?.missionId} / {step.stepId}: {step.objectiveText}";

                return step.objectiveText.Trim();
            }
        };
        _menu["light"] = new MenuNode
        {
            title = "LIGHT",
            type = MenuNodeType.Final,
            finalAnswer = () => "Say 'Light' to turn on your head light"

        };
       
        _menu["doors"] = new MenuNode
        {
            title = "DOORS",
            type = MenuNodeType.Final,
            finalAnswer = () => "Say 'Open/close [Door ID]' to open/close a door"

        };
        _menu["tutorials"] = new MenuNode
        {
            title = "TUTORIALS",
            type = MenuNodeType.Submenu,
            options = new List<string> { "vents", "stealth", "bridges", "moc" },
            next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "vents", "vents" },
                { "bridges", "bridges" },
                { "moc", "moc" },
                { "stealth", "stealth" }
            }
        };
         _menu["bridges"] = new MenuNode
        {
            title = "BRIDGES",
            type = MenuNodeType.Final,
            finalAnswer = () => "Say 'Extend/retract [Bridge ID]' to extend/retract the bridge"

        };
        _menu["moc"] = new MenuNode
        {
            title = "MOC",
            type = MenuNodeType.Final,
            finalAnswer = () => "Press [TAB] to access your [MOC].\n The [MOC] displays what [Crawler Units MK II] currently detect.\n The [Ventilation System] is also shown for [rapid access when required]."

        };
        _menu["vents"] = new MenuNode
        {
            title = "VENTS",
            type = MenuNodeType.Final,
            finalAnswer = () => "You can interact with objects and vents by pressing [E].\n Vents allow you to [muffle your voice] and [redirect it to another location]."

        };
        _menu["stealth"] = new MenuNode
        {
            title = "STEALTH",
            type = MenuNodeType.Final,
            finalAnswer = () => "Use [LShift] to [sprint].\n Use [LCtrl] to [crouch]."

        };

        _menu["more"] = new MenuNode
        {
            title = "MORE",
            type = MenuNodeType.Submenu,
            options = new List<string> { "anxious", "weather" },
            next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "anxious", "anxious" },
                { "weather", "weather" }
            }
        };
        _menu["doors"] = new MenuNode
        {
            title = "say 'Open/Close [door ID]' to open/close a door",
            type = MenuNodeType.Submenu,
            options = new List<string> { "N-85 [your office]","N-99 [locker-room]","N-11 [Entrance Reception]","N-20&N-27 [Security Check]","N-41 [Entrance Common Room-Offices]" },
            next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "N-85 [your office]", "N-85 [your office]" },
                { "N-99 [locker-room]", "N-99 [locker-room]" },
                { "N-11 [Entrance Reception]", "N-11 [Entrance Reception]" },
                { "N-20&N-27 [Security Check]", "N-20&N-27 [Security Check]" },
                { "N-41 [Entrance Common Room-Offices]", "N-41 [Entrance Common Room-Offices]" }
            }
        };

        _menu["anxious"] = new MenuNode
        {
            title = "ANXIOUS",
            type = MenuNodeType.Final,
            finalAnswer = () => "Slow breaths. Stay away from anything that could cause anxiety"
        };

        _menu["weather"] = new MenuNode
        {
            title = "WEATHER",
            type = MenuNodeType.Final,
            finalAnswer = () => "Outside is a beautiful 47C° with no clouds in sight"
        };
    }
    private void EnterMenu()//method generated with ChatGPT
    {
        infoMenuActive = true;

        
        StopDialogCoroutines();

        _menuStack.Clear();
        _menuStack.Push("root");

        PlayMenuBeep(menuEnterBeep);
        ResetMenuTimeout();
        UpdateMenuText();

    }

    private void UpdateMenuText()//method generated with ChatGPT
    {
        if (DialogText == null) return;

        StopDialogCoroutines(); // critical: menu owns the text

        // Force left alignment
        if (!_alignmentSaved)
        {
            _prevAlignment = DialogText.alignment;
            _alignmentSaved = true;
        }
        DialogText.alignment = TextAlignmentOptions.Left;

        string key = _menuStack.Peek();
        if (!_menu.TryGetValue(key, out var node)) return;

        var sb = new System.Text.StringBuilder();

        // --- Title ---
        sb.AppendLine(node.title);
        sb.AppendLine("--------------------");

        // --- Options ---
        if (node.type == MenuNodeType.Submenu)
        {
            for (int i = 0; i < node.options.Count; i++)
            {
                string opt = node.options[i];
                string nextKey = node.next[opt];
                bool isFinal =
                    _menu.TryGetValue(nextKey, out var child) &&
                    child.type == MenuNodeType.Final;

                sb.AppendLine($"- {opt}{(isFinal ? "  >" : "")}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Say: back / exit");

        // --- TIME BAR ---
        if (showTextTimeoutBar && menuTimeoutSeconds > 0f)
        {
            sb.AppendLine();
            sb.AppendLine(BuildTimeBar());
        }

        DialogText.text = sb.ToString();

        ApplyMenuGrammar(node);
    }
    private string BuildTimeBar()//method generated with ChatGPT
    {
        int total = Mathf.RoundToInt(menuTimeoutSeconds);
        int remaining = Mathf.Clamp(
            Mathf.CeilToInt(_menuTimeoutRemaining),
            0,
            total
        );

        var sb = new System.Text.StringBuilder();
        sb.Append(timeBarPrefix);

        for (int i = 0; i < total; i++)
        {
            if (i < remaining)
                sb.Append($"[{timeBlockFull}]");
            else
                sb.Append($"[{timeBlockEmpty}]");
        }

        return sb.ToString();
    }


    private void ExitMenu(string response = null)//method generated with ChatGPT
    {
        infoMenuActive = false;

        if (_menuTimeoutCoroutine != null)
        {
            StopCoroutine(_menuTimeoutCoroutine);
            _menuTimeoutCoroutine = null;
        }

        // restore alignment
        if (DialogText != null && _alignmentSaved)
            DialogText.alignment = _prevAlignment;

        _alignmentSaved = false;

        RestoreDefaultGrammar();

        if (!string.IsNullOrEmpty(response))
            AddResponse(response);
        else if (DialogText != null)
            DialogText.text = "";
    }




    private void ApplyMenuGrammar(MenuNode node)//method generated with ChatGPT
    {
        if (VoskSpeechToText == null) return;

        var phrases = new List<string>();

        // only options that are on screen
        if (node.type == MenuNodeType.Submenu && node.options != null)
            phrases.AddRange(node.options);

        // always allow navigation inside menu
        phrases.Add("back");
        phrases.Add("exit");

        VoskSpeechToText.SetKeyPhrasesRuntime(phrases);
    }

    private void RestoreDefaultGrammar()//method generated with ChatGPT
    {
        if (VoskSpeechToText == null) return;

        // Rebuild *normal* grammar: doors + bridges + light + information.
        // We don’t have direct access to those build methods here, so simplest is:
        // call StartVoskStt again with startMicrophone:false and NO override list,
        // OR you expose a public RebuildDefaultGrammar() on VoskSpeechToText.

        // ✅ Best: add this public method to VoskSpeechToText and call it here.
        VoskSpeechToText.RebuildDefaultGrammar();
    }
    private void PlayMenuBeep(AudioClip clip)//method generated with ChatGPT
    {
        if (clip == null) return;

        if (uiBeepSource != null)
        {
            uiBeepSource.PlayOneShot(clip, menuBeepVolume);
        }
        else
        {
            // fallback
            AudioSource.PlayClipAtPoint(clip, Camera.main ? Camera.main.transform.position : Vector3.zero, menuBeepVolume);
        }
    }

    private void StopDialogCoroutines()//method generated with ChatGPT
    {
        if (_typeCoroutine != null) { StopCoroutine(_typeCoroutine); _typeCoroutine = null; }
        if (_clearCoroutine != null) { StopCoroutine(_clearCoroutine); _clearCoroutine = null; }
    }

    private void ResetMenuTimeout() //method generated with ChatGPT
    {
        if (menuTimeoutSeconds <= 0f) return;

        _menuTimeoutRemaining = menuTimeoutSeconds;

        if (_menuTimeoutCoroutine != null)
            StopCoroutine(_menuTimeoutCoroutine);

        _menuTimeoutCoroutine = StartCoroutine(MenuTimeoutRoutine());
    }


    private IEnumerator MenuTimeoutRoutine() //method generated with ChatGPT
    {
        if (menuTimeoutSeconds <= 0f)
            yield break;

        _menuTimeoutRemaining = menuTimeoutSeconds;

        while (infoMenuActive && _menuTimeoutRemaining > 0f)
        {
            UpdateMenuText(); // redraw menu + bar
            yield return new WaitForSecondsRealtime(1f);
            _menuTimeoutRemaining -= 1f;
        }

        _menuTimeoutCoroutine = null;

        if (infoMenuActive)
        {
            PlayMenuBeep(menuErrorBeep);
            ExitMenu("Menu timed out.");
        }
    }









}
