using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(GravityGun))]
public class PushAbility : MonoBehaviour, IAbility
{
    [Header("Empuje (valores de balance, sin cerrar todavía)")]
    [SerializeField] private float range = 10f;
    [SerializeField] private float pushRadius = 0.5f;
    [SerializeField] private float pushForce = 15f;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private LayerMask affectedLayers;
    

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    private PlayerPhysics physics;
    private PlayerInput playerInput;
    private GravityGun aimSource;
    private NIS inputActions;
    private float lastActivationTime = -999f;

    public float Cooldown => cooldown;
    public bool IsReady => Time.time - lastActivationTime >= cooldown;

    private void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        playerInput = GetComponent<PlayerInput>();
        aimSource = GetComponent<GravityGun>();
        inputActions = new NIS();
    }

    private void OnEnable()
    {
        // Mismo patrón que PulseAbility/AnchorAbility: reutiliza los devices ya
        // asignados a este PlayerInput para que el empuje responda al mando/teclado
        // correcto de este jugador específico.
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
        Vector3 direction = aimSource.AimPoint - aimSource.Muzzle.position;
        direction.z = 0f;

        // Fallback si el jugador aún no movió el aim en este frame.
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.right;

        Activate(physics, direction.normalized);
    }

    public void Activate(PlayerPhysics casterPhysics, Vector3 direction)
    {
        if (!IsReady) return;
        lastActivationTime = Time.time;

        Vector3 origin = aimSource.Muzzle != null
            ? aimSource.Muzzle.position
            : casterPhysics.transform.position;

        if (Physics.SphereCast(origin, pushRadius, direction, out RaycastHit hit, range, affectedLayers))
        {
            // Nunca afectarse a sí mismo.
            if (hit.collider.gameObject == casterPhysics.gameObject) return;

            ApplyPush(hit.collider, direction);
        }
    }

    private void ApplyPush(Collider target, Vector3 direction)
    {
        // Caso 1: jugador o enemigo. Ambos pasan siempre por PlayerPhysics
        // (EnemyController lo requiere igual que cualquier jugador), así que
        // compañero, enemigo-contra-pared y enemigo-contra-enemigo quedan
        // cubiertos por esta misma línea sin distinguir entre ellos.
        if (target.TryGetComponent(out PlayerPhysics targetPhysics))
        {
            targetPhysics.ApplyImpulse(direction * pushForce);
            return;
        }

        // Caso 2: objeto genérico sin PlayerPhysics (caja, prop). Fallback simple
        // mientras no exista un sistema de físicas propio para objetos del escenario.
        if (target.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(direction * pushForce, ForceMode.Impulse);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        Vector3 origin = aimSource != null && aimSource.Muzzle != null
            ? aimSource.Muzzle.position
            : transform.position;
        Vector3 direction = aimSource != null
            ? (aimSource.AimPoint - origin).normalized
            : transform.right;

        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(origin, pushRadius);
        Gizmos.DrawLine(origin, origin + direction * range);
        Gizmos.DrawWireSphere(origin + direction * range, pushRadius);
    }
}