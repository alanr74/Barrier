using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ava.Models;
using Avalonia.Media;
using Polly;
using Polly.Extensions.Http;

namespace Ava.Services
{
    public class NumberPlateService : INumberPlateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService _loggingService;
        private readonly string _apiUrl;
        private readonly List<WhitelistCredential> _whitelistCredentials;
        private readonly SemaphoreSlim _fetchSemaphore = new SemaphoreSlim(1, 1);
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        private List<NumberPlateEntry> _numberPlates = new();
        private bool _allowAnyPlate;

        public bool AllowAnyPlate => _allowAnyPlate;

        public NumberPlateService(HttpClient httpClient, ILoggingService loggingService, string apiUrl, List<WhitelistCredential> whitelistCredentials)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
            _apiUrl = apiUrl;
            _whitelistCredentials = whitelistCredentials;
        }

        public async Task<bool> FetchNumberPlatesAsync()
        {
            await _fetchSemaphore.WaitAsync();
            try
            {
                _loggingService.Log("Fetching number plates from API...");
                var allPlates = new List<NumberPlateEntry>();
                var hasAnySuccess = false;

                foreach (var credential in _whitelistCredentials)
                {
                    try
                    {
                        var url = _apiUrl.Replace("{id}", credential.Id);
                        _loggingService.Log($"Fetching whitelist for {credential.Id} from {url}");

                        // Create HttpClient with specific basic auth for this whitelist ID
                        var client = new HttpClient();

                        // Create per-ID retry policy with URL in context for better error logging
                        var retryPolicy = HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .RetryAsync(3, (outcome, retryCount, context) =>
                            {
                                _loggingService.LogWithColor($"Retry {retryCount} for number plates fetch on {url} failed: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}", Colors.Orange);
                            });

                        var authToken = Convert.ToBase64String(
                            System.Text.Encoding.ASCII.GetBytes($"{credential.Username}:{credential.Password}")
                        );
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

                        var response = await retryPolicy.ExecuteAsync(() => client.GetAsync(url));

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            var plates = JsonSerializer.Deserialize<List<NumberPlateEntry>>(json, options);
                            if (plates != null && plates.Any())
                            {
                                allPlates.AddRange(plates);
                                _loggingService.LogWithColor($"Fetched {plates.Count} plates for {credential.Id}", Colors.Green);
                                hasAnySuccess = true;
                            }
                            else
                            {
                                _loggingService.LogWithColor($"No plates returned for {credential.Id}", Colors.Orange);
                            }
                        }
                        else
                        {
                            _loggingService.LogWithColor($"Failed to fetch plates for {credential.Id}: {response.StatusCode}", Colors.Red);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWithColor($"Error fetching plates for {credential.Id}: {ex.Message}", Colors.Red);
                    }
                }

                // Update the global list
                _numberPlates.Clear();
                _numberPlates.AddRange(allPlates);
                _loggingService.LogWithColor($"Total fetched: {allPlates.Count} number plates across all whitelists", Colors.Green);
                foreach (var plate in allPlates)
                {
                    _loggingService.Log($"  - {plate.Plate} (valid {plate.Start:yyyy-MM-dd HH:mm} to {plate.Finish:yyyy-MM-dd HH:mm})");
                }

                _allowAnyPlate = false;
                return hasAnySuccess;
            }
            catch (Exception ex)
            {
                _loggingService.LogWithColor($"Critical error fetching number plates: {ex.Message}", Colors.Red);
                HandleApiDown();
                return false;
            }
            finally
            {
                _fetchSemaphore.Release();
            }
        }

        private void HandleApiDown()
        {
            // Global fallback for when API is down, but per-barrier behavior overrides in validation
            _allowAnyPlate = false;
            _loggingService.Log("API down, using historic data globally (per-barrier behavior applies in validation).");
        }

        public bool IsValidPlate(string plate, int direction, string apiDownBehavior)
        {
            // Only check for In direction (1)
            if (direction != 1) return true; // Allow Out direction always

            switch (apiDownBehavior)
            {
                case "OpenAny":
                    return true;
                case "DontOpen":
                    return false;
                case "UseHistoric":
                    if (_allowAnyPlate) return true;
                    return _numberPlates.Any(p => p.Plate == plate && DateTime.Now >= p.Start && DateTime.Now <= p.Finish);
                default:
                    // Default to UseHistoric
                    if (_allowAnyPlate) return true;
                    return _numberPlates.Any(p => p.Plate == plate && DateTime.Now >= p.Start && DateTime.Now <= p.Finish);
            }
        }

        public string? GetValidationReason(string plate, int direction, string apiDownBehavior)
        {
            if (direction != 1) return null; // No validation for Out

            switch (apiDownBehavior)
            {
                case "OpenAny":
                    return null;
                case "DontOpen":
                    return "API down, DontOpen mode: barrier not opened";
                case "UseHistoric":
                    if (_allowAnyPlate) return null;

                    var matchingPlate = _numberPlates.FirstOrDefault(p => p.Plate == plate);
                    if (matchingPlate == null)
                    {
                        return $"Plate '{plate}' not found in authorized list";
                    }

                    if (DateTime.Now < matchingPlate.Start)
                    {
                        return $"Plate '{plate}' not yet valid (starts {matchingPlate.Start:yyyy-MM-dd HH:mm})";
                    }

                    if (DateTime.Now > matchingPlate.Finish)
                    {
                        return $"Plate '{plate}' expired (ended {matchingPlate.Finish:yyyy-MM-dd HH:mm})";
                    }

                    return null; // Should not reach here if IsValidPlate is false
                default:
                    // Default to UseHistoric
                    if (_allowAnyPlate) return null;

                    var matchingPlateDefault = _numberPlates.FirstOrDefault(p => p.Plate == plate);
                    if (matchingPlateDefault == null)
                    {
                        return $"Plate '{plate}' not found in authorized list";
                    }

                    if (DateTime.Now < matchingPlateDefault.Start)
                    {
                        return $"Plate '{plate}' not yet valid (starts {matchingPlateDefault.Start:yyyy-MM-dd HH:mm})";
                    }

                    if (DateTime.Now > matchingPlateDefault.Finish)
                    {
                        return $"Plate '{plate}' expired (ended {matchingPlateDefault.Finish:yyyy-MM-dd HH:mm})";
                    }

                    return null;
            }
        }
    }
}
