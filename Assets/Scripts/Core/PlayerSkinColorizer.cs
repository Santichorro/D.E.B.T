using UnityEngine;

[RequireComponent(typeof(PlayerRoleController))]
public class PlayerSkinColorizer : MonoBehaviour
{
    [Header("Partes de la skin")]
    [Tooltip("Arrastra aquí los Renderers de las partes del cuerpo que deben teñirse con el color del jugador (torso, casco, detalles, etc). Deja fuera las partes que NO deben cambiar (ej: visor, piel).")]
    public Renderer[] skinParts;

    private PlayerRoleController roleController;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        roleController = GetComponent<PlayerRoleController>();
        propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        roleController.OnRoleChanged += OnRoleChanged;
    }

    private void OnDisable()
    {
        roleController.OnRoleChanged -= OnRoleChanged;
    }

    private void Start()
    {
        RefreshRoleColor();
    }

    private void OnRoleChanged(PlayerRole role)
    {
        RefreshRoleColor();
    }

    private void RefreshRoleColor()
    {
        if (roleController != null && roleController.HasRole)
            ApplyColor(roleController.AssignedRoleColor);
    }

    public void ApplyColor(Color color)
    {
        foreach (var part in skinParts)
        {
            if (part == null) continue;

            part.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", color);
            part.SetPropertyBlock(propBlock);
        }
    }
}
