using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Marca un objeto que puede ser entregado a la nave. La entrega es idempotente:
/// aunque varios colliders entren al trigger en el mismo frame, su valor solo se suma una vez.
/// </summary>
[DisallowMultipleComponent]
public class ValuableObject : MonoBehaviour
{
    [Header("Entrega")]
    [SerializeField, Min(1)] private int monetaryValue = 10;
    [SerializeField] private bool canBeStored = true;
    [SerializeField] private bool disableObjectAfterStorage = true;

    private Rigidbody cachedRigidbody;
    private Collider[] cachedColliders;

    public int MonetaryValue => monetaryValue;
    public bool CanBeStored => canBeStored;
    public bool IsStored { get; private set; }

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    /// <summary>
    /// Desconecta cualquier GravityGun, detiene la física y registra el valor una única vez.
    /// </summary>
    public bool TryStore(MissionMoney money)
    {
        if (IsStored || !canBeStored || monetaryValue <= 0 || money == null)
            return false;

        // Se marca primero para cubrir entradas simultáneas de varios colliders/jugadores.
        IsStored = true;

        if (!money.AddMoney(monetaryValue))
        {
            IsStored = false;
            return false;
        }

        ReleaseGravityGuns();
        StopPhysics();

        if (disableObjectAfterStorage)
            gameObject.SetActive(false);

        return true;
    }

    private void ReleaseGravityGuns()
    {
        if (cachedRigidbody == null)
            return;

        // Ocurre solo al entregar, nunca por frame.
        foreach (PlayerInput playerInput in PlayerInput.all)
        {
            if (playerInput == null)
                continue;

            GravityGun gravityGun = playerInput.GetComponent<GravityGun>();
            if (gravityGun != null)
                gravityGun.ForceReleaseIfAttachedTo(cachedRigidbody);
        }
    }

    private void StopPhysics()
    {
        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.detectCollisions = false;
        }

        foreach (Collider objectCollider in cachedColliders)
        {
            if (objectCollider != null)
                objectCollider.enabled = false;
        }
    }

    private void OnValidate()
    {
        monetaryValue = Mathf.Max(1, monetaryValue);
    }
}
