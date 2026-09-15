using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carga una escena una sola vez cuando la nave entra en este trigger.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class NextSceneTrigger : MonoBehaviour
{
    [Header("Escena destino")]
    [SerializeField] private string destinationScene = "SEGUNDO NIVEL";

    private bool isLoading;

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading || other == null)
            return;

        // La nave puede tener el collider en su raíz o en un hijo. Ningún otro
        // objeto puede activar la transición si no pertenece a ShipController.
        if (other.GetComponentInParent<ShipController>() == null)
            return;

        if (string.IsNullOrWhiteSpace(destinationScene))
        {
            Debug.LogError("[NextSceneTrigger] Asigna una escena destino en el Inspector.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError(
                $"[NextSceneTrigger] La escena '{destinationScene}' no existe o no está habilitada en Build Settings.",
                this);
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(destinationScene);
    }

    private void EnsureTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }
}
