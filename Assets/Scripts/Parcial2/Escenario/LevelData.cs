using UnityEngine;

// Datos del escenario del Parcial 2 (mapa "Variante A - anillo" de la consigna).
// No tiene lógica de IA: sólo describe el nivel, sus zonas, los nodos del grafo y las rutas de patrulla.
//
//   '#' = pared / macizo      '.' = piso transitable
//
// Cada celda mide CellSize x CellSize. La fila 0 es el norte (z+) y la columna 0 el oeste (x-).
// El centro del Coto de caza coincide con el origen (0,0,0), igual que el objeto Bounds del Parcial 1.
public static class LevelData
{
    public const float CellSize = 2f;

    private const float CenterCol = 22.5f;
    private const float CenterRow = 16.5f;

    // 46 columnas x 34 filas.
    // Anillo exterior, Coto de caza central, dos habitaciones de patrulla (norte y sur),
    // Zona inicial (oeste), Salida (este) y dos Zonas seguras en callejones sin salida.
    public static readonly string[] Map =
    {
        "##############################################",
        "##################..........##################",
        "##################..........##################",
        "##################..........##################",
        "##################..........##################",
        "######################..######################",
        "######..................................######",
        "######..................................######",
        "######..................................######",
        "######..................................######",
        "######..................................######",
        "######.......#########..#########.......######",
        "######.......#########..######..#.......######",
        "######.......................#..........######",
        "######.......................#..........######",
        "#....#.......####............#..#.......#....#",
        "#....#.......#..#............####.......#....#",
        "#............#..#............................#",
        "#...............#............................#",
        "#....#..........#............####.......#....#",
        "#....#.......#..#............####.......#....#",
        "######.......#########..#########.......######",
        "######.......#########..#########.......######",
        "######..................................######",
        "######..................................######",
        "######..................................######",
        "######..................................######",
        "######..................................######",
        "######################..######################",
        "##################..........##################",
        "##################..........##################",
        "##################..........##################",
        "##################..........##################",
        "##############################################",
    };

    public readonly struct NodeDef
    {
        public readonly string Id;
        public readonly float Col;
        public readonly float Row;

        public NodeDef(string id, float col, float row)
        {
            Id = id;
            Col = col;
            Row = row;
        }
    }

    public readonly struct RouteDef
    {
        public readonly string Owner;
        public readonly string[] NodeIds;
        public readonly bool PingPong;

        public RouteDef(string owner, string[] nodeIds, bool pingPong)
        {
            Owner = owner;
            NodeIds = nodeIds;
            PingPong = pingPong;
        }
    }

    public readonly struct ZoneDef
    {
        public readonly string Name;
        public readonly ZoneType Type;
        public readonly int Col0;
        public readonly int Col1;
        public readonly int Row0;
        public readonly int Row1;

        public ZoneDef(string name, ZoneType type, int col0, int col1, int row0, int row1)
        {
            Name = name;
            Type = type;
            Col0 = col0;
            Col1 = col1;
            Row0 = row0;
            Row1 = row1;
        }

        public bool Contains(float col, float row)
        {
            return col >= Col0 - 0.5f && col <= Col1 + 0.5f && row >= Row0 - 0.5f && row <= Row1 + 0.5f;
        }

        public Vector3 WorldCenter => ToWorld((Col0 + Col1) * 0.5f, (Row0 + Row1) * 0.5f);

        public Vector2 WorldSize => new((Col1 - Col0 + 1f) * CellSize, (Row1 - Row0 + 1f) * CellSize);
    }

    // Zonas obligatorias de la consigna. Los rectángulos son inclusivos en celdas.
    public static readonly ZoneDef[] Zones =
    {
        new("Zona_Inicial", ZoneType.Initial, 1, 4, 15, 20),
        new("Coto_De_Caza", ZoneType.Hunting, 17, 28, 13, 20),
        new("Zona_Segura_A", ZoneType.Safe, 14, 15, 16, 20),
        new("Zona_Segura_B", ZoneType.Safe, 30, 31, 12, 15),
        new("Salida", ZoneType.Exit, 41, 44, 15, 20),
        // Habitaciones de patrulla: se pintan distinto para que el recorrido de los lobos se entienda.
        new("Habitacion_Norte", ZoneType.Patrol, 18, 27, 1, 4),
        new("Habitacion_Sur", ZoneType.Patrol, 18, 27, 29, 32),
    };

    // Nodos del grafo. Están ubicados en el centro de pasillos, habitaciones y puertas.
    // Las conexiones las genera Graph con SphereCast, así que ninguna arista atraviesa paredes.
    public static readonly NodeDef[] Nodes =
    {
        // Zona inicial y Salida (zonas aisladas con una sola puerta).
        new("INIT_1", 2.5f, 18f),
        new("INIT_DOOR", 5f, 18f),
        new("EXIT_1", 42.5f, 18f),
        new("EXIT_DOOR", 40f, 18f),

        // Anillo exterior: oeste.
        new("W_S", 8f, 22f),
        new("W_M", 8f, 18f),
        new("W_N", 8f, 12f),

        // Anillo exterior: norte.
        new("N_W1", 8f, 8f),
        new("N_W2", 12f, 8f),
        new("N_W3", 17f, 8f),
        new("N_TOP", 22.5f, 8f),
        new("N_E1", 27f, 8f),
        new("N_E2", 32f, 8f),
        new("N_E3", 37f, 8f),

        // Anillo exterior: este.
        new("E_N", 37f, 12f),
        new("E_M", 37f, 18f),
        new("E_S", 37f, 22f),
        new("E_SE", 37f, 25f),

        // Anillo exterior: sur.
        new("S_E1", 32f, 25f),
        new("S_E2", 27f, 25f),
        new("S_BOT", 22.5f, 25f),
        new("S_W1", 17f, 25f),
        new("S_W2", 12f, 25f),
        new("S_W3", 8f, 25f),

        // Habitaciones de patrulla (norte y sur).
        new("TOP_1", 22.5f, 3f),
        new("TOP_2", 19f, 3f),
        new("TOP_3", 26f, 3f),
        new("BOT_1", 22.5f, 31f),
        new("BOT_2", 19f, 31f),
        new("BOT_3", 26f, 31f),

        // Coto de caza: grilla interna para que el Cazador y los lobos puedan recorrerlo con A*.
        new("HUNT_NW", 19f, 15f),
        new("HUNT_N", 22.5f, 15f),
        new("HUNT_NE", 27f, 15f),
        new("HUNT_W", 19f, 17f),
        new("HUNT_C", 22.5f, 17f),
        new("HUNT_E", 27f, 17f),
        new("HUNT_SW", 19f, 19f),
        new("HUNT_S", 22.5f, 19f),
        new("HUNT_SE", 27f, 19f),

        // Nodos de las cuatro puertas del Coto.
        new("HUNT_N_DOOR", 22.5f, 11.5f),
        new("HUNT_S_DOOR", 22.5f, 21.5f),
        new("HUNT_W_DOOR", 14.5f, 13.5f),
        new("HUNT_E_DOOR", 30.5f, 18f),

        // Zonas seguras (callejones sin salida). Los lobos tienen prohibido usar estos nodos.
        new("SAFE_A", 14.5f, 18.5f),
        new("SAFE_A_OUT", 10f, 18.5f),
        new("SAFE_B", 30.5f, 13.5f),
        new("SAFE_B_OUT", 35f, 13.5f),
    };

    // Rutas de patrulla de los lobos. Las zonas seguras quedan lejos de los waypoints:
    // los lobos patrullan el anillo norte/sur y no acampan en las salidas de las Zonas seguras.
    // Tampoco usan nodos de Zonas seguras, de la Zona inicial ni de la Salida.
    public static readonly RouteDef[] WolfRoutes =
    {
        new("Lobo_1", new[] { "TOP_1", "TOP_2", "N_W3", "N_W2", "N_W1", "N_TOP" }, true),
        new("Lobo_2", new[] { "TOP_3", "N_TOP", "N_E1", "N_E2", "N_E3" }, true),
        new("Lobo_3", new[] { "BOT_3", "S_BOT", "S_E2", "S_E1", "E_SE" }, true),
        new("Lobo_4", new[] { "BOT_2", "BOT_1", "S_BOT", "S_W1", "S_W2", "S_W3" }, true),
        new("Lobo_5", new[] { "N_W2", "N_W3", "TOP_1", "TOP_3", "N_E1", "N_E2" }, false),
    };

    public const string HunterStartNode = "INIT_1";
    public const string HunterHuntingNode = "HUNT_C";
    public const string HunterExitNode = "EXIT_1";

    // Nodo más cercano a la entrada de cada Zona segura (para elegir la huida por longitud de camino A*).
    public const string SafeZoneANode = "SAFE_A";
    public const string SafeZoneBNode = "SAFE_B";

    public static int Cols => Map[0].Length;
    public static int Rows => Map.Length;

    public static bool InBounds(int col, int row)
    {
        return row >= 0 && row < Rows && col >= 0 && col < Cols;
    }

    // Fuera del mapa cuenta como pared.
    public static bool IsWall(int col, int row)
    {
        if (!InBounds(col, row)) return true;
        return Map[row][col] == '#';
    }

    // Centro de la celda (col, row) en el mundo (y = 0).
    public static Vector3 ToWorld(int col, int row) => ToWorld((float)col, (float)row);

    // Permite nodos en el centro exacto de una puerta de dos celdas (por ejemplo 22.5).
    public static Vector3 ToWorld(float col, float row)
    {
        return new Vector3((col - CenterCol) * CellSize, 0f, (CenterRow - row) * CellSize);
    }

    public static bool IsSafeZoneCell(float col, float row)
    {
        foreach (ZoneDef zone in Zones)
        {
            if (zone.Type == ZoneType.Safe && zone.Contains(col, row)) return true;
        }

        return false;
    }

    public static bool IsSafeZonePosition(Vector3 worldPosition)
    {
        foreach (ZoneDef zone in Zones)
        {
            if (zone.Type != ZoneType.Safe) continue;

            Vector3 center = zone.WorldCenter;
            Vector2 size = zone.WorldSize;

            if (Mathf.Abs(worldPosition.x - center.x) <= size.x * 0.5f &&
                Mathf.Abs(worldPosition.z - center.z) <= size.y * 0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
