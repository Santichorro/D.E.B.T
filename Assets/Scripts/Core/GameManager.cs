using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dinero de la partida")]
    [SerializeField] private MissionMoney missionMoney;

    public MissionMoney MissionMoney => missionMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Si no se asignó manualmente, buscarlo en la escena inicial
        if (missionMoney == null)
        {
            missionMoney = FindFirstObjectByType<MissionMoney>();
        }

        if (missionMoney == null)
        {
            Debug.LogError(
                "[GameManager] No se encontró MissionMoney en la escena."
            );
        }
    }
}