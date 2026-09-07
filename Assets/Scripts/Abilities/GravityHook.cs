using UnityEngine;

/// <summary>
/// Proyectil de la pistola de gravedad. Vuela en línea recta, y al chocar
/// con algo que tenga Rigidbody, se detiene y queda "enganchado" ahí.
/// No aplica fuerzas por sí mismo: solo reporta el objetivo a GravityGun.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityHook : MonoBehaviour
{
    public float speed = 25f;
    public float maxLifetime = 3f; // si no pega a nada, se autodestruye

    private Rigidbody rb;
    private GravityGun owner;
    private bool attached;

    public Rigidbody TargetRb { get; private set; }
    public PlayerPhysics TargetPhysics { get; private set; } // null si no es un jugador
    public Vector3 LocalAttachPoint { get; private set; }    // punto de enganche relativo al target

    public void Init(GravityGun owner, Vector3 direction)
    {
        this.owner = owner;
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = direction.normalized * speed;
        Destroy(gameObject, maxLifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (attached) return;

        Rigidbody targetRb = other.attachedRigidbody;
        if (targetRb == null) return; // no engancha a cosas estáticas sin Rigidbody

        attached = true;
        TargetRb = targetRb;
        TargetPhysics = targetRb.GetComponent<PlayerPhysics>(); // puede ser null

        // Punto de enganche en espacio local del objetivo, para que el cable
        // se sienta "pegado" a un punto concreto y no al centro de masa.
        LocalAttachPoint = targetRb.transform.InverseTransformPoint(transform.position);

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true; // ya no necesita física propia

        owner.OnHookAttached(this);
    }
}