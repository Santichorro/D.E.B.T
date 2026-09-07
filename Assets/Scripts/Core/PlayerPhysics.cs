using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPhysics : MonoBehaviour
{
    [Header("Límites y amortiguación")]
    public float maxSpeed = 8f;
    public float deceleration = 10f;
    [Tooltip("Velocidad mínima para considerar que el objeto está 'quieto' y frenarlo por completo.")]
    public float stopThreshold = 0.05f;

    private Rigidbody rb;
    private Vector3 pendingForce;
    private bool forceAppliedThisFrame;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void ApplyForce(Vector3 force)
    {
        pendingForce += force;
        forceAppliedThisFrame = true;
    }

    public void ApplyImpulse(Vector3 impulse)
    {
        rb.AddForce(impulse, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (forceAppliedThisFrame)
        {
            rb.AddForce(pendingForce, ForceMode.Force);
        }
        else if (rb.linearVelocity.magnitude > stopThreshold)
        {
            Vector3 decelForce = -rb.linearVelocity.normalized * deceleration;
            rb.AddForce(decelForce, ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.magnitude <= stopThreshold)
        {
            rb.linearVelocity = Vector3.zero;
        }

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);

        pendingForce = Vector3.zero;
        forceAppliedThisFrame = false;
    }

    public Vector3 CurrentVelocity => rb.linearVelocity;
}