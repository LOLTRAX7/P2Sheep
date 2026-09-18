using UnityEngine;


public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private float height = 30f;
    [SerializeField] private float width = 60f;
    [SerializeField] private bool drawGizmos;

    private void Awake()
    {
        
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        
        Vector3 newPosition = position;

        if (position.x > width / 2) newPosition.x = -width / 2;
        if (position.x < -width / 2) newPosition.x = width / 2;
        if (position.z > height / 2) newPosition.z = -height / 2;
        if (position.z < -height / 2) newPosition.z = height / 2;

        return newPosition;
    }

    
    public Vector3 GetRandomPointInside()
    {
        float x = Random.Range(-width / 2f, width / 2f);
        float z = Random.Range(-height / 2f, height / 2f);
        return new Vector3(x, 0f, z);
    }

    private void OnDrawGizmos()
    {
        
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0, height));
    }
}