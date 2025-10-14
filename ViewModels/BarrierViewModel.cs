using System;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;
using Ava.Models;
using Ava.Repositories;
using Ava.Services;

namespace Ava.ViewModels
{
    public class BarrierViewModel : ReactiveObject
    {
        private bool _isEnabled;
        private bool _isSendingPulse;
        private IBrush _indicatorColor = Brushes.Orange; // Amber for unknown
        private string _lastNumberPlate = "No data";

        public string Name { get; }
        public string CronExpression { get; }
        public string ApiUrl { get; }
        public int LaneId { get; }
        public string ApiDownBehavior { get; }
        public DateTime LastProcessedDate { get; set; } = DateTime.MinValue;
        public BarrierConfig? BarrierConfig { get; set; }

        private readonly IBarrierService _barrierService;
        private readonly ILoggingService _loggingService;
        private readonly INumberPlateService _numberPlateService;
        private readonly ITransactionRepository _transactionRepository;
        private readonly SemaphoreSlim _pulseSemaphore = new SemaphoreSlim(1, 1);

        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        public bool IsSendingPulse
        {
            get => _isSendingPulse;
            set => this.RaiseAndSetIfChanged(ref _isSendingPulse, value);
        }

        public IBrush IndicatorColor
        {
            get => _indicatorColor;
            set => this.RaiseAndSetIfChanged(ref _indicatorColor, value);
        }

        private IBrush _apiStatusColor = Brushes.Orange; // Amber for unknown

        public IBrush ApiStatusColor
        {
            get => _apiStatusColor;
            set => this.RaiseAndSetIfChanged(ref _apiStatusColor, value);
        }

        public string LastNumberPlate
        {
            get => _lastNumberPlate;
            set => this.RaiseAndSetIfChanged(ref _lastNumberPlate, value);
        }

        public ICommand SendPulseCommand { get; }

        public BarrierViewModel(
            string name,
            string cronExpression,
            string apiUrl,
            int laneId,
            string apiDownBehavior,
            bool initialEnabled,
            IBarrierService barrierService,
            ILoggingService loggingService,
            INumberPlateService numberPlateService,
            ITransactionRepository transactionRepository,
            DateTime startupTime)
        {
            Name = name;
            CronExpression = cronExpression;
            ApiUrl = apiUrl;
            LaneId = laneId;
            ApiDownBehavior = apiDownBehavior;
            IsEnabled = initialEnabled;
            _barrierService = barrierService;
            _loggingService = loggingService;
            _numberPlateService = numberPlateService;
            _transactionRepository = transactionRepository;

            LastProcessedDate = startupTime;

            SendPulseCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsSendingPulse = true;
                try
                {
                    await SendPulseAsync(false);
                }
                finally
                {
                    IsSendingPulse = false;
                }
            }, this.WhenAnyValue(x => x.IsSendingPulse).Select(b => !b));
        }

        public async Task UpdateApiStatusAsync()
        {
            var status = await _barrierService.CheckApiStatusAsync(ApiUrl, Name);
            switch (status)
            {
                case ApiStatus.Up:
                    ApiStatusColor = Brushes.Green;
                    break;
                case ApiStatus.Down:
                    ApiStatusColor = Brushes.Red;
                    break;
                case ApiStatus.Unknown:
                    ApiStatusColor = Brushes.Orange;
                    break;
            }
        }

        public async Task<bool> SendPulseAsync(bool isCron = false, string pulseSource = "Manual")
        {
            await _pulseSemaphore.WaitAsync();
            try
            {
                Console.WriteLine($"SendPulseAsync called for {Name}, isCron: {isCron} at {DateTime.Now}");

                if (isCron && !IsEnabled) return false;

                Transaction? transaction = null;

                // For cron, get next transaction; for manual, don't read db
                if (isCron)
                {
                    transaction = _transactionRepository.GetNextTransaction(LaneId, LastProcessedDate);
                    if (transaction != null)
                    {
                        LastNumberPlate = transaction.OcrPlate;
                        LastProcessedDate = transaction.Created;
                        _loggingService.Log($"Processing transaction for {Name}: {LastNumberPlate}");

                        // For In (direction 1), check plate validity; for Out (0), always pulse
                        if (transaction.Direction == 1)
                        {
                            if (!_numberPlateService.IsValidPlate(transaction.OcrPlate, transaction.Direction, ApiDownBehavior))
                            {
                                var reason = _numberPlateService.GetValidationReason(transaction.OcrPlate, transaction.Direction, ApiDownBehavior);
                                _loggingService.LogWithColor($"Invalid plate '{transaction.OcrPlate}' for In transaction on {Name}, skipping pulse. Reason: {reason ?? "Unknown validation error"}", Colors.Red);
                                return false;
                            }
                            _loggingService.LogWithColor($"Valid In transaction for plate '{transaction.OcrPlate}', sending pulse for {Name}", Colors.Green);
                        }
                        else
                        {
                            _loggingService.Log($"Out transaction, sending pulse for {Name}");
                        }
                    }
                    else
                    {
                        _loggingService.Log($"No pending transactions for {Name}, skipping cron pulse");
                        return false;
                    }
                }
                else
                {
                }

                var success = await _barrierService.SendPulseAsync(ApiUrl, Name, isCron ? 3 : 0);
                if (success)
                {
                    IndicatorColor = Brushes.Green; // Green for working
                    _loggingService.LogWithColor($"{pulseSource} pulse sent successfully for {Name}", Colors.Green);
                }
                else
                {
                    IndicatorColor = Brushes.Red; // Red for error
                    _loggingService.LogWithColor($"{pulseSource} pulse failed for {Name}", Colors.Red);
                }

                // Update API status after pulse
                await UpdateApiStatusAsync();

                return success;
            }
            finally
            {
                _pulseSemaphore.Release();
            }
        }
    }
}
