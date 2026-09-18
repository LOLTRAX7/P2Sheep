using UnityEngine;

public class HunterPatrolState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;

    private int _currentIndex;
    private int _direction = 1;
    private float _spawnTimer;

    private bool _isPlacingInterestPoint;
    private float _placeTimer;

    public HunterPatrolState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        Debug.Log("[Cazador] Entra a Patrol.");
        _hunter.SetFeedback(HunterAgent.HunterState.Patrol);
    }

    public override void Update()
    {
        if (_hunter.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }

        if (_hunter.IsAttackReady && _hunter.FindNearestLiveBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Attack);
            return;
        }

        if (_isPlacingInterestPoint)
        {
            UpdatePlacingInterestPoint();
            return;
        }

        PatrolWaypoints();
        UpdateSpawnTimer();
    }

    public override void Exit()
    {
        Debug.Log("[Cazador] Sale de Patrol.");
        _hunter.StopMoving();
        _isPlacingInterestPoint = false;
    }

    private void PatrolWaypoints()
    {
        var waypoints = _hunter.Waypoints;

        if (waypoints == null || waypoints.Count == 0)
            return;

        if (_currentIndex >= waypoints.Count)
            _currentIndex = 0;

        Transform currentWaypoint = waypoints[_currentIndex];

        if (currentWaypoint == null)
            return;

        _hunter.MoveTowards(currentWaypoint.position);

        float dist = Vector3.Distance(
            _hunter.transform.position,
            currentWaypoint.position
        );

        if (dist <= _hunter.WaypointArrivalDistance)
        {
            AdvanceWaypoint(waypoints.Count);
        }
    }

    private void AdvanceWaypoint(int waypointCount)
    {
        if (_hunter.PingPongPatrol)
        {
            int next = _currentIndex + _direction;

            if (next >= waypointCount || next < 0)
            {
                _direction *= -1;
                next = _currentIndex + _direction;
            }

            _currentIndex = Mathf.Clamp(next, 0, waypointCount - 1);
        }
        else
        {
            _currentIndex = (_currentIndex + 1) % waypointCount;
        }
    }

    private void UpdateSpawnTimer()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < _hunter.PoiSpawnInterval)
            return;

        if (!_hunter.HasRoomForInterestPoint())
            return;

        _spawnTimer = 0f;
        _isPlacingInterestPoint = true;
        _placeTimer = 0f;

        _hunter.StopMoving();
        _hunter.SetFeedback(
            HunterAgent.HunterState.Patrol,
            null,
            "Colocando objeto de interés"
        );

        Debug.Log("[Cazador] Se detiene a colocar un objeto de interés.");
    }

    private void UpdatePlacingInterestPoint()
    {
        _hunter.StopMoving();
        _placeTimer += Time.deltaTime;

        if (_placeTimer >= _hunter.InterestPointPlaceDuration)
        {
            _hunter.SpawnInterestPointAtCurrentPosition();
            _isPlacingInterestPoint = false;
            _hunter.SetFeedback(HunterAgent.HunterState.Patrol);
        }
    }
}