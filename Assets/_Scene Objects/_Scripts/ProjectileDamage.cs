using UnityEngine;
using Ilumisoft.HealthSystem.UI;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Impact Effect")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float impactDestroyTime = 2f;

    private bool hasHit = false;

    private void OnTriggerEnter(Collider other)
    {
        DealDamage(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        DealDamage(collision.collider);
    }

    private void DealDamage(Collider hitCollider)
    {
        if (hasHit) return;

        HealthComponent health = hitCollider.GetComponentInParent<HealthComponent>();
        if (health == null) return;

        hasHit = true;

        if (impactEffect != null)
        {
            GameObject impact = Instantiate(
                impactEffect,
                transform.position,
                Quaternion.identity
            );

            Destroy(impact, impactDestroyTime);
        }

        health.TakeDamage(damage);

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}