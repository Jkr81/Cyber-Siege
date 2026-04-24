using UnityEngine;
using Ilumisoft.HealthSystem.UI;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool destroyOnHit = true;

    private void OnTriggerEnter(Collider other)
    {
        HealthComponent health = other.GetComponentInParent<HealthComponent>();

        if (health != null)
        {
            health.TakeDamage(damage);

            if (destroyOnHit)
                Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HealthComponent health = collision.collider.GetComponentInParent<HealthComponent>();

        if (health != null)
        {
            health.TakeDamage(damage);

            if (destroyOnHit)
                Destroy(gameObject);
        }
    }
}