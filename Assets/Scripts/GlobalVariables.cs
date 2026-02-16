using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    public static string GAME_DIFFICULTY = "Normal";

    public static int TOTAL_CLUES_FOUND = 0;
    public static int TOTAL_CONVERSATIONS = 0;

    public static int WRONG_ACCUSATIONS = 0;
    public static bool IS_SUSPECT_GUILTY = false;

    public static Suspect CURRENT_SUSPECT;
    public static string CURRENT_SUSPECT_NAME = "";
    public static string CURRENT_SUSPECT_PERSONALITY = "";
    public static string CURRENT_SUSPECT_MOOD = "Neutral";

    public static List<string> INTERROGATION_LOG = new();
    public static List<string> FOUND_EVIDENCE = new();

    public static float MASTER_VOLUME = 1f;
    public static float MUSIC_VOLUME = 0.8f;
    public static float SOUND_EFFECTS_VOLUME = 0.5f;

    public static string LANGUAGE_SETTINGS = "ENGLISH";

    public static void ResetGameVariables()
    {
        TOTAL_CLUES_FOUND = 0;
        TOTAL_CONVERSATIONS = 0;

        WRONG_ACCUSATIONS = 0;
        IS_SUSPECT_GUILTY = false;

        CURRENT_SUSPECT = null;
        CURRENT_SUSPECT_NAME = "";
        CURRENT_SUSPECT_MOOD = "Neutral";

        INTERROGATION_LOG.Clear();
        FOUND_EVIDENCE.Clear();
    }
}