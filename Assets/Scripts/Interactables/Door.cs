using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, IPulseReactive
{
    [Header("Posiciones (eje Y local)")]
    [SerializeField] private float closedY = 0f;
    [SerializeField] private float openY = 3f;
    [SerializeField] private bool startsOpen = false;

    [Header("Movimiento")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Cierre automático")]
    [SerializeField] private bool autoClose = true;
    [SerializeField] private float autoCloseDelay = 5f;

    private bool isOpen;
    private Coroutine moveRoutine;
    private Coroutine autoCloseRoutine;

    private void Awake()
    {
        isOpen = startsOpen;
        SetImmediatePosition(isOpen);
    }

    public void Open()
    {
        if (isOpen)
        {
            RestartAutoCloseTimer();
            return;
        }

        isOpen = true;
        StartMove(openY);
        RestartAutoCloseTimer();
    }

    public void Close()
    {
        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        if (!isOpen) return;
        isOpen = false;
        StartMove(closedY);
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    // Se llama automáticamente desde PulseAbility cuando el pulso golpea este objeto,
    // gracias a que PulseAbility busca IPulseReactive en cada hit del OverlapSphere.
    public void OnPulseHit(Vector3 origin, float force)
    {
        Open();
    }

    private void RestartAutoCloseTimer()
    {
        if (!autoClose) return;

        if (autoCloseRoutine != null)
            StopCoroutine(autoCloseRoutine);

        autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        autoCloseRoutine = null;
        Close();
    }

    private void StartMove(float targetY)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveRoutine(targetY));
    }

    private IEnumerator MoveRoutine(float targetY)
    {
        float startY = transform.localPosition.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = smoothCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(startY, targetY, t);
            transform.localPosition = pos;

            yield return null;
        }

        Vector3 finalPos = transform.localPosition;
        finalPos.y = targetY;
        transform.localPosition = finalPos;
    }

    private void SetImmediatePosition(bool open)
    {
        Vector3 pos = transform.localPosition;
        pos.y = open ? openY : closedY;
        transform.localPosition = pos;
    }
}