using System;
using System.Linq;
using System.Net.Http;
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
        private IBrush _indicatorColor = Brushes.Orange; // Amber for unknown
        private string _lastNumberPlate = "No data";

        public string Name { get; }
        public string CronExpression { get; }
        public string ApiUrl { get; }
        public int LaneId { get; }
        public DateTime LastProcessedDate { get; set; } = DateTime.MinValue;

        private readonly IBarrierService _barrierService;
        private readonly ILoggingService _loggingService;
        private readonly INumberPlateService _numberPlateService;
        private readonly ITransactionRepository _transactionRepository;

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

        public BarrierViewModel(
            string name,
            string cronExpression,
            string apiUrl,
            int laneId,
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
            _barrierService = barrierService;
            _loggingService = loggingService;
            _numberPlateService = numberPlateService;
            _transactionRepository = transactionRepository;

            LastProcessedDate = startupTime;

            SendPulseCommand = ReactiveCommand.CreateFromTask(() => SendPulseAsync(false));
        }

        public async Task SendPulseAsync(bool isCron = false)
        {
            if (isCron && !IsEnabled) return;

            // For cron, get next transaction; for manual, don't read db
            if (isCron)
            {
                var transaction = _transactionRepository.GetNextTransaction(LaneId, LastProcessedDate);
                if (transaction != null)
                {
                    LastNumberPlate = transaction.OcrPlate;
                    LastProcessedDate = transaction.Created;
                    _loggingService.Log($"Processing transaction for {Name}: {LastNumberPlate}");

                    // For In (direction 1), check plate validity; for Out (0), always pulse
                    if (transaction.Direction == 1)
                    {
                        if (!_numberPlateService.AllowAnyPlate && !_numberPlateService.IsValidPlate(transaction.OcrPlate, transaction.Direction))
                        {
                            var reason = _numberPlateService.GetValidationReason(transaction.OcrPlate, transaction.Direction);
                            _loggingService.Log($"Invalid plate '{transaction.OcrPlate}' for In transaction on {Name}, skipping pulse. Reason: {reason ?? "Unknown validation error"}");
                            return;
                        }
                        _loggingService.Log($"Valid In transaction for plate '{transaction.OcrPlate}', sending pulse for {Name}");
                    }
                    else
                    {
                        _loggingService.Log($"Out transaction, sending pulse for {Name}");
                    }
                }
                else
                {
                    _loggingService.Log($"No pending transactions for {Name}, skipping cron pulse");
                    return;
                }
            }
            else
            {
                _loggingService.Log($"Manual pulse for {Name}");
            }

            _loggingService.Log($"Sending pulse for {Name}");

            var success = await _barrierService.SendPulseAsync(ApiUrl, Name);
            if (success)
            {
                IndicatorColor = Brushes.Green; // Green for working
                _loggingService.Log($"Pulse sent successfully for {Name}");
            }
            else
            {
                IndicatorColor = Brushes.Red; // Red for error
                _loggingService.Log($"Pulse failed for {Name}");
            }
        }
    }
}
