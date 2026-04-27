using UnityEngine;

public class BossBulletDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 35f;

    [Header("Lifetime")]
    public float lifetime = 4f;

    [Header("Impact Effect")]
    public GameObject impactEffect;
    public float impactDestroyTime = 2f;

    [Header("Ignore Colliders")]
    public bool ignoreNonPlayerHits = true;

    private bool hasHit = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        PlayerDamageReceiver player = other.GetComponentInParent<PlayerDamageReceiver>();

        // Ignore everything except player
        if (player == null && ignoreNonPlayerHits)
            return;

        hasHit = true;

        if (player != null)
        {
            player.TakeDamage(damage);
        }

        SpawnImpact(transform.position);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        PlayerDamageReceiver player = collision.collider.GetComponentInParent<PlayerDamageReceiver>();

        // Ignore everything except player
        if (player == null && ignoreNonPlayerHits)
            return;

        hasHit = true;

        Vector3 hitPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        if (player != null)
        {
            player.TakeDamage(damage);
        }

        SpawnImpact(hitPoint);
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