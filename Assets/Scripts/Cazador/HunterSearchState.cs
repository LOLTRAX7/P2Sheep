using UnityEngine;

// Estado Search (reemplaza al Patrol del Parcial 1).
//
// - Si está fuera del Coto, calcula con A* el camino hasta el Coto y lo recorre.
// - Dentro del Coto, si no percibe Boids, se desplaza a nodos del Coto elegidos al azar (A*).
// - Cada cierto intervalo genera un objeto de interés, si hay menos de 5 activos.
//
// Orden de evaluación (prioridad de la consigna):
//   1) Flee  si percibe un lobo.
//   2) Gather si percibe un Boid eliminado.
//   3) Attack si el TBA terminó y percibe al menos un Boid vivo.
public class HunterSearchState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;

    private Vector3 _wanderTarget;
    private bool _hasWanderTarget;
    private float _interestTimer;

    public HunterSearchState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _hasWanderTarget = false;
        _interestTimer = _hunter.InterestPointInterval;

        _hunter.SetFeedback(HunterAgent.HunterState.Search, null, "Yendo al Coto de caza");
        FeedbackLog.Add("[Cazador] Search: se dirige al Coto de caza.", Color.white, false);
    }

    public override void Update()
    {
        // Prioridad 1: lobo percibido -> Flee.
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        // Prioridad 2: Boid eliminado dentro de VisionRadius -> Gather.
        if (_hunter.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }

        // Prioridad 3: TBA listo + al menos un Boid vivo percibido -> Attack.
        if (_hunter.IsAttackReady && _hunter.FindNearestLiveBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Attack);
            return;
        }

        if (_hunter.HuntingZone == null)
        {
            _hunter.StopMoving();
            return;
        }

        if (!_hunter.HuntingZone.Contains(_hunter.transform.position))
        {
            _hunter.MoveTo(_hunter.HuntingZone.Center, _hunter.MoveSpeed);
            _hunter.SetFeedback(HunterAgent.HunterState.Search, null, "Yendo al Coto de caza");
            return;
        }

        // Ya está dentro del Coto: genera objetos de interés cada cierto intervalo.
        _interestTimer -= Time.deltaTime;

        if (_interestTimer <= 0f)
        {
            _interestTimer = _hunter.InterestPointInterval;
            _hunter.SpawnInterestPoint();
        }

        // Si no percibe Boids vivos, recorre el Coto hacia nodos elegidos al azar.
        bool seesLiveBoid = _hunter.FindNearestLiveBoid() != null;

        if (seesLiveBoid)
        {
            _hunter.StopMoving();
            _hunter.SetFeedback(HunterAgent.HunterState.Search, null, "Esperando el TBA");
            return;
        }

        if (!_hasWanderTarget)
        {
            Node node = _hunter.GetRandomHuntingNode();

            if (node == null)
            {
                _hunter.StopMoving();
                return;
            }

            _wanderTarget = node.transform.position;
            _hasWanderTarget = true;
        }

        _hunter.MoveTo(_wanderTarget, _hunter.MoveSpeed);
        _hunter.SetFeedback(HunterAgent.HunterState.Search, null, "Recorriendo el Coto en busca de Boids");

        if (_hunter.HasReached(_wanderTarget, 0.8f))
        {
            _hasWanderTarget = false;
        }
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
