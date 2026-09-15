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
    private bool introFinished;

    private void OnEnable()
    {
        if (freezePlayersUntilFinished && PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined += HandlePlayerJoined;
        }
    }

    private void OnDisable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= HandlePlayerJoined;
        }
    }

    private void HandlePlayerJoined(PlayerInput playerInput)
    {
        if (playerInput == null)
            return;

        // Si el intro ya terminó o ya se pidió saltarlo,
        // NO congelamos a los jugadores nuevos.
        if (!freezePlayersUntilFinished || introFinished || skipRequested)
        {
            return;
        }

        var controller = playerInput.GetComponent<PlayerController>();
        var physics = playerInput.GetComponent<PlayerPhysics>();

        if (controller == null || physics == null)
            return;

        // Congelar jugador mientras dura el intro
        controller.enabled = false;
        physics.Anchor(9999f);

        // Evitar registrar dos veces al mismo jugador
        foreach (var player in joinedPlayers)
        {
            if (player.controller == controller)
                return;
        }

        joinedPlayers.Add((controller, physics));
    }

    private void UnfreezeAllPlayers()
    {
        // Primero liberar a los jugadores que registramos
        foreach (var (controller, physics) in joinedPlayers)
        {
            if (physics != null && physics.IsAnchored)
            {
                physics.ReleaseAnchor();
            }

            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        joinedPlayers.Clear();

        // Seguridad adicional:
        // liberar TODOS los jugadores que existan actualmente,
        // pero SOLO si de verdad están anclados.
        foreach (PlayerInput playerInput in PlayerInput.all)
        {
            if (playerInput == null)
                continue;

            var controller = playerInput.GetComponent<PlayerController>();
            var physics = playerInput.GetComponent<PlayerPhysics>();

            if (physics != null && physics.IsAnchored)
            {
                physics.ReleaseAnchor();
            }

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }

    private void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);

        StartCoroutine(PlayIntro());
    }

    private void Update()
    {
        if (!introRunning || !allowSkip)
            return;

        // Teclado
        if (Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame)
        {
            RequestSkip();
        }

        // Cualquier botón de cualquier mando
        foreach (var gamepad in Gamepad.all)
        {
            foreach (var control in gamepad.allControls)
            {
                if (control is ButtonControl button &&
                    button.wasPressedThisFrame)
                {
                    RequestSkip();
                    break;
                }
            }

            if (skipRequested)
                break;
        }
    }

    private void RequestSkip()
    {
        if (skipRequested)
            return;

        skipRequested = true;

        // Importante:
        // desde este momento dejamos de congelar jugadores nuevos.
        UnfreezeAllPlayers();
    }

    private IEnumerator PlayIntro()
    {
        introRunning = true;
        introFinished = false;

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

            if (skipRequested)
                break;

            yield return StartCoroutine(FadeText(1f, 0f));

            loreText.text = string.Empty;

            if (!skipRequested)
            {
                yield return StartCoroutine(FadeText(0f, 1f));
            }
        }

        yield return StartCoroutine(Fade(1f, 0f));

        // Marcar que el intro terminó ANTES de liberar jugadores
        introFinished = true;
        introRunning = false;

        canvasGroup.gameObject.SetActive(false);

        // Liberar jugadores
        UnfreezeAllPlayers();

        // Avisar al resto del juego
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

            canvasGroup.alpha =
                Mathf.Lerp(from, to, t / fadeDuration);

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator FadeText(float from, float to)
    {
        float t = 0f;
        float duration = fadeDuration * 0.5f;

        loreText.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;

            loreText.alpha =
                Mathf.Lerp(from, to, t / duration);

            yield return null;
        }

        loreText.alpha = to;
    }
}