using UnityEditor;
using UnityEngine;

// Crea en el proyecto las layers que usa el Parcial 2 (Wall, Hunter y Wolf) si todavía no existen.
// Se ejecuta sola al abrir el proyecto o recompilar. Es un script de Editor: no entra en el juego.
[InitializeOnLoad]
public static class LayerSetup
{
    private static readonly string[] Layers = { "Wall", "Hunter", "Wolf" };

    static LayerSetup()
    {
        EditorApplication.delayCall += CreateMissingLayers;
    }

    private static void CreateMissingLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

        if (assets == null || assets.Length == 0) return;

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        bool changed = false;

        foreach (string layerName in Layers)
        {
            if (HasLayer(layers, layerName)) continue;

            // Las layers de usuario son las que van de la 8 a la 31: se usa el primer lugar libre.
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);

                if (!string.IsNullOrEmpty(slot.stringValue)) continue;

                slot.stringValue = layerName;
                changed = true;
                break;
            }
        }

        if (!changed) return;

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[Parcial 2] Se crearon las layers Wall, Hunter y Wolf.");
    }

    private static bool HasLayer(SerializedProperty layers, string layerName)
    {
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return true;
        }

        return false;
    }
}
