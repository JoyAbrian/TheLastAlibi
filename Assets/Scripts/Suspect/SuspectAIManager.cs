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

    [HideInInspector] public SuspectProfile currentProfile;
    public SuspectInterrogationLog interrogationLog = new();

    private string systemPrompt = "";
    private string conversationHistory = "";
    private int dialogueCount = 0;
    private int pressureLevel = 0;

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

    // =================================================================================
    // 1. GENERATE PROFILE
    // =================================================================================
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
            - guilt_status: {(GlobalVariables.IS_SUSPECT_GUILTY ? "Guilty" : "Not Guilty")}
            - a 2-4 paragraph backstory that subtly ties them to the crime. (separated by '\\n') 
                * If guilty: contain inconsistencies.
                * If innocent: contain harmless events that look suspicious.
                * Make it ambiguous. Never explicitly say 'I did it' or 'I am innocent' in the backstory text itself.
            - After the backstory:
                * Generate 3-5 specific clues/evidence items related to them. (separated by '\\n')
        
            Return plain text like:
            <BEGIN>
            $ActualName|$ActualPersonality|$guilt_status|backstory paragraph 1\\paragraph 2|clue 1\\clue 2
            <END>

            Don't return markdown. Just raw delimited string.
        ";

        bool success = false;
        int maxAttempts = 5;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            yield return SendPromptToGemini(prompt, "", rawText =>
            {
                try
                {
                    var profile = ParseDelimitedSuspectProfile(rawText);
                    currentProfile = profile;
                    GeneratedProfile = profile;

                    systemPrompt = $@"
                    You are {profile.name}, a {profile.personality} character.
                    Status: {(GlobalVariables.IS_SUSPECT_GUILTY ? "GUILTY" : "INNOCENT")}.
                    
                    BACKSTORY:
                    {string.Join("\n", profile.backstory)}

                    KNOWN EVIDENCE (Only reveal if pressured):
                    {string.Join("\n", profile.evidence)}

                    INTERROGATION RULES:
                    1. If GUILTY: Lie, deflect, and hide the truth. Only reveal clues if 'Pressure Level' is high.
                    2. If INNOCENT: You are nervous. You tell the truth but might omit details out of fear.
                    3. Stay in character. Keep answers short (1-2 sentences).
                    ";

                    conversationHistory = "";
                    pressureLevel = 0;

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
        Debug.LogError("❌ Failed to generate suspect.");
    }

    private SuspectProfile ParseDelimitedSuspectProfile(string raw)
    {
        raw = Regex.Unescape(raw);
        Match match = Regex.Match(raw, "<BEGIN>(.*?)<END>", RegexOptions.Singleline);
        if (!match.Success) throw new Exception("No valid content between <BEGIN> and <END>");

        string delimited = match.Groups[1].Value.Trim();
        string[] parts = delimited.Split('|');
        if (parts.Length < 5) throw new Exception("Incomplete profile data.");

        List<string> CleanSplit(string input)
        {
            return input.Replace("\\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        }

        return new SuspectProfile
        {
            name = parts[0].Trim(),
            personality = parts[1].Trim(),
            backstory = CleanSplit(parts[3]),
            evidence = CleanSplit(parts[4])
        };
    }

    // =================================================================================
    // 2. INTERROGATION (WITH RAG & DIFFICULTY)
    // =================================================================================
    public void SendPlayerQuestion(string question, Action<SuspectInterrogationEntry> onResponse)
    {
        StartCoroutine(SendInterrogationRoutine(question, onResponse));
    }

    private IEnumerator SendInterrogationRoutine(string playerQuestion, Action<SuspectInterrogationEntry> onComplete)
    {
        dialogueCount++;
        if (dialogueCount % 5 == 0) yield return SummarizeHistory();

        string relevantClues = GetRelevantClues(playerQuestion);

        UpdatePressure(playerQuestion, relevantClues);
        int slipThreshold = GetSlipThreshold();

        string dynamicPrompt = $@"
            {systemPrompt}

            CURRENT CONVERSATION:
            {conversationHistory}

            PLAYER: ""{playerQuestion}""

            ANALYSIS:
            - Context: Player is asking about: [{relevantClues}]
            - Pressure Level: {pressureLevel} / {slipThreshold}
            - Difficulty Mode: {GlobalVariables.GAME_DIFFICULTY}

            INSTRUCTION:
            - Determine your response based on Pressure vs Threshold.
            - If Pressure > Threshold: You stumble, panic, or accidentally reveal a hint related to [{relevantClues}].
            - If Pressure <= Threshold: Maintain your defense/story comfortably.

            Return format exactly:
            <BEGIN>
            Response Text|Expression (angry,concerned,happy,neutral,smile)|Clue Revealed (or null)
            <END>
        ";

        bool success = false;
        int maxAttempts = 3;

        for (int i = 0; i < maxAttempts; i++)
        {
            yield return SendPromptToGemini(dynamicPrompt, "", raw =>
            {
                try
                {
                    raw = Regex.Unescape(raw);
                    var match = Regex.Match(raw, "<BEGIN>(.*?)<END>", RegexOptions.Singleline);
                    if (!match.Success) throw new Exception("Missing tags");

                    string[] parts = match.Groups[1].Value.Trim().Split('|');
                    if (parts.Length < 3) throw new Exception("Invalid format");

                    string response = parts[0].Trim();
                    string expression = parts[1].Trim().ToLower();
                    string clue = parts[2].Trim();

                    if (!new[] { "angry", "concerned", "happy", "neutral", "smile" }.Contains(expression))
                        expression = "neutral";

                    string finalClue = (clue.ToLower() == "null" || clue.Length < 3) ? null : clue;

                    if (finalClue != null) pressureLevel = Mathf.Max(0, pressureLevel - 2);

                    var entry = new SuspectInterrogationEntry
                    {
                        playerQuestion = playerQuestion,
                        response = response,
                        expression = expression,
                        clue = finalClue
                    };

                    conversationHistory += $"Player: {playerQuestion}\nSuspect: {response}\n";
                    interrogationLog.AddEntry(entry);

                    if (GetComponent<LogManager>()) GetComponent<LogManager>().AddLog(playerQuestion, response);
                    if (GetComponent<EvidenceManager>() && finalClue != null) GetComponent<EvidenceManager>().AddEvidence(finalClue);

                    onComplete?.Invoke(entry);
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Interrogation Parse Error: {e.Message}");
                }
            });

            if (success) break;
            yield return new WaitForSeconds(0.5f);
        }

        if (!success) onComplete?.Invoke(null);
    }

    private string GetRelevantClues(string question)
    {
        if (currentProfile == null || currentProfile.evidence == null) return "None";

        var words = question.ToLower().Split(new[] { ' ', '?', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var hits = new List<string>();

        foreach (var ev in currentProfile.evidence)
        {
            foreach (var word in words)
            {
                if (word.Length > 3 && ev.ToLower().Contains(word))
                {
                    hits.Add(ev);
                    break;
                }
            }
        }

        if (hits.Count == 0) return "General Topic";
        return string.Join(", ", hits.Distinct());
    }

    private void UpdatePressure(string question, string relevantClues)
    {
        if (relevantClues != "General Topic" && relevantClues != "None")
        {
            pressureLevel += 1;
        }

        if (question.ToLower().Contains("murder") || question.ToLower().Contains("kill") || question.ToLower().Contains("lie"))
        {
            pressureLevel += 1;
        }
    }

    private int GetSlipThreshold()
    {
        switch (GlobalVariables.GAME_DIFFICULTY)
        {
            case "Easy": return 2;
            case "Normal": return 4;
            case "Hard": return 7;
            default: return 4;
        }
    }

    // =================================================================================
    // 3. SUMMARIZATION & UTILS
    // =================================================================================
    private IEnumerator SummarizeHistory()
    {
        string prompt = $@"
            Summarize the following conversation in 2 sentences. 
            Focus on what lies were told or what topics were discussed.
            
            Conversation:
            {conversationHistory}
        ";

        yield return SendPromptToGemini(prompt, "", raw =>
        {
            if (!string.IsNullOrEmpty(raw))
            {
                conversationHistory = $"[Previous Summary: {raw.Trim()}]\n";
                Debug.Log("📝 History Summarized.");
            }
        });
    }

    private IEnumerator SendPromptToGemini(string prompt, string historyContext, Action<string> onResponseText)
    {
        string fullContent = historyContext + prompt;

        string requestBody = $@"{{
          ""contents"": [
            {{
              ""parts"": [{{ ""text"": {JsonEscape(fullContent)} }}]
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

        string extracted = ExtractDelimitedResponse(req.downloadHandler.text);
        if (string.IsNullOrEmpty(extracted))
        {
            extracted = req.downloadHandler.text;
        }

        onResponseText?.Invoke(extracted);
    }

    private string ExtractDelimitedResponse(string responseText)
    {
        var match = Regex.Match(responseText, "\"text\"\\s*:\\s*\"(.*?)\"", RegexOptions.Singleline);
        if (match.Success)
        {
            string raw = match.Groups[1].Value;
            return raw.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\").Trim();
        }
        return responseText;
    }

    private string JsonEscape(string raw)
    {
        return "\"" + raw.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
    }
}