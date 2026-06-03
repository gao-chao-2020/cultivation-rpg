using System.Windows;
using CultivationRPG.Services;

namespace CultivationRPG;

public partial class MainWindow : Window
{
    private readonly NotifyIconService _tray;

    public MainWindow()
    {
        InitializeComponent();
        _tray = new NotifyIconService(this);
        Loaded += (_, _) => StyleTabButtons();
    }

    private readonly System.Windows.Media.SolidColorBrush _activeBg = new(System.Windows.Media.Color.FromRgb(0x42, 0x7B, 0xD4));
    private readonly System.Windows.Media.SolidColorBrush _hoverBg = new(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x50));
    private readonly System.Windows.Media.SolidColorBrush _normalFg = new(System.Windows.Media.Color.FromRgb(0xAA, 0xAA, 0xCC));
    private readonly System.Windows.Media.SolidColorBrush _activeFg = System.Windows.Media.Brushes.White;

    private void StyleTabButtons()
    {
        foreach (System.Windows.Controls.Button btn in TabPanel.Children)
        {
            btn.FontSize = 11;
            btn.Width = 44;
            btn.Height = 26;
            btn.Cursor = System.Windows.Input.Cursors.Hand;
            btn.BorderThickness = new System.Windows.Thickness(0);
            btn.Foreground = _normalFg;
            btn.Background = System.Windows.Media.Brushes.Transparent;
            btn.MouseEnter += (_, _) => { if (btn.Tag?.ToString() != _activeTab) btn.Background = _hoverBg; };
            btn.MouseLeave += (_, _) => { if (btn.Tag?.ToString() != _activeTab) btn.Background = System.Windows.Media.Brushes.Transparent; };
        }
    }

    private string _activeTab = "0";

    private void OnTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || DataContext is not ViewModels.MainViewModel vm) return;
        _activeTab = (string)btn.Tag;
        vm.TabIndex = int.Parse(_activeTab);
        foreach (System.Windows.Controls.Button child in TabPanel.Children)
        {
            var active = child == btn;
            child.Background = active ? _activeBg : System.Windows.Media.Brushes.Transparent;
            child.Foreground = active ? _activeFg : _normalFg;
        }
    }
}
