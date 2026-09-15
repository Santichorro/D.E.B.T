using UnityEngine;

/// <summary>
/// Trigger de entrega de la nave. Solo procesa objetos con ValuableObject.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class ShipCargoDeliveryZone : MonoBehaviour
{
    private MissionMoney money;

    private void Awake()
    {
        Collider deliveryCollider = GetComponent<Collider>();
        if (deliveryCollider != null && !deliveryCollider.isTrigger)
            Debug.LogWarning("[ShipCargoDeliveryZone] El collider de entrega debe tener Is Trigger activado.", this);

        ResolveMoney();
    }

    private void OnTriggerEnter(Collider other)
    {
        ValuableObject valuable = other.GetComponentInParent<ValuableObject>();
        if (valuable == null || !valuable.CanBeStored || valuable.IsStored)
            return;

        if (ResolveMoney())
            valuable.TryStore(money);
    }

    private bool ResolveMoney()
    {
        if (money == null)
            money = MissionMoney.Instance;

        if (money == null)
            money = FindFirstObjectByType<MissionMoney>();

        if (money == null)
        {
            Debug.LogWarning("[ShipCargoDeliveryZone] No se encontró MissionMoney en la escena.", this);
            return false;
        }

        return true;
    }
}
