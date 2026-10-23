using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Verifica que el proyecto también compile como aplicación independiente.
public static class Parcial2BuildVerification
{
    [MenuItem("Parcial 2/Verificar compilación Windows")]
    public static void BuildWindows()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Revision/Build"));
        Directory.CreateDirectory(folder);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = Path.Combine(folder, "P2Sheep.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        string message = "[Revision] Compilación Windows: " + report.summary.result
            + ", errores: " + report.summary.totalErrors + ", advertencias: " + report.summary.totalWarnings;
        Debug.Log(message);
        File.WriteAllText(Path.Combine(folder, "Resultado.txt"), message);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(message);
    }
}
