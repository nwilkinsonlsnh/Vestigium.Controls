using System.Windows;

namespace Vestigium.Controls.StatusBar.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
