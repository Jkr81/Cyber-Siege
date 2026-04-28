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

        // INTRO PANEL CHECK
        ShootableIntroPanel introPanel = hitCollider.GetComponentInParent<ShootableIntroPanel>();

        if (introPanel != null)
        {
            hasHit = true;

            SpawnImpact(hitPoint);

            if (destroyOnHit)
                Destroy(gameObject);

            introPanel.HitPanel();
            return;
        }

        // END / RESTART PANEL CHECK
        ShootableEndPanel endPanel = hitCollider.GetComponentInParent<ShootableEndPanel>();

        if (endPanel != null)
        {
            hasHit = true;

            SpawnImpact(hitPoint);

            if (destroyOnHit)
                Destroy(gameObject);

            endPanel.HitPanel();
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