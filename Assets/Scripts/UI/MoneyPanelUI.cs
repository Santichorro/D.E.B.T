using TMPro;
using UnityEngine;

/// <summary>
/// Presenta el saldo de MissionMoney. No hace búsquedas por frame.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class MoneyPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private string prefix = "DINERO  $";

    private MissionMoney money;
    private bool isSubscribed;

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
    }

    private void Unsubscribe()
    {
        if (money != null && isSubscribed)
            money.MoneyChanged -= OnMoneyChanged;

        isSubscribed = false;
    }

    private void OnMoneyChanged(int currentMoney)
    {
        if (moneyText != null)
            moneyText.text = $"{prefix}{currentMoney:N0}";
    }

    private void Refresh()
    {
        OnMoneyChanged(money != null ? money.CurrentMoney : 0);
    }
}
