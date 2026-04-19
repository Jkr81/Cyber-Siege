using System.Collections.Generic;
using UnityEngine;

namespace Asteroids
{
    public class AsteroidBeltSpawner : MonoBehaviour
    {
        [Header("Center")]
        [Tooltip("The object the asteroid belt forms around.")]
        public Transform beltCenter;

        [Header("Asteroid Prefab")]
        [Tooltip("Asteroid prefab to spawn.")]
        public GameObject asteroidPrefab;

        [Header("Asteroid Count")]
        [Tooltip("Minimum number of asteroids to spawn.")]
        public int minAsteroidsCount = 250;

        [Tooltip("Maximum number of asteroids to spawn.")]
        public int maxAsteroidsCount = 450;

        [Header("Belt Radius")]
        [Tooltip("Inner radius of the asteroid belt.")]
        public float minOrbitRadius = 40f;

        [Tooltip("Outer radius of the asteroid belt.")]
        public float maxOrbitRadius = 80f;

        [Header("Height")]
        [Tooltip("How tall the asteroid belt can be vertically.")]
        public float verticalThickness = 8f;

        [Header("Asteroid Scale")]
        [Tooltip("Minimum asteroid size.")]
        public float minSize = 0.75f;

        [Tooltip("Maximum asteroid size.")]
        public float maxSize = 2.5f;

        [Header("Spacing")]
        [Tooltip("Minimum extra space between asteroids.")]
        public float spaceBetween = 3f;

        [Header("Distribution")]
        [Tooltip("Optional curve for horizontal belt density.")]
        public AnimationCurve horizontalSpreadCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip("Optional curve for vertical belt density.")]
        public AnimationCurve verticalSpreadCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Header("Parent")]
        [Tooltip("Optional parent for all spawned asteroids.")]
        public Transform asteroidParent;

        private readonly List<Vector4> asteroidPositionsAndSizes = new List<Vector4>();

        void Start()
        {
            SpawnAsteroids();
        }

        void SpawnAsteroids()
        {
            if (beltCenter == null)
            {
                Debug.LogError("AsteroidBeltSpawner: No beltCenter assigned.");
                return;
            }

            if (asteroidPrefab == null)
            {
                Debug.LogError("AsteroidBeltSpawner: No asteroidPrefab assigned.");
                return;
            }

            asteroidPositionsAndSizes.Clear();

            int targetAsteroidCount = Random.Range(minAsteroidsCount, maxAsteroidsCount + 1);
            int currentAsteroidsCount = 0;
            int attempts = targetAsteroidCount * 20; // prevents infinite loops

            for (int i = 0; i < attempts && currentAsteroidsCount < targetAsteroidCount; i++)
            {
                float normalizedAngle = Random.Range(0f, 1f);
                float angle = normalizedAngle * 360f;

                float horizontalDensity = horizontalSpreadCurve.Evaluate(normalizedAngle);
                float verticalDensity = verticalSpreadCurve.Evaluate(normalizedAngle);

                float orbitRadius = Random.Range(minOrbitRadius, maxOrbitRadius);

                float horizontalOffset = Random.Range(
                    -horizontalDensity * 5f,
                     horizontalDensity * 5f
                );

                Vector3 orbitDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                Vector3 orbitPosition = orbitDirection * (orbitRadius + horizontalOffset);

                float verticalOffset = Random.Range(
                    -verticalThickness * 0.5f * Mathf.Max(0.1f, verticalDensity),
                     verticalThickness * 0.5f * Mathf.Max(0.1f, verticalDensity)
                );

                Vector3 finalPosition = beltCenter.position + orbitPosition + new Vector3(0f, verticalOffset, 0f);

                float asteroidSize = Random.Range(minSize, maxSize);

                if (!IsPositionValidWithSize(finalPosition, asteroidSize))
                    continue;

                asteroidPositionsAndSizes.Add(new Vector4(
                    finalPosition.x,
                    finalPosition.y,
                    finalPosition.z,
                    asteroidSize
                ));

                SpawnAsteroid(finalPosition, asteroidSize);
                currentAsteroidsCount++;
            }

            Debug.Log("Spawned " + currentAsteroidsCount + " asteroids.");
        }

        bool IsPositionValidWithSize(Vector3 newPosition, float newSize)
        {
            foreach (Vector4 asteroidData in asteroidPositionsAndSizes)
            {
                Vector3 existingPosition = new Vector3(asteroidData.x, asteroidData.y, asteroidData.z);
                float existingSize = asteroidData.w;

                float distanceBetweenAsteroids = Vector3.Distance(newPosition, existingPosition);
                float requiredDistance = (existingSize * 0.5f) + (newSize * 0.5f) + spaceBetween;

                if (distanceBetweenAsteroids < requiredDistance)
                    return false;
            }

            return true;
        }

        void SpawnAsteroid(Vector3 position, float size)
        {
            GameObject asteroid = Instantiate(asteroidPrefab, position, Random.rotation);

            if (asteroidParent != null)
                asteroid.transform.SetParent(asteroidParent);

            asteroid.transform.localScale = Vector3.one * size;
            asteroid.SetActive(true);
        }
    }
}