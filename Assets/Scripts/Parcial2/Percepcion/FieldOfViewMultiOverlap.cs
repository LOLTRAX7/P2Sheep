using System.Collections.Generic;
using UnityEngine;

// Igual que FieldOfViewOverlap, pero guarda TODOS los objetivos visibles y no sólo el más cercano.
// Lo usa el Cazador para saber cuántos lobos tiene a la vista (sin ángulo: 360° + línea de visión).
public class FieldOfViewMultiOverlap : FieldOfViewSensor
{
    [Header("Overlap")]
    [SerializeField] private float _checkInterval = 0.2f;

    private static readonly Collider[] Buffer = new Collider[128];

    private float _timer;
    private readonly List<Transform> _visibleTargets = new();

    public IReadOnlyList<Transform> VisibleTargets => _visibleTargets;

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _checkInterval)
        {
            _timer = 0f;
            CheckVisibility();
        }
    }

    private void CheckVisibility()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, _viewRadius, Buffer, _targetMask);

        _visibleTargets.Clear();

        Transform closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider hit = Buffer[i];

            if (hit == null || hit.transform.IsChildOf(transform)) continue;

            Transform candidate = hit.transform;
            if (!IsAllowed(candidate) || _visibleTargets.Contains(candidate)) continue;

            _target = candidate;

            if (!IsVisibleNow(candidate)) continue;

            _visibleTargets.Add(candidate);

            float distance = (candidate.position - transform.position).sqrMagnitude;

            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closest = candidate;
        }

        _target = closest;
        CanSeeTarget = closest != null;
    }
}
