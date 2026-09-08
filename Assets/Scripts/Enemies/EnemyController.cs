using UnityEngine;

[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveForce = 10f;
    [Tooltip("Distancia máxima para detectar y perseguir a un jugador. Fuera de este rango, el enemigo no tiene objetivo (normalmente iría hacia la nave, pero esa mecánica aún no existe).")]
    public float detectionRange = 8f;

    [Header("Ataque por colisión")]
    public float collisionDamage = 10f;
    [Tooltip("Tiempo mínimo entre golpes al mismo objetivo mientras siguen en contacto.")]
    public float damageCooldown = 1f;

    private PlayerPhysics physics;
    private Health health;
    private Transform targetPlayer;
    private float lastDamageTime = -999f;

    private void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        health = GetComponent<Health>();
    }

    private void OnEnable() => health.OnDeath += HandleDeath;
    private void OnDisable() => health.OnDeath -= HandleDeath;

    private void FixedUpdate()
    {
        FindNearestPlayerInRange();

        if (targetPlayer == null) return;

        Vector3 toPlayer = targetPlayer.position - transform.position;
        toPlayer.z = 0f;

        if (toPlayer.magnitude > 0.01f)
        {
            physics.ApplyForce(toPlayer.normalized * moveForce);
        }
    }

    private void FindNearestPlayerInRange()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0)
        {
            targetPlayer = null;
            return;
        }

        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = p.transform;
            }
        }

        targetPlayer = (closestDist <= detectionRange) ? closest : null;
    }

    private void OnCollisionEnter(Collision collision) => TryDealDamage(collision.collider);
    private void OnCollisionStay(Collision collision) => TryDealDamage(collision.collider);

    private void TryDealDamage(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        Health targetHealth = other.GetComponent<Health>();
        if (targetHealth == null) return;

        targetHealth.TakeDamage(collisionDamage);
        lastDamageTime = Time.time;
    }

    private void HandleDeath()
    {
        Destroy(gameObject);
    }
}