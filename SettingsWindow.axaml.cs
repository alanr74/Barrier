using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Ava.ViewModels;

namespace Ava;

public partial class SettingsWindow : ReactiveWindow<SettingsViewModel>
{
    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        ViewModel = new SettingsViewModel(config);
    }
}
