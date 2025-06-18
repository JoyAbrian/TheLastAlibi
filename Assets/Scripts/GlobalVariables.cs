using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    public static string GAME_DIFFICULTY = "Normal";

    // Player status
    public static int TOTAL_CLUES_FOUND = 0;
    public static int WRONG_ACCUSATIONS = 0;
    public static bool MADE_FINAL_ACCUSATION = false;

    // Game state
    public static int CURRENT_SUSPECT_INDEX = 0;
    public static string CURRENT_SUSPECT_NAME = "";
    public static string CURRENT_SUSPECT_MOOD = "Neutral";

    // Game Log
    public static List<string> INTERROGATION_LOG = new List<string>();
    public static List<string> FOUND_EVIDENCE = new List<string>();

    public static void ResetGameVariables()
    {
        GAME_DIFFICULTY = "Normal";

        TOTAL_CLUES_FOUND = 0;
        WRONG_ACCUSATIONS = 0;
        MADE_FINAL_ACCUSATION = false;

        CURRENT_SUSPECT_INDEX = 0;
        CURRENT_SUSPECT_NAME = "";
        CURRENT_SUSPECT_MOOD = "Neutral";

        INTERROGATION_LOG.Clear();
        FOUND_EVIDENCE.Clear();
    }
}