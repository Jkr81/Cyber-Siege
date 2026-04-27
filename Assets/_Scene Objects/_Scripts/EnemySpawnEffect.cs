using UnityEngine;

public class EnemySpawnEffect : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float effectDuration = 1.2f;

    [Header("Hologram Look")]
    [SerializeField] private Color hologramColor = Color.cyan;
    [SerializeField] private float emissionStrength = 3f;
    [SerializeField] private float flickerSpeed = 20f;

    [Header("Optional Scale Pop")]
    [SerializeField] private bool useScalePop = true;
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float startScaleMultiplier = 0.7f;

    private Renderer[] renderers;
    private Material[] materials;
    private Color[] originalEmission;

    private Vector3 originalScale;
    private float timer = 0f;

    void Start()
    {
        originalScale = transform.localScale;

        // Cache materials safely
        renderers = GetComponentsInChildren<Renderer>();

        int count = 0;
        foreach (Renderer r in renderers)
            count += r.materials.Length;

        materials = new Material[count];
        originalEmission = new Color[count];

        int index = 0;

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                materials[index] = mat;

                if (mat != null && mat.HasProperty("_EmissionColor"))
                {
                    originalEmission[index] = mat.GetColor("_EmissionColor");
                    mat.EnableKeyword("_EMISSION");
                }
                else
                {
                    originalEmission[index] = Color.black;
                }

                index++;
            }
        }

        // Scale pop start
        if (useScalePop)
            transform.localScale = originalScale * startScaleMultiplier;
    }

    void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / effectDuration);
        bool spawning = t < 1f;

        // 🔵 Hologram flicker (EMISSION ONLY — safe)
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null || !mat.HasProperty("_EmissionColor")) continue;

            if (spawning)
            {
                float flicker = Mathf.Abs(Mathf.Sin(Time.time * flickerSpeed));

                mat.SetColor(
                    "_EmissionColor",
                    hologramColor * emissionStrength * flicker
                );
            }
            else
            {
                mat.SetColor("_EmissionColor", originalEmission[i]);
            }
        }

        // 📏 Scale pop
        if (useScalePop)
        {
            float popT = Mathf.Clamp01(timer / popDuration);

            transform.localScale = Vector3.Lerp(
                originalScale * startScaleMultiplier,
                originalScale,
                popT
            );
        }

        // 🧹 Done → stop updating
        if (!spawning)
            enabled = false;
    }
}