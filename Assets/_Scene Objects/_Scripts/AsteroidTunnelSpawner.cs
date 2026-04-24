using UnityEngine;
using System.Collections.Generic;

public class AsteroidTunnelSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Asteroid Prefabs")]
    [SerializeField] private GameObject[] asteroidPrefabs;

    [Header("Enemy Spawn")]
    [SerializeField] private GameObject whiteEnemyPrefab;
    [SerializeField] private GameObject redEnemyPrefab;
    [SerializeField, Range(0f, 1f)] private float enemySpawnChance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float redEnemyChance = 0.25f;
    [SerializeField] private float enemyHeightOffset = 1.5f;
    [SerializeField] private bool enemyFacesPlayer = true;

    [Header("Asteroid Glow")]
    [SerializeField] private bool enableAsteroidGlow = true;
    [SerializeField] private Color normalGlowColor = new Color(0.6f, 0f, 1f);
    [SerializeField] private Color enemyAsteroidGlowColor = Color.red;
    [SerializeField] private Color movingAsteroidGlowColor = Color.cyan;
    [SerializeField] private float glowIntensity = 4f;

    [Header("Moving Empty Asteroids")]
    [SerializeField] private bool moveEmptyAsteroids = true;
    [SerializeField, Range(0f, 1f)] private float movingEmptyAsteroidChance = 0.5f;
    [SerializeField] private float minDriftSpeed = 0.15f;
    [SerializeField] private float maxDriftSpeed = 0.5f;
    [SerializeField] private float sidewaysDriftAmount = 0.25f;
    [SerializeField] private float verticalDriftAmount = 0.15f;
    [SerializeField] private float minRotationSpeed = 2f;
    [SerializeField] private float maxRotationSpeed = 8f;

    [Header("Section Spawn")]
    [SerializeField] private float sectionLength = 40f;
    [SerializeField] private int sectionsAhead = 6;
    [SerializeField] private int sectionsBehindToKeep = 2;

    [Header("Start Gap")]
    [SerializeField] private float startOffset = 25f;
    [SerializeField] private float safeRadius = 12f;

    [Header("Asteroid Scale")]
    [SerializeField] private float randomScaleMin = 0.7f;
    [SerializeField] private float randomScaleMax = 1.6f;

    [Header("Rotation")]
    [SerializeField] private bool randomRotation = true;

    [Header("Optional")]
    [SerializeField] private bool clearChildrenOnStart = true;

    [Header("Zone 1: Intro (0 - 100)")]
    [SerializeField] private float zone1Radius = 10f;
    [SerializeField] private float zone1Thickness = 2f;
    [SerializeField] private int zone1AsteroidsPerSection = 22;

    [Header("Zone 2: Combat (100 - 250)")]
    [SerializeField] private float zone2Radius = 8f;
    [SerializeField] private float zone2Thickness = 2.2f;
    [SerializeField] private int zone2AsteroidsPerSection = 32;

    [Header("Zone 3: Danger (250 - 400)")]
    [SerializeField] private float zone3Radius = 6f;
    [SerializeField] private float zone3Thickness = 2.5f;
    [SerializeField] private int zone3AsteroidsPerSection = 42;

    [Header("Zone 4: Boss (400+)")]
    [SerializeField] private float zone4Radius = 12f;
    [SerializeField] private float zone4Thickness = 1.5f;
    [SerializeField] private int zone4AsteroidsPerSection = 12;

    private class SpawnedSection
    {
        public int sectionIndex;
        public List<GameObject> asteroids = new List<GameObject>();
    }

    private struct SpawnSettings
    {
        public float radius;
        public float thickness;
        public int asteroidsPerSection;

        public SpawnSettings(float radius, float thickness, int asteroidsPerSection)
        {
            this.radius = radius;
            this.thickness = thickness;
            this.asteroidsPerSection = asteroidsPerSection;
        }
    }

    private readonly Dictionary<int, SpawnedSection> spawnedSections = new Dictionary<int, SpawnedSection>();

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("AsteroidTunnelSpawner: No player assigned.");
            enabled = false;
            return;
        }

        if (asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            Debug.LogWarning("AsteroidTunnelSpawner: No asteroid prefabs assigned.");
            enabled = false;
            return;
        }

        if (clearChildrenOnStart)
            ClearSpawnedAsteroids();

        UpdateTunnel();
    }

    private void Update()
    {
        UpdateTunnel();
        UpdateMovingAsteroids();
    }

    private void UpdateTunnel()
    {
        float playerForwardDistance = GetPlayerForwardDistance();
        int currentSection = Mathf.FloorToInt(playerForwardDistance / sectionLength);

        for (int i = 0; i <= sectionsAhead; i++)
        {
            int sectionToSpawn = currentSection + i;

            if (!spawnedSections.ContainsKey(sectionToSpawn))
                SpawnSection(sectionToSpawn);
        }

        List<int> sectionsToRemove = new List<int>();

        foreach (var kvp in spawnedSections)
        {
            if (kvp.Key < currentSection - sectionsBehindToKeep)
                sectionsToRemove.Add(kvp.Key);
        }

        for (int i = 0; i < sectionsToRemove.Count; i++)
            RemoveSection(sectionsToRemove[i]);
    }

    private float GetPlayerForwardDistance()
    {
        Vector3 tunnelStart = transform.position + player.forward * startOffset;
        Vector3 fromStartToPlayer = player.position - tunnelStart;
        return Vector3.Dot(fromStartToPlayer, player.forward);
    }

    private void SpawnSection(int sectionIndex)
    {
        SpawnSettings settings = GetSettingsForDistance(sectionIndex * sectionLength);

        SpawnedSection newSection = new SpawnedSection();
        newSection.sectionIndex = sectionIndex;

        Vector3 tunnelStart = transform.position + player.forward * startOffset;
        float sectionStartDistance = sectionIndex * sectionLength;
        float sectionEndDistance = sectionStartDistance + sectionLength;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = settings.asteroidsPerSection * 12;

        while (spawned < settings.asteroidsPerSection && attempts < maxAttempts)
        {
            attempts++;

            float forwardDistance = Random.Range(sectionStartDistance, sectionEndDistance);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(settings.radius, settings.radius + settings.thickness);

            Vector3 localOffset = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                forwardDistance
            );

            Vector3 worldPos = tunnelStart + player.TransformDirection(localOffset);

            if (Vector3.Distance(worldPos, player.position) < safeRadius)
                continue;

            GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            Quaternion rot = randomRotation ? Random.rotation : Quaternion.identity;

            GameObject asteroid = Instantiate(prefab, worldPos, rot, transform);

            float randomScale = Random.Range(randomScaleMin, randomScaleMax);
            asteroid.transform.localScale = Vector3.one * randomScale;

            bool spawnedEnemy = TrySpawnEnemyOnAsteroid(asteroid);
            bool isMovingAsteroid = false;

            if (!spawnedEnemy && moveEmptyAsteroids && Random.value < movingEmptyAsteroidChance)
            {
                MovingAsteroid moving = asteroid.AddComponent<MovingAsteroid>();
                moving.Initialize(
                    Random.Range(minDriftSpeed, maxDriftSpeed),
                    sidewaysDriftAmount,
                    verticalDriftAmount,
                    Random.Range(minRotationSpeed, maxRotationSpeed)
                );

                isMovingAsteroid = true;
            }

            if (enableAsteroidGlow)
            {
                if (spawnedEnemy)
                    ApplyGlow(asteroid, enemyAsteroidGlowColor);
                else if (isMovingAsteroid)
                    ApplyGlow(asteroid, movingAsteroidGlowColor);
                else
                    ApplyGlow(asteroid, normalGlowColor);
            }

            newSection.asteroids.Add(asteroid);
            spawned++;
        }

        spawnedSections.Add(sectionIndex, newSection);
    }

    private bool TrySpawnEnemyOnAsteroid(GameObject asteroid)
    {
        if (Random.value > enemySpawnChance)
            return false;

        GameObject chosenEnemyPrefab = ChooseEnemyPrefab();

        if (chosenEnemyPrefab == null)
            return false;

        Vector3 enemyPos = asteroid.transform.position + Vector3.up * enemyHeightOffset;

        Quaternion enemyRot = Quaternion.identity;

        if (enemyFacesPlayer && player != null)
        {
            Vector3 lookDirection = player.position - enemyPos;
            lookDirection.y = 0f;

            if (lookDirection != Vector3.zero)
                enemyRot = Quaternion.LookRotation(lookDirection);
        }

        GameObject enemy = Instantiate(chosenEnemyPrefab, enemyPos, enemyRot, asteroid.transform);

        enemy.transform.localScale = Vector3.one;

        return true;
    }

    private GameObject ChooseEnemyPrefab()
    {
        if (whiteEnemyPrefab == null && redEnemyPrefab == null)
            return null;

        if (whiteEnemyPrefab == null)
            return redEnemyPrefab;

        if (redEnemyPrefab == null)
            return whiteEnemyPrefab;

        return Random.value < redEnemyChance ? redEnemyPrefab : whiteEnemyPrefab;
    }

    private void ApplyGlow(GameObject asteroid, Color glowColor)
    {
        Renderer[] renderers = asteroid.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            Material[] materials = r.materials;

            foreach (Material mat in materials)
            {
                if (mat == null)
                    continue;

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", glowColor * glowIntensity);
                }

                if (mat.HasProperty("_BaseColor"))
                {
                    Color baseColor = Color.Lerp(Color.black, glowColor, 0.35f);
                    mat.SetColor("_BaseColor", baseColor);
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color baseColor = Color.Lerp(Color.black, glowColor, 0.35f);
                    mat.SetColor("_Color", baseColor);
                }
            }
        }
    }

    private void RemoveSection(int sectionIndex)
    {
        if (!spawnedSections.TryGetValue(sectionIndex, out SpawnedSection section))
            return;

        for (int i = 0; i < section.asteroids.Count; i++)
        {
            if (section.asteroids[i] != null)
                Destroy(section.asteroids[i]);
        }

        spawnedSections.Remove(sectionIndex);
    }

    private void ClearSpawnedAsteroids()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        spawnedSections.Clear();
    }

    private SpawnSettings GetSettingsForDistance(float distance)
    {
        if (distance < 100f)
            return new SpawnSettings(zone1Radius, zone1Thickness, zone1AsteroidsPerSection);

        if (distance < 250f)
            return new SpawnSettings(zone2Radius, zone2Thickness, zone2AsteroidsPerSection);

        if (distance < 400f)
            return new SpawnSettings(zone3Radius, zone3Thickness, zone3AsteroidsPerSection);

        return new SpawnSettings(zone4Radius, zone4Thickness, zone4AsteroidsPerSection);
    }

    private void UpdateMovingAsteroids()
    {
        MovingAsteroid[] movingAsteroids = GetComponentsInChildren<MovingAsteroid>();

        for (int i = 0; i < movingAsteroids.Length; i++)
        {
            if (movingAsteroids[i] != null)
                movingAsteroids[i].ManualUpdate();
        }
    }

    private class MovingAsteroid : MonoBehaviour
    {
        private float driftSpeed;
        private float rotationSpeed;
        private Vector3 driftDirection;

        public void Initialize(float speed, float sidewaysAmount, float verticalAmount, float rotation)
        {
            driftSpeed = speed;
            rotationSpeed = rotation;

            driftDirection = new Vector3(
                Random.Range(-sidewaysAmount, sidewaysAmount),
                Random.Range(-verticalAmount, verticalAmount),
                -1f
            ).normalized;
        }

        public void ManualUpdate()
        {
            transform.position += driftDirection * driftSpeed * Time.deltaTime;

            transform.Rotate(
                rotationSpeed * Time.deltaTime,
                rotationSpeed * 0.7f * Time.deltaTime,
                rotationSpeed * 0.4f * Time.deltaTime,
                Space.Self
            );
        }
    }
}