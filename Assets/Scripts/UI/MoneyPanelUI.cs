using TMPro;
using UnityEngine;

/// <summary>
/// Presenta el saldo de MissionMoney. No hace búsquedas por frame.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class MoneyPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private string prefix = "DINERO: ";

    private MissionMoney money;
    private MissionGoal missionGoal;
    private bool isSubscribed;
    private bool isGoalSubscribed;

    private void Awake()
    {
        if (moneyText == null)
            moneyText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        ResolveAndSubscribe();
    }

    private void Start()
    {
        ResolveAndSubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
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

        if (missionGoal == null)
            missionGoal = MissionGoal.Instance;

        if (missionGoal == null)
            missionGoal = FindFirstObjectByType<MissionGoal>();

        if (missionGoal != null && !isGoalSubscribed)
        {
            missionGoal.GoalStateChanged += OnGoalStateChanged;
            isGoalSubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (money != null && isSubscribed)
            money.MoneyChanged -= OnMoneyChanged;

        if (missionGoal != null && isGoalSubscribed)
            missionGoal.GoalStateChanged -= OnGoalStateChanged;

        isSubscribed = false;
        isGoalSubscribed = false;
    }

    private void OnMoneyChanged(int currentMoney)
    {
        if (moneyText != null)
            moneyText.text = FormatMoney(currentMoney);
    }

    private void OnGoalStateChanged(bool _) => Refresh();

    private void Refresh()
    {
        OnMoneyChanged(money != null ? money.CurrentMoney : 0);
    }

    private string FormatMoney(int currentMoney)
    {
        if (missionGoal == null)
            return $"{prefix}{currentMoney:N0}";

        string progress = $"{currentMoney:N0} / {missionGoal.RequiredMoney:N0}";
        return missionGoal.IsCompleted
            ? $"META COMPLETADA:  {progress}"
            : $"DINERO:  {progress}";
    }
}
