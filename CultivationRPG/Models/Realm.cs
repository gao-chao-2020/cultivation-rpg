namespace CultivationRPG.Models;

public enum RealmLevel
{
    QiRefining = 0,      // 练气
    Foundation = 1,      // 筑基
    GoldenCore = 2,      // 金丹
    NascentSoul = 3,     // 元婴
    SpiritSevering = 4,  // 化神
    Tribulation = 5,     // 渡劫
    Mahayana = 6,        // 大乘
    Ascension = 7,       // 飞升
}

public static class RealmInfo
{
    public static readonly (long Xp, string Cn, string En, string Title)[] Levels =
    [
        (0,             "练气", "Qi Refining",    "凡人"),
        (10_000,        "筑基", "Foundation",     "修士"),
        (50_000,        "金丹", "Golden Core",    "真人"),
        (200_000,       "元婴", "Nascent Soul",   "真君"),
        (1_000_000,     "化神", "Spirit Severing","道君"),
        (5_000_000,     "渡劫", "Tribulation",    "仙尊"),
        (20_000_000,    "大乘", "Mahayana",       "仙帝"),
        (100_000_000,   "飞升", "Ascension",      "飞升者"),
    ];

    public static (RealmLevel Level, string Cn, string En, string Title) GetRealm(long xp)
    {
        for (int i = Levels.Length - 1; i >= 0; i--)
            if (xp >= Levels[i].Xp)
                return ((RealmLevel)i, Levels[i].Cn, Levels[i].En, Levels[i].Title);
        return (RealmLevel.QiRefining, "练气", "Qi Refining", "凡人");
    }

    public static long XpToNext(long xp)
    {
        for (int i = 0; i < Levels.Length; i++)
            if (xp < Levels[i].Xp)
                return Levels[i].Xp - xp;
        return 0; // already at max
    }

    public static long XpForLevel(RealmLevel level)
        => Levels[(int)level].Xp;

    public static double ProgressInRealm(long xp)
    {
        var realm = GetRealm(xp);
        int idx = (int)realm.Level;
        if (idx >= Levels.Length - 1) return 1.0;
        long current = xp - Levels[idx].Xp;
        long total = Levels[idx + 1].Xp - Levels[idx].Xp;
        return total > 0 ? Math.Clamp((double)current / total, 0, 1) : 1.0;
    }
}
