using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerCameraGroupManager : MonoBehaviour
{
    [Tooltip("El GameObject que tiene el componente CinemachineTargetGroup")]
    public CinemachineTargetGroup targetGroup;

    [Tooltip("Peso que tiene cada jugador dentro del framing. Igual para todos = misma importancia.")]
    public float memberWeight = 1f;

    [Tooltip("Radio 'imaginario' alrededor del jugador que la cámara respeta como margen extra.")]
    public float memberRadius = 1.5f;

    private PlayerInputManager inputManager;

    private void Start()
    {
        inputManager = PlayerInputManager.instance;

        if (inputManager == null)
        {
            Debug.LogError("No se encontró un PlayerInputManager en la escena.");
            return;
        }

        inputManager.onPlayerJoined += HandlePlayerJoined;
        inputManager.onPlayerLeft += HandlePlayerLeft;
    }

    private void OnDisable()
    {
        if (inputManager == null) return;
        inputManager.onPlayerJoined -= HandlePlayerJoined;
        inputManager.onPlayerLeft -= HandlePlayerLeft;
    }

    private void HandlePlayerJoined(PlayerInput player)
    {
        targetGroup.AddMember(player.transform, memberWeight, memberRadius);
    }

    private void HandlePlayerLeft(PlayerInput player)
    {
        targetGroup.RemoveMember(player.transform);
    }
}