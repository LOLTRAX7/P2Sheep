using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Herramienta de Editor: crea la escena de entrega del Parcial 2 con la cámara, la luz y el LevelBuilder.
// Menú: Parcial 2 / Crear escena de entrega.
// También se puede ejecutar en batchmode:
//   Unity.exe -batchmode -quit -projectPath ... -executeMethod Parcial2SceneSetup.CreateScene
public static class Parcial2SceneSetup
{
    private const string ScenePath = "Assets/Scenes/Parcial2.unity";

    [MenuItem("Parcial 2/Crear escena de entrega")]
    public static void CreateScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Cámara cenital.
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.orthographic = true;
        camera.orthographicSize = 37f;
        camera.transform.position = new Vector3(0f, 70f, 0f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 200f;

        // Luz direccional (el nivel no depende de ella para funcionar, pero se ve mejor).
        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.2f;

        // El nivel se construye al darle Play.
        GameObject levelObject = new("Nivel");
        levelObject.AddComponent<LevelBuilder>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Deja el Parcial 2 como primera escena y conserva el Lvl1 del Parcial 1.
        List<EditorBuildSettingsScene> scenes = new();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

        const string lvl1 = "Assets/Scenes/Lvl1.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(lvl1) != null)
        {
            scenes.Add(new EditorBuildSettingsScene(lvl1, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Parcial 2] Escena creada en " + ScenePath + " y agregada a Build Settings.");
    }
}
