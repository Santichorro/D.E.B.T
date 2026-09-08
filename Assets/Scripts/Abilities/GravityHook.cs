using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityHook : MonoBehaviour
{
    public enum HookState { Flying, Attached, Returning }

    [Header("Vuelo")]
    public float speed = 25f;
    public float maxLifetime = 3f;
    public float maxRange = 6f;
    public float returnSpeed = 30f;
    public float returnDistanceThreshold = 0.3f;

    private Rigidbody rb;
    private GravityGun owner;
    private Rigidbody ownerRb;
    private float timer;

    public HookState State { get; private set; } = HookState.Flying;
    public Rigidbody TargetRb { get; private set; }
    public PlayerPhysics TargetPhysics { get; private set; }
    public Vector3 LocalAttachPoint { get; private set; }

    public void Init(GravityGun owner, Rigidbody ownerRb, Vector3 direction)
    {
        this.owner = owner;
        this.ownerRb = ownerRb;
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = direction.normalized * speed;

        Collider myCollider = GetComponent<Collider>();
        if (ownerRb != null && myCollider != null)
        {
            Collider[] ownerColliders = ownerRb.GetComponentsInChildren<Collider>();
            foreach (var col in ownerColliders)
            {
                Physics.IgnoreCollision(myCollider, col, true);
            }
        }
    }

    private void Update()
    {
        if (State == HookState.Flying)
        {
            timer += Time.deltaTime;
            float distanceTraveled = speed * timer;

            if (timer >= maxLifetime || distanceTraveled >= maxRange)
            {
                State = HookState.Returning;
            }
        }
        else if (State == HookState.Returning)
        {
            Vector3 toOwner = owner.Muzzle.position - transform.position;

            if (toOwner.magnitude <= returnDistanceThreshold)
            {
                owner.OnHookReturned(this);
                Destroy(gameObject);
                return;
            }

            rb.linearVelocity = toOwner.normalized * returnSpeed;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (State != HookState.Flying) return;

        Rigidbody targetRb = other.attachedRigidbody;
        if (targetRb == null) return;

        if (targetRb == ownerRb) return;

        State = HookState.Attached;
        TargetRb = targetRb;
        TargetPhysics = targetRb.GetComponent<PlayerPhysics>();
        LocalAttachPoint = targetRb.transform.InverseTransformPoint(transform.position);

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        transform.SetParent(targetRb.transform, true);

        EnemyController enemyCtrl = targetRb.GetComponent<EnemyController>();
        if (enemyCtrl != null)
            enemyCtrl.SetGrabbed(true);

        owner.OnHookAttached(this);
    }

    public void BeginReturn()
    {
        if (State == HookState.Returning) return;

        if (State == HookState.Attached)
        {
            EnemyController enemyCtrl = TargetRb.GetComponent<EnemyController>();
            if (enemyCtrl != null)
                enemyCtrl.SetGrabbed(false);

            transform.SetParent(null, true);
            rb.isKinematic = false;
            TargetRb = null;
            TargetPhysics = null;
        }

        State = HookState.Returning;
        timer = 0f;
    }

    public void ForceRelease()
    {
        if (State == HookState.Attached && TargetRb != null)
        {
            EnemyController enemyCtrl = TargetRb.GetComponent<EnemyController>();
            if (enemyCtrl != null)
                enemyCtrl.SetGrabbed(false);
        }

        Destroy(gameObject);
    }
}