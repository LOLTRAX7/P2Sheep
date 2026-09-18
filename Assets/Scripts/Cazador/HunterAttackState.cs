using UnityEngine;

public class HunterAttackState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;
    private float _timer;
    private bool _preparing;


    public HunterAttackState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _target = _hunter.FindNearestLiveBoid();
        _timer = 0f;
        _preparing = false;

        Debug.Log($"[Cazador] Attack. Objetivo: {(_target != null ? _target.name : "ninguno")}");
        _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target, "Buscando rango de ataque");
    }

    public override void Update()
    {
        // Si no hay objetivo, vuelve a Patrol.
        if (_target == null || _target.IsEliminated)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        // Si el Boid se aleja demasiado, se cancela Attack.
        if (distance > _hunter.PerceptionLossRadius)
        {
            _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target, "Objetivo perdido");
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
            return;
        }

        // Todavía no está en ningún rango de ataque: perseguir.
        if (distance > _hunter.RangeAttackRadius)
        {
            _preparing = false;
            _timer = 0f;
            _hunter.MoveTowards(_target.transform.position);
            _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target, "Persiguiendo");
            return;
        }

        // Ya está en rango: detenerse y preparar el ataque.
        _hunter.StopMoving();

        if (!_preparing)
        {
            _preparing = true;
            _timer = 0f;
        }

        _timer += Time.deltaTime;

        string tipo = distance <= _hunter.MeleeAttackRadius
            ? "Preparando ataque cuerpo a cuerpo"
            : "Preparando ataque a distancia";

        _hunter.SetFeedback(
            HunterAgent.HunterState.Attack,
            _target,
            $"{tipo} ({_timer:0.0}/{_hunter.AttackPreparationTime:0.0}s)"
        );

        if (_timer < _hunter.AttackPreparationTime)
            return;

        // Se vuelve a comprobar la distancia justo antes de atacar.
        distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (distance <= _hunter.MeleeAttackRadius)
        {
            _hunter.PerformMeleeAttack(_target);
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
        }
        else if (distance <= _hunter.RangeAttackRadius)
        {
            _hunter.PerformRangedAttack(_target);
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
        }
        else
        {
            // Se alejó mientras preparábamos el ataque
            _preparing = false;
            _timer = 0f;
            _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target, "Ataque cancelado");
        }
    }

    public override void Exit()
    {
        _hunter.StopMoving();
        Debug.Log("[Cazador] Sale de Attack.");
    }
}
