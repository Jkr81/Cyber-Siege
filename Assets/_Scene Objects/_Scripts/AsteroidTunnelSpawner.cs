using UnityEngine;
using System.Collections.Generic;

public class AsteroidTunnelSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private ShipFollowHybrid ship;

    [Header("Asteroid Prefabs")]
    [SerializeField] private GameObject[] asteroidPrefabs;

    [Header("Curved Tunnel")]
    [SerializeField] private bool enableCurve = true;
    [SerializeField] private float curveSideAmount = 10f;
    [SerializeField] private float curveUpAmount = 4f;
    [SerializeField] private float curveLength = 250f;

    [Header("Progression Timer")]
    [SerializeField] private float timeToMaxDifficulty = 180f;
    [SerializeField] private float enemyStartDelay = 5f;
    private float runTimer = 0f;

    [Header("Distance Zones")]
    [SerializeField] private float introEndDistance = 250f;
    [SerializeField] private float combatEndDistance = 700f;
    [SerializeField] private float chaosEndDistance = 1200f;

    [Header("Difficulty")]
    [SerializeField] private float maxDifficulty = 3f;
    [SerializeField] private float maxEnemySpawnChance = 0.6f;
    [SerializeField] private float maxRedEnemyChance = 0.45f;

    [Header("Enemy Spawn")]
    [SerializeField] private GameObject whiteEnemyPrefab;
    [SerializeField] private GameObject redEnemyPrefab;
    [SerializeField, Range(0f, 1f)] private float enemySpawnChance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float redEnemyChance = 0.15f;
    [SerializeField] private int minEnemySpawnPointsPerSection = 2;
    [SerializeField] private float enemyExtraHeight = -0.3f;
    [SerializeField] private bool enemyFacesPlayer = true;
    [SerializeField] private float enemyActivationDistance = 140f;

    [Header("Enemy Aim")]
    [SerializeField] private float enemyAimHeightOffset = 1.2f;

    [Header("Enemy Spawn Constraints")]
    [SerializeField] private float minEnemyForwardDistance = 60f;
    [SerializeField] private float maxEnemyForwardDistance = 220f;
    [SerializeField] private float minEnemyHeightRelativeToPlayer = -0.5f;
    [SerializeField] private float maxEnemyHeightRelativeToPlayer = 20f;

    [Header("Enemy Despawn")]
    [SerializeField] private bool fadeEnemyOnDespawn = true;
    [SerializeField] private float enemyFadeTime = 0.75f;

    [Header("Asteroid Glow")]
    [SerializeField] private bool enableAsteroidGlow = true;
    [SerializeField] private Color normalGlowColor = new Color(0.6f, 0f, 1f);
    [SerializeField] private Color enemyAsteroidGlowColor = Color.red;
    [SerializeField] private float glowIntensity = 4f;

    [Header("All Asteroid Left/Right Drift")]
    [SerializeField] private bool driftAllAsteroids = true;
    [SerializeField] private float driftAmount = 0.5f;
    [SerializeField] private float driftSpeed = 0.6f;

    [Header("Section Spawn")]
    [SerializeField] private float sectionLength = 40f;
    [SerializeField] private int sectionsAhead = 10;
    [SerializeField] private int sectionsBehindToKeep = 1;

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

    [Header("Zone 1: Intro")]
    [SerializeField] private float zone1Radius = 7f;
    [SerializeField] private float zone1Thickness = 1.5f;
    [SerializeField] private int zone1AsteroidsPerSection = 26;

    [Header("Zone 2: Combat")]
    [SerializeField] private float zone2Radius = 5.5f;
    [SerializeField] private float zone2Thickness = 1.6f;
    [SerializeField] private int zone2AsteroidsPerSection = 34;

    [Header("Zone 3: Chaos")]
    [SerializeField] private float zone3Radius = 4.5f;
    [SerializeField] private float zone3Thickness = 1.8f;
    [SerializeField] private int zone3AsteroidsPerSection = 40;

    [Header("Zone 4: Boss Arena")]
    [SerializeField] private float zone4Radius = 8f;
    [SerializeField] private float zone4Thickness = 1.2f;
    [SerializeField] private int zone4AsteroidsPerSection = 18;

    private class SpawnedSection
    {
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
        if (player == null || asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            Debug.LogWarning("AsteroidTunnelSpawner: Missing player or asteroid prefabs.");
            enabled = false;
            return;
        }

        if (ship == null)
            ship = player.GetComponentInParent<ShipFollowHybrid>();

        if (clearChildrenOnStart)
            ClearSpawnedAsteroids();

        UpdateTunnel();
    }

    private void Update()
    {
        runTimer += Time.deltaTime;

        UpdateTunnel();
        UpdateAsteroidBehaviors();
    }

    private float GetDifficulty()
    {
        float timeDifficulty = Mathf.Lerp(1f, maxDifficulty, Mathf.Clamp01(runTimer / timeToMaxDifficulty));

        if (ship == null)
            return timeDifficulty;

        return Mathf.Clamp(Mathf.Max(ship.DifficultyMultiplier, timeDifficulty), 1f, maxDifficulty);
    }

    private float GetPlayerForwardDistance()
    {
        Vector3 tunnelStart = transform.position + Vector3.forward * startOffset;
        Vector3 fromStartToPlayer = player.position - tunnelStart;
        return Vector3.Dot(fromStartToPlayer, Vector3.forward);
    }

    private Vector3 GetTunnelCenter(float distance)
    {
        Vector3 basePos = transform.position + Vector3.forward * (startOffset + distance);

        if (!enableCurve)
            return basePos;

        float t = distance / curveLength;

        float sideCurve = Mathf.Sin(t) * curveSideAmount;
        float upCurve = Mathf.Sin(t * 0.6f) * curveUpAmount;

        return basePos + Vector3.right * sideCurve + Vector3.up * upCurve;
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

        foreach (int sectionIndex in sectionsToRemove)
            RemoveSection(sectionIndex);
    }

    private void SpawnSection(int sectionIndex)
    {
        float sectionStartDistance = sectionIndex * sectionLength;
        float sectionEndDistance = sectionStartDistance + sectionLength;

        SpawnSettings settings = GetSettingsForDistance(sectionStartDistance);
        SpawnedSection newSection = new SpawnedSection();

        int enemySpawnPointsThisSection = 0;
        List<GameObject> validEnemyAsteroids = new List<GameObject>();

        float difficulty = GetDifficulty();

        float scaledEnemySpawnChance = Mathf.Clamp(
            enemySpawnChance * difficulty,
            enemySpawnChance,
            maxEnemySpawnChance
        );

        float scaledActivationDistance = enemyActivationDistance * difficulty;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = settings.asteroidsPerSection * 15;

        while (spawned < settings.asteroidsPerSection && attempts < maxAttempts)
        {
            attempts++;

            float forwardDistance = Random.Range(sectionStartDistance, sectionEndDistance);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(settings.radius, settings.radius + settings.thickness);

            Vector3 tunnelCenter = GetTunnelCenter(forwardDistance);

            Vector3 ringOffset =
                Vector3.right * Mathf.Cos(angle) * radius +
                Vector3.up * Mathf.Sin(angle) * radius;

            Vector3 worldPos = tunnelCenter + ringOffset;

            if (Vector3.Distance(worldPos, player.position) < safeRadius)
                continue;

            GameObject asteroidPrefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            Quaternion asteroidRot = randomRotation ? Random.rotation : Quaternion.identity;

            GameObject asteroid = Instantiate(asteroidPrefab, worldPos, asteroidRot, transform);
            asteroid.transform.localScale = Vector3.one * Random.Range(randomScaleMin, randomScaleMax);

            Rigidbody[] rigidbodies = asteroid.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            if (driftAllAsteroids)
            {
                HorizontalDriftAsteroid drift = asteroid.AddComponent<HorizontalDriftAsteroid>();
                drift.Initialize(driftAmount, driftSpeed * difficulty);
            }

            bool validForEnemy = IsValidEnemyAsteroidPosition(asteroid.transform.position);

            if (validForEnemy)
                validEnemyAsteroids.Add(asteroid);

            bool willHaveEnemy = validForEnemy && Random.value <= scaledEnemySpawnChance;
            GameObject enemyPrefab = willHaveEnemy ? ChooseEnemyPrefab(difficulty) : null;

            if (enableAsteroidGlow)
                ApplyGlow(asteroid, enemyPrefab != null ? enemyAsteroidGlowColor : normalGlowColor);

            if (enemyPrefab != null)
            {
                EnemySpawnPoint spawnPoint = asteroid.AddComponent<EnemySpawnPoint>();
                spawnPoint.Initialize(
                    enemyPrefab,
                    player,
                    scaledActivationDistance,
                    enemyExtraHeight,
                    enemyFacesPlayer,
                    fadeEnemyOnDespawn,
                    enemyFadeTime,
                    enemyAimHeightOffset,
                    enemyStartDelay
                );

                enemySpawnPointsThisSection++;
            }

            newSection.asteroids.Add(asteroid);
            spawned++;
        }

        while (
            enemySpawnPointsThisSection < minEnemySpawnPointsPerSection &&
            validEnemyAsteroids.Count > 0
        )
        {
            int index = Random.Range(0, validEnemyAsteroids.Count);
            GameObject asteroid = validEnemyAsteroids[index];
            validEnemyAsteroids.RemoveAt(index);

            if (asteroid == null)
                continue;

            if (asteroid.GetComponent<EnemySpawnPoint>() != null)
                continue;

            GameObject forcedEnemyPrefab = ChooseEnemyPrefab(difficulty);

            if (forcedEnemyPrefab == null)
                break;

            EnemySpawnPoint spawnPoint = asteroid.AddComponent<EnemySpawnPoint>();
            spawnPoint.Initialize(
                forcedEnemyPrefab,
                player,
                scaledActivationDistance,
                enemyExtraHeight,
                enemyFacesPlayer,
                fadeEnemyOnDespawn,
                enemyFadeTime,
                enemyAimHeightOffset,
                enemyStartDelay
            );

            if (enableAsteroidGlow)
                ApplyGlow(asteroid, enemyAsteroidGlowColor);

            enemySpawnPointsThisSection++;
        }

        spawnedSections.Add(sectionIndex, newSection);
    }

    private SpawnSettings GetSettingsForDistance(float distance)
    {
        if (distance < introEndDistance)
            return new SpawnSettings(zone1Radius, zone1Thickness, zone1AsteroidsPerSection);

        if (distance < combatEndDistance)
            return new SpawnSettings(zone2Radius, zone2Thickness, zone2AsteroidsPerSection);

        if (distance < chaosEndDistance)
            return new SpawnSettings(zone3Radius, zone3Thickness, zone3AsteroidsPerSection);

        return new SpawnSettings(zone4Radius, zone4Thickness, zone4AsteroidsPerSection);
    }

    private bool IsValidEnemyAsteroidPosition(Vector3 asteroidPosition)
    {
        if (player == null)
            return false;

        Vector3 toAsteroid = asteroidPosition - player.position;

        float forwardDistance = Vector3.Dot(toAsteroid, Vector3.forward);
        float heightDifference = asteroidPosition.y - player.position.y;

        if (forwardDistance < minEnemyForwardDistance)
            return false;

        if (forwardDistance > maxEnemyForwardDistance)
            return false;

        if (heightDifference < minEnemyHeightRelativeToPlayer)
            return false;

        if (heightDifference > maxEnemyHeightRelativeToPlayer)
            return false;

        return true;
    }

    private GameObject ChooseEnemyPrefab(float difficulty)
    {
        if (whiteEnemyPrefab == null && redEnemyPrefab == null)
            return null;

        if (whiteEnemyPrefab == null)
            return redEnemyPrefab;

        if (redEnemyPrefab == null)
            return whiteEnemyPrefab;

        float scaledRedChance = Mathf.Clamp(
            redEnemyChance * difficulty,
            redEnemyChance,
            maxRedEnemyChance
        );

        return Random.value < scaledRedChance ? redEnemyPrefab : whiteEnemyPrefab;
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
                    mat.SetColor("_BaseColor", Color.Lerp(Color.black, glowColor, 0.35f));
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", Color.Lerp(Color.black, glowColor, 0.35f));
            }
        }
    }

    private void RemoveSection(int sectionIndex)
    {
        if (!spawnedSections.TryGetValue(sectionIndex, out SpawnedSection section))
            return;

        foreach (GameObject asteroid in section.asteroids)
        {
            if (asteroid != null)
                Destroy(asteroid);
        }

        spawnedSections.Remove(sectionIndex);
    }

    private void ClearSpawnedAsteroids()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        spawnedSections.Clear();
    }

    private void UpdateAsteroidBehaviors()
    {
        HorizontalDriftAsteroid[] driftingAsteroids = GetComponentsInChildren<HorizontalDriftAsteroid>();
        EnemySpawnPoint[] enemySpawnPoints = GetComponentsInChildren<EnemySpawnPoint>();

        foreach (HorizontalDriftAsteroid drift in driftingAsteroids)
            drift.ManualUpdate();

        foreach (EnemySpawnPoint spawnPoint in enemySpawnPoints)
            spawnPoint.ManualUpdate();
    }

    private class HorizontalDriftAsteroid : MonoBehaviour
    {
        private Vector3 startPosition;
        private Vector3 driftDirection;
        private float driftAmount;
        private float driftSpeed;
        private float offset;

        public void Initialize(float amount, float speed)
        {
            startPosition = transform.position;
            driftAmount = amount;
            driftSpeed = speed;
            offset = Random.Range(0f, 100f);
            driftDirection = Vector3.right;
        }

        public void ManualUpdate()
        {
            float drift = Mathf.Sin(Time.time * driftSpeed + offset) * driftAmount;
            transform.position = startPosition + driftDirection * drift;
        }
    }

    private class EnemySpawnPoint : MonoBehaviour
    {
        private GameObject enemyPrefab;
        private GameObject spawnedEnemy;
        private Transform player;
        private float activationDistance;
        private float despawnDistance;
        private float enemyExtraHeight;
        private bool enemyFacesPlayer;
        private bool fadeOnDespawn;
        private float fadeTime;
        private bool isDespawning;
        private bool hasSpawnedOnce;
        private float aimHeightOffset;
        private float startDelay;

        public void Initialize(
            GameObject prefab,
            Transform playerTransform,
            float distance,
            float extraHeight,
            bool facePlayer,
            bool fade,
            float fadeDuration,
            float targetHeightOffset,
            float enemyDelay
        )
        {
            enemyPrefab = prefab;
            player = playerTransform;
            activationDistance = distance;
            despawnDistance = activationDistance * 2f;
            enemyExtraHeight = extraHeight;
            enemyFacesPlayer = facePlayer;
            fadeOnDespawn = fade;
            fadeTime = fadeDuration;
            aimHeightOffset = targetHeightOffset;
            startDelay = enemyDelay;

            hasSpawnedOnce = false;
            isDespawning = false;
            spawnedEnemy = null;
        }

        public void ManualUpdate()
        {
            if (enemyPrefab == null || player == null || isDespawning)
                return;

            if (Time.timeSinceLevelLoad < startDelay)
                return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (!hasSpawnedOnce && spawnedEnemy == null && distanceToPlayer <= activationDistance)
            {
                SpawnEnemy();
                hasSpawnedOnce = true;
            }

            if (spawnedEnemy != null && enemyFacesPlayer)
                AimEnemyAtPlayer();

            if (spawnedEnemy != null && distanceToPlayer >= despawnDistance)
            {
                if (fadeOnDespawn)
                {
                    EnemyFadeDestroy fade = spawnedEnemy.AddComponent<EnemyFadeDestroy>();
                    fade.BeginFade(fadeTime);
                    isDespawning = true;
                    spawnedEnemy = null;
                }
                else
                {
                    Destroy(spawnedEnemy);
                    spawnedEnemy = null;
                    isDespawning = true;
                }
            }
        }

        private void SpawnEnemy()
        {
            float topHeight = GetTopHeight();
            Vector3 enemyPos = transform.position + Vector3.up * (topHeight + enemyExtraHeight);

            if (player != null && enemyPos.y < player.position.y - 0.5f)
                enemyPos.y = player.position.y - 0.5f;

            Quaternion enemyRot = Quaternion.identity;

            if (enemyFacesPlayer && player != null)
            {
                Vector3 lookDir = GetTargetPosition() - enemyPos;

                if (lookDir.sqrMagnitude > 0.001f)
                    enemyRot = Quaternion.LookRotation(lookDir);
            }

            spawnedEnemy = Instantiate(enemyPrefab, enemyPos, enemyRot, transform);
            spawnedEnemy.transform.localScale = Vector3.one;
            isDespawning = false;
        }

        private void AimEnemyAtPlayer()
        {
            Vector3 lookDir = GetTargetPosition() - spawnedEnemy.transform.position;

            if (lookDir.sqrMagnitude <= 0.001f)
                return;

            spawnedEnemy.transform.rotation = Quaternion.LookRotation(lookDir);
        }

        private Vector3 GetTargetPosition()
        {
            return player.position + Vector3.up * aimHeightOffset;
        }

        private float GetTopHeight()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                return 2f;

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.extents.y;
        }
    }

    private class EnemyFadeDestroy : MonoBehaviour
    {
        private Renderer[] renderers;
        private Material[] materials;
        private float fadeTime;
        private float timer;

        public void BeginFade(float duration)
        {
            fadeTime = Mathf.Max(0.01f, duration);
            renderers = GetComponentsInChildren<Renderer>();

            List<Material> materialList = new List<Material>();

            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat != null)
                    {
                        SetupTransparentMaterial(mat);
                        materialList.Add(mat);
                    }
                }
            }

            materials = materialList.ToArray();

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
                c.enabled = false;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);

            foreach (Material mat in materials)
            {
                if (mat == null)
                    continue;

                if (mat.HasProperty("_BaseColor"))
                {
                    Color color = mat.GetColor("_BaseColor");
                    color.a = alpha;
                    mat.SetColor("_BaseColor", color);
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color color = mat.GetColor("_Color");
                    color.a = alpha;
                    mat.SetColor("_Color", color);
                }
            }

            if (timer >= fadeTime)
                Destroy(gameObject);
        }

        private void SetupTransparentMaterial(Material mat)
        {
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);

            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);

            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}