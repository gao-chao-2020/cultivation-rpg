using System.Windows;

namespace CultivationRPG.Views;

public partial class NameDialog : Window
{
    public string PlayerName { get; private set; } = "";

    public NameDialog()
    {
        InitializeComponent();
        NameBox.Focus();
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        PlayerName = NameBox.Text.Trim();
        DialogResult = true;
        Close();
    }
}
