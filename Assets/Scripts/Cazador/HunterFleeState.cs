using UnityEngine;

// Estado Flee (prioridad sólo superada por Dead).
//
// Al entrar elige la Zona segura más cercana por longitud de camino A* y se dirige allí con A*.
// No ataca ni recolecta mientras huye; una recolección en curso se cancela al cambiar de estado.
// Al entrar en la Zona segura espera SafeZoneWaitTime y retoma:
//   - Exit si ya alcanzó RequiredGathers;
//   - Search en cualquier otro caso.
public class HunterFleeState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private Zone _safeZone;
    private float _waitTimer;
    private bool _waiting;
    private bool _arrived;

    public HunterFleeState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _waitTimer = 0f;
        _waiting = false;
        _arrived = false;

        _safeZone = _hunter.ChooseNearestSafeZone();

        if (_safeZone == null)
        {
            FeedbackLog.Add("[Cazador] Flee: no hay Zona segura configurada.", Color.red);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        string safeName = _safeZone.name;
        _hunter.SetFeedback(HunterAgent.HunterState.Flee, null, "Huyendo a " + safeName);
        FeedbackLog.Add("[Cazador] ¡Ve un lobo! Huye a la Zona segura más cercana por A*: " + safeName + ".", new Color(0.2f, 0.9f, 1f));
    }

    public override void Update()
    {
        if (_safeZone == null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        // Ya está dentro de la Zona segura: los lobos no pueden detectarlo. Espera y retoma.
        if (_hunter.IsInSafeZone)
        {
            _hunter.StopMoving();

            if (!_waiting)
            {
                _waiting = true;
                _waitTimer = _hunter.SafeZoneWaitTime;
                _hunter.SetFeedback(HunterAgent.HunterState.Flee, null, "A salvo en " + _safeZone.name + ": esperando " + _waitTimer.ToString("0.0") + " s");
                FeedbackLog.Add("[Cazador] Entró a " + _safeZone.name + ". Los lobos pierden el objetivo. Espera " + _waitTimer.ToString("0.0") + " s.", new Color(0.2f, 0.9f, 1f), false);
            }

            _waitTimer -= Time.deltaTime;

            if (_waitTimer > 0f) return;

            FeedbackLog.Add("[Cazador] Terminó la espera en la Zona segura. Retoma su actividad.", new Color(0.2f, 0.9f, 1f), false);

            if (_hunter.Gathers >= _hunter.RequiredGathers)
                _fsm.ChangeState(HunterAgent.HunterState.Exit);
            else
                _fsm.ChangeState(HunterAgent.HunterState.Search);

            return;
        }

        // Todavía no llegó: se mueve con A* hacia el centro de la Zona segura.
        _hunter.MoveTo(_safeZone.Center, _hunter.FleeSpeed);

        if (!_arrived && _hunter.HasReached(_safeZone.ClosestPoint(_hunter.transform.position), 1.2f))
        {
            _arrived = true;
        }

        string action = _arrived
            ? "Entrando a " + _safeZone.name
            : "Huyendo a " + _safeZone.name + " (" + _hunter.VisibleWolfCount + " lobo/s a la vista)";

        _hunter.SetFeedback(HunterAgent.HunterState.Flee, null, action);
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
