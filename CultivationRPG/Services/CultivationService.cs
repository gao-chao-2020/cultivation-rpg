using System.Threading.Channels;
using CultivationRPG.Models;

namespace CultivationRPG.Services;

public sealed class CultivationService : IDisposable
{
    private readonly DatabaseService _db;
    private readonly ChannelReader<InputEvent> _reader;
    private readonly Player _player;
    private readonly CancellationTokenSource _cts = new();
    private long _todayKeys, _todayClicks;
    private double _todayDist;
    private double _todayActiveSec;
    private string _lastDate = "";
    private (int X, int Y)? _lastPos;
    private DateTime _lastEvent = DateTime.MinValue;

    public Player Player => _player;

    public CultivationService(DatabaseService db, ChannelReader<InputEvent> reader)
    {
        _db = db;
        _reader = reader;
        _player = db.LoadPlayer() ?? new Player();
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var (k, c, d, a, _) = db.GetTodayStats(today);
        _todayKeys = k; _todayClicks = c; _todayDist = d; _todayActiveSec = a * 60;
        _player.TodayKeys = k; _player.TodayClicks = c;
        _player.TodayDistance = d; _player.TodayActiveMin = a;
        Task.Run(LoopAsync);
    }

    public void Reborn()
    {
        _player.Xp = 0; _player.Spirit = 0; _player.Agility = 0;
        _player.Wisdom = 0; _player.Alchemy = 0;
        _player.TodayKeys = 0; _player.TodayClicks = 0;
        _player.TodayDistance = 0; _player.TodayActiveMin = 0;
        _todayKeys = 0; _todayClicks = 0; _todayDist = 0; _todayActiveSec = 0;
        _db.SavePlayer(_player);
        using var c = _db.Connection.CreateCommand();
        c.CommandText = "DELETE FROM daily_stats"; c.ExecuteNonQuery();
    }

    public void Flush()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var am = (int)(_todayActiveSec / 60);
        var dxp = _todayKeys + (long)_todayDist + _todayClicks + am;

        // Save today running total to DB (overwrite)
        _db.SaveDailyStats(date, _todayKeys, _todayClicks, _todayDist, am, dxp);

        // Accumulate to player (these are deltas since we reset each time)
        _player.Xp = (_player.Xp - _player.TodayKeys - (long)_player.TodayDistance - _player.TodayClicks - _player.TodayActiveMin) + dxp;
        _player.Spirit = (_player.Spirit - _player.TodayKeys) + _todayKeys;
        _player.Agility = (_player.Agility - _player.TodayDistance) + _todayDist;
        _player.Alchemy = (_player.Alchemy - _player.TodayClicks) + _todayClicks;
        _player.Wisdom = (_player.Wisdom - _player.TodayActiveMin) + am;

        // Keep today display showing running totals
        _player.TodayKeys = _todayKeys;
        _player.TodayClicks = _todayClicks;
        _player.TodayDistance = _todayDist;
        _player.TodayActiveMin = am;

        _db.SavePlayer(_player);
    }

    private async Task LoopAsync()
    {
        while (await _reader.WaitToReadAsync(_cts.Token))
        {
            while (_reader.TryRead(out var evt))
            {
                var now = DateTime.Now;
                var today = now.ToString("yyyy-MM-dd");
                if (_lastDate != "" && _lastDate != today)
                {
                    Flush();
                    _todayKeys = 0; _todayClicks = 0; _todayDist = 0; _todayActiveSec = 0;
                    _player.TodayKeys = 0; _player.TodayClicks = 0; _player.TodayDistance = 0; _player.TodayActiveMin = 0;
                }
                _lastDate = today;

                if (_lastEvent != DateTime.MinValue)
                {
                    var gap = (now - _lastEvent).TotalSeconds;
                    if (gap <= 300) _todayActiveSec += Math.Min(gap, 300);
                }
                _lastEvent = now;
                switch (evt.Type)
                {
                    case EventType.Key: _todayKeys++; break;
                    case EventType.Click: _todayClicks++; break;
                    case EventType.Move:
                        if (_lastPos.HasValue)
                        {
                            var dx = evt.X - _lastPos.Value.X;
                            var dy = evt.Y - _lastPos.Value.Y;
                            _todayDist += Math.Sqrt(dx * dx + dy * dy) / 1000.0;
                        }
                        _lastPos = (evt.X, evt.Y);
                        break;
                }
            }
        }
    }

    public void Dispose() { _cts.Cancel(); _cts.Dispose(); }
}
