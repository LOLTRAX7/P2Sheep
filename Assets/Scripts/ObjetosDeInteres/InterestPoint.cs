using System.Collections.Generic;
using UnityEngine;

// Objeto de interés generado periódicamente por el Cazador
public class InterestPoint : MonoBehaviour
{
    // Registro compartido de objetos de interés activos
    public static readonly List<InterestPoint> ActivePoints = new List<InterestPoint>();

    [SerializeField] private float maxDurability = 30f;
    [Tooltip("Opcional: cambia de color a medida que se va destruyendo, de verde a rojo.")]
    [SerializeField] private Renderer feedbackRenderer;

    private float _currentDurability;

    private void OnEnable()
    {
        _currentDurability = maxDurability;
        ActivePoints.Add(this);
        UpdateFeedback();
    }

    private void OnDisable()
    {
        ActivePoints.Remove(this);
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
