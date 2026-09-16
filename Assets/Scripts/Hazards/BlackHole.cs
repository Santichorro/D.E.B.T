using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// Se ejecuta después de PlayerPhysics para que su fuerza se combine después
// del input normal del jugador durante el mismo tick de física.
[DefaultExecutionOrder(100)]
public class BlackHole : MonoBehaviour
{
    [Header("Detección")]
    [FormerlySerializedAs("attractionRadius")]
    [SerializeField, Min(0.01f)] private float repulsionRadius = 6f;
    [Tooltip("Semiprofundidad Z del volumen de detección. La distancia de juego sigue calculándose sólo en X/Y.")]
    [SerializeField, Min(0.01f)] private float planarDetectionHalfDepth = 50f;
    [SerializeField] private LayerMask playerLayers = 1 << 7;
    [SerializeField, Min(1)] private int colliderBufferSize = 16;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Repulsión")]
    [FormerlySerializedAs("minimumAttractionForce")]
    [SerializeField, Min(0f)] private float minimumRepulsionForce = 8f;
    [FormerlySerializedAs("maximumAttractionForce")]
    [SerializeField, Min(0f)] private float maximumRepulsionForce = 40f;
    [Tooltip("0 representa el borde del radio de repulsión y 1 el centro del agujero.")]
    [SerializeField] private AnimationCurve forceByProximity = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Desactivación cooperativa")]
    [Tooltip("Los activadores que deben recibir Interact casi al mismo tiempo (todos ellos) para apagar este agujero negro.")]
    [SerializeField] private BlackHoleDeactivator[] deactivationSwitches = new BlackHoleDeactivator[2];
    [Tooltip("Diferencia máxima entre la primera y la última interacción de todos los activadores.")]
    [SerializeField, Min(0.01f)] private float simultaneousDeactivationWindow = 0.5f;
    [SerializeField] private bool requireDifferentPlayers = true;
    [SerializeField] private bool hideVisualWhenDeactivated = true;

    private Collider[] playerColliderBuffer;
    private readonly HashSet<int> processedPlayerIds = new HashSet<int>();
    private bool warnedAboutFullBuffer;
    private float[] deactivationTimes;
    private int[] deactivationPlayerIds;
    private Action<BlackHoleDeactivator, PlayerInput>[] deactivationHandlers;
    private Renderer[] visualRenderers;
    private bool isSubscribedToDeactivationSwitches;
    private HashSet<int> distinctPlayerIdScratch;

    public bool IsDeactivated { get; private set; }
    public event Action OnDeactivated;

    // Es compartida por todos los agujeros negros: una ventana concedida por
    // Push o Propulsion permite atravesar cualquiera de ellos durante su duración.
    private static readonly Dictionary<int, float> repulsionImmunityEndTimes = new Dictionary<int, float>();

    public static void GrantRepulsionImmunity(PlayerPhysics playerPhysics, float duration)
    {
        if (playerPhysics == null || duration <= 0f)
            return;

        int playerId = playerPhysics.GetInstanceID();
        float endTime = Time.time + duration;

        if (repulsionImmunityEndTimes.TryGetValue(playerId, out float currentEndTime))
            repulsionImmunityEndTimes[playerId] = Mathf.Max(currentEndTime, endTime);
        else
            repulsionImmunityEndTimes.Add(playerId, endTime);
    }

    private void Awake()
    {
        ValidateValues();
        playerColliderBuffer = new Collider[colliderBufferSize];
        visualRenderers = GetComponentsInChildren<Renderer>(true);
        ResetDeactivationAttempts();
    }

    private void OnEnable()
    {
        SubscribeToDeactivationSwitches();
    }

    private void OnDisable()
    {
        UnsubscribeFromDeactivationSwitches();
    }

    private void FixedUpdate()
    {
        if (IsDeactivated || playerColliderBuffer == null || repulsionRadius <= 0f)
            return;

        Vector3 center = transform.position;
        // El volumen amplio en Z permite que la representación visual del agujero
        // esté a otra profundidad. Después se filtra con una distancia circular X/Y.
        int colliderCount = Physics.OverlapBoxNonAlloc(
            center,
            new Vector3(repulsionRadius, repulsionRadius, planarDetectionHalfDepth),
            playerColliderBuffer,
            Quaternion.identity,
            playerLayers,
            triggerInteraction);

        if (colliderCount == playerColliderBuffer.Length && !warnedAboutFullBuffer)
        {
            Debug.LogWarning($"{name}: el buffer de jugadores está lleno. Aumenta Collider Buffer Size si pueden coincidir más de {colliderBufferSize} colliders.", this);
            warnedAboutFullBuffer = true;
        }

        processedPlayerIds.Clear();

        for (int i = 0; i < colliderCount; i++)
        {
            Collider playerCollider = playerColliderBuffer[i];
            if (playerCollider == null)
                continue;

            PlayerPhysics playerPhysics = playerCollider.GetComponentInParent<PlayerPhysics>();
            if (playerPhysics == null || !playerPhysics.isActiveAndEnabled || !playerPhysics.CompareTag("Player"))
                continue;

            // Un jugador puede tener más de un Collider: se procesa una única vez
            // para no multiplicar la fuerza ni el daño.
            if (!processedPlayerIds.Add(playerPhysics.GetInstanceID()))
                continue;

            Health health = playerPhysics.GetComponent<Health>();
            if (health == null || health.IsDead)
                continue;

            if (!HasRepulsionImmunity(playerPhysics))
                ApplyRepulsion(playerPhysics, center);
        }
    }

    private static bool HasRepulsionImmunity(PlayerPhysics playerPhysics)
    {
        int playerId = playerPhysics.GetInstanceID();
        if (!repulsionImmunityEndTimes.TryGetValue(playerId, out float endTime))
            return false;

        if (Time.time < endTime)
            return true;

        repulsionImmunityEndTimes.Remove(playerId);
        return false;
    }

    private void ApplyRepulsion(PlayerPhysics playerPhysics, Vector3 center)
    {
        Vector3 playerPosition = playerPhysics.transform.position;
        Vector3 planarOffset = new Vector3(
            playerPosition.x - center.x,
            playerPosition.y - center.y,
            0f);

        float squaredDistance = planarOffset.sqrMagnitude;
        if (squaredDistance > repulsionRadius * repulsionRadius)
            return;

        float distance = Mathf.Sqrt(squaredDistance);
        float proximity = 1f - Mathf.Clamp01(distance / repulsionRadius);
        float curveValue = forceByProximity != null
            ? Mathf.Clamp01(forceByProximity.Evaluate(proximity))
            : proximity;
        float force = Mathf.Lerp(minimumRepulsionForce, maximumRepulsionForce, curveValue);

        // En el centro no existe una dirección radial. Se escoge +X para expulsar
        // al jugador sin tocar Z ni dejarlo permanentemente dentro del agujero.
        Vector3 direction = squaredDistance > 0.0001f ? planarOffset / distance : Vector3.right;

        playerPhysics.ApplyForce(direction * force);
    }

    private void SubscribeToDeactivationSwitches()
    {
        if (isSubscribedToDeactivationSwitches || deactivationSwitches == null)
            return;

        EnsureDeactivationHandlers();

        for (int i = 0; i < deactivationSwitches.Length; i++)
        {
            BlackHoleDeactivator deactivationSwitch = deactivationSwitches[i];
            if (deactivationSwitch != null)
                deactivationSwitch.OnActivated += deactivationHandlers[i];
        }

        isSubscribedToDeactivationSwitches = true;
    }

    private void UnsubscribeFromDeactivationSwitches()
    {
        if (!isSubscribedToDeactivationSwitches || deactivationSwitches == null)
            return;

        for (int i = 0; i < deactivationSwitches.Length; i++)
        {
            BlackHoleDeactivator deactivationSwitch = deactivationSwitches[i];
            if (deactivationSwitch != null)
                deactivationSwitch.OnActivated -= deactivationHandlers[i];
        }

        isSubscribedToDeactivationSwitches = false;
    }

    private void HandleDeactivationSwitchActivated(
        int switchIndex,
        BlackHoleDeactivator deactivationSwitch,
        PlayerInput activatingPlayer)
    {
        if (IsDeactivated)
            return;

        EnsureDeactivationAttemptBuffers();
        deactivationTimes[switchIndex] = Time.time;
        deactivationPlayerIds[switchIndex] = activatingPlayer != null ? activatingPlayer.GetInstanceID() : 0;

        TryDeactivateWhenSwitchesMatch();
    }

    private void TryDeactivateWhenSwitchesMatch()
    {
        if (deactivationSwitches == null || deactivationSwitches.Length == 0)
            return;

        float earliestActivation = float.MaxValue;
        float latestActivation = float.MinValue;

        if (requireDifferentPlayers)
        {
            distinctPlayerIdScratch ??= new HashSet<int>();
            distinctPlayerIdScratch.Clear();
        }

        for (int i = 0; i < deactivationSwitches.Length; i++)
        {
            if (deactivationSwitches[i] == null || deactivationTimes[i] < 0f)
                return; // todavía falta algún activador por dispararse

            earliestActivation = Mathf.Min(earliestActivation, deactivationTimes[i]);
            latestActivation = Mathf.Max(latestActivation, deactivationTimes[i]);

            if (requireDifferentPlayers)
                distinctPlayerIdScratch.Add(deactivationPlayerIds[i]);
        }

        bool withinWindow = latestActivation - earliestActivation <= simultaneousDeactivationWindow;
        bool differentPlayers = !requireDifferentPlayers
            || distinctPlayerIdScratch.Count == deactivationSwitches.Length;

        if (withinWindow && differentPlayers)
            Deactivate();
    }

    public void Deactivate()
    {
        if (IsDeactivated)
            return;

        IsDeactivated = true;

        if (hideVisualWhenDeactivated && visualRenderers != null)
        {
            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer != null)
                    visualRenderer.enabled = false;
            }
        }

        OnDeactivated?.Invoke();
    }

    private void ResetDeactivationAttempts()
    {
        EnsureDeactivationAttemptBuffers();

        for (int i = 0; i < deactivationTimes.Length; i++)
        {
            deactivationTimes[i] = -1f;
            deactivationPlayerIds[i] = 0;
        }
    }

    private void EnsureDeactivationAttemptBuffers()
    {
        int switchCount = deactivationSwitches != null ? deactivationSwitches.Length : 0;
        if (deactivationTimes == null || deactivationTimes.Length != switchCount)
        {
            deactivationTimes = new float[switchCount];
            deactivationPlayerIds = new int[switchCount];
            for (int i = 0; i < deactivationTimes.Length; i++)
            {
                deactivationTimes[i] = -1f;
                deactivationPlayerIds[i] = 0;
            }
        }
    }

    private void EnsureDeactivationHandlers()
    {
        int switchCount = deactivationSwitches != null ? deactivationSwitches.Length : 0;
        if (deactivationHandlers != null && deactivationHandlers.Length == switchCount)
            return;

        deactivationHandlers = new Action<BlackHoleDeactivator, PlayerInput>[switchCount];
        for (int i = 0; i < deactivationHandlers.Length; i++)
        {
            int switchIndex = i;
            deactivationHandlers[i] = (deactivationSwitch, activatingPlayer) =>
                HandleDeactivationSwitchActivated(switchIndex, deactivationSwitch, activatingPlayer);
        }
    }

    private void OnValidate()
    {
        ValidateValues();
    }

    private void ValidateValues()
    {
        repulsionRadius = Mathf.Max(0.01f, repulsionRadius);
        planarDetectionHalfDepth = Mathf.Max(0.01f, planarDetectionHalfDepth);
        minimumRepulsionForce = Mathf.Max(0f, minimumRepulsionForce);
        maximumRepulsionForce = Mathf.Max(minimumRepulsionForce, maximumRepulsionForce);
        colliderBufferSize = Mathf.Max(1, colliderBufferSize);
        simultaneousDeactivationWindow = Mathf.Max(0.01f, simultaneousDeactivationWindow);

        if (deactivationSwitches == null)
            deactivationSwitches = new BlackHoleDeactivator[2];

        if (forceByProximity == null || forceByProximity.length == 0)
            forceByProximity = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.45f, 0.15f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, repulsionRadius);
    }
}