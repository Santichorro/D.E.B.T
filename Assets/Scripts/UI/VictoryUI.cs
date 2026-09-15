using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class VictoryUI : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject divertidoPanel;

    [Header("Texto de victoria")]
    [SerializeField] private TMP_Text victoryMoneyText;

    [Header("Sonido")]
    [SerializeField] private AudioSource menuAudioSource;

    [Header("Condición de victoria")]
    [SerializeField] private int requiredMoney = 8000;

    [Header("Prueba temporal")]
    [SerializeField] private Key testVictoryKey = Key.V;

    private MissionMoney missionMoney;
    private bool victoryShown = false;

    private void Awake()
    {
        victoryPanel.SetActive(false);
        divertidoPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void Start()
    {
        missionMoney = MissionMoney.Instance;

        if (missionMoney == null)
        {
            missionMoney = FindFirstObjectByType<MissionMoney>();
        }
    }

    private void Update()
    {
        // PRUEBA TEMPORAL CON V
        if (Keyboard.current != null &&
            Keyboard.current[testVictoryKey].wasPressedThisFrame)
        {
            ShowVictory();
        }

        // VICTORIA REAL AL LLEGAR A $8.000
        if (!victoryShown &&
            missionMoney != null &&
            missionMoney.CurrentMoney >= requiredMoney)
        {
            ShowVictory();
        }
    }

    public void ShowVictory()
    {
        if (victoryShown)
            return;

        victoryShown = true;

        PlayMenuSound();

        // Buscar MissionMoney por seguridad
        if (missionMoney == null)
        {
            missionMoney = MissionMoney.Instance;

            if (missionMoney == null)
            {
                missionMoney = FindFirstObjectByType<MissionMoney>();
            }
        }

        // Mostrar la cantidad de dinero recolectada
        if (victoryMoneyText != null)
        {
            int currentMoney = 0;

            if (missionMoney != null)
            {
                currentMoney = missionMoney.CurrentMoney;
            }

            victoryMoneyText.text = $"${currentMoney:N0}";
        }

        victoryPanel.SetActive(true);
        divertidoPanel.SetActive(false);

        Time.timeScale = 0f;
    }

    public void ContinueToFunnyPanel()
    {
        PlayMenuSound();

        victoryPanel.SetActive(false);
        divertidoPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void BackToVictoryPanel()
    {
        PlayMenuSound();

        divertidoPanel.SetActive(false);
        victoryPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void TryAgain()
    {
        StartCoroutine(RestartAfterSound());
    }

    private System.Collections.IEnumerator RestartAfterSound()
    {
        PlayMenuSound();

        yield return new WaitForSecondsRealtime(0.2f);

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

        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void PlayMenuSound()
    {
        if (menuAudioSource != null)
        {
            menuAudioSource.Play();
        }
    }
}