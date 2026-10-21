using System.Collections.Generic;
using UnityEngine;

// Panel en pantalla (arriba a la derecha) con los últimos eventos del FeedbackLog.
// Usa OnGUI como el HUD del Cazador del Parcial 1, que queda arriba a la izquierda: no se pisan.
public class FeedbackLogHUD : MonoBehaviour
{
    [SerializeField] private bool _show = true;
    [SerializeField] private int _maxVisibleLines = 12;
    [SerializeField] private float _lineLifetime = 15f;
    [SerializeField] private float _panelWidth = 540f;

    private const float LineHeight = 20f;

    private readonly List<FeedbackLog.Entry> _visible = new();
    private GUIStyle _lineStyle;
    private GUIStyle _titleStyle;

    private void OnGUI()
    {
        if (!_show) return;

        EnsureStyles();

        IReadOnlyList<FeedbackLog.Entry> entries = FeedbackLog.Entries;
        int first = Mathf.Max(0, entries.Count - _maxVisibleLines);

        _visible.Clear();
        for (int i = first; i < entries.Count; i++)
        {
            if (Time.time - entries[i].Time <= _lineLifetime) _visible.Add(entries[i]);
        }

        if (_visible.Count == 0) return;

        float height = 28f + _visible.Count * LineHeight;
        Rect panel = new(Screen.width - _panelWidth - 10f, 10f, _panelWidth, height);

        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, _panelWidth - 16f, 20f), "Eventos: percepción / alerta / A*", _titleStyle);

        for (int i = 0; i < _visible.Count; i++)
        {
            FeedbackLog.Entry entry = _visible[i];
            _lineStyle.normal.textColor = entry.Color;

            Rect line = new(panel.x + 8f, panel.y + 28f + i * LineHeight, _panelWidth - 16f, LineHeight);
            GUI.Label(line, "[" + entry.Time.ToString("0.0") + "s] " + entry.Message, _lineStyle);
        }
    }

    private void EnsureStyles()
    {
        if (_lineStyle != null) return;

        _lineStyle = new GUIStyle(GUI.skin.label);
        _lineStyle.fontSize = 12;
        _lineStyle.wordWrap = false;
        _lineStyle.clipping = TextClipping.Clip;

        _titleStyle = new GUIStyle(GUI.skin.label);
        _titleStyle.fontSize = 13;
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.normal.textColor = Color.white;
    }
}
