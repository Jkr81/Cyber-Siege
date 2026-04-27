using UnityEngine;
using System.Collections;
using MikeNspired.XRIStarterKit;

public class BossAnimationGunTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ProjectileWeapon gun;
    [SerializeField] private Transform firePoint;

    [Header("Player Target")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float chestOffset = -0.35f;
    [SerializeField] private float detectionDistance = 300f;

    [Header("Prediction Aim")]
    [SerializeField] private bool usePrediction = false;
    [SerializeField] private float predictionAmount = 0.1f;

    [Header("Boss Shooting")]
    [SerializeField] private float fireRate = 1.2f;
    [SerializeField] private float animationFireDelay = 0.25f;

    [Header("Debug Aim Line")]
    [SerializeField] private bool showDebugLine = true;
    [SerializeField] private float debugLineDuration = 0.1f;

    [Header("Animation")]
    [SerializeField] private string shootingBoolName = "IsShooting";

    private float nextFireTime;
    private bool isWaitingToFire = false;

    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;

    private void Start()
    {
        if (gun == null)
            gun = GetComponent<ProjectileWeapon>();

        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (firePoint == null && gun != null)
            firePoint = gun.transform;

        if (firePoint == null)
            firePoint = transform;

        FindPlayerTarget();

        if (playerTarget != null)
            lastTargetPosition = playerTarget.position;

        nextFireTime = Time.time + fireRate;
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
            return;
        }

        if (gun == null || firePoint == null)
            return;

        UpdateTargetVelocity();

        Vector3 targetPosition = GetTargetPosition();
        Vector3 shootOrigin = firePoint.position;
        Vector3 directionToPlayer = (targetPosition - shootOrigin).normalized;
        float distance = Vector3.Distance(shootOrigin, targetPosition);

        // LONG LINE: firePoint/gun to player. Ignores colliders visually.
        if (showDebugLine)
        {
            Debug.DrawLine(shootOrigin, targetPosition, Color.green, debugLineDuration);
        }

        if (distance > detectionDistance)
        {
            SetShootingAnimation(false);
            return;
        }

        AimAtPlayer(directionToPlayer);

        if (Time.time >= nextFireTime && !isWaitingToFire)
        {
            StartCoroutine(FireAfterAnimationDelay());
        }
    }

    private void FindPlayerTarget()
    {
        if (Camera.main != null)
        {
            playerTarget = Camera.main.transform;
            return;
        }

        Camera cam = FindFirstObjectByType<Camera>();

        if (cam != null)
            playerTarget = cam.transform;
    }

    private void UpdateTargetVelocity()
    {
        Vector3 currentPosition = playerTarget.position;

        if (Time.deltaTime > 0f)
            targetVelocity = (currentPosition - lastTargetPosition) / Time.deltaTime;

        lastTargetPosition = currentPosition;
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 targetPosition = playerTarget.position + Vector3.up * chestOffset;

        if (usePrediction)
            targetPosition += targetVelocity * predictionAmount;

        return targetPosition;
    }

    private void AimAtPlayer(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);

        firePoint.rotation = lookRotation;
        transform.rotation = lookRotation;
    }

    private IEnumerator FireAfterAnimationDelay()
    {
        isWaitingToFire = true;
        nextFireTime = Time.time + fireRate;

        SetShootingAnimation(true);

        yield return new WaitForSeconds(animationFireDelay);

        FireGun();

        yield return new WaitForSeconds(0.1f);

        SetShootingAnimation(false);
        isWaitingToFire = false;
    }

    private void FireGun()
    {
        if (gun == null) return;

        gun.FireGun();
    }

    private void SetShootingAnimation(bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(shootingBoolName))
        {
            animator.SetBool(shootingBoolName, value);
        }
    }
}