using System.Windows;
using Vestigium.Controls.Shell;

namespace Vestigium.Controls.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(new GalleryViewModel())
    {
    }

    public MainWindow(GalleryViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.RootShell = LiveShell;
        viewModel.SlotCount = LiveShell.NavItems.Count;
        Loaded += (_, _) =>
        {
            viewModel.RootShell = LiveShell;
            viewModel.SlotCount = LiveShell.NavItems.Count;
            NestedSample.ApplySpec(new VestigiumShellSpec
            {
                IsSubShell = true,
                ShowStatusBar = false,
                ShellDepth = 1,
                NavIndent = viewModel.NavIndent,
                Items =
                {
                    new VestigiumNavItemSpec("Overview") { Subject = "Nested Overview", Description = "This inner form never gets File / View." },
                    new VestigiumNavItemSpec("Activity") { Subject = "Nested Activity" },
                    new VestigiumNavItemSpec("Alerts") { Subject = "Nested Alerts" }
                }
            });
        };
    }

    public GalleryViewModel ViewModel { get; }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
