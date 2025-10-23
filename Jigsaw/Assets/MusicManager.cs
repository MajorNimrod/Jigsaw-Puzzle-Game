using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource musicSource;

    private void Awake()
    {
        // Singleton pattern so you can access from anywhere
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    public void SetVolume(float volume)
    {
        musicSource.volume = volume;
    }

    public void ToggleMute(bool isMuted)
    {
        musicSource.mute = isMuted;
    }

    public float GetVolume()
    {
        return musicSource.volume;
    }

    public bool IsMuted()
    {
        return musicSource.mute;
    }
}
