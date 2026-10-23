using System.Collections.Generic;
using UnityEngine;

// Búsqueda: el lobo llegó a la zona donde se vio al Cazador y no lo encuentra.
// Mira alrededor (gira en el lugar) y recorre nodos cercanos a esa posición.
// Renueva los puntos cada SearchDuration; no olvida al objetivo por un temporizador.
//
// Transiciones:
//   -> Chase        si vuelve a percibir al Cazador
//   -> GoToPosition si llega una posición conocida nueva
//   -> Patrol       si el Cazador entra a una Zona segura o si muere
public class WolfSearchState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;
    private readonly List<Node> _candidates = new();

    private float _timer;
    private float _scanTimer;
    private bool _scanning;
    private int _index;

    public WolfSearchState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        _timer = _wolf.SearchDurationForState;
        _scanTimer = 0f;
        _scanning = false;
        _index = 0;

        ChooseSearchPoints();

        _wolf.SetStateFeedbackLabel("Buscando al Cazador cerca de " + WolfAgent.Format(_wolf.LastKnownPosition));
        FeedbackLog.Add(_wolf.name + " busca al Cazador cerca de " + WolfAgent.Format(_wolf.LastKnownPosition) + ".", new Color(1f, 0.9f, 0.2f), false);
    }

    public override void Update()
    {
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
            _fsm.ChangeState(WolfAgent.WolfState.GoToPosition);
            return;
        }

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            _timer = _wolf.SearchDurationForState;
            ChooseSearchPoints();
            _index = _candidates.Count > 0 ? _index % _candidates.Count : 0;
        }

        if (_candidates.Count == 0)
        {
            _wolf.Scan(120f);
            return;
        }

        if (_scanning)
        {
            _wolf.Scan(150f);
            _scanTimer -= Time.deltaTime;

            if (_scanTimer <= 0f)
            {
                _scanning = false;
                _index = (_index + 1) % _candidates.Count;
            }

            return;
        }

        Vector3 point = _candidates[_index].transform.position;
        _wolf.MoveTo(point, _wolf.AlertSpeed);
        _wolf.SetStateFeedbackLabel("Buscando: nodo " + _candidates[_index].name + " (" + (_index + 1) + "/" + _candidates.Count + ")");

        if (!_wolf.HasReached(point, 1f)) return;

        _scanning = true;
        _scanTimer = _wolf.ScanDuration;
    }

    private void ChooseSearchPoints()
    {
        _candidates.Clear();

        Graph graph = _wolf.Graph;

        if (graph == null) return;

        graph.GetNodesWithin(_wolf.LastKnownPosition, _wolf.SearchRadius, _candidates, true);

        // Si no hay nodos cerca, se usa el más cercano a la posición conocida (sin entrar a Zonas seguras).
        if (_candidates.Count == 0)
        {
            Node closest = graph.GetClosestNode(_wolf.LastKnownPosition, true);
            if (closest != null) _candidates.Add(closest);
        }
    }

    public override void Exit() => _wolf.StopMoving();
}
