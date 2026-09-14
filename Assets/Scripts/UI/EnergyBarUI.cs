using UnityEngine;
using UnityEngine.UI;

public class EnergyBarUI : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;

    [Header("Rotación fija")]
    [Tooltip("Rotación mundial que debe mantener la barra sin importar cómo rote el jugador.")]
    public Vector3 fixedWorldRotation = Vector3.zero;

    private GravityGun myGun;
    private PlayerRoleController roleController;

    private void Awake()
    {
        myGun = GetComponentInParent<GravityGun>();
        roleController = GetComponentInParent<PlayerRoleController>();
    }

    private void OnEnable()
    {
        if (roleController != null)
            roleController.OnRoleChanged += OnRoleChanged;
    }

    private void OnDisable()
    {
        if (roleController != null)
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

    private void Update()
    {
        if (myGun == null || slider == null) return;

        slider.value = myGun.EnergyNormalized;

    }

    private void RefreshRoleColor()
    {
        if (fillImage != null && roleController != null && roleController.HasRole)
            fillImage.color = roleController.AssignedRoleColor;
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(fixedWorldRotation);
    }
}
