using System.IO;
using System.Threading.Channels;
using System.Windows;
using CultivationRPG.Helpers;
using CultivationRPG.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CultivationRPG;

public partial class App : Application
{
    private InputTrackerService? _tracker;
    private CultivationService? _cultivation;
    private DatabaseService? _db;
    private SingleInstance? _single;

    public CultivationService? Cultivation => _cultivation;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _single = new SingleInstance("Global\\CultivationRPG");
        if (!_single.IsFirstInstance)
        {
            MessageBox.Show("Cultivation RPG is already running.", "Cultivation RPG");
            Shutdown(); return;
        }

        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CultivationRPG", "player.db");
        _db = new DatabaseService(dbPath);

        var channel = Channel.CreateBounded<InputEvent>(new BoundedChannelOptions(10000) { FullMode = BoundedChannelFullMode.DropWrite });
        _tracker = new InputTrackerService(channel.Writer);
        _cultivation = new CultivationService(_db, channel.Reader);
        _tracker.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _cultivation?.Flush();
        _tracker?.Dispose();
        _cultivation?.Dispose();
        _db?.Dispose();
        _single?.Dispose();
        base.OnExit(e);
    }
}
