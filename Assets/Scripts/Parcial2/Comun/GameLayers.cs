using UnityEngine;

// Layers que usa el Parcial 2. Se crean solas con LayerSetup (carpeta Editor); si no existen se usan los índices 8, 9 y 10.
public static class GameLayers
{
    public static int Wall => Resolve("Wall", 8);
    public static int Hunter => Resolve("Hunter", 9);
    public static int Wolf => Resolve("Wolf", 10);
    public static int Boid => Resolve("Boid", 11);
    public static int Interest => Resolve("Interest", 12);

    public static LayerMask WallMask => 1 << Wall;
    public static LayerMask HunterMask => 1 << Hunter;
    public static LayerMask WolfMask => 1 << Wolf;
    public static LayerMask BoidMask => 1 << Boid;
    public static LayerMask InterestMask => 1 << Interest;

    private static int Resolve(string layerName, int fallback)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : fallback;
    }

    // Cambia la layer del objeto y la de todos sus hijos (el collider puede estar en un hijo).
    public static void SetLayer(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayer(child.gameObject, layer);
        }
    }
}
