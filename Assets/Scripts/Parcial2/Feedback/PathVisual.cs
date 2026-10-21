using System.Collections.Generic;
using UnityEngine;

// Dibuja el camino A* que está siguiendo el agente (agente -> nodos que faltan -> destino) con un LineRenderer.
// Sólo se ve mientras el GraphNavigator usa pathfinding: es el feedback de "cuándo se está usando A*".
[RequireComponent(typeof(GraphNavigator))]
public class PathVisual : MonoBehaviour
{
    [SerializeField] private Color _color = new(1f, 0.6f, 0.1f);
    [SerializeField] private float _width = 0.16f;
    [SerializeField] private float _height = 0.2f;

    private GraphNavigator _navigator;
    private LineRenderer _line;

    private void Awake()
    {
        _navigator = GetComponent<GraphNavigator>();

        GameObject lineObject = new("PathLine");
        lineObject.transform.SetParent(transform, false);

        _line = lineObject.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.sharedMaterial = FeedbackMaterials.CreateUnlit(Color.white);
        _line.widthMultiplier = _width;
        _line.numCornerVertices = 3;
        _line.startColor = _color;
        _line.endColor = _color;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.enabled = false;
    }

    private void LateUpdate()
    {
        if (!_navigator.UsingPathfinding)
        {
            if (_line.enabled) _line.enabled = false;
            return;
        }

        IReadOnlyList<Vector3> path = _navigator.PathPoints;

        _line.positionCount = path.Count + 2;
        _line.SetPosition(0, Lift(transform.position));

        for (int i = 0; i < path.Count; i++)
        {
            _line.SetPosition(i + 1, Lift(path[i]));
        }

        _line.SetPosition(path.Count + 1, Lift(_navigator.CurrentDestination));

        if (!_line.enabled) _line.enabled = true;
    }

    private Vector3 Lift(Vector3 point)
    {
        return new Vector3(point.x, _height, point.z);
    }
}
