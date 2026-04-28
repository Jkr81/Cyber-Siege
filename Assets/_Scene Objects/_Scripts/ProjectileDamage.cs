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
        DealDamage(other, transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        DealDamage(collision.collider, hitPoint);
    }

    private void DealDamage(Collider hitCollider, Vector3 hitPoint)
    {
        if (hasHit) return;

        // 🔥 PANEL CHECK FIRST
        ShootableIntroPanel panel = hitCollider.GetComponentInParent<ShootableIntroPanel>();

        if (panel != null)
        {
            hasHit = true;

            SpawnImpact(hitPoint);

            if (destroyOnHit)
                Destroy(gameObject); // destroy bullet FIRST

            panel.HitPanel();

            return;
        }

        // NORMAL ENEMY DAMAGE
        HealthComponent health = hitCollider.GetComponentInParent<HealthComponent>();
        if (health == null) return;

        hasHit = true;

        SpawnImpact(hitPoint);
        health.TakeDamage(damage);

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private void SpawnImpact(Vector3 position)
    {
        if (impactEffect == null) return;

        GameObject impact = Instantiate(
            impactEffect,
            position,
            Quaternion.identity
        );

        Destroy(impact, impactDestroyTime);
    }
}