using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class SuspectAIManager : MonoBehaviour
{
    [Header("Gemini API Key JSON")]
    public TextAsset apiKeyFile;

    public static SuspectProfile GeneratedProfile;
    private string apiKey;
    private string model = "gemini-2.0-flash";
    private string history = "";

    [HideInInspector] public SuspectProfile currentProfile;
    public SuspectInterrogationLog interrogationLog = new();

    [Serializable] private class APIKeyWrapper { public string key; }

    private void Awake() => LoadAPIKey();

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

    // ========== 1. Generate Profile ==========
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
            - a 2–4 very short paragraph backstory (each paragraph separated by '\\n')
            - 1–3 very short clues hidden in the story (each clue separated by '\\n')

            Return plain text like:
            <BEGIN>
                name|personality|backstory paragraph 1\\nparagraph 2\\n...|clue 1\\nclue 2\\nclue 3
            <END>

            Don't return markdown or explanation. Just raw delimited string.
        ";

        int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            bool success = false;

            yield return SendPromptToGemini(prompt, rawText =>
            {
                try
                {
                    var profile = ParseDelimitedSuspectProfile(rawText);
                    currentProfile = profile;
                    GeneratedProfile = profile;

                    history = $"Backstory:\n{string.Join("\n", profile.backstory)}\nClues:\n- {string.Join("\n- ", profile.evidence)}\n";
                    onGenerated?.Invoke(profile);
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Attempt {attempt + 1} failed: {e.Message}");
                }
            });

            if (success) yield break;
            yield return new WaitForSeconds(1f);
        }

        Debug.LogError("❌ Failed to generate valid suspect after 3 attempts.");
    }

    private SuspectProfile ParseDelimitedSuspectProfile(string raw)
    {
        raw = System.Text.RegularExpressions.Regex.Unescape(raw);

        Match match = Regex.Match(raw, "<BEGIN>(.*?)<END>", RegexOptions.Singleline);
        if (!match.Success)
            throw new Exception("No valid content between <BEGIN> and <END>");

        string delimited = match.Groups[1].Value.Trim();

        string[] parts = delimited.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogError("🛑 Delimited response was too short:\n" + delimited);
            throw new Exception("Incomplete delimited profile. Expected 4 parts: name, personality, backstory, evidence");
        }

        List<string> CleanSplit(string input)
        {
            return input.Split(new[] { "\\n" }, StringSplitOptions.None)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList();
        }

        return new SuspectProfile
        {
            name = parts[0].Trim(),
            personality = parts[1].Trim(),
            backstory = CleanSplit(parts[2]),
            evidence = CleanSplit(parts[3])
        };
    }

    // ========== 2. Interrogation ==========
    public void SendPlayerQuestion(string question, Action<SuspectInterrogationEntry> onResponse)
    {
        StartCoroutine(SendInterrogationRoutine(question, onResponse));
    }

    private IEnumerator SendInterrogationRoutine(string playerQuestion, Action<SuspectInterrogationEntry> onComplete)
    {
        string prompt = $@"
            You are a suspect named {GlobalVariables.CURRENT_SUSPECT_NAME} with a {GlobalVariables.CURRENT_SUSPECT_PERSONALITY} personality.

            Answer in character as a murder suspect. Be natural and react based on your backstory and clues.

            Difficulty: {GlobalVariables.GAME_DIFFICULTY}
            - Easy: You might reveal clues even accidentally.
            - Normal: You might slip under pressure.
            - Hard: Avoid giving clues unless heavily pressed.

            Conversation so far:
            {history}
            Player: {playerQuestion}

            Return delimited text like:
            response|expression|clue (or null)

            Don't return markdown or explanation. Just raw line.
        ";

        history += $"\nPlayer: {playerQuestion}\n";

        yield return SendPromptToGemini(prompt, raw =>
        {
            try
            {
                string[] parts = raw.Split('|');
                if (parts.Length < 3) throw new Exception("Incomplete interrogation response");

                SuspectInterrogationEntry entry = new()
                {
                    playerQuestion = playerQuestion,
                    response = parts[0].Trim(),
                    expression = parts[1].Trim(),
                    clue = parts[2].Trim() == "null" ? null : parts[2].Trim()
                };

                history += $"Suspect: {entry.response}\n";
                interrogationLog.AddEntry(entry);
                onComplete?.Invoke(entry);

                if (!string.IsNullOrEmpty(entry.clue) && entry.clue != "null")
                {
                    FindObjectOfType<EvidenceManager>()?.AddEvidence(entry.clue);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Failed to parse interrogation reply: " + e.Message);
            }
        });
    }

    // ========== 3. Gemini Communication ==========
    private IEnumerator SendPromptToGemini(string prompt, Action<string> onResponseText)
    {
        string requestBody = $@"{{
            ""contents"": [
            {{
                ""parts"": [{{ ""text"": {JsonEscape(history + "\n" + prompt)} }}]
            }}
            ]
        }}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        UnityWebRequest req = new UnityWebRequest(
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}",
            "POST"
        );

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gemini API Error: " + req.error);
            yield break;
        }

        string responseText = req.downloadHandler.text;
        string extracted = ExtractDelimitedResponse(responseText);

        if (string.IsNullOrEmpty(extracted))
        {
            Debug.LogError("❌ Failed to extract delimited text from Gemini response.");
            yield break;
        }

        Debug.Log("📦 Raw Delimited:\n" + extracted);
        onResponseText?.Invoke(extracted);
    }

    private string ExtractDelimitedResponse(string responseText)
    {
        var match = Regex.Match(responseText, "\"text\"\\s*:\\s*\"(.*?)\"", RegexOptions.Singleline);
        if (!match.Success) return null;

        string raw = match.Groups[1].Value;
        raw = raw.Replace("\\n", "\\n").Replace("\\\"", "\"").Replace("\\\\", "\\").Trim();
        return raw;
    }

    private string JsonEscape(string raw)
    {
        return "\"" + raw.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
    }
}