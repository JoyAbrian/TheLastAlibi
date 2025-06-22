using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class SuspectAIManager : MonoBehaviour
{
    [Header("Gemini API Key JSON")]
    public TextAsset apiKeyFile;

    [HideInInspector] public SuspectProfile currentProfile;
    public SuspectInterrogationLog interrogationLog = new();

    private string apiKey;

    [Serializable]
    private class APIKeyWrapper
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
            Debug.LogError("Missing Gemini API key file.");
        }
    }

    // =========================
    // 1. Generate Suspect Profile
    // =========================
    public void GenerateSuspectProfile(Action<SuspectProfile> onGenerated)
    {
        StartCoroutine(GenerateSuspectRoutine(onGenerated));
    }

    private IEnumerator GenerateSuspectRoutine(Action<SuspectProfile> onGenerated)
    {
        string prompt = $@"
            Generate a fictional murder suspect with:
            - name: {GlobalVariables.CURRENT_SUSPECT_NAME}
            - personality: {GlobalVariables.CURRENT_SUSPECT_PERSONALITY}
            - a 2-4 paragraph backstory
            - 1 to 3 clues hidden in the story depending on difficulty

            Return JSON like:
            {{
              ""name"": ""<name>"",
              ""personality"": ""<personality>"",
              ""backstory"": [""..."", ""..."", ""...""],
              ""evidence"": [""..."", ""...""]
            }}

            Difficulty: {GlobalVariables.GAME_DIFFICULTY}
        ";

        yield return SendPromptToGemini(prompt, json =>
        {
            SuspectProfile profile = JsonUtility.FromJson<SuspectProfile>(json);
            currentProfile = profile;
            onGenerated?.Invoke(profile);
        });
    }

    // =========================
    // 2. Interrogation Dialogue
    // =========================
    public void SendPlayerQuestion(string playerQuestion, Action<SuspectInterrogationEntry> onResponse)
    {
        StartCoroutine(SendInterrogationRoutine(playerQuestion, onResponse));
    }

    private IEnumerator SendInterrogationRoutine(string playerQuestion, Action<SuspectInterrogationEntry> onComplete)
    {
        string difficulty = GlobalVariables.GAME_DIFFICULTY;
        string backstory = string.Join("\n", currentProfile.backstory);
        string evidence = string.Join("\n- ", currentProfile.evidence);

        StringBuilder conversationHistory = new();
        foreach (var entry in interrogationLog.entries)
        {
            conversationHistory.AppendLine($"Player: {entry.playerQuestion}");
            conversationHistory.AppendLine($"Suspect: {entry.response}");
        }

        string prompt = $@"
            You are a suspect named {GlobalVariables.CURRENT_SUSPECT_NAME}. Your personality is {GlobalVariables.CURRENT_SUSPECT_PERSONALITY}.

            ### Backstory:
            {backstory}

            ### Known facts (clues):
            - {evidence}

            ### Previous conversation:
            {conversationHistory.ToString().Trim()}

            Now answer the next question **in character**. Only return JSON with these keys:
            {{
              ""response"": ""<your reply>"",
              ""expression"": ""<Angry|Concerned|Happy|Neutral|Smile>"",
              ""clue"": ""<optional clue from above or null>""
            }}

            ### Difficulty: {difficulty}
            - Easy: You might reveal clues easily, even accidentally.
            - Normal: You are cautious, but might slip up if pressed.
            - Hard: Avoid giving clues unless cornered logically.

            Player: {playerQuestion}
        ";

        yield return SendPromptToGemini(prompt, json =>
        {
            SuspectInterrogationEntry entry = JsonUtility.FromJson<SuspectInterrogationEntry>(json);
            entry.playerQuestion = playerQuestion;

            interrogationLog.AddEntry(entry);
            onComplete?.Invoke(entry);
        });
    }

    // =========================
    // Gemini Communication
    // =========================
    private IEnumerator SendPromptToGemini(string prompt, Action<string> onJsonExtracted)
    {
        var body = new
        {
            contents = new[]
            {
                new {
                    parts = new[] {
                        new { text = prompt.Trim() }
                    }
                }
            }
        };

        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest req = new UnityWebRequest(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro-latest:generateContent",
            "POST"
        );

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gemini API Error: " + req.error);
            yield break;
        }

        string responseText = req.downloadHandler.text;
        string extractedJson = ExtractJSONFromResponse(responseText);

        if (string.IsNullOrEmpty(extractedJson))
        {
            Debug.LogError("Failed to parse Gemini JSON.");
            yield break;
        }

        onJsonExtracted?.Invoke(extractedJson);
    }

    private string ExtractJSONFromResponse(string responseText)
    {
        string marker = "\"parts\":[{\"text\":\"";
        int start = responseText.IndexOf(marker);
        if (start == -1) return null;
        start += marker.Length;

        int end = responseText.IndexOf("\"}],", start);
        if (end == -1) end = responseText.IndexOf("\"}]", start);
        if (end == -1) return null;

        string json = responseText.Substring(start, end - start);
        json = Regex.Unescape(json);
        json = json.Trim().Trim('`');

        return json;
    }
}