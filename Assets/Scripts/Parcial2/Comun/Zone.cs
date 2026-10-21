using System;
using System.Collections.Generic;
using UnityEngine;

// Tipo de zona del escenario del Parcial 2.
public enum ZoneType
{
    Initial,
    Hunting,
    Safe,
    Exit,
    Patrol,
}

// Zona del nivel: Zona inicial, Coto de caza, Zona segura o Salida.
// Guarda un rectángulo en el plano XZ y se registra en una lista estática para que el Cazador y los lobos
// puedan consultarla sin usar FindObjectsOfType todos los frames.
[ExecuteAlways]
public class Zone : MonoBehaviour
{
    [SerializeField] private ZoneType _type = ZoneType.Patrol;
    [SerializeField] private Vector3 _center;
    [SerializeField] private Vector2 _size = new(2f, 2f);

    private static readonly List<Zone> _all = new();
    private static readonly List<Zone> _safeZones = new();

    public static IReadOnlyList<Zone> All => _all;
    public static IReadOnlyList<Zone> SafeZones => _safeZones;

    public ZoneType Type => _type;
    public Vector2 Size => _size;
    public Vector3 Center => _center;

    // Bounds en el plano XZ (altura 1 para que los gizmos se vean como una franja).
    public UnityEngine.Bounds WorldBounds => new(new Vector3(_center.x, 0.5f, _center.z), new Vector3(_size.x, 1f, _size.y));

    public static event Action ZonesChanged = delegate { };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _all.Clear();
        _safeZones.Clear();
    }

    private void OnEnable()
    {
        if (!_all.Contains(this)) _all.Add(this);
        if (_type == ZoneType.Safe && !_safeZones.Contains(this)) _safeZones.Add(this);

        ZonesChanged?.Invoke();
    }

    private void OnDisable()
    {
        _all.Remove(this);
        _safeZones.Remove(this);

        ZonesChanged?.Invoke();
    }

    public void Configure(ZoneType type, Vector3 center, Vector2 size)
    {
        _type = type;
        _center = new Vector3(center.x, 0f, center.z);
        _size = size;

        _all.Remove(this);
        _safeZones.Remove(this);

        _all.Add(this);
        if (_type == ZoneType.Safe) _safeZones.Add(this);

        ZonesChanged?.Invoke();
    }

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - _center;
        return Mathf.Abs(offset.x) <= _size.x * 0.5f && Mathf.Abs(offset.z) <= _size.y * 0.5f;
    }

    public Vector3 ClosestPoint(Vector3 worldPosition)
    {
        float halfX = _size.x * 0.5f;
        float halfZ = _size.y * 0.5f;

        return new Vector3(
            Mathf.Clamp(worldPosition.x, _center.x - halfX, _center.x + halfX),
            0f,
            Mathf.Clamp(worldPosition.z, _center.z - halfZ, _center.z + halfZ));
    }

    public Vector3 RandomPoint()
    {
        float halfX = _size.x * 0.5f;
        float halfZ = _size.y * 0.5f;

        return new Vector3(
            UnityEngine.Random.Range(_center.x - halfX, _center.x + halfX),
            0f,
            UnityEngine.Random.Range(_center.z - halfZ, _center.z + halfZ));
    }

    public static bool IsInsideSafeZone(Vector3 worldPosition)
    {
        for (int i = 0; i < _safeZones.Count; i++)
        {
            if (_safeZones[i] != null && _safeZones[i].Contains(worldPosition)) return true;
        }

        return false;
    }

    public static Zone FindNearestSafeZone(Vector3 worldPosition)
    {
        Zone best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < _safeZones.Count; i++)
        {
            Zone zone = _safeZones[i];

            if (zone == null) continue;

            float distance = (zone.ClosestPoint(worldPosition) - worldPosition).sqrMagnitude;

            if (distance >= bestDistance) continue;

            best = zone;
            bestDistance = distance;
        }

        return best;
    }

    public static Color ColorFor(ZoneType type)
    {
        return type switch
        {
            ZoneType.Initial => new Color(0.35f, 0.85f, 0.45f),
            ZoneType.Hunting => new Color(0.95f, 0.82f, 0.32f),
            ZoneType.Safe => new Color(0.65f, 0.5f, 0.92f),
            ZoneType.Exit => new Color(0.3f, 0.78f, 0.92f),
            _ => new Color(0.65f, 0.65f, 0.65f),
        };
    }

    private void OnDrawGizmos()
    {
        Color color = ColorFor(_type);
        Vector3 size = new(_size.x, 0.2f, _size.y);

        Gizmos.color = new Color(color.r, color.g, color.b, 0.15f);
        Gizmos.DrawCube(new Vector3(_center.x, 0.1f, _center.z), size);

        Gizmos.color = color;
        Gizmos.DrawWireCube(new Vector3(_center.x, 0.1f, _center.z), size);
    }
}
