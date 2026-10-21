# P2Sheep — Parcial 2 de Inteligencia Artificial

**Alumno:** Valentin Lamas  
**Materia:** Inteligencia Artificial  
**Motor:** Unity 6000.3.17f1  
**Repositorio:** <https://github.com/LOLTRAX7/P2Sheep>  
**PDF de entrega:** `Lamas Valentin Parcial 2.pdf`

## Cómo ejecutar

1. Abrir el proyecto con Unity **6000.3.17f1**.
2. Abrir la escena `Assets/Scenes/Parcial2.unity`.
3. Presionar **Play**. El nivel, los agentes y el HUD se generan en runtime desde `LevelBuilder`.

La escena del Parcial 1 (`Assets/Scenes/Lvl1.unity`) se conserva y sigue siendo jugable.

## Prefabs y herramientas de Editor

En el menú **Parcial 2 / Crear prefabs básicos** se generan los prefabs de Hunter, Wolf, Boid (Prey) e InterestPoint en `Assets/Prefabs/`. Si se asignan en el Inspector de `LevelBuilder` (campos opcionales "Prefabs"), el constructor los usará en lugar de crear primitivas por código; si no se asignan, se mantiene el comportamiento actual de generación por código.

## Escenario (Ejemplo 1 — Variante A: anillo)

El mapa mide 46 × 34 celdas de 2 × 2 metros y se genera desde `LevelData`:

- **Zona inicial aislada** al oeste: punto de aparición y respawn del Cazador.
- **Coto de caza** central: contiene 8 Boids y los objetos de interés.
- **Zona segura A** y **Zona segura B**: callejones sin salida. Los lobos no pueden entrar ni usar sus nodos.
- **Salida** al este: el Cazador debe llegar después de recolectar 3 Boids.
- Pasillos, habitaciones de patrulla y obstáculos alrededor del anillo.

## Agentes

### Cazador (FSM)

Estados: `Search`, `Attack`, `Gather`, `Flee`, `Exit`, `Dead`.

Prioridad de transiciones: `Dead > Flee > Exit > Gather > Attack > Search`.

- `Search`: entra al Coto, crea objetos de interés y patrulla nodos del Coto.
- `Attack`: ataque melee o a distancia según `MeleeAttackRadius` y `RangeAttackRadius`; respeta el `TBA`.
- `Gather`: recolecta un Boid eliminado durante `GatherDuration`; suma una recolección.
- `Flee`: elige por A* la Zona segura más cercana y espera `SafeZoneWaitTime`.
- `Exit`: con `RequiredGathers` recolecciones se dirige a la Salida y reinicia el ciclo.
- `Dead`: un lobo lo alcanza; espera `RespawnTime` y reaparece en la Zona inicial.

Valores principales: `RequiredGathers = 3`, `RespawnTime = 5`, `SafeZoneWaitTime = 4`, `TBA = 1.5`.

La navegación usa **A\* sobre el grafo compartido**. Sólo el tramo final hacia un Boid con línea de visión directa utiliza steering del Parcial 1 (`Pursuit`, `Seek`/`Arrive`).

Percepción: radio máximo + obstáculos que cortan la línea de visión, sin ángulo (360°). Se muestra en el HUD y con Gizmos.

### Lobos (5)

Cada lobo tiene una **FSM** con `Patrol`, `Chase`, `GoToPosition` y `Search`.

- Patrullan por waypoints; cuando no hay línea de visión usan A* sobre el mismo grafo, excluyendo los nodos de Zonas seguras.
- FoV con distancia, ángulo y línea de visión (paredes).
- Al detectar al Cazador aúllan: sonido, anillo visual, aviso a los demás lobos y última posición conocida.
- Los que reciben la alerta van a la última posición y buscan.
- Si el Cazador entra en una Zona segura, descartan el objetivo y vuelven a patrullar.
- Si lo alcanzan dentro de `CaptureRadius`, el Cazador pasa a `Dead`.
- Velocidades: patrulla 2.6, alerta 3.4, persecución 4.2; siempre menores que el Cazador (5.2 / 6.2).

### Boids (8)

Se conserva el flocking del Parcial 1 (separación, alineación, cohesión y evasión) y se reutiliza el componente `Bounds`. Los Boids perciben vecinos, objetos de interés y al Cazador mediante física, sin listas globales.

## Pathfinding

- Un único grafo compartido por Cazador y lobos.
- 47 nodos y 150 conexiones; las conexiones se validan con `SphereCast` para no atravesar paredes.
- **A\* propio** con `PriorityQueue`, costo real por distancia y heurística euclidiana fija.
- `closed` es local a cada búsqueda: no se guarda `visited` dentro de los nodos y no hay enum de heurísticas.
- Los lobos llaman a `AStar(..., avoidSafeZones: true)` y usan `GraphNavigator.Configure(true)`; el Cazador puede usar todos los nodos.
- No se usa NavMesh, grilla de celdas ni rutas fijas para navegar.

## Feedback visual y sonoro

- HUD del Cazador: estado, recolecciones, TBA, lobos percibidos y Zona segura elegida.
- Líneas en Game view: detección Cazador → lobo, lobo → Cazador, Zona segura elegida y camino A*.
- Colores por estado, aullido, destello al recibir alerta y marcador de última posición conocida.
- Panel de eventos con detecciones, alertas, uso de A* y cambios de estado.
- Gizmos siempre visibles: radios de Boids, radios y FoV del Cazador, FoV y `CaptureRadius` de los lobos, y grafo del nivel.

## Estructura relevante

```text
Assets/
  Scenes/
    Parcial2.unity          Escena de entrega
    Lvl1.unity              Parcial 1 conservado
  Scripts/
    Cazador/                HunterAgent + estados de la FSM
    Parcial2/
      Comun/                Zones, Health, GameLayers
      Escenario/            LevelData, LevelBuilder, GraphSelfTest
      Pathfinding/          Graph, A*, GraphNavigator, Node
      Lobos/                WolfAgent + estados
      Feedback/             HUD, gizmos, líneas y registro de eventos
  Tests/PlayMode/           Pruebas automáticas de escena, grafo y captura/respawn
```

## Pruebas automáticas

Las pruebas PlayMode comprueban:

- que la escena del Parcial 2 se construya y el grafo esté conectado (incluido el subgrafo sin Zonas seguras);
- que existan al menos 6 Boids y 4 lobos, que los lobos no usen nodos seguros y que sus velocidades sean menores que las del Cazador;
- la captura del Cazador, el estado `Dead`, el respawn y el reinicio de recolecciones;
- que `Lvl1` siga cargando y generando Boids, con su Hunter en modo de compatibilidad.

La suite PlayMode finalizó correctamente con **3/3 pruebas aprobadas**.

Comando de ejemplo:

```powershell
Unity.exe -batchmode -nographics -projectPath "C:\Users\valen\P2" `
  -runTests -testPlatform PlayMode `
  -testResults "TestResults.xml" -logFile "tests.log"
```

## Controles

La simulación es automática; no requiere input del jugador. Se puede pausar con el botón **Pause** del Editor y observar los Gizmos en la pestaña **Scene**.
