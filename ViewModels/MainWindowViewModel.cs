using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using Ava.Models;
using Ava.Repositories;
using Ava.Services;
using Avalonia.Media;

namespace Ava.ViewModels
{
        public class MainWindowViewModel : ReactiveObject
        {
            private string _logText = "Application started.\n";
            private bool _isAutoScrollEnabled = true;
            private string _numberPlateApiStatus = "Waiting";
            private IBrush _numberPlateApiColor = Brushes.Orange; // Amber for waiting

            public static MainWindowViewModel? Instance { get; private set; }

            public ObservableCollection<BarrierViewModel> Barriers { get; } = new();
            public ITransactionRepository TransactionRepository { get; }
            public INumberPlateService NumberPlateService { get; }
            public IBarrierService BarrierService { get; }
            public ISchedulingService SchedulingService { get; }
            public ILoggingService LoggingService { get; }

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
                set => this.RaiseAndSetIfChanged(ref _numberPlateApiColor, value);
            }

        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> FetchNumberPlatesCommand { get; }

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

            LoggingService.Log($"Config loaded, Count: {appConfig.Barriers.Count}, Barriers dict count: {appConfig.Barriers.Barriers.Count}");

            for (int i = 1; i <= appConfig.Barriers.Count; i++)
            {
                var barrierKey = $"Barrier{i}";
                if (appConfig.Barriers.Barriers.TryGetValue(barrierKey, out var barrierConfig))
                {
                    var barrierVm = new BarrierViewModel(barrierKey, barrierConfig.CronExpression, barrierConfig.ApiUrl, barrierConfig.LaneId, barrierConfig.ApiDownBehavior, BarrierService, LoggingService, NumberPlateService, TransactionRepository, AppStartupTime);
                    Barriers.Add(barrierVm);
                    LoggingService.Log($"Added barrier {barrierKey} with LaneId {barrierConfig.LaneId}, ApiDownBehavior {barrierConfig.ApiDownBehavior}");
                }
                else
                {
                    LoggingService.Log($"Barrier {barrierKey} not found");
                }
            }

            LoggingService.Log($"Total barriers loaded: {Barriers.Count}");

            // Initialize scheduling
            SchedulingService.Initialize(Barriers.AsEnumerable(), appConfig.NumberPlatesCronExpression, NumberPlateService);
        }

        private async void InitializeApplication()
        {
            TransactionRepository.InitializeDatabase();
            TransactionRepository.InsertSampleData();
            await SchedulingService.StartAsync();
        }

        public void Log(string message)
        {
            LoggingService.Log(message);
        }
    }
}
