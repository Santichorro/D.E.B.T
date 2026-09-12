using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerRole
{
    Piloto,
    Ingeniero,
    Artillero,
    Demoledor,
}

[System.Serializable]
public class RoleAbilityBinding
{
    public PlayerRole role;
    public MonoBehaviour abilityComponent;

    // Acceso tipado como IAbility, sin cambiar cómo se asigna en el Inspector.
    public IAbility Ability => abilityComponent as IAbility;
}

[RequireComponent(typeof(PlayerInput))]
public class PlayerRoleController : MonoBehaviour
{
    [SerializeField] private RoleAbilityBinding[] abilityBindings;

    private static readonly HashSet<PlayerRole> takenRoles = new HashSet<PlayerRole>();

    public PlayerRole AssignedRole { get; private set; }
    public bool HasRole { get; private set; }

    public IAbility CurrentAbility { get; private set; }

    public event Action<PlayerRole> OnRoleChanged;

    private PlayerInput playerInput;
    private NIS inputActions;
    private PlayerRoleController roleController;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inputActions = new NIS();

        foreach (var binding in abilityBindings)
        {
            if (binding.abilityComponent == null) continue;

            if (binding.Ability == null)
            {
                Debug.LogError($"[{gameObject.name}] El componente asignado a '{binding.role}' " +
                                $"({binding.abilityComponent.GetType().Name}) no implementa IAbility.", this);
            }

            binding.abilityComponent.enabled = false;
        }
    }

    private void OnEnable()
    {
        inputActions.devices = playerInput.devices;
        inputActions.Player.Enable();
        inputActions.Player.Interact.performed += OnInteractInput;

        AssignFirstAvailableRole();
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteractInput;
        inputActions.Player.Disable();

        ReleaseRole();
    }

    private void OnInteractInput(InputAction.CallbackContext ctx)
    {
        CycleToNextRole();
    }

    private void AssignFirstAvailableRole()
    {
        if (HasRole) return;

        foreach (PlayerRole role in Enum.GetValues(typeof(PlayerRole)))
        {
            if (TryAssignRole(role)) break;
        }
    }

    public void CycleToNextRole()
    {
        var roles = (PlayerRole[])Enum.GetValues(typeof(PlayerRole));
        int startIndex = HasRole ? ((int)AssignedRole + 1) % roles.Length : 0;

        for (int i = 0; i < roles.Length; i++)
        {
            PlayerRole candidate = roles[(startIndex + i) % roles.Length];
            if (TryAssignRole(candidate)) return;
        }
    }

    public static bool IsRoleTaken(PlayerRole role)
    {
        return takenRoles.Contains(role);
    }

    public bool TryAssignRole(PlayerRole role)
    {
        if (takenRoles.Contains(role) && (!HasRole || AssignedRole != role)) return false;

        if (HasRole)
            takenRoles.Remove(AssignedRole);

        AssignedRole = role;
        HasRole = true;
        takenRoles.Add(role);

        ApplyRole(role);
        return true;
    }

    public void ReleaseRole()
    {
        if (!HasRole) return;

        takenRoles.Remove(AssignedRole);
        HasRole = false;
        CurrentAbility = null;

        foreach (var binding in abilityBindings)
        {
            if (binding.abilityComponent != null)
                binding.abilityComponent.enabled = false;
        }
    }

    private void ApplyRole(PlayerRole role)
    {
        CurrentAbility = null;

        foreach (var binding in abilityBindings)
        {
            if (binding.abilityComponent == null) continue;

            bool isActive = binding.role == role;
            binding.abilityComponent.enabled = isActive;

            if (isActive)
                CurrentAbility = binding.Ability;
        }

        OnRoleChanged?.Invoke(role);
    }

    public static void ResetAllRoles()
    {
        takenRoles.Clear();
    }

    private void Start()
    {
        roleController = GetComponent<PlayerRoleController>();

        if (roleController.HasRole)
        {
            Debug.Log($"Rol asignado: {roleController.AssignedRole}");
        }
    }
}