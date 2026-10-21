using System.Collections.Generic;
using UnityEngine;

// Nodo del grafo de navegación.
// El grafo se representa con LISTAS DE ADYACENCIA (no con una matriz ni con una grilla de celdas).
// No guarda estado de búsqueda (visited / cost): A* usa colecciones locales, así el grafo puede
// consultarse desde varios agentes sin que una búsqueda pise a otra.
public class Node : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private bool _blocked;
    [SerializeField] private bool _isSafeZone;
    [SerializeField] private Color _defaultColor = new(1f, 0.25f, 0.65f);

    private readonly List<Node> _neighbors = new();

    public IReadOnlyList<Node> Neighbors => _neighbors;

    // Un nodo bloqueado no se usa al buscar caminos.
    public bool Blocked => _blocked;

    // Los nodos de las Zonas seguras existen en el grafo compartido, pero los lobos los excluyen.
    public bool IsSafeZone => _isSafeZone;

    // Conexión en un solo sentido: para que sea bidireccional hay que llamarla en los dos nodos.
    public void AddNeighbor(Node node)
    {
        if (node == null || node == this || _neighbors.Contains(node)) return;

        _neighbors.Add(node);
    }

    public void ClearNeighbors() => _neighbors.Clear();

    public void SetSafeZone(bool value) => _isSafeZone = value;

    public float DistanceTo(Node other) => Vector3.Distance(transform.position, other.transform.position);

    public void SetMarkerRenderer(Renderer renderer)
    {
        _renderer = renderer;
        ResetColor();
    }

    public void SetDefaultColor(Color color)
    {
        _defaultColor = color;
        ResetColor();
    }

    public void ChangeColor(Color color)
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _renderer.material.color = color;
    }

    public void ResetColor() => ChangeColor(_blocked ? Color.red : _defaultColor);

    private void OnValidate()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isSafeZone ? new Color(0.65f, 0.5f, 0.92f) : _defaultColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.15f, 0.28f);
    }
}
