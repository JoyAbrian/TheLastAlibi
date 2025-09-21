using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CaseDatabase", menuName = "Game/Case Database")]
public class CaseDatabase : ScriptableObject
{
    public List<CaseData> cases = new List<CaseData>();
}

[System.Serializable]
public class CaseData
{
    public string caseName;
    public DifficultyLevel difficulty;
}

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}