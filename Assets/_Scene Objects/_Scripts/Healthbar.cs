using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.HealthSystem.UI
{
    [AddComponentMenu("Health System/UI/Healthbar")]
    public class Healthbar : MonoBehaviour
    {
        [field: SerializeField]
        public HealthComponent Health { get; set; }

        [SerializeField] private Canvas canvas;
        [SerializeField] private Image fillImage;

        [SerializeField] private bool hideEmpty = false;
        [SerializeField] private bool alignWithCamera = false;
        [SerializeField] private float changeSpeed = 100;

        float currentValue;

        void Update()
        {
            if (Health == null || fillImage == null) return;

            float targetValue = Health.CurrentHealth / Health.MaxHealth;

            // Smooth animation
            currentValue = Mathf.MoveTowards(currentValue, targetValue, changeSpeed * Time.deltaTime);

            fillImage.fillAmount = currentValue;

            // 🔥 COLOR CHANGE (this is the important part)
            UpdateColor(currentValue);

            // Hide if empty
            if (hideEmpty)
                canvas.enabled = currentValue > 0;

            // Face camera
            if (alignWithCamera && Camera.main != null)
                canvas.transform.forward = Camera.main.transform.forward;
        }

        void UpdateColor(float value)
        {
            // Green → Yellow → Red
            if (value > 0.5f)
            {
                // Green to Yellow
                fillImage.color = Color.Lerp(Color.yellow, Color.green, (value - 0.5f) * 2f);
            }
            else
            {
                // Yellow to Red
                fillImage.color = Color.Lerp(Color.red, Color.yellow, value * 2f);
            }
        }
    }
}