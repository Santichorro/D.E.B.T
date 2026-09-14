using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class BlackHoleDeactivator : MonoBehaviour
{
    public event Action<BlackHoleDeactivator, PlayerInput> OnActivated;

    private readonly Dictionary<PlayerInput, PlayerInteractionBinding> nearbyPlayers =
        new Dictionary<PlayerInput, PlayerInteractionBinding>();

    private void OnTriggerEnter(Collider other)
    {
        PlayerInput playerInput = other.GetComponentInParent<PlayerInput>();
        if (playerInput == null || !playerInput.CompareTag("Player"))
            return;

        if (nearbyPlayers.TryGetValue(playerInput, out PlayerInteractionBinding existingBinding))
        {
            existingBinding.AddOverlap();
            return;
        }

        NIS inputActions = new NIS();
        inputActions.devices = playerInput.devices;
        Action<InputAction.CallbackContext> handler = _ => OnActivated?.Invoke(this, playerInput);

        inputActions.Player.Interact.started += handler;
        inputActions.Player.Enable();
        nearbyPlayers.Add(playerInput, new PlayerInteractionBinding(inputActions, handler));
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInput playerInput = other.GetComponentInParent<PlayerInput>();
        if (playerInput != null)
            RemoveNearbyPlayerOverlap(playerInput);
    }

    private void OnDisable()
    {
        foreach (PlayerInteractionBinding binding in nearbyPlayers.Values)
            binding.Dispose();

        nearbyPlayers.Clear();
    }

    private void RemoveNearbyPlayerOverlap(PlayerInput playerInput)
    {
        if (!nearbyPlayers.TryGetValue(playerInput, out PlayerInteractionBinding binding))
            return;

        if (binding.RemoveOverlap() > 0)
            return;

        binding.Dispose();
        nearbyPlayers.Remove(playerInput);
    }

    private sealed class PlayerInteractionBinding
    {
        private readonly NIS inputActions;
        private readonly Action<InputAction.CallbackContext> handler;
        private int overlapCount = 1;

        public PlayerInteractionBinding(NIS inputActions, Action<InputAction.CallbackContext> handler)
        {
            this.inputActions = inputActions;
            this.handler = handler;
        }

        public void Dispose()
        {
            inputActions.Player.Interact.started -= handler;
            inputActions.Player.Disable();
            inputActions.Dispose();
        }

        public void AddOverlap() => overlapCount++;

        public int RemoveOverlap()
        {
            overlapCount = Mathf.Max(0, overlapCount - 1);
            return overlapCount;
        }
    }
}
