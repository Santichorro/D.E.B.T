using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento (fuerzas)")]
    public float thrustForce = 15f;
    public float maxSpeed = 8f;

    [Header("Rotación hacia el movimiento")]
    public float rotationSpeed = 360f;
    public float minSpeedToRotate = 0.15f;
    public float anguloOffset = -90f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private NIS inputActions;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationY;

        inputActions = new NIS();
    }

    private void Start()
    {
        var playerInput = GetComponent<PlayerInput>();
        inputActions.devices = playerInput.devices;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        Vector3 inputDir = new Vector3(moveInput.x, moveInput.y, 0f);
        rb.AddForce(inputDir * thrustForce, ForceMode.Force);
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);

        if (rb.linearVelocity.magnitude > minSpeedToRotate)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle + anguloOffset);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}