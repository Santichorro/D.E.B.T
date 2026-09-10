using UnityEngine;

[RequireComponent(typeof(Health))]
public class ShipHealth : MonoBehaviour
{
    private Health health;

    public event System.Action OnShipDestroyed;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDeath += HandleShipDeath;
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleShipDeath;
    }

    private void HandleShipDeath()
    {
        OnShipDestroyed?.Invoke();
        Debug.Log("Nave destruida: el nivel debería reiniciarse aquí.");
    }
}