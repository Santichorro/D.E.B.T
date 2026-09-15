using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Completa la meta monetaria de la misión y desbloquea una puerta final.
/// La meta escucha eventos de MissionMoney; no realiza búsquedas ni trabajo por frame.
/// </summary>
[DisallowMultipleComponent]
public class MissionGoal : MonoBehaviour
{
    public static MissionGoal Instance { get; private set; }

    [Header("Meta monetaria")]
    [SerializeField, Min(1)] private int requiredMoney = 100;

    [Header("Salida")]
    [Tooltip("Puerta que permanecerá cerrada hasta alcanzar la meta.")]
    [SerializeField] private Door finalDoor;
    [SerializeField] private bool lockFinalDoorUntilCompleted = true;

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
            Debug.LogWarning("[MissionGoal] Hay más de una meta en la escena. Se deshabilitó el duplicado.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        ResolveAndSubscribe();
    }

    private void Start()
    {
        ResolveAndSubscribe();

        if (finalDoor == null)
            Debug.LogWarning("[MissionGoal] Asigna la Puerta Final en el Inspector de Managers.", this);
        else if (lockFinalDoorUntilCompleted)
            finalDoor.SetMissionLocked(true);

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

        IsCompleted = true;

        if (finalDoor != null)
            finalDoor.UnlockForMission();

        GoalStateChanged?.Invoke(true);
        onGoalCompleted?.Invoke();
    }

    private void OnValidate()
    {
        requiredMoney = Mathf.Max(1, requiredMoney);
    }
}
