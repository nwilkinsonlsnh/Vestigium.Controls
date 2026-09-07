using System.Windows;

namespace Vestigium.Controls.PropertiesGrid.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
