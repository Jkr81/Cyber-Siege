using UnityEngine;

public class EnemyBulletDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 100f;

    [Header("Lifetime (controls distance)")]
    public float lifetime = 2.5f;

    [Header("Impact Effect")]
    public GameObject impactEffect;
    public float impactDestroyTime = 2f;

    private bool hasHit = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        hasHit = true;

        SpawnImpact(transform.position);

        PlayerDamageReceiver player = other.GetComponentInParent<PlayerDamageReceiver>();

        if (player != null)
        {
            player.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        SpawnImpact(collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);

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