using UnityEngine;

// Ir a la última posición conocida: el lobo no ve al Cazador, pero sabe dónde estuvo (porque lo vio o porque otro aulló).
// Va hacia esa posición con A* si no hay línea recta. La posición no se actualiza sola: es una foto del momento del aviso.
//
// Transiciones:
//   -> Chase   si vuelve a percibir al Cazador
//   -> Search  al llegar a la posición conocida
//   -> Patrol  si el Cazador está en una Zona segura o murió
// Si mientras camina llega una alerta más nueva, actualiza el destino y sigue en el mismo estado.
public class WolfGoToPositionState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;
    private Vector3 _destination;

    public WolfGoToPositionState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        _wolf.ConsumePendingAlert();

        if (!_wolf.HasKnownPosition || Zone.IsInsideSafeZone(_wolf.LastKnownPosition))
        {
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        _destination = _wolf.LastKnownPosition;
        _wolf.SetStateFeedbackLabel("Yendo a la última posición conocida " + WolfAgent.Format(_destination));
        FeedbackLog.Add(_wolf.name + " va a la última posición conocida " + WolfAgent.Format(_destination) + ".", new Color(1f, 0.65f, 0.2f), false);
    }

    public override void Update()
    {
        // Si el Cazador entró a una Zona segura, se descarta el objetivo.
        if (_wolf.HunterInsideSafeZone || _wolf.HunterIsDead)
        {
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        if (_wolf.CanSeeHunter)
        {
            _fsm.ChangeState(WolfAgent.WolfState.Chase);
            return;
        }

        if (_wolf.HasPendingAlert)
        {
            _wolf.ConsumePendingAlert();
            _destination = _wolf.LastKnownPosition;

            FeedbackLog.Add(_wolf.name + " actualiza la posición conocida a " + WolfAgent.Format(_destination) + ".", new Color(1f, 0.65f, 0.2f), false);
        }

        if (!_wolf.HasKnownPosition || Zone.IsInsideSafeZone(_destination))
        {
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        _wolf.MoveTo(_destination, _wolf.AlertSpeed);
        _wolf.SetStateFeedbackLabel("Yendo a la posición conocida " + WolfAgent.Format(_destination));

        if (!_wolf.HasReached(_destination, 1.2f)) return;

        _fsm.ChangeState(WolfAgent.WolfState.Search);
    }

    public override void Exit() => _wolf.StopMoving();
}
