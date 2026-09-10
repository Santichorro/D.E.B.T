using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Health))]
public class HitFeedback : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El renderer del cuerpo (cápsula/mesh) que cambia a blanco al recibir daño.")]
    public Renderer targetRenderer;
    [Tooltip("El objeto visual que tiembla. Debe ser un hijo separado del Rigidbody, no el mismo objeto.")]
    public Transform shakeTransform;

    [Header("Flash")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;

    [Header("Shake")]
    public float shakeAmount = 0.1f;
    public float shakeDuration = 0.15f;

    [Header("Muerte")]
    public ParticleSystem deathEffectPrefab;
    [Tooltip("Enemigos: true (se destruyen). Jugador: false (lo maneja RespawnSystem).")]
    public bool destroyOnDeath = true;
    public float destroyDelay = 0.05f;

    private Health health;
    private MaterialPropertyBlock propBlock;
    private Color originalColor;
    private Vector3 shakeOriginalLocalPos;
    private Coroutine flashRoutine;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        health = GetComponent<Health>();
        propBlock = new MaterialPropertyBlock();

        if (shakeTransform != null)
            shakeOriginalLocalPos = shakeTransform.localPosition;

        if (targetRenderer != null)
            originalColor = targetRenderer.sharedMaterial.GetColor("_BaseColor");
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    private void HandleDamaged(float amount)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());

        if (shakeTransform != null)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (targetRenderer == null) yield break;

        propBlock.SetColor("_BaseColor", flashColor);
        targetRenderer.SetPropertyBlock(propBlock);

        yield return new WaitForSeconds(flashDuration);

        propBlock.SetColor("_BaseColor", originalColor);
        targetRenderer.SetPropertyBlock(propBlock);
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            Vector3 offset = Random.insideUnitSphere * shakeAmount;
            offset.z = 0f;
            shakeTransform.localPosition = shakeOriginalLocalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeTransform.localPosition = shakeOriginalLocalPos;
    }

    private void HandleDeath()
    {
        if (deathEffectPrefab != null)
        {
            ParticleSystem fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}