namespace CultivationRPG.Models;

public sealed class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Name { get; set; } = "无名修士";
    public long Xp { get; set; }
    public long Spirit { get; set; }   // 灵力 (keystrokes)
    public double Agility { get; set; }  // 身法 (mouse distance)
    public long Wisdom { get; set; }   // 神识 (active minutes)
    public long Alchemy { get; set; }  // 炼丹 (clicks)
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public RealmLevel Level => RealmInfo.GetRealm(Xp).Level;
    public int Rebirth { get; set; }
    public string RealmPrefix => Rebirth > 0 ? $"{Rebirth}转·" : "";
    public string RealmName => RealmPrefix + RealmInfo.GetRealm(Xp).Cn;
    public string RealmTitle => RealmInfo.GetRealm(Xp).Title;
    public string RealmNameEn => RealmInfo.GetRealm(Xp).En;
    public double RealmProgress => RealmInfo.ProgressInRealm(Xp);
    public long XpToNext => RealmInfo.XpToNext(Xp);

    // Today's tracking
    public long TodayKeys { get; set; }
    public long TodayClicks { get; set; }
    public double TodayDistance { get; set; }
    public int TodayActiveMin { get; set; }
    public long TodayXp => (long)(TodayKeys + TodayDistance + TodayClicks + TodayActiveMin);
}
