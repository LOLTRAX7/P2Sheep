using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Casos de la consigna y regresiones encontradas durante la revisión.
public class Parcial2RequirementsTests
{
    [UnitySetUp]
    public IEnumerator LoadLevel()
    {
        Time.timeScale = 1f;
        UnityEngine.Random.InitState(2026);
        SceneManager.LoadScene("Parcial2");
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator ResetTime()
    {
        Time.timeScale = 1f;
        yield return null;
    }

    private static void Set(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, name);
        field.SetValue(target, value);
    }

    private static void HideWolves()
    {
        foreach (WolfAgent wolf in Object.FindObjectsByType<WolfAgent>(FindObjectsSortMode.None))
            wolf.gameObject.SetActive(false);
        Physics.SyncTransforms();
    }

    private static void HideBoids()
    {
        foreach (BoidAgent boid in Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None))
            boid.gameObject.SetActive(false);
        Physics.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator ObjetosDeInteres_SeCreanEnLaPosicionElegidaYRespetanElLimite()
    {
        HideWolves();
        HideBoids();
        HunterAgent hunter = HunterAgent.Current;
        hunter.enabled = false;
        hunter.transform.position = hunter.HuntingZone.Center + Vector3.up;
        for (int i = 0; i < 8; i++) hunter.SpawnInterestPoint();
        InterestPoint[] points = Object.FindObjectsByType<InterestPoint>(FindObjectsSortMode.None);
        Assert.AreEqual(5, points.Length);
        bool spread = false;
        foreach (InterestPoint point in points)
        {
            Assert.IsTrue(hunter.HuntingZone.Contains(point.transform.position));
            Assert.AreEqual(0.35f, point.transform.position.y, 0.001f);
            spread |= point.transform.position.sqrMagnitude > 1f;
        }
        Assert.IsTrue(spread, "Todos los objetos quedaron erróneamente en el origen.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Search_ConservaElIntervaloEntreInterrupciones()
    {
        HideWolves();
        HideBoids();
        HunterAgent hunter = HunterAgent.Current;
        hunter.enabled = false;
        hunter.transform.position = hunter.HuntingZone.Center + Vector3.up;
        Set(hunter, "_interestPointInterval", 0.3f);
        yield return new WaitForSeconds(0.25f); // vacía la muestra anterior de percepción
        var fsm = new FiniteStateMachine<HunterAgent.HunterState>();
        fsm.AddState(HunterAgent.HunterState.Search, new HunterSearchState(hunter));
        fsm.ChangeState(HunterAgent.HunterState.Search);
        float end = Time.time + 0.8f;
        while (Time.time < end)
        {
            fsm.ChangeState(HunterAgent.HunterState.Search);
            fsm.Update();
            yield return null;
        }
        Assert.Greater(Object.FindObjectsByType<InterestPoint>(FindObjectsSortMode.None).Length, 0);
    }

    [UnityTest]
    public IEnumerator SinCamino_ElAgenteNoAvanzaAtravesandoParedes()
    {
        HideWolves();
        HideBoids();
        HunterAgent hunter = HunterAgent.Current;
        hunter.enabled = false;
        Graph graph = Graph.Current;
        foreach (Node node in graph.GetAllNodes) node.ClearNeighbors();
        Vector3 start = hunter.transform.position;
        GraphNavigator navigator = hunter.GetComponent<GraphNavigator>();
        navigator.ResetPath();
        Assert.AreEqual(start, navigator.GetNextPoint(hunter.ExitZone.Center));
        for (int i = 0; i < 30; i++)
        {
            hunter.MoveTo(hunter.ExitZone.Center, hunter.MoveSpeed);
            yield return null;
        }
        Assert.Less(Vector3.Distance(start, hunter.transform.position), 0.01f);
    }

    [UnityTest]
    public IEnumerator Cazador_UsaAStarParaViajarInclusoConVisionDirecta()
    {
        HideWolves();
        HunterAgent hunter = HunterAgent.Current;
        hunter.enabled = false;
        hunter.transform.position = LevelData.ToWorld(19f, 17f) + Vector3.up;
        GraphNavigator navigator = hunter.GetComponent<GraphNavigator>();
        navigator.ResetPath();
        navigator.GetNextPoint(LevelData.ToWorld(27f, 17f));
        Assert.IsTrue(navigator.UsingPathfinding);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Vision_RespetaDistanciaAnguloParedesYRefugios()
    {
        WolfAgent wolf = Object.FindFirstObjectByType<WolfAgent>();
        HunterAgent hunter = HunterAgent.Current;
        hunter.enabled = false;
        wolf.enabled = false;
        wolf.transform.position = LevelData.ToWorld(22f, 17f) + Vector3.up;
        wolf.transform.forward = Vector3.right;
        hunter.transform.position = wolf.transform.position + Vector3.right * 5f;
        Physics.SyncTransforms();
        Assert.IsTrue(wolf.Sensor.IsVisibleNow(hunter.transform));
        hunter.transform.position = wolf.transform.position + Vector3.left * 5f;
        Assert.IsFalse(wolf.Sensor.IsVisibleNow(hunter.transform));
        hunter.transform.position = wolf.transform.position + Vector3.right * (wolf.ViewRadius + 2f);
        Assert.IsFalse(wolf.Sensor.IsVisibleNow(hunter.transform));
        hunter.transform.position = wolf.transform.position + Vector3.right * 5f;
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.layer = GameLayers.Wall;
        wall.transform.position = wolf.transform.position + Vector3.right * 2.5f;
        wall.transform.localScale = new Vector3(0.5f, 5f, 5f);
        Physics.SyncTransforms();
        Assert.IsFalse(wolf.Sensor.IsVisibleNow(hunter.transform));
        Object.Destroy(wall);
        hunter.transform.position = Zone.SafeZones[0].Center + Vector3.up;
        Assert.IsFalse(wolf.Sensor.IsVisibleNow(hunter.transform));
        yield return null;
    }

    [UnityTest]
    public IEnumerator Alerta_ComparteUnaPosicionQueNoSeActualizaSinPercepcion()
    {
        HideBoids();
        HunterAgent hunter = HunterAgent.Current;
        hunter.enabled = false;
        WolfAgent[] wolves = Object.FindObjectsByType<WolfAgent>(FindObjectsSortMode.None);
        foreach (WolfAgent wolf in wolves) wolf.enabled = false;
        WolfAgent source = wolves[0];
        WolfAgent receiver = wolves[1];
        source.transform.position = LevelData.ToWorld(22f, 17f) + Vector3.up;
        source.transform.forward = Vector3.right;
        hunter.transform.position = source.transform.position + Vector3.right * 3f;
        receiver.transform.position = source.transform.position + Vector3.left * 4f;
        receiver.transform.forward = Vector3.left;
        // Un agente deshabilitado se desuscribe de las alertas. Se mantiene
        // suscrito, pero se pausa su FSM para observar el mensaje sin consumirlo.
        Set(receiver, "_configured", false);
        receiver.enabled = true;
        Physics.SyncTransforms();
        yield return new WaitForSeconds(0.25f);
        source.NotifyTargetSpotted();
        Assert.IsTrue(receiver.HasPendingAlert);
        receiver.ConsumePendingAlert();
        Vector3 known = receiver.LastKnownPosition;
        hunter.transform.position += Vector3.forward * 4f;
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(known, receiver.LastKnownPosition);
        Assert.AreNotEqual(hunter.transform.position, receiver.LastKnownPosition);
    }

    [UnityTest]
    public IEnumerator Refugio_SeEligePorCostoAStarYLobosNoPuedenCruzar()
    {
        HideWolves();
        HunterAgent hunter = HunterAgent.Current;
        hunter.enabled = false;
        hunter.transform.position = LevelData.ToWorld(19f, 17f) + Vector3.up;
        Zone chosen = hunter.ChooseNearestSafeZone();
        Assert.IsNotNull(chosen);
        float chosenCost = Graph.Current.GetPathCost(hunter.transform.position, chosen.Center);
        foreach (Zone zone in hunter.SafeZones)
            Assert.LessOrEqual(chosenCost, Graph.Current.GetPathCost(hunter.transform.position, zone.Center) + 0.001f);
        Vector3 from = chosen.Center - Vector3.forward * (chosen.Size.y + 1f);
        Vector3 to = chosen.Center + Vector3.forward * (chosen.Size.y + 1f);
        Assert.IsTrue(Zone.CrossesSafeZone(from, to));
        Assert.IsFalse(Graph.Current.CanTravel(from, to, true));
        yield return null;
    }

    [UnityTest]
    public IEnumerator Gather_CanceladoPorAmenaza_NoSumaYEsperaEnRefugio()
    {
        HideWolves();
        HunterAgent hunter = HunterAgent.Current;
        hunter.transform.position = hunter.HuntingZone.Center + Vector3.up;
        BoidAgent[] boids = Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        BoidAgent target = boids[0];
        target.transform.position = hunter.transform.position + Vector3.right * 0.5f;
        target.TakeDamage(1000f);
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        Assert.AreEqual(HunterAgent.HunterState.Gather, hunter.CurrentState);
        WolfAgent wolf = Object.FindFirstObjectByType<WolfAgent>(FindObjectsInactive.Include);
        wolf.enabled = false;
        wolf.transform.position = hunter.transform.position + Vector3.forward * 5f;
        wolf.gameObject.SetActive(true);
        Physics.SyncTransforms();
        yield return new WaitForSeconds(0.35f);
        Assert.AreEqual(HunterAgent.HunterState.Flee, hunter.CurrentState);
        Assert.AreEqual(0, hunter.Gathers);
        Assert.IsTrue(target.IsPendingGather);
        Zone safe = hunter.ChosenSafeZone;
        Assert.IsNotNull(safe);
        hunter.transform.position = safe.Center + Vector3.up;
        yield return null;
        Vector3 atSafe = hunter.transform.position;
        yield return new WaitForSeconds(hunter.SafeZoneWaitTime * 0.5f);
        Assert.AreEqual(HunterAgent.HunterState.Flee, hunter.CurrentState);
        Assert.Less(Vector3.Distance(atSafe, hunter.transform.position), 0.01f);
        yield return new WaitForSeconds(hunter.SafeZoneWaitTime * 0.5f + 0.1f);
        Assert.AreNotEqual(HunterAgent.HunterState.Flee, hunter.CurrentState);
    }

    [UnityTest]
    public IEnumerator ObjetivoDesapareceDuranteGather_NoSuma()
    {
        HideWolves();
        HunterAgent hunter = HunterAgent.Current;
        hunter.transform.position = hunter.HuntingZone.Center + Vector3.up;
        BoidAgent target = Object.FindFirstObjectByType<BoidAgent>();
        target.transform.position = hunter.transform.position + Vector3.right * 0.5f;
        target.TakeDamage(1000f);
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        Assert.AreEqual(HunterAgent.HunterState.Gather, hunter.CurrentState);
        target.gameObject.SetActive(false);
        yield return null;
        yield return null;
        Assert.AreEqual(0, hunter.Gathers);
        Assert.AreNotEqual(HunterAgent.HunterState.Gather, hunter.CurrentState);
    }

    [UnityTest]
    public IEnumerator Boids_SalenDeSuperposicionYSeMantienenDentroDelCoto()
    {
        HideWolves();
        HunterAgent.Current.gameObject.SetActive(false);
        BoidAgent[] boids = Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        for (int i = 2; i < boids.Length; i++) boids[i].gameObject.SetActive(false);
        boids[0].transform.position = Bounds.Instance.Center + Vector3.up * 0.4f;
        boids[1].transform.position = boids[0].transform.position;
        Physics.SyncTransforms();
        yield return new WaitForSeconds(2f);
        Assert.Greater(Vector3.Distance(boids[0].transform.position, boids[1].transform.position), 0.7f);
        for (int i = 0; i < 90; i++)
        {
            Assert.IsTrue(Bounds.Instance.Contains(boids[0].transform.position));
            Assert.IsTrue(Bounds.Instance.Contains(boids[1].transform.position));
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator CicloAutonomo_CazaRecolectaSaleYReinicia()
    {
        HideWolves();
        HunterAgent hunter = HunterAgent.Current;
        Time.timeScale = 12f;
        bool attacked = false, gathered = false, exited = false, completed = false;
        float deadline = Time.time + 180f;
        while (Time.time < deadline)
        {
            attacked |= hunter.CurrentState == HunterAgent.HunterState.Attack;
            gathered |= hunter.Gathers > 0;
            exited |= hunter.CurrentState == HunterAgent.HunterState.Exit;
            if (exited && hunter.Gathers == 0 && hunter.InitialZone.Contains(hunter.transform.position))
            {
                completed = true;
                break;
            }
            Assert.IsFalse(Physics.CheckSphere(new Vector3(hunter.transform.position.x, 0.5f, hunter.transform.position.z),
                0.35f, GameLayers.WallMask), "El Cazador entró a una pared.");
            yield return null;
        }
        Assert.IsTrue(attacked, "No atacó.");
        Assert.IsTrue(gathered, "No recolectó.");
        Assert.IsTrue(exited, "No llegó a Exit.");
        Assert.IsTrue(completed, "No completó el ciclo autónomo en 180 segundos simulados.");
    }

    [UnityTest]
    public IEnumerator Attack_RespetaTBA_YPerderElObjetivoNoLoReinicia()
    {
        HideWolves();
        HunterAgent hunter = HunterAgent.Current;
        BoidAgent[] boids = Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        BoidAgent target = boids[0];
        for (int i = 1; i < boids.Length; i++) boids[i].gameObject.SetActive(false);
        Set(target, "maxSpeed", 0f);
        Set(target, "_velocity", Vector3.zero);
        hunter.transform.position = new Vector3(-10f, hunter.transform.position.y, 0f);
        target.transform.position = new Vector3(3f, target.transform.position.y, 0f);
        Physics.SyncTransforms();
        yield return null;
        Assert.AreEqual(HunterAgent.HunterState.Attack, hunter.CurrentState);
        target.transform.position = new Vector3(100f, target.transform.position.y, 100f);
        Physics.SyncTransforms();
        yield return null;
        Assert.AreEqual(HunterAgent.HunterState.Search, hunter.CurrentState);
        Assert.IsTrue(hunter.IsAttackReady, "Perder un objetivo no debe reiniciar TBA.");
        target.transform.position = hunter.transform.position + Vector3.right * 4f;
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        Assert.Less(target.HealthNormalized, 1f);
        Assert.IsFalse(hunter.IsAttackReady);
        float health = target.HealthNormalized;
        yield return new WaitForSeconds(hunter.TBA * 0.5f);
        Assert.AreEqual(health, target.HealthNormalized, "Atacó antes de terminar TBA.");
    }

    [UnityTest]
    public IEnumerator Dead_InterrumpeExitYReiniciaDespuesDelTiempoConfigurado()
    {
        WolfAgent wolf = Object.FindFirstObjectByType<WolfAgent>();
        HideWolves();
        HunterAgent hunter = HunterAgent.Current;
        for (int i = 0; i < hunter.RequiredGathers; i++) hunter.AddGather();
        yield return null;
        Assert.AreEqual(HunterAgent.HunterState.Exit, hunter.CurrentState);
        wolf.transform.position = hunter.transform.position + Vector3.right * 0.5f;
        Assert.IsTrue(wolf.TryCaptureHunter());
        yield return null;
        Assert.IsTrue(hunter.IsDead);
        Assert.IsFalse(hunter.GetComponent<Collider>().enabled);
        Vector3 position = hunter.transform.position;
        yield return new WaitForSeconds(hunter.RespawnTime * 0.5f);
        Assert.IsTrue(hunter.IsDead);
        Assert.AreEqual(position, hunter.transform.position);
        yield return new WaitForSeconds(hunter.RespawnTime * 0.5f + 0.05f);
        Assert.IsFalse(hunter.IsDead);
        Assert.AreEqual(0, hunter.Gathers);
        Assert.IsTrue(hunter.InitialZone.Contains(hunter.transform.position));
    }

    [UnityTest]
    public IEnumerator AStar_CostoMinimoParaTodosLosParesConYSinRefugios()
    {
        Graph graph = Graph.Current;
        int checkedPaths = 0;
        for (int mode = 0; mode < 2; mode++)
        {
            bool avoidSafe = mode == 1;
            foreach (Node start in graph.GetAllNodes)
            {
                if (avoidSafe && start.IsSafeZone) continue;
                var distances = new Dictionary<Node, float>();
                var pending = new List<Node>();
                foreach (Node node in graph.GetAllNodes)
                {
                    if (avoidSafe && node.IsSafeZone) continue;
                    distances[node] = node == start ? 0f : float.PositiveInfinity;
                    pending.Add(node);
                }
                // Dijkstra de referencia con selección lineal, independiente de
                // la cola de prioridad y de la heurística que usa A*.
                while (pending.Count > 0)
                {
                    Node current = pending[0];
                    foreach (Node node in pending)
                        if (distances[node] < distances[current]) current = node;
                    pending.Remove(current);
                    foreach (Node next in current.Neighbors)
                    {
                        if (!distances.ContainsKey(next)) continue;
                        if (avoidSafe && Zone.CrossesSafeZone(current.transform.position, next.transform.position, 0.45f)) continue;
                        distances[next] = Mathf.Min(distances[next], distances[current] + current.DistanceTo(next));
                    }
                }
                foreach (var pair in distances)
                {
                    List<Node> path = Pathfinding.AStar(start, pair.Key, avoidSafe);
                    Assert.IsNotEmpty(path, start.name + " -> " + pair.Key.name);
                    Assert.AreEqual(pair.Value, Pathfinding.PathCost(path), 0.01f);
                    foreach (Node node in path)
                        Assert.IsFalse(avoidSafe && node.IsSafeZone);
                    checkedPaths++;
                }
            }
        }
        Debug.Log("[Revision] A*: " + checkedPaths + " caminos con costo mínimo verificado.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SimulacionConLobos_PercibeHuyeYRespetaParedesYRefugios()
    {
        HunterAgent hunter = HunterAgent.Current;
        WolfAgent[] wolves = Object.FindObjectsByType<WolfAgent>(FindObjectsSortMode.None);
        Time.timeScale = 8f;
        bool fled = false, chased = false, exited = false;
        int maxGathers = 0;
        float deadline = Time.time + 360f;
        float nextReport = Time.time + 30f;
        bool screenshot = false;
        while (Time.time < deadline)
        {
            fled |= hunter.CurrentState == HunterAgent.HunterState.Flee;
            exited |= hunter.CurrentState == HunterAgent.HunterState.Exit;
            maxGathers = Mathf.Max(maxGathers, hunter.Gathers);
            if (Time.time >= nextReport)
            {
                nextReport += 30f;
                Debug.Log("[Revision] Simulación con lobos: " + hunter.CurrentState + " en " + hunter.transform.position
                    + ", recolecciones " + hunter.Gathers + ", máximo " + maxGathers + ", Exit " + exited);
                if (!screenshot && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    Camera camera = Camera.main;
                    RenderTexture previous = camera.targetTexture;
                    RenderTexture previousActive = RenderTexture.active;
                    RenderTexture frame = new(1600, 900, 24);
                    Texture2D image = new(1600, 900, TextureFormat.RGB24, false);
                    try
                    {
                        camera.targetTexture = frame;
                        camera.Render();
                        RenderTexture.active = frame;
                        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                        image.Apply();
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,
                            "../Revision/Resultados/Simulacion.png"), image.EncodeToPNG());
                    }
                    finally
                    {
                        camera.targetTexture = previous;
                        RenderTexture.active = previousActive;
                        Object.Destroy(frame);
                        Object.Destroy(image);
                    }
                    screenshot = true;
                }
            }
            foreach (WolfAgent wolf in wolves)
            {
                chased |= wolf.CurrentState == WolfAgent.WolfState.Chase;
                Assert.IsFalse(Zone.IsInsideSafeZone(wolf.transform.position), wolf.name);
                Assert.IsFalse(Physics.CheckSphere(new Vector3(wolf.transform.position.x, 0.5f, wolf.transform.position.z),
                    0.35f, GameLayers.WallMask), wolf.name + " atravesó una pared.");
            }
            yield return null;
        }
        Assert.IsTrue(fled, "No se observó Flee en 360 segundos simulados.");
        Assert.IsTrue(chased, "No se observó Chase en 360 segundos simulados.");
        Assert.Greater(maxGathers, 0, "El Cazador no logró recolectar con los lobos presentes.");
        Assert.IsTrue(exited, "El Cazador no completó las recolecciones con lobos presentes.");
    }
}
