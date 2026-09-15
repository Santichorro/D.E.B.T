using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class IntroLoreSequence : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text loreText;

    [Header("Contenido del lore")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] paragraphs = new string[]
    {
        "En el año en que las rutas comerciales del sistema colapsaron, decenas de estaciones, minas y naves quedaron a la deriva, abandonadas junto con todo lo que alguna vez tuvieron valor.",
        "Ustedes son cuatro miembros de un Escuadrón de Recuperación: un Piloto, un Ingeniero, un Artillero y un Demoledor, reunidos a bordo de la nave nodriza.",
        "Su misión: rescatar cargamentos perdidos, tecnología valiosa y recursos vitales de esos lugares olvidados, antes de que se pierdan para siempre entre los restos.",
        "Cada fragmento que recuperen puede significar la diferencia entre la supervivencia y el olvido para las colonias que aún resisten."
    };

    [Header("Timing")]
    [SerializeField] private float typeSpeed = 0.03f;
    [SerializeField] private float pauseBetweenParagraphs = 1.8f;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private bool allowSkip = true;

    [Header("Jugadores durante el intro")]
    [SerializeField] private bool freezePlayersUntilFinished = true;
    private readonly List<(PlayerController controller, PlayerPhysics physics)> joinedPlayers = new();

    [Header("Eventos")]
    public UnityEvent OnIntroFinished;

    private bool skipRequested;
    private bool introRunning;

    private void OnEnable()
    {
        if (freezePlayersUntilFinished && PlayerInputManager.instance != null)
            PlayerInputManager.instance.onPlayerJoined += HandlePlayerJoined;
    }

    private void OnDisable()
    {
        if (PlayerInputManager.instance != null)
            PlayerInputManager.instance.onPlayerJoined -= HandlePlayerJoined;
    }

    private void HandlePlayerJoined(PlayerInput playerInput)
    {
        var controller = playerInput.GetComponent<PlayerController>();
        var physics = playerInput.GetComponent<PlayerPhysics>();
        if (controller == null || physics == null) return;

        controller.enabled = false;          // no lee input mientras dura el lore
        physics.Anchor(9999f);               // congela fuerzas/velocidad sin tocar isKinematic

        joinedPlayers.Add((controller, physics));
    }

    private void UnfreezeAllPlayers()
    {
        foreach (var (controller, physics) in joinedPlayers)
        {
            if (physics == null || controller == null) continue; // por si algo lo destruyó mientras tanto

            physics.ReleaseAnchor();
            controller.enabled = true;
        }
        joinedPlayers.Clear();
    }

    private void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);
        StartCoroutine(PlayIntro());
    }

    private void Update()
    {
        if (!introRunning || !allowSkip) return;

        // Cualquier botón de cualquier jugador/dispositivo salta el intro.
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            skipRequested = true;

        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.allControls.Count > 0)
            {
                foreach (var control in gamepad.allControls)
                {
                    if (control is ButtonControl button && button.wasPressedThisFrame)
                    {
                        skipRequested = true;
                        break;
                    }
                }
            }
            if (skipRequested) break;
        }
    }

    private IEnumerator PlayIntro()
    {
        introRunning = true;
        yield return StartCoroutine(Fade(0f, 1f));

        foreach (string paragraph in paragraphs)
        {
            yield return StartCoroutine(TypeParagraph(paragraph));

            float t = 0f;
            while (t < pauseBetweenParagraphs && !skipRequested)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (skipRequested) break;

            yield return StartCoroutine(FadeText(1f, 0f));
            loreText.text = string.Empty;
            if (!skipRequested)
                yield return StartCoroutine(FadeText(0f, 1f));
        }

        yield return StartCoroutine(Fade(1f, 0f));

        introRunning = false;
        canvasGroup.gameObject.SetActive(false);

        UnfreezeAllPlayers();
        OnIntroFinished?.Invoke();
    }

    private IEnumerator TypeParagraph(string paragraph)
    {
        loreText.text = string.Empty;
        loreText.alpha = 1f;

        foreach (char c in paragraph)
        {
            if (skipRequested)
            {
                loreText.text = paragraph;
                yield break;
            }

            loreText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator FadeText(float from, float to)
    {
        float t = 0f;
        loreText.alpha = from;
        while (t < fadeDuration * 0.5f)
        {
            t += Time.deltaTime;
            loreText.alpha = Mathf.Lerp(from, to, t / (fadeDuration * 0.5f));
            yield return null;
        }
        loreText.alpha = to;
    }
}