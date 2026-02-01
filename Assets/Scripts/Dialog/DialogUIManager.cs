using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogUIManager : MonoBehaviour
{
    [Header("INPUT UI")]
    public TMP_InputField playerInput;
    public GameObject loadingText;

    [Header("OUTPUT UI")]
    public TextMeshProUGUI npcText;
    public TypewriterText typewriter;

    [Header("FACES & NAMES")]
    public Image npcFace;
    public Image playerFace;
    public GameObject playerName, npcName;

    [Header("SUGGESTION SYSTEM")]
    public Button toggleSuggestionButton;

    public GameObject suggestionPanel;
    public Transform suggestionContainerParent;
    public SuggestionTextUI suggestionPrefab;

    private bool isShowingResponse = false;
    private SuspectAIManager suspectManager;
    private List<string> currentSuggestions = new List<string>();

    private void Start()
    {
        suspectManager = FindObjectOfType<SuspectAIManager>();

        ChangeFace("neutral");

        typewriter.uiText = npcText;
        playerInput.onSubmit.AddListener(_ => OnSend());

        if (toggleSuggestionButton != null)
        {
            toggleSuggestionButton.onClick.AddListener(ToggleSuggestionMenu);
        }

        ShowPlayerInput();
    }

    void Update()
    {
        if (isShowingResponse && typewriter.IsTypingFinished && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            isShowingResponse = false;
            ShowPlayerInput();
            return;
        }

        if (!isShowingResponse && playerInput.gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Return))
        {
            OnSend();
        }
    }

    void ShowPlayerInput()
    {
        playerName.SetActive(true);
        npcName.SetActive(false);

        npcText.text = "";
        loadingText.SetActive(false);

        playerInput.text = "";
        playerInput.gameObject.SetActive(true);
        playerInput.ActivateInputField();

        if (suggestionPanel != null) suggestionPanel.SetActive(false);

        if (suspectManager != null)
        {
            suspectManager.GenerateSuggestedQuestions(OnSuggestionsReceived);
        }
    }

    public void ToggleSuggestionMenu()
    {
        if (isShowingResponse || loadingText.activeSelf) return;

        bool isSuggestionOpen = suggestionPanel.activeSelf;

        if (isSuggestionOpen)
        {
            suggestionPanel.SetActive(false);
            playerInput.gameObject.SetActive(true);
            playerInput.ActivateInputField();
        }
        else
        {
            playerInput.gameObject.SetActive(false);
            suggestionPanel.SetActive(true);
        }
    }

    void HideAllInputs()
    {
        playerName.SetActive(false);
        npcName.SetActive(false);

        playerInput.gameObject.SetActive(false);
        if (suggestionPanel != null) suggestionPanel.SetActive(false);

        npcText.text = "";
    }

    private void ClearSuggestionsUI()
    {
        foreach (Transform child in suggestionContainerParent)
        {
            Destroy(child.gameObject);
        }
        currentSuggestions.Clear();
    }

    private void OnSuggestionsReceived(List<string> suggestions)
    {
        ClearSuggestionsUI();

        currentSuggestions = suggestions;

        if (suggestions == null || suggestions.Count == 0) return;

        foreach (string text in suggestions)
        {
            SuggestionTextUI newBtn = Instantiate(suggestionPrefab, suggestionContainerParent);
            newBtn.suggestionText.text = text;

            newBtn.suggestionButton.onClick.AddListener(() => OnSuggestionClicked(text));
        }
    }

    private void OnSuggestionClicked(string questionText)
    {
        playerInput.text = questionText;
        suggestionPanel.SetActive(false);

        ClearSuggestionsUI();
        OnSend();
    }

    void OnSend()
    {
        if (string.IsNullOrWhiteSpace(playerInput.text) || typewriter.IsTyping())
            return;
        ClearSuggestionsUI();

        string question = playerInput.text.Trim();
        StartCoroutine(AskQuestion(question));
    }

    IEnumerator AskQuestion(string question)
    {
        HideAllInputs();
        loadingText.SetActive(true);

        string npcResponse = "";
        string expression = "neutral";
        bool isDone = false;

        suspectManager.SendPlayerQuestion(question, entry =>
        {
            if (entry != null)
            {
                npcResponse = entry.response;
                expression = entry.expression;
            }
            else
            {
                npcResponse = "...";
            }
            isDone = true;
        });

        while (!isDone) yield return null;

        loadingText.SetActive(false);

        npcName.SetActive(true);
        ChangeFace(expression);

        npcText.gameObject.SetActive(true);
        typewriter.StartTyping(npcResponse);

        isShowingResponse = true;
    }

    private void ChangeFace(string expression)
    {
        if (SuspectManager.SuspectSingleton == null) return;

        if (expression == "angry") npcFace.sprite = SuspectManager.SuspectSingleton.angrySprite;
        else if (expression == "concerned") npcFace.sprite = SuspectManager.SuspectSingleton.concernedSprite;
        else if (expression == "happy") npcFace.sprite = SuspectManager.SuspectSingleton.happySprite;
        else if (expression == "neutral") npcFace.sprite = SuspectManager.SuspectSingleton.neutralSprite;
        else if (expression == "smile") npcFace.sprite = SuspectManager.SuspectSingleton.smileSprite;
    }
}