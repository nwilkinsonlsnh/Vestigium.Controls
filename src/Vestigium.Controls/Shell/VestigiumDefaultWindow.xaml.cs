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
        viewModel.RootShell = RootShell;
        Loaded += (_, _) => viewModel.RootShell = RootShell;
    }

    public VestigiumDefaultWindowViewModel ViewModel { get; }

    public VestigiumShell HostShell => RootShell;

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
