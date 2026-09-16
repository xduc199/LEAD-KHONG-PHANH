using System;

[Serializable]
public class LeaderboardEntry
{
    public string uid;
    public string displayName;
    public int bestScore;

    public LeaderboardEntry()
    {
        uid = string.Empty;
        displayName = "Player";
        bestScore = 0;
    }

    public LeaderboardEntry(
        string uid,
        string displayName,
        int bestScore)
    {
        this.uid = uid;
        this.displayName = displayName;
        this.bestScore = bestScore;
    }
}