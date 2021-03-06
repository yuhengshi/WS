﻿using System.Collections.Generic;

[System.Serializable]
public class Lineup
{
    public static int tacticLimit = 10;
    public Dictionary<Location, Collection> cardLocations;
    public List<Tactic> tactics;
    public string boardName;
    public string lineupName;

    public Lineup()
    {
        Clear();
    }

    public Lineup(Dictionary<Location, Collection> cardLoc, List<string> Tactics, string BoardName = "Standard Board", string LineupName = "Custom Lineup", string General = "Standard General")
    {
        List<Tactic> playerTactics = new List<Tactic>();
        foreach (string tacticName in Tactics) playerTactics.Add(new Tactic(Database.FindTacticAttributes(tacticName)));
        SetInfo(cardLoc, playerTactics, BoardName, LineupName, General);
    }

    public Lineup(Dictionary<Location, Collection> cardLoc, List<Tactic> Tactics, string BoardName = "Standard Board", string LineupName = "Custom Lineup", string General = "Standard General")
    {
        SetInfo(cardLoc, Tactics, BoardName, LineupName, General);
    }

    private void SetInfo(Dictionary<Location, Collection> cardLoc, List<Tactic> Tactics, string BoardName = "Standard Board", string LineupName = "Custom Lineup", string General = "Standard General")
    {
        cardLocations = cardLoc;
        tactics = Tactics;
        boardName = BoardName;
        lineupName = LineupName;
    }

    public void Clear()
    {
        cardLocations = new Dictionary<Location, Collection>();
        tactics = new List<Tactic>();
        boardName = "Standard Board";
        lineupName = "Custom Lineup";
    }

    public bool IsEmpty()
    {
        return cardLocations.Count == 0 && tactics.Count == 0;
    }

    public bool IsComplete()
    {
        return tactics.Count == tacticLimit;
    }

    public string GetGeneral()
    {
        return cardLocations[Database.boards[boardName].agloc].name;
    }
}