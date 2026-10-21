using UnityEngine;

// Estado Attack.
//
// Sólo se entra si el TBA terminó y hay al menos un Boid vivo dentro de la percepción.
// Dentro del estado:
//   - si el objetivo está dentro de MeleeAttackRadius -> ataque cuerpo a cuerpo;
//   - si está dentro de RangeAttackRadius -> ataque a distancia;
//   - si no está en ningún rango -> lo persigue con Pursuit (predicción de la posición futura),
//     usando A* cuando no hay línea de visión directa.
//
// Al atacar con éxito reinicia el TBA y vuelve a Search. Si el objetivo abandona el rango de visión,
// vuelve a Search SIN reiniciar el TBA.
public class HunterAttackState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;

    public HunterAttackState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _target = _hunter.FindBestLiveBoid();

        if (_target == null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target, "Eligiendo objetivo");
        FeedbackLog.Add("[Cazador] Attack: objetivo " + _target.name + ".", new Color(1f, 0.4f, 0.4f), false);
    }

    public override void Update()
    {
        // Prioridad 1: un lobo percibido cancela el ataque.
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        // Prioridad 2: Gather tiene más prioridad que Attack.
        if (_hunter.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }

        if (_target == null || _target.IsEliminated)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        float distance = _hunter.FlatDistanceTo(_target.transform.position);
        bool inSight = distance <= _hunter.VisionRadius && _hunter.HasClearSightTo(_target.transform.position);

        // Si el objetivo abandona el rango de visión, vuelve a Search sin reiniciar el TBA.
        if (!inSight)
        {
            _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target, "Objetivo perdido");
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        if (distance <= _hunter.MeleeAttackRadius)
        {
            _hunter.StopMoving();
            _hunter.PerformMeleeAttack(_target);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        if (distance <= _hunter.RangeAttackRadius)
        {
            _hunter.StopMoving();
            _hunter.PerformRangedAttack(_target);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        // Fuera de rango: Pursuit con predicción. Si no hay línea de visión, el HunterAgent usa A*.
        _hunter.PursueBoid(_target, _hunter.MoveSpeed);
        _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target, "Persiguiendo (Pursuit)");
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
