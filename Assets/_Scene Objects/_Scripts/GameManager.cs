using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Player Health")]
    [SerializeField] private PlayerDamageReceiver playerHealth;
    [SerializeField] private TMP_Text healthText;

    [Header("Kills")]
    [SerializeField] private TMP_Text killCountText;

    private int kills = 0;
    private bool gameEnded = false;

    void Start()
    {
        Time.timeScale = 1f;
        UpdateKillUI();
        UpdateHealthUI();
    }

    void Update()
    {
        if (gameEnded) return;
        if (playerHealth == null) return;

        UpdateHealthUI();

        if (playerHealth.currentHealth <= 0)
        {
            EndGame();
        }
    }

    public void AddKill()
    {
        kills++;
        UpdateKillUI();
    }

    private void UpdateKillUI()
    {
        if (killCountText != null)
        {
            killCountText.text = kills.ToString();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthText != null && playerHealth != null)
        {
            healthText.text = Mathf.RoundToInt(playerHealth.currentHealth).ToString();
        }
    }

    private void EndGame()
    {
        gameEnded = true;
        Debug.Log("GAME OVER");
        Time.timeScale = 0f;
    }
}