using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerPhysics))]
public class GravityGun : MonoBehaviour
{
    [Header("Disparo")]
    public GravityHook hookPrefab;
    public Transform muzzle;

    [Header("Aim")]
    public Camera aimCamera;

    [Header("Cable")]
    public float ropeLength = 6f;
    public float pullForce = 20f;
    public LineRenderer cableRenderer;

    [Header("Energía / Recarga")]
    public float maxEnergy = 100f;
    [Tooltip("Cuánta energía se consume por segundo mientras hay un gancho activo (volando, enganchado o regresando).")]
    public float energyDrainPerSecond = 25f;
    [Tooltip("Cuánta energía se recupera por segundo cuando NO hay gancho activo.")]
    public float energyRechargeRate = 15f;
    [Tooltip("Energía mínima requerida para poder disparar de nuevo.")]
    public float minEnergyToFire = 10f;

    private float currentEnergy;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyNormalized => currentEnergy / maxEnergy;

    [Header("Aim Visual")]
    [Tooltip("El objeto visual (ej. un círculo/sprite) que se mueve al punto exacto de apuntado.")]
    public Transform aimReticle;
    [Tooltip("Distancia MÁXIMA a la que puede llegar el reticle con el stick al fondo. Con el mouse no aplica: ahí el reticle sigue la posición real del cursor.")]
    public float maxAimDistance = 6f;

    [Header("Colores por jugador")]
    [Tooltip("Índice 0 = Player1, 1 = Player2, etc. Debe tener al menos 4 colores.")]
    public Color[] playerColors = new Color[]
    {
        Color.cyan,
        Color.yellow,
        Color.green,
        Color.magenta
    };

    private PlayerPhysics myPhysics;
    private Rigidbody myRigidbody;
    private GravityHook activeHook;
    private NIS inputActions;
    private PlayerInput playerInput;
    private Vector2 aimInput;
    private Camera sharedCamera;
    private Vector3 lastAimDirection = Vector3.right;
    private Vector3 currentAimPoint;
    private Renderer aimReticleRenderer;

    public Vector3 AimPoint => currentAimPoint;
    public Transform Muzzle => muzzle;
    
    public bool HasAttachedTarget =>
        activeHook != null && activeHook.State == GravityHook.HookState.Attached;

    public PlayerPhysics AttachedTargetPhysics =>
        HasAttachedTarget ? activeHook.TargetPhysics : null;

    public Rigidbody AttachedTargetRb =>
        HasAttachedTarget ? activeHook.TargetRb : null;

    private void Awake()
    {
        myPhysics = GetComponent<PlayerPhysics>();
        myRigidbody = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        sharedCamera = aimCamera != null ? aimCamera : Camera.main;

        if (muzzle != null)
            currentAimPoint = muzzle.position + lastAimDirection * maxAimDistance;

        currentEnergy = maxEnergy;
    }

    private void Start()
    {
        ApplyPlayerColor();
    }

    private void ApplyPlayerColor()
    {
        int index = playerInput.playerIndex;
        Color color = (index >= 0 && index < playerColors.Length)
            ? playerColors[index]
            : Color.white;

        if (aimReticle != null)
        {
            aimReticleRenderer = aimReticle.GetComponent<Renderer>();
            if (aimReticleRenderer != null)
            {
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                aimReticleRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", color);
                aimReticleRenderer.SetPropertyBlock(propBlock);
            }
        }

        if (cableRenderer != null)
        {
            cableRenderer.startColor = color;
            cableRenderer.endColor = color;
        }
    }

    private void OnEnable()
    {
        inputActions = new NIS();
        inputActions.devices = playerInput.devices;
        inputActions.Player.Enable();

        inputActions.Player.GravityGun.started += OnFireStarted;
        inputActions.Player.GravityGun.canceled += OnFireCanceled;

        inputActions.Player.Aim.performed += OnAim;
        inputActions.Player.Aim.canceled += OnAim;
    }

    private void OnDisable()
    {
        inputActions.Player.GravityGun.started -= OnFireStarted;
        inputActions.Player.GravityGun.canceled -= OnFireCanceled;
        inputActions.Player.Aim.performed -= OnAim;
        inputActions.Player.Aim.canceled -= OnAim;
        inputActions.Player.Disable();
    }

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (activeHook == null && currentEnergy >= minEnergyToFire)
            Fire();
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        if (activeHook != null)
            activeHook.BeginReturn();
    }

    private void OnAim(InputAction.CallbackContext ctx)
    {
        aimInput = ctx.ReadValue<Vector2>();
    }

    private void Update()
    {
        GetAimDirection();

        if (aimReticle != null)
            aimReticle.position = currentAimPoint;

        UpdateEnergy();
    }

    private void UpdateEnergy()
    {
        if (activeHook != null)
        {
            currentEnergy -= energyDrainPerSecond * Time.deltaTime;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                activeHook.BeginReturn();
            }
        }
        else
        {
            currentEnergy += energyRechargeRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
    }

    private void Fire()
    {
        Vector3 dir = GetAimDirection();
        GravityHook hook = Instantiate(hookPrefab, muzzle.position, Quaternion.identity);
        hook.Init(this, myRigidbody, dir);
        activeHook = hook;
    }

    private Vector3 GetAimDirection()
    {
        bool usandoMouse = playerInput.currentControlScheme == "Teclado_Mouse";

        if (usandoMouse)
        {
            Ray ray = sharedCamera.ScreenPointToRay(aimInput);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, muzzle.position.z));

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 worldPoint = ray.GetPoint(dist);
                Vector3 dir = worldPoint - muzzle.position;
                dir.z = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    lastAimDirection = dir.normalized;
                    worldPoint.z = muzzle.position.z;
                    currentAimPoint = worldPoint;
                    return lastAimDirection;
                }
            }
        }
        else
        {
            if (aimInput.sqrMagnitude > 0.01f)
            {
                Vector3 camRight = sharedCamera.transform.right;
                Vector3 camUp = sharedCamera.transform.up;
                camRight.z = 0f;
                camUp.z = 0f;

                Vector3 rawDir = (camRight.normalized * aimInput.x) + (camUp.normalized * aimInput.y);
                if (rawDir.sqrMagnitude > 0.01f)
                {
                    lastAimDirection = rawDir.normalized;

                    float stickStrength = Mathf.Clamp01(aimInput.magnitude);
                    float distance = stickStrength * maxAimDistance;

                    currentAimPoint = muzzle.position + lastAimDirection * distance;
                    return lastAimDirection;
                }
            }
        }

        return lastAimDirection;
    }

    public void OnHookAttached(GravityHook hook) { }

    public void OnHookReturned(GravityHook hook)
    {
        if (activeHook == hook)
            activeHook = null;

        if (cableRenderer != null)
            cableRenderer.enabled = false;
    }

    public void ForceReleaseHook()
    {
        if (activeHook != null)
        {
            activeHook.ForceRelease();
            activeHook = null;

            if (cableRenderer != null)
                cableRenderer.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (activeHook == null)
        {
            if (cableRenderer != null) cableRenderer.enabled = false;
            return;
        }

        Vector3 targetPoint;

        if (activeHook.State == GravityHook.HookState.Attached)
        {
            Vector3 attachWorldPos = activeHook.transform.position;
            Vector3 toPlayer = transform.position - attachWorldPos;

            Vector3 force = toPlayer.normalized * pullForce;

            if (activeHook.TargetPhysics != null)
                activeHook.TargetPhysics.ApplyForce(force);
            else
                activeHook.TargetRb.AddForce(force, ForceMode.Force);

            targetPoint = attachWorldPos;
        }
        else
        {
            targetPoint = activeHook.transform.position;
        }

        UpdateCableVisual(targetPoint);
    }

    private void UpdateCableVisual(Vector3 attachPoint)
    {
        if (cableRenderer == null) return;
        cableRenderer.enabled = true;
        cableRenderer.positionCount = 2;
        cableRenderer.SetPosition(0, muzzle.position);
        cableRenderer.SetPosition(1, attachPoint);
    }
}