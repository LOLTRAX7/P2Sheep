using UnityEngine;

// Estado Gather.
//
// Se entra al detectar un Boid eliminado (pendiente de recolección) dentro de VisionRadius.
// - Se dirige al Boid (A* si no hay línea de visión; Seek/Arrive en el tramo final directo).
// - Ejecuta la recolección durante GatherDuration.
// - Al terminar, el Boid desaparece y reaparece dentro del Coto, y el Cazador suma una recolección.
//
// Si el objetivo deja de estar disponible antes de completar la acción, se cancela sin sumar.
// Si aparece un lobo, Flee tiene prioridad y la recolección se cancela.
public class HunterGatherState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;
    private float _gatherTimer;

    public HunterGatherState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _target = _hunter.FindNearestEliminatedBoid();
        _gatherTimer = 0f;

        if (_target == null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target, "Yendo a recolectar");
        FeedbackLog.Add("[Cazador] Gather: va a recolectar a " + _target.name + ".", new Color(1f, 0.9f, 0.3f), false);
    }

    public override void Update()
    {
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        // El Boid ya no está disponible (lo recolectó otro, revivió, etc.): se abandona sin sumar.
        if (_target == null || !_target.isActiveAndEnabled || !_target.IsEliminated || !_target.IsPendingGather)
        {
            FeedbackLog.Add("[Cazador] Gather cancelado: el objetivo ya no está disponible.", Color.yellow, false);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        float distance = _hunter.FlatDistanceTo(_target.transform.position);

        if (distance > _hunter.GatherInteractionRadius)
        {
            _gatherTimer = 0f;
            // Tramo final: si hay línea de visión directa, Seek/Arrive; si no, A* sobre el grafo.
            if (_hunter.HasClearSightTo(_target.transform.position))
                _hunter.MoveDirect(_target.transform.position, _hunter.MoveSpeed);
            else
                _hunter.MoveTo(_target.transform.position, _hunter.MoveSpeed);

            _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target, "Yendo a recolectar");
            return;
        }

        _hunter.StopMoving();
        _gatherTimer += Time.deltaTime;
        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target, "Recolectando (" + _gatherTimer.ToString("0.0") + "/" + _hunter.GatherDuration.ToString("0.0") + " s)");

        if (_gatherTimer < _hunter.GatherDuration) return;

        _target.OnGathered(_hunter.BoidRespawnDelay);
        _hunter.AddGather();

        if (_hunter.Gathers >= _hunter.RequiredGathers && _hunter.ExitZone != null)
            _fsm.ChangeState(HunterAgent.HunterState.Exit);
        else
            _fsm.ChangeState(HunterAgent.HunterState.Search);
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
