using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Ava;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;

        // Subscribe to auto-scroll event
        ViewModel.ScrollToBottomRequested += () => LogScrollViewer.ScrollToEnd();
    }
}
