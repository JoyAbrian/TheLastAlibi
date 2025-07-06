using UnityEngine;

public class AmbienceManager : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.loop = true;
    }

    private void Update()
    {
        audioSource.volume = GlobalVariables.MUSIC_VOLUME;
    }
}