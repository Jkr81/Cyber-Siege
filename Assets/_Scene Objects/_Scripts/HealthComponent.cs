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

            // 💀 spawn death effect
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

            GameManager manager = FindFirstObjectByType<GameManager>();
            if (manager != null)
            {
                manager.AddKill();
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}