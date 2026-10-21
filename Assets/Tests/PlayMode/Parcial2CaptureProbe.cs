using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Sonda temporal: fuerza un encuentro lobo-Cazador para validar la captura y el respawn de la FSM Dead.
public class Parcial2CaptureProbe
{
    [UnityTest]
    public IEnumerator Lobo_Captura_Y_Cazador_Reaparece()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("Parcial2");
        yield return null;
        yield return new WaitForSeconds(1f);

        HunterAgent hunter = HunterAgent.Current;
        WolfAgent[] wolves = Object.FindObjectsByType<WolfAgent>(FindObjectsSortMode.None);

        Assert.IsNotNull(hunter);
        Assert.GreaterOrEqual(wolves.Length, 1);

        WolfAgent wolf = wolves[0];

        // Coto de caza, lejos de las Zonas seguras: el Cazador queda dentro de CaptureRadius.
        // Se llama al mismo método de captura que usa la FSM del lobo, para que la prueba sea determinista
        // (la percepción y la persecución ya se ven en la sonda de comportamiento).
        wolf.transform.position = new Vector3(0f, wolf.transform.position.y, 0f);
        hunter.transform.position = new Vector3(0f, hunter.transform.position.y, 0.8f);

        Assert.IsTrue(wolf.TryCaptureHunter(), "El lobo no capturó al Cazador dentro de CaptureRadius.");
        yield return null;

        Assert.IsTrue(hunter.IsDead, "El Cazador no pasó a DEAD después de la captura.");
        Assert.AreEqual(0, hunter.Gathers, "El contador debería quedar en cero al morir.");

        // Dead debe esperar RespawnTime y volver a Search en la Zona inicial.
        yield return new WaitForSeconds(hunter.RespawnTime + 0.25f);

        Assert.IsFalse(hunter.IsDead, "El Cazador no reapareció después de RespawnTime.");
        Assert.AreEqual(0, hunter.Gathers, "El contador de recolecciones no se reinició al reaparecer.");
        Assert.Less(Vector3.Distance(hunter.transform.position, new Vector3(-40f, hunter.transform.position.y, -3f)), 3f,
            "El Cazador no reapareció en la Zona inicial.");
    }
}
