using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogUIManager : MonoBehaviour
{
    public TMP_InputField playerInput;
    public GameObject loadingText;
    public TextMeshProUGUI npcText;
    public TypewriterText typewriter;

    [Header("OTHER UI")]
    public Image npcFace;
    public Image playerFace;
    public GameObject playerName, npcName;

    private bool isShowingResponse = false;

    private void Start()
    {
        ChangeFace("neutral");

        typewriter.uiText = npcText;
        playerInput.onSubmit.AddListener(_ => OnSend());
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
        playerInput.text = "";
        playerInput.gameObject.SetActive(true);
        playerInput.ActivateInputField();
        loadingText.SetActive(false);
    }

    void HideAll()
    {
        playerName.SetActive(false);
        npcName.SetActive(false);

        playerInput.gameObject.SetActive(false);
        loadingText.SetActive(false);
        npcText.text = "";
    }

    void OnSend()
    {
        if (string.IsNullOrWhiteSpace(playerInput.text) || typewriter.IsTyping()) 
            return;

        string question = playerInput.text.Trim();
        StartCoroutine(AskQuestion(question));
    }

    IEnumerator AskQuestion(string question)
    {
        HideAll();
        loadingText.SetActive(true);

        string npcResponse = "";
        bool isDone = false;

        FindObjectOfType<SuspectAIManager>().SendPlayerQuestion(question, entry =>
        {
            npcName.SetActive(true);
            npcResponse = entry.response;
            ChangeFace(entry.expression);
            isDone = true;
        });

        while (!isDone) yield return null;

        loadingText.SetActive(false);
        npcText.gameObject.SetActive(true);
        typewriter.StartTyping(npcResponse);
        isShowingResponse = true;
    }

    private void ChangeFace(string expression)
    {
        if (expression == "angry")
        {
            npcFace.sprite = SuspectManager.SuspectSingleton.angrySprite;
        }
        else if (expression == "concerned")
        {
            npcFace.sprite = SuspectManager.SuspectSingleton.concernedSprite;
        }
        else if (expression == "happy")
        {
            npcFace.sprite = SuspectManager.SuspectSingleton.happySprite;
        }
        else if (expression == "neutral")
        {
            npcFace.sprite = SuspectManager.SuspectSingleton.neutralSprite;
        }
        else if (expression == "smile")
        {
            npcFace.sprite = SuspectManager.SuspectSingleton.smileSprite;
        }
    } 
}