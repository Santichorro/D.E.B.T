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
    public float damageCooldown = 1f;

    [Header("Daño por impacto (al ser lanzado contra algo)")]
    [Tooltip("Velocidad relativa mínima del choque para que el impacto haga daño.")]
    public float minImpactSpeed = 6f;

    [Tooltip("Daño = velocidad de impacto x este multiplicador.")]
    public float impactDamageMultiplier = 8f;

    public bool IsGrabbed { get; private set; }

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
        if (IsGrabbed) return;

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

    public void SetGrabbed(bool grabbed)
    {
        IsGrabbed = grabbed;
        if (grabbed)
            targetPlayer = null;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            TryDealDamageToPlayer(collision.collider);
        }
        else
        {
            TryTakeImpactDamage(collision);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
            TryDealDamageToPlayer(collision.collider);
    }

    private void TryDealDamageToPlayer(Collider other)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        Health targetHealth = other.GetComponent<Health>();
        if (targetHealth == null) return;

        targetHealth.TakeDamage(collisionDamage);
        lastDamageTime = Time.time;
    }

    private void TryTakeImpactDamage(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed) return;

        float damage = impactSpeed * impactDamageMultiplier;
        health.TakeDamage(damage);
    }

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