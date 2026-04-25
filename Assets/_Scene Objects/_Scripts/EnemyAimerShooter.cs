using UnityEngine;

public class EnemyAimerShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float chestHeightOffset = 1.0f;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 6f;

    [Header("Range")]
    [SerializeField] private float detectionRange = 30f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string shootingBoolName = "IsShooting";

    private Transform playerTarget;

    void Start()
    {
        GameObject player = GameObject.FindWithTag(playerTag);

        if (player != null)
            playerTarget = player.transform;
        else
            Debug.LogWarning("EnemyAimerShooter: No Player found. Tag your XR Origin as Player.");

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (playerTarget == null || animator == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= detectionRange)
        {
            RotateEnemyTowardPlayer();
            animator.SetBool(shootingBoolName, true);
        }
        else
        {
            animator.SetBool(shootingBoolName, false);
        }
    }

    void RotateEnemyTowardPlayer()
    {
        Vector3 targetPos = playerTarget.position;
        targetPos.y += chestHeightOffset;

        Vector3 direction = targetPos - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }
}