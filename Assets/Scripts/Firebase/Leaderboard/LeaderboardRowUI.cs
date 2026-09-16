using TMPro;
using UnityEngine;

public sealed class LeaderboardRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;

    public void SetData(
        int rank,
        LeaderboardEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (rankText != null)
        {
            rankText.text = rank.ToString();
        }

        if (nameText != null)
        {
            nameText.text =
                string.IsNullOrEmpty(entry.displayName)
                    ? "Player"
                    : entry.displayName;
        }

        if (scoreText != null)
        {
            scoreText.text =
                entry.bestScore.ToString("N0");
        }
    }
}