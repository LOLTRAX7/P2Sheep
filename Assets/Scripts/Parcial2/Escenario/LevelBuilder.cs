using System.Collections.Generic;
using UnityEngine;

// Genera el escenario del Parcial 2 al darle Play.
//
// No contiene IA: sólo construye el mapa del Ejemplo 1 (anillo), las zonas, el grafo compartido,
// el área de los Boids, el Cazador, los Boids y los lobos. Así la escena que se entrega es chica y
// todo el nivel se arma de forma reproducible.
//
// Se ejecuta antes que el resto de los scripts ([DefaultExecutionOrder]) para que:
//   - las Zonas ya existan cuando el Cazador y los lobos hacen Start;
//   - el grafo ya tenga sus conexiones antes de que los agentes empiecen a navegar.
[DefaultExecutionOrder(-500)]
public class LevelBuilder : MonoBehaviour
{
    [Header("Generación")]
    [SerializeField] private bool _buildOnAwake = true;
    [SerializeField] private int _boidCount = 8;
    [SerializeField] private bool _spawnWolves = true;

    [Header("Medidas")]
    [SerializeField] private float _wallHeight = 3f;
    [SerializeField] private float _wallThickness = 2f;

    [Header("Escena")]
    [SerializeField] private bool _setupCamera = true;
    [SerializeField] private bool _createHud = true;
    [SerializeField] private bool _runGraphSelfTest = true;

    [Header("Prefabs (opcional)")]
    [Tooltip("Si se asignan, LevelBuilder los instancia en lugar de crear primitivas por código. Si no, se usa el comportamiento actual.")]
    [SerializeField] private GameObject _hunterPrefab;
    [SerializeField] private GameObject _wolfPrefab;
    [SerializeField] private GameObject _boidPrefab;
    [SerializeField] private GameObject _interestPointPrefab;

    private const float HunterHeight = 0.95f;
    private const float WolfHeight = 0.85f;
    private const float BoidHeight = 0.4f;

    private readonly Dictionary<string, Node> _nodesById = new();
    private readonly List<Zone> _safeZones = new();
    private readonly Dictionary<Color, Material> _materials = new();

    private GameObject _root;
    private Graph _graph;
    private Bounds _bounds;
    private HunterAgent _hunter;
    private Zone _initialZone;
    private Zone _huntingZone;
    private Zone _exitZone;

    public Graph Graph => _graph;
    public HunterAgent Hunter => _hunter;
    public Zone HuntingZone => _huntingZone;
    public Zone InitialZone => _initialZone;
    public Zone ExitZone => _exitZone;

    private void Awake()
    {
        if (_buildOnAwake) Build();
    }

    [ContextMenu("Construir nivel")]
    public void Build()
    {
        if (_root != null) Destroy(_root);

        _nodesById.Clear();
        _safeZones.Clear();

        _root = new GameObject("Nivel_Generado");

        BuildFloorAndZones();
        BuildWalls();
        BuildGraph();
        BuildBounds();
        BuildHunter();
        BuildBoids();

        if (_spawnWolves) BuildWolves();

        if (_setupCamera) SetupCamera();

        if (_createHud) CreateHud();

        if (_runGraphSelfTest)
        {
            GameObject selfTest = new("AutoTest_Grafo");
            GraphSelfTest test = selfTest.AddComponent<GraphSelfTest>();
            test.Configure(true);
        }

        FeedbackLog.Add("Nivel generado: 5 zonas, 5 lobos y " + _boidCount + " Boids.", new Color(0.4f, 1f, 0.5f));
    }

    // ---------- Mapa, zonas y paredes ----------

    private void BuildFloorAndZones()
    {
        // Piso base de todo el mapa.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Piso";
        floor.transform.SetParent(_root.transform, false);
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        floor.transform.localScale = new Vector3(LevelData.Cols * LevelData.CellSize, 0.2f, LevelData.Rows * LevelData.CellSize);
        floor.GetComponent<Renderer>().sharedMaterial = MaterialForUnlit(new Color(0.12f, 0.12f, 0.14f));

        GameObject zonesRoot = new("Zonas");
        zonesRoot.transform.SetParent(_root.transform, false);

        foreach (LevelData.ZoneDef def in LevelData.Zones)
        {
            Vector3 center = def.WorldCenter;
            Vector2 size = def.WorldSize;

            GameObject zoneObject = new(def.Name);
            zoneObject.transform.SetParent(zonesRoot.transform, false);
            zoneObject.transform.position = center;

            Zone zone = zoneObject.AddComponent<Zone>();
            zone.Configure(def.Type, center, size);

            switch (def.Type)
            {
                case ZoneType.Initial:
                    _initialZone = zone;
                    break;
                case ZoneType.Hunting:
                    _huntingZone = zone;
                    break;
                case ZoneType.Exit:
                    _exitZone = zone;
                    break;
                case ZoneType.Safe:
                    _safeZones.Add(zone);
                    break;
            }

            // Piso coloreado de la zona (sin collider: es sólo visual).
            GameObject zoneFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zoneFloor.name = "Piso_" + def.Name;
            zoneFloor.transform.SetParent(zoneObject.transform, false);
            zoneFloor.transform.position = new Vector3(center.x, 0.025f, center.z);
            zoneFloor.transform.localScale = new Vector3(size.x, 0.05f, size.y);
            zoneFloor.GetComponent<Renderer>().sharedMaterial = MaterialForUnlit(Zone.ColorFor(def.Type) * 0.55f + Color.black * 0.45f);
            Destroy(zoneFloor.GetComponent<Collider>());

            CreateZoneBorder(zone, size, Zone.ColorFor(def.Type));
        }
    }

    private void CreateZoneBorder(Zone zone, Vector2 size, Color color)
    {
        GameObject borderObject = new("Borde_" + zone.name);
        borderObject.transform.SetParent(zone.transform, false);

        LineRenderer line = borderObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 4;
        line.widthMultiplier = 0.18f;
        line.sharedMaterial = FeedbackMaterials.CreateUnlit(color);
        line.startColor = color;
        line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        Vector3 center = zone.Center;
        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;

        line.SetPosition(0, new Vector3(center.x - hx, 0.08f, center.z - hz));
        line.SetPosition(1, new Vector3(center.x - hx, 0.08f, center.z + hz));
        line.SetPosition(2, new Vector3(center.x + hx, 0.08f, center.z + hz));
        line.SetPosition(3, new Vector3(center.x + hx, 0.08f, center.z - hz));
    }

    private void BuildWalls()
    {
        Transform wallsRoot = new GameObject("Paredes").transform;
        wallsRoot.SetParent(_root.transform, false);

        Material wallMaterial = MaterialForLit(new Color(0.22f, 0.24f, 0.30f));

        for (int row = 0; row < LevelData.Rows; row++)
        {
            for (int col = 0; col < LevelData.Cols; col++)
            {
                if (!LevelData.IsWall(col, row)) continue;
                if (!TouchesFloor(col, row)) continue;

                Vector3 position = LevelData.ToWorld(col, row);
                position.y = _wallHeight * 0.5f;

                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Pared_" + col + "_" + row;
                wall.transform.SetParent(wallsRoot, false);
                wall.transform.position = position;
                wall.transform.localScale = new Vector3(_wallThickness, _wallHeight, _wallThickness);
                wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

                GameLayers.SetLayer(wall, GameLayers.Wall);
            }
        }
    }

    private static bool TouchesFloor(int col, int row)
    {
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dz == 0) continue;
                if (!LevelData.IsWall(col + dx, row + dz)) return true;
            }
        }

        return false;
    }

    // ---------- Grafo ----------

    private void BuildGraph()
    {
        GameObject graphObject = new("Grafo");
        graphObject.transform.SetParent(_root.transform, false);

        Transform nodesRoot = new GameObject("Nodos").transform;
        nodesRoot.SetParent(graphObject.transform, false);

        // Primero se crean todos los nodos para que Graph.Awake los encuentre al agregarse.
        foreach (LevelData.NodeDef def in LevelData.Nodes)
        {
            Vector3 position = LevelData.ToWorld(def.Col, def.Row);

            GameObject nodeObject = new(def.Id);
            nodeObject.transform.SetParent(nodesRoot, false);
            nodeObject.transform.position = position;

            Node node = nodeObject.AddComponent<Node>();

            bool safe = LevelData.IsSafeZonePosition(position);
            node.SetSafeZone(safe);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Marcador";
            marker.transform.SetParent(nodeObject.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.3f;
            Destroy(marker.GetComponent<Collider>());

            Color color = safe ? Zone.ColorFor(ZoneType.Safe) : NodeColorAt(position);
            marker.GetComponent<Renderer>().sharedMaterial = MaterialForUnlit(color);
            node.SetMarkerRenderer(marker.GetComponent<Renderer>());
            node.SetDefaultColor(color);

            _nodesById[def.Id] = node;
        }

        _graph = graphObject.AddComponent<Graph>();
        _graph.Configure(GameLayers.WallMask);
        _graph.GenerateConnections();
    }

    private Color NodeColorAt(Vector3 position)
    {
        if (_huntingZone != null && _huntingZone.Contains(position)) return Zone.ColorFor(ZoneType.Hunting);
        if (_initialZone != null && _initialZone.Contains(position)) return Zone.ColorFor(ZoneType.Initial);
        if (_exitZone != null && _exitZone.Contains(position)) return Zone.ColorFor(ZoneType.Exit);

        return new Color(1f, 0.35f, 0.75f);
    }

    // ---------- Área de los Boids y agentes ----------

    private void BuildBounds()
    {
        GameObject boundsObject = new("Bounds");
        boundsObject.transform.SetParent(_root.transform, false);

        _bounds = boundsObject.AddComponent<Bounds>();

        Vector3 center = _huntingZone.Center;
        Vector2 size = _huntingZone.Size - Vector2.one * 2f;

        _bounds.Configure(center, size.x, size.y);
    }

    private void BuildHunter()
    {
        Vector3 start = _nodesById[LevelData.HunterStartNode].transform.position.ToXZ(0f) + Vector3.up * HunterHeight;

        GameObject hunterObject;

        if (_hunterPrefab != null)
        {
            hunterObject = Instantiate(_hunterPrefab, start, Quaternion.identity);
            hunterObject.name = "Cazador";
        }
        else
        {
            hunterObject = CreateAgentBody("Cazador", PrimitiveType.Capsule, start, new Color(0.9f, 0.9f, 0.95f), HunterHeight);
            Renderer renderer = hunterObject.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.sharedMaterial = MaterialForLit(new Color(0.9f, 0.9f, 0.95f));
        }

        _hunter = hunterObject.GetComponent<HunterAgent>();
        if (_hunter == null) _hunter = hunterObject.AddComponent<HunterAgent>();

        _hunter.Configure(
            start,
            _initialZone,
            _huntingZone,
            _safeZones,
            _exitZone,
            _nodesById[LevelData.HunterExitNode].transform.position);
    }

    private void BuildBoids()
    {
        Transform boidsRoot = new GameObject("Boids").transform;
        boidsRoot.SetParent(_root.transform, false);

        for (int i = 0; i < _boidCount; i++)
        {
            Vector3 position = _bounds.GetRandomPointInside();
            position.y = BoidHeight;

            GameObject boidObject;

            if (_boidPrefab != null)
            {
                boidObject = Instantiate(_boidPrefab, position, Quaternion.identity);
                boidObject.name = "Boid_" + (i + 1);
                boidObject.transform.SetParent(boidsRoot, false);
            }
            else
            {
                boidObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                boidObject.name = "Boid_" + (i + 1);
                boidObject.transform.SetParent(boidsRoot, false);
                boidObject.transform.position = position;
                boidObject.transform.localScale = Vector3.one * 0.7f;

                Renderer renderer = boidObject.GetComponent<Renderer>();
                Color color = Color.Lerp(new Color(0.55f, 0.75f, 1f), new Color(0.9f, 0.95f, 1f), i / (float)Mathf.Max(1, _boidCount - 1));
                renderer.sharedMaterial = MaterialForLit(color);

                Collider collider = boidObject.GetComponent<Collider>();
                if (collider != null) collider.isTrigger = true;

                boidObject.AddComponent<BoidAgent>();
            }
        }
    }

    private void BuildWolves()
    {
        foreach (LevelData.RouteDef route in LevelData.WolfRoutes)
        {
            List<Node> routeNodes = new();

            foreach (string nodeId in route.NodeIds)
            {
                if (_nodesById.TryGetValue(nodeId, out Node node) && !node.IsSafeZone) routeNodes.Add(node);
            }

            if (routeNodes.Count == 0)
            {
                Debug.LogWarning("[LevelBuilder] La ruta " + route.Owner + " no tiene nodos válidos.");
                continue;
            }

            Vector3 start = routeNodes[0].transform.position.ToXZ(0f) + Vector3.up * WolfHeight;

            GameObject wolfObject;
            WolfAgent wolf;

            if (_wolfPrefab != null)
            {
                wolfObject = Instantiate(_wolfPrefab, start, Quaternion.identity);
                wolfObject.name = route.Owner;
                wolf = wolfObject.GetComponent<WolfAgent>();
                if (wolf == null) wolf = wolfObject.AddComponent<WolfAgent>();
            }
            else
            {
                wolfObject = CreateAgentBody(route.Owner, PrimitiveType.Capsule, start, new Color(0.65f, 0.65f, 0.7f), WolfHeight);
                Renderer renderer = wolfObject.GetComponentInChildren<Renderer>();
                if (renderer != null) renderer.sharedMaterial = MaterialForLit(new Color(0.65f, 0.65f, 0.7f));

                wolfObject.transform.localScale = new Vector3(0.7f, 0.85f, 1.1f);
                wolf = wolfObject.AddComponent<WolfAgent>();
            }

            wolfObject.transform.SetParent(_root.transform, false);
            wolf.Configure(_graph, routeNodes, route.PingPong);
        }
    }

    private GameObject CreateAgentBody(string objectName, PrimitiveType primitive, Vector3 position, Color color, float height)
    {
        GameObject body = GameObject.CreatePrimitive(primitive);
        body.name = objectName;
        body.transform.position = position;
        body.transform.localScale = new Vector3(0.8f, height, 0.8f);

        Renderer renderer = body.GetComponentInChildren<Renderer>();
        if (renderer != null) renderer.sharedMaterial = MaterialForLit(color);

        Collider collider = body.GetComponentInChildren<Collider>();
        if (collider != null) collider.isTrigger = true;

        return body;
    }

    // ---------- Cámara, luz y HUD ----------

    private void SetupCamera()
    {
        Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();

        if (camera == null)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 70f, 0f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 200f;

        float aspect = camera.aspect > 0.01f ? camera.aspect : 16f / 9f;
        float mapWidth = LevelData.Cols * LevelData.CellSize;
        float mapDepth = LevelData.Rows * LevelData.CellSize;
        camera.orthographicSize = Mathf.Max(mapDepth * 0.5f, mapWidth * 0.5f / aspect) + 3f;

        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.2f;
        }
    }

    private void CreateHud()
    {
        GameObject hud = new("HUD");
        hud.AddComponent<FeedbackLogHUD>();
        hud.AddComponent<HunterFeedback>();
        hud.AddComponent<ZoneLegend>();
    }

    // ---------- Materiales ----------

    private Material MaterialForUnlit(Color color)
    {
        if (_materials.TryGetValue(color, out Material material)) return material;

        material = FeedbackMaterials.CreateUnlit(color);
        _materials[color] = material;
        return material;
    }

    private Material MaterialForLit(Color color)
    {
        if (_materials.TryGetValue(color, out Material material)) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);

        _materials[color] = material;
        return material;
    }

    private void OnDestroy()
    {
        if (_root != null) Destroy(_root);
    }
}

// Pequeña ayuda para no repetir la asignación de Y en las posiciones generadas.
public static class Vector3Extensions
{
    public static Vector3 ToXZ(this Vector3 value, float y)
    {
        return new Vector3(value.x, y, value.z);
    }
}
