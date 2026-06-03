namespace CultivationRPG.Models;

public sealed class LeaderboardEntry
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = "";
    public string Realm { get; set; } = "";
    public long Xp { get; set; }
    public long Spirit { get; set; }
    public long Agility { get; set; }
    public string RankColor => Rank switch
    {
        1 => "#FFC107",
        2 => "#90CAF9",
        3 => "#FF8A65",
        _ => "#888"
    };

    public bool IsTop3 => Rank <= 3;

    public string XpDisplay => Xp switch
    {
        >= 1_000_000_000_000 => $"{Xp / 1_000_000_000_000}兆",
        >= 100_000_000 => $"{Xp / 100_000_000}亿",
        >= 10_000 => $"{Xp / 10_000}万",
        _ => $"{Xp:N0}"
    };
}
