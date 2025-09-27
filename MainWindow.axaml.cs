using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Ava.ViewModels;
using Ava.Repositories;
using Ava.Services;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;

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

        var appConfig = config.Get<Ava.AppConfig>();

        // Create services (in a real app, use DI container)
        var httpClient = new HttpClient();
        var transactionRepository = new TransactionRepository();
        var loggingService = new LoggingService();
        var barrierService = new BarrierService(httpClient, loggingService);
        var numberPlateService = new NumberPlateService(httpClient, loggingService, appConfig.NumberPlatesApiUrl, appConfig.ApiDownBehavior);
        var schedulingService = new SchedulingService();

        ViewModel = new Ava.ViewModels.MainWindowViewModel(
            transactionRepository,
            numberPlateService,
            barrierService,
            schedulingService,
            loggingService);

        // Set logging actions
        loggingService.LogAction = msg => ViewModel.LogText += msg;
        loggingService.ScrollAction = () => { if (ViewModel.IsAutoScrollEnabled) LogScrollViewer?.ScrollToEnd(); };

        DataContext = ViewModel;


    }
}
