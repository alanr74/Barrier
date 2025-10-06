using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using Polly;
using Polly.Extensions.Http;

namespace Ava.Services
{
    public class BarrierService : IBarrierService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService _loggingService;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        public BarrierService(HttpClient httpClient, ILoggingService loggingService)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(3, (outcome, retryCount, context) =>
                {
                    _loggingService.LogWithColor($"Retry {retryCount} for API call failed: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}", Colors.Orange);
                });
        }

        public async Task<bool> SendPulseAsync(string apiUrl, string barrierName)
        {
            _loggingService.Log($"Sending pulse to {apiUrl} for {barrierName}");
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(apiUrl, null));
                if (response.IsSuccessStatusCode)
                {
                    _loggingService.LogWithColor($"Pulse sent successfully for {barrierName}", Colors.Green);
                }
                else
                {
                    _loggingService.LogWithColor($"Pulse failed for {barrierName}: {response.StatusCode}", Colors.Red);
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _loggingService.LogWithColor($"Pulse error for {barrierName}: {ex.Message}", Colors.Red);
                return false;
            }
        }

        public async Task<ApiStatus> CheckApiStatusAsync(string apiUrl, string barrierName)
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(apiUrl));
                if (response.IsSuccessStatusCode)
                {
                    return ApiStatus.Up;
                }
                else
                {
                    _loggingService.LogWithColor($"API status check failed for {barrierName}: {response.StatusCode}", Colors.Red);
                    return ApiStatus.Down;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogWithColor($"API status check error for {barrierName}: {ex.Message}", Colors.Red);
                return ApiStatus.Down;
            }
        }
    }
}
