using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(GravityGun))]
public class AnchorAbility : MonoBehaviour, IAbility
{
    [Header("Anclaje (valores de balance, sin cerrar todavía)")]
    [SerializeField] private float range = 8f;
    [SerializeField] private float anchorDuration = 3f;
    [SerializeField] private float cooldown = 6f;
    [SerializeField] private LayerMask anchorableLayers;

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
        Debug.Log(">>> [AnchorAbility] OnAbilityInput detectado. Tecla presionada.");

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

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, anchorableLayers))
        {
            if (hit.collider.gameObject == casterPhysics.gameObject) return;

            if (hit.collider.TryGetComponent(out IAnchorable anchorable))
            {
                anchorable.Anchor(anchorDuration);
            }
        }
    }
}