using UnityEngine;

/// <summary>
/// Restablece las referencias del HUD exclusivamente en la escena MK2.
/// Se ejecuta antes de PlayerHUDManager para que este pueda registrar jugadores
/// y enlazar sus datos desde el primer frame.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class Mk2HudBindings : MonoBehaviour
{
    [SerializeField] private PlayerHUDManager hudManager;
    [SerializeField] private PlayerVisualConfig visualConfig;
    [SerializeField] private PlayerHUDSlot[] hudSlots;

    private void Awake()
    {
        if (hudManager == null)
        {
            Debug.LogError("[MK2 HUD] Falta la referencia a PlayerHUDManager.", this);
            return;
        }

        if (visualConfig == null)
        {
            Debug.LogError("[MK2 HUD] Falta la referencia a PlayerVisualConfig.", this);
            return;
        }

        if (hudSlots == null || hudSlots.Length != 4)
        {
            Debug.LogError("[MK2 HUD] Se requieren los cuatro PlayerHUDSlot.", this);
            return;
        }

        hudManager.visualConfig = visualConfig;
        hudManager.hudSlots = hudSlots;
    }
}
