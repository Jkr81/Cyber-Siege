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
    [SerializeField] private bool usePrediction = true;
    [SerializeField] private float predictionAmount = 0.2f;

    [Header("Boss Shooting")]
    [SerializeField] private float fireRate = 1.8f;
    [SerializeField] private float animationFireDelay = 0.35f;

    [Header("Aim Check")]
    [SerializeField] private bool onlyShootWhenAimed = true;
    [SerializeField, Range(0.8f, 1f)] private float aimAccuracy = 0.97f;

    [Header("ProjectileWeapon Fire Method")]
    [SerializeField] private string shootMethodName = "Shoot";

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

        if (firePoint == null)
            firePoint = transform;

        FindPlayerTarget();

        if (playerTarget != null)
            lastTargetPosition = playerTarget.position;
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
        Vector3 directionToPlayer = (targetPosition - firePoint.position).normalized;
        float distance = Vector3.Distance(firePoint.position, targetPosition);

        if (distance > detectionDistance)
        {
            SetShootingAnimation(false);
            return;
        }

        AimAtPlayer(directionToPlayer);

        if (Time.time >= nextFireTime && !isWaitingToFire)
        {
            if (!onlyShootWhenAimed || IsAimedAtPlayer(directionToPlayer))
            {
                StartCoroutine(FireAfterAnimationDelay());
            }
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

    private bool IsAimedAtPlayer(Vector3 directionToPlayer)
    {
        float dot = Vector3.Dot(firePoint.forward, directionToPlayer);

        return dot >= aimAccuracy;
    }

    private IEnumerator FireAfterAnimationDelay()
    {
        isWaitingToFire = true;

        SetShootingAnimation(true);

        yield return new WaitForSeconds(animationFireDelay);

        FireGun();

        yield return new WaitForSeconds(0.1f);
        SetShootingAnimation(false);

        nextFireTime = Time.time + fireRate;
        isWaitingToFire = false;
    }

    private void FireGun()
    {
        if (gun == null) return;

        gun.gameObject.SendMessage(
            shootMethodName,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void SetShootingAnimation(bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(shootingBoolName))
        {
            animator.SetBool(shootingBoolName, value);
        }
    }
}