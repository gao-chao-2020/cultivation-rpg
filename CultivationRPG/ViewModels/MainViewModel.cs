using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CultivationRPG.Models;
using CultivationRPG.Services;

namespace CultivationRPG.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly CultivationService? _svc;
    private readonly SupabaseService _supabase = new();
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private DateTime _lastUpload = DateTime.MinValue;

    public MainViewModel()
    {
        _svc = ((App)System.Windows.Application.Current).Cultivation;
        RefreshCommand = new RelayCommand(Refresh);
        StartEditCommand = new RelayCommand(() => { EditName = PlayerName; IsEditing = true; });
        SaveNameCommand = new RelayCommand(SaveName);
        TabCommand = new RelayCommand<string>(t => TabIndex = int.Parse(t!));
        RebornCommand = new RelayCommand(Reborn);
        _timer = new System.Windows.Threading.DispatcherTimer(TimeSpan.FromSeconds(3),
            System.Windows.Threading.DispatcherPriority.Background, (_, _) => Refresh(),
            System.Windows.Application.Current.Dispatcher);
        _timer.Start();
        Refresh();
    }

    // ── Properties ──

    private string _name = "";
    public string PlayerName { get => _name; set { _name = value; OnPropertyChanged(); } }
    private string _realm = "";
    public string Realm { get => _realm; set { _realm = value; OnPropertyChanged(); } }
    private string _title = "";
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    private double _realmProgress;
    public double RealmProgress { get => _realmProgress; set { _realmProgress = value; OnPropertyChanged(); } }
    private string _xp = "0";
    public string Xp { get => _xp; set { _xp = value; OnPropertyChanged(); } }
    private string _xpToNext = "0";
    public string XpToNext { get => _xpToNext; set { _xpToNext = value; OnPropertyChanged(); } }
    private string _spirit = "0";
    public string Spirit { get => _spirit; set { _spirit = value; OnPropertyChanged(); } }
    private string _agility = "0";
    public string Agility { get => _agility; set { _agility = value; OnPropertyChanged(); } }
    private string _wisdom = "0";
    public string Wisdom { get => _wisdom; set { _wisdom = value; OnPropertyChanged(); } }
    private string _alchemy = "0";
    public string Alchemy { get => _alchemy; set { _alchemy = value; OnPropertyChanged(); } }
    private string _todayKeys = "0";
    public string TodayKeys { get => _todayKeys; set { _todayKeys = value; OnPropertyChanged(); } }
    private string _todayClicks = "0";
    public string TodayClicks { get => _todayClicks; set { _todayClicks = value; OnPropertyChanged(); } }
    private string _todayDist = "0";
    public string TodayDist { get => _todayDist; set { _todayDist = value; OnPropertyChanged(); } }
    private string _todayActive = "0";
    public string TodayActive { get => _todayActive; set { _todayActive = value; OnPropertyChanged(); } }
    private string _todayXp = "0";
    public string TodayXp { get => _todayXp; set { _todayXp = value; OnPropertyChanged(); } }
    private bool _isEditing;
    public bool IsEditing { get => _isEditing; set { _isEditing = value; OnPropertyChanged(); } }
    private string _editName = "";
    public string EditName { get => _editName; set { _editName = value; OnPropertyChanged(); } }

    public ObservableCollection<LeaderboardEntry> Leaderboard { get; } = new();
    public string MyRank { get; set; } = "未上榜";
    public bool CanReborn { get; set; }
    public ICommand RebornCommand { get; }

    private int _tabIndex;
    public int TabIndex { get => _tabIndex; set { _tabIndex = value; OnPropertyChanged(); _ = RefreshLeaderboardAsync(_svc?.Player!); } }
    public ICommand TabCommand { get; }

    public ICommand RefreshCommand { get; }
    public ICommand StartEditCommand { get; }
    public ICommand SaveNameCommand { get; }

    // ── Methods ──

    private bool _firstRefresh = true;
    public void Refresh()
    {
        if (_svc?.Player is not { } p) return;
        _svc.Flush();
        PlayerName = p.Name;
        var r = RealmInfo.GetRealm(p.Xp);
        var rx = RealmInfo.GetRealm(p.Xp);
        Realm = rx.Cn; Title = rx.Title;
        RealmProgress = RealmInfo.ProgressInRealm(p.Xp);
        Xp = FormatNum(p.Xp); XpToNext = FormatNum(RealmInfo.XpToNext(p.Xp));
        Spirit = FormatNum(p.Spirit);
        Agility = FormatDouble(p.Agility);
        Wisdom = FormatNum(p.Wisdom);
        Alchemy = FormatNum(p.Alchemy);
        TodayKeys = FormatNum(p.TodayKeys); TodayClicks = FormatNum(p.TodayClicks);
        TodayDist = FormatDouble(p.TodayDistance); TodayActive = FormatNum(p.TodayActiveMin);
        TodayXp = FormatNum((long)(p.TodayKeys + p.TodayDistance + p.TodayClicks + p.TodayActiveMin));

        if ((DateTime.Now - _lastUpload).TotalSeconds > 10)
        {
            _lastUpload = DateTime.Now;
            UploadAsync(p);
        }
    }

    private void Reborn()
    {
        var result = System.Windows.MessageBox.Show(
            "确定要重生吗？\n\n所有数据将清空，从头开始修炼。此操作不可撤销。",
            "重生", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;
        _svc?.Reborn();
        Refresh();
        UploadAsync(_svc!.Player);
    }

    private async void SaveName()
    {
        if (_svc?.Player is not { } p || string.IsNullOrWhiteSpace(EditName)) return;
        p.Name = EditName;
        PlayerName = EditName;
        IsEditing = false;
        try
        {
            await _supabase.UpsertAsync(p);
            await RefreshLeaderboardAsync(p);
        }
        catch { }
    }

    private async Task UploadAsync(Player p)
    {
        try
        {
            await _supabase.UpsertAsync(p);
            await RefreshLeaderboardAsync(p);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Upload failed: {ex.Message}");
        }
    }

    private async Task RefreshLeaderboardAsync(Player? p)
    {
        if (p == null) return;
        string? date = _tabIndex switch { 0 => DateTime.Now.ToString("yyyy-MM-dd"), 1 => DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"), _ => null };
        var list = await _supabase.FetchAsync(date, 20);
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Leaderboard.Clear();
            foreach (var e in list) Leaderboard.Add(e);
            var me = list.FirstOrDefault(e => e.PlayerName == p.Name);
            MyRank = me != null ? $"第{me.Rank}名" : "-";
            OnPropertyChanged(nameof(MyRank));
            OnPropertyChanged(nameof(Leaderboard));
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private static string FormatNum(long n) => n switch
    {
        >= 1_000_000_000_000 => $"{n / 1_000_000_000_000}兆",
        >= 100_000_000 => $"{n / 100_000_000}亿",
        >= 10_000 => $"{n / 10_000}万",
        _ => $"{n:N0}"
    };

    private static string FormatDouble(double d)
    {
        long n = (long)(d + 0.5); // round to nearest, matching Agility accumulation
        return n switch
        {
            >= 1_000_000_000_000 => $"{n / 1_000_000_000_000}兆",
            >= 100_000_000 => $"{n / 100_000_000}亿",
            >= 10_000 => $"{n / 10_000}万",
            _ => $"{n:N0}"
        };
    }
}

// ── Converters & Commands ──

public sealed class ProgressWidthConverter : System.Windows.Data.IValueConverter
{
    public static readonly ProgressWidthConverter Instance = new();
    public object Convert(object value, Type t, object p, System.Globalization.CultureInfo c) => value is double d ? d * 300 : 0;
    public object ConvertBack(object value, Type t, object p, System.Globalization.CultureInfo c) => throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();
    public object Convert(object value, Type t, object p, System.Globalization.CultureInfo c)
    {
        bool invert = p is string s && s == "invert";
        bool v = value is true;
        return (invert ? !v : v) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type t, object p, System.Globalization.CultureInfo c) => throw new NotSupportedException();
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _a;
    public RelayCommand(Action a) => _a = a;
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
    public bool CanExecute(object? p) => true;
    public void Execute(object? p) => _a();
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _a;
    public RelayCommand(Action<T?> a) => _a = a;
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
    public bool CanExecute(object? p) => true;
    public void Execute(object? p) => _a((T?)p);
}
