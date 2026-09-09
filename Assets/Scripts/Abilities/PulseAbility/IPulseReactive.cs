using UnityEngine;

/// <summary>
/// Interfaz opt-in para objetos que reaccionan al Pulso del Demoledor sin necesariamente
/// recibir daño (por ejemplo, un Mechanism que debe desactivarse temporalmente, según la
/// sección 4.5 del documento de mecánicas: "Puede desactivar determinados mecanismos").
/// Al ser opt-in, un Door o Mechanism decide explícitamente si le afecta el pulso o no.
/// </summary>
public interface IPulseReactive
{
    /// <param name="origin">Posición desde la que se originó el pulso.</param>
    /// <param name="force">Fuerza/intensidad del pulso, por si el mecanismo quiere escalar su reacción.</param>
    void OnPulseHit(Vector3 origin, float force);
}
