using System;
using UnityEngine;

/// <summary>
/// Saldo único de la partida actual. No persiste entre cargas de escena.
/// </summary>
[DisallowMultipleComponent]
public class MissionMoney : MonoBehaviour
{
    public static MissionMoney Instance { get; private set; }

    [SerializeField, Min(0)] private int startingMoney;

    public int CurrentMoney { get; private set; }
    public event Action<int> MoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MissionMoney] Hay más de un saldo en la escena. Se deshabilitó el duplicado.", this);
            enabled = false;
            return;
        }

        Instance = this;
        CurrentMoney = Mathf.Max(0, startingMoney);
    }

    private void Start()
    {
        MoneyChanged?.Invoke(CurrentMoney);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool AddMoney(int amount)
    {
        if (!isActiveAndEnabled || amount <= 0)
            return false;

        long newAmount = (long)CurrentMoney + amount;
        CurrentMoney = newAmount >= int.MaxValue ? int.MaxValue : (int)newAmount;
        MoneyChanged?.Invoke(CurrentMoney);
        return true;
    }
}
