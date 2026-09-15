using UnityEngine;

/// <summary>
/// Cable individual entre un jugador y la nave activa. La nave se registra una
/// sola vez desde ShipController; este componente no realiza búsquedas globales.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
public class PlayerShipLine : MonoBehaviour
{
    [Header("Restricción de distancia")]
    [SerializeField, Min(0.1f)] private float distanciaMaxima = 25f;
    [SerializeField, Min(0f)] private float inicioTension = 20f;
    [SerializeField, Min(0f)] private float fuerzaMinima = 18f;
    [SerializeField, Min(0f)] private float fuerzaMaxima = 60f;
    [SerializeField, Min(0f)] private float margenFreno = 2f;
    [SerializeField, Min(0f)] private float impulsoMaximoDeFreno = 1.5f;

    [Header("Visual - Cable")]
    [SerializeField] private LineRenderer cableRenderer;
    [Tooltip("Si queda vacío, el cable sale del transform raíz del jugador.")]
    [SerializeField] private Transform puntoOrigenJugador;
    [SerializeField] private bool ocultarMientrasPilota = true;
    [SerializeField] private bool ocultarSiEstaMuerto = true;
    [SerializeField] private bool usarColorDeGravityGun = true;
    [SerializeField] private Color colorFijo = Color.cyan;

    private PlayerPhysics playerPhysics;
    private GravityGun gravityGun;
    private Health health;
    private ShipController nave;
    private bool tetherActivo = true;

    private Transform NaveTransform => nave != null ? nave.transform : null;

    private void Awake()
    {
        playerPhysics = GetComponent<PlayerPhysics>();
        gravityGun = GetComponent<GravityGun>();
        health = GetComponent<Health>();
        AplicarColorCable();
        MostrarCable(false);
    }

    private void OnEnable()
    {
        ShipController.ActiveTetherShipChanged += OnActiveTetherShipChanged;
        VincularNave(ShipController.ActiveTetherShip);
    }

    private void OnDisable()
    {
        ShipController.ActiveTetherShipChanged -= OnActiveTetherShipChanged;
        MostrarCable(false);
    }

    private void OnActiveTetherShipChanged(ShipController nuevaNave)
    {
        VincularNave(nuevaNave);
    }

    private void VincularNave(ShipController nuevaNave)
    {
        nave = nuevaNave;
        ActualizarCable();
    }

    private void FixedUpdate()
    {
        if (!PuedeUsarTether() || playerPhysics == null || playerPhysics.IsAnchored)
            return;

        Vector3 desdeNave = transform.position - NaveTransform.position;
        desdeNave.z = 0f;
        float distancia = desdeNave.magnitude;

        if (distancia <= inicioTension || distancia <= 0.0001f)
            return;

        Vector3 haciaNave = -desdeNave / distancia;
        float limiteDeFuerza = Mathf.Max(distanciaMaxima, inicioTension + 0.01f);
        float progreso = Mathf.InverseLerp(inicioTension, limiteDeFuerza, distancia);
        float fuerza = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, progreso);
        playerPhysics.ApplyForce(haciaNave * fuerza);

        // Si sigue alejándose mucho, frena únicamente su velocidad radial. No hay teletransporte.
        if (distancia > distanciaMaxima + margenFreno && impulsoMaximoDeFreno > 0f)
        {
            Vector3 velocidad = playerPhysics.CurrentVelocity;
            velocidad.z = 0f;
            float velocidadHaciaFuera = Vector3.Dot(velocidad, -haciaNave);
            if (velocidadHaciaFuera > 0f)
                playerPhysics.ApplyImpulse(haciaNave * Mathf.Min(velocidadHaciaFuera, impulsoMaximoDeFreno));
        }
    }

    private void LateUpdate()
    {
        ActualizarCable();
    }

    /// <summary>
    /// Lo usa ShipController al entrar/salir del estado de pilotaje. En el flujo
    /// actual, pilotear es el estado que representa estar dentro de la nave.
    /// </summary>
    public void SetTetherActivo(bool activo)
    {
        tetherActivo = activo;
        ActualizarCable();
    }

    private bool PuedeUsarTether()
    {
        if (!tetherActivo || NaveTransform == null)
            return false;

        return !ocultarSiEstaMuerto || health == null || !health.IsDead;
    }

    private void ActualizarCable()
    {
        bool debeMostrarse = tetherActivo && NaveTransform != null &&
                              (!ocultarSiEstaMuerto || health == null || !health.IsDead);

        if (!debeMostrarse && !ocultarMientrasPilota && NaveTransform != null &&
            (!ocultarSiEstaMuerto || health == null || !health.IsDead))
        {
            debeMostrarse = true;
        }

        MostrarCable(debeMostrarse);
        if (!debeMostrarse || cableRenderer == null)
            return;

        cableRenderer.positionCount = 2;
        cableRenderer.SetPosition(0, puntoOrigenJugador != null ? puntoOrigenJugador.position : transform.position);
        cableRenderer.SetPosition(1, NaveTransform.position);
    }

    private void MostrarCable(bool mostrar)
    {
        if (cableRenderer != null && cableRenderer.enabled != mostrar)
            cableRenderer.enabled = mostrar;
    }

    private void AplicarColorCable()
    {
        if (cableRenderer == null)
            return;

        Color color = usarColorDeGravityGun && gravityGun != null
            ? gravityGun.GetAssignedColor()
            : colorFijo;

        cableRenderer.startColor = color;
        cableRenderer.endColor = color;
    }

    private void OnValidate()
    {
        distanciaMaxima = Mathf.Max(0.1f, distanciaMaxima);
        inicioTension = Mathf.Clamp(inicioTension, 0f, distanciaMaxima);
        fuerzaMinima = Mathf.Max(0f, fuerzaMinima);
        fuerzaMaxima = Mathf.Max(fuerzaMinima, fuerzaMaxima);
        margenFreno = Mathf.Max(0f, margenFreno);
        impulsoMaximoDeFreno = Mathf.Max(0f, impulsoMaximoDeFreno);
    }

    private void OnDrawGizmosSelected()
    {
        if (NaveTransform == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(NaveTransform.position, inicioTension);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(NaveTransform.position, distanciaMaxima);
    }
}
