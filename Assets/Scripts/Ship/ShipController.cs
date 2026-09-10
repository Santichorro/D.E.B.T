using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    public float moveSpeed = 3f;

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

        pilot.SetPiloting(null);
        pilot = null;
    }

    private void FixedUpdate()
    {
        if (pilot == null) return;

        Vector2 input = pilot.ReadMove();
        Vector3 movement = new Vector3(input.x, input.y, 0f) * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
}