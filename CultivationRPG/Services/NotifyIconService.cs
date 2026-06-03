using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using FontStyle = System.Drawing.FontStyle;

namespace CultivationRPG.Services;

public sealed class NotifyIconService : IDisposable
{
    private readonly Window _window;
    private readonly NotifyIcon _icon;
    private bool _disposed;

    public NotifyIconService(Window window)
    {
        _window = window;
        _window.Closing += (_, e) => { if (!_disposed) { e.Cancel = true; _window.Hide(); _window.ShowInTaskbar = false; } };
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp); g.Clear(Color.Transparent);
        using var b = new SolidBrush(Color.FromArgb(0xC0, 0x40, 0xE0));
        g.FillEllipse(b, 2, 2, 28, 28);
        using var p = new Pen(Color.White, 2f); g.DrawEllipse(p, 2, 2, 28, 28);
        using var f = new Font("Segoe UI", 8f, FontStyle.Bold);
        g.DrawString("仙", f, Brushes.White, 6, 8);

        _icon = new NotifyIcon { Icon = Icon.FromHandle(bmp.GetHicon()), Text = "Cultivation RPG", Visible = true };
        _icon.DoubleClick += (_, _) => ShowWindow();
        _icon.BalloonTipTitle = "Cultivation RPG";
        _icon.BalloonTipText = "Running in background. Right-click to exit.";
        _icon.BalloonTipIcon = ToolTipIcon.Info;
        _icon.ShowBalloonTip(3000);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => ShowWindow());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => { _disposed = true; _icon.Visible = false; Application.Current.Shutdown(); });
        _icon.ContextMenuStrip = menu;
    }

    public void ShowWindow()
    {
        _window.Dispatcher.Invoke(() => { _window.ShowInTaskbar = true; _window.Show(); _window.WindowState = WindowState.Normal; _window.Activate(); });
    }

    public void Dispose() { if (!_disposed) { _disposed = true; _icon.Visible = false; _icon.Dispose(); } }
}
