using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using Ava.Models;
using Ava.Repositories;
using Ava.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Ava.ViewModels
{
    public enum Status
    {
        Green,
        Amber,
        Red
    }

    public class LogEntry : ReactiveObject
    {
        private Color _color;
        private IBrush _colorBrush;

        public string Text { get; }
        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    this.RaisePropertyChanged(nameof(Color));
                    _colorBrush = new SolidColorBrush(value);
                    this.RaisePropertyChanged(nameof(ColorBrush));
                }
            }
        }
        public IBrush ColorBrush => _colorBrush;

        public LogEntry(string text, Color color)
        {
            Text = text;
            Color = color;
            _colorBrush = new SolidColorBrush(color);
        }
    }

        public class MainWindowViewModel : ReactiveObject
        {
        private string _logText = "Application started.\n";
            private bool _isAutoScrollEnabled = true;
            private string _numberPlateApiStatus = "Waiting";
            private IBrush _numberPlateApiColor = Brushes.Orange; // Amber for waiting

            public static MainWindowViewModel? Instance { get; private set; }

            public ObservableCollection<BarrierViewModel> Barriers { get; } = new();
            public ObservableCollection<LogEntry> LogEntries { get; } = new();
            public ITransactionRepository TransactionRepository { get; }
            public INumberPlateService NumberPlateService { get; }
            public IBarrierService BarrierService { get; }
            public ISchedulingService SchedulingService { get; }
            public ILoggingService LoggingService { get; }

            private bool _sendInitialPulse;
            private bool _skipInitialCronPulse;
            private bool _autostartNumberPlates;

            public static DateTime AppStartupTime { get; private set; }

            public string LogText
            {
                get => _logText;
                set => this.RaiseAndSetIfChanged(ref _logText, value);
            }

            public bool IsAutoScrollEnabled
            {
                get => _isAutoScrollEnabled;
                set => this.RaiseAndSetIfChanged(ref _isAutoScrollEnabled, value);
            }

            public string NumberPlateApiStatus
            {
                get => _numberPlateApiStatus;
                set => this.RaiseAndSetIfChanged(ref _numberPlateApiStatus, value);
            }

            public IBrush NumberPlateApiColor
            {
                get => _numberPlateApiColor;
                set
                {
                    this.RaiseAndSetIfChanged(ref _numberPlateApiColor, value);
                    UpdateOverallStatus();
                }
            }

            public bool SkipInitialCronPulse
            {
                get => _skipInitialCronPulse;
                set => this.RaiseAndSetIfChanged(ref _skipInitialCronPulse, value);
            }

            private Status _overallStatus = Status.Amber; // Initial amber

            public Status OverallStatus
            {
                get => _overallStatus;
                set => this.RaiseAndSetIfChanged(ref _overallStatus, value);
            }

        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> FetchNumberPlatesCommand { get; }
        public ReactiveCommand<LogEntry, System.Reactive.Unit> CopyLogEntryCommand { get; }

        public MainWindowViewModel(
                ITransactionRepository transactionRepository,
                INumberPlateService numberPlateService,
                IBarrierService barrierService,
                ISchedulingService schedulingService,
                ILoggingService loggingService)
            {
                AppStartupTime = DateTime.Now;

                Instance = this;
                TransactionRepository = transactionRepository;
                NumberPlateService = numberPlateService;
                BarrierService = barrierService;
                SchedulingService = schedulingService;
                LoggingService = loggingService;

                LoggingService.LogWithColorAction = (message, color) => LogEntries.Add(new LogEntry(message, color));

                FetchNumberPlatesCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    NumberPlateApiStatus = "Fetching...";
                    NumberPlateApiColor = Brushes.Orange;
                    var success = await NumberPlateService.FetchNumberPlatesAsync();
                    if (success)
                    {
                        NumberPlateApiStatus = "Success";
                        NumberPlateApiColor = Brushes.Green;
                    }
                    else
                    {
                        NumberPlateApiStatus = "Failed";
                        NumberPlateApiColor = Brushes.Red;
                    }
                });

                CopyLogEntryCommand = ReactiveCommand.Create<LogEntry>(async entry =>
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var desktop = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                        var clipboard = desktop?.MainWindow?.Clipboard;
                        if (clipboard != null)
                        {
                            clipboard.SetTextAsync(entry.Text);
                        }
                    });
                });

                LoadConfiguration();
                InitializeApplication();
            }

        private void LoadConfiguration()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

                var appConfig = config.Get<AppConfig>() ?? new AppConfig();
                _sendInitialPulse = appConfig.SendInitialPulse;
                var performInitialApiStatusCheck = appConfig.PerformInitialApiStatusCheck;
                _skipInitialCronPulse = appConfig.SkipInitialCronPulse;
                _autostartNumberPlates = appConfig.AutostartNumberPlates;

                Console.WriteLine($"Loaded SendInitialPulse: {appConfig.SendInitialPulse}");

                LoggingService.Log($"Config loaded, Count: {appConfig.Barriers.Count}, Barriers dict count: {appConfig.Barriers.Barriers.Count}");

            for (int i = 1; i <= appConfig.Barriers.Count; i++)
            {
                var barrierKey = $"Barrier{i}";
                if (appConfig.Barriers.Barriers.TryGetValue(barrierKey, out var barrierConfig))
                {
                    var barrierVm = new BarrierViewModel(barrierKey, barrierConfig.CronExpression, barrierConfig.ApiUrl, barrierConfig.LaneId, barrierConfig.ApiDownBehavior, barrierConfig.IsEnabled, BarrierService, LoggingService, NumberPlateService, TransactionRepository, AppStartupTime);
                    Barriers.Add(barrierVm);
                    if (performInitialApiStatusCheck)
                    {
                        _ = barrierVm.UpdateApiStatusAsync(); // Initial status check
                    }
                    LoggingService.Log($"Added barrier {barrierKey} with LaneId {barrierConfig.LaneId}, ApiDownBehavior {barrierConfig.ApiDownBehavior}");
                }
                else
                {
                    LoggingService.Log($"Barrier {barrierKey} not found");
                }
            }

            LoggingService.Log($"Total barriers loaded: {Barriers.Count}");

            // Subscribe to barrier status changes
            foreach (var barrier in Barriers)
            {
                barrier.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "ApiStatusColor")
                    {
                        UpdateOverallStatus();
                    }
                };
            }

            // Initial status update
            UpdateOverallStatus();

            // Initialize scheduling
            SchedulingService.Initialize(Barriers.AsEnumerable(), appConfig.NumberPlatesCronExpression, NumberPlateService);
        }

        private async void InitializeApplication()
        {
            TransactionRepository.InitializeDatabase();
            TransactionRepository.InsertSampleData();
            await SchedulingService.StartAsync();

            LoggingService.Log($"AutostartNumberPlates config: {_autostartNumberPlates}");

            if (_autostartNumberPlates)
            {
                _ = FetchNumberPlatesCommand.Execute();
            }

            Console.WriteLine($"SendInitialPulse config: {_sendInitialPulse}");

            LoggingService.Log($"SendInitialPulse config: {_sendInitialPulse}");

            if (_sendInitialPulse)
            {
                foreach (var barrier in Barriers)
                {
                    try
                    {
                        var success = await barrier.SendPulseAsync(false);
                        if (success)
                        {
                            LoggingService.LogWithColor($"Initial pulse sent successfully for {barrier.Name}", Colors.Green);
                        }
                        else
                        {
                            LoggingService.LogWithColor($"Initial pulse failed for {barrier.Name}", Colors.Red);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogWithColor($"Initial pulse error for {barrier.Name}: {ex.Message}", Colors.Red);
                    }
                }
            }
        }

        private void UpdateOverallStatus()
        {
            bool hasRed = Barriers.Any(b => b.ApiStatusColor == Brushes.Red) || NumberPlateApiColor == Brushes.Red;
            if (hasRed)
            {
                OverallStatus = Status.Red;
                return;
            }
            bool hasAmber = Barriers.Any(b => b.ApiStatusColor == Brushes.Orange) || NumberPlateApiColor == Brushes.Orange;
            if (hasAmber)
            {
                OverallStatus = Status.Amber;
                return;
            }
            OverallStatus = Status.Green;
        }

        private void UpdateLogColors(bool isDark)
        {
            foreach (var entry in LogEntries)
            {
                if (isDark)
                {
                    if (entry.Color == Colors.Black) entry.Color = Colors.LightGray;
                    else if (entry.Color == Colors.DarkGreen) entry.Color = Colors.LightGreen;
                    else if (entry.Color == Colors.DarkRed) entry.Color = Colors.LightCoral;
                    else if (entry.Color == Colors.DarkOrange) entry.Color = Colors.Yellow;
                }
                else
                {
                    if (entry.Color == Colors.LightGray) entry.Color = Colors.Black;
                    else if (entry.Color == Colors.LightGreen) entry.Color = Colors.DarkGreen;
                    else if (entry.Color == Colors.LightCoral) entry.Color = Colors.DarkRed;
                    else if (entry.Color == Colors.Yellow) entry.Color = Colors.DarkOrange;
                }
                // Add more mappings as needed
            }
        }

        public void Log(string message)
        {
            LoggingService.Log(message);
        }
    }
}
