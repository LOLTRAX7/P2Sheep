using UnityEngine;

// Compatibilidad con el Hunter del Parcial 1.
//
// La escena Lvl1 conserva su Hunter con una lista de waypoints, pero no tiene las zonas ni el grafo
// del Parcial 2. Cuando HunterAgent detecta esa situación, usa este estado (con el ID Search) para
// seguir patrullando como antes, sin tocar la FSM del Parcial 2.
public class HunterLegacyPatrolState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;

    private int _currentIndex;
    private int _direction = 1;
    private float _spawnTimer;
    private bool _isPlacingInterestPoint;
    private float _placeTimer;

    public HunterLegacyPatrolState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _hunter.SetFeedback(HunterAgent.HunterState.Search, null, "Patrullando (modo Parcial 1)");
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
        _hunter.StopMoving();
        _isPlacingInterestPoint = false;
    }

    private void PatrolWaypoints()
    {
        var waypoints = _hunter.LegacyWaypoints;

        if (waypoints == null || waypoints.Count == 0)
        {
            _hunter.StopMoving();
            return;
        }

        if (_currentIndex >= waypoints.Count) _currentIndex = 0;

        Transform currentWaypoint = waypoints[_currentIndex];

        if (currentWaypoint == null)
        {
            AdvanceWaypoint(waypoints.Count);
            return;
        }

        _hunter.MoveTowards(currentWaypoint.position);

        if (_hunter.HasReached(currentWaypoint.position, _hunter.WaypointArrivalDistance))
            AdvanceWaypoint(waypoints.Count);
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

        if (_spawnTimer < _hunter.InterestPointInterval) return;
        if (!_hunter.HasRoomForInterestPoint()) return;

        _spawnTimer = 0f;
        _isPlacingInterestPoint = true;
        _placeTimer = 0f;

        _hunter.StopMoving();
        _hunter.SetFeedback(HunterAgent.HunterState.Search, null, "Colocando objeto de interés");
    }

    private void UpdatePlacingInterestPoint()
    {
        _hunter.StopMoving();
        _placeTimer += Time.deltaTime;

        if (_placeTimer < _hunter.InterestPointPlaceDuration) return;

        _hunter.SpawnInterestPointAtCurrentPosition();
        _isPlacingInterestPoint = false;
        _hunter.SetFeedback(HunterAgent.HunterState.Search, null, "Patrullando (modo Parcial 1)");
    }
}
