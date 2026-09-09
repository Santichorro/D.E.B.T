using UnityEngine;

/// <summary>
/// Contrato mínimo para objetos que pueden recibir daño (enemigos, nave, objetos de valor).
/// Si ya tienes un script Health con otra firma, ajusta esta interfaz o haz que Health la implemente.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}

/// <summary>
/// Contrato de opt-in para mecanismos que deben reaccionar al pulso del Demoledor.
/// No todo Mechanism debe implementarlo: el documento pide evitar que el pulso
/// se vuelva una solución universal para puzzles.
/// </summary>
public interface IPulseReactive
{
    void OnPulse();
}

public interface IAbility
{
    void Activate(PlayerPhysics physics, Vector3 direction);
    float Cooldown { get; }
    bool IsReady { get; }
}

[RequireComponent(typeof(PlayerPhysics))]
public class PulseAbility : MonoBehaviour, IAbility
{
    [Header("Pulso de área")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float impulseForce = 12f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float cooldown = 4f;
    [Tooltip("Qué capas puede afectar el pulso (enemigos, mecanismos, etc.)")]
    [SerializeField] private LayerMask affectedLayers;

    private float cooldownTimer;

    public float Cooldown => cooldown;
    public bool IsReady => cooldownTimer <= 0f;

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    /// <summary>
    /// El Pulso ignora "direction" porque es un efecto radial, no direccional.
    /// Se mantiene el parámetro para conservar la firma común de IAbility.
    /// </summary>
    public void Activate(PlayerPhysics physics, Vector3 direction)
    {
        if (!IsReady) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, affectedLayers);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // no afectarse a sí mismo

            // Empuje: pasa por el sistema común de física DEL OBJETIVO, no del Demoledor.
            var targetPhysics = hit.GetComponent<PlayerPhysics>();
            if (targetPhysics != null)
            {
                Vector3 pushDir = (hit.transform.position - transform.position).normalized;
                targetPhysics.ApplyImpulse(pushDir * impulseForce);
            }

            // Daño: cualquier objeto que implemente IDamageable (enemigos, etc.)
            var damageable = hit.GetComponent<IDamageable>();
            damageable?.TakeDamage(damage);

            // Mecanismos: solo los que optan explícitamente por reaccionar al pulso.
            var reactive = hit.GetComponent<IPulseReactive>();
            reactive?.OnPulse();
        }

        cooldownTimer = cooldown;
    }

    // Ayuda visual en el editor para calibrar el radio.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}