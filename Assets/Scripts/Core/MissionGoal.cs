using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Completa la meta monetaria de la misión y desbloquea una puerta final.
/// La meta escucha eventos de MissionMoney; no realiza búsquedas ni trabajo por frame.
/// </summary>
[DisallowMultipleComponent]
public class MissionGoal : MonoBehaviour
{
    public static MissionGoal Instance { get; private set; }

    [Header("Meta monetaria")]
    [SerializeField, Min(1)] private int requiredMoney = 8000;

    [Header("Salida")]
    [Tooltip("Puerta que permanecerá cerrada hasta alcanzar la meta.")]
    [SerializeField] private Door finalDoor;

    [SerializeField] private bool lockFinalDoorUntilCompleted = true;

    [Header("Mensaje de misión completada")]
    [Tooltip("Panel que aparecerá cuando se complete la deuda.")]
    [SerializeField] private GameObject debtCompletedPanel;

    [Tooltip("Texto que mostrará el mensaje de deuda completada.")]
    [SerializeField] private TMP_Text debtCompletedText;

    [TextArea(2, 5)]
    [SerializeField]
    private string debtCompletedMessage =
        "¡DEUDA PAGADA!\nHan reunido el dinero necesario.\nLa puerta de salida está abierta.";

    [Header("Eventos opcionales")]
    [SerializeField] private UnityEvent onGoalCompleted;

    private MissionMoney money;
    private bool isSubscribed;

    public int RequiredMoney => requiredMoney;
    public bool IsCompleted { get; private set; }

    public event Action<bool> GoalStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[MissionGoal] Hay más de una meta en la escena. " +
                "Se deshabilitó el duplicado.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        // El mensaje comienza oculto
        if (debtCompletedPanel != null)
            debtCompletedPanel.SetActive(false);
    }

    private void OnEnable()
    {
        ResolveAndSubscribe();
    }

    private void Start()
    {
        ResolveAndSubscribe();

        if (finalDoor == null)
        {
            Debug.LogWarning(
                "[MissionGoal] Asigna la Puerta Final en el Inspector de Managers.",
                this
            );
        }
        else if (lockFinalDoorUntilCompleted)
        {
            finalDoor.SetMissionLocked(true);
        }

        Evaluate(money != null ? money.CurrentMoney : 0);
    }

    private void OnDisable()
    {
        if (money != null && isSubscribed)
            money.MoneyChanged -= OnMoneyChanged;

        isSubscribed = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ResolveAndSubscribe()
    {
        if (money == null)
            money = MissionMoney.Instance;

        if (money == null)
            money = FindFirstObjectByType<MissionMoney>();

        if (money != null && !isSubscribed)
        {
            money.MoneyChanged += OnMoneyChanged;
            isSubscribed = true;
        }
    }

    private void OnMoneyChanged(int currentMoney)
    {
        Evaluate(currentMoney);
    }

    private void Evaluate(int currentMoney)
    {
        if (IsCompleted || currentMoney < requiredMoney)
            return;

        // Marcar la misión como completada
        IsCompleted = true;

        // Abrir la puerta
        if (finalDoor != null)
            finalDoor.UnlockForMission();

        // Mostrar mensaje en pantalla
        ShowDebtCompletedMessage(currentMoney);

        // Notificar a otros sistemas
        GoalStateChanged?.Invoke(true);
        onGoalCompleted?.Invoke();
    }

    private void ShowDebtCompletedMessage(int currentMoney)
    {
        if (debtCompletedPanel == null)
            return;

        // Si se asignó un texto, actualizarlo
        if (debtCompletedText != null)
        {
            debtCompletedText.text =
                $"¡DEUDA PAGADA!\n" +
                $"Dirigirse arriba izquierda,\r\n" +
                $"La puerta de salida esta abierta.";
        }

        // Mostrar el panel
        debtCompletedPanel.SetActive(true);
    }

    public void HideDebtCompletedMessage()
    {
        if (debtCompletedPanel != null)
            debtCompletedPanel.SetActive(false);
    }

    private void OnValidate()
    {
        requiredMoney = Mathf.Max(1, requiredMoney);
    }
}