using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class AIManager : MonoBehaviour
{
    private string apiKey;

    [Header("API Key File")]
    public TextAsset apiKeyFile;

    [Header("UI Elements")]
    public TMP_InputField playerInput;
    public TMP_Text suspectOutputText;
    public TMP_Text clueOutputText;

    [System.Serializable]
    public class APIKeyWrapper
    {
        public string key;
    }

    private void Awake()
    {
        LoadAPIKey();
    }

    private void LoadAPIKey()
    {
        if (apiKeyFile != null)
        {
            var json = JsonUtility.FromJson<APIKeyWrapper>(apiKeyFile.text);
            apiKey = json.key;
        }
        else
        {
            Debug.LogError("API key TextAsset not assigned in Inspector.");
        }
    }

    public void OnAskButtonClicked()
    {
        string playerQuestion = playerInput.text;
        StartCoroutine(GetSuspectResponse(playerQuestion));
    }

    IEnumerator GetSuspectResponse(string playerQuestion)
    {
        string suspectPrompt = $"You are a murder suspect named John Doe. You are being interrogated. Respond to this question in character.\nPlayer: {playerQuestion}\nYou:";
        GeminiRequest req = CreateGeminiRequest(suspectPrompt);

        yield return SendToGeminiAPI(req, response =>
        {
            string suspectReply = response.candidates[0].content.parts[0].text;
            suspectOutputText.text = suspectReply;

            StartCoroutine(GetClueAnalysis(suspectReply));
        });
    }

    IEnumerator GetClueAnalysis(string suspectReply)
    {
        string cluePrompt = $"You are a forensic assistant analyzing interrogation. Based on this statement:\n\"{suspectReply}\"\nGenerate 1 short clue or observation. Respond with 'None' if there's nothing useful.";
        GeminiRequest req = CreateGeminiRequest(cluePrompt);

        yield return SendToGeminiAPI(req, response =>
        {
            string clue = response.candidates[0].content.parts[0].text;
            if (clue.ToLower() != "none")
            {
                clueOutputText.text += "\n• " + clue.Trim();
            }
        });
    }

    private GeminiRequest CreateGeminiRequest(string prompt)
    {
        return new GeminiRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    parts = new[]
                    {
                        new GeminiMessagePart { text = prompt }
                    }
                }
            }
        };
    }

    IEnumerator SendToGeminiAPI(GeminiRequest request, System.Action<GeminiResponse> onSuccess)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro-latest:generateContent";

        string jsonBody = JsonUtility.ToJson(request);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gemini API Error: " + req.error);
        }
        else
        {
            GeminiResponse result = JsonUtility.FromJson<GeminiResponse>(req.downloadHandler.text);
            onSuccess?.Invoke(result);
        }
    }
}