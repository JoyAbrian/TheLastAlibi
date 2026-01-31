using UnityEngine;
using UnityEngine.UI;

public class MenuSettings : MonoBehaviour
{
    public Slider masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider;
    public Button backButton;
    public GameObject mainMenu, settingsMenu;

    void Start()
    {
        masterVolumeSlider.value = GlobalVariables.MASTER_VOLUME;
        musicVolumeSlider.value = GlobalVariables.MUSIC_VOLUME;
        sfxVolumeSlider.value = GlobalVariables.SOUND_EFFECTS_VOLUME;
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        backButton.onClick.AddListener(BackToMainMenu);
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

    public void BackToMainMenu()
    {
        settingsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
