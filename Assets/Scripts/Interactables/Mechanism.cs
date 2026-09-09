using System.Collections;
using UnityEngine;

/// <summary>
/// Mecanismo que reacciona al Pulso del Demoledor desactivándose temporalmente
/// (sección 4.5 del documento de mecánicas: "Puede desactivar determinados mecanismos").
/// No tiene Health ni recibe daño — solo implementa IPulseReactive, así que PulseAbility
/// lo detecta automáticamente vía TryGetComponent sin necesitar conocer esta clase.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Mechanism : MonoBehaviour, IPulseReactive
{
    [Header("Reacción al Pulso")]
    [SerializeField] private float disableDuration = 3f;

    [Tooltip("Opcional: objeto visual a ocultar mientras está desactivado (mesh, luces, etc.). " +
             "Si se deja vacío, solo se desactiva el Collider.")]
    [SerializeField] private GameObject visualToDisable;

    private Collider mechanismCollider;
    private Coroutine disableRoutine;
    public bool IsDisabled { get; private set; }

    private void Awake()
    {
        mechanismCollider = GetComponent<Collider>();
    }

    public void OnPulseHit(Vector3 origin, float force)
    {
        // Si ya está desactivado, un segundo golpe de pulso simplemente reinicia el temporizador
        // en vez de apilar corrutinas o duplicar el efecto.
        if (disableRoutine != null)
            StopCoroutine(disableRoutine);

        disableRoutine = StartCoroutine(DisableTemporarily());
    }

    private IEnumerator DisableTemporarily()
    {
        SetDisabledState(true);
        yield return new WaitForSeconds(disableDuration);
        SetDisabledState(false);
        disableRoutine = null;
    }

    private void SetDisabledState(bool disabled)
    {
        IsDisabled = disabled;
        mechanismCollider.enabled = !disabled;

        if (visualToDisable != null)
            visualToDisable.SetActive(!disabled);
    }
}