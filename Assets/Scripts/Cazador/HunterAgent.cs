using System;
using System.Collections.Generic;
using UnityEngine;

public class HunterAgent : Agent
{
    public enum HunterState { Patrol, Attack, Gather }

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Header("Patrulla")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float waypointArrivalDistance = 0.4f;
    [SerializeField] private bool pingPongPatrol = false;

    [Header("Objetos de interés")]
    [SerializeField] private InterestPoint interestPointPrefab;
    [SerializeField] private float poiSpawnInterval = 4f;
    [SerializeField] private int maxActiveInterestPoints = 5;
    [SerializeField] private float interestPointPlaceDuration = 2f;

    [Header("Percepción")]
    [SerializeField] private float perceptionRadius = 8f;
    [SerializeField] private float perceptionLossMargin = 2f;

    [Header("Variables obligatorias del Cazador")]
    [field: SerializeField] public float TBA { get; private set; } = 5f;
    [field: SerializeField] public float RangeAttackRadius { get; private set; } = 5f;
    [field: SerializeField] public float MeleeAttackRadius { get; private set; } = 1.5f;

    [Header("Daño")]
    [SerializeField] private float meleeDamage = 34f;
    [SerializeField] private float rangedDamage = 20f;

    [Header("Ataque")]
    [SerializeField] private float attackPreparationTime = 0.7f;
    public float AttackPreparationTime => attackPreparationTime;

    [Header("Recolección (Gather)")]
    [SerializeField] private float gatherInteractionRadius = 1.2f;
    [SerializeField] private float gatherDuration = 2f;
    [SerializeField] private float boidRespawnDelay = 5f;

    [Header("Feedback visual/funcional")]
    [SerializeField] private Renderer feedbackRenderer;
    [SerializeField] private Color patrolColor = Color.white;
    [SerializeField] private Color attackColor = Color.red;
    [SerializeField] private Color gatherColor = Color.yellow;
    [SerializeField] private bool showOnScreenDebug = true;

    public static readonly List<HunterAgent> All = new List<HunterAgent>();

    private readonly FiniteStateMachine<HunterState> _fsm = new();
    private float _attackCooldown;

    private HunterState _debugState;
    private string _debugTarget = "-";
    private string _debugAction = "-";

    public List<Transform> Waypoints => waypoints;
    public float WaypointArrivalDistance => waypointArrivalDistance;
    public bool PingPongPatrol => pingPongPatrol;
    public float PerceptionRadius => perceptionRadius;
    public float PerceptionLossRadius => perceptionRadius + perceptionLossMargin;
    public float PoiSpawnInterval => poiSpawnInterval;
    public float InterestPointPlaceDuration => interestPointPlaceDuration;
    public float GatherInteractionRadius => gatherInteractionRadius;
    public float GatherDuration => gatherDuration;
    public float BoidRespawnDelay => boidRespawnDelay;
    public bool IsAttackReady => _attackCooldown <= 0f;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void Awake()
    {
        DisablePhysicsInterference();
    }

    private void Start()
    {
        var patrol = new HunterPatrolState(this);
        var attack = new HunterAttackState(this);
        var gather = new HunterGatherState(this);

        _fsm.AddState(HunterState.Patrol, patrol);
        _fsm.AddState(HunterState.Attack, attack);
        _fsm.AddState(HunterState.Gather, gather);

        _fsm.ChangeState(HunterState.Patrol);
    }

    private void Update()
    {
        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;

        _fsm.Update();
    }

    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            StopMoving();
            return;
        }

        _velocity = direction.normalized * moveSpeed;
        transform.position += _velocity * Time.deltaTime;
        transform.forward = direction.normalized;
    }

    public void StopMoving() => _velocity = Vector3.zero;

    public BoidAgent FindNearestLiveBoid() => FindNearestBoid(b => !b.IsEliminated);

    public BoidAgent FindNearestEliminatedBoid() => FindNearestBoid(b => b.IsEliminated && b.IsPendingGather);

    private BoidAgent FindNearestBoid(Func<BoidAgent, bool> matches)
    {
        BoidAgent nearest = null;
        float nearestDist = perceptionRadius;

        foreach (var boid in BoidAgent.All)
        {
            if (!matches(boid))
                continue;

            float dist = Vector3.Distance(transform.position, boid.transform.position);

            if (dist <= nearestDist)
            {
                nearest = boid;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    public int CountLiveBoidsInPerception()
    {
        int count = 0;

        foreach (var boid in BoidAgent.All)
        {
            if (!boid.IsEliminated &&
                Vector3.Distance(transform.position, boid.transform.position) <= perceptionRadius)
            {
                count++;
            }
        }

        return count;
    }

    public int CountPendingGatherBoidsInPerception()
    {
        int count = 0;

        foreach (var boid in BoidAgent.All)
        {
            if (boid.IsEliminated &&
                boid.IsPendingGather &&
                Vector3.Distance(transform.position, boid.transform.position) <= perceptionRadius)
            {
                count++;
            }
        }

        return count;
    }

    public void ResetAttackTimer() => _attackCooldown = TBA;

    public void PerformMeleeAttack(BoidAgent target)
    {
        _debugAction = $"Ataque cuerpo a cuerpo a {target.name}";
        Debug.Log($"[Cazador] {_debugAction}");

        target.TakeDamage(meleeDamage);
        ResetAttackTimer();
    }

    public void PerformRangedAttack(BoidAgent target)
    {
        _debugAction = $"Ataque a distancia a {target.name}";
        Debug.Log($"[Cazador] {_debugAction}");

        target.TakeDamage(rangedDamage);
        ResetAttackTimer();
    }

    public bool HasRoomForInterestPoint()
    {
        if (interestPointPrefab == null)
            return false;

        return InterestPoint.ActivePoints.Count < maxActiveInterestPoints;
    }

    public void SpawnInterestPointAtCurrentPosition()
    {
        if (interestPointPrefab == null)
            return;

        if (!HasRoomForInterestPoint())
            return;

        Vector3 spawnPos = transform.position;

        Instantiate(interestPointPrefab, spawnPos, Quaternion.identity);

        Debug.Log($"[Cazador] Genera un objeto de interés en {spawnPos}.");
    }

    public void SetFeedback(HunterState state, BoidAgent target = null, string action = null)
    {
        _debugState = state;
        _debugTarget = target != null ? target.name : "-";

        if (action != null)
            _debugAction = action;
        else if (target == null)
            _debugAction = "-";

        if (feedbackRenderer == null)
            return;

        Color color = state switch
        {
            HunterState.Attack => attackColor,
            HunterState.Gather => gatherColor,
            _ => patrolColor
        };

        feedbackRenderer.material.color = color;
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug)
            return;

        int liveDetected = CountLiveBoidsInPerception();
        int pendingDetected = CountPendingGatherBoidsInPerception();

        GUI.Label(
            new Rect(10, 10, 360, 130),
            $"Cazador\nEstado: {_debugState}\nObjetivo: {_debugTarget}\nAgentes detectados: {liveDetected} vivos / {pendingDetected} caídos\nÚltima acción: {_debugAction}\nTBA listo: {IsAttackReady}"
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.color = new Color(0f, 0.6f, 0.6f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, PerceptionLossRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position, RangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MeleeAttackRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gatherInteractionRadius);
    }
}