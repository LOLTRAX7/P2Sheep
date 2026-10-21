using UnityEngine;

// Feedback de un lobo (todo lo que se ve y se oye), separado de WolfAgent para que la IA no maneje colores, líneas ni sonido.
// Cubre lo que pide la consigna (1.10):
//   - cuándo detecta al Cazador       -> lobo en rojo, línea roja lobo -> Cazador, mensaje en el panel
//   - cuándo se emite la alerta       -> sonido de aullido, anillo que se expande, mensaje
//   - cuándo los demás la reciben     -> destello celeste y mensaje
//   - qué posición conocida usan      -> marcador + línea en esa posición y coordenadas en la etiqueta
//   - cuándo se usa Pathfinding       -> camino A* dibujado (PathVisual) y "A*: N nodos" en la etiqueta
[RequireComponent(typeof(WolfAgent))]
public class WolfFeedback : MonoBehaviour
{
    [Header("Colores por estado")]
    [SerializeField] private Color _patrolColor = new(0.75f, 0.75f, 0.78f);
    [SerializeField] private Color _goToPositionColor = new(1f, 0.55f, 0.1f);
    [SerializeField] private Color _chaseColor = new(1f, 0.15f, 0.15f);
    [SerializeField] private Color _searchColor = new(1f, 0.9f, 0.2f);
    [SerializeField] private Color _alertFlashColor = new(0.2f, 0.9f, 1f);

    [Header("Etiqueta en pantalla")]
    [SerializeField] private bool _showLabel = true;

    private WolfAgent _wolf;
    private GraphNavigator _navigator;
    private ColorFeedback _colorFeedback;
    private AudioSource _audio;

    private Transform _marker;
    private LineRenderer _markerLine;
    private LineRenderer _detectLine;
    private GUIStyle _labelStyle;

    private void Awake()
    {
        _wolf = GetComponent<WolfAgent>();
        _navigator = GetComponent<GraphNavigator>();
        _colorFeedback = GetComponent<ColorFeedback>();

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        _audio.volume = 0.6f;

        CreateMarker();
        _markerLine = CreateLine("LineaPosicionConocida", 0.07f);
        _detectLine = CreateLine("LineaDeteccion", 0.12f);
    }

    private void OnEnable()
    {
        _wolf.OnStateChanged += HandleStateChanged;
        _wolf.OnHowled += HandleHowled;
        _wolf.OnAlertReceived += HandleAlertReceived;
    }

    private void OnDisable()
    {
        _wolf.OnStateChanged -= HandleStateChanged;
        _wolf.OnHowled -= HandleHowled;
        _wolf.OnAlertReceived -= HandleAlertReceived;
    }

    private void OnDestroy()
    {
        if (_marker != null) Destroy(_marker.gameObject);
    }

    private void LateUpdate()
    {
        UpdateKnownPositionMarker();
        UpdateDetectionLine();
    }

    // ---------- Reacciones a los eventos del lobo ----------

    private void HandleStateChanged(WolfAgent.WolfState state)
    {
        _colorFeedback.SetColor(ColorFor(state));
    }

    private void HandleHowled(Vector3 position)
    {
        HowlEffect.Spawn(transform.position, new Color(1f, 0.6f, 0.1f));
        _audio.PlayOneShot(HowlSound.Clip);
    }

    private void HandleAlertReceived(WolfAgent source, Vector3 position)
    {
        _colorFeedback.Flash(_alertFlashColor, 0.7f);
    }

    // ---------- Marcador de la posición conocida y línea de detección ----------

    private void UpdateKnownPositionMarker()
    {
        bool show = _wolf.HasKnownPosition;

        _marker.gameObject.SetActive(show);
        _markerLine.enabled = show;

        if (!show) return;

        Vector3 known = _wolf.LastKnownPosition;
        Color color = ColorFor(_wolf.CurrentState);

        _marker.position = new Vector3(known.x, 0.35f, known.z);

        _markerLine.startColor = color;
        _markerLine.endColor = color;
        _markerLine.SetPosition(0, new Vector3(transform.position.x, 0.3f, transform.position.z));
        _markerLine.SetPosition(1, new Vector3(known.x, 0.3f, known.z));
    }

    private void UpdateDetectionLine()
    {
        FieldOfViewSensor sensor = _wolf.Sensor;
        bool show = sensor.CanSeeTarget && sensor.Target != null;

        _detectLine.enabled = show;

        if (!show) return;

        _detectLine.startColor = _chaseColor;
        _detectLine.endColor = _chaseColor;
        _detectLine.SetPosition(0, new Vector3(transform.position.x, 0.45f, transform.position.z));
        _detectLine.SetPosition(1, new Vector3(sensor.Target.position.x, 0.45f, sensor.Target.position.z));
    }

    private Color ColorFor(WolfAgent.WolfState state)
    {
        return state switch
        {
            WolfAgent.WolfState.Chase => _chaseColor,
            WolfAgent.WolfState.GoToPosition => _goToPositionColor,
            WolfAgent.WolfState.Search => _searchColor,
            _ => _patrolColor,
        };
    }

    private static string StateLabel(WolfAgent.WolfState state)
    {
        return state switch
        {
            WolfAgent.WolfState.Chase => "PERSIGUIENDO",
            WolfAgent.WolfState.GoToPosition => "Yendo a la posición conocida",
            WolfAgent.WolfState.Search => "Buscando",
            _ => "Patrullando",
        };
    }

    // ---------- Creación de objetos de feedback ----------

    private void CreateMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "PosicionConocida_" + name;
        marker.transform.localScale = Vector3.one * 0.8f;
        marker.GetComponent<Renderer>().sharedMaterial = FeedbackMaterials.CreateUnlit(new Color(1f, 0.55f, 0.1f, 0.9f));

        // Sólo es visual: sin collider.
        Destroy(marker.GetComponent<Collider>());

        marker.SetActive(false);
        _marker = marker.transform;
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
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;

        return line;
    }

    // ---------- Etiqueta sobre el lobo ----------

    private void OnGUI()
    {
        if (!_showLabel) return;

        Camera cam = Camera.main;

        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);

        if (screen.z <= 0f) return;

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 11;
            _labelStyle.alignment = TextAnchor.LowerCenter;
            _labelStyle.wordWrap = false;
        }

        string text = name + "\n" + _wolf.StateFeedbackLabel;

        if (_navigator.UsingPathfinding) text += "\nA*: " + _navigator.PathPoints.Count + " nodos";
        if (_wolf.HasKnownPosition) text += "\nPos. conocida: " + WolfAgent.Format(_wolf.LastKnownPosition);

        Rect rect = new(screen.x - 100f, Screen.height - screen.y - 70f, 200f, 70f);

        // Sombra negra para que se lea sobre cualquier fondo.
        _labelStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _labelStyle);

        _labelStyle.normal.textColor = Color.white;
        GUI.Label(rect, text, _labelStyle);
    }
}
