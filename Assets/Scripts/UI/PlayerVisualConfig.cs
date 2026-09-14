using UnityEngine;

[CreateAssetMenu(fileName = "PlayerVisualConfig", menuName = "Config/Player Visual Config")]
public class PlayerVisualConfig : ScriptableObject
{
    [System.Serializable]
    public class RoleVisual
    {
        public PlayerRole role;
        public string displayName;
        public Sprite icon; // el PNG del personaje/rol
    }

    [Header("Colores por índice de jugador (0 = P1, 1 = P2, etc.)")]
    [Tooltip("Debe coincidir con los colores usados en GravityGun.playerColors")]
    public Color[] playerColors = new Color[]
    {
        Color.cyan,
        Color.yellow,
        Color.green,
        Color.magenta
    };

    [Header("Visual por rol (nombre + ícono/PNG)")]
    public RoleVisual[] roleVisuals;

    public Color GetColor(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerColors.Length)
            return playerColors[playerIndex];
        return Color.white;
    }

    public RoleVisual GetRoleVisual(PlayerRole role)
    {
        foreach (var rv in roleVisuals)
        {
            if (rv.role == role) return rv;
        }
        return null;
    }
}
