using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerInput))]
public class PulseAbility : MonoBehaviour, IAbility
{
    [Header("Pulso (valores de balance, sin cerrar todavía)")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float damage = 30f;
    [SerializeField] private float impulseForce = 12f;
    [SerializeField] private float cooldown = 4f;
    [SerializeField] private LayerMask affectedLayers;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    private PlayerPhysics physics;
    private PlayerInput playerInput;
    private NIS inputActions;
    private float lastActivationTime = -999f;

    public float Cooldown => cooldown;
    public bool IsReady => Time.time - lastActivationTime >= cooldown;

    [Header("Visual")]
    [SerializeField] private GameObject visiblePulseObject;
    [SerializeField] private float visualDuration = 1f;

private Coroutine visualRoutine;

    private void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        playerInput = GetComponent<PlayerInput>();
        inputActions = new NIS();
    }

    private void OnEnable()
    {
        // Mismo patrón que PlayerController: reutiliza los devices ya asignados a este PlayerInput
        // para que el pulso responda al mando/teclado correcto de este jugador específico.
        inputActions.devices = playerInput.devices;

        inputActions.Player.Enable();
        inputActions.Player.Ability.performed += OnAbilityInput;
    }

    private void OnDisable()
    {
        inputActions.Player.Ability.performed -= OnAbilityInput;
        inputActions.Player.Disable();
    }

    private void OnAbilityInput(InputAction.CallbackContext ctx)
    {
        Debug.Log("Ability input detectado"); // TEMPORAL

        // El Pulso es de área, así que la dirección no se usa; se pasa Vector3.zero
        // únicamente para cumplir con el contrato común de IAbility.
        Activate(physics, Vector3.zero);
    }

    public void Activate(PlayerPhysics casterPhysics, Vector3 direction)
    {
        Debug.Log($"Activate llamado. IsReady={IsReady}"); 
        if (!IsReady) return;

        lastActivationTime = Time.time;

        Vector3 origin = casterPhysics.transform.position;
        if (visiblePulseObject != null)
        {
        
            if (visualRoutine != null)
                StopCoroutine(visualRoutine);

            visualRoutine = StartCoroutine(ShowVisualPulse());
        }

        Collider[] hits = Physics.OverlapSphere(origin, radius, affectedLayers);

        foreach (var hit in hits)
        {
            // Nunca afectarse a sí mismo.
            if (hit.gameObject == casterPhysics.gameObject) continue;

            // Daño: referencia directa a Health (sin interfaz IDamageable, para no tocar ese script).
            if (hit.TryGetComponent(out Health health))
            {
                health.TakeDamage(damage);
            }

            // Reacción opt-in sin daño: mecanismos que deben desactivarse temporalmente.
            if (hit.TryGetComponent(out IPulseReactive reactive))
            {
                reactive.OnPulseHit(origin, impulseForce);
            }

            // Empuje físico: siempre a través de PlayerPhysics del objetivo, nunca tocando su Rigidbody directo.
            if (hit.TryGetComponent(out PlayerPhysics targetPhysics) && targetPhysics != casterPhysics)
            {
                Vector3 pushDir = hit.transform.position - origin;
                pushDir.z = 0f;

                if (pushDir.sqrMagnitude > 0.0001f)
                {
                    targetPhysics.ApplyImpulse(pushDir.normalized * impulseForce);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private System.Collections.IEnumerator ShowVisualPulse()
    {
        visiblePulseObject.SetActive(true);
        yield return new WaitForSeconds(visualDuration);
        visiblePulseObject.SetActive(false);
    }
}
