using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Ava.ViewModels;
using Ava.Repositories;
using Ava.Services;
using System.Net.Http;

namespace Ava;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        // Create services (in a real app, use DI container)
        var httpClient = new HttpClient();
        var transactionRepository = new TransactionRepository();
        var loggingService = new LoggingService();
        var barrierService = new BarrierService(httpClient, loggingService);
        var numberPlateService = new NumberPlateService(httpClient, loggingService, "https://api.example.com/numberplates", "UseHistoric");
        var schedulingService = new SchedulingService();

        ViewModel = new Ava.ViewModels.MainWindowViewModel(
            transactionRepository,
            numberPlateService,
            barrierService,
            schedulingService,
            loggingService);

        // Set logging actions
        loggingService.LogAction = msg => ViewModel.LogText += msg;
        loggingService.ScrollAction = () => { if (ViewModel.IsAutoScrollEnabled) LogScrollViewer.ScrollToEnd(); };

        DataContext = ViewModel;


    }
}
