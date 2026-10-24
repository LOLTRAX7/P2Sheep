using System.Collections.Generic;
using UnityEngine;

// Adaptado de A* de clase: costo por distancia y heurística euclidiana.
// El filtro de refugios permite compartir el grafo con los lobos.
public static class Pathfinding
{
    // Devuelve los nodos desde startingNode hasta endNode (ambos incluidos), o una lista vacía si no hay camino.
    public static List<Node> AStar(Node startingNode, Node endNode, bool avoidSafeZones = false)
    {
        if (startingNode == null || endNode == null || startingNode.Blocked || endNode.Blocked)
            return new();

        if (avoidSafeZones && (startingNode.IsSafeZone || endNode.IsSafeZone))
            return new();

        // Nodos por explorar, ordenados por f = costo acumulado + heurística.
        PriorityQueue<Node> frontier = new();
        frontier.Enqueue(startingNode, 0f);

        Dictionary<Node, Node> cameFrom = new();
        cameFrom.Add(startingNode, null);

        Dictionary<Node, float> costSoFar = new();
        costSoFar.Add(startingNode, 0f);

        // Nodos ya expandidos por ESTA búsqueda (no es estado del nodo).
        HashSet<Node> closed = new();

        Vector3 endPosition = endNode.transform.position;

        while (frontier.Count != 0)
        {
            Node current = frontier.Dequeue();

            if (current == endNode)
            {
                return ReconstructPath(cameFrom, current);
            }

            // La cola no permite bajar la prioridad de un nodo, así que puede tener copias viejas.
            if (!closed.Add(current)) continue;

            foreach (Node neighbor in current.Neighbors)
            {
                if (neighbor.Blocked || closed.Contains(neighbor)) continue;
                if (avoidSafeZones && neighbor.IsSafeZone) continue;
                if (avoidSafeZones && Zone.CrossesSafeZone(current.transform.position, neighbor.transform.position, 0.45f)) continue;

                float newCost = costSoFar[current] + current.DistanceTo(neighbor);

                if (cameFrom.ContainsKey(neighbor) && newCost >= costSoFar[neighbor]) continue;

                float heuristic = Vector3.Distance(neighbor.transform.position, endPosition);
                frontier.Enqueue(neighbor, newCost + heuristic);
                cameFrom[neighbor] = current;
                costSoFar[neighbor] = newCost;
            }
        }

        return new();
    }

    private static List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        List<Node> path = new();

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    public static float PathCost(List<Node> path)
    {
        float cost = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            cost += path[i].DistanceTo(path[i + 1]);
        }

        return cost;
    }
}
