using UnityEngine;
using System.Text.RegularExpressions;

namespace Asteroids
{
    public class RND_sliders_random : MonoBehaviour
    {
        void Start()
        {
            Renderer renderer = GetComponent<Renderer>();
            Material material = renderer.sharedMaterial;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);

            Regex rangeRegex = new Regex(@"_RND_(-?\d+(\.\d+)?)_(-?\d+(\.\d+)?)");

            for (int i = 0; i < material.shader.GetPropertyCount(); i++)
            {
                if (material.shader.GetPropertyName(i).Contains("_RND"))
                {
                    string sliderName = material.shader.GetPropertyName(i);
                    Match match = rangeRegex.Match(sliderName);

                    if (match.Success && match.Groups.Count >= 3)
                    {
                        float minValue = float.Parse(match.Groups[1].Value);
                        float maxValue = float.Parse(match.Groups[3].Value);
                        float randomValue = UnityEngine.Random.Range(minValue, maxValue);
                        mpb.SetFloat(sliderName, randomValue);
                    }
                    else
                    {
                        Debug.LogWarning($"Property {sliderName} does not contain valid range parameters.");
                    }
                }
            }

            renderer.SetPropertyBlock(mpb);
        }
    }
}