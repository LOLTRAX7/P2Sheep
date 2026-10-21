using UnityEngine;

// Objeto de interés generado periódicamente por el Cazador.
// Parcial 2: ya no hay una lista static de objetos activos. Los Boids lo perciben con Physics.OverlapSphere
// (como el resto de la percepción), así que no conocen todos los objetos de la escena.
public class InterestPoint : MonoBehaviour
{
    [SerializeField] private float maxDurability = 30f;
    [Tooltip("Opcional: cambia de color a medida que se va destruyendo, de verde a rojo.")]
    [SerializeField] private Renderer feedbackRenderer;

    private float _currentDurability;

    private void Awake()
    {
        // Para que los Boids lo puedan percibir con OverlapSphere necesita un collider.
        if (GetComponent<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }

        if (feedbackRenderer == null) feedbackRenderer = GetComponentInChildren<Renderer>();
    }

    // Lo usa el Cazador cuando crea el objeto por código (sin prefab).
    public void Initialize(Renderer renderer)
    {
        feedbackRenderer = renderer;
        UpdateFeedback();
    }

    private void OnEnable()
    {
        _currentDurability = maxDurability;
        UpdateFeedback();
    }

    // Llamado por cada BoidAgent
    public void TakeDamage(float amount)
    {
        _currentDurability -= amount;
        UpdateFeedback();

        if (_currentDurability <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateFeedback()
    {
        if (feedbackRenderer == null) return;
        float t = Mathf.Clamp01(_currentDurability / maxDurability);
        feedbackRenderer.material.color = Color.Lerp(Color.red, Color.green, t);
    }
}
