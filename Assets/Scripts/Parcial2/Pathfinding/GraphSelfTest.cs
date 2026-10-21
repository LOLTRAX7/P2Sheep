using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Autotest del grafo y de A*. Es una HERRAMIENTA DE VERIFICACIÓN (no forma parte de la IA):
// te dice si las conexiones del grafo y Pathfinding.AStar están bien, comparando contra un Dijkstra de referencia.
// El LevelBuilder lo agrega solo; también se puede correr desde el menú contextual del componente.
public class GraphSelfTest : MonoBehaviour
{
    [SerializeField] private bool _runOnStart = true;
    [SerializeField] private float _costTolerance = 0.01f;

    public void Configure(bool runOnStart)
    {
        _runOnStart = runOnStart;
    }

    private IEnumerator Start()
    {
        // Espera un frame para que el Graph termine de generar las conexiones en su Start.
        yield return null;

        if (_runOnStart) Run();
    }

    [ContextMenu("Probar grafo y A*")]
    public void Run()
    {
        Graph graph = Graph.Current;

        if (graph == null)
        {
            Debug.LogWarning("[AutoTest] No hay un Graph en la escena.");
            return;
        }

        List<Node> nodes = graph.GetAllNodes;

        // 1) Estructura del grafo
        int directedEdges = 0;
        int asymmetric = 0;
        int crossingWalls = 0;
        Vector3 lift = Vector3.up * graph.CastHeight;

        foreach (Node node in nodes)
        {
            foreach (Node other in node.Neighbors)
            {
                directedEdges++;

                if (!HasNeighbor(other, node)) asymmetric++;

                if (Physics.Linecast(node.transform.position + lift, other.transform.position + lift, graph.WallMask)) crossingWalls++;
            }
        }

        int components = CountComponents(nodes);

        bool graphOk = directedEdges > 0 && asymmetric == 0 && crossingWalls == 0 && components == 1;
        string graphReport = "[AutoTest] Grafo -> nodos: " + nodes.Count
            + " | aristas: " + (directedEdges / 2)
            + " | componentes conexas: " + components + " (debería ser 1)"
            + " | aristas no simétricas: " + (asymmetric / 2)
            + " | aristas que cruzan paredes: " + (crossingWalls / 2);

        Report(graphReport, graphOk);

        // 2) A* contra Dijkstra
        int tested = 0;
        int noPath = 0;
        int invalid = 0;
        int notOptimal = 0;

        foreach (Node origin in nodes)
        {
            Dictionary<Node, float> expected = Dijkstra(nodes, origin);

            foreach (Node target in nodes)
            {
                if (origin == target) continue;

                tested++;

                List<Node> path = Pathfinding.AStar(origin, target);

                if (path.Count == 0)
                {
                    noPath++;
                    continue;
                }

                if (!IsValidPath(path, origin, target))
                {
                    invalid++;
                    continue;
                }

                if (Mathf.Abs(PathCost(path) - expected[target]) > _costTolerance) notOptimal++;
            }
        }

        bool astarOk = tested > 0 && noPath == 0 && invalid == 0 && notOptimal == 0;
        string astarReport = "[AutoTest] A* -> pares probados: " + tested
            + " | sin camino: " + noPath
            + " | camino inválido: " + invalid
            + " | costo distinto al óptimo: " + notOptimal;

        Report(astarReport, astarOk);
    }

    private static void Report(string message, bool ok)
    {
        FeedbackLog.Add(message, ok ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.3f), false);

        if (ok) Debug.Log(message);
        else Debug.LogWarning(message);
    }

    private static bool HasNeighbor(Node node, Node candidate)
    {
        foreach (Node n in node.Neighbors)
        {
            if (n == candidate) return true;
        }

        return false;
    }

    private static int CountComponents(List<Node> nodes)
    {
        HashSet<Node> visited = new();
        int components = 0;

        foreach (Node start in nodes)
        {
            if (visited.Contains(start)) continue;

            components++;

            Queue<Node> pending = new();
            pending.Enqueue(start);
            visited.Add(start);

            while (pending.Count > 0)
            {
                Node current = pending.Dequeue();

                foreach (Node next in current.Neighbors)
                {
                    if (visited.Add(next)) pending.Enqueue(next);
                }
            }
        }

        return components;
    }

    private static bool IsValidPath(List<Node> path, Node origin, Node target)
    {
        if (path[0] != origin || path[path.Count - 1] != target) return false;

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (!HasNeighbor(path[i], path[i + 1])) return false;
        }

        return true;
    }

    private static float PathCost(List<Node> path)
    {
        float cost = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            cost += path[i].DistanceTo(path[i + 1]);
        }

        return cost;
    }

    // Dijkstra simple (O(V^2)) usado sólo como referencia para comprobar que A* devuelve el costo óptimo.
    private static Dictionary<Node, float> Dijkstra(List<Node> nodes, Node source)
    {
        Dictionary<Node, float> distance = new();
        HashSet<Node> done = new();

        foreach (Node n in nodes) distance[n] = float.PositiveInfinity;
        distance[source] = 0f;

        for (int i = 0; i < nodes.Count; i++)
        {
            Node current = null;
            float best = float.PositiveInfinity;

            foreach (Node n in nodes)
            {
                if (done.Contains(n) || distance[n] >= best) continue;

                best = distance[n];
                current = n;
            }

            if (current == null) break;

            done.Add(current);

            foreach (Node next in current.Neighbors)
            {
                float candidate = distance[current] + current.DistanceTo(next);
                if (candidate < distance[next]) distance[next] = candidate;
            }
        }

        return distance;
    }
}
