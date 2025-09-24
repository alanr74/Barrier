using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ava.Models;

namespace Ava.Services
{
    public class NumberPlateService : INumberPlateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService _loggingService;
        private readonly string _apiUrl;
        private readonly string _apiDownBehavior;
        private readonly SemaphoreSlim _fetchSemaphore = new SemaphoreSlim(1, 1);

        private List<NumberPlateEntry> _numberPlates = new();
        private bool _allowAnyPlate;

        public bool AllowAnyPlate => _allowAnyPlate;

        public NumberPlateService(HttpClient httpClient, ILoggingService loggingService, string apiUrl, string apiDownBehavior)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
            _apiUrl = apiUrl;
            _apiDownBehavior = apiDownBehavior;
        }

        public async Task FetchNumberPlatesAsync()
        {
            await _fetchSemaphore.WaitAsync();
            try
            {
                _loggingService.Log("Fetching number plates from API...");
                var response = await _httpClient.GetAsync(_apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var newPlates = JsonSerializer.Deserialize<List<NumberPlateEntry>>(json);
                    if (newPlates != null)
                    {
                        _numberPlates.Clear();
                        _numberPlates.AddRange(newPlates);
                        _loggingService.Log($"Fetched {newPlates.Count} number plates.");
                    }
                    else
                    {
                        _loggingService.Log("Failed to deserialize number plates data.");
                    }
                    _allowAnyPlate = false;
                }
                else
                {
                    _loggingService.Log($"Failed to fetch number plates: {response.StatusCode}");
                    HandleApiDown();
                }
            }
            catch (Exception ex)
            {
                _loggingService.Log($"Error fetching number plates: {ex.Message}");
                HandleApiDown();
            }
            finally
            {
                _fetchSemaphore.Release();
            }
        }

        private void HandleApiDown()
        {
            switch (_apiDownBehavior)
            {
                case "UseHistoric":
                    // Keep existing data
                    _allowAnyPlate = false;
                    _loggingService.Log("API down, using historic data.");
                    break;
                case "DontOpen":
                    _numberPlates.Clear();
                    _allowAnyPlate = false;
                    _loggingService.Log("API down, DontOpen mode: cleared number plates.");
                    break;
                case "OpenAny":
                    _allowAnyPlate = true;
                    _loggingService.Log("API down, OpenAny mode: allowing any plate.");
                    break;
                default:
                    // Default to UseHistoric
                    _allowAnyPlate = false;
                    _loggingService.Log("API down, defaulting to use historic data.");
                    break;
            }
        }

        public bool IsValidPlate(string plate, int direction)
        {
            // Only check for In direction (1)
            if (direction != 1) return true; // Allow Out direction always

            if (_allowAnyPlate) return true;

            return _numberPlates.Any(p => p.Plate == plate && DateTime.Now >= p.Start && DateTime.Now <= p.Finish);
        }
    }
}
