using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;
using ReactiveUI;

public class MainWindowViewModel : ReactiveObject
{
    private string _logText = "Application started.\n";
    private bool _isAutoScrollEnabled = true;

    public static MainWindowViewModel Instance { get; private set; }

    public ObservableCollection<BarrierViewModel> Barriers { get; } = new();
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

    public event Action ScrollToBottomRequested;

    private IScheduler _scheduler;

    public MainWindowViewModel()
    {
        Instance = this;
        DatabaseHelper.InitializeDatabase();
        DatabaseHelper.InsertSampleData(); // For testing
        LoadConfiguration();
        SetupScheduler();
    }

    private void LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var appConfig = config.Get<AppConfig>();

        Log($"Config loaded, Count: {appConfig.Barriers.Count}, Barriers dict count: {appConfig.Barriers.Barriers.Count}");

        for (int i = 1; i <= appConfig.Barriers.Count; i++)
        {
            var barrierKey = $"Barrier{i}";
            if (appConfig.Barriers.Barriers.TryGetValue(barrierKey, out var barrierConfig))
            {
                var barrierVm = new BarrierViewModel(barrierKey, barrierConfig.CronExpression, barrierConfig.ApiUrl, barrierConfig.LaneId);
                Barriers.Add(barrierVm);
                Log($"Added barrier {barrierKey} with LaneId {barrierConfig.LaneId}");
            }
            else
            {
                Log($"Barrier {barrierKey} not found");
            }
        }

        Log($"Total barriers loaded: {Barriers.Count}");
    }

    private async void SetupScheduler()
    {
        var factory = new StdSchedulerFactory();
        _scheduler = await factory.GetScheduler();
        await _scheduler.Start();

        foreach (var barrier in Barriers)
        {
            var job = JobBuilder.Create<PulseJob>()
                .WithIdentity(barrier.Name)
                .UsingJobData("ApiUrl", barrier.ApiUrl)
                .UsingJobData("BarrierName", barrier.Name)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{barrier.Name}Trigger")
                .WithCronSchedule(barrier.CronExpression)
                .Build();

            await _scheduler.ScheduleJob(job, trigger);
        }
    }

    public void Log(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            LogText += $"{DateTime.Now}: {message}\n";
            if (IsAutoScrollEnabled)
            {
                ScrollToBottomRequested?.Invoke();
            }
        });
    }
}

public class PulseJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var barrierName = context.JobDetail.JobDataMap.GetString("BarrierName");

        var barrier = MainWindowViewModel.Instance.Barriers.FirstOrDefault(b => b.Name == barrierName);
        if (barrier != null && barrier.IsEnabled)
        {
            // Check if there is a pending transaction
            var transaction = DatabaseHelper.GetNextTransaction(barrier.LaneId, barrier.LastProcessedDate);
            if (transaction != null)
            {
                MainWindowViewModel.Instance.Log($"Cron timer fired for {barrierName}, processing transaction");
                await barrier.SendPulseAsync(true);
            }
            else
            {
                MainWindowViewModel.Instance.Log($"Cron timer fired for {barrierName}, no pending transactions");
            }
        }
    }
}
