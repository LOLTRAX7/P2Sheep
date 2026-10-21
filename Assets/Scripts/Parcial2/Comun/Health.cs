using System;
using UnityEngine;

// Vida de un agente. Está separada de BoidAgent / HunterAgent (devolución del Parcial 1: una clase, una responsabilidad).
// Quien necesite reaccionar a un daño o a una muerte se suscribe a los eventos.
public class Health : MonoBehaviour
{
    public event Action<float> OnDamaged = delegate { };
    public event Action OnDied = delegate { };

    [SerializeField] private float _maxHealth = 100f;

    public float Max => _maxHealth;
    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    // Vida actual entre 0 y 1.
    public float Normalized => _maxHealth > 0f ? Current / _maxHealth : 0f;

    private void Awake()
    {
        Current = _maxHealth;
    }

    // Define la vida máxima y la deja llena.
    public void Initialize(float maxHealth)
    {
        _maxHealth = maxHealth;
        ResetHealth();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        OnDamaged?.Invoke(amount);

        if (Current > 0f) return;

        IsDead = true;
        OnDied?.Invoke();
    }

    // Vuelve a llenar la vida y revive (respawn).
    public void ResetHealth()
    {
        Current = _maxHealth;
        IsDead = false;
    }
}
