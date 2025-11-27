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
    private int dialogueCount = 0;

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
            - guilt_status: {(GlobalVariables.IS_SUSPECT_GUILTY ? "Guilty" : "Not Guilty")}
            - a 2-4 very short paragraph backstory that explains their relationship to the crime (each paragraph separated by '\\n')
            - If guilty: 1-3 clues that could incriminate them (separated by '\\n')
            - If not guilty: 1-3 clues that could prove their innocence or point to the real culprit (separated by '\\n')

            Return plain text like:
            <BEGIN>
            $ActualName|$ActualPersonality|$guilt_status|backstory paragraph 1\\paragraph 2\\...|clue 1\\clue 2\nclue 3
            <END>

            Don't return markdown or explanation. Never add second name, just use the first name. Just raw delimited string.
        ";

        int maxAttempts = 10;
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
                    history = $"You are {(GlobalVariables.IS_SUSPECT_GUILTY ? "Guilty" : "Not Guilty")}. Remember this throughout the interrogation.\nBackstory:\n{string.Join("\n", profile.backstory)}\nClues:\n- {string.Join("\n- ", profile.evidence)}\n";
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
        Debug.LogError("❌ Failed to generate valid suspect after 10 attempts.");
    }

    private SuspectProfile ParseDelimitedSuspectProfile(string raw)
    {
        raw = System.Text.RegularExpressions.Regex.Unescape(raw);
        Match match = Regex.Match(raw, "<BEGIN>(.*?)<END>", RegexOptions.Singleline);
        if (!match.Success)
            throw new Exception("No valid content between <BEGIN> and <END>");

        string delimited = match.Groups[1].Value.Trim();
        string[] parts = delimited.Split('|');
        if (parts.Length < 5)
        {
            Debug.LogError("🛑 Delimited response was too short:\n" + delimited);
            throw new Exception("Incomplete delimited profile. Expected 5 parts: name, personality, guilt_status, backstory, evidence");
        }

        List<string> CleanSplit(string input)
        {
            string normalized = input
                .Replace("\\\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\r", "");

            return normalized
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().TrimEnd('\\'))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }

        return new SuspectProfile
        {
            name = parts[0].Trim(),
            personality = parts[1].Trim(),
            backstory = CleanSplit(parts[3]),
            evidence = CleanSplit(parts[4])
        };
    }

    // ========== 2. Interrogation ==========
    public void SendPlayerQuestion(string question, Action<SuspectInterrogationEntry> onResponse)
    {
        StartCoroutine(SendInterrogationRoutine(question, onResponse));
    }

    private IEnumerator SendInterrogationRoutine(string playerQuestion, Action<SuspectInterrogationEntry> onComplete)
    {
        int maxAttempts = 10;
        int attempt = 0;
        bool success = false;

        // Check if we need to summarize every 5 dialogues
        dialogueCount++;
        if (dialogueCount % 5 == 0)
        {
            yield return SummarizeHistory();
        }

        while (attempt < maxAttempts && !success)
        {
            attempt++;
            string prompt = $@"
                You are {GlobalVariables.CURRENT_SUSPECT_NAME} with a {GlobalVariables.CURRENT_SUSPECT_PERSONALITY} personality.
                You are being interrogated for murder. You know you are {(GlobalVariables.IS_SUSPECT_GUILTY ? "Guilty" : "Not Guilty")}.

                IMPORTANT BEHAVIORAL RULES:
                - If you are 'guilty': You will try to hide the truth. You may lie, deflect, or act defensive. Only reveal clues when heavily pressured or caught in contradictions.
                - If you are 'not_guilty': You may be scared, confused, or frustrated. You can be more open about your innocence but might still be nervous or withhold information if it looks bad for you.

                Difficulty: {GlobalVariables.GAME_DIFFICULTY}
                - Easy: You are more nervous and may accidentally reveal information.
                - Normal: You balance between being cautious and occasionally slipping up under pressure.
                - Hard: You are very careful and composed, rarely revealing anything unless cornered with strong evidence.

                Stay consistent with your previous answers. If you lied before, remember that lie. If you told the truth, don't contradict yourself.

                Conversation so far:
                {history}

                Player: {playerQuestion}

                Return exactly this format:
                $response|$expression|$clue

                Where:
                - response = your spoken reply to the player (stay in character)
                - expression = one of: angry, concerned, happy, neutral, smile
                - clue = a short clue if revealed, or null

                DO NOT add explanations or extra lines.
                Only return one single line like this:
                <BEGIN>
                My answer to the player here|concerned|null
                <END>
            ";

            history += $"\nPlayer: {playerQuestion}\n";
            bool isDone = false;

            yield return SendPromptToGemini(prompt, raw =>
            {
                try
                {
                    raw = Regex.Unescape(raw);
                    var match = Regex.Match(raw, "<BEGIN>(.*?)<END>", RegexOptions.Singleline);
                    if (!match.Success) throw new Exception("Missing <BEGIN> or <END>");

                    string content = match.Groups[1].Value.Trim();
                    string[] parts = content.Split('|');
                    if (parts.Length < 3)
                        throw new Exception("Invalid format. Expected 3 parts: response|expression|clue");

                    string response = parts[0].Trim();
                    string expression = parts[1].Trim().ToLower();
                    string clue = parts[2].Trim();

                    if (!new[] { "angry", "concerned", "happy", "neutral", "smile" }.Contains(expression))
                        throw new Exception("Invalid expression: " + expression);

                    var entry = new SuspectInterrogationEntry
                    {
                        playerQuestion = playerQuestion,
                        response = response,
                        expression = expression,
                        clue = clue == "null" ? null : clue
                    };

                    history += $"Suspect: {entry.response}\n";
                    interrogationLog.AddEntry(entry);

                    var logManager = GetComponent<LogManager>();
                    if (logManager != null)
                    {
                        logManager.AddLog(entry.playerQuestion, entry.response);
                    }

                    var clueManager = GetComponent<EvidenceManager>();
                    if (clueManager != null)
                    {
                        clueManager.AddEvidence(entry.clue);
                    }

                    onComplete?.Invoke(entry);
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Attempt {attempt} failed to parse interrogation reply: {e.Message}");
                }
                finally
                {
                    isDone = true;
                }
            });

            while (!isDone) yield return null;
            if (!success)
                yield return new WaitForSeconds(1f);
        }

        if (!success)
        {
            Debug.LogError("❌ All attempts to get a valid suspect response failed.");
            onComplete?.Invoke(null);
        }
    }

    // ========== Summary System (Token Optimization) ==========
    private IEnumerator SummarizeHistory()
    {
        string summaryPrompt = $@"
            Summarize the last 5 exchanges of this interrogation into a brief 2-3 sentence conclusion about what was discussed and any key revelations or lies. Keep the suspect's guilt status and core backstory intact.

            Current History:
            {history}

            Return only the summary text without tags or formatting.
        ";

        bool isDone = false;
        yield return SendPromptToGemini(summaryPrompt, raw =>
        {
            try
            {
                string summary = raw.Trim();
                string coreInfo = $"You are {(GlobalVariables.IS_SUSPECT_GUILTY ? "Guilty" : "Not Guilty")}. Remember this throughout the interrogation.\nBackstory:\n{string.Join("\n", currentProfile.backstory)}\nClues:\n- {string.Join("\n- ", currentProfile.evidence)}\n";
                history = coreInfo + "\n[Previous conversation summary: " + summary + "]\n";
                Debug.Log("📝 History summarized to save tokens.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to summarize history: {e.Message}");
            }
            finally
            {
                isDone = true;
            }
        });
        while (!isDone) yield return null;
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
        raw = raw.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\").Trim();
        return raw;
    }

    private string JsonEscape(string raw)
    {
        return "\"" + raw.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
    }
}
