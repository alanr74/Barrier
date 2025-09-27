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
        private readonly SemaphoreSlim _fetchSemaphore = new SemaphoreSlim(1, 1);

        private List<NumberPlateEntry> _numberPlates = new();
        private bool _allowAnyPlate;

        public bool AllowAnyPlate => _allowAnyPlate;

        public NumberPlateService(HttpClient httpClient, ILoggingService loggingService, string apiUrl)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
            _apiUrl = apiUrl;
        }

        public async Task<bool> FetchNumberPlatesAsync()
        {
            await _fetchSemaphore.WaitAsync();
            try
            {
                _loggingService.Log("Fetching number plates from API...");
                var response = await _httpClient.GetAsync(_apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var newPlates = JsonSerializer.Deserialize<List<NumberPlateEntry>>(json, options);
                    if (newPlates != null)
                    {
                        _numberPlates.Clear();
                        _numberPlates.AddRange(newPlates);
                        _loggingService.Log($"Fetched {newPlates.Count} number plates:");
                        foreach (var plate in newPlates)
                        {
                            _loggingService.Log($"  - {plate.Plate} (valid {plate.Start:yyyy-MM-dd HH:mm} to {plate.Finish:yyyy-MM-dd HH:mm})");
                        }
                    }
                    else
                    {
                        _loggingService.Log("Failed to deserialize number plates data.");
                    }
                    _allowAnyPlate = false;
                    return true;
                }
                else
                {
                    _loggingService.Log($"Failed to fetch number plates: {response.StatusCode}");
                    HandleApiDown();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Log($"Error fetching number plates: {ex.Message}");
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
