using System;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public static class GameEvents
{
    public struct EventData
    {
        public string key;
        public object payload;

        public EventData(string key, object payload = null)
        {
            this.key = key;
            this.payload = payload;
        }
    }

    public static event Action<EventData> OnEvent;

    public static void Raise(string key, object payload = null)
    {
        OnEvent?.Invoke(new EventData(key, payload));
    }
}
