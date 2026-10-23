using UnityEngine;

// HUD y Gizmos del Cazador (separado de HunterAgent para que la IA no maneje GUI ni colores).
// Muestra:
//   - estado actual de la FSM, recolecciones (X/RequiredGathers), TBA, lobos a la vista;
//   - Zona segura elegida para huir;
//   - radios de visión, ataque melee y ataque a distancia (Gizmos siempre visibles);
//   - línea hacia cada lobo percibido.
public class HunterFeedback : MonoBehaviour
{
    [SerializeField] private bool _showHud = true;
    [SerializeField] private bool _showGizmos = true;
    [SerializeField] private Color _visionColor = new(0.4f, 1f, 0.5f, 0.55f);
    [SerializeField] private Color _meleeColor = new(1f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color _rangeColor = new(1f, 0.85f, 0.25f, 0.7f);
    [SerializeField] private Color _safeZoneLineColor = new(0.65f, 0.5f, 0.92f, 0.9f);

    private HunterAgent _hunter;
    private GUIStyle _titleStyle;
    private GUIStyle _lineStyle;
    private GUIStyle _boxStyle;
    private LineRenderer _wolfLines;
    private LineRenderer _safeZoneLine;

    private void Awake()
    {
        _wolfLines = CreateLine("LineasDeteccionCazador", 0.1f);
        _safeZoneLine = CreateLine("LineaZonaSegura", 0.12f);
        _safeZoneLine.startColor = _safeZoneLineColor;
        _safeZoneLine.endColor = _safeZoneLineColor;
    }

    private void LateUpdate()
    {
        HunterAgent hunter = Hunter;

        if (hunter == null || !hunter.IsPresent)
        {
            _wolfLines.enabled = false;
            _safeZoneLine.enabled = false;
            return;
        }

        // Líneas hacia los lobos que el Cazador está percibiendo (visibles en la pestaña Game).
        int count = hunter.VisibleWolves.Count;
        _wolfLines.enabled = count > 0;

        if (count > 0)
        {
            _wolfLines.positionCount = count * 2;
            int index = 0;

            foreach (Transform wolf in hunter.VisibleWolves)
            {
                Vector3 start = new(hunter.transform.position.x, 0.45f, hunter.transform.position.z);
                Vector3 end = wolf != null ? new Vector3(wolf.position.x, 0.45f, wolf.position.z) : start;

                _wolfLines.SetPosition(index++, start);
                _wolfLines.SetPosition(index++, end);
            }
        }

        // Línea hacia la Zona segura elegida mientras huye.
        bool showSafe = hunter.CurrentState == HunterAgent.HunterState.Flee && hunter.ChosenSafeZone != null;
        _safeZoneLine.enabled = showSafe;

        if (showSafe)
        {
            _safeZoneLine.SetPosition(0, new Vector3(hunter.transform.position.x, 0.4f, hunter.transform.position.z));
            _safeZoneLine.SetPosition(1, new Vector3(hunter.ChosenSafeZone.Center.x, 0.4f, hunter.ChosenSafeZone.Center.z));
        }
    }

    private LineRenderer CreateLine(string lineName, float width)
    {
        GameObject lineObject = new(lineName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = width;
        line.sharedMaterial = FeedbackMaterials.CreateUnlit(Color.white);
        line.startColor = _meleeColor;
        line.endColor = _meleeColor;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;

        return line;
    }

    private HunterAgent Hunter
    {
        get
        {
            if (_hunter == null) _hunter = HunterAgent.Current;
            return _hunter;
        }
    }

    private void OnGUI()
    {
        if (!_showHud) return;

        HunterAgent hunter = Hunter;
        if (hunter == null) return;

        EnsureStyles();

        const float width = 360f;
        const float height = 188f;
        Rect panel = new(10f, 10f, width, height);

        GUI.Box(panel, GUIContent.none, _boxStyle);

        string state = StateLabel(hunter.CurrentState);
        string gathers = hunter.Gathers + " / " + hunter.RequiredGathers;
        string tba = hunter.IsAttackReady ? "LISTO" : hunter.AttackCooldownRemaining.ToString("0.0") + " s";
        string safeZone = hunter.ChosenSafeZone != null ? hunter.ChosenSafeZone.name : "-";
        string wolves = hunter.VisibleWolfCount.ToString();
        string target = string.IsNullOrEmpty(hunter.DebugTarget) ? "-" : hunter.DebugTarget;
        string action = string.IsNullOrEmpty(hunter.DebugAction) ? "-" : hunter.DebugAction;

        float y = panel.y + 8f;

        GUI.Label(new Rect(panel.x + 10f, y, width - 20f, 22f), "CAZADOR - FSM", _titleStyle);
        y += 24f;

        DrawLine(ref y, panel, "Estado: ", state);
        DrawLine(ref y, panel, "Recolecciones: ", gathers + "   (TBA " + tba + ")");
        DrawLine(ref y, panel, "Lobos a la vista: ", wolves);
        DrawLine(ref y, panel, "Objetivo: ", target);
        DrawLine(ref y, panel, "Zona segura elegida: ", safeZone);
        DrawLine(ref y, panel, "Acción: ", action);
        DrawLine(ref y, panel, "Prioridad: ", "Dead > Flee > Exit > Gather > Attack > Search");
    }

    private void DrawLine(ref float y, Rect panel, string label, string value)
    {
        _lineStyle.normal.textColor = new Color(0.78f, 0.82f, 0.9f);
        GUI.Label(new Rect(panel.x + 10f, y, 155f, 18f), label, _lineStyle);

        _lineStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(panel.x + 165f, y, panel.width - 175f, 18f), value, _lineStyle);

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
            normal = { background = MakeTexture(new Color(0f, 0f, 0f, 0.72f)) },
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static string StateLabel(HunterAgent.HunterState state)
    {
        return state switch
        {
            HunterAgent.HunterState.Attack => "ATTACK",
            HunterAgent.HunterState.Gather => "GATHER",
            HunterAgent.HunterState.Flee => "FLEE",
            HunterAgent.HunterState.Exit => "EXIT",
            HunterAgent.HunterState.Dead => "DEAD",
            _ => "SEARCH",
        };
    }

    private void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        HunterAgent hunter = Hunter;
        if (hunter == null) return;

        Vector3 position = hunter.transform.position;

        // Radios del Cazador, siempre visibles.
        Gizmos.color = _visionColor;
        Gizmos.DrawWireSphere(position, hunter.VisionRadius);

        Gizmos.color = _meleeColor;
        Gizmos.DrawWireSphere(position, hunter.MeleeAttackRadius);

        Gizmos.color = _rangeColor;
        Gizmos.DrawWireSphere(position, hunter.RangeAttackRadius);

        // Todas las líneas de percepción hacia los lobos visibles.
        Gizmos.color = Color.red;
        foreach (Transform wolf in hunter.VisibleWolves)
        {
            if (wolf == null) continue;
            Gizmos.DrawLine(position + Vector3.up * hunter.EyeHeight, wolf.position + Vector3.up * hunter.EyeHeight);
        }

        // Zona segura elegida para huir.
        if (hunter.CurrentState == HunterAgent.HunterState.Flee && hunter.ChosenSafeZone != null)
        {
            Gizmos.color = _safeZoneLineColor;
            Gizmos.DrawLine(position + Vector3.up * 0.3f, hunter.ChosenSafeZone.Center + Vector3.up * 0.3f);
            Gizmos.DrawWireSphere(hunter.ChosenSafeZone.Center, 0.6f);
        }

        // Si ya está a salvo, un anillo violeta alrededor del Cazador.
        if (hunter.IsInSafeZone)
        {
            Gizmos.color = _safeZoneLineColor;
            Gizmos.DrawWireSphere(position, 1.1f);
        }
    }
}
