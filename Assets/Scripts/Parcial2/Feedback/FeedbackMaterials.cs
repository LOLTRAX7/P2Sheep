using UnityEngine;

// Material sin iluminación para las líneas, conos y anillos del feedback.
// "Sprites/Default" existe en cualquier pipeline (Built-in, URP) y respeta el color y la transparencia.
public static class FeedbackMaterials
{
    public static Material CreateUnlit(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
