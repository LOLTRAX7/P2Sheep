using System;
using UnityEngine;

// Sensor de percepción (distancia + ángulo opcional + línea de visión), basado en el Field Of View de la materia.
// El Cazador lo usa con ángulo 360° (la consigna sólo pide distancia máxima y obstáculos).
public abstract class FieldOfViewSensor : MonoBehaviour
{
    [Header("Field Of View")]
    [SerializeField] protected float _viewRadius = 8f;
    [SerializeField] protected float _eyeHeight = 1f;
    [SerializeField, Range(0f, 360f)] protected float _viewAngle = 90f;

    [Header("Filtros por Layer")]
    [SerializeField] protected LayerMask _targetMask = ~0;
    [SerializeField] protected LayerMask _obstacleMask = ~0;

    protected Transform _target;

    // Filtro adicional opcional (por ejemplo: "no percibir al Cazador mientras está en una Zona segura").
    // Lo usa WolfAgent sin acoplar el sensor a la clase WolfAgent.
    public Func<Transform, bool> TargetFilter { get; set; }

    public bool CanSeeTarget { get; protected set; }

    public Transform Target => _target;
    public float ViewRadius => _viewRadius;
    public float ViewAngle => _viewAngle;
    public float EyeHeight => _eyeHeight;

    // Permite armar el sensor por código: los lobos y el Cazador se crean al dar Play.
    public void Configure(float viewRadius, float viewAngle, float eyeHeight, LayerMask targetMask, LayerMask obstacleMask)
    {
        _viewRadius = viewRadius;
        _viewAngle = viewAngle;
        _eyeHeight = eyeHeight;
        _targetMask = targetMask;
        _obstacleMask = obstacleMask;
    }

    protected static bool IsAValidTarget(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    protected bool IsAllowed(Transform candidate)
    {
        if (candidate == null) return false;
        return TargetFilter == null || TargetFilter(candidate);
    }

    protected bool InViewAngle()
    {
        if (_target == null) return false;
        if (_viewAngle >= 359f) return true;

        Vector3 dirToTarget = (_target.position - transform.position).normalized;
        return Vector3.Angle(transform.forward, dirToTarget) <= _viewAngle * 0.5f;
    }

    protected bool HasLineOfSight()
    {
        if (_target == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * _eyeHeight;
        Vector3 targetPos = _target.position + Vector3.up * _eyeHeight;
        Vector3 direction = targetPos - eyePos;

        if (Physics.Raycast(eyePos, direction.normalized, out RaycastHit hit, direction.magnitude, _obstacleMask))
            return hit.transform == _target || hit.transform.IsChildOf(_target);

        return true;
    }
}
