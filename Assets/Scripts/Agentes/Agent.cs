using UnityEngine;

// Clase base de los agentes de Steering. Guarda la velocidad para que los
// comportamientos derivados puedan mover al agente y consultar su movimiento.
public class Agent : MonoBehaviour
{
    protected Vector3 _velocity;

    // Permite que otros scripts consulten la velocidad actual sin modificarla.
    // Se usa, por ejemplo, para calcular Pursuit, Evade o Flocking.
    public Vector3 Velocity => _velocity;

    // Adaptación de CalculateSteering / Seek / Arrive de Clase 3.
    // Se aplica tanto al próximo nodo A* como al tramo final hacia un objetivo.
    protected void MoveWithSteering(Vector3 target, float speed, float maxSteering,
        float slowingDistance, bool avoidSafeZones = false)
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance < 0.02f)
        {
            _velocity = Vector3.zero;
            return;
        }

        float desiredSpeed = speed * Mathf.Clamp01(distance / Mathf.Max(slowingDistance, 0.02f));
        Vector3 desired = offset / distance * desiredSpeed;
        _velocity += Vector3.ClampMagnitude(desired - _velocity, maxSteering * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity, speed);
        Vector3 step = _velocity * Time.deltaTime;
        if (step.magnitude > distance) step = step.normalized * distance;

        Graph graph = Graph.Current;
        if (graph != null && !graph.CanTravel(transform.position, transform.position + step, avoidSafeZones))
        {
            // La inercia no puede cortar una esquina ni cruzar un refugio.
            _velocity = Vector3.zero;
            return;
        }

        transform.position += step;
    }

    // Si el objeto tiene Rigidbody, lo deja en modo Kinematic para que no
    // compita con el movimiento manual por transform.position.
    protected void DisablePhysicsInterference()
    {
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
