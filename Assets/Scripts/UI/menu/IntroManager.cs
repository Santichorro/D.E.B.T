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
    [SerializeField] private CanvasGroup promptGroup;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float blinkSpeed = 0.6f;

    [Header("Escena destino")]
    [SerializeField] private string nombreEscenaJuego = "Escena_Inicio";

    [Header("Sonido")]
    [Tooltip("AudioSource que reproducirá el sonido al confirmar inicio. Si se deja vacío, se buscará uno en este GameObject.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sonido que se reproduce al presionar el botón para iniciar el juego.")]
    [SerializeField] private AudioClip confirmSound;
    [Tooltip("Segundos de espera antes de cargar la siguiente escena, para que el sonido de inicio se alcance a escuchar.")]
    [SerializeField] private float delayAntesDeCargar = 3f;

    private bool puedeContinuar = false;
    private bool yaSalto = false;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        teamLogoGroup.alpha = 0f;
        gameLogoGroup.alpha = 0f;
        promptGroup.alpha = 0f;

        StartCoroutine(SecuenciaIntro());
    }

    private IEnumerator SecuenciaIntro()
    {
        yield return StartCoroutine(Fade(teamLogoGroup, 0f, 1f, fadeInDuration));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(Fade(teamLogoGroup, 1f, 0f, fadeOutDuration));

        yield return StartCoroutine(Fade(gameLogoGroup, 0f, 1f, fadeInDuration));

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
            float alpha = (Mathf.Sin(Time.time / blinkSpeed * Mathf.PI) + 1f) / 2f;
            promptGroup.alpha = alpha;
            yield return null;
        }
    }

    private void Update()
    {
        if (!puedeContinuar || yaSalto) return;

        bool presionoTeclado = Keyboard.current != null &&
                                Keyboard.current.eKey.wasPressedThisFrame;

        bool presionoMando = Gamepad.current != null &&
                              Gamepad.current.buttonWest.wasPressedThisFrame;

        if (presionoTeclado || presionoMando)
        {
            yaSalto = true;
            PlayConfirmSound();
            StartCoroutine(CargarJuegoRoutine());
        }
    }

    private void PlayConfirmSound()
    {
        if (audioSource != null && confirmSound != null)
            audioSource.PlayOneShot(confirmSound);
    }

    private IEnumerator CargarJuegoRoutine()
    {
        yield return new WaitForSeconds(delayAntesDeCargar);

        CargarJuego();
    }

    private void CargarJuego()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }
}