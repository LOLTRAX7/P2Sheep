using UnityEngine;

// Persecución: el Cazador está dentro de las condiciones de percepción (distancia + ángulo + línea de visión).
// Corre hacia él con A* cuando no hay línea recta y lo captura al entrar en CaptureRadius.
//
// Transiciones:
//   -> GoToPosition si deja de percibirlo (va a la última posición conocida)
//   -> Patrol       si el Cazador entra a una Zona segura o muere
public class WolfChaseState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;

    public WolfChaseState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        if (_wolf.Hunter == null || _wolf.Hunter.IsDead || _wolf.Hunter.IsInSafeZone)
        {
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        _wolf.NotifyTargetSpotted();
        _wolf.SetStateFeedbackLabel("Persiguiendo al Cazador");
    }

    public override void Update()
    {
        HunterAgent hunter = _wolf.Hunter;

        if (hunter == null || hunter.IsDead)
        {
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        // Regla de la consigna: si el Cazador entra a una Zona segura, se descarta el objetivo y se vuelve a patrullar.
        if (hunter.IsInSafeZone)
        {
            FeedbackLog.Add(_wolf.name + " abandona la persecución: el Cazador entró a una Zona segura.", new Color(0.65f, 0.5f, 0.92f));
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        if (!_wolf.CanSeeHunter)
        {
            _fsm.ChangeState(WolfAgent.WolfState.GoToPosition);
            return;
        }

        _wolf.NotifyTargetSpotted();
        _wolf.MoveTo(hunter.transform.position, _wolf.ChaseSpeed);
        _wolf.SetStateFeedbackLabel("Persiguiendo al Cazador");

        if (!_wolf.TryCaptureHunter()) return;

        _wolf.ForgetHunter();
        _fsm.ChangeState(WolfAgent.WolfState.Patrol);
    }

    public override void Exit() => _wolf.StopMoving();
}
