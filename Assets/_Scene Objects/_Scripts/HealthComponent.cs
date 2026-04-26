using UnityEngine;

namespace Ilumisoft.HealthSystem.UI
{
    public class HealthComponent : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float CurrentHealth;

        [SerializeField] private bool destroyOnDeath = true;

        [Header("Death Effect")]
        [SerializeField] private GameObject deathEffect;
        [SerializeField] private float deathEffectDestroyTime = 3f;

        [Header("Kill Points")]
        [SerializeField] private int defaultPointsOnKill = 100;
        [SerializeField] private int redEnemyPointsOnKill = 250;

        private bool isDead = false;

        private void Awake()
        {
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;

            if (deathEffect != null)
            {
                GameObject effect = Instantiate(
                    deathEffect,
                    transform.position,
                    Quaternion.identity
                );

                Destroy(effect, deathEffectDestroyTime);
            }

            Debug.Log(gameObject.name + " died");

            if (GameManager.Instance != null)
            {
                int points = defaultPointsOnKill;

                if (gameObject.CompareTag("RedEnemy"))
                {
                    points = redEnemyPointsOnKill;
                }

                GameManager.Instance.AddKill(points);
            }
            else
            {
                Debug.LogWarning("No GameManager.Instance found. Kill/score not added.");
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}