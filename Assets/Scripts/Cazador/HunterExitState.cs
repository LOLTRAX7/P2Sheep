using UnityEngine;

// Estado Exit.
//
// Se entra al completar RequiredGathers. Se dirige a la Salida mediante A* y no ataca ni recolecta.
// Al llegar, desaparece, reaparece en la Zona inicial con el contador en cero y vuelve a Search.
// Si aparece un lobo, Flee tiene prioridad y se interrumpe la salida.
public class HunterExitState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private bool _completing;
    private float _completeTimer;

    public HunterExitState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _completing = false;
        _completeTimer = 0f;

        _hunter.SetFeedback(HunterAgent.HunterState.Exit, null, "Yendo a la Salida");
        FeedbackLog.Add("[Cazador] Exit: recolectó " + _hunter.Gathers + "/" + _hunter.RequiredGathers + ". Se dirige a la Salida.", new Color(0.3f, 1f, 0.4f));
    }

    public override void Update()
    {
        // Flee tiene más prioridad que Exit.
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        if (_completing)
        {
            _completeTimer -= Time.deltaTime;

            if (_completeTimer > 0f) return;

            _hunter.FinishExitCycle();
            FeedbackLog.Add("[Cazador] Objetivo cumplido: reaparece en la Zona inicial y reinicia el ciclo.", new Color(0.3f, 1f, 0.4f));
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        if (_hunter.Gathers < _hunter.RequiredGathers)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        _hunter.MoveTo(_hunter.ExitZone.Center, _hunter.MoveSpeed);
        _hunter.SetFeedback(HunterAgent.HunterState.Exit, null, "Yendo a la Salida");

        if (!_hunter.HasReached(_hunter.ExitZone.Center, 1.2f)) return;

        _completing = true;
        _completeTimer = 0.6f;
        _hunter.StopMoving();
        _hunter.SetBodyVisible(false);
        _hunter.SetFeedback(HunterAgent.HunterState.Exit, null, "Llegó a la Salida");
        FeedbackLog.Add("[Cazador] Llegó a la Salida. Desaparece.", new Color(0.3f, 1f, 0.4f), false);
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
