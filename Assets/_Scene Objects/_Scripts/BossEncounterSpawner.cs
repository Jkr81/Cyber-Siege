using UnityEngine;
using System.Collections;

public class BossEncounterSpawner : MonoBehaviour
{
    [Header("Boss Setup")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject bossAsteroidPrefab;
    [SerializeField] private Transform player;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnDistanceFromStart = 1200f;
    [SerializeField] private float spawnInFrontOfPlayer = 220f;

    [Header("Boss Placement")]
    [SerializeField] private Vector3 asteroidOffset = new Vector3(0f, -8f, 0f);
    [SerializeField] private Vector3 bossOffsetOnAsteroid = new Vector3(0f, 10f, 0f);

    [Header("Asteroid Movement")]
    [SerializeField] private bool moveBossAsteroid = true;
    [SerializeField] private float asteroidMoveAmount = 6f;
    [SerializeField] private float asteroidMoveSpeed = 1.2f;

    [Header("Ship Stop")]
    [SerializeField] private ShipFollowHybrid shipMovement;
    [SerializeField] private bool stopShipDuringBoss = true;

    [Header("Entrance")]
    [SerializeField] private AudioSource bossEntranceSound;
    [SerializeField] private GameObject entranceEffect;
    [SerializeField] private bool scaleEncounterIn = true;
    [SerializeField] private float scaleInDuration = 2f;

    private Vector3 startPosition;
    private bool spawned = false;

    private void Start()
    {
        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        if (player != null)
            startPosition = player.position;
    }

    private void Update()
    {
        if (spawned) return;
        if (player == null || bossPrefab == null || bossAsteroidPrefab == null) return;

        float distance = Vector3.Distance(startPosition, player.position);

        if (distance >= spawnDistanceFromStart)
            SpawnBossEncounter();
    }

    private void SpawnBossEncounter()
    {
        spawned = true;

        Vector3 basePosition = player.position + player.forward * spawnInFrontOfPlayer;
        Vector3 asteroidPosition = basePosition + asteroidOffset;

        GameObject asteroid = Instantiate(bossAsteroidPrefab, asteroidPosition, Quaternion.identity);

        Vector3 bossPosition = asteroid.transform.position + bossOffsetOnAsteroid;

        GameObject boss = Instantiate(
            bossPrefab,
            bossPosition,
            Quaternion.LookRotation(-player.forward)
        );

        boss.transform.SetParent(asteroid.transform, true);

        if (scaleEncounterIn)
            StartCoroutine(ScaleInEncounter(asteroid, boss, scaleInDuration));

        if (moveBossAsteroid)
        {
            BossAsteroidMover mover = asteroid.AddComponent<BossAsteroidMover>();
            mover.Setup(asteroidMoveAmount, asteroidMoveSpeed);
        }

        if (entranceEffect != null)
            Instantiate(entranceEffect, bossPosition, Quaternion.identity);

        if (bossEntranceSound != null)
            bossEntranceSound.Play();

        if (stopShipDuringBoss && shipMovement != null)
            shipMovement.SetBossPaused(true);

        Debug.Log("BOSS ENCOUNTER STARTED");
    }

    private IEnumerator ScaleInEncounter(GameObject asteroid, GameObject boss, float duration)
    {
        Vector3 asteroidFinalScale = asteroid.transform.localScale;
        Vector3 bossFinalScale = boss.transform.localScale;

        asteroid.transform.localScale = Vector3.zero;
        boss.transform.localScale = Vector3.zero;

        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            asteroid.transform.localScale = Vector3.Lerp(Vector3.zero, asteroidFinalScale, t);
            boss.transform.localScale = Vector3.Lerp(Vector3.zero, bossFinalScale, t);

            timer += Time.deltaTime;
            yield return null;
        }

        asteroid.transform.localScale = asteroidFinalScale;
        boss.transform.localScale = bossFinalScale;
    }
}

public class BossAsteroidMover : MonoBehaviour
{
    private Vector3 startPosition;
    private float moveAmount;
    private float moveSpeed;

    public void Setup(float amount, float speed)
    {
        moveAmount = amount;
        moveSpeed = speed;
        startPosition = transform.position;
    }

    private void Update()
    {
        float xOffset = Mathf.Sin(Time.time * moveSpeed) * moveAmount;
        transform.position = startPosition + Vector3.right * xOffset;
    }
}