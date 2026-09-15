using UnityEngine;

[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveForce = 10f;

    [Header("Objetivo: nave")]
    [Tooltip("Si queda vacío, toma la nave activa de la escena y, como respaldo, busca el tag 'Ship'.")]
    public Transform naveTransform;

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

    /// <summary>El movimiento de los enemigos tiene como único objetivo la nave.</summary>
    private Transform ElegirObjetivo()
    {
        // Una referencia guardada en un prefab puede apuntar al asset de la nave
        // y no a su instancia en juego. Solo aceptamos transforms de una escena cargada.
        if (!NaveEnEscenaEsValida())
            BuscarNave();

        return naveTransform; // Puede ser null si todavía no existe la nave en la escena.
    }

    private bool NaveEnEscenaEsValida()
    {
        return naveTransform != null &&
               naveTransform.gameObject.scene.IsValid() &&
               naveTransform.gameObject.scene.isLoaded;
    }

    private void BuscarNave()
    {
        if (ShipController.ActiveTetherShip != null)
        {
            naveTransform = ShipController.ActiveTetherShip.transform;
            return;
        }

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
}
