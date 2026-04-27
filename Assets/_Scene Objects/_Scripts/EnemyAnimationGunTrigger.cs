using UnityEngine;
using MikeNspired.XRIStarterKit;

public class EnemyAnimationGunTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ProjectileWeapon gun;

    [Header("Aim Reference")]
    [SerializeField] private Transform firePoint;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionDistance = 60f;
    [SerializeField] private float chestHeightOffset = 1.2f;

    [Header("Spawn Delay")]
    [SerializeField] private float spawnEffectTime = 1.8f;
    [SerializeField] private bool blockShootingDuringSpawn = true;

    [Header("Optional Spawn Effect")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float spawnEffectDestroyTime = 2.5f;

    [Header("Shooting")]
    [SerializeField] private float fireRate = 1.3f;

    [Header("Debug Aim Lines")]
    [SerializeField] private bool showAimDebug = true;
    [SerializeField] private float debugRayLength = 20f;

    private Transform player;
    private float nextFireTime;
    private float spawnTime;
    private bool spawnedEffectPlayed = false;

    void Start()
    {
        spawnTime = Time.time;

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

        nextFireTime = Time.time + spawnEffectTime + Random.Range(0f, fireRate);
    }

    void Update()
    {
        bool spawning = Time.time - spawnTime < spawnEffectTime;

        if (!spawnedEffectPlayed)
        {
            spawnedEffectPlayed = true;

            if (spawnEffectPrefab != null)
            {
                GameObject effect = Instantiate(
                    spawnEffectPrefab,
                    transform.position,
                    Quaternion.identity
                );

                Destroy(effect, spawnEffectDestroyTime);
            }
        }

        if (player == null || gun == null) return;

        Transform aimOrigin = firePoint != null ? firePoint : transform;

        Vector3 targetPoint = player.position + Vector3.up * chestHeightOffset;
        float distance = Vector3.Distance(aimOrigin.position, targetPoint);
        Vector3 aimDirection = targetPoint - aimOrigin.position;

        if (showAimDebug)
        {
            Debug.DrawLine(aimOrigin.position, targetPoint, Color.green);
            Debug.DrawRay(aimOrigin.position, aimOrigin.forward * debugRayLength, Color.red);
        }

        if (distance > detectionDistance)
        {
            if (animator != null)
                animator.enabled = false;

            return;
        }

        if (animator != null && !animator.enabled && !spawning)
            animator.enabled = true;

        if (aimDirection.sqrMagnitude > 0.001f)
        {
            Quaternion aimRotation = Quaternion.LookRotation(aimDirection.normalized);
            aimOrigin.rotation = aimRotation;
        }

        if (blockShootingDuringSpawn && spawning)
            return;

        if (Time.time >= nextFireTime)
        {
            gun.ShootFromAnimation();

            float randomOffset = Random.Range(0.8f, 1.2f);
            nextFireTime = Time.time + fireRate * randomOffset;
        }
    }
}