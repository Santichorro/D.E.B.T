using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class HitFeedback : MonoBehaviour
{
    private const string BaseColorProperty = "_BaseColor";
    private const string ColorProperty = "_Color";

    [Header("Referencias")]
    [Tooltip("Fuente de daño opcional. Úsala cuando el visual sea hijo de un objeto que posee el Health real, como la nave.")]
    [SerializeField] private Health healthSource;
    [Tooltip("Renderer principal opcional. Si está vacío, se resuelve desde PlayerSkinColorizer o el hijo Visual.")]
    public Renderer targetRenderer;
    [Tooltip("Objeto visual que tiembla. Si está vacío, se usa el hijo Visual cuando exista.")]
    public Transform shakeTransform;

    [Header("Flash")]
    public Color flashColor = Color.white;
    [Min(0f)] public float flashDuration = 0.1f;

    [Header("Shake")]
    public float shakeAmount = 0.1f;
    [Min(0f)] public float shakeDuration = 0.15f;

    [Header("Muerte")]
    public ParticleSystem deathEffectPrefab;
    [Tooltip("Enemigos: true. Jugador: false, porque lo administra RespawnSystem.")]
    public bool destroyOnDeath = true;
    [Min(0f)] public float destroyDelay = 0.05f;

    private sealed class FlashState
    {
        public Renderer Renderer;
        public Material Material;
        public MaterialPropertyBlock PropertyBlock;
        public int ColorPropertyId;
        public Color OriginalColor;
    }

    private Health health;
    private FlashState[] flashStates = System.Array.Empty<FlashState>();
    private Vector3 shakeOriginalLocalPos;
    private Coroutine flashRoutine;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        health = healthSource != null ? healthSource : GetComponent<Health>();

        if (health == null)
        {
            Debug.LogError("[HitFeedback] No se encontró Health para escuchar el daño.", this);
            enabled = false;
            return;
        }

        ResolveVisualReferences();
        CacheFlashStates();

        if (shakeTransform != null)
            shakeOriginalLocalPos = shakeTransform.localPosition;
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            RestoreFlashColors();
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
            RestoreShakePosition();
        }
    }

    private void HandleDamaged(float amount)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            RestoreFlashColors();
        }

        flashRoutine = StartCoroutine(FlashRoutine());

        if (shakeTransform != null)
        {
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(ShakeRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        ApplyFlashColor();
        yield return new WaitForSeconds(flashDuration);
        RestoreFlashColors();
        flashRoutine = null;
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

        RestoreShakePosition();
        shakeRoutine = null;
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

    private void ResolveVisualReferences()
    {
        Transform visualRoot = transform.Find("Visual");

        if (targetRenderer == null)
        {
            PlayerSkinColorizer skinColorizer = GetComponent<PlayerSkinColorizer>();
            if (skinColorizer != null && skinColorizer.skinParts != null)
            {
                for (int i = 0; i < skinColorizer.skinParts.Length; i++)
                {
                    if (skinColorizer.skinParts[i] != null)
                    {
                        targetRenderer = skinColorizer.skinParts[i];
                        break;
                    }
                }
            }

            if (targetRenderer == null)
            {
                Transform searchRoot = visualRoot != null ? visualRoot : transform;
                targetRenderer = searchRoot.GetComponentInChildren<Renderer>(true);
            }
        }

        if (shakeTransform == null)
            shakeTransform = visualRoot != null ? visualRoot : targetRenderer != null ? targetRenderer.transform : null;
    }

    private void CacheFlashStates()
    {
        List<Renderer> renderers = new List<Renderer>();

        PlayerSkinColorizer skinColorizer = GetComponent<PlayerSkinColorizer>();
        if (skinColorizer != null && skinColorizer.skinParts != null)
        {
            for (int i = 0; i < skinColorizer.skinParts.Length; i++)
                AddRendererIfUnique(renderers, skinColorizer.skinParts[i]);
        }

        AddRendererIfUnique(renderers, targetRenderer);

        if (renderers.Count == 0)
        {
            Transform visualRoot = transform.Find("Visual");
            Renderer[] discoveredRenderers = (visualRoot != null ? visualRoot : transform)
                .GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < discoveredRenderers.Length; i++)
                AddRendererIfUnique(renderers, discoveredRenderers[i]);
        }

        List<FlashState> states = new List<FlashState>(renderers.Count);

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer renderer = renderers[i];
            Material material = renderer != null ? renderer.sharedMaterial : null;

            if (material == null)
                continue;

            int propertyId = material.HasProperty(BaseColorProperty)
                ? Shader.PropertyToID(BaseColorProperty)
                : material.HasProperty(ColorProperty)
                    ? Shader.PropertyToID(ColorProperty)
                    : 0;

            if (propertyId == 0)
                continue;

            states.Add(new FlashState
            {
                Renderer = renderer,
                Material = material,
                PropertyBlock = new MaterialPropertyBlock(),
                ColorPropertyId = propertyId
            });
        }

        flashStates = states.ToArray();
    }

    private static void AddRendererIfUnique(List<Renderer> renderers, Renderer renderer)
    {
        if (renderer != null && !renderers.Contains(renderer))
            renderers.Add(renderer);
    }

    private void ApplyFlashColor()
    {
        for (int i = 0; i < flashStates.Length; i++)
        {
            FlashState state = flashStates[i];
            if (state.Renderer == null || state.Material == null)
                continue;

            state.PropertyBlock.Clear();
            state.Renderer.GetPropertyBlock(state.PropertyBlock);

            Color colorFromBlock = state.PropertyBlock.GetColor(state.ColorPropertyId);
            state.OriginalColor = colorFromBlock != default
                ? colorFromBlock
                : state.Material.GetColor(state.ColorPropertyId);

            state.PropertyBlock.SetColor(state.ColorPropertyId, flashColor);
            state.Renderer.SetPropertyBlock(state.PropertyBlock);
        }
    }

    private void RestoreFlashColors()
    {
        for (int i = 0; i < flashStates.Length; i++)
        {
            FlashState state = flashStates[i];
            if (state.Renderer == null)
                continue;

            state.PropertyBlock.SetColor(state.ColorPropertyId, state.OriginalColor);
            state.Renderer.SetPropertyBlock(state.PropertyBlock);
        }
    }

    private void RestoreShakePosition()
    {
        if (shakeTransform != null)
            shakeTransform.localPosition = shakeOriginalLocalPos;
    }

    private void OnValidate()
    {
        flashDuration = Mathf.Max(0f, flashDuration);
        shakeDuration = Mathf.Max(0f, shakeDuration);
        destroyDelay = Mathf.Max(0f, destroyDelay);
    }
}
