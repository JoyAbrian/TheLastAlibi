using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public Slider masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider;
    public Button restartButton, quitButton;
    public GameObject restartConfirmation, quitConfirmation;

    void Start()
    {
        masterVolumeSlider.value = GlobalVariables.MASTER_VOLUME;
        musicVolumeSlider.value = GlobalVariables.MUSIC_VOLUME;
        sfxVolumeSlider.value = GlobalVariables.SOUND_EFFECTS_VOLUME;

        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMasterVolume(float value)
    {
        GlobalVariables.MASTER_VOLUME = value;
        AudioListener.volume = value;
    }

    public void SetMusicVolume(float value)
    {
        GlobalVariables.MUSIC_VOLUME = value;
    }

    public void SetSFXVolume(float value)
    {
        GlobalVariables.SOUND_EFFECTS_VOLUME = value;
    }

    public void ShowRestartConfirmation()
    {
        restartConfirmation.SetActive(true);
        gameObject.SetActive(false);
    }

    public void ShowQuitConfirmation()
    {
        quitConfirmation.SetActive(true);
        gameObject.SetActive(false);
    }
}
