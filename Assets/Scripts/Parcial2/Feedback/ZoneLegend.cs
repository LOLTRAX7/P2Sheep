using UnityEngine;

// Leyenda de zonas y ayuda de teclas en la esquina inferior izquierda.
// Es sólo feedback: no participa de la IA.
public class ZoneLegend : MonoBehaviour
{
    [SerializeField] private bool _show = true;

    private GUIStyle _titleStyle;
    private GUIStyle _lineStyle;
    private GUIStyle _boxStyle;
    private Texture2D _initialTexture;
    private Texture2D _huntingTexture;
    private Texture2D _safeTexture;
    private Texture2D _exitTexture;

    private void Awake()
    {
        _initialTexture = MakeTexture(Zone.ColorFor(ZoneType.Initial));
        _huntingTexture = MakeTexture(Zone.ColorFor(ZoneType.Hunting));
        _safeTexture = MakeTexture(Zone.ColorFor(ZoneType.Safe));
        _exitTexture = MakeTexture(Zone.ColorFor(ZoneType.Exit));
    }

    private void OnDestroy()
    {
        Destroy(_initialTexture);
        Destroy(_huntingTexture);
        Destroy(_safeTexture);
        Destroy(_exitTexture);
    }

    private void OnGUI()
    {
        if (!_show) return;

        EnsureStyles();

        const float width = 360f;
        const float height = 142f;
        Rect panel = new(10f, Screen.height - height - 10f, width, height);

        GUI.Box(panel, GUIContent.none, _boxStyle);

        float y = panel.y + 8f;
        GUI.Label(new Rect(panel.x + 10f, y, width - 20f, 20f), "ZONAS DEL NIVEL", _titleStyle);
        y += 24f;

        DrawEntry(ref y, panel, _initialTexture, "Zona inicial del Cazador (aislada)");
        DrawEntry(ref y, panel, _huntingTexture, "Coto de caza (sólo Boids y objetos de interés)");
        DrawEntry(ref y, panel, _safeTexture, "Zonas seguras A/B (los lobos no entran)");
        DrawEntry(ref y, panel, _exitTexture, "Salida (después de 3 recolecciones)");

        _lineStyle.normal.textColor = new Color(0.65f, 0.9f, 1f);
        GUI.Label(new Rect(panel.x + 10f, panel.y + height - 24f, width - 20f, 18f),
            "Cazador: FSM + A*. Lobos: patrulla, FoV, aullido y captura.", _lineStyle);
    }

    private void DrawEntry(ref float y, Rect panel, Texture2D swatch, string text)
    {
        GUI.DrawTexture(new Rect(panel.x + 12f, y + 4f, 12f, 12f), swatch);

        _lineStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(panel.x + 32f, y, panel.width - 44f, 18f), text, _lineStyle);

        y += 20f;
    }

    private void EnsureStyles()
    {
        if (_titleStyle != null) return;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.85f, 0.35f) },
        };

        _lineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = false,
        };

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeBlackTexture() },
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static Texture2D MakeBlackTexture()
    {
        return MakeTexture(new Color(0f, 0f, 0f, 0.72f));
    }
}
