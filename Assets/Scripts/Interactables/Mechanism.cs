using System;
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

    [Header("Efecto al recibir Pulso")]
    [Tooltip("VFX que se reproduce inmediatamente al ser alcanzado por PulseAbility. " +
             "No depende de disableDuration.")]
    [SerializeField] private GameObject pulseHitEffect;
    [SerializeField, Min(0f)] private float pulseHitEffectDuration = 1f;

    private Collider mechanismCollider;
    private Coroutine disableRoutine;
    private Coroutine pulseHitEffectRoutine;
    public bool IsDisabled { get; private set; }
    public event Action<bool> OnDisabledStateChanged;

    private void Awake()
    {
        mechanismCollider = GetComponent<Collider>();
    }

    public void OnPulseHit(Vector3 origin, float force)
    {
        PlayPulseHitEffect();

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
        bool stateChanged = IsDisabled != disabled;
        IsDisabled = disabled;
        mechanismCollider.enabled = !disabled;

        if (stateChanged)
            OnDisabledStateChanged?.Invoke(disabled);
    }

    private void PlayPulseHitEffect()
    {
        if (pulseHitEffect == null) return;

        if (pulseHitEffectRoutine != null)
            StopCoroutine(pulseHitEffectRoutine);

        pulseHitEffect.SetActive(true);

        foreach (ParticleSystem particleSystem in
                 pulseHitEffect.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }

        pulseHitEffectRoutine = StartCoroutine(HidePulseHitEffectRoutine());
    }

    private IEnumerator HidePulseHitEffectRoutine()
    {
        yield return new WaitForSeconds(pulseHitEffectDuration);

        if (pulseHitEffect != null)
            pulseHitEffect.SetActive(false);

        pulseHitEffectRoutine = null;
    }
}
