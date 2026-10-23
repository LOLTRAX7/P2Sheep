# Revisión del Parcial 2

Fecha: 23 de octubre de 2026. Proyecto: P2Sheep. Unity: 6000.3.17f1.

## Correcciones realizadas

1. **Objetos de interés:** las primitivas creadas sin prefab ahora se ubican en la posición aleatoria elegida dentro del Coto. Se conservó el límite de cinco objetos. El intervalo de Search ya no se reinicia cada vez que termina un ataque o una recolección. El prefab opcional del constructor se conecta al Cazador.
2. **Navegación:** si A* no encuentra ruta, el agente se detiene. Ya no existe el avance directo hacia un destino bloqueado como fallback. Se validan extremos y segmentos, incluidos los atajos del navegador.
3. **Steering:** los nodos se recorren con velocidad deseada menos velocidad actual, fuerza limitada y aceleración dependiente de deltaTime, siguiendo el ejemplo de Clase 3. Pursuit conserva predicción y valida que el punto predicho no quede detrás de una pared.
4. **Viajes del Cazador:** Search, Flee y Exit exigen A* incluso con visión directa. Esto sigue la exigencia específica del Cazador en la consigna. Los lobos sí aprovechan LoS para no calcular caminos innecesarios.
5. **FSM:** el lobo informa una captura; la propia FSM evalúa la condición prioritaria de Dead. No se fuerza ese cambio desde el método externo de captura.
6. **Refugios:** los lobos excluyen nodos seguros, aristas que atraviesan un refugio, atajos y pasos con inercia que entrarían en su área. La elección del Cazador compara costos calculados sobre A*.
7. **Percepción y comunicación:** el radio considera el centro del objetivo, no sólo el contacto con su collider. Se revalida distancia, ángulo y obstáculos antes de usar una posición en persecución. Una observación propia reemplaza alertas antiguas. El aullido avisa a todos los lobos, como pide la consigna.
8. **Percepción eficiente:** capas dedicadas para Boids y objetos de interés evitan llenar los buffers con paredes. Los contadores del Cazador consideran LoS y el HUD muestra el TBA restante.
9. **Recolección y presencia:** Gather cancela si el objetivo deja de estar disponible y reinicia la interacción si debe volver a acercarse. Un Cazador oculto al completar Exit no puede ser capturado ni iniciar otra huida. Los Boids reaparecen conservando su altura.
10. **Flocking:** las fuerzas de alineación, cohesión y separación se integran como steering con deltaTime. Dos Boids exactamente superpuestos reciben separación en sentidos opuestos. Evade mantiene prioridad y Bounds los retiene dentro del Coto.
11. **Demostración de lobos:** el alcance por defecto pasa a 18 m. Con el alcance anterior de 12 m, el Cazador los veía a 14 m y huía antes de que los lobos pudieran iniciar una persecución en la prueba larga. Las velocidades no se aumentaron.
12. **Organización:** herramientas de Editor reunidas bajo Parcial2/Editor, tutorial y escena de plantilla apartados, paquete opcional de NavMesh eliminado. Se conserva el perfil gráfico llamado SampleSceneProfile porque también lo usan Lvl1 y los renderizadores. El autodiagnóstico exhaustivo del grafo deja de ejecutarse en cada inicio; la verificación se realiza desde pruebas.

## Correspondencia con las consignas

| Requisito | Implementación / evidencia |
| --- | --- |
| Zonas y mapa con obstáculos | LevelData y LevelBuilder: zona inicial, Coto, dos refugios laterales sin salida y salida. Pisos y bordes de colores. |
| Boids del P1 | Ocho agentes, sensores de alcance limitado, separación menor que alineación/cohesión, Evade prioritario, Arrive a objetos, daño periódico, eliminación y reaparición en Bounds. |
| Cazador con FSM | Search, Attack, Gather, Flee, Exit y Dead. Captura prioritaria dentro de la FSM y transiciones de comportamiento dentro de los estados. |
| Objetivo y variables | Tres recolecciones por defecto; RespawnTime 5 s; SafeZoneWaitTime configurable. Attack distingue melee, distancia y Pursuit; respeta TBA. |
| Lobos | Cinco, patrulla por waypoints, velocidades 2.6 / 3.4 / 4.2 frente a 5.2 / 6.2 del Cazador, FoV, LoS, aullido, última posición conocida y búsqueda. |
| Grafo y A* propios | Grafo compartido explícito, cola de prioridad propia, heurística euclidiana fija y exclusión de refugios. Sin NavMesh ni rutas prefijadas que reemplacen la búsqueda. |
| Feedback | Estado, objetivo, recolecciones, TBA, percepción, alertas, posición conocida, refugio elegido, caminos y Gizmos de radios/FoV. |
| Entrega | PDF con nombre de alumno y enlace al repositorio público; Assets, Packages y ProjectSettings versionados. |

## Alcance de la verificación

Las 18 pruebas PlayMode finalizaron aprobadas, sin omisiones, en Unity 6000.3.17f1. Se verificaron 4.234 caminos de A* contra Dijkstra y una simulación integrada de 360 segundos simulados. Los resultados completos se guardan en `Documentacion/Pruebas.xml`. Una suite aprobada verifica los casos incluidos, no sustituye la evaluación del docente.

La aplicación de Windows se compiló correctamente con cero errores y cero advertencias. El resumen está en `Documentacion/Compilacion.txt` y la compilación local en `Revision/Build/`.

El ZIP de respaldo previo, material de trabajo, archivos apartados, capturas y logs se encuentran en `Revision/`, que no se sube a GitHub. Los ZIP originales del Escritorio no se modificaron.

El mapa se conserva como anillo. No se recibió la imagen original del primer ejemplo; no se certifica que cada pared coincida con ella. La consigna permite un mapa propio que cumpla los criterios. La documentación de procedencia está en `BaseDelProfesor.md`.
