using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerRole
{
    // El orden de esta lista define el orden de asignación y de cambio de rol.
    // No reordenar sus miembros sin actualizar los assets serializados que usan PlayerRole.
    Demoledor,
    Ingeniero,
    Artillero,
    Piloto
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
    [SerializeField] private PlayerVisualConfig visualConfig;

    private static readonly HashSet<PlayerRole> takenRoles = new HashSet<PlayerRole>();
    private static readonly PlayerRole[] rolesInEnumOrder =
        (PlayerRole[])Enum.GetValues(typeof(PlayerRole));

    public PlayerRole AssignedRole { get; private set; }
    public bool HasRole { get; private set; }
    public Color AssignedRoleColor => HasRole && visualConfig != null
        ? visualConfig.GetRoleColor(AssignedRole)
        : Color.white;

    /// <summary>
    /// La habilidad actualmente activa como IAbility, o null si no hay rol asignado
    /// o el binding no tiene componente. Útil para UI (cooldown) sin conocer el tipo concreto.
    /// </summary>
    public IAbility CurrentAbility { get; private set; }

    /// <summary>Se dispara cada vez que el rol asignado cambia (por interact o al hacer join). Útil para refrescar UI sin que esta tenga que hacer polling.</summary>
    public event Action<PlayerRole> OnRoleChanged;

    private PlayerInput playerInput;
    private NIS inputActions;
    private PlayerRoleController roleController;
    private bool canCycleRoles;
    private Coroutine enableRoleCyclingRoutine;

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

        // El botón que crea un PlayerInput también puede disparar Interact en este
        // mismo frame. Esperar un frame evita que ese input de join cambie el rol
        // recién asignado por el enum.
        canCycleRoles = false;
        enableRoleCyclingRoutine = StartCoroutine(EnableRoleCyclingNextFrame());
    }

    private void OnDisable()
    {
        if (enableRoleCyclingRoutine != null)
        {
            StopCoroutine(enableRoleCyclingRoutine);
            enableRoleCyclingRoutine = null;
        }

        canCycleRoles = false;
        inputActions.Player.Interact.performed -= OnInteractInput;
        inputActions.Player.Disable();

        ReleaseRole();
    }

    private void OnInteractInput(InputAction.CallbackContext ctx)
    {
        if (!canCycleRoles) return;

        CycleToNextRole();
    }

    private System.Collections.IEnumerator EnableRoleCyclingNextFrame()
    {
        yield return null;
        canCycleRoles = true;
        enableRoleCyclingRoutine = null;
    }

    private void AssignFirstAvailableRole()
    {
        if (HasRole) return;

        foreach (PlayerRole role in rolesInEnumOrder)
        {
            if (TryAssignRole(role)) break;
        }
    }

    public void CycleToNextRole()
    {
        int currentIndex = Array.IndexOf(rolesInEnumOrder, AssignedRole);
        int startIndex = HasRole && currentIndex >= 0
            ? (currentIndex + 1) % rolesInEnumOrder.Length
            : 0;

        for (int i = 0; i < rolesInEnumOrder.Length; i++)
        {
            PlayerRole candidate = rolesInEnumOrder[
                (startIndex + i) % rolesInEnumOrder.Length
            ];
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
