using UnityEngine;

// Clase base de los agentes de Steering. Guarda la velocidad para que los
// comportamientos derivados puedan mover al agente y consultar su movimiento.
public class Agent : MonoBehaviour
{
    protected Vector3 _velocity;

    // Permite que otros scripts consulten la velocidad actual sin modificarla.
    // Se usa, por ejemplo, para calcular Pursuit, Evade o Flocking.
    public Vector3 Velocity => _velocity;

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
