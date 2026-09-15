using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AnchorableTarget : MonoBehaviour, IAnchorable
{
    private Rigidbody rb;
    private EnemyController enemyController; // opcional, puede ser null en puertas u otros props
    private bool wasKinematicBeforeAnchor;
    private Coroutine anchorRoutine;

    public bool IsAnchored { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyController = GetComponent<EnemyController>();
    }

    public void Anchor(float duration)
    {
        if (anchorRoutine != null)
            StopCoroutine(anchorRoutine);

        anchorRoutine = StartCoroutine(AnchorRoutine(duration));
    }

    private IEnumerator AnchorRoutine(float duration)
    {
        if (!IsAnchored)
            wasKinematicBeforeAnchor = rb.isKinematic;

        IsAnchored = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Igual que con el grab del gravity gun: pausar la lógica propia
        // del enemigo (detección/persecución) mientras está anclado.
        if (enemyController != null)
            enemyController.SetGrabbed(true);

        yield return new WaitForSeconds(duration);

        rb.isKinematic = wasKinematicBeforeAnchor;
        IsAnchored = false;

        if (enemyController != null)
            enemyController.SetGrabbed(false);

        anchorRoutine = null;
    }
}