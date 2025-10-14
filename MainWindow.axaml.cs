using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Ava.ViewModels;
using Ava.Repositories;
using Ava.Services;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.ComponentModel;
using System.Linq;

namespace Ava;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        // Read config
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var appConfig = config.Get<Ava.AppConfig>() ?? new AppConfig();

        // Create services (in a real app, use DI container)
        var httpClient = new HttpClient();
        var serviceConfig = new Config();
        var transactionRepository = new TransactionRepository(serviceConfig);
        var loggingService = new LoggingService();
        var barrierService = new BarrierService(httpClient, loggingService, appConfig.DebugMode, appConfig.NoRelayCalls);
        var numberPlateService = new NumberPlateService(httpClient, loggingService, appConfig.NumberPlatesApiUrl, appConfig.WhitelistCredentials);
        var schedulingService = new SchedulingService();

        ViewModel = new Ava.ViewModels.MainWindowViewModel(
            transactionRepository,
            numberPlateService,
            barrierService,
            schedulingService,
            loggingService);

        // Set logging actions
        loggingService.ScrollAction = () => { if (ViewModel.IsAutoScrollEnabled && ViewModel.LogEntries.Any()) LogListBox!.ScrollIntoView(ViewModel.LogEntries[^1]); };

        DataContext = ViewModel;

        // Handle closing to hide instead of exit
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        ShowInTaskbar = false;
    }
}
