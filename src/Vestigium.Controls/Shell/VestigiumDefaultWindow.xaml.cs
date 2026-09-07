using System.Windows;

namespace Vestigium.Controls.Shell;

public partial class VestigiumDefaultWindow : Window
{
    public VestigiumDefaultWindow()
        : this(new VestigiumDefaultWindowViewModel())
    {
    }

    public VestigiumDefaultWindow(VestigiumDefaultWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
