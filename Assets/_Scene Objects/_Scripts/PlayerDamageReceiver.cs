using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDamageReceiver : MonoBehaviour
{
    [Header("Health / Score")]
    public float maxHealth = 5000f;
    public float currentHealth = 5000f;

    [Header("UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthNumberText;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log("Player lost " + damage + ". Health/Score now: " + currentHealth);

        UpdateUI();
    }

    public void AddHealthScore(float amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            maxHealth = currentHealth;
        }

        Debug.Log("Player gained " + amount + ". Health/Score now: " + currentHealth);

        UpdateUI();
    }

    private void UpdateUI()
    {
        float percent = currentHealth / maxHealth;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = percent;

            if (percent > 0.5f)
                healthBarFill.color = Color.Lerp(Color.yellow, Color.green, (percent - 0.5f) * 2f);
            else
                healthBarFill.color = Color.Lerp(Color.red, Color.yellow, percent * 2f);
        }

        if (healthNumberText != null)
        {
            healthNumberText.text = Mathf.RoundToInt(currentHealth).ToString();
        }
    }
}