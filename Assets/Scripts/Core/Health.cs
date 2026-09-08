using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;

    private float currentHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    public event Action<float> OnDamaged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth -= amount;
        OnDamaged?.Invoke(amount);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead = false;
    }
}