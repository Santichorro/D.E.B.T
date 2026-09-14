using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private CanvasGroup teamLogoGroup;
    [SerializeField] private CanvasGroup gameLogoGroup;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float blinkSpeed = 0.6f;

    [Header("Escena destino")]
    [SerializeField] private string nombreEscenaJuego = "Escena_Inicio";

    private bool puedeContinuar = false;
    private bool yaSalto = false;

    private void Start()
    {
        // Estado inicial: todo invisible.
        teamLogoGroup.alpha = 0f;
        gameLogoGroup.alpha = 0f;
        promptText.alpha = 0f;

        StartCoroutine(SecuenciaIntro());
    }

    private IEnumerator SecuenciaIntro()
    {
        // 1) Logo del team: fade in -> hold -> fade out
        yield return StartCoroutine(Fade(teamLogoGroup, 0f, 1f, fadeInDuration));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(Fade(teamLogoGroup, 1f, 0f, fadeOutDuration));

        // 2) Logo del juego: fade in y se queda
        yield return StartCoroutine(Fade(gameLogoGroup, 0f, 1f, fadeInDuration));

        // 3) Habilita el texto titilante y la posibilidad de continuar
        puedeContinuar = true;
        StartCoroutine(TitilarTexto());
    }

    private IEnumerator Fade(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        grupo.alpha = hasta;
    }

    private IEnumerator TitilarTexto()
    {
        while (!yaSalto)
        {
            // Efecto de titileo con una onda seno (más suave que on/off)
            float alpha = (Mathf.Sin(Time.time / blinkSpeed * Mathf.PI) + 1f) / 2f;
            promptText.alpha = alpha;
            yield return null;
        }
    }

    private void Update()
    {
        if (!puedeContinuar || yaSalto) return;

        bool presionoTeclado = Keyboard.current != null &&
                                Keyboard.current.eKey.wasPressedThisFrame;

        bool presionoMando = Gamepad.current != null &&
                              Gamepad.current.buttonWest.wasPressedThisFrame; // Cuadrado en PlayStation

        if (presionoTeclado || presionoMando)
        {
            yaSalto = true;
            CargarJuego();
        }
    }

    private void CargarJuego()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }
}