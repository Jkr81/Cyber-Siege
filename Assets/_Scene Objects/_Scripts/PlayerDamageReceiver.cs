using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDamageReceiver : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 1000f;
    public float currentHealth = 1000f;

    [Header("UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthPercentText;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log("Player took damage. Health now: " + currentHealth);

        UpdateUI();
    }

    private void UpdateUI()
    {
        float percent = currentHealth / maxHealth;

        // Fill amount
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = percent;

            // 🔥 Color change: Green → Yellow → Red
            if (percent > 0.5f)
            {
                // Green to Yellow
                healthBarFill.color = Color.Lerp(Color.yellow, Color.green, (percent - 0.5f) * 2f);
            }
            else
            {
                // Yellow to Red
                healthBarFill.color = Color.Lerp(Color.red, Color.yellow, percent * 2f);
            }
        }

        // Percent text
        if (healthPercentText != null)
            healthPercentText.text = Mathf.RoundToInt(percent * 100f) + "%";
    }
}