using UnityEngine;

[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveForce = 10f;

    [Header("Objetivo: nave (prioridad por defecto)")]
    [Tooltip("Si lo dejás vacío, busca un objeto con tag 'Ship' apenas lo necesite.")]
    public Transform naveTransform;
    public float shipAggroRange = 30f;

    [Header("Objetivo: jugador cercano (tiene prioridad sobre la nave)")]
    [Tooltip("Si un jugador está a esta distancia o menos, el enemigo lo persigue a él en vez de ir hacia la nave. Fuera de esta distancia, siempre va hacia la nave sin importar qué tan lejos esté.")]
    public float playerAggroRange = 8f;

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

        Transform objetivo = ElegirObjetivo();
        if (objetivo == null) return;

        Vector3 haciaObjetivo = objetivo.position - transform.position;
        haciaObjetivo.z = 0f;

        if (haciaObjetivo.magnitude > 0.01f)
        {
            physics.ApplyForce(haciaObjetivo.normalized * moveForce);
        }
    }

    /// <summary>
    /// Prioridad: jugador cercano (dentro de playerAggroRange) > nave, sin importar
    /// qué tan lejos esté esta última.
    /// </summary>
    private Transform ElegirObjetivo()
    {
        Transform jugadorCercano = BuscarJugadorCercanoDentroDelRango();
        if (jugadorCercano != null) return jugadorCercano;

        if (naveTransform == null) return null;

        float distNave = Vector3.Distance(transform.position, naveTransform.position);
        return (distNave <= shipAggroRange) ? naveTransform : null;
    }

    private Transform BuscarJugadorCercanoDentroDelRango()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return null;

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

        return (closestDist <= playerAggroRange) ? closest : null;
    }

    private void BuscarNave()
    {
        GameObject nave = GameObject.FindGameObjectWithTag("Ship");
        if (nave != null)
            naveTransform = nave.transform;
    }

    public void SetGrabbed(bool grabbed)
    {
        IsGrabbed = grabbed;
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

    private void HandleDeath()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerAggroRange);
    }
}