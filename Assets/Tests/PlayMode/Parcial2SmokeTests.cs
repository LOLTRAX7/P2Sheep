using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Pruebas de humo del proyecto:
// - la escena del Parcial 2 se construye, el grafo queda conectado y los lobos respetan las Zonas seguras;
// - la escena del Parcial 1 sigue cargando y generando sus Boids.
// No prueban "jugabilidad" fina: sirven para detectar excepciones y escenas rotas.
public class Parcial2SmokeTests
{
    [UnityTest]
    public IEnumerator Escena_SeConstruye_ConGrafoConectado()
    {
        SceneManager.LoadScene("Parcial2");
        yield return null;
        yield return new WaitForSeconds(2f);

        HunterAgent hunter = HunterAgent.Current;
        Graph graph = Graph.Current;

        Assert.IsNotNull(hunter, "No se creó el Cazador.");
        Assert.IsNotNull(graph, "No se creó el grafo.");
        Assert.IsTrue(graph.IsGenerated, "El grafo no generó sus conexiones.");
        Assert.GreaterOrEqual(graph.GetAllNodes.Count, 30, "El grafo tiene muy pocos nodos.");

        // Todos los nodos deben tener al menos una conexión.
        foreach (Node node in graph.GetAllNodes)
        {
            Assert.IsNotNull(node, "Hay un nodo nulo en el grafo.");
            Assert.Greater(node.Neighbors.Count, 0, "El nodo " + node.name + " quedó aislado.");
        }

        Assert.IsTrue(EsComponenteConexa(graph, false), "El grafo completo tiene componentes desconectadas.");
        Assert.IsTrue(EsComponenteConexa(graph, true), "El subgrafo sin Zonas seguras tiene componentes desconectadas.");

        BoidAgent[] boids = Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        WolfAgent[] wolves = Object.FindObjectsByType<WolfAgent>(FindObjectsSortMode.None);

        Assert.GreaterOrEqual(boids.Length, 6, "La consigna pide al menos 6 Boids.");
        Assert.GreaterOrEqual(wolves.Length, 4, "La consigna pide al menos 4 lobos.");

        foreach (BoidAgent boid in boids)
        {
            Assert.IsTrue(Bounds.Instance.Contains(boid.transform.position),
                "El Boid " + boid.name + " quedó fuera del área del Coto.");
        }

        foreach (WolfAgent wolf in wolves)
        {
            Assert.Less(wolf.ChaseSpeed, hunter.MoveSpeed,
                "El lobo " + wolf.name + " no puede ser más rápido que el Cazador.");

            foreach (Node node in wolf.PatrolNodes)
            {
                Assert.IsFalse(node.IsSafeZone,
                    "La ruta del lobo " + wolf.name + " usa el nodo seguro " + node.name + ".");
            }

            Assert.IsFalse(Zone.IsInsideSafeZone(wolf.transform.position),
                "El lobo " + wolf.name + " arrancó dentro de una Zona segura.");
        }

        // Deja correr la FSM y la navegación unos segundos más.
        yield return new WaitForSeconds(4f);

        foreach (WolfAgent wolf in wolves)
        {
            Assert.IsFalse(Zone.IsInsideSafeZone(wolf.transform.position),
                "El lobo " + wolf.name + " entró a una Zona segura.");
        }

        Assert.IsNotNull(HunterAgent.Current, "El Cazador desapareció sin respawn.");
    }

    [UnityTest]
    public IEnumerator Parcial1_SigueCargandoYBoidSpawner()
    {
        SceneManager.LoadScene("Lvl1");
        yield return null;
        yield return new WaitForSeconds(1f);

        Assert.IsNotNull(Bounds.Instance, "La escena del Parcial 1 no tiene Bounds.");

        BoidAgent[] boids = Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        Assert.Greater(boids.Length, 0, "El BoidSpawner del Parcial 1 no generó Boids.");

        HunterAgent hunter = Object.FindFirstObjectByType<HunterAgent>();
        Assert.IsNotNull(hunter, "La escena del Parcial 1 no tiene Hunter.");
        Assert.IsTrue(hunter.IsLegacyMode, "El Hunter del Parcial 1 no entró en modo de compatibilidad.");
        Assert.Greater(hunter.LegacyWaypoints.Count, 0, "El Hunter del Parcial 1 perdió sus waypoints.");
    }

    private static bool EsComponenteConexa(Graph graph, bool avoidSafeZones)
    {
        List<Node> nodes = graph.GetAllNodes;
        Node start = null;

        foreach (Node node in nodes)
        {
            if (avoidSafeZones && node.IsSafeZone) continue;
            start = node;
            break;
        }

        if (start == null) return false;

        HashSet<Node> visited = new();
        Queue<Node> queue = new();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();

            foreach (Node neighbor in current.Neighbors)
            {
                if (neighbor == null || visited.Contains(neighbor)) continue;
                if (avoidSafeZones && neighbor.IsSafeZone) continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        foreach (Node node in nodes)
        {
            if (avoidSafeZones && node.IsSafeZone) continue;
            if (!visited.Contains(node)) return false;
        }

        return true;
    }
}
