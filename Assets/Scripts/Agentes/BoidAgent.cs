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

    public static readonly List<BoidAgent> All = new List<BoidAgent>();

    private float _currentHealth;
    private InterestPoint _targetPoi;
    private HunterAgent _threat;
    private float _harvestTimer;
    private Collider _collider;
    private Color _originalColor;

    public bool IsEliminated { get; private set; }

    // Distingue "murió y espera a que el cazador lo recolecte" de "ya fue
    // recolectado y está en cuenta regresiva para reaparecer". Ver OnGathered().
    public bool IsPendingGather { get; private set; }

    private void Awake()
    {
        // Evita que un Rigidbody sin configurar pelee contra el movimiento manual.
        DisablePhysicsInterference();

        _currentHealth = maxHealth;
        _collider = GetComponent<Collider>();
        _velocity = RandomDirection() * maxSpeed;

        if (feedbackRenderer != null) _originalColor = feedbackRenderer.material.color;
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void Update()
    {
        if (IsEliminated) return;

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
                SetFeedback(_originalColor);
            }
        }

        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);
        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.0001f)
            transform.forward = _velocity.normalized;

        if (Bounds.Instance != null)
            transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }

    // Percepción 

    private void DetectThreat()
    {
        // Histéresis: si ya se estaba evadiendo, exige un poco más de
        // distancia antes de dar por terminada la amenaza (evita temblores).
        float effectiveRadius = _threat != null ? hunterVisionRadius + threatLossMargin : hunterVisionRadius;

        _threat = null;
        float nearestDist = effectiveRadius;

        foreach (var hunter in HunterAgent.All)
        {
            float dist = Vector3.Distance(transform.position, hunter.transform.position);
            if (dist <= nearestDist)
            {
                _threat = hunter;
                nearestDist = dist;
            }
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

        foreach (var poi in InterestPoint.ActivePoints)
        {
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

        _currentHealth -= amount;

        Debug.Log($"[{name}] Recibe {amount} de daño. Vida: {Mathf.Max(_currentHealth, 0f)}/{maxHealth}.");

        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;
            IsEliminated = true;
            IsPendingGather = true; // recién murió: todavía no lo recolectaron
            _velocity = Vector3.zero;
            SetFeedback(eliminatedColor);
            Debug.Log($"[{name}] Eliminado. Queda inactivo hasta que el cazador lo recolecte.");
        }
    }

    // Llamado por HunterGatherState al completar la recolección
    public void OnGathered(float respawnDelay)
    {
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
            transform.position = Bounds.Instance.GetRandomPointInside();

        _currentHealth = maxHealth;
        IsEliminated = false;
        IsPendingGather = false;
        _targetPoi = null;
        _threat = null;
        _harvestTimer = 0f;
        _velocity = RandomDirection() * maxSpeed;

        SetVisible(true);
        SetFeedback(_originalColor);
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
        if (feedbackRenderer != null) feedbackRenderer.material.color = color;
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

        foreach (var other in All)
        {
            if (other == this) continue;

            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist > 0.0001f && dist <= separationRadius)
            {
                sum += transform.position - other.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        return sum / count;
    }

    private Vector3 CalculateAlignment()
    {
        
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var other in All)
        {
            if (other == this || other.IsEliminated) continue;

            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist <= neighborRadius)
            {
                sum += other.Velocity.normalized;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        return sum / count;
    }

    private Vector3 CalculateCohesion()
    {
    
        Vector3 centroid = Vector3.zero;
        int count = 0;

        foreach (var other in All)
        {
            if (other == this || other.IsEliminated) continue;

            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist <= neighborRadius)
            {
                centroid += other.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        centroid /= count;
        return (centroid - transform.position).normalized;
    }

    // ---------- Utilidades de Editor ----------

    private void OnValidate()
    {
        if (separationRadius >= neighborRadius)
        {
            Debug.LogWarning($"[{name}] Separation Radius ({separationRadius}) debería ser MENOR que Neighbor Radius ({neighborRadius}), tal como pide la consigna.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position, hunterVisionRadius);
    }
}