using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHUDManager : MonoBehaviour
{
    [Header("Config compartida")]
    public PlayerVisualConfig visualConfig;

    [Header("Slots en orden")]
    [Tooltip("0 = TopLeft, 1 = TopRight, 2 = BottomLeft, 3 = BottomRight")]
    public PlayerHUDSlot[] hudSlots = new PlayerHUDSlot[4];

    private bool[] registeredPlayers = new bool[4];

    private void Awake()
    {
        HideAllSlots();
    }

    private void OnEnable()
    {

        SubscribeToPlayerManager();
    }


    private void Start()
    {
        SubscribeToPlayerManager();

        RegisterExistingPlayers();
    }


    private void OnDisable()
    {
        UnsubscribeFromPlayerManager();
    }

    private void SubscribeToPlayerManager()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= HandlePlayerJoined;
            PlayerInputManager.instance.onPlayerJoined += HandlePlayerJoined;

            Debug.Log("[HUD] PlayerHUDManager conectado al PlayerInputManager.");
        }
    }

    private void UnsubscribeFromPlayerManager()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= HandlePlayerJoined;
        }
    }

    private void RegisterExistingPlayers()
    {
        foreach (PlayerInput playerInput in PlayerInput.all)
        {
            RegisterPlayer(playerInput);
        }
    }

    private void HandlePlayerJoined(PlayerInput playerInput)
    {
        Debug.Log(
            $"[HUD] Nuevo jugador detectado: " +
            $"{playerInput.gameObject.name} | " +
            $"Player Index: {playerInput.playerIndex}"
        );

        RegisterPlayer(playerInput);
    }

    public void RegisterPlayer(PlayerInput playerInput)
    {
        if (playerInput == null)
            return;


        int index = playerInput.playerIndex;


        Debug.Log(
            $"[HUD] Intentando registrar Player {index}: " +
            $"{playerInput.gameObject.name}"
        );


        if (index < 0 || index >= hudSlots.Length)
        {
            Debug.LogWarning(
                $"[HUD] No existe un slot para playerIndex {index}."
            );

            return;
        }


        if (hudSlots[index] == null)
        {
            Debug.LogError(
                $"[HUD] El slot {index} está vacío en el Inspector."
            );

            return;
        }


        if (visualConfig == null)
        {
            Debug.LogError(
                "[HUD] PlayerVisualConfig NO está asignado."
            );

            return;
        }


        PlayerRoleController roleController =
            playerInput.GetComponent<PlayerRoleController>();

        Health health =
            playerInput.GetComponent<Health>();


        if (roleController == null)
        {
            Debug.LogError(
                $"[HUD] Player {index} no tiene PlayerRoleController."
            );

            return;
        }


        if (health == null)
        {
            Debug.LogError(
                $"[HUD] Player {index} no tiene Health."
            );

            return;
        }


        Color color = visualConfig.GetColor(index);


        hudSlots[index].SetSlotVisible(true);


        hudSlots[index].Bind(
            roleController,
            health,
            color,
            visualConfig
        );


        registeredPlayers[index] = true;


        Debug.Log(
            $"[HUD] Player {index} conectado correctamente. " +
            $"Rol actual: {roleController.AssignedRole}"
        );
    }


    private void Update()
    {

        foreach (PlayerInput playerInput in PlayerInput.all)
        {
            if (playerInput == null)
                continue;


            int index = playerInput.playerIndex;


            if (index < 0 || index >= registeredPlayers.Length)
                continue;


            if (!registeredPlayers[index])
            {
                Debug.Log(
                    $"[HUD] Detectado jugador no registrado mediante respaldo: Player {index}"
                );

                RegisterPlayer(playerInput);
            }
        }
    }


    private void HideAllSlots()
    {
        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] != null)
            {
                hudSlots[i].SetSlotVisible(false);
            }

            if (i < registeredPlayers.Length)
            {
                registeredPlayers[i] = false;
            }
        }
    }
}
