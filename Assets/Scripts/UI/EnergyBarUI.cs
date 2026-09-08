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

    private void Awake()
    {
        myGun = GetComponentInParent<GravityGun>();
    }

    private void Update()
    {
        if (myGun == null || slider == null) return;

        slider.value = myGun.EnergyNormalized;

        if (fillImage != null)
        {
            int index = myGun.GetComponent<UnityEngine.InputSystem.PlayerInput>().playerIndex;
            if (index >= 0 && index < myGun.playerColors.Length)
                fillImage.color = myGun.playerColors[index];
        }
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(fixedWorldRotation);
    }
}