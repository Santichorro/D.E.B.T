using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPhysics : MonoBehaviour, IAnchorable
{
    [Header("Límites y amortiguación")]
    public float maxSpeed = 8f;
    public float deceleration = 10f;
    [Tooltip("Velocidad mínima para considerar que el objeto está 'quieto' y frenarlo por completo.")]
    public float stopThreshold = 0.05f;

    [Header("Anclaje")]
    [Tooltip("Segundos antes de que termine el anclaje en los que se debería avisar (señal sonora u otra) de que está por soltarse.")]
    [SerializeField] private float anchorWarningLead = 1.5f;

    // NUEVO: ventana de gracia tras un impulso externo (empuje/knockback).
    [Header("Impulso externo")]
    [Tooltip("Segundos tras un ApplyImpulse durante los que se ignora el clamp de maxSpeed y la desaceleración automática, para que el empuje realmente se sienta.")]
    [SerializeField] private float impulseGraceDuration = 0.3f;


    public event System.Action OnAnchorAboutToEnd;

    private Rigidbody rb;
    private Vector3 pendingForce;
    private bool forceAppliedThisFrame;

    // NUEVO: marca de tiempo hasta la que dura la ventana de gracia del último impulso.
    private float impulseActiveUntil = -999f;

    // --- Anclaje (IAnchorable) ---
    private Vector3 preAnchorVelocity;
    private float anchorEndTime;
    private Coroutine anchorRoutine;

    public bool IsAnchored { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void ApplyForce(Vector3 force)
    {
        if (IsAnchored) return; // Mientras está anclado, se ignoran fuerzas entrantes.
        pendingForce += force;
        forceAppliedThisFrame = true;
    }

    public void ApplyImpulse(Vector3 impulse)
    {
        if (IsAnchored) return;

        // NUEVO: abre la ventana de gracia para que FixedUpdate no frene/clampee
        // este impulso en el mismo frame o el siguiente.
        impulseActiveUntil = Time.time + impulseGraceDuration;

        rb.AddForce(impulse, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (IsAnchored)
        {
            // Se mantiene como Rigidbody dinámico real (nunca isKinematic = true),
            // pero sin velocidad propia mientras dure el anclaje.
            rb.linearVelocity = Vector3.zero;
            pendingForce = Vector3.zero;
            forceAppliedThisFrame = false;
            return;
        }

        // NUEVO: mientras dure la ventana de gracia de un impulso, se suspenden
        // el clamp de maxSpeed y la desaceleración automática. El input normal
        // del jugador (ApplyForce) sigue funcionando igual durante este rato.
        bool impulseGraceActive = Time.time < impulseActiveUntil;

        if (forceAppliedThisFrame)
        {
            rb.AddForce(pendingForce, ForceMode.Force);
        }
        else if (!impulseGraceActive && rb.linearVelocity.magnitude > stopThreshold)
        {
            Vector3 decelForce = -rb.linearVelocity.normalized * deceleration;
            rb.AddForce(decelForce, ForceMode.Acceleration);
        }
        else if (!impulseGraceActive && rb.linearVelocity.magnitude <= stopThreshold)
        {
            rb.linearVelocity = Vector3.zero;
        }

        if (!impulseGraceActive)
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);

        pendingForce = Vector3.zero;
        forceAppliedThisFrame = false;
    }

    public Vector3 CurrentVelocity => rb.linearVelocity;

    // --- IAnchorable ---

    public void Anchor(float duration)
    {
        if (anchorRoutine != null)
            StopCoroutine(anchorRoutine);

        if (!IsAnchored)
            preAnchorVelocity = rb.linearVelocity;

        IsAnchored = true;
        anchorEndTime = Time.time + duration;
        anchorRoutine = StartCoroutine(AnchorRoutine(duration));
    }

    private System.Collections.IEnumerator AnchorRoutine(float duration)
    {
        float warningDelay = Mathf.Max(0f, duration - anchorWarningLead);
        yield return new WaitForSeconds(warningDelay);

        OnAnchorAboutToEnd?.Invoke();

        float remaining = anchorEndTime - Time.time;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        IsAnchored = false;
        anchorRoutine = null;
        // preAnchorVelocity queda disponible por si en el futuro se decide reanudar el impulso
        // en vez de arrancar de cero; por ahora el objeto retoma control normal desde velocidad 0.
    }
}