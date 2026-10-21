#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Crea prefabs básicos del Parcial 2 para que el nivel se pueda montar desde el Inspector.
// Ejecutar: menú "Parcial 2 / Crear prefabs básicos".
public static class CreateParcial2Prefabs
{
    private const string Folder = "Assets/Prefabs";

    [MenuItem("Parcial 2/Crear prefabs básicos")]
    public static void CreateAllPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        CreateHunterPrefab();
        CreateWolfPrefab();
        CreateBoidPrefab();
        CreateInterestPointPrefab();

        AssetDatabase.SaveAssets();
        Debug.Log("[Parcial 2] Prefabs creados en " + Folder);
    }

    private static GameObject CreatePrimitiveBody(string name, PrimitiveType type, Vector3 scale, Color color, bool isTrigger)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.localScale = scale;

        var renderer = go.GetComponentInChildren<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };

        var collider = go.GetComponentInChildren<Collider>();
        if (collider != null)
            collider.isTrigger = isTrigger;

        return go;
    }

    private static void SavePrefab(GameObject go, string path)
    {
        if (PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
            return;

        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
    }

    private static void CreateHunterPrefab()
    {
        GameObject hunter = CreatePrimitiveBody("Hunter", PrimitiveType.Capsule, new Vector3(0.8f, 0.95f, 0.8f), new Color(0.9f, 0.9f, 0.95f), true);

        var eyes = new GameObject("Eyes");
        eyes.transform.SetParent(hunter.transform, false);
        eyes.transform.localPosition = new Vector3(0f, 0.465f, 0.353f);
        eyes.transform.localScale = new Vector3(0.516f, 0.255f, 0.570f);
        eyes.AddComponent<MeshRenderer>();
        var eyesCol = eyes.AddComponent<BoxCollider>();
        eyesCol.isTrigger = true;

        hunter.AddComponent<HunterAgent>();

        SavePrefab(hunter, Folder + "/Hunter.prefab");
    }

    private static void CreateWolfPrefab()
    {
        GameObject wolf = CreatePrimitiveBody("Wolf", PrimitiveType.Capsule, new Vector3(0.7f, 0.85f, 1.1f), new Color(0.65f, 0.65f, 0.7f), true);

        wolf.AddComponent<WolfAgent>();

        SavePrefab(wolf, Folder + "/Wolf.prefab");
    }

    private static void CreateBoidPrefab()
    {
        GameObject boid = CreatePrimitiveBody("Prey", PrimitiveType.Sphere, Vector3.one * 0.7f, new Color(0.7f, 0.85f, 1f), true);
        boid.AddComponent<BoidAgent>();

        SavePrefab(boid, Folder + "/Prey.prefab");
    }

    private static void CreateInterestPointPrefab()
    {
        GameObject poi = CreatePrimitiveBody("InterestPoint", PrimitiveType.Cylinder, new Vector3(0.45f, 0.2f, 0.45f), new Color(1f, 0.85f, 0.2f), true);
        UnityEngine.Object.DestroyImmediate(poi.GetComponent<Collider>());
        var sphere = poi.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 0.5f;

        var renderer = poi.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.85f, 0.2f) };

        poi.AddComponent<InterestPoint>();

        SavePrefab(poi, Folder + "/InterestPoint.prefab");
    }
}
#endif
