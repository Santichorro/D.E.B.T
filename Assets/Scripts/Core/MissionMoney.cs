using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MissionMoney : MonoBehaviour
{
    public static MissionMoney Instance { get; private set; }

    [SerializeField, Min(0)] private int startingMoney = 0;

    public int CurrentMoney { get; private set; }

    public event Action<int> MoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[MissionMoney] Ya existe un sistema de dinero. " +
                "Se eliminará este duplicado.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // El dinero permanece entre niveles
        DontDestroyOnLoad(gameObject);

        CurrentMoney = Mathf.Max(0, startingMoney);
    }

    private void Start()
    {
        MoneyChanged?.Invoke(CurrentMoney);
    }

    public bool AddMoney(int amount)
    {
        if (!isActiveAndEnabled || amount <= 0)
            return false;

        long newAmount = (long)CurrentMoney + amount;

        CurrentMoney = newAmount >= int.MaxValue
            ? int.MaxValue
            : (int)newAmount;

        MoneyChanged?.Invoke(CurrentMoney);

        return true;
    }

    public void ResetMoney()
    {
        CurrentMoney = Mathf.Max(0, startingMoney);

        MoneyChanged?.Invoke(CurrentMoney);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}