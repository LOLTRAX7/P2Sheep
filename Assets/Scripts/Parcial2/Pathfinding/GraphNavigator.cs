using System.Collections.Generic;
using UnityEngine;

// Movimiento por el grafo. Lo usan los lobos Y el Cazador.
//
// Se le pregunta "¿hacia qué punto camino para llegar a este destino?" con GetNextPoint(destino):
//   - Si hay línea recta libre (sin paredes) hacia el destino, devuelve el destino: va directo.
//   - Si no, calcula un camino con A* sobre el grafo y devuelve el próximo nodo del camino.
//
// El Cazador sólo usa el tramo directo para el ataque (Pursuit) y la recolección; para ir al Coto,
// a una Zona segura o a la Salida el GraphNavigator hace A* cuando no hay línea de visión.
//
// Los lobos se configuran con avoidSafeZones = true: A* nunca usa los nodos de las Zonas seguras.
public class GraphNavigator : MonoBehaviour
{
    [Header("Llegada")]
    [Tooltip("Distancia a la que se da por alcanzado un nodo del camino y se pasa al siguiente.")]
    [SerializeField] private float _nodeArrivalDistance = 0.5f;

    [Header("Replanificación")]
    [Tooltip("Si el destino se mueve más que esto desde que se planificó, se vuelve a planificar.")]
    [SerializeField] private float _destinationRepathDistance = 2f;
    [SerializeField] private float _minRepathInterval = 0.3f;
    [Tooltip("Cada cuánto se revisa si apareció (o se despejó) la línea recta hacia el destino.")]
    [SerializeField] private float _lineCheckInterval = 0.25f;

    [Header("Feedback")]
    [SerializeField] private bool _logPathfinding = true;
    [SerializeField] private float _logCooldown = 2f;
    [SerializeField] private Color _logColor = new(1f, 0.65f, 0.2f);

    private readonly List<Vector3> _path = new();
    private Vector3 _plannedDestination;
    private bool _hasPlan;
    private bool _avoidSafeZones;
    private float _lastPlanTime = -999f;
    private float _nextLineCheck;
    private float _nextLogTime;

    // True mientras el agente sigue un camino calculado con A* (si va directo al destino es false).
    public bool UsingPathfinding => _path.Count > 0;

    // Nodos que le faltan recorrer (para dibujar el camino).
    public IReadOnlyList<Vector3> PathPoints => _path;

    public Vector3 CurrentDestination => _plannedDestination;
    public bool AvoidSafeZones => _avoidSafeZones;

    // Lo llama el LevelBuilder / WolfAgent.
    public void Configure(bool avoidSafeZones)
    {
        _avoidSafeZones = avoidSafeZones;
    }

    // Olvida el camino actual (se llama al cambiar de estado).
    public void ResetPath()
    {
        _path.Clear();
        _hasPlan = false;
    }

    // ¿El destino está prohibido para este agente? (los lobos no pueden entrar a las Zonas seguras).
    public bool IsDestinationForbidden(Vector3 destination)
    {
        return _avoidSafeZones && Zone.IsInsideSafeZone(destination);
    }

    // Devuelve el punto al que hay que caminar ahora para ir hacia 'destination'.
    public Vector3 GetNextPoint(Vector3 destination)
    {
        Graph graph = Graph.Current;

        // Sin grafo en la escena se comporta como un movimiento directo.
        if (graph == null) return destination;

        if (IsDestinationForbidden(destination))
        {
            _path.Clear();
            return transform.position;
        }

        Vector3 position = transform.position;

        bool destinationMoved = Flat(destination - _plannedDestination).sqrMagnitude > _destinationRepathDistance * _destinationRepathDistance;

        if (!_hasPlan || (destinationMoved && Time.time - _lastPlanTime >= _minRepathInterval))
        {
            Plan(graph, position, destination);
        }
        else if (Time.time >= _nextLineCheck)
        {
            _nextLineCheck = Time.time + _lineCheckInterval;

            bool clear = graph.IsPathClear(position, destination);

            // Iba directo y apareció una pared en el medio: ahora hace falta A*.
            if (_path.Count == 0 && !clear) Plan(graph, position, destination);

            // Iba por el grafo y ya hay línea recta: atajo, se descarta el resto del camino.
            else if (_path.Count > 0 && clear) _path.Clear();
        }

        // Se descartan los nodos que ya se alcanzaron.
        while (_path.Count > 0 && Flat(_path[0] - position).magnitude <= _nodeArrivalDistance)
        {
            _path.RemoveAt(0);
        }

        return _path.Count > 0 ? _path[0] : destination;
    }

    private void Plan(Graph graph, Vector3 position, Vector3 destination)
    {
        _hasPlan = true;
        _plannedDestination = destination;
        _lastPlanTime = Time.time;
        _nextLineCheck = Time.time + _lineCheckInterval;
        _path.Clear();

        // Si hay línea recta libre no hace falta A*.
        if (graph.IsPathClear(position, destination)) return;

        // Punto de entrada al grafo (desde mi posición) y de salida (hacia el destino).
        Node start = graph.GetClosestVisibleNode(position, false, _avoidSafeZones);
        Node goal = graph.GetClosestVisibleNode(destination, true, _avoidSafeZones);

        if (goal == null) goal = graph.GetClosestVisibleNode(destination, false, _avoidSafeZones);
        if (start == null) start = graph.GetClosestNode(position, _avoidSafeZones);
        if (goal == null) goal = graph.GetClosestNode(destination, _avoidSafeZones);

        if (start == null || goal == null) return;

        List<Node> nodes = Pathfinding.AStar(start, goal, _avoidSafeZones);

        // Sin camino posible: queda en movimiento directo (no se inventa una ruta).
        if (nodes.Count == 0) return;

        foreach (Node node in nodes) _path.Add(node.transform.position);

        SmoothPath(graph, position, destination);

        if (_logPathfinding && Time.time >= _nextLogTime)
        {
            _nextLogTime = Time.time + _logCooldown;
            FeedbackLog.Add(name + " usa A* hacia (" + destination.x.ToString("0") + ", " + destination.z.ToString("0") + "): " + _path.Count + " nodos", _logColor, false);
        }
    }

    // Atajos para evitar rodeos inútiles:
    //  - si desde mi posición ya veo el 2° nodo, el 1° sobra;
    //  - si desde el penúltimo nodo ya veo el destino, el último sobra.
    private void SmoothPath(Graph graph, Vector3 position, Vector3 destination)
    {
        while (_path.Count > 1 && graph.IsPathClear(position, _path[1]))
        {
            _path.RemoveAt(0);
        }

        while (_path.Count > 1 && graph.IsLinkClear(_path[_path.Count - 2], destination))
        {
            _path.RemoveAt(_path.Count - 1);
        }
    }

    private static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}
