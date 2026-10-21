using UnityEngine;

// Patrulla: recorre los waypoints (nodos) de su ruta. El próximo punto se alcanza directo si hay línea recta libre
// y con A* si no (lo resuelve GraphNavigator). No usa nodos de Zonas seguras.
//
// Transiciones:
//   -> Chase        si el sensor ve al Cazador
//   -> GoToPosition si llegó una alerta de otro lobo
public class WolfPatrolState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;

    private int _index;
    private int _direction = 1;
    private bool _firstEnter = true;

    public WolfPatrolState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        if (_wolf.PatrolNodes.Count == 0) return;

        _index = _firstEnter ? 0 : GetClosestWaypointIndex();
        _firstEnter = false;
        _wolf.SetStateFeedbackLabel("Patrullando");
        FeedbackLog.Add(_wolf.name + " patrulla por " + _wolf.PatrolNodes.Count + " waypoints.", new Color(0.75f, 0.75f, 0.8f), false);
    }

    public override void Update()
    {
        if (_wolf.PatrolNodes.Count == 0) return;

        // El Cazador está en una Zona segura (o muerto): no hay nada que perseguir.
        if (_wolf.HunterInsideSafeZone || _wolf.HunterIsDead)
        {
            if (_wolf.HasKnownPosition) _wolf.ForgetHunter();
        }
        else if (_wolf.CanSeeHunter)
        {
            _fsm.ChangeState(WolfAgent.WolfState.Chase);
            return;
        }
        else if (_wolf.HasPendingAlert)
        {
            _fsm.ChangeState(WolfAgent.WolfState.GoToPosition);
            return;
        }

        Vector3 target = _wolf.PatrolNodes[_index].transform.position;

        _wolf.MoveTo(target, _wolf.PatrolSpeed);

        if (!_wolf.HasReached(target, 0.9f)) return;

        Advance();
    }

    private void Advance()
    {
        int count = _wolf.PatrolNodes.Count;

        if (count <= 1)
        {
            _index = 0;
            return;
        }

        if (!_wolf.PingPong)
        {
            _index = (_index + 1) % count;
            return;
        }

        _index += _direction;

        if (_index >= count)
        {
            _index = count - 2;
            _direction = -1;
        }
        else if (_index < 0)
        {
            _index = 1;
            _direction = 1;
        }
    }

    private int GetClosestWaypointIndex()
    {
        int best = 0;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < _wolf.PatrolNodes.Count; i++)
        {
            Vector3 offset = _wolf.PatrolNodes[i].transform.position - _wolf.transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude >= bestDistance) continue;

            bestDistance = offset.sqrMagnitude;
            best = i;
        }

        return best;
    }

    public override void Exit() => _wolf.StopMoving();
}
