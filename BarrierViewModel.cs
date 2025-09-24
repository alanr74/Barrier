using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;

public class BarrierViewModel : ReactiveObject
{
    private bool _isEnabled;
    private IBrush _indicatorColor = Brushes.Gray;
    private string _lastNumberPlate = "No data";

    public string Name { get; }
    public string CronExpression { get; }
    public string ApiUrl { get; }
    public int LaneId { get; }
    public DateTime LastProcessedDate { get; set; } = DateTime.MinValue;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public IBrush IndicatorColor
    {
        get => _indicatorColor;
        set => this.RaiseAndSetIfChanged(ref _indicatorColor, value);
    }

    public string LastNumberPlate
    {
        get => _lastNumberPlate;
        set => this.RaiseAndSetIfChanged(ref _lastNumberPlate, value);
    }

    public ICommand SendPulseCommand { get; }

    public BarrierViewModel(string name, string cronExpression, string apiUrl, int laneId)
    {
        Name = name;
        CronExpression = cronExpression;
        ApiUrl = apiUrl;
        LaneId = laneId;
        SendPulseCommand = ReactiveCommand.CreateFromTask(() => SendPulseAsync(false));
    }

    public async Task SendPulseAsync(bool isCron = false)
    {
        if (isCron && !IsEnabled) return;

        // For cron, get next transaction; for manual, don't read db
        if (isCron)
        {
            var transaction = DatabaseHelper.GetNextTransaction(LaneId, LastProcessedDate);
            if (transaction != null)
            {
                LastNumberPlate = transaction.OcrPlate;
                LastProcessedDate = transaction.Created;
                MainWindowViewModel.Instance.Log($"Processing transaction for {Name}: {LastNumberPlate}");
            }
            else
            {
                MainWindowViewModel.Instance.Log($"No pending transactions for {Name}, skipping cron pulse");
                return;
            }
        }
        else
        {
            MainWindowViewModel.Instance.Log($"Manual pulse for {Name}");
        }

        MainWindowViewModel.Instance.Log($"Sending pulse for {Name}");

        try
        {
            using var client = new HttpClient();
            var response = await client.PostAsync(ApiUrl, null);
            if (response.IsSuccessStatusCode)
            {
                IndicatorColor = Brushes.Green;
                MainWindowViewModel.Instance.Log($"Pulse sent successfully for {Name}");
                // Reset after some time
                await Task.Delay(2000);
                IndicatorColor = Brushes.Gray;
            }
            else
            {
                IndicatorColor = Brushes.Red;
                MainWindowViewModel.Instance.Log($"Pulse failed for {Name}: {response.StatusCode}");
                await Task.Delay(2000);
                IndicatorColor = Brushes.Gray;
            }
        }
        catch (Exception ex)
        {
            IndicatorColor = Brushes.Red;
            MainWindowViewModel.Instance.Log($"Pulse error for {Name}: {ex.Message}");
            await Task.Delay(2000);
            IndicatorColor = Brushes.Gray;
        }
    }
}
