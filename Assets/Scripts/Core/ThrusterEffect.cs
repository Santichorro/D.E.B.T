using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ThrusterEffect : MonoBehaviour
{
    public ParticleSystem thrusterParticles;
    public float maxEmissionRate = 30f;
    public float emissionSmoothSpeed = 5f;
    public float angleOffset = 0f;
    public float rotationSmoothSpeed = 540f;

    private PlayerController playerController;
    private ParticleSystem.EmissionModule emission;
    private float currentEmissionRate;
    private Quaternion targetRotation;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (thrusterParticles != null)
        {
            emission = thrusterParticles.emission;

            var rate = emission.rateOverTime;
            rate.constant = 0f;
            emission.rateOverTime = rate;

            targetRotation = thrusterParticles.transform.rotation;

            if (!thrusterParticles.isPlaying)
                thrusterParticles.Play();
        }
    }

    private void Update()
    {
        if (thrusterParticles == null) return;

        bool isThrusting = playerController.IsThrusting;

        if (isThrusting)
        {
            Vector3 dir = playerController.MoveDirection;
            float angle = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
        }

        thrusterParticles.transform.rotation = Quaternion.RotateTowards(
            thrusterParticles.transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime);

        float targetRate = isThrusting ? maxEmissionRate : 0f;
        currentEmissionRate = Mathf.Lerp(currentEmissionRate, targetRate, Time.deltaTime * emissionSmoothSpeed);

        var rateOverTime = emission.rateOverTime;
        rateOverTime.constant = currentEmissionRate;
        emission.rateOverTime = rateOverTime;
    }
}