using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// NPC Cazador del Parcial 2.
//
// Toda su conducta se organiza con una FSM (FiniteStateMachine / State):
//   Search -> Attack -> Gather -> Flee -> Exit -> Dead.
// Las transiciones las decide cada estado a partir de sus propias condiciones; no hay un controlador externo
// que le diga qué estado ejecutar. La prioridad es: Dead > Flee > Exit > Gather > Attack > Search.
//
// Navegación: A* sobre el grafo compartido (GraphNavigator). Sólo el tramo final hacia un Boid que está en
// línea de visión directa usa steering (Pursuit / Seek / Arrive), como pide la consigna.
public class HunterAgent : Agent
{
    public enum HunterState { Search, Attack, Gather, Flee, Exit, Dead }

    public static HunterAgent Current { get; private set; }

    [Header("Movimiento")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float _moveSpeed = 5.2f;
    [SerializeField] private float _fleeSpeed = 6.2f;
    [SerializeField] private float _turnSpeed = 720f;

    [Header("Percepción (Boids y lobos)")]
    [Tooltip("Radio en el que el Cazador percibe Boids vivos, Boids eliminados y lobos (con línea de visión).")]
    [FormerlySerializedAs("perceptionRadius")]
    [SerializeField] private float _visionRadius = 14f;
    [SerializeField] private float _eyeHeight = 0.6f;

    [Header("Variables obligatorias del Cazador")]
    [Tooltip("Cantidad de recolecciones necesarias para ir a la Salida.")]
    [field: SerializeField] public int RequiredGathers { get; private set; } = 3;
    [Tooltip("Segundos que tarda en reaparecer en la Zona inicial después de ser alcanzado por un lobo.")]
    [field: SerializeField] public float RespawnTime { get; private set; } = 5f;
    [Tooltip("Segundos que espera dentro de una Zona segura antes de retomar su actividad.")]
    [field: SerializeField] public float SafeZoneWaitTime { get; private set; } = 4f;

    [Header("Ataque")]
    [field: SerializeField] public float TBA { get; private set; } = 1.5f;
    [field: SerializeField] public float MeleeAttackRadius { get; private set; } = 2f;
    [field: SerializeField] public float RangeAttackRadius { get; private set; } = 7f;
    [SerializeField] private float _meleeDamage = 100f;
    [SerializeField] private float _rangedDamage = 50f;

    [Header("Recolección")]
    [FormerlySerializedAs("gatherInteractionRadius")]
    [SerializeField] private float _gatherInteractionRadius = 1.5f;
    [FormerlySerializedAs("gatherDuration")]
    [SerializeField] private float _gatherDuration = 2f;
    [FormerlySerializedAs("boidRespawnDelay")]
    [SerializeField] private float _boidRespawnDelay = 5f;

    [Header("Objetos de interés")]
    [Tooltip("Prefab opcional. Si está vacío se crea uno por código dentro del Coto.")]
    [FormerlySerializedAs("interestPointPrefab")]
    [SerializeField] private InterestPoint _interestPointPrefab;
    [FormerlySerializedAs("poiSpawnInterval")]
    [SerializeField] private float _interestPointInterval = 6f;
    [FormerlySerializedAs("maxActiveInterestPoints")]
    [SerializeField] private int _maxActiveInterestPoints = 5;
    [FormerlySerializedAs("interestPointPlaceDuration")]
    [SerializeField] private float _legacyInterestPointPlaceDuration = 2f;

    [Header("Compatibilidad con el Parcial 1")]
    [Tooltip("Waypoints del Hunter del Parcial 1. Sólo se usan si la escena no configura las zonas del Parcial 2.")]
    [FormerlySerializedAs("waypoints")]
    [SerializeField] private List<Transform> _legacyWaypoints = new();
    [FormerlySerializedAs("waypointArrivalDistance")]
    [SerializeField] private float _legacyWaypointArrivalDistance = 0.4f;
    [FormerlySerializedAs("pingPongPatrol")]
    [SerializeField] private bool _legacyPingPongPatrol;

    [Header("Feedback visual")]
    [SerializeField] private Color _searchColor = Color.white;
    [SerializeField] private Color _attackColor = new(1f, 0.25f, 0.25f);
    [SerializeField] private Color _gatherColor = new(1f, 0.9f, 0.2f);
    [SerializeField] private Color _fleeColor = new(0.2f, 0.9f, 1f);
    [SerializeField] private Color _exitColor = new(0.3f, 1f, 0.4f);
    [SerializeField] private Color _deadColor = new(0.35f, 0.35f, 0.35f);

    // Buffer para no asignar un array nuevo en cada consulta de física.
    private static readonly Collider[] QueryBuffer = new Collider[128];

    private readonly FiniteStateMachine<HunterState> _fsm = new();
    private readonly List<Zone> _safeZones = new();
    private readonly List<Node> _huntingNodes = new();
    private readonly List<Renderer> _hiddenRenderers = new();
    private readonly List<Collider> _disabledColliders = new();

    private ColorFeedback _colorFeedback;
    private GraphNavigator _navigator;
    private FieldOfViewMultiOverlap _wolfSensor;
    private Graph _graph;

    private Zone _initialZone;
    private Zone _huntingZone;
    private Zone _exitZone;
    private Vector3 _startPosition;
    private Vector3 _exitPosition;

    private float _attackCooldown;
    private int _gathers;
    private bool _bodyVisible = true;
    private bool _legacyMode;

    private float _countTimer;
    private int _liveInPerception;
    private int _eliminatedInPerception;

    private HunterState _debugState;
    private string _debugTarget = "-";
    private string _debugAction = "-";
    private Zone _chosenSafeZone;

    public Zone InitialZone => _initialZone;
    public Zone HuntingZone => _huntingZone;
    public Zone ExitZone => _exitZone;
    public IReadOnlyList<Zone> SafeZones => _safeZones;
    public IReadOnlyList<Node> HuntingNodes => _huntingNodes;
    public Graph NavigationGraph => _graph;

    public float MoveSpeed => _moveSpeed;
    public float FleeSpeed => _fleeSpeed;
    public float VisionRadius => _visionRadius;
    public float EyeHeight => _eyeHeight;
    public float GatherInteractionRadius => _gatherInteractionRadius;
    public float GatherDuration => _gatherDuration;
    public float BoidRespawnDelay => _boidRespawnDelay;
    public float InterestPointInterval => _interestPointInterval;
    public int MaxActiveInterestPoints => _maxActiveInterestPoints;
    public float InterestPointPlaceDuration => _legacyInterestPointPlaceDuration;
    public IReadOnlyList<Transform> LegacyWaypoints => _legacyWaypoints;
    public float WaypointArrivalDistance => _legacyWaypointArrivalDistance;
    public bool PingPongPatrol => _legacyPingPongPatrol;
    public bool IsLegacyMode => _legacyMode;

    public int Gathers => _gathers;
    public bool IsDead => _fsm.CurrentStateID == HunterState.Dead;
    public bool IsAttackReady => _attackCooldown <= 0f;
    public HunterState CurrentState => _fsm.CurrentStateID;
    public Zone ChosenSafeZone => _chosenSafeZone;
    public bool IsInSafeZone => Zone.IsInsideSafeZone(transform.position);

    public int LiveBoidsInPerception => _liveInPerception;
    public int EliminatedBoidsInPerception => _eliminatedInPerception;
    public IReadOnlyList<Transform> VisibleWolves => _wolfSensor != null ? _wolfSensor.VisibleTargets : Array.Empty<Transform>();
    public int VisibleWolfCount => _wolfSensor != null ? _wolfSensor.VisibleTargets.Count : 0;

    // Dispara Flee: el Cazador ve por lo menos un lobo (distancia + línea de visión; sin ángulo).
    public bool IsThreatened => !IsDead && _wolfSensor != null && _wolfSensor.CanSeeTarget;

    // Mientras ya está dentro de una Zona segura no necesita volver a huir: los lobos no pueden detectarlo.
    public bool ShouldFlee => IsThreatened && !IsInSafeZone;

    public string DebugTarget => _debugTarget;
    public string DebugAction => _debugAction;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Current = null;

    public void Configure(
        Vector3 startPosition,
        Zone initialZone,
        Zone huntingZone,
        IReadOnlyList<Zone> safeZones,
        Zone exitZone,
        Vector3 exitPosition)
    {
        _startPosition = new Vector3(startPosition.x, transform.position.y, startPosition.z);
        _initialZone = initialZone;
        _huntingZone = huntingZone;
        _exitZone = exitZone;
        _exitPosition = new Vector3(exitPosition.x, transform.position.y, exitPosition.z);

        _safeZones.Clear();
        for (int i = 0; i < safeZones.Count; i++)
        {
            if (safeZones[i] != null) _safeZones.Add(safeZones[i]);
        }
    }

    private void Awake()
    {
        Current = this;

        DisablePhysicsInterference();

        // Los lobos lo perciben con Physics.OverlapSphere (layer Hunter): necesita un collider.
        if (GetComponentInChildren<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.55f;
        }

        GameLayers.SetLayer(gameObject, GameLayers.Hunter);

        _colorFeedback = gameObject.GetOrAddComponent<ColorFeedback>();
        _colorFeedback.SetRenderer(GetComponentInChildren<Renderer>());

        _navigator = gameObject.GetOrAddComponent<GraphNavigator>();
        _wolfSensor = gameObject.GetOrAddComponent<FieldOfViewMultiOverlap>();
        gameObject.GetOrAddComponent<PathVisual>();
    }

    private void Start()
    {
        _graph = Graph.Current;

        if (_startPosition == Vector3.zero) _startPosition = transform.position;
        _debugState = HunterState.Search;

        ConfigureWolfSensor();
        BuildHuntingNodeList();

        // La escena del Parcial 1 tiene un Hunter con waypoints, pero no las zonas ni el grafo del
        // Parcial 2. En ese caso se usa un estado de patrulla compatible para no romper el P1.
        _legacyMode = _huntingZone == null || _initialZone == null || _exitZone == null;

        if (_legacyMode)
        {
            _fsm.AddState(HunterState.Search, new HunterLegacyPatrolState(this));
            _fsm.AddState(HunterState.Attack, new HunterAttackState(this));
            _fsm.AddState(HunterState.Gather, new HunterGatherState(this));
            _fsm.AddState(HunterState.Dead, new HunterDeadState(this));

            _fsm.OnStateChanged += _ => _navigator.ResetPath();
            _fsm.ChangeState(HunterState.Search);

            FeedbackLog.Add("[Cazador] Modo Parcial 1: patrulla por waypoints.", new Color(0.4f, 1f, 0.5f), false);
            return;
        }

        _fsm.AddState(HunterState.Search, new HunterSearchState(this));
        _fsm.AddState(HunterState.Attack, new HunterAttackState(this));
        _fsm.AddState(HunterState.Gather, new HunterGatherState(this));
        _fsm.AddState(HunterState.Flee, new HunterFleeState(this));
        _fsm.AddState(HunterState.Exit, new HunterExitState(this));
        _fsm.AddState(HunterState.Dead, new HunterDeadState(this));

        // Al cambiar de estado se descarta el camino que se venía siguiendo.
        _fsm.OnStateChanged += _ => _navigator.ResetPath();

        _fsm.ChangeState(HunterState.Search);

        FeedbackLog.Add("[Cazador] Inicia en la Zona inicial. Debe recolectar " + RequiredGathers + " Boids y llegar a la Salida.", new Color(0.4f, 1f, 0.5f));
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void ConfigureWolfSensor()
    {
        // 360°: la consigna del Cazador sólo exige distancia máxima y obstáculos que corten la línea de visión.
        _wolfSensor.Configure(_visionRadius, 360f, _eyeHeight, GameLayers.WolfMask, GameLayers.WallMask);
    }

    private void BuildHuntingNodeList()
    {
        _huntingNodes.Clear();

        if (_graph == null || _huntingZone == null) return;

        foreach (Node node in _graph.GetAllNodes)
        {
            if (!node.IsSafeZone && _huntingZone.Contains(node.transform.position))
                _huntingNodes.Add(node);
        }
    }

    private void Update()
    {
        if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;

        UpdatePerceptionCounters();
        _fsm.Update();
    }

    // ---------- Movimiento ----------

    // Movimiento normal: A* si no hay línea recta libre (GraphNavigator decide).
    public void MoveTo(Vector3 destination, float speed)
    {
        Vector3 next = _navigator.GetNextPoint(destination);
        MoveDirect(next, speed);
    }

    // Steering del Parcial 1: sólo para el tramo final con línea de visión directa.
    public void MoveDirect(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            StopMoving();
            return;
        }

        direction.Normalize();
        _velocity = direction * speed;
        transform.position += _velocity * Time.deltaTime;

        Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, _turnSpeed * Time.deltaTime);
    }

    // Pursuit con predicción de la posición futura del Boid (sólo si hay línea de visión directa).
    public void PursueBoid(BoidAgent target, float speed)
    {
        if (target == null) return;

        if (_graph != null && !_graph.IsPathClear(transform.position, target.transform.position))
        {
            MoveTo(target.transform.position, speed);
            return;
        }

        float distance = FlatDistanceTo(target.transform.position);
        float targetSpeed = target.Velocity.magnitude;
        float prediction = distance / Mathf.Max(speed + targetSpeed, 0.001f);
        Vector3 futurePosition = target.transform.position + target.Velocity * prediction;

        MoveDirect(futurePosition, speed);
    }

    public void StopMoving() => _velocity = Vector3.zero;

    // Movimiento directo del Parcial 1 (sin grafo), usado por la patrulla de compatibilidad.
    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            StopMoving();
            return;
        }

        direction.Normalize();
        _velocity = direction * _moveSpeed;
        transform.position += _velocity * Time.deltaTime;
        transform.forward = direction;
    }

    public float FlatDistanceTo(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    public bool HasReached(Vector3 position, float arrivalDistance)
    {
        return FlatDistanceTo(position) <= arrivalDistance;
    }

    // ---------- Percepción de Boids ----------

    public BoidAgent FindNearestLiveBoid() => FindBestBoid(boid => !boid.IsEliminated, false);

    public BoidAgent FindNearestEliminatedBoid() => FindBestBoid(boid => boid.IsEliminated && boid.IsPendingGather, false);

    // Elige al Boid más herido dentro de la percepción; si hay empate, el más cercano.
    public BoidAgent FindBestLiveBoid() => FindBestBoid(boid => !boid.IsEliminated, true);

    private BoidAgent FindBestBoid(Func<BoidAgent, bool> matchesGiven, bool chooseWeakest)
    {
        BoidAgent best = null;
        float bestScore = float.PositiveInfinity;
        float bestDistance = float.PositiveInfinity;

        int count = Physics.OverlapSphereNonAlloc(transform.position, _visionRadius, QueryBuffer);

        for (int i = 0; i < count; i++)
        {
            BoidAgent boid = QueryBuffer[i].GetComponentInParent<BoidAgent>();

            if (boid == null || !matchesGiven(boid)) continue;

            float distance = FlatDistanceTo(boid.transform.position);
            if (distance > _visionRadius || !HasClearSightTo(boid.transform.position)) continue;

            float score = chooseWeakest ? boid.HealthNormalized : distance;

            if (score < bestScore - 0.001f || (Mathf.Abs(score - bestScore) <= 0.001f && distance < bestDistance))
            {
                best = boid;
                bestScore = score;
                bestDistance = distance;
            }
        }

        return best;
    }

    public bool HasClearSightTo(Vector3 targetPosition)
    {
        float height = transform.position.y + _eyeHeight;
        Vector3 eye = new(transform.position.x, height, transform.position.z);
        Vector3 targetEye = new(targetPosition.x, height, targetPosition.z);

        return !Physics.Linecast(eye, targetEye, GameLayers.WallMask);
    }

    public Node GetRandomHuntingNode()
    {
        if (_huntingNodes.Count == 0) return null;
        return _huntingNodes[UnityEngine.Random.Range(0, _huntingNodes.Count)];
    }

    private void UpdatePerceptionCounters()
    {
        _countTimer -= Time.deltaTime;

        if (_countTimer > 0f) return;

        _countTimer = 0.2f;
        _liveInPerception = CountBoids(boid => !boid.IsEliminated);
        _eliminatedInPerception = CountBoids(boid => boid.IsEliminated && boid.IsPendingGather);
    }

    private int CountBoids(Func<BoidAgent, bool> matchesGiven)
    {
        int total = 0;
        int count = Physics.OverlapSphereNonAlloc(transform.position, _visionRadius, QueryBuffer);

        for (int i = 0; i < count; i++)
        {
            BoidAgent boid = QueryBuffer[i].GetComponentInParent<BoidAgent>();

            if (boid != null && matchesGiven(boid) && FlatDistanceTo(boid.transform.position) <= _visionRadius)
                total++;
        }

        return total;
    }

    // ---------- Ataque ----------

    public void ResetAttackTimer()
    {
        _attackCooldown = TBA;
    }

    public void PerformMeleeAttack(BoidAgent target)
    {
        _debugAction = "Ataque CUERPO A CUERPO a " + target.name;
        target.TakeDamage(_meleeDamage);
        FeedbackLog.Add("[Cazador] " + _debugAction, _attackColor);
        ResetAttackTimer();
    }

    public void PerformRangedAttack(BoidAgent target)
    {
        _debugAction = "Ataque A DISTANCIA a " + target.name;
        target.TakeDamage(_rangedDamage);
        FeedbackLog.Add("[Cazador] " + _debugAction, _attackColor);
        ResetAttackTimer();
    }

    // ---------- Recolección ----------

    public void AddGather()
    {
        _gathers++;
        FeedbackLog.Add("[Cazador] Recolección completada (" + _gathers + "/" + RequiredGathers + ").", new Color(0.4f, 1f, 0.4f));
    }

    public void ResetGathers() => _gathers = 0;

    // ---------- Objetos de interés ----------

    public bool CanSpawnInterestPoint()
    {
        if (_huntingZone == null || !_huntingZone.Contains(transform.position)) return false;

        InterestPoint[] active = FindObjectsByType<InterestPoint>(FindObjectsSortMode.None);
        return active.Length < _maxActiveInterestPoints;
    }

    public void SpawnInterestPoint()
    {
        if (!CanSpawnInterestPoint()) return;

        Vector3 position = _huntingZone.RandomPoint();
        position.y = 0.35f;

        InterestPoint point;

        if (_interestPointPrefab != null)
        {
            point = Instantiate(_interestPointPrefab, position, Quaternion.identity);
        }
        else
        {
            GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pointObject.name = "ObjetoDeInteres";

            Destroy(pointObject.GetComponent<Collider>());
            pointObject.AddComponent<SphereCollider>().isTrigger = true;
            pointObject.GetComponent<SphereCollider>().radius = 0.5f;
            pointObject.transform.localScale = new Vector3(0.45f, 0.2f, 0.45f);

            Renderer renderer = pointObject.GetComponent<Renderer>();
            renderer.sharedMaterial = FeedbackMaterials.CreateUnlit(new Color(1f, 0.85f, 0.2f));

            point = pointObject.AddComponent<InterestPoint>();
            point.Initialize(renderer);
        }

        FeedbackLog.Add("[Cazador] Genera un objeto de interés en " + WolfAgent.Format(position) + " (máx. " + _maxActiveInterestPoints + ").", new Color(1f, 0.85f, 0.2f), false);
    }

    // Compatibilidad con el Hunter del Parcial 1: coloca el objeto donde está parado el Cazador.
    public bool HasRoomForInterestPoint()
    {
        if (_interestPointPrefab == null) return false;
        return FindObjectsByType<InterestPoint>(FindObjectsSortMode.None).Length < _maxActiveInterestPoints;
    }

    public void SpawnInterestPointAtCurrentPosition()
    {
        if (!HasRoomForInterestPoint()) return;

        Vector3 position = new(transform.position.x, 0.35f, transform.position.z);
        Instantiate(_interestPointPrefab, position, Quaternion.identity);
        FeedbackLog.Add("[Cazador] Coloca un objeto de interés en " + WolfAgent.Format(position) + ".", new Color(1f, 0.85f, 0.2f), false);
    }

    // ---------- Zonas seguras ----------

    // Elige la Zona segura más cercana según la longitud del camino A* (no según la distancia en línea recta).
    public Zone ChooseNearestSafeZone()
    {
        Zone best = null;
        float bestCost = float.PositiveInfinity;

        foreach (Zone zone in _safeZones)
        {
            if (zone == null) continue;

            float cost = _graph != null
                ? _graph.GetPathCost(transform.position, zone.Center, false)
                : FlatDistanceTo(zone.Center);

            if (cost < bestCost)
            {
                bestCost = cost;
                best = zone;
            }
        }

        _chosenSafeZone = best;
        return best;
    }

    // ---------- Vida, muerte, respawn y ciclos ----------

    public void DieFromWolf()
    {
        if (IsDead) return;

        FeedbackLog.Add("[Cazador] ¡Alcanzado por un lobo! Pasa a DEAD.", new Color(1f, 0.2f, 0.2f));
        _fsm.ChangeState(HunterState.Dead);
    }

    public void SetBodyVisible(bool visible)
    {
        if (_bodyVisible == visible) return;

        _bodyVisible = visible;

        if (!visible)
        {
            _hiddenRenderers.Clear();
            _disabledColliders.Clear();

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                renderer.enabled = false;
                _hiddenRenderers.Add(renderer);
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled) continue;
                collider.enabled = false;
                _disabledColliders.Add(collider);
            }

            return;
        }

        foreach (Renderer renderer in _hiddenRenderers)
        {
            if (renderer != null) renderer.enabled = true;
        }

        foreach (Collider collider in _disabledColliders)
        {
            if (collider != null) collider.enabled = true;
        }

        _hiddenRenderers.Clear();
        _disabledColliders.Clear();
    }

    public void RespawnAtInitialZone()
    {
        transform.position = _startPosition;
        transform.rotation = Quaternion.identity;
        ResetGathers();
        _attackCooldown = 0f;
        _navigator.ResetPath();
        SetBodyVisible(true);
        _debugTarget = "-";
        _debugAction = "Reapareció en la Zona inicial";
    }

    public void FinishExitCycle()
    {
        transform.position = _startPosition;
        transform.rotation = Quaternion.identity;
        ResetGathers();
        _attackCooldown = 0f;
        _navigator.ResetPath();
        SetBodyVisible(true);
        _debugTarget = "-";
        _debugAction = "Completó el objetivo: vuelve a la Zona inicial";
    }

    // ---------- Feedback ----------

    public void SetFeedback(HunterState state, BoidAgent target = null, string action = null)
    {
        _debugState = state;
        _debugTarget = target != null ? target.name : "-";

        if (action != null) _debugAction = action;
        else if (target == null) _debugAction = "-";

        Color color = state switch
        {
            HunterState.Attack => _attackColor,
            HunterState.Gather => _gatherColor,
            HunterState.Flee => _fleeColor,
            HunterState.Exit => _exitColor,
            HunterState.Dead => _deadColor,
            _ => _searchColor,
        };

        if (_colorFeedback != null) _colorFeedback.SetColor(color);
    }

    public Color ColorFor(HunterState state)
    {
        return state switch
        {
            HunterState.Attack => _attackColor,
            HunterState.Gather => _gatherColor,
            HunterState.Flee => _fleeColor,
            HunterState.Exit => _exitColor,
            HunterState.Dead => _deadColor,
            _ => _searchColor,
        };
    }
}
