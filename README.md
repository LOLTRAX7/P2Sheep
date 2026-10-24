# Parcial 2 - Inteligencia Artificial

Valentin Lamas · Unity **6000.3.17f1**

## Ejecución

Abrir el proyecto en Unity, cargar `Assets/Scenes/Parcial2.unity` y presionar Play.
La simulación es automática. Los radios y el grafo se pueden observar con Gizmos en Scene.

## Mapa A: anillo

El nivel sigue la distribución del primer ejemplo de la consigna:

- Zona inicial al oeste y salida al este.
- Coto central con ocho Boids.
- Dos refugios en callejones sin salida.
- Cuatro lobos con patrullas oeste, norte, este y sur.
- Habitaciones de patrulla dentro del anillo, al norte y al sur.

El Cazador debe recolectar tres Boids y llegar a la salida. Si ve un lobo, huye al
refugio más cercano por costo de A*. Si lo capturan, reaparece después de cinco segundos.

## Código

`Assets/Scripts/Cazador` contiene el Cazador y sus estados. `FSM` contiene la máquina
de estados. `Agentes` mantiene el flocking y los límites del Coto. `Parcial2` agrupa
el grafo, A*, percepción, lobos, zonas y feedback. Vida y colores están separados
en `Health` y `ColorFeedback`.

A* y los sensores están adaptados del material de clase. El Cazador y los lobos
comparten el grafo; los lobos excluyen los refugios. No se utiliza NavMesh.

La escena `Assets/Scenes/Lvl1.unity` conserva la simulación del Parcial 1.

## Entrega

El PDF es `Lamas Valentin Parcial 2.pdf`.
Repositorio público: [LOLTRAX7/P2Sheep](https://github.com/LOLTRAX7/P2Sheep).
