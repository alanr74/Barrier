using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Ava.ViewModels;

namespace Ava;

public partial class SettingsWindow : ReactiveWindow<SettingsViewModel>
{
    public SettingsWindow(AppConfig config)
    {
        ViewModel = new SettingsViewModel(config);
    }
}
