using UnityEngine;

/// <summary>
/// Puerta-obstáculo independiente de Door/Mechanism. Recorre un ciclo a velocidad
/// constante y puede pausarse temporalmente mediante IAnchorable.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class CyclicAnchorableDoor : MonoBehaviour, IAnchorable
{
    private enum CyclePhase { Opening, OpenWait, Closing, ClosedWait }

    [Header("Hojas y colisiones")]
    [SerializeField] private Transform[] doorLeaves;
    [Tooltip("Colliders que bloquean el paso. Deben ser sólidos, no Trigger.")]
    [SerializeField] private Collider[] blockingColliders;

    [Header("Movimiento local constante")]
    [Tooltip("Desplazamiento local desde la posición cerrada hasta la abierta.")]
    [SerializeField] private Vector3 openOffsetLocal = Vector3.forward * 3f;
    [SerializeField, Min(0.01f)] private float movementSpeed = 6f;
    [SerializeField, Min(0f)] private float openWaitDuration = 0.2f;
    [SerializeField, Min(0f)] private float closedWaitDuration = 0.2f;
    [SerializeField] private bool startOpen;

    [Header("Anclaje")]
    [SerializeField] private bool acceptsAnchoring = true;

    public bool IsAnchored { get; private set; }

    private Rigidbody doorRigidbody;
    private Vector3[] closedLocalPositions;
    private CyclePhase phase;
    private float phaseTimer;
    private float anchorEndTime;

    private void Awake()
    {
        if (GetComponent<Door>() != null)
        {
            Debug.LogError($"{name}: CyclicAnchorableDoor no puede coexistir con Door. Usa un objeto de puerta independiente.", this);
            enabled = false;
            return;
        }

        doorRigidbody = GetComponent<Rigidbody>();
        ConfigureKinematicBody();

        if (doorLeaves == null || doorLeaves.Length == 0)
        {
            Debug.LogError($"{name}: asigna al menos una hoja en CyclicAnchorableDoor.", this);
            enabled = false;
            return;
        }

        closedLocalPositions = new Vector3[doorLeaves.Length];
        for (int i = 0; i < doorLeaves.Length; i++)
        {
            if (doorLeaves[i] != null)
                closedLocalPositions[i] = doorLeaves[i].localPosition;
        }

        if (blockingColliders == null || blockingColliders.Length == 0)
            blockingColliders = GetComponentsInChildren<Collider>(true);

        WarnAboutInvalidBlockers();

        if (startOpen)
        {
            SetLeavesAt(1f);
            phase = CyclePhase.OpenWait;
            phaseTimer = openWaitDuration;
        }
        else
        {
            phase = CyclePhase.ClosedWait;
            phaseTimer = closedWaitDuration;
        }
    }

    private void FixedUpdate()
    {
        if (IsAnchored)
        {
            if (Time.time < anchorEndTime)
                return;

            IsAnchored = false;
        }

        AdvanceCycle(Time.fixedDeltaTime);
    }

    public void Anchor(float duration)
    {
        if (!acceptsAnchoring || duration <= 0f || !enabled)
            return;

        IsAnchored = true;
        // Repetir el anclaje extiende la pausa y nunca crea corrutinas duplicadas.
        anchorEndTime = Mathf.Max(anchorEndTime, Time.time + duration);
    }

    private void AdvanceCycle(float deltaTime)
    {
        switch (phase)
        {
            case CyclePhase.Opening:
                if (MoveLeavesTowards(1f, deltaTime))
                {
                    phase = CyclePhase.OpenWait;
                    phaseTimer = openWaitDuration;
                }
                break;

            case CyclePhase.OpenWait:
                if ((phaseTimer -= deltaTime) <= 0f)
                    phase = CyclePhase.Closing;
                break;

            case CyclePhase.Closing:
                if (MoveLeavesTowards(0f, deltaTime))
                {
                    phase = CyclePhase.ClosedWait;
                    phaseTimer = closedWaitDuration;
                }
                break;

            case CyclePhase.ClosedWait:
                if ((phaseTimer -= deltaTime) <= 0f)
                    phase = CyclePhase.Opening;
                break;
        }
    }

    private bool MoveLeavesTowards(float openAmount, float deltaTime)
    {
        bool allReached = true;
        float step = movementSpeed * deltaTime;

        for (int i = 0; i < doorLeaves.Length; i++)
        {
            Transform leaf = doorLeaves[i];
            if (leaf == null)
                continue;

            Vector3 target = closedLocalPositions[i] + openOffsetLocal * openAmount;
            leaf.localPosition = Vector3.MoveTowards(leaf.localPosition, target, step);
            if ((leaf.localPosition - target).sqrMagnitude > 0.000001f)
                allReached = false;
        }

        return allReached;
    }

    private void SetLeavesAt(float openAmount)
    {
        for (int i = 0; i < doorLeaves.Length; i++)
        {
            if (doorLeaves[i] != null)
                doorLeaves[i].localPosition = closedLocalPositions[i] + openOffsetLocal * openAmount;
        }
    }

    private void ConfigureKinematicBody()
    {
        if (doorRigidbody == null)
            return;

        doorRigidbody.useGravity = false;
        doorRigidbody.isKinematic = true;
        doorRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        doorRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void WarnAboutInvalidBlockers()
    {
        foreach (Collider blocker in blockingColliders)
        {
            if (blocker != null && blocker.isTrigger)
                Debug.LogWarning($"{name}: {blocker.name} está configurado como Trigger y no bloqueará players ni nave.", blocker);
        }
    }

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0.01f, movementSpeed);
        openWaitDuration = Mathf.Max(0f, openWaitDuration);
        closedWaitDuration = Mathf.Max(0f, closedWaitDuration);
    }
}
