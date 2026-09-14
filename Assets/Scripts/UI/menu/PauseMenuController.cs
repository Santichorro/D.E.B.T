using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject characterInfoPanel;

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Sonido")]
    [SerializeField] private AudioSource menuAudioSource;

    private bool isPaused = false;


    private void Awake()
    {
        Time.timeScale = 1f;

        pauseMenuPanel.SetActive(false);
        characterInfoPanel.SetActive(false);

        isPaused = false;
    }


    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        PlayMenuSound();

        if (isPaused)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }



    private void PlayMenuSound()
    {
        if (menuAudioSource != null)
        {
            menuAudioSource.Play();
        }
    }


    public void OpenPauseMenu()
    {
        isPaused = true;

        pauseMenuPanel.SetActive(true);
        characterInfoPanel.SetActive(false);

        Time.timeScale = 0f;
    }


    public void ClosePauseMenu()
    {
        isPaused = false;

        pauseMenuPanel.SetActive(false);
        characterInfoPanel.SetActive(false);

        Time.timeScale = 1f;
    }


    public void Resume()
    {
        PlayMenuSound();

        ClosePauseMenu();
    }


    public void OpenCharacterInfo()
    {
        PlayMenuSound();

        pauseMenuPanel.SetActive(false);
        characterInfoPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void BackToPauseMenu()
    {
        PlayMenuSound();

        characterInfoPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Restart()
    {
        StartCoroutine(RestartAfterSound());
    }

    private System.Collections.IEnumerator RestartAfterSound()
    {
        PlayMenuSound();

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void QuitGame()
    {
        StartCoroutine(QuitAfterSound());
    }

    private System.Collections.IEnumerator QuitAfterSound()
    {
        PlayMenuSound();

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}