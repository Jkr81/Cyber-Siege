using UnityEngine;
using System.Collections;

namespace HyperspaceFX2
{
    public class HSEffectCycler : MonoBehaviour
    {
        [Header("Single Effect")]
        [SerializeField] private GameObject hyperspaceEffectPrefab;

        [Header("Timing")]
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private float effectDuration = 2f;

        [Header("Options")]
        [SerializeField] private bool disableLights = false;
        [SerializeField] private bool disableSound = false;
        [SerializeField] private bool destroyAfterPlaying = true;

        private GameObject spawnedEffect;
        private bool hasPlayed = false;

        private void OnEnable()
        {
            hasPlayed = false;
            StartCoroutine(PlaySingleEffect());
        }

        private IEnumerator PlaySingleEffect()
        {
            if (hasPlayed) yield break;
            hasPlayed = true;

            yield return new WaitForSeconds(startDelay);

            if (hyperspaceEffectPrefab == null)
            {
                Debug.LogWarning("No hyperspace effect prefab assigned.");
                yield break;
            }

            spawnedEffect = Instantiate(
                hyperspaceEffectPrefab,
                transform.position,
                transform.rotation
            );

            if (disableLights)
            {
                Light[] lights = spawnedEffect.GetComponentsInChildren<Light>();
                foreach (Light light in lights)
                    light.enabled = false;
            }

            if (disableSound)
            {
                AudioSource[] sounds = spawnedEffect.GetComponentsInChildren<AudioSource>();
                foreach (AudioSource sound in sounds)
                    sound.enabled = false;
            }

            yield return new WaitForSeconds(effectDuration);

            if (destroyAfterPlaying && spawnedEffect != null)
                Destroy(spawnedEffect);
        }
    }
}