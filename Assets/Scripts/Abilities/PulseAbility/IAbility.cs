using UnityEngine;

/// <summary>
/// Contrato común para todas las habilidades de rol (Ancla, Propulsión, Empuje, Pulso...).
/// El PlayerRoleController solo necesita conocer esta interfaz, no cada implementación concreta.
/// </summary>
public interface IAbility
{
    /// <summary>Tiempo de reutilización en segundos (valor de balance, expuesto en Inspector por la implementación).</summary>
    float Cooldown { get; }

    /// <summary>True si ya pasó el cooldown y la habilidad puede activarse de nuevo.</summary>
    bool IsReady { get; }

    /// <summary>
    /// Ejecuta la habilidad. "direction" se ignora en habilidades de área como el Pulso,
    /// pero se mantiene en la firma para que todas las habilidades compartan el mismo contrato
    /// (por ejemplo Empuje o Propulsión sí la usan).
    /// </summary>
    void Activate(PlayerPhysics physics, Vector3 direction);
}
