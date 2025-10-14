using Microsoft.AspNetCore.Mvc;
using Ava.Repositories;
using Ava.Services;
using Ava.ViewModels;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Ava;
using Avalonia.Media;

namespace Ava.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CameraController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILoggingService _loggingService;
        private readonly Config _config;

        public CameraController(ITransactionRepository transactionRepository, ILoggingService loggingService, Config config)
        {
            _transactionRepository = transactionRepository;
            _loggingService = loggingService;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> PostCameraData([FromBody] JsonElement data)
        {
            try
            {
                if (data.TryGetProperty("vrmMessages", out var vrmMessages) && vrmMessages.ValueKind == JsonValueKind.Array)
                {
                    // Handle multiple records
                    var messages = JsonSerializer.Deserialize<List<CameraMessage>>(vrmMessages.GetRawText())!;
                    foreach (var message in messages)
                    {
                        await _transactionRepository.AddCameraDataAsync(message);
                        if (_config.PulseTriggerMode == "camera")
                        {
                            await TriggerBarrierValidationAndPulse(message);
                        }
                    }
                    _loggingService.LogWithColor($"Camera data received: {messages.Count} records, first VRM: {messages.FirstOrDefault()?.Vrm}", Avalonia.Media.Colors.Green);
                    return Ok(new { saved = true, count = messages.Count });
                }
                else
                {
                    // Handle single record
                    var message = JsonSerializer.Deserialize<CameraMessage>(data.GetRawText())!;
                    await _transactionRepository.AddCameraDataAsync(message);
                    if (_config.PulseTriggerMode == "camera")
                    {
                        await TriggerBarrierValidationAndPulse(message);
                    }
                    _loggingService.LogWithColor($"Camera data received: VRM {message.Vrm}", Avalonia.Media.Colors.Green);
                    return Ok(new { saved = true, count = 1 });
                }
            }
            catch (System.Exception ex)
            {
                _loggingService.LogWithColor($"Camera data save failed: {ex.Message}", Avalonia.Media.Colors.Red);
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task TriggerBarrierValidationAndPulse(CameraMessage message)
        {
            try
            {
                // Access the current main window view model via static instance
                var vm = MainWindowViewModel.Instance;
                if (vm == null) return;

                // Find the barrier by matching camera serial to barrier's CameraSerial
                BarrierViewModel? targetBarrier = null;
                int mappedLaneId = 1; // Default if no match

                foreach (var barrier in vm.Barriers)
                {
                    if (barrier.BarrierConfig?.CameraSerial == (message.CameraSerial ?? string.Empty))
                    {
                        targetBarrier = barrier;
                        mappedLaneId = barrier.LaneId;
                        break;
                    }
                }

                // If no match, use first enabled barrier and default lane to maintain backward compatibility
                if (targetBarrier == null)
                {
                    targetBarrier = vm.Barriers.FirstOrDefault(b => b.IsEnabled);
                    mappedLaneId = 1;
                    if (message.CameraSerial != null)
                    {
                        _loggingService.LogWithColor($"No barrier found for camera serial '{message.CameraSerial}', using first enabled barrier (lane {mappedLaneId})", Avalonia.Media.Colors.Orange);
                    }
                }

                if (targetBarrier != null && targetBarrier.IsEnabled)
                {
                    // Insert transaction with mapped lane and direction
                    var direction = string.IsNullOrWhiteSpace(message.LogicalDirection) ? targetBarrier.BarrierConfig?.Direction ?? 0 : (message.LogicalDirection == "1" ? 1 : 0);
                    await _transactionRepository.AddCameraDataAsync(message, mappedLaneId, direction);

                    // Get the transaction we just inserted for validation
                    var transaction = await GetRecentlyInsertedTransaction(message, mappedLaneId, direction);

                    if (transaction == null)
                    {
                        _loggingService.LogWithColor($"Failed to find transaction for validation in camera mode for {message.Vrm}", Avalonia.Media.Colors.Red);
                        return;
                    }

                    // Validate the plate based on direction and barrier's ApiDownBehavior
                    bool shouldPulse = false;
                    if (direction == 1) // Inbound
                    {
                        if (MainWindowViewModel.Instance?.NumberPlateService.IsValidPlate(transaction.OcrPlate, direction, targetBarrier.ApiDownBehavior) == true)
                        {
                            _loggingService.LogWithColor($"Valid camera transaction for plate '{transaction.OcrPlate}', triggering camera pulse for {targetBarrier.Name}", Avalonia.Media.Colors.Green);
                            shouldPulse = true;
                        }
                        else
                        {
                            var reason = MainWindowViewModel.Instance?.NumberPlateService.GetValidationReason(transaction.OcrPlate, direction, targetBarrier.ApiDownBehavior);
                            _loggingService.LogWithColor($"Invalid plate '{transaction.OcrPlate}' for camera In transaction on {targetBarrier.Name}, skipping pulse. Reason: {reason ?? "Unknown validation error"}", Avalonia.Media.Colors.Red);
                        }
                    }
                    else // Outbound
                    {
                        _loggingService.Log($"Camera Out transaction, triggering camera pulse for {targetBarrier.Name}");
                        shouldPulse = true;
                    }

                    if (shouldPulse)
                    {
                        var success = await targetBarrier.SendPulseAsync(false, "Camera");

                        if (success)
                        {
                            await _transactionRepository.MarkTransactionSentDirectly(message, mappedLaneId);
                            _loggingService.LogWithColor($"Camera pulse sent successfully for {targetBarrier.Name}", Avalonia.Media.Colors.Green);
                        }
                        else
                        {
                            _loggingService.LogWithColor($"Camera pulse failed for {targetBarrier.Name}", Avalonia.Media.Colors.Red);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                _loggingService.LogWithColor($"Failed to trigger barrier pulse for camera: {ex.Message}", Avalonia.Media.Colors.Red);
            }
        }

        private async Task<Models.Transaction?> GetRecentlyInsertedTransaction(CameraMessage message, int barrierLaneId, int direction)
        {
            // This is a simple implementation; in production, could use a callback or queue
            await Task.Delay(100); // Small delay to ensure insert is complete

            // Since we just inserted, retrieve the most recent transaction by VRM and lane
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_config.DatabasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, created, datetime, ocr_plate, ocr_accuracy, direction, lane_id, camera_id, image1, image2, image3, sent, sent_datetime
                FROM transactions
                WHERE lane_id = $lane_id AND ocr_plate = $ocr_plate AND sent = 0
                ORDER BY created DESC
                LIMIT 1;
            ";

            command.Parameters.AddWithValue("$lane_id", barrierLaneId);
            command.Parameters.AddWithValue("$ocr_plate", message.Vrm ?? string.Empty);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Models.Transaction
                {
                    Id = reader.GetInt32(0),
                    Created = reader.GetDateTime(1),
                    DateTime = reader.GetDateTime(2),
                    OcrPlate = reader.GetString(3) ?? string.Empty,
                    OcrAccuracy = reader.GetInt32(4),
                    Direction = reader.GetInt32(5),
                    LaneId = reader.GetInt32(6),
                    CameraId = reader.GetInt32(7),
                    Image1 = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Image2 = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Image3 = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Sent = reader.GetInt32(11),
                    SentDateTime = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12)
                };
            }
            return null;
        }
    }
}
