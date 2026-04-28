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
    [SerializeField] private int defaultPointsPerKill = 100;
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
        AddKill(defaultPointsPerKill);
    }

    public void AddKill(int enemyBasePoints)
    {
        if (gameEnded) return;

        kills++;

        int pointsThisKill = Mathf.RoundToInt(enemyBasePoints * intensityMultiplier);

        if (playerHealth != null)
        {
            playerHealth.AddHealthScore(pointsThisKill);
        }

        intensityMultiplier += intensityIncreasePerKill;
        intensityMultiplier = Mathf.Clamp(intensityMultiplier, 1f, maxIntensityMultiplier);

        UpdateUI();

        Debug.Log("Kill +" + pointsThisKill + " points");
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

        MainGameAudioOnly audio = FindObjectOfType<MainGameAudioOnly>();
        if (audio != null)
            audio.StopAudio();

        if (CyberEndSequence.Instance != null)
        {
            CyberEndSequence.Instance.PlayLoseEnding();
        }
    }

    public void WinGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log("YOU WIN");

        MainGameAudioOnly audio = FindObjectOfType<MainGameAudioOnly>();
        if (audio != null)
            audio.StopAudio();

        if (CyberEndSequence.Instance != null)
        {
            CyberEndSequence.Instance.PlayWinEnding();
        }
    }
}