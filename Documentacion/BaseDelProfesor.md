# Base de clase y adaptaciones del Parcial 2

Material cotejado: `UTN-AI-2026-main.zip` y `Pathfinding + LoS + FoV.zip`, aportados para esta revisión. Los scripts del ZIP `Assets.zip` coincidían con los del proyecto antes de corregirlo.

| Sistema | Referencia del profesor | Implementación del proyecto y cambios |
| --- | --- | --- |
| Percepción | UTN, `Clase 7/FieldOfViewSensor.cs`, `FieldOfViewOverlap.cs` y `FieldOfViewGizmos.cs` | `Assets/Scripts/Parcial2/Percepcion`. Conserva radio, ángulo, altura de ojos, máscaras y raycast. Agrega filtros de muerte/refugios, consulta sin asignación, visión múltiple para el Cazador y validación de distancia al centro del objetivo. |
| Cola de prioridad | `Clase 11/PriorityQueue.cs` y `Pathfinding/PriorityQueue.cs` del segundo ZIP | `Assets/Scripts/Parcial2/Pathfinding/PriorityQueue.cs`. Conserva el heap binario y sus operaciones Enqueue, Dequeue, HeapifyUp y HeapifyDown. |
| A* | `Clase 11/Pathfinding.cs` y `Pathfinding/Pathfinding.cs` del segundo ZIP | Conserva frontier, cameFrom, costSoFar, costo + heurística y reconstrucción por padres. Usa distancias reales entre nodos y heurística euclidiana fija. No tiene selector HeuristicMethod ni parámetro de visualización visited. Excluye nodos y segmentos de refugios para lobos. |
| Grafo | Graph/Node de los ejemplos de grafos y del segundo ZIP | Grafo compartido de nodos explícitos y listas de adyacencia. Las conexiones se verifican con SphereCast y solapamiento de extremos. La matriz de caracteres de LevelData construye paredes; no se usa como grilla de búsqueda. |
| FSM | `Clase 5/StateMachine.cs`, sus estados y `Pathfinding/FSM/FiniteStateMachine.cs` del segundo ZIP | Conserva registro de estados y Enter/Update/Exit. La versión genérica enlaza estados con su FSM y publica el ID para feedback. La captura informa una condición; la FSM decide la transición global prioritaria a Dead. Las otras transiciones las deciden sus estados. |
| Steering | `Clase 3/IntermediateAgentSteering.cs` | `Agent.MoveWithSteering` adapta desired - velocity, fuerza limitada por deltaTime, Seek/Arrive e integración de velocidad. `HunterAgent.PursueBoid` calcula posición futura. Se agrega validación de movimiento contra paredes y refugios. |
| Flocking y P1 | Requisitos del Parcial 1 y scripts heredados de este proyecto | Se conservan BoidAgent, Bounds, BoidSpawner, InterestPoint y la escena Lvl1. Se verifican separación, alineación, cohesión, Evade prioritario, Arrive, daño al objeto, eliminación, recolección y reaparición. |

El conjunto `closed` de A* es local a cada búsqueda y evita expandir entradas duplicadas de la cola; no es un campo visited en Node ni una lista de visualización del recorrido. Esto coincide con el uso de un conjunto `processed` en el ejemplo de Clase 11. Se prioriza la consigna escrita según la indicación del alumno.

Las FSM, los estados, la coordinación por aullido, la construcción del nivel y el feedback están adaptados para el parcial. No se afirma que todo el proyecto sea una copia literal de los scripts del profesor. No se proporcionó una versión histórica separada del código del P1: su conservación se comprueba sobre la escena heredada y los comportamientos exigidos.

## Navegación exigida

- Cazador: A* para viajar al Coto, recorrer sus nodos, huir y salir. Pursuit/Seek/Arrive sólo para el tramo final hacia un Boid con visión directa.
- Lobos: waypoints de patrulla, como exige la consigna. Desplazamiento directo con LoS; A* cuando una pared impide ir directo. Las alertas transmiten una posición tomada en el momento de la detección.
- Sin ruta válida, el agente se detiene. No hay avance directo como sustituto de un A* fallido.
- No hay NavMeshAgent, NavMeshSurface ni llamadas de navegación NavMesh en los scripts. También se quitó el paquete opcional de navegación que estaba instalado y no se utilizaba.

## Escenario elegido

Se conserva el mapa anular identificado como Ejemplo 1 / Variante A en el proyecto: zona inicial al oeste, Coto central, dos refugios laterales en callejones y salida al este. El archivo de consigna recibido sólo contiene los nombres blobid0.png y blobid1.png; sin esas imágenes no puede asegurarse una coincidencia geométrica exacta con la referencia. La consigna permite usar una referencia o un mapa propio que cumpla las zonas y la navegación.
