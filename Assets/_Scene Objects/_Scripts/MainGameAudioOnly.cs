using UnityEngine;

public class MainGameAudioOnly : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.Stop();
    }

    public void PlayAudio()
    {
        Debug.Log("MAIN GAME AUDIO PLAY CALLED");

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    public void StopAudio()
    {
        audioSource.Stop();
    }
}