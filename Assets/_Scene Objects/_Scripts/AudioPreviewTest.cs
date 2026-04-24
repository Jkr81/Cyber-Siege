using UnityEngine;

public class AudioPreviewTest : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    void Start()
    {
        if (source != null)
            source.Play();
    }
}