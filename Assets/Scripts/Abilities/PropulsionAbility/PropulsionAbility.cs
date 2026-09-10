using System;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(GravityGun))]
public class PropulsionAbility : MonoBehaviour, IAbility
{
    [Header("Propulsión (valores de balance, sin cerrar todavía)")]
    [SerializeField] private float propulsionForce = 18f;
    [SerializeField] private float cooldown = 2.5f;

    [Header("Velocidad mínima garantizada")]
    [Tooltip("Si es > 0, tras propulsarse se garantiza al menos esta velocidad en la dirección del impulso. Pensado para el agujero negro artificial (12.3), que requiere una velocidad mínima de cruce.")]
    [SerializeField] private float guaranteedMinSpeed = 10f;

    [Header("Ataque de colisión")]
    [Tooltip("Ventana de tiempo tras propulsarse durante la que un choque contra un enemigo cuenta como ataque.")]
    [SerializeField] private float collisionAttackWindow = 0.35f;
    [Tooltip("Velocidad mínima del jugador para que el choque cuente como ataque (evita daño por un roce a baja velocidad).")]
    [SerializeField] private float minCollisionSpeed = 5f;
    [SerializeField] private float collisionDamage = 25f;
    [Tooltip("Layers que cuentan como enemigo para el ataque de colisión, igual que affectedLayers en PushAbility.")]
    [SerializeField] private LayerMask enemyLayers;

    /// <summary>Se dispara justo al propulsarse. Pensado para VFX/audio/cámara, desacoplado igual que OnAnchorAboutToEnd en PlayerPhysics.</summary>
    public event Action OnPropelled;

    private PlayerPhysics physics;
    private PlayerInput playerInput;
    private GravityGun aimSource;
    private NIS inputActions;
    private float lastActivationTime = -999f;
    private float collisionWindowEndTime = -999f;

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
        // Mismo patrón que PushAbility/AnchorAbility: reutiliza los devices ya
        // asignados a este PlayerInput para que la propulsión responda al mando/teclado
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

        // La propulsión es la fuerza CONTRARIA a la dirección de atracción/apuntado.
        Vector3 propulsionDir = -direction;

        casterPhysics.ApplyImpulse(propulsionDir * propulsionForce);

        if (guaranteedMinSpeed > 0f)
            EnsureMinimumSpeedAlong(casterPhysics, propulsionDir);

        collisionWindowEndTime = Time.time + collisionAttackWindow;

        OnPropelled?.Invoke();
    }

    private void EnsureMinimumSpeedAlong(PlayerPhysics casterPhysics, Vector3 direction)
    {
        float currentSpeedAlongDir = Vector3.Dot(casterPhysics.CurrentVelocity, direction);
        if (currentSpeedAlongDir < guaranteedMinSpeed)
        {
            float missing = guaranteedMinSpeed - currentSpeedAlongDir;
            casterPhysics.ApplyImpulse(direction * missing);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time > collisionWindowEndTime) return;
        if (physics.CurrentVelocity.magnitude < minCollisionSpeed) return;

        // Detección por layer (no por componente): igual criterio que affectedLayers
        // en PushAbility. Cualquier collider en enemyLayers cuenta como objetivo.
        int colliderLayer = collision.collider.gameObject.layer;
        if ((enemyLayers.value & (1 << colliderLayer)) == 0) return;

        Health enemyHealth = collision.collider.GetComponentInParent<Health>();
        if (enemyHealth != null)
            enemyHealth.TakeDamage(collisionDamage);

        collisionWindowEndTime = -999f; // Un solo impacto por propulsión.
    }
}