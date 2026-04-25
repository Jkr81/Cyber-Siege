using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Health / Score")]
    [SerializeField] private PlayerDamageReceiver playerHealth;
    [SerializeField] private TMP_Text healthScoreText;

    [Header("HUD")]
    [SerializeField] private TMP_Text killCountText;

    [Header("Points")]
    [SerializeField] private int basePointsPerKill = 100;
    [SerializeField] private float intensityMultiplier = 1f;
    [SerializeField] private float intensityIncreasePerKill = 0.15f;
    [SerializeField] private float maxIntensityMultiplier = 5f;

    private int kills = 0;
    private bool gameEnded = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        UpdateUI();
    }

    void Update()
    {
        if (gameEnded) return;
        if (playerHealth == null) return;

        UpdateUI();

        if (playerHealth.currentHealth <= 0)
        {
            EndGame();
        }
    }

    public void AddKill()
    {
        kills++;

        int pointsThisKill = Mathf.RoundToInt(basePointsPerKill * intensityMultiplier);

        if (playerHealth != null)
        {
            playerHealth.AddHealthScore(pointsThisKill);
        }

        intensityMultiplier += intensityIncreasePerKill;
        intensityMultiplier = Mathf.Clamp(intensityMultiplier, 1f, maxIntensityMultiplier);

        UpdateUI();

        Debug.Log("Kill +" + pointsThisKill + " | Health/Score: " + playerHealth.currentHealth);
    }

    private void UpdateUI()
    {
        if (healthScoreText != null && playerHealth != null)
        {
            healthScoreText.text = Mathf.RoundToInt(playerHealth.currentHealth).ToString();
        }

        if (killCountText != null)
        {
            killCountText.text = kills.ToString();
        }
    }

    private void EndGame()
    {
        gameEnded = true;
        Debug.Log("GAME OVER");
        Time.timeScale = 0f;
    }
}