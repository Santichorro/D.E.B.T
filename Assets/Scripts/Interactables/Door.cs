using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Las dos hojas que se desplazan al abrir la puerta.")]
    [SerializeField] private Transform[] doorLeaves;

    [Tooltip("Mecanismos hijos que reciben el Pulso y controlan esta puerta.")]
    [SerializeField] private Mechanism[] linkedMechanisms;

    [Header("Movimiento local")]
    [Tooltip("Distancia que cada hoja recorre hacia su eje local Z positivo al abrirse.")]
    [SerializeField] private float openDistance = 3f;
    [SerializeField] private float duration = 1f;
    [SerializeField]
    private AnimationCurve smoothCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Al reactivarse el mecanismo")]
    [Tooltip("Si está activo, la puerta vuelve a cerrarse cuando ningún mecanismo hijo siga desactivado.")]
    [SerializeField] private bool closeWhenMechanismsReactivate = true;

    [Header("Sonidos")]
    [Tooltip("AudioSource que reproducirá los sonidos de apertura y cierre. Si se deja vacío, se buscará uno en este GameObject.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sonido que se reproduce cuando la puerta empieza a abrirse.")]
    [SerializeField] private AudioClip openSound;
    [Tooltip("Sonido que se reproduce cuando la puerta empieza a cerrarse.")]
    [SerializeField] private AudioClip closeSound;

    private Vector3[] closedLocalPositions;
    private Coroutine moveRoutine;
    private bool isOpen;
    private bool missionLocked;
    private bool missionUnlocked;

    private void Awake()
    {
        if (linkedMechanisms == null || linkedMechanisms.Length == 0)
            linkedMechanisms = GetComponentsInChildren<Mechanism>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        closedLocalPositions = new Vector3[doorLeaves.Length];
        for (int i = 0; i < doorLeaves.Length; i++)
        {
            if (doorLeaves[i] != null)
                closedLocalPositions[i] = doorLeaves[i].localPosition;
        }
    }

    private void OnEnable()
    {
        foreach (Mechanism mechanism in linkedMechanisms)
        {
            if (mechanism != null)
                mechanism.OnDisabledStateChanged += HandleMechanismStateChanged;
        }
    }

    private void Start()
    {
        RefreshStateFromMechanisms();
    }

    private void OnDisable()
    {
        foreach (Mechanism mechanism in linkedMechanisms)
        {
            if (mechanism != null)
                mechanism.OnDisabledStateChanged -= HandleMechanismStateChanged;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    private void HandleMechanismStateChanged(bool isDisabled)
    {
        RefreshStateFromMechanisms();
    }

    private void RefreshStateFromMechanisms()
    {
        if (missionLocked)
        {
            if (missionUnlocked)
                Open();
            else
                Close();

            return;
        }

        if (AnyLinkedMechanismIsDisabled())
            Open();
        else if (closeWhenMechanismsReactivate)
            Close();
    }

    /// <summary>
    /// Hace que esta puerta ignore sus mecanismos hasta que MissionGoal la desbloquee.
    /// No se serializa ni altera el comportamiento de las demás puertas.
    /// </summary>
    public void SetMissionLocked(bool locked)
    {
        missionLocked = locked;
        if (locked)
            RefreshStateFromMechanisms();
    }

    /// <summary>
    /// Abre la puerta permanentemente para la misión actual y evita que un mecanismo la cierre.
    /// </summary>
    public void UnlockForMission()
    {
        missionLocked = true;
        missionUnlocked = true;
        Open();
    }

    private bool AnyLinkedMechanismIsDisabled()
    {
        foreach (Mechanism mechanism in linkedMechanisms)
        {
            if (mechanism != null && mechanism.IsDisabled)
                return true;
        }

        return false;
    }

    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        PlaySound(openSound);
        StartMove(open: true);
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        PlaySound(closeSound);
        StartMove(open: false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void StartMove(bool open)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveRoutine(open));
    }

    private IEnumerator MoveRoutine(bool open)
    {
        if (duration <= 0f)
        {
            SetImmediatePositions(open);
            moveRoutine = null;
            yield break;
        }

        Vector3[] startPositions = new Vector3[doorLeaves.Length];
        Vector3[] targetPositions = new Vector3[doorLeaves.Length];

        for (int i = 0; i < doorLeaves.Length; i++)
        {
            if (doorLeaves[i] == null) continue;

            startPositions[i] = doorLeaves[i].localPosition;
            targetPositions[i] = closedLocalPositions[i] +
                (open ? Vector3.forward * openDistance : Vector3.zero);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = smoothCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < doorLeaves.Length; i++)
            {
                if (doorLeaves[i] != null)
                    doorLeaves[i].localPosition = Vector3.Lerp(
                        startPositions[i], targetPositions[i], t);
            }

            yield return null;
        }

        SetImmediatePositions(open);
        moveRoutine = null;
    }

    private void SetImmediatePositions(bool open)
    {
        for (int i = 0; i < doorLeaves.Length; i++)
        {
            if (doorLeaves[i] == null) continue;

            doorLeaves[i].localPosition = closedLocalPositions[i] +
                (open ? Vector3.forward * openDistance : Vector3.zero);
        }
    }
}
