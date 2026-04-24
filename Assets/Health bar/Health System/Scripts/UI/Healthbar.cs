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

        [SerializeField, Tooltip("Whether the healthbar should be hidden when health is empty")]
        private bool hideEmpty = false;

        [SerializeField, Tooltip("Makes the healthbar align with the camera")]
        private bool alignWithCamera = true;

        [SerializeField, Min(0.1f), Tooltip("Controls how fast changes animate in points/second")]
        private float changeSpeed = 100f;

        private float currentValue;

        private void Awake()
        {
            if (Health == null)
                Health = GetComponentInParent<HealthComponent>();

            if (canvas == null)
                canvas = GetComponentInChildren<Canvas>();

            if (fillImage == null)
                fillImage = GetComponentInChildren<Image>();

            if (Health != null)
                currentValue = Health.CurrentHealth;
        }

        private void Start()
        {
            if (Health != null)
                currentValue = Health.CurrentHealth;
        }

        private void Update()
        {
            if (Health == null || fillImage == null)
                return;

            if (alignWithCamera && Camera.main != null)
                AlignWithCamera();

            currentValue = Mathf.MoveTowards(
                currentValue,
                Health.CurrentHealth,
                Time.deltaTime * changeSpeed
            );

            UpdateFillbar();
            UpdateVisibility();
        }

        private void AlignWithCamera()
        {
            transform.forward = Camera.main.transform.forward;
        }

        private void UpdateFillbar()
        {
            float value = Mathf.InverseLerp(0, Health.MaxHealth, currentValue);
            fillImage.fillAmount = value;
        }

        private void UpdateVisibility()
        {
            if (canvas == null)
                return;

            float value = fillImage.fillAmount;

            if (Mathf.Approximately(value, 0))
            {
                if (hideEmpty && canvas.gameObject.activeSelf)
                    canvas.gameObject.SetActive(false);
            }
            else if (value > 0 && canvas.gameObject.activeSelf == false)
            {
                canvas.gameObject.SetActive(true);
            }
        }
    }
}