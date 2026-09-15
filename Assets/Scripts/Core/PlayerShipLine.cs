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
    [Tooltip("Si hay un GravityGun en el mismo jugador, usa su color asignado para el cable en vez de un color fijo.")]
    [SerializeField] private bool usarColorDeGravityGun = true;
    [SerializeField] private Color colorFijo = Color.cyan;

    private PlayerPhysics playerPhysics;
    private GravityGun gravityGun;

    private void Awake()
    {
        playerPhysics = GetComponent<PlayerPhysics>();
        gravityGun = GetComponent<GravityGun>();

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

        AplicarColorCable();

        if (cableRenderer != null)
            cableRenderer.enabled = true;
    }

    private void AplicarColorCable()
    {
        if (cableRenderer == null) return;

        Color color = (usarColorDeGravityGun && gravityGun != null)
            ? gravityGun.GetAssignedColor()
            : colorFijo;

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

        DibujarCable(); // El cable siempre es visible, tire o no tire (mientras tetherActivo).

        // Si ya está anclado por otra habilidad (ej. el Ingeniero), no se aplica fuerza,
        // pero el cable de todos modos se dibuja arriba.
        if (playerPhysics.IsAnchored) return;

        Vector3 desdeNave = transform.position - naveTransform.position;
        float distancia = desdeNave.magnitude;

        if (distancia <= distanciaMaxima) return;

        Vector3 direccionHaciaNave = -desdeNave.normalized;
        float exceso = distancia - distanciaMaxima;
        float intensidad = Mathf.Clamp01(exceso / zonaDeTransicion);

        playerPhysics.ApplyForce(direccionHaciaNave * fuerzaTiron * intensidad);
    }

    /// <summary>
    /// Llamalo desde el script que maneja el pilotaje de la nave: false cuando el
    /// jugador entra a pilotear (oculta el cable y deja de tirar de él), true cuando sale.
    /// </summary>
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