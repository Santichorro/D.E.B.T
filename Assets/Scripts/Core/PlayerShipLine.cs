using UnityEngine;

[RequireComponent(typeof(PlayerPhysics))]
public class PlayerShipLine : MonoBehaviour
{
    [Header("Referencia a la nave")]
    [Tooltip("Si lo dejás vacío, busca un objeto con tag 'Ship' al iniciar.")]
    [SerializeField] private Transform naveTransform;

    [Header("Configuración del tether")]
    [SerializeField] private float distanciaMaxima = 25f;
    [Tooltip("Fuerza de tirón una vez pasada la zona de transición.")]
    [SerializeField] private float fuerzaTiron = 15f;
    [Tooltip("Distancia extra después del límite en la que la fuerza sube gradualmente en vez de aplicarse de golpe.")]
    [SerializeField] private float zonaDeTransicion = 5f;
    [Tooltip("Mientras esté en false, no se aplica fuerza y el cable se oculta. Se controla desde afuera (ej. el script que maneja el pilotaje de la nave).")]
    [SerializeField] private bool tetherActivo = true;

    [Header("Visual - Cable")]
    [Tooltip("LineRenderer propio del tether. Se prende solo mientras se está tirando del jugador, no mientras está dentro del rango permitido.")]
    [SerializeField] private LineRenderer cableRenderer;
    [Tooltip("Punto del jugador desde el que sale el cable (ej. la espalda o el centro). Si lo dejás vacío usa transform.position.")]
    [SerializeField] private Transform anchorPointJugador;
    [Tooltip("Color usado hasta que se asigne un rol, o si no hay PlayerRoleController en este objeto.")]
    [SerializeField] private Color colorPorDefecto = Color.cyan;

    private PlayerPhysics playerPhysics;
    private PlayerRoleController roleController;

    private void Awake()
    {
        playerPhysics = GetComponent<PlayerPhysics>();
        roleController = GetComponent<PlayerRoleController>();

        if (naveTransform == null)
        {
            GameObject nave = GameObject.FindGameObjectWithTag("Ship");
            if (nave != null)
            {
                naveTransform = nave.transform;
            }
            else
            {
                Debug.LogWarning($"{name}: no se asignó 'naveTransform' ni se encontró un objeto con tag 'Ship'.");
            }
        }

        if (cableRenderer != null)
            cableRenderer.enabled = true;
    }

    private void OnEnable()
    {
        if (roleController != null)
            roleController.OnRoleChanged += OnRoleChanged;
    }

    private void OnDisable()
    {
        if (roleController != null)
            roleController.OnRoleChanged -= OnRoleChanged;
    }

    private void Start()
    {
        RefreshRoleColor();
    }

    private void OnRoleChanged(PlayerRole role)
    {
        RefreshRoleColor();
    }

    private void RefreshRoleColor()
    {
        if (cableRenderer == null) return;

        Color color = (roleController != null && roleController.HasRole)
            ? roleController.AssignedRoleColor
            : colorPorDefecto;

        cableRenderer.startColor = color;
        cableRenderer.endColor = color;
    }

    private void FixedUpdate()
    {
        if (naveTransform == null) return;

        if (!tetherActivo)
        {
            if (cableRenderer != null) cableRenderer.enabled = false;
            return;
        }

        DibujarCable();
        if (playerPhysics.IsAnchored) return;

        Vector3 desdeNave = transform.position - naveTransform.position;
        float distancia = desdeNave.magnitude;

        if (distancia <= distanciaMaxima) return;

        Vector3 direccionHaciaNave = -desdeNave.normalized;
        float exceso = distancia - distanciaMaxima;
        float intensidad = Mathf.Clamp01(exceso / zonaDeTransicion);

        playerPhysics.ApplyForce(direccionHaciaNave * fuerzaTiron * intensidad);
    }

    public void SetTetherActivo(bool activo)
    {
        tetherActivo = activo;
        if (!activo && cableRenderer != null)
            cableRenderer.enabled = false;
    }

    private void DibujarCable()
    {
        if (cableRenderer == null) return;

        cableRenderer.enabled = true;
        cableRenderer.positionCount = 2;

        Vector3 origen = anchorPointJugador != null ? anchorPointJugador.position : transform.position;
        cableRenderer.SetPosition(0, origen);
        cableRenderer.SetPosition(1, naveTransform.position);
    }

    // Ayuda visual en el editor para ver el radio permitido.
    private void OnDrawGizmosSelected()
    {
        if (naveTransform == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(naveTransform.position, distanciaMaxima);
    }
}