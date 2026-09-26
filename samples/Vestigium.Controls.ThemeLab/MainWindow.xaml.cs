using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Controls.ThemeLab;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ThemeBox.ItemsSource = App.Themes.AvailableThemes;
        ThemeBox.SelectedValue = App.Themes.Current?.Id ?? "StandardWPF";
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeBox.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
            return;
        if (App.Themes.Current?.Id == id)
            return;
        App.Themes.SwitchTheme(id);
    }
}
