using UnityEngine;
using MikeNspired.XRIStarterKit;

public class EnemyAnimationGunTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ProjectileWeapon gun;

    [Header("Animation")]
    [SerializeField] private string shootStateName = "Shooting";
    [SerializeField, Range(0f, 1f)] private float fireAtNormalizedTime = 0.35f;

    private bool firedThisLoop;
    private float previousTime;

    void Awake()
    {
        if (gun == null)
            gun = GetComponent<ProjectileWeapon>();

        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    void Update()
    {
        if (animator == null || gun == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (!state.IsName(shootStateName))
        {
            firedThisLoop = false;
            previousTime = 0f;
            return;
        }

        float time = state.normalizedTime % 1f;

        if (time < previousTime)
            firedThisLoop = false;

        if (!firedThisLoop && time >= fireAtNormalizedTime)
        {
            firedThisLoop = true;
            gun.ShootFromAnimation();
        }

        previousTime = time;
    }
}