using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class RespawnSystem : MonoBehaviour
{
    [Header("Reaparición")]
    public float respawnCooldown = 3f;
    [Tooltip("Punto cercano a la nave donde reaparece el jugador. Asignar manualmente hasta tener ShipController.")]
    public Transform respawnPoint;

    private Health health;
    private PlayerController playerController;
    private PlayerPhysics physics;
    private GravityGun gravityGun;
    private Rigidbody rb;
    private Renderer[] renderers;
    private Collider[] colliders;

    private void Awake()
    {
        health = GetComponent<Health>();
        playerController = GetComponent<PlayerController>();
        physics = GetComponent<PlayerPhysics>();
        gravityGun = GetComponent<GravityGun>();
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable() => health.OnDeath += HandleDeath;
    private void OnDisable() => health.OnDeath -= HandleDeath;

    private void HandleDeath()
    {
        gravityGun?.ForceReleaseHook();
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        SetActiveState(false);

        yield return new WaitForSeconds(respawnCooldown);

        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }

        health.ResetHealth();
        SetActiveState(true);

        rb.linearVelocity = Vector3.zero;
    }

    private void SetActiveState(bool active)
    {
        if (playerController != null) playerController.enabled = active;
        if (physics != null) physics.enabled = active;
        if (gravityGun != null) gravityGun.enabled = active;

        rb.isKinematic = !active;

        foreach (var r in renderers) r.enabled = active;
        foreach (var c in colliders) c.enabled = active;
    }
}