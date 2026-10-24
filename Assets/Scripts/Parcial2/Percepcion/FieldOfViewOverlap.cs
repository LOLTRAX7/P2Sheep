using UnityEngine;

// Sensor de un solo objetivo (el más cercano que cumpla distancia + ángulo + línea de visión).
// Se usa en los lobos para detectar al Cazador. El filtro TargetFilter permite que un lobo ignore
// al Cazador mientras está dentro de una Zona segura.
public class FieldOfViewOverlap : FieldOfViewSensor
{
    [Header("Overlap")]
    [SerializeField] private float _checkInterval = 0.2f;

    private readonly Collider[] _buffer = new Collider[128];

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _checkInterval)
        {
            _timer = 0f;
            CanSeeTarget = CheckVisibility();
        }
    }

    private bool CheckVisibility()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, _viewRadius, _buffer, _targetMask);

        _target = null;
        Transform best = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider hit = _buffer[i];

            if (hit == null || hit.transform.IsChildOf(transform)) continue;

            Transform candidate = hit.transform;
            if (!IsAllowed(candidate)) continue;

            _target = candidate;

            if (!IsVisibleNow(candidate)) continue;

            float distance = (candidate.position - transform.position).sqrMagnitude;

            if (distance >= closestDistance) continue;

            closestDistance = distance;
            best = candidate;
        }

        _target = best;
        return best != null;
    }
}
