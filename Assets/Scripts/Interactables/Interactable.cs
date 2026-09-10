using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class Interactable : MonoBehaviour
{
    private NIS inputActions;
    private PlayerInput playerInput;
    private ShipController nearbyShip;
    private ShipController currentShip;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        inputActions = new NIS();
        inputActions.devices = playerInput.devices;
        inputActions.Player.Enable();
        inputActions.Player.Interact.started += OnInteractPressed;
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.started -= OnInteractPressed;
        inputActions.Player.Disable();
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (currentShip != null)
            currentShip.ExitShip(this);
        else if (nearbyShip != null)
            nearbyShip.EnterShip(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        ShipController ship = other.GetComponentInParent<ShipController>();
        if (ship != null) nearbyShip = ship;
    }

    private void OnTriggerExit(Collider other)
    {
        ShipController ship = other.GetComponentInParent<ShipController>();
        if (ship == nearbyShip) nearbyShip = null;
    }

    public Vector2 ReadMove() => inputActions.Player.Move.ReadValue<Vector2>();

    public void SetPiloting(ShipController ship) => currentShip = ship;
}