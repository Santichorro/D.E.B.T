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

    private bool isPaused = false;

    private void Awake()
    {
        Time.timeScale = 1f;

        pauseMenuPanel.SetActive(false);
        characterInfoPanel.SetActive(false);
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
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;

        pauseMenuPanel.SetActive(true);
        characterInfoPanel.SetActive(false);

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        pauseMenuPanel.SetActive(false);
        characterInfoPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void OpenCharacterInfo()
    {
        pauseMenuPanel.SetActive(false);
        characterInfoPanel.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        characterInfoPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
