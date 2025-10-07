using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;

namespace Ava.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private AppConfig _config;

    public SettingsViewModel(AppConfig config)
    {
        _config = config;
        LoadSettings();
    }

    public ObservableCollection<SettingItem> Settings { get; } = new();

    private void LoadSettings()
    {
        // App level settings
        AddSetting("Number Plates API URL", _config.NumberPlatesApiUrl);
        AddSetting("Number Plates Cron Expression", _config.NumberPlatesCronExpression);
        AddSetting("Whitelist IDs", string.Join(", ", _config.WhitelistIds ?? new()));
        AddSetting("Send Initial Pulse", _config.SendInitialPulse.ToString());
        AddSetting("Skip Initial Cron Pulse", _config.SkipInitialCronPulse.ToString());
        AddSetting("Perform Initial API Status Check", _config.PerformInitialApiStatusCheck.ToString());
        AddSetting("Autostart Number Plates", _config.AutostartNumberPlates.ToString());
        AddSetting("Start Open on Launch", _config.StartOpenOnLaunch.ToString());

        // Barriers
        AddSetting("Barriers Count", _config.Barriers.Count.ToString());
        foreach (var barrier in _config.Barriers.Barriers)
        {
            AddSetting($"Barrier {barrier.Key} - Cron Expression", barrier.Value.CronExpression);
            AddSetting($"Barrier {barrier.Key} - API URL", barrier.Value.ApiUrl);
            AddSetting($"Barrier {barrier.Key} - Lane ID", barrier.Value.LaneId.ToString());
            AddSetting($"Barrier {barrier.Key} - API Down Behavior", barrier.Value.ApiDownBehavior);
            AddSetting($"Barrier {barrier.Key} - Is Enabled", barrier.Value.IsEnabled.ToString());
        }
    }

    private void AddSetting(string name, string value)
    {
        Settings.Add(new SettingItem { Name = name, Value = value, IsUnset = IsUnset(value) });
    }

    private bool IsUnset(string value)
    {
        return string.IsNullOrEmpty(value) || value == "0" || value.ToLower() == "false";
    }
}

public class SettingItem : ReactiveObject
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    private bool _isUnset;
    public bool IsUnset
    {
        get => _isUnset;
        set => this.RaiseAndSetIfChanged(ref _isUnset, value);
    }
    public IBrush Color => IsUnset ? Brushes.Red : Brushes.Black;
}
