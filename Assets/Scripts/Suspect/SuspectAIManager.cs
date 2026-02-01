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
    private string generationModel = "gemini-2.5-pro";
    private string chatModel = "gemini-2.5-flash";

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
            Task: Create a complex murder suspect profile (Suspect POV).
            
            Context:
            - Name: {GlobalVariables.CURRENT_SUSPECT_NAME} (Use exactly this name)
            - Personality: {GlobalVariables.CURRENT_SUSPECT_PERSONALITY}
            - Guilt: {(GlobalVariables.IS_SUSPECT_GUILTY ? "Guilty" : "Not Guilty")}
            - Language: {GlobalVariables.LANGUAGE_SETTINGS} (ONLY FOR BACKSTORY AND CLUES)
            
            Directives for BACKSTORY (2-3 concise paragraph):
            1. POV: First Person ('I' / 'Me').
            2. CRITICAL: The very first sentence MUST define your relationship with the victim (e.g., 'She was my business partner', 'He was a stranger I met at the bar', 'She was like a daughter to me').
            3. CONTENT: 
               - Describe your history with the victim clearly.
               - Explain where you were during the crime (Alibi).
               - If GUILTY: You did it, but your backstory is the lie/half-truth you tell the police. You admit the relationship, but lie about the killing.
               - If INNOCENT: You are telling the truth, but you have a secret or embarrassing reason to be nervous.
            
            Directives for EVIDENCE (3-5 items):
            - Physical items (receipts, tools, messages) that connect you to the scene or victim.

            Output Format (Strict Plain Text):
            <BEGIN>
            {GlobalVariables.CURRENT_SUSPECT_NAME}|$ActualPersonality|$guilt_status|backstory paragraph 1\\paragraph 2|clue 1\\clue 2
            <END>

            Do not use Markdown. Do not translate <BEGIN> tags.
        ";

        bool success = false;
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            yield return SendPromptToGemini(prompt, generationModel ,"", rawText =>
            {
                try
                {
                    var profile = ParseDelimitedSuspectProfile(rawText);
                    currentProfile = profile;
                    GeneratedProfile = profile;

                    systemPrompt = BuildSystemPrompt(profile);

                    Debug.Log("<color=cyan>=== PROFILE GENERATED ===</color>");
                    Debug.Log($"<b>Name:</b> {profile.name}");
                    Debug.Log($"<b>Personality:</b> {profile.personality}");
                    Debug.Log($"<b>Backstory:</b>\n{string.Join("\n", profile.backstory)}");
                    Debug.Log($"<b>Evidence:</b>\n{string.Join("\n", profile.evidence)}");
                    Debug.Log("<color=cyan>=========================</color>");

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

    private string BuildSystemPrompt(SuspectProfile profile)
    {
        return $@"
            IDENTITY: You are {profile.name}, a {profile.personality} character.
            STATUS: {(GlobalVariables.IS_SUSPECT_GUILTY ? "GUILTY" : "INNOCENT")}.
            LANGUAGE: {GlobalVariables.LANGUAGE_SETTINGS}.

            [ABSOLUTE TRUTH - MEMORY IMPLANT]
            The text below is your ABSOLUTE MEMORY. You cannot know anything outside of this scope.
            
            === YOUR MEMORY (BACKSTORY) ===
            {string.Join("\n", profile.backstory)}

            === KNOWN EVIDENCE (REVEAL ONLY IF PRESSURED) ===
            {string.Join("\n", profile.evidence)}

            [INTERROGATION RULES]
            1. ACKNOWLEDGE THE VICTIM: The person described in your Backstory IS the victim. 
                - NEVER say 'I don't know who you mean' or 'No relationship' if the backstory says otherwise.
                - If the backstory says 'like a brother/daughter', ADMIT this bond emotionally.
        
            2. DEFENSE STRATEGY:
                - If GUILTY: ADMIT the relationship, but DENY the murder. Stick to the alibi in your backstory. Do not feign amnesia.
                - If INNOCENT: You are nervous but truthful. You want to help but are afraid of being blamed.

            3. TONE:
                - Answering 'I don't know' to basic questions about yourself/victim makes you look fake. Avoid it.
                - Keep answers natural, short (1-3 sentences).
        ";
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
            string processed = input.Replace("\\\\", "\n");
            processed = processed.Replace("\\", "\n");
            processed = processed.Replace("\\n", "\n");

            return processed.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
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

    public void SendPlayerQuestion(string question, Action<SuspectInterrogationEntry> onResponse)
    {
        StartCoroutine(SendInterrogationRoutine(question, onResponse));
    }

    private IEnumerator SendInterrogationRoutine(string playerQuestion, Action<SuspectInterrogationEntry> onComplete)
    {
        dialogueCount++;
        GlobalVariables.TOTAL_CONVERSATIONS++;

        if (dialogueCount % 5 == 0) yield return SummarizeHistory();

        if (string.IsNullOrEmpty(systemPrompt))
        {
            if (GeneratedProfile.name != null)
            {
                Debug.Log("⚠️ System Prompt was empty. Rebuilding from Static Profile.");
                currentProfile = GeneratedProfile;
                systemPrompt = BuildSystemPrompt(currentProfile);
            }
            else
            {
                Debug.LogError("❌ FATAL: No Profile Data found to generate System Prompt!");
                onComplete?.Invoke(null);
                yield break;
            }
        }

        string relevantClues = GetRelevantClues(playerQuestion);

        UpdatePressure(playerQuestion, relevantClues);
        int slipThreshold = GetSlipThreshold();

        string dynamicPrompt = $@"
            {systemPrompt}

            REMINDER: Reply strictly in {GlobalVariables.LANGUAGE_SETTINGS}, ONLY FOR RESPONSE AND ALL CLUES.

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
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            yield return SendPromptToGemini(dynamicPrompt, chatModel, "", raw =>
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

                    Debug.Log($"<color=yellow>[PLAYER]:</color> {playerQuestion}");
                    Debug.Log($"<color=white>[SUSPECT]:</color> {response}");
                    Debug.Log($"<color=grey>[EXPRESSION]:</color> {expression}");
                    Debug.Log($"<color=grey>[PRESSURE]:</color> {pressureLevel} / {slipThreshold}");

                    if (finalClue != null)
                    {
                        Debug.Log($"<color=red><b>[!!! CLUE REVEALED !!!]:</b> {finalClue}</color>");
                        var evidenceMgr = FindObjectOfType<EvidenceManager>();

                        if (evidenceMgr != null)
                        {
                            bool isNew = evidenceMgr.AddEvidence(finalClue);
                            if (isNew)
                            {
                                evidenceMgr.ShowClueFoundIcon();
                            }
                        }
                        else
                        {
                            Debug.LogError("⚠️ SuspectAIManager: Tidak bisa menemukan EvidenceManager di Scene!");
                        }
                    }

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

                    if (finalClue != null)
                    {
                        var evidenceMgr = GetComponent<EvidenceManager>();
                        if (evidenceMgr != null)
                        {
                            evidenceMgr.AddEvidence(finalClue);
                            evidenceMgr.ShowClueFoundIcon();
                        }
                    }

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
    // 2.5. PLAYER SUGGESTION SYSTEM (AUTO HINTS)
    // =================================================================================
    
    public void GenerateSuggestedQuestions(Action<List<string>> onSuggestionsReady)
    {
        if (GlobalVariables.GAME_DIFFICULTY == "Hard")
        {
            Debug.Log("🔒 [HINTS]: Suggestions disabled in HARD mode.");
            onSuggestionsReady?.Invoke(new List<string>());
            return;
        }

        StartCoroutine(GenerateSuggestionsRoutine(onSuggestionsReady));
    }

    private IEnumerator GenerateSuggestionsRoutine(Action<List<string>> onReady)
    {
        if (currentProfile == null || currentProfile.evidence == null)
        {
            Debug.LogWarning("⚠️ Cannot generate hints: No profile loaded.");
            yield break;
        }

        string evidenceList = string.Join(", ", currentProfile.evidence);

        string prompt = $@"
            ROLE: Expert Detective Assistant.
            TARGET: {currentProfile.name}
            GOAL: The player needs to catch the suspect lying or reveal a clue.
            
            HIDDEN TRUTHS (CLUES) THE SUSPECT IS HIDING:
            [{evidenceList}]

            CURRENT CONTEXT:
            {conversationHistory}

            TASK:
            Generate 3 sharp, specific questions for the PLAYER to ask right now.
            
            CRITERIA:
            1. High Probability (80%): The questions must trap the suspect into talking about the 'HIDDEN TRUTHS' listed above.
            2. LENGTH: STRICTLY 8 TO 10 WORDS MAX per question. (Must fit in small UI button).
            3. Style: Direct, aggressive, short, and punchy.
            4. Language: {GlobalVariables.LANGUAGE_SETTINGS}.

            OUTPUT FORMAT (Strictly 3 lines separated by pipe '|'):
            Question 1|Question 2|Question 3
        ";

        Debug.Log($"<color=magenta>🧠 [GENERATING HINTS]...</color>");

        yield return SendPromptToGemini(prompt, chatModel, "", raw =>
        {
            List<string> suggestions = new List<string>();

            try
            {
                string cleanRaw = Regex.Unescape(raw).Trim();

                if (cleanRaw.StartsWith("\"") && cleanRaw.EndsWith("\""))
                    cleanRaw = cleanRaw.Substring(1, cleanRaw.Length - 2);

                string[] parts = cleanRaw.Split('|');

                foreach (string part in parts)
                {
                    string q = part.Trim();
                    if (!string.IsNullOrEmpty(q))
                    {
                        suggestions.Add(q);
                    }
                }

                Debug.Log("<color=magenta>=== 💡 SUGGESTED QUESTIONS (80% Success Rate) ===</color>");
                for (int i = 0; i < suggestions.Count; i++)
                {
                    Debug.Log($"<b>Option {i + 1}:</b> {suggestions[i]}");
                }
                Debug.Log("<color=magenta>===============================================</color>");

                onReady?.Invoke(suggestions);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to parse suggestions: {e.Message}");
                onReady?.Invoke(null);
            }
        });
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

        yield return SendPromptToGemini(prompt, generationModel, "", raw =>
        {
            if (!string.IsNullOrEmpty(raw))
            {
                conversationHistory = $"[Previous Summary: {raw.Trim()}]\n";
                Debug.Log("📝 History Summarized.");
            }
        });
    }

    private IEnumerator SendPromptToGemini(string prompt, string modelToUse, string historyContext, Action<string> onResponseText)
    {
        string fullContent = historyContext + prompt;

        Debug.Log($"<color=orange><b>[SENDING PROMPT TO {modelToUse}]:</b></color>\n{fullContent}");

        string requestBody = $@"{{
          ""contents"": [
            {{
              ""parts"": [{{ ""text"": {JsonEscape(fullContent)} }}]
            }}
          ],
          ""generationConfig"": {{
            ""maxOutputTokens"": 8192,
            ""temperature"": 0.7
          }}
        }}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        UnityWebRequest req = new UnityWebRequest(
            $"https://generativelanguage.googleapis.com/v1beta/models/{modelToUse}:generateContent?key={apiKey}",
            "POST"
        );

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gemini API Error: " + req.error);
            Debug.LogError("Response Error Body: " + req.downloadHandler.text); // Debug body error
            yield break;
        }

        Debug.Log($"[GEMINI RAW JSON]: {req.downloadHandler.text}");
        
        string extracted = ExtractDelimitedResponse(req.downloadHandler.text);
        if (string.IsNullOrEmpty(extracted))
            Debug.Log($"[GEMINI EXTRACTED]: <Empty>");
        else
            Debug.Log($"[GEMINI EXTRACTED]: {extracted}");

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