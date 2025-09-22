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
    [Header("Case Database")]
    public CaseDatabase caseDatabase;

    [Header("Gemini API Key JSON")]
    public TextAsset apiKeyFile;

    public static SuspectProfile GeneratedProfile;
    private string apiKey;
    private string model = "gemini-2.0-flash";
    private string history = "";

    [HideInInspector] public SuspectProfile currentProfile;
    public SuspectInterrogationLog interrogationLog = new();

    [Serializable] private class APIKeyWrapper { public string key; }
    private CaseData currentCase;

    private void Awake() => LoadAPIKey();

    private void Start()
    {
        if (caseDatabase == null || caseDatabase.cases == null || caseDatabase.cases.Count == 0)
        {
            Debug.LogError("⚠️ CaseDatabase is empty or not assigned!");
            return;
        }

        if (GeneratedProfile != null)
        {
            currentProfile = GeneratedProfile;
            currentCase = GlobalVariables.CURRENT_CASE;
            history = $"Backstory:\n{string.Join("\n", currentProfile.backstory ?? new List<string>())}\nClues:\n- {string.Join("\n- ", currentProfile.evidence ?? new List<string>())}\n";
            Debug.Log($"♻️ Using existing case: {currentCase?.caseName} | Difficulty: {GlobalVariables.GAME_DIFFICULTY}");
        }
        else
        {
            SelectRandomCaseByDifficulty();
            Debug.Log($"🎯 Loaded Case: {currentCase.caseName} | Difficulty: {currentCase.difficulty}");
        }
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

    /// <summary>
    /// Pilih case random sesuai GlobalVariables.GAME_DIFFICULTY.
    /// Jika difficulty invalid atau tidak ada case yang cocok, fallback ke seluruh daftar.
    /// </summary>
    private void SelectRandomCaseByDifficulty()
    {
        DifficultyLevel selectedDifficulty;
        bool parsed = Enum.TryParse(GlobalVariables.GAME_DIFFICULTY, true, out selectedDifficulty);

        if (!parsed)
        {
            Debug.LogWarning("⚠️ GlobalVariables.GAME_DIFFICULTY invalid or empty, defaulting to Normal");
            selectedDifficulty = DifficultyLevel.Normal;
        }

        var filtered = caseDatabase.cases.Where(c => c.difficulty == selectedDifficulty).ToList();

        if (filtered.Count == 0)
        {
            Debug.LogWarning($"⚠️ No cases found for difficulty {selectedDifficulty}. Falling back to all cases.");
            filtered = caseDatabase.cases.ToList();
        }

        int idx = UnityEngine.Random.Range(0, filtered.Count);
        currentCase = filtered[idx];

        // keep global difficulty & case in-sync
        GlobalVariables.GAME_DIFFICULTY = currentCase.difficulty.ToString();
        GlobalVariables.CURRENT_CASE = currentCase;
    }

    // ========== 1. Generate Profile ==========
    public void GenerateSuspectProfile(Action<SuspectProfile> onGenerated)
    {
        StartCoroutine(GenerateSuspectRoutine(onGenerated));
    }

    private IEnumerator GenerateSuspectRoutine(Action<SuspectProfile> onGenerated)
    {
        string caseContext = currentCase != null ? $"Case: {currentCase.caseName}" : "Case: Unknown";

        string prompt = $@"
            {caseContext}

            Generate a fictional SUSPECT profile. Always from suspect POV (never victim or narrator).
            Details:
            - name: {GlobalVariables.CURRENT_SUSPECT_NAME}
            - personality: {GlobalVariables.CURRENT_SUSPECT_PERSONALITY}
            - a 2–4 very short paragraph backstory (each separated by '\n')
            - 1–3 very short clues hidden in the story (each separated by '\n')
            - guilty: decide randomly TRUE or FALSE

            Rules:
            - Always write from suspect's POV (first person).
            - If guilty = TRUE, suspect hides something or lies.
            - If guilty = FALSE, suspect defends themselves, frustrated at being accused.

            Return plain text only:
            <BEGIN>
            name|personality|paragraph1\nparagraph2...|clue1\nclue2|TRUE/FALSE
            <END>
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
                    if (profile == null) throw new Exception("Parsed profile is null");

                    currentProfile = profile;
                    GeneratedProfile = profile; // ✅ Simpan supaya tidak regenerate di scene berikutnya

                    // ✅ Set guilty langsung dari profile
                    GlobalVariables.IS_SUSPECT_GUILTY = profile.isGuilty;

                    history = $"Backstory:\n{string.Join("\n", profile.backstory ?? new List<string>())}\nClues:\n- {string.Join("\n- ", profile.evidence ?? new List<string>())}\n";
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

        Debug.LogError("❌ Failed to generate valid suspect after multiple attempts.");
    }

    private SuspectProfile ParseDelimitedSuspectProfile(string raw)
    {
        raw = Regex.Unescape(raw ?? "");

        Match match = Regex.Match(raw, "<BEGIN>(.*?)<END>", RegexOptions.Singleline);
        if (!match.Success)
            throw new Exception("No valid content between <BEGIN> and <END>");

        string delimited = match.Groups[1].Value.Trim();
        string[] parts = delimited.Split('|');

        if (parts.Length < 5)
            throw new Exception("Incomplete delimited profile. Expected 5 parts: name, personality, backstory, evidence, guilty");

        List<string> CleanSplit(string input)
        {
            if (string.IsNullOrEmpty(input)) return new List<string>();

            // Pisah berdasarkan newline nyata
            return input.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList();
        }

        // Normalize guilty parsing
        string guiltRaw = parts[4].Trim().ToLower();
        bool isGuilty = guiltRaw == "true" || guiltRaw == "yes" || guiltRaw == "guilty" || guiltRaw == "1";

        return new SuspectProfile
        {
            name = parts[0].Trim(),
            personality = parts[1].Trim(),
            backstory = CleanSplit(parts[2]),
            evidence = CleanSplit(parts[3]),
            isGuilty = isGuilty
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

        while (attempt < maxAttempts && !success)
        {
            attempt++;

            string prompt = $@"
                You are the suspect named {GlobalVariables.CURRENT_SUSPECT_NAME} 
                with a {GlobalVariables.CURRENT_SUSPECT_PERSONALITY} personality.

                Always answer as THIS suspect (first person). 
                Never say you are the victim. Never speak as narrator. Always suspect POV.

                Guilt status: {(GlobalVariables.IS_SUSPECT_GUILTY ? "GUILTY" : "INNOCENT")}
                - If guilty: act evasive, nervous, or lie to hide facts.
                - If innocent: act frustrated, defensive, or indignant at being accused.

                Difficulty: {GlobalVariables.GAME_DIFFICULTY}
                - Easy: Might reveal clues even accidentally.
                - Normal: Might slip under pressure.
                - Hard: Avoid giving clues unless heavily pressed.

                Conversation so far:
                {history}
                Player: {playerQuestion}

                ⚠️ CRITICAL RULES:
                - You MUST return exactly one single line, in this format:
                <BEGIN>
                your spoken response here|expression|clue
                <END>

                Where:
                - response = what you say (natural suspect reply, 1–3 sentences max)
                - expression = ONLY one of [angry, concerned, happy, neutral, smile]
                - clue = a short clue text, or 'null' if none
            ";

            history += $"\nPlayer: {playerQuestion}\n";

            bool isDone = false;

            yield return SendPromptToGemini(prompt, raw =>
            {
                try
                {
                    raw = Regex.Unescape(raw ?? "");

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
                        logManager.AddLog(entry.playerQuestion, entry.response);

                    var clueManager = GetComponent<EvidenceManager>();
                    if (clueManager != null)
                        clueManager.AddEvidence(entry.clue);

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

        // Decode escape sequences dari JSON
        raw = raw.Replace("\\\"", "\"")
                 .Replace("\\\\", "\\")
                 .Replace("\\n", "\n")
                 .Replace("\\r", "");

        // Gabungkan newline beruntun jadi satu
        raw = Regex.Replace(raw, @"\n{2,}", "\n");

        return raw.Trim();
    }

    private string JsonEscape(string raw)
    {
        return "\"" + (raw ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
    }
}