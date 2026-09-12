using UnityEngine;

[RequireComponent(typeof(GravityGun))]
public class PlayerSkinColorizer : MonoBehaviour
{
    [Header("Partes de la skin")]
    [Tooltip("Arrastra aquí los Renderers de las partes del cuerpo que deben teñirse con el color del jugador (torso, casco, detalles, etc). Deja fuera las partes que NO deben cambiar (ej: visor, piel).")]
    public Renderer[] skinParts;

    private GravityGun gravityGun;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        gravityGun = GetComponent<GravityGun>();
        propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Se ejecuta después de que PlayerInput ya asignó el playerIndex real
        // (al hacer join desde el lobby), así que el color siempre es el correcto.
        ApplyColor(gravityGun.GetAssignedColor());
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