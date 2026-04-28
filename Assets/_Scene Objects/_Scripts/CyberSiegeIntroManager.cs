using UnityEngine;
using System.Collections;

public class CyberSiegeIntroManager : MonoBehaviour
{
    [Header("Intro Panels")]
    [SerializeField] private GameObject panel1;
    [SerializeField] private GameObject panel2;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip welcomeAudio;
    [SerializeField] private AudioClip enemyIntroAudio;

    [SerializeField] private float panel1AudioFadeOutTime = 0.5f;
    [SerializeField] private float delayBeforePanel2Audio = 0.7f;

    [SerializeField] private float panel2AudioFadeOutTime = 0.3f;
    [SerializeField] private float enemySpawnDelayAfterAudio = 0.5f;

    [Header("Ship Movement")]
    [SerializeField] private ShipFollowHybrid shipMovement;

    [Header("Tutorial Enemy")]
    [SerializeField] private GameObject tutorialEnemyPrefab;
    [SerializeField] private Transform tutorialEnemySpawnPoint;

    [Header("Tunnel Spawner")]
    [SerializeField] private AsteroidTunnelSpawner tunnelSpawner;

    [Header("Enemy Spawners")]
    [SerializeField] private GameObject[] enemySpawners;

    [Header("Light Speed Effect")]
    [SerializeField] private GameObject hyperspaceEffect;
    [SerializeField] private float hyperspaceDuration = 3.5f;
    [SerializeField] private float hyperspaceBuildUpDelay = 0.5f;
    [SerializeField] private float lightSpeedBoost = 45f;

    [Header("Effect Follow Position")]
    [SerializeField] private Vector3 hyperspaceLocalPosition = new Vector3(0f, 0f, 8f);
    [SerializeField] private Vector3 hyperspaceLocalRotation = Vector3.zero;

    private GameObject spawnedTutorialEnemy;
    private bool introComplete = false;
    private bool panel1Used = false;
    private bool panel2Used = false;
    private Transform originalHyperspaceParent;

    private void Start()
    {
        // Ensure music is OFF during intro
        MainGameAudioOnly music = FindObjectOfType<MainGameAudioOnly>();
        if (music != null)
            music.StopAudio();

        DisableRealGameSpawners();

        if (hyperspaceEffect != null)
        {
            originalHyperspaceParent = hyperspaceEffect.transform.parent;
            hyperspaceEffect.SetActive(false);
        }

        if (shipMovement != null)
        {
            shipMovement.SetIntroPaused(true);
            shipMovement.SetIntroAutoCentering(true);
        }

        if (panel1 != null) panel1.SetActive(true);
        if (panel2 != null) panel2.SetActive(false);

        PlayAudio(welcomeAudio);
    }

    private void DisableRealGameSpawners()
    {
        if (tunnelSpawner != null)
            tunnelSpawner.enabled = false;

        foreach (GameObject spawner in enemySpawners)
        {
            if (spawner != null)
                spawner.SetActive(false);
        }
    }

    private void EnableRealGameSpawners()
    {
        if (tunnelSpawner != null)
            tunnelSpawner.enabled = true;

        if (shipMovement != null)
        {
            shipMovement.SetIntroPaused(false);
            shipMovement.SetIntroAutoCentering(false);
        }

        foreach (GameObject spawner in enemySpawners)
        {
            if (spawner != null)
                spawner.SetActive(true);
        }
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.Stop();
        audioSource.volume = 1f;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private IEnumerator FadeOutAudio(float duration)
    {
        if (audioSource == null || !audioSource.isPlaying)
            yield break;

        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public void ShootPanel1()
    {
        if (panel1Used) return;
        panel1Used = true;

        if (panel1 != null)
            panel1.SetActive(false);

        StartCoroutine(TransitionToPanel2());
    }

    private IEnumerator TransitionToPanel2()
    {
        yield return StartCoroutine(FadeOutAudio(panel1AudioFadeOutTime));
        yield return new WaitForSeconds(delayBeforePanel2Audio);

        if (panel2 != null)
            panel2.SetActive(true);

        PlayAudio(enemyIntroAudio);
    }

    public void ShootPanel2()
    {
        if (panel2Used) return;
        panel2Used = true;

        if (panel2 != null)
            panel2.SetActive(false);

        StartCoroutine(HandlePanel2Transition());
    }

    private IEnumerator HandlePanel2Transition()
    {
        yield return StartCoroutine(FadeOutAudio(panel2AudioFadeOutTime));
        yield return new WaitForSeconds(enemySpawnDelayAfterAudio);

        SpawnTutorialEnemy();
    }

    private void SpawnTutorialEnemy()
    {
        if (tutorialEnemyPrefab == null || tutorialEnemySpawnPoint == null)
        {
            Debug.LogWarning("Tutorial enemy prefab or spawn point missing.");
            return;
        }

        spawnedTutorialEnemy = Instantiate(
            tutorialEnemyPrefab,
            tutorialEnemySpawnPoint.position,
            tutorialEnemySpawnPoint.rotation
        );

        StartCoroutine(WaitForEnemyDeath());
    }

    private IEnumerator WaitForEnemyDeath()
    {
        while (spawnedTutorialEnemy != null)
            yield return null;

        StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
    {
        if (introComplete) yield break;
        introComplete = true;

        if (hyperspaceEffect != null && shipMovement != null)
        {
            hyperspaceEffect.transform.SetParent(shipMovement.transform);
            hyperspaceEffect.transform.localPosition = hyperspaceLocalPosition;
            hyperspaceEffect.transform.localEulerAngles = hyperspaceLocalRotation;
            hyperspaceEffect.SetActive(true);
        }

        yield return new WaitForSeconds(hyperspaceBuildUpDelay);

        if (shipMovement != null)
        {
            shipMovement.SetIntroPaused(false);
            shipMovement.SetIntroAutoCentering(false);
            shipMovement.StartLightSpeedBurst(hyperspaceDuration, lightSpeedBoost);
        }

        yield return new WaitForSeconds(hyperspaceDuration);

        if (hyperspaceEffect != null)
        {
            hyperspaceEffect.SetActive(false);
            hyperspaceEffect.transform.SetParent(originalHyperspaceParent);
        }

        EnableRealGameSpawners();

        MainGameAudioOnly music = FindObjectOfType<MainGameAudioOnly>();

        if (music != null)
        {
            Debug.Log("MAIN GAME MUSIC STARTED");
            music.PlayAudio();
        }
    }
}