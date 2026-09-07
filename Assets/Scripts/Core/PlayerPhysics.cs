using UnityEngine;

/// <summary>
/// Sistema común de físicas. Cualquier fuerza del juego (input del jugador,
/// pistola de gravedad, empuje, pulso, agujero negro, etc.) debe pasar por aquí
/// en lugar de llamar rb.AddForce directamente. Esto asegura que todos los
/// roles y habilidades comparten exactamente el mismo comportamiento físico.
/// </summary>
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

    /// <summary>
    /// Punto de entrada único para fuerzas continuas (thrust del jugador,
    /// atracción de la pistola de gravedad, empuje, viento de fuga de presión, etc.)
    /// Se acumulan y se aplican todas juntas en FixedUpdate.
    /// </summary>
    public void ApplyForce(Vector3 force)
    {
        pendingForce += force;
        forceAppliedThisFrame = true;
    }

    /// <summary>
    /// Punto de entrada único para impulsos instantáneos (golpe del Artillero,
    /// pulso del Demoledor, daño por colisión con empuje, etc.)
    /// </summary>
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
            // Sin fuerzas activas este frame: frenado uniforme para todos los roles/objetos
            Vector3 decelForce = -rb.linearVelocity.normalized * deceleration;
            rb.AddForce(decelForce, ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.magnitude <= stopThreshold)
        {
            rb.linearVelocity = Vector3.zero;
        }

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);

        // Reset para el siguiente frame
        pendingForce = Vector3.zero;
        forceAppliedThisFrame = false;
    }

    public Vector3 CurrentVelocity => rb.linearVelocity;
}