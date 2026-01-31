using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image flagDisplay;
    [SerializeField] private TextMeshProUGUI languageNameText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Language Data")]
    [SerializeField] public List<LanguageOption> languageList;

    private int currentIndex = 0;

    [System.Serializable]
    public class LanguageOption
    {
        public string languageName;
        public Sprite flagSprite;
        public string languageCode;
    }

    private void Start()
    {
        if (languageList == null || languageList.Count == 0)
        {
            Debug.LogError("List Bahasa Kosong! Tolong isi di Inspector.");
            return;
        }

        prevButton.onClick.AddListener(OnPrevClick);
        nextButton.onClick.AddListener(OnNextClick);

        UpdateUI();
    }

    private void OnPrevClick()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateUI();
        }
    }

    private void OnNextClick()
    {
        if (currentIndex < languageList.Count - 1)
        {
            currentIndex++;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        LanguageOption currentLang = languageList[currentIndex];
        flagDisplay.sprite = currentLang.flagSprite;
        languageNameText.text = currentLang.languageName;

        if (currentIndex <= 0)
        {
            SetButtonState(prevButton, false);
        }
        else
        {
            SetButtonState(prevButton, true);
        }

        if (currentIndex >= languageList.Count - 1)
        {
            SetButtonState(nextButton, false);
        }
        else
        {
            SetButtonState(nextButton, true);
        }

        GlobalVariables.LANGUAGE_SETTINGS = currentLang.languageCode;
    }

    private void SetButtonState(Button btn, bool isActive)
    {
        btn.interactable = isActive;

        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null)
        {
            Color color = btnImage.color;

            color.a = isActive ? 1f : 0.5f;
            btnImage.color = color;
        }
    }
}