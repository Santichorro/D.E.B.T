using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUDSlot : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image borderOrBackground;   // Se tiñe con el color del jugador
    public Image portraitImage;        // PNG del rol/personaje
    public TMP_Text roleNameText;      // Nombre del rol
    public Image healthFillImage;      // Barra de vida
    public TMP_Text healthPercentText;  // Porcentaje de vida

    [Header("Feedback al morir")]
    [Range(0f, 1f)]
    public float deadPortraitAlpha = 0.35f;

    private PlayerRoleController roleController;
    private Health health;
    private PlayerVisualConfig visualConfig;

    public void SetSlotVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }



    public void Bind(
        PlayerRoleController role,
        Health playerHealth,
        Color playerColor,
        PlayerVisualConfig config)
    {
        // Por si el slot se reutiliza o Bind se llama más de una vez.
        Unbind();

        roleController = role;
        health = playerHealth;
        visualConfig = config;

        if (borderOrBackground != null)
        {
            borderOrBackground.color = playerColor;
        }


        if (roleController != null)
        {
            roleController.OnRoleChanged += OnRoleChanged;
        }


        if (health != null)
        {
            health.OnDamaged += OnHealthDamaged;
            health.OnDeath += OnHealthDeath;
        }

        // Actualizar inmediatamente la información.
        RefreshRoleVisual();
        RefreshHealthVisual();
    }


    private void Unbind()
    {
        if (roleController != null)
        {
            roleController.OnRoleChanged -= OnRoleChanged;
        }

        if (health != null)
        {
            health.OnDamaged -= OnHealthDamaged;
            health.OnDeath -= OnHealthDeath;
        }
    }


    private void OnDestroy()
    {
        Unbind();
    }



    private void OnRoleChanged(PlayerRole newRole)
    {
        RefreshRoleVisual();
    }


    private void OnHealthDamaged(float amount)
    {
        RefreshHealthVisual();
    }

    private void OnHealthDeath()
    {
        RefreshHealthVisual();
    }


    private void RefreshRoleVisual()
    {

        if (roleController == null)
            return;


        if (!roleController.HasRole)
            return;

        if (visualConfig == null)
            return;


        var visual =
            visualConfig.GetRoleVisual(
                roleController.AssignedRole
            );



        if (visual == null)
        {
            Debug.LogWarning(
                $"No existe configuración visual para el rol " +
                $"{roleController.AssignedRole}"
            );

            return;
        }



        if (roleNameText != null)
        {
            roleNameText.text = visual.displayName;
        }



        if (portraitImage != null)
        {
            if (visual.icon != null)
            {
                portraitImage.sprite = visual.icon;

                // Asegurarnos de que la imagen sea visible.
                portraitImage.color = Color.white;
            }
            else
            {
                Debug.LogWarning(
                    $"El rol {roleController.AssignedRole} " +
                    $"no tiene un Icon asignado en PlayerVisualConfig."
                );
            }
        }
    }


    private void Update()
    {
        // Sondeo de respaldo para detectar ResetHealth()
        // después de un respawn.
        RefreshHealthVisual();
    }


    private void RefreshHealthVisual()
    {
        if (health == null)
            return;


        // Evitar división por cero.
        if (health.maxHealth <= 0f)
            return;


        // Calcular porcentaje de vida.
        float pct = Mathf.Clamp01(
            health.CurrentHealth / health.maxHealth
        );



        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = pct;
        }



        if (healthPercentText != null)
        {
            healthPercentText.text =
                Mathf.RoundToInt(pct * 100f) + "%";
        }



        if (portraitImage != null)
        {
            portraitImage.color = health.IsDead
                ? new Color(
                    1f,
                    1f,
                    1f,
                    deadPortraitAlpha
                )
                : Color.white;
        }
    }
}