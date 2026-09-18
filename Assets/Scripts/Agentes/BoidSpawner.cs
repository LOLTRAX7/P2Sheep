using UnityEngine;


public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private BoidAgent boidPrefab;
    [SerializeField] private int count = 8;

    private void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogWarning("[BoidSpawner] No se asignó un prefab de BoidAgent.", this);
            return;
        }

        if (Bounds.Instance == null)
        {
            Debug.LogWarning("[BoidSpawner] No hay un objeto Bounds en la escena.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = Bounds.Instance.GetRandomPointInside();
            Instantiate(boidPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
