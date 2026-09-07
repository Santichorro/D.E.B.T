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

    private PlayerPhysics myPhysics;
    private GravityHook currentHook;
    private NIS inputActions;
    private PlayerInput playerInput;
    private Vector2 aimInput;

    private Camera sharedCamera;

    private void Awake()
    {
        myPhysics = GetComponent<PlayerPhysics>();
        playerInput = GetComponent<PlayerInput>();
        sharedCamera = Camera.main;
    }

    private void OnEnable()
    {
        inputActions = new NIS();
        inputActions.devices = playerInput.devices;
        inputActions.Player.Enable();

        inputActions.Player.GravityGun.performed += OnFirePressed;

        inputActions.Player.Aim.performed += OnAim;
        inputActions.Player.Aim.canceled += OnAim;
    }

    private void OnDisable()
    {
        inputActions.Player.GravityGun.performed -= OnFirePressed;
        inputActions.Player.Aim.performed -= OnAim;
        inputActions.Player.Aim.canceled -= OnAim;
        inputActions.Player.Disable();
    }

    private void OnAim(InputAction.CallbackContext ctx)
    {
        aimInput = ctx.ReadValue<Vector2>();
    }

    private void OnFirePressed(InputAction.CallbackContext ctx)
    {
        if (currentHook != null) Release();
        else Fire();
    }

    private void Fire()
    {
        Vector3 dir = GetAimDirection();
        GravityHook hook = Instantiate(hookPrefab, muzzle.position, Quaternion.identity);
        hook.Init(this, dir);
    }

    /// <summary>
    /// Calcula la dirección de disparo según el dispositivo activo:
    /// - Mouse: aimInput es una posición de pantalla, se proyecta al plano del jugador.
    /// - Gamepad: aimInput ya es una dirección (-1..1), se usa directo.
    /// </summary>
    private Vector3 GetAimDirection()
    {
        bool usandoMouse = playerInput.currentControlScheme == "Keyboard_Mouse";

        if (usandoMouse)
        {
            Ray ray = sharedCamera.ScreenPointToRay(aimInput);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 worldPoint = ray.GetPoint(dist);
                Vector3 dir = worldPoint - muzzle.position;
                dir.z = 0f;
                if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            }
            return transform.right;
        }
        else
        {
            if (aimInput.sqrMagnitude < 0.01f) return transform.right;
            return new Vector3(aimInput.x, aimInput.y, 0f).normalized;
        }
    }

    public void OnHookAttached(GravityHook hook) => currentHook = hook;

    private void Release()
    {
        if (currentHook != null)
        {
            Destroy(currentHook.gameObject);
            currentHook = null;
        }
        if (cableRenderer != null) cableRenderer.enabled = false;
    }

    private void FixedUpdate()
    {
        if (currentHook == null) return;

        Vector3 attachWorldPos = currentHook.TargetRb.transform.TransformPoint(currentHook.LocalAttachPoint);
        Vector3 toPlayer = transform.position - attachWorldPos;
        float distance = toPlayer.magnitude;

        if (distance > ropeLength)
        {
            Vector3 force = toPlayer.normalized * pullForce;

            if (currentHook.TargetPhysics != null)
                currentHook.TargetPhysics.ApplyForce(force);
            else
                currentHook.TargetRb.AddForce(force, ForceMode.Force);
        }

        UpdateCableVisual(attachWorldPos);
    }

    private void UpdateCableVisual(Vector3 attachPoint)
    {
        if (cableRenderer == null) return;
        cableRenderer.enabled = true;
        cableRenderer.SetPosition(0, muzzle.position);
        cableRenderer.SetPosition(1, attachPoint);
    }
}