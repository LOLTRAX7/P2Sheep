using UnityEngine;

// Cambio de color de un agente: el color de su estado y un destello temporal (como AgentFeedback de la Clase 8B).
// Está separado de los agentes para que no manejen materiales (devolución del Parcial 1).
public class ColorFeedback : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;

    private Material _material;
    private Color _baseColor = Color.white;
    private Color _stateColor = Color.white;
    private Color _flashColor;
    private float _flashUntil;

    // Color que tenía el renderer al empezar.
    public Color BaseColor => _baseColor;

    // Indica sobre qué renderer se trabaja. Si es null no se cambia ningún color.
    public void SetRenderer(Renderer renderer)
    {
        _renderer = renderer;
        _material = null;

        if (_renderer == null) return;

        _material = _renderer.material;
        _baseColor = _material.color;
        _stateColor = _baseColor;
    }

    // Color "de fondo" (el del estado actual).
    public void SetColor(Color color)
    {
        _stateColor = color;
        Apply();
    }

    // Durante 'duration' segundos se ve 'color'; después vuelve al color del estado.
    public void Flash(Color color, float duration)
    {
        _flashColor = color;
        _flashUntil = Time.time + duration;
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (_material == null) return;

        _material.color = Time.time < _flashUntil ? _flashColor : _stateColor;
    }

    private void OnValidate()
    {
        if (_renderer == null) TryGetComponent(out _renderer);
    }
}
