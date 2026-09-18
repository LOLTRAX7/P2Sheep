using UnityEngine;
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
        Debug.Log($"[Cazador] Entra a Gather. Objetivo: {(_target != null ? _target.name : "ninguno")}");
        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target);
    }

    public override void Update()
    {
        // El objetivo dejó de estar disponible (ya no existe, revivió, o ya
        // fue recolectado): se abandona la recolección y se vuelve a Patrol.
        if (_target == null || !_target.IsEliminated || !_target.IsPendingGather)
        {
            Debug.Log("[Cazador] El objetivo de recolección ya no está disponible.");
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (distance > _hunter.GatherInteractionRadius)
        {
            _hunter.MoveTowards(_target.transform.position);
            _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target, "Yendo a recolectar");
            return;
        }

        // Ya está en posición: ejecuta la acción de recolección durante el tiempo requerido.
        _hunter.StopMoving();
        _gatherTimer += Time.deltaTime;
        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target, "Recolectando");

        if (_gatherTimer >= _hunter.GatherDuration)
        {
            Debug.Log("[Cazador] Recolección completa.");
            _target.OnGathered(_hunter.BoidRespawnDelay);
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
        }
    }

    public override void Exit()
    {
        Debug.Log("[Cazador] Sale de Gather.");
    }
}
