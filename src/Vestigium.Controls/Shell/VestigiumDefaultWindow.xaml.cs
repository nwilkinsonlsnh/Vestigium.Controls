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
        ViewModel = viewModel;
        DataContext = viewModel;
        viewModel.Shell = RootShell;
        Loaded += (_, _) => viewModel.Shell = RootShell;
    }

    public VestigiumDefaultWindowViewModel ViewModel { get; }

    public VestigiumShell Shell => RootShell;

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
