using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;

namespace Ava.Services
{
    public class BarrierService : IBarrierService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService _loggingService;

        public BarrierService(HttpClient httpClient, ILoggingService loggingService)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
        }

        public async Task SendPulseAsync(string apiUrl, string barrierName)
        {
            try
            {
                _loggingService.Log($"Sending pulse for {barrierName}");
                var response = await _httpClient.PostAsync(apiUrl, null);
                if (response.IsSuccessStatusCode)
                {
                    _loggingService.Log($"Pulse sent successfully for {barrierName}");
                }
                else
                {
                    _loggingService.Log($"Pulse failed for {barrierName}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Log($"Pulse error for {barrierName}: {ex.Message}");
            }
        }
    }
}
