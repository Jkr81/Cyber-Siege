using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CyberEndSequence : MonoBehaviour
{
    public static CyberEndSequence Instance;

    [Header("Player / Ship")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform endSpacePoint;

    [Header("Disable On End")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Hyperspace Effect")]
    [SerializeField] private GameObject hyperspaceEffect;
    [SerializeField] private float effectDuration = 2.0f;

    [Header("End Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip winAudio;
    [SerializeField] private AudioClip loseAudio;

    private bool endingStarted = false;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (hyperspaceEffect != null) hyperspaceEffect.SetActive(false);
    }

    public void PlayWinEnding()
    {
        if (endingStarted) return;
        endingStarted = true;
        StartCoroutine(HandleEnding(true));
    }

    public void PlayLoseEnding()
    {
        if (endingStarted) return;
        endingStarted = true;
        StartCoroutine(HandleEnding(false));
    }

    private IEnumerator HandleEnding(bool won)
    {
        // Freeze gameplay systems, NOT the whole Unity time
        DisableSystems();

        // Teleport player/ship to ending space
        TeleportToEnd();

        // Show hyperspace effect
        if (hyperspaceEffect != null)
        {
            hyperspaceEffect.SetActive(true);
            RestartEffect(hyperspaceEffect);
        }

        yield return new WaitForSeconds(effectDuration);

        if (hyperspaceEffect != null)
            hyperspaceEffect.SetActive(false);

        if (won)
        {
            if (winPanel != null) winPanel.SetActive(true);

            if (audioSource != null && winAudio != null)
                audioSource.PlayOneShot(winAudio);
        }
        else
        {
            if (losePanel != null) losePanel.SetActive(true);

            if (audioSource != null && loseAudio != null)
                audioSource.PlayOneShot(loseAudio);
        }
    }

    private void RestartEffect(GameObject effect)
    {
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        Animator[] animators = effect.GetComponentsInChildren<Animator>(true);

        foreach (Animator anim in animators)
        {
            anim.Play(0, 0, 0f);
        }
    }

    private void DisableSystems()
    {
        foreach (var script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = false;
        }
    }

    private void TeleportToEnd()
    {
        if (playerRoot != null && endSpacePoint != null)
        {
            playerRoot.position = endSpacePoint.position;
            playerRoot.rotation = endSpacePoint.rotation;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}