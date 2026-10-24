using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidAgent : Agent
{
    [Header("Movimiento")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float maxForce = 6f;

    [Header("Flocking")]
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float neighborRadius = 5f;
    [SerializeField, Range(0f, 5f)] private float separationWeight = 1.6f;
    [SerializeField, Range(0f, 5f)] private float alignmentWeight = 1f;
    [SerializeField, Range(0f, 5f)] private float cohesionWeight = 1f;

    [Header("Evade")]
    [SerializeField] private float hunterVisionRadius = 6f;
    [SerializeField, Range(0f, 5f)] private float evadeWeight = 2.2f;
    [Tooltip("Margen extra que se suma a Hunter Vision Radius mientras ya se está evadiendo, para no perder y recuperar la amenaza en el mismo frame cuando la distancia queda justo en el borde (evita temblores). Ajustable: si el boid sigue temblando al huir, subilo un poco más.")]
    [SerializeField] private float threatLossMargin = 2f;
    [Range(0f, 1f), Tooltip("Fracción del peso normal de Separation que se mantiene mientras se evade (no cero, para que el grupo no se superponga al huir).")]
    [SerializeField] private float separationWeightWhileEvading = 0.35f;

    [Header("Arrive")]
    [SerializeField] private float poiVisionRadius = 10f;
    [SerializeField] private float arriveSlowingDistance = 3f;
    [SerializeField] private float arriveMinDistance = 1.2f;
    [SerializeField, Range(0f, 5f)] private float arriveWeight = 1.4f;
    [SerializeField] private float harvestInterval = 1f;
    [SerializeField] private float harvestDamage = 10f;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Feedback visual")]
    [SerializeField] private Renderer feedbackRenderer;
    [SerializeField] private Color evadeColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color harvestColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color eliminatedColor = Color.gray;

    // Cada Boid guarda sólo los resultados de su consulta de percepción.
    private readonly Collider[] _queryBuffer = new Collider[128];

    private Health _health;
    private ColorFeedback _colorFeedback;
    private readonly List<BoidAgent> _neighbors = new List<BoidAgent>();
    private InterestPoint _targetPoi;
    private HunterAgent _threat;
    private float _harvestTimer;
    private Collider _collider;
    private float _spawnHeight;

    public bool IsEliminated { get; private set; }

    // Vida actual entre 0 y 1 (el Cazador la usa para atacar al Boid más herido).
    public float HealthNormalized => _health != null ? _health.Normalized : 1f;

    // Distingue "murió y espera a que el cazador lo recolecte" de "ya fue
    // recolectado y está en cuenta regresiva para reaparecer". Ver OnGathered().
    public bool IsPendingGather { get; private set; }

    private void Awake()
    {
        _spawnHeight = transform.position.y;
        GameLayers.SetLayer(gameObject, GameLayers.Boid);
        // Evita que un Rigidbody sin configurar pelee contra el movimiento manual.
        DisablePhysicsInterference();

        // Los demás agentes lo perciben con OverlapSphere: necesita un collider.
        _collider = GetComponent<Collider>();

        if (_collider == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
            _collider = sphere;
        }

        // La vida y el color están en componentes aparte (Health y ColorFeedback).
        _health = gameObject.GetOrAddComponent<Health>();
        _health.Initialize(maxHealth);

        if (feedbackRenderer == null) feedbackRenderer = GetComponentInChildren<Renderer>();

        _colorFeedback = gameObject.GetOrAddComponent<ColorFeedback>();
        _colorFeedback.SetRenderer(feedbackRenderer);

        _velocity = RandomDirection() * maxSpeed;
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDied -= HandleDied;
    }

    private Color OriginalColor => _colorFeedback != null ? _colorFeedback.BaseColor : Color.white;

    private void Update()
    {
        if (IsEliminated) return;

        GatherNeighbors();
        DetectThreat();
        bool hasThreat = _threat != null;

        // Mientras hay amenaza, Evade tiene prioridad sobre Separation,
        // Alignment y Cohesion: estas dos últimas se apagan del todo y
        // Separation se reduce a una fracción chica (no a cero).
        float currentSeparationWeight = hasThreat ? separationWeight * separationWeightWhileEvading : separationWeight;
        float currentAlignmentWeight = hasThreat ? 0f : alignmentWeight;
        float currentCohesionWeight = hasThreat ? 0f : cohesionWeight;

        Vector3 steering = CalculateSeparation() * currentSeparationWeight
                          + CalculateAlignment() * currentAlignmentWeight
                          + CalculateCohesion() * currentCohesionWeight;

        if (hasThreat)
        {
            
            steering += Evade(_threat) * evadeWeight;
            SetFeedback(evadeColor);
        }
        else
        {
            UpdatePoiTarget();

            if (_targetPoi != null)
            {
                
                steering += Arrive(_targetPoi.transform.position) * arriveWeight;
                TryHarvest();
                SetFeedback(harvestColor);
            }
            else
            {
                SetFeedback(OriginalColor);
            }
        }

        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);
        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.0001f)
            transform.forward = _velocity.normalized;

        if (Bounds.Instance != null)
        {
            Vector3 position = transform.position;
            Bounds.Instance.Constrain(ref position, ref _velocity);
            transform.position = position;
        }
    }

    // Percepción 

    private void DetectThreat()
    {
        // Histéresis: si ya se estaba evadiendo, exige un poco más de
        // distancia antes de dar por terminada la amenaza (evita temblores).
        float effectiveRadius = _threat != null ? hunterVisionRadius + threatLossMargin : hunterVisionRadius;

        _threat = null;
        float nearestDist = effectiveRadius;

        // Parcial 2: percibe sólo lo que está dentro de su radio de visión (OverlapSphere), no una lista de todos los cazadores.
        int count = Physics.OverlapSphereNonAlloc(transform.position, effectiveRadius, _queryBuffer, GameLayers.HunterMask);

        for (int i = 0; i < count; i++)
        {
            HunterAgent hunter = _queryBuffer[i].GetComponentInParent<HunterAgent>();
            if (hunter == null || hunter.IsDead) continue;

            float dist = Vector3.Distance(transform.position, hunter.transform.position);
            if (dist <= nearestDist)
            {
                _threat = hunter;
                nearestDist = dist;
            }
        }
    }

    // Parcial 2: junta los Boids que tiene cerca (dentro de Neighbor Radius) con OverlapSphere.
    // Separation, Alignment y Cohesion trabajan sobre esta lista en lugar de recorrer todos los Boids de la escena.
    private void GatherNeighbors()
    {
        _neighbors.Clear();

        int count = Physics.OverlapSphereNonAlloc(transform.position, neighborRadius, _queryBuffer, GameLayers.BoidMask);

        for (int i = 0; i < count; i++)
        {
            BoidAgent other = _queryBuffer[i].GetComponentInParent<BoidAgent>();

            if (other != null && other != this && !_neighbors.Contains(other))
                _neighbors.Add(other);
        }
    }

    private void UpdatePoiTarget()
    {
        
        if (_targetPoi == null)
        {
            _targetPoi = FindNearestInterestPoint();
            _harvestTimer = 0f;
        }
    }

    private InterestPoint FindNearestInterestPoint()
    {
        InterestPoint nearest = null;
        float nearestDist = poiVisionRadius;

        int count = Physics.OverlapSphereNonAlloc(transform.position, poiVisionRadius, _queryBuffer, GameLayers.InterestMask);

        for (int i = 0; i < count; i++)
        {
            InterestPoint poi = _queryBuffer[i].GetComponentInParent<InterestPoint>();
            if (poi == null) continue;

            // Parcial 2: los Boids permanecen en el Coto; ignoran objetos de interés que quedaron fuera de su área.
            if (Bounds.Instance != null && !Bounds.Instance.Contains(poi.transform.position)) continue;

            float dist = Vector3.Distance(transform.position, poi.transform.position);
            if (dist <= nearestDist)
            {
                nearest = poi;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    private void TryHarvest()
    {
        float dist = Vector3.Distance(transform.position, _targetPoi.transform.position);

        
        if (dist > arriveMinDistance + 0.3f)
        {
            _harvestTimer = 0f;
            return;
        }

        _harvestTimer += Time.deltaTime;
        if (_harvestTimer >= harvestInterval)
        {
            _harvestTimer = 0f;
            _targetPoi.TakeDamage(harvestDamage);
        }
    }


    // Vida / eliminación / reaparición 


    public void TakeDamage(float amount)
    {
        if (IsEliminated) return;

        Debug.Log($"[{name}] Recibe {amount} de daño. Vida: {Mathf.Max(_health.Current - amount, 0f)}/{maxHealth}.");

        // La vida la maneja el componente Health; si llega a 0 dispara OnDied y se ejecuta HandleDied().
        _health.TakeDamage(amount);
    }

    private void HandleDied()
    {
        IsEliminated = true;
        IsPendingGather = true; // recién murió: todavía no lo recolectaron
        _velocity = Vector3.zero;
        SetFeedback(eliminatedColor);
        Debug.Log($"[{name}] Eliminado. Queda inactivo hasta que el cazador lo recolecte.");
    }

    // Llamado por HunterGatherState al completar la recolección
    public void OnGathered(float respawnDelay)
    {
        if (!IsEliminated || !IsPendingGather) return;
        // Apenas se recolecta, deja de estar "pendiente de recolección"
        // (así el cazador no lo vuelve a detectar como recolectable).
        IsPendingGather = false;
        Debug.Log($"[{name}] Recolectado por el cazador. Reaparece en {respawnDelay:0.0}s.");
        SetVisible(false);
        StartCoroutine(RespawnRoutine(respawnDelay));
    }

    private IEnumerator RespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (Bounds.Instance != null)
        {
            Vector3 spawn = Bounds.Instance.GetRandomPointInside();
            spawn.y = _spawnHeight;
            transform.position = spawn;
        }

        _health.ResetHealth();
        IsEliminated = false;
        IsPendingGather = false;
        _targetPoi = null;
        _threat = null;
        _harvestTimer = 0f;
        _velocity = RandomDirection() * maxSpeed;

        SetVisible(true);
        SetFeedback(OriginalColor);
        Debug.Log($"[{name}] Reapareció en {transform.position}.");
    }

    private Vector3 RandomDirection()
    {
        Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        return dir.sqrMagnitude < 0.0001f ? Vector3.forward : dir.normalized;
    }

    private void SetVisible(bool visible)
    {
        if (feedbackRenderer != null) feedbackRenderer.enabled = visible;
        if (_collider != null) _collider.enabled = visible;
    }

    private void SetFeedback(Color color)
    {
        if (_colorFeedback != null) _colorFeedback.SetColor(color);
    }


    // Steering 
    

    private Vector3 DesiredVelocity(Vector3 target)
    {
        Vector3 desired = target - transform.position;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) return Vector3.zero;
        return desired.normalized * maxSpeed;
    }

    private Vector3 SteeringFromDesired(Vector3 desired)
    {
        Vector3 steering = desired - _velocity;
        return Vector3.ClampMagnitude(steering, maxForce * Time.deltaTime);
    }

    private Vector3 Flee(Vector3 target) => SteeringFromDesired(-DesiredVelocity(target));

    private Vector3 Arrive(Vector3 target)
    {
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance <= arriveMinDistance)
        {
            // Frena activamente hacia velocidad cero (sin esto, la inercia lo
            // arrastraba y terminaba superponiéndose con el objeto de interés).
            return SteeringFromDesired(Vector3.zero);
        }

        float speed = maxSpeed * Mathf.Clamp01(distance / arriveSlowingDistance);
        Vector3 desired = toTarget.normalized * speed;
        return SteeringFromDesired(desired);
    }

    private Vector3 CalculateFuturePosition(Agent target)
    {
        Vector3 toTarget = target.transform.position - transform.position;
        float prediction = toTarget.magnitude / (maxSpeed + target.Velocity.magnitude + 0.001f);
        return target.transform.position + target.Velocity * prediction;
    }

    private Vector3 Evade(Agent target) => Flee(CalculateFuturePosition(target));

    private Vector3 CalculateSeparation()
    {
        
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var other in _neighbors)
        {
            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist <= separationRadius)
            {
                Vector3 away = transform.position - other.transform.position;
                // Dos agentes exactamente superpuestos también deben separarse.
                if (dist < 0.0001f)
                    away = GetInstanceID() < other.GetInstanceID() ? Vector3.left : Vector3.right;
                sum += away.normalized / Mathf.Max(dist, 0.1f);
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        return SteeringFromDesired((sum / count).normalized * maxSpeed);
    }

    private Vector3 CalculateAlignment()
    {
        
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var other in _neighbors)
        {
            if (other.IsEliminated) continue;

            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist <= neighborRadius)
            {
                sum += other.Velocity.normalized;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        return SteeringFromDesired((sum / count).normalized * maxSpeed);
    }

    private Vector3 CalculateCohesion()
    {
    
        Vector3 centroid = Vector3.zero;
        int count = 0;

        foreach (var other in _neighbors)
        {
            if (other.IsEliminated) continue;

            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist <= neighborRadius)
            {
                centroid += other.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        centroid /= count;
        return SteeringFromDesired((centroid - transform.position).normalized * maxSpeed);
    }

    // ---------- Utilidades de Editor ----------

    private void OnValidate()
    {
        if (separationRadius >= neighborRadius)
        {
            Debug.LogWarning($"[{name}] Separation Radius ({separationRadius}) debería ser MENOR que Neighbor Radius ({neighborRadius}), tal como pide la consigna.", this);
        }
    }

    private void OnDrawGizmos()
    {
        // Siempre visibles (consigna del P2): radios de Separation, Neighbor y visión del Cazador.
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, hunterVisionRadius);

        Gizmos.color = new Color(0.7f, 1f, 0.25f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, poiVisionRadius);
    }
}
