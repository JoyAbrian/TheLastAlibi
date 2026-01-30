using UnityEngine;
using UnityEngine.UI;

public class MenuOpen : MonoBehaviour
{
    public Button evidenceButton, logButton, profileButton, settingButton;
    public GameObject background, evidenceMenu, logMenu, profileMenu, settingMenu;

    private void Awake()
    {
        evidenceButton.onClick.AddListener(() => ToggleMenu(evidenceMenu));
        logButton.onClick.AddListener(() => ToggleMenu(logMenu));
        profileButton.onClick.AddListener(() => ToggleMenu(profileMenu));
        settingButton.onClick.AddListener(() => ToggleMenu(settingMenu));

        CloseAllMenus(false);
    }

    private void ToggleMenu(GameObject menu)
    {
        if (menu.activeSelf)
        {
            menu.SetActive(false);
            background.SetActive(false);

            SoundManager.PlaySound(SoundType.BookClose, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
        }
        else
        {
            CloseAllMenus(false);

            menu.SetActive(true);
            if (menu == evidenceMenu)
            {
                Debug.Log("Hiding Clue Found Icon");
                GameObject.Find("Suspect Manager").GetComponent<EvidenceManager>().HideClueFoundIcon();
            }
            background.SetActive(true);

            SoundManager.PlaySound(SoundType.BookOpen, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
        }
    }

    public void CloseAllMenus(bool playSound)
    {
        evidenceMenu.SetActive(false);
        logMenu.SetActive(false);
        profileMenu.SetActive(false);
        settingMenu.SetActive(false);
        background.SetActive(false);

        if (playSound)
        {
            SoundManager.PlaySound(SoundType.BookClose, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
        }
    }
}