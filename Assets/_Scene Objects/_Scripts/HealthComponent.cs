using UnityEngine;

namespace Ilumisoft.HealthSystem.UI
{
    public class HealthComponent : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float CurrentHealth;

        private void Awake()
        {
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(float damage)
        {
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

            if (CurrentHealth <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}