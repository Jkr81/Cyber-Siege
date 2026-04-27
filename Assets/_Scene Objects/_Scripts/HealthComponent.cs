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
        [SerializeField] private int defaultPointsOnKill = 50;
        [SerializeField] private int redEnemyPointsOnKill = 125;

        [Header("Point Scaling")]
        [SerializeField] private bool scalePointsWithDifficulty = true;
        [SerializeField] private float minDifficulty = 1f;
        [SerializeField] private float maxDifficulty = 3f;
        [SerializeField] private float maxPointMultiplier = 1.5f;

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
                int basePoints = defaultPointsOnKill;

                if (gameObject.CompareTag("RedEnemy"))
                {
                    basePoints = redEnemyPointsOnKill;
                }

                int finalPoints = basePoints;

                if (scalePointsWithDifficulty)
                {
                    float difficulty = 1f;

                    ShipFollowHybrid ship = FindFirstObjectByType<ShipFollowHybrid>();

                    if (ship != null)
                    {
                        difficulty = ship.DifficultyMultiplier;
                    }

                    float normalized = Mathf.InverseLerp(
                        minDifficulty,
                        maxDifficulty,
                        difficulty
                    );

                    float multiplier = Mathf.Lerp(
                        1f,
                        maxPointMultiplier,
                        normalized
                    );

                    finalPoints = Mathf.RoundToInt(basePoints * multiplier);
                }

                GameManager.Instance.AddKill(finalPoints);
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