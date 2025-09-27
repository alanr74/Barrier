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

        public async Task<bool> SendPulseAsync(string apiUrl, string barrierName)
        {
            try
            {
                var response = await _httpClient.PostAsync(apiUrl, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _loggingService.Log($"Pulse error for {barrierName}: {ex.Message}");
                return false;
            }
        }
    }
}
