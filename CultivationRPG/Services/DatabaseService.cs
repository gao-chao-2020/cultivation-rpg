using System.IO;
using CultivationRPG.Models;
using Microsoft.Data.Sqlite;

namespace CultivationRPG.Services;

public sealed class DatabaseService : IDisposable
{
    private readonly SqliteConnection _conn;

    public DatabaseService(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        using var p = _conn.CreateCommand(); p.CommandText = "PRAGMA journal_mode=WAL;"; p.ExecuteNonQuery();
        CreateTables();
    }

    private void CreateTables()
    {
        using var c = _conn.CreateCommand();
        c.CommandText = @"
            CREATE TABLE IF NOT EXISTS player (
                id TEXT PRIMARY KEY, name TEXT NOT NULL,
                xp INTEGER DEFAULT 0, spirit INTEGER DEFAULT 0,
                agility INTEGER DEFAULT 0, wisdom INTEGER DEFAULT 0,
                alchemy INTEGER DEFAULT 0, rebirth INTEGER DEFAULT 0,
                created_at TEXT
            );
            CREATE TABLE IF NOT EXISTS daily_stats (
                date TEXT PRIMARY KEY, keys INTEGER DEFAULT 0,
                clicks INTEGER DEFAULT 0, distance_m REAL DEFAULT 0,
                active_min INTEGER DEFAULT 0, xp_earned INTEGER DEFAULT 0
            );
        ";
        c.ExecuteNonQuery();

        // Migration: add rebirth column if missing
        try { using var m = _conn.CreateCommand(); m.CommandText = "ALTER TABLE player ADD COLUMN rebirth INTEGER DEFAULT 0"; m.ExecuteNonQuery(); }
        catch { /* column already exists */ }
    }

    public Player? LoadPlayer()
    {
        try
        {
            using var c = _conn.CreateCommand();
            c.CommandText = "SELECT id, name, xp, spirit, agility, wisdom, alchemy, rebirth, created_at FROM player LIMIT 1";
            using var r = c.ExecuteReader();
            if (r.Read())
            {
                return new Player
                {
                    Id = r.GetString(0), Name = r.GetString(1),
                    Xp = r.GetInt64(2), Spirit = r.GetInt64(3),
                    Agility = r.GetInt64(4), Wisdom = r.GetInt64(5),
                    Alchemy = r.GetInt64(6), Rebirth = r.GetInt32(7),
                    CreatedAt = DateTime.Parse(r.GetString(8))
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadPlayer error: {ex.Message}");
            return null;
        }
    }

    public void SavePlayer(Player p)
    {
        using var c = _conn.CreateCommand();
        c.CommandText = @"INSERT INTO player (id,name,xp,spirit,agility,wisdom,alchemy,rebirth,created_at)
            VALUES (@i,@n,@x,@s,@a,@w,@l,@r,@c)
            ON CONFLICT(id) DO UPDATE SET name=@n,xp=@x,spirit=@s,agility=@a,wisdom=@w,alchemy=@l,rebirth=@r";
        c.Parameters.AddWithValue("@i", p.Id); c.Parameters.AddWithValue("@n", p.Name);
        c.Parameters.AddWithValue("@x", p.Xp); c.Parameters.AddWithValue("@s", p.Spirit);
        c.Parameters.AddWithValue("@a", p.Agility); c.Parameters.AddWithValue("@w", p.Wisdom);
        c.Parameters.AddWithValue("@l", p.Alchemy); c.Parameters.AddWithValue("@r", p.Rebirth);
        c.Parameters.AddWithValue("@c", p.CreatedAt.ToString("O"));
        c.ExecuteNonQuery();
    }

    public void SaveDailyStats(string date, long keys, long clicks, double dist, int activeMin, long xp)
    {
        using var c = _conn.CreateCommand();
        c.CommandText = @"INSERT OR REPLACE INTO daily_stats (date,keys,clicks,distance_m,active_min,xp_earned)
            VALUES (@d,@k,@c,@m,@a,@x)";
        c.Parameters.AddWithValue("@d", date); c.Parameters.AddWithValue("@k", keys);
        c.Parameters.AddWithValue("@c", clicks); c.Parameters.AddWithValue("@m", dist);
        c.Parameters.AddWithValue("@a", activeMin); c.Parameters.AddWithValue("@x", xp);
        c.ExecuteNonQuery();
    }

    public (long keys, long clicks, double dist, int activeMin, long xp) GetTodayStats(string date)
    {
        using var c = _conn.CreateCommand();
        c.CommandText = "SELECT keys,clicks,distance_m,active_min,xp_earned FROM daily_stats WHERE date=@d";
        c.Parameters.AddWithValue("@d", date);
        using var r = c.ExecuteReader();
        if (r.Read()) return (r.GetInt64(0), r.GetInt64(1), r.GetDouble(2), r.GetInt32(3), r.GetInt64(4));
        return (0, 0, 0, 0, 0);
    }

    public SqliteConnection Connection => _conn;
    public void Dispose() { _conn.Close(); _conn.Dispose(); }
}
