using System.Collections.Generic;
using UnityEngine;

// Registro de eventos para el feedback del Parcial 2 (quién detectó, quién aulló, quién recibió
// la alerta, cuándo se usa A*...). Sólo guarda y muestra texto: no toma ninguna decisión de IA.
// Uso:  FeedbackLog.Add("Lobo_2 detectó al Cazador", Color.red);
// FeedbackLogHUD lo dibuja en pantalla.
public static class FeedbackLog
{
    public struct Entry
    {
        public float Time;
        public string Message;
        public Color Color;
    }

    private const int MaxEntries = 60;
    private static readonly List<Entry> _entries = new List<Entry>();

    public static IReadOnlyList<Entry> Entries => _entries;

    // Si las "Enter Play Mode Options" están con Domain Reload apagado, la lista static sobrevive entre Plays.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _entries.Clear();
    }

    public static void Add(string message, Color color, bool alsoLogToConsole = true)
    {
        Entry entry = new Entry { Time = Time.time, Message = message, Color = color };

        _entries.Add(entry);
        if (_entries.Count > MaxEntries) _entries.RemoveAt(0);

        if (alsoLogToConsole) Debug.Log(message);
    }

    public static void Add(string message)
    {
        Add(message, Color.white);
    }
}
