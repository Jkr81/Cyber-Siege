using UnityEngine;
using MikeNspired.XRIStarterKit;

public class EnemyAnimationGunTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ProjectileWeapon gun;

    [Header("Aim Reference")]
    [Tooltip("Assign the FirePoint / muzzle object here if you have one.")]
    [SerializeField] private Transform firePoint;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionDistance = 60f;
    [SerializeField] private float chestHeightOffset = 1.2f;

    [Header("Shooting")]
    [Tooltip("Seconds between shots. Higher = slower. Lower = faster.")]
    [SerializeField] private float fireRate = 1.3f;

    [Header("Debug Aim Lines")]
    [SerializeField] private bool showAimDebug = true;
    [SerializeField] private float debugRayLength = 20f;

    private Transform player;
    private float nextFireTime;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (gun == null)
            gun = GetComponent<ProjectileWeapon>();

        if (firePoint == null)
        {
            Transform found = transform.Find("FirePoint");
            if (found != null)
                firePoint = found;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObj != null)
            player = playerObj.transform;
        else if (Camera.main != null)
            player = Camera.main.transform;

        if (animator != null)
            animator.enabled = false;

        nextFireTime = Time.time + Random.Range(0f, fireRate);
    }

    void Update()
    {
        if (player == null || gun == null) return;

        Transform aimOrigin = firePoint != null ? firePoint : transform;

        Vector3 targetPoint = player.position + Vector3.up * chestHeightOffset;
        float distance = Vector3.Distance(aimOrigin.position, targetPoint);

        Vector3 aimDirection = targetPoint - aimOrigin.position;

        if (showAimDebug)
        {
            // GREEN = exact line to your chest
            Debug.DrawLine(aimOrigin.position, targetPoint, Color.green);

            // RED = where the FirePoint is actually aiming
            Debug.DrawRay(aimOrigin.position, aimOrigin.forward * debugRayLength, Color.red);
        }

        if (distance > detectionDistance)
        {
            if (animator != null)
                animator.enabled = false;

            return;
        }

        if (animator != null && !animator.enabled)
            animator.enabled = true;

        if (aimDirection.sqrMagnitude > 0.001f)
        {
            Quaternion aimRotation = Quaternion.LookRotation(aimDirection.normalized);
            aimOrigin.rotation = aimRotation;
        }

        if (Time.time >= nextFireTime)
        {
            gun.ShootFromAnimation();

            float randomOffset = Random.Range(0.8f, 1.2f);
            nextFireTime = Time.time + (fireRate * randomOffset);
        }
    }
}