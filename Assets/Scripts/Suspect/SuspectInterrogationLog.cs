using System.Collections.Generic;
using UnityEngine;

public class SuspectInterrogationLog
{
    public EvidenceManager evidenceManager;
    public LogManager logManager;

    public List<SuspectInterrogationEntry> entries = new();

    public void AddEntry(SuspectInterrogationEntry entry)
    {
        entries.Add(entry);
    }

    public List<string> GetAllClues()
    {
        List<string> clues = new();
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.clue) && entry.clue.ToLower() != "null")
                clues.Add(entry.clue);
        }
        return clues;
    }
}