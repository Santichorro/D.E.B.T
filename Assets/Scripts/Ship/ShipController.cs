using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    public static ShipController ActiveTetherShip { get; private set; }
    public static event Action<ShipController> ActiveTetherShipChanged;

    public float moveSpeed = 3f;

    [Header("Tether de jugadores")]
    [Tooltip("Actívalo únicamente en la nave de PRIMER NIVEL. Su propio transform será el extremo del cable.")]
    [SerializeField] private bool habilitarTetherDeJugadores;

    [Header("Referencias para esconder/mostrar al piloto")]
    public Transform pilotVisual;

    private Rigidbody rb;
    private Interactable pilot;
    private Collider pilotCollider;
    private Rigidbody pilotRigidbody;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (!habilitarTetherDeJugadores)
            return;

        if (ActiveTetherShip != null && ActiveTetherShip != this)
            Debug.LogWarning("Hay más de una nave configurada como tether activo. Se usará la última que se habilitó.", this);

        ActiveTetherShip = this;
        ActiveTetherShipChanged?.Invoke(this);
    }

    private void OnDisable()
    {
        if (ActiveTetherShip != this)
            return;

        ActiveTetherShip = null;
        ActiveTetherShipChanged?.Invoke(null);
    }

    public void EnterShip(Interactable player)
    {
        if (pilot != null) return;

        pilot = player;
        pilot.SetPiloting(this);

        pilotRigidbody = player.GetComponent<Rigidbody>();
        pilotCollider = player.GetComponent<Collider>();

        pilotRigidbody.isKinematic = true;
        if (pilotCollider != null) pilotCollider.enabled = false;

        var visual = player.transform.Find("Visual");
        if (visual != null) visual.gameObject.SetActive(false);

        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        var gravityGun = player.GetComponent<GravityGun>();
        if (gravityGun != null) gravityGun.enabled = false;

        // NUEVO: mientras pilotea, no debe seguir amarrado a la nave (ni tirón ni cable visible).
        var tether = player.GetComponent<PlayerShipLine>();
        if (tether != null) tether.SetTetherActivo(false);
    }

    public void ExitShip(Interactable player)
    {
        if (pilot != player) return;

        pilotRigidbody.isKinematic = false;
        pilotRigidbody.position = transform.position + transform.right * 1.5f;

        if (pilotCollider != null) pilotCollider.enabled = true;

        var visual = player.transform.Find("Visual");
        if (visual != null) visual.gameObject.SetActive(true);

        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = true;

        var gravityGun = player.GetComponent<GravityGun>();
        if (gravityGun != null) gravityGun.enabled = true;

        // NUEVO: al salir, se reactiva el tether con normalidad.
        var tether = player.GetComponent<PlayerShipLine>();
        if (tether != null) tether.SetTetherActivo(true);

        pilot.SetPiloting(null);
        pilot = null;
    }

    private void FixedUpdate()
    {
        if (pilot == null)
        {
            // En la configuración dinámica de PRIMER NIVEL, la nave debe quedar
            // completamente detenida al abandonar el pilotaje.
            if (!rb.isKinematic)
                rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector2 input = pilot.ReadMove();
        Vector3 velocity = new Vector3(input.x, input.y, 0f) * moveSpeed;

        // Un Rigidbody dinámico respeta la resolución de colisiones con paredes
        // y se desliza sobre ellas. Se conserva MovePosition para naves antiguas
        // que sigan configuradas como cinemáticas en otras escenas.
        if (rb.isKinematic)
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        else
            rb.linearVelocity = velocity;
    }
}
