using System;
using System.Collections.Generic;
using UnityEngine;

// Lobo del Parcial 2.
//
// - Patrulla por waypoints (nodos del grafo) usando A* cuando no hay línea de visión.
// - Percibe al Cazador con Field Of View (distancia + ángulo + línea de visión, sin atravesar paredes).
// - Al detectarlo aúlla: avisa a los demás lobos con su última posición conocida (no se actualiza sola).
// - Lo persigue y, si llega a CaptureRadius, lo captura: el Cazador pasa al estado Dead.
// - Si el Cazador entra a una Zona segura, los lobos descartan el objetivo y vuelven a patrullar.
// - Nunca navega usando los nodos de las Zonas seguras (GraphNavigator con avoidSafeZones = true).
//
// La conducta se organiza con la FSM del Parcial 1: WolfPatrolState, WolfChaseState,
// WolfGoToPositionState y WolfSearchState. Cada estado decide sus propias transiciones.
[RequireComponent(typeof(FieldOfViewOverlap))]
[RequireComponent(typeof(GraphNavigator))]
[RequireComponent(typeof(ColorFeedback))]
[RequireComponent(typeof(Collider))]
public class WolfAgent : Agent
{
    public enum WolfState { Patrol, Chase, GoToPosition, Search }

    // Eventos que usa el feedback (WolfFeedback). No toman decisiones de IA.
    public event Action<WolfState> OnStateChanged = delegate { };
    public event Action<Vector3> OnHowled = delegate { };
    public event Action<WolfAgent, Vector3> OnAlertReceived = delegate { };

    // Alerta compartida. El lobo que aúlla dispara el evento con la posición donde vio al Cazador;
    // los demás se suscriben en OnEnable. Es el mismo patrón de evento estático visto en clase.
    public static event Action<WolfAgent, Vector3> AlertIssued = delegate { };

    [Header("Velocidades configurables (siempre menores que el Cazador)")]
    [SerializeField] private float _patrolSpeed = 2.6f;
    [SerializeField] private float _alertSpeed = 3.4f;
    [SerializeField] private float _chaseSpeed = 4.2f;
    [SerializeField] private float _maxSteering = 20f;

    [Header("Percepción (FoV)")]
    [SerializeField] private float _viewRadius = 18f;
    [SerializeField, Range(0f, 360f)] private float _viewAngle = 110f;
    [SerializeField] private float _eyeHeight = 0.6f;

    [Header("Captura")]
    [SerializeField] private float _captureRadius = 1.3f;

    [Header("Alerta / aullido")]
    [SerializeField] private float _howlCooldown = 6f;

    [Header("Búsqueda")]
    [Tooltip("Intervalo para renovar los nodos de búsqueda. Sigue buscando hasta detectar, recibir una alerta o perder el objetivo por refugio/muerte.")]
    [SerializeField] private float _searchDuration = 6f;
    [SerializeField] private float _scanDuration = 1f;
    [SerializeField] private float _searchRadius = 14f;

    private readonly FiniteStateMachine<WolfState> _fsm = new();
    private readonly List<Node> _patrolNodes = new();

    private FieldOfViewOverlap _sensor;
    private GraphNavigator _navigator;
    private ColorFeedback _colorFeedback;
    private WolfFeedback _feedback;
    private Graph _graph;

    private bool _pingPong = true;
    private bool _configured;
    private bool _hasKnownPosition;
    private bool _hasPendingAlert;
    private Vector3 _lastKnownPosition;
    private Vector3 _pendingAlertPosition;
    private float _nextHowlTime;
    private bool _loggedNoRoute;

    public WolfState CurrentState => _fsm.CurrentStateID;

    public IReadOnlyList<Node> PatrolNodes => _patrolNodes;
    public bool PingPong => _pingPong;

    public float PatrolSpeed => _patrolSpeed;
    public float AlertSpeed => _alertSpeed;
    public float ChaseSpeed => _chaseSpeed;
    public float CaptureRadius => _captureRadius;
    public float ViewRadius => _viewRadius;
    public float SearchDurationForState => _searchDuration;
    public float ScanDuration => _scanDuration;
    public float SearchRadius => _searchRadius;

    public string StateFeedbackLabel { get; private set; } = "Patrullando";

    public FieldOfViewOverlap Sensor => _sensor;
    public GraphNavigator Navigator => _navigator;
    public Graph Graph => _graph;

    public bool HasKnownPosition => _hasKnownPosition;
    public bool HasPendingAlert => _hasPendingAlert;
    public Vector3 LastKnownPosition => _lastKnownPosition;
    public bool CanSeeHunter => _sensor != null && _sensor.CanSeeTarget && Hunter != null
        && Hunter.IsPresent && !Hunter.IsInSafeZone && _sensor.IsVisibleNow(Hunter.transform);
    public HunterAgent Hunter => HunterAgent.Current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        AlertIssued = delegate { };
    }

    private void Awake()
    {
        DisablePhysicsInterference();

        if (GetComponentInChildren<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }

        GameLayers.SetLayer(gameObject, GameLayers.Wolf);

        _colorFeedback = gameObject.GetOrAddComponent<ColorFeedback>();
        _colorFeedback.SetRenderer(GetComponentInChildren<Renderer>());

        _navigator = gameObject.GetOrAddComponent<GraphNavigator>();
        _navigator.Configure(true); // los lobos no usan nodos de Zonas seguras

        _sensor = gameObject.GetOrAddComponent<FieldOfViewOverlap>();

        // El sensor ignora al Cazador muerto o dentro de una Zona segura.
        _sensor.TargetFilter = candidate =>
        {
            HunterAgent hunter = candidate != null ? candidate.GetComponent<HunterAgent>() : null;
            return hunter != null && hunter.IsPresent && !hunter.IsInSafeZone;
        };

        gameObject.GetOrAddComponent<FieldOfViewGizmos>();
        gameObject.GetOrAddComponent<PathVisual>();
        _feedback = gameObject.GetOrAddComponent<WolfFeedback>();
    }

    private void OnEnable()
    {
        AlertIssued += HandleAlertIssued;
    }

    private void OnDisable()
    {
        AlertIssued -= HandleAlertIssued;
    }

    private void Start()
    {
        _graph = Graph.Current;

        _sensor.Configure(_viewRadius, _viewAngle, _eyeHeight, GameLayers.HunterMask, GameLayers.WallMask);
        _navigator.Configure(true);

        _fsm.AddState(WolfState.Patrol, new WolfPatrolState(this));
        _fsm.AddState(WolfState.Chase, new WolfChaseState(this));
        _fsm.AddState(WolfState.GoToPosition, new WolfGoToPositionState(this));
        _fsm.AddState(WolfState.Search, new WolfSearchState(this));

        _fsm.OnStateChanged += HandleStateChanged;
        _fsm.ChangeState(WolfState.Patrol);
    }

    private void Update()
    {
        if (!_configured)
        {
            if (!_loggedNoRoute)
            {
                _loggedNoRoute = true;
                FeedbackLog.Add(name + " no tiene ruta de patrulla configurada.", Color.red);
            }

            return;
        }

        _fsm.Update();
    }

    private void HandleStateChanged(WolfState state)
    {
        OnStateChanged(state);
    }

    // Lo llama el LevelBuilder: ruta de patrulla y referencia al grafo compartido.
    public void Configure(Graph graph, IReadOnlyList<Node> patrolNodes, bool pingPong)
    {
        _graph = graph;
        _pingPong = pingPong;

        _patrolNodes.Clear();

        for (int i = 0; i < patrolNodes.Count; i++)
        {
            if (patrolNodes[i] != null) _patrolNodes.Add(patrolNodes[i]);
        }

        _configured = _patrolNodes.Count > 0;
    }

    // ---------- Movimiento ----------

    // Se mueve hacia 'destination'. GraphNavigator usa A* si no hay línea recta libre.
    public void MoveTo(Vector3 destination, float speed)
    {
        if (_navigator.IsDestinationForbidden(destination))
        {
            StopMoving();
            return;
        }

        Vector3 next = _navigator.GetNextPoint(destination);
        MoveDirect(next, speed);
    }

    public void MoveDirect(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            StopMoving();
            return;
        }

        MoveWithSteering(destination, speed, _maxSteering, 0.6f, true);
        if (_velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
    }

    public void StopMoving()
    {
        _velocity = Vector3.zero;
    }

    public bool HasReached(Vector3 position, float arrivalDistance)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude <= arrivalDistance;
    }

    public void SetStateFeedbackLabel(string label)
    {
        StateFeedbackLabel = label;
    }

    public float FlatDistanceTo(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    public float FlatDistanceToHunter()
    {
        return Hunter != null ? FlatDistanceTo(Hunter.transform.position) : float.PositiveInfinity;
    }

    // Gira en el lugar para "mirar alrededor" durante Search.
    public void Scan(float degreesPerSecond)
    {
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
    }

    // ---------- Detección, aullido y alerta ----------

    // Registra la posición conocida y, si pasó el cooldown, aúlla para avisar a los demás.
    public void NotifyTargetSpotted()
    {
        HunterAgent hunter = Hunter;

        if (hunter == null || !CanSeeHunter)
        {
            ForgetHunter();
            return;
        }

        _lastKnownPosition = hunter.transform.position;
        _hasKnownPosition = true;
        _hasPendingAlert = false; // una observación propia reemplaza alertas anteriores

        if (Time.time < _nextHowlTime) return;

        _nextHowlTime = Time.time + _howlCooldown;

        OnHowled(transform.position);
        AlertIssued(this, _lastKnownPosition);

        FeedbackLog.Add(name + " aúlla y avisa: Cazador en " + Format(_lastKnownPosition) + ".", new Color(1f, 0.6f, 0.1f));
    }

    private void HandleAlertIssued(WolfAgent source, Vector3 position)
    {
        if (source == this) return;
        if (Hunter == null || Hunter.IsDead || Hunter.IsInSafeZone) return;

        _pendingAlertPosition = position;
        _hasPendingAlert = true;

        OnAlertReceived(source, position);
        FeedbackLog.Add(name + " recibe la alerta de " + source.name + ": posición " + Format(position) + ".", new Color(0.3f, 0.9f, 1f), false);
    }

    // Un estado en GoToPosition consume la alerta una sola vez.
    public void ConsumePendingAlert()
    {
        if (!_hasPendingAlert) return;

        _lastKnownPosition = _pendingAlertPosition;
        _hasKnownPosition = true;
        _hasPendingAlert = false;
    }

    public void ForgetHunter()
    {
        _hasKnownPosition = false;
        _hasPendingAlert = false;
        _lastKnownPosition = Vector3.zero;
        _pendingAlertPosition = Vector3.zero;

        if (_navigator != null) _navigator.ResetPath();
    }

    // Si el Cazador está dentro de una Zona segura, los lobos no pueden entrar ni seguir usándolo como objetivo.
    public bool HunterInsideSafeZone => Hunter != null && Hunter.IsInSafeZone;
    public bool HunterIsDead => Hunter == null || Hunter.IsDead;

    // ---------- Captura ----------

    public bool TryCaptureHunter()
    {
        HunterAgent hunter = Hunter;

        if (hunter == null || !hunter.IsPresent || hunter.IsInSafeZone) return false;
        if (FlatDistanceToHunter() > _captureRadius) return false;
        if (_graph != null && !_graph.IsPathClear(transform.position, hunter.transform.position)) return false;

        FeedbackLog.Add(name + " alcanzó al Cazador dentro de CaptureRadius (" + _captureRadius.ToString("0.0") + " m).", new Color(1f, 0.15f, 0.15f));

        hunter.DieFromWolf();
        ForgetHunter();
        return true;
    }

    // ---------- Utilidades ----------

    public static string Format(Vector3 position)
    {
        return "(" + position.x.ToString("0.0") + ", " + position.z.ToString("0.0") + ")";
    }

    private void OnDrawGizmos()
    {
        // Radio de captura del lobo, siempre visible.
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, _captureRadius);

        if (_hasKnownPosition)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, _lastKnownPosition + Vector3.up * 0.2f);
            Gizmos.DrawWireSphere(_lastKnownPosition, 0.45f);
        }
    }
}
