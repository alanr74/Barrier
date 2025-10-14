using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        /// <summary>
        /// Receives camera data for vehicle number plate detection and processes it for barrier control.
        /// </summary>
        /// <param name="data">JSON payload containing vehicle detection details, such as VRM, camera serial, and direction. Supports single or array format.</param>
        /// <returns>Confirmation of successful data saving or error details.</returns>
        /// <response code="200">Data saved successfully, returns count of processed messages.</response>
        /// <response code="400">Bad request, likely malformed JSON or missing required fields.</response>
        /// <response code="500">Internal server error during processing.</response>
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILoggingService _loggingService;
        private readonly Config _config;
        private readonly DuplicateSuppressorService _duplicateSuppressorService;
        private readonly ILogger<CameraController> _logger;

        public CameraController(ITransactionRepository transactionRepository, ILoggingService loggingService, Config config, DuplicateSuppressorService duplicateSuppressorService, ILogger<CameraController> logger)
        {
            _transactionRepository = transactionRepository;
            _loggingService = loggingService;
            _config = config;
            _duplicateSuppressorService = duplicateSuppressorService;
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint to verify API functionality.
        /// </summary>
        /// <returns>A success message.</returns>
        /// <response code="200">API is working.</response>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { success = true });
        }

        /// <summary>
        /// Receives camera data for vehicle number plate detection and processes it for barrier control.
        /// </summary>
        /// <param name="data">JSON payload containing vehicle detection details, such as VRM, camera serial, and direction. Supports single or array format.</param>
        /// <returns>Confirmation of successful data saving or error details.</returns>
        /// <response code="200">Data saved successfully, returns count of processed messages.</response>
        /// <response code="400">Bad request, likely malformed JSON or missing required fields.</response>
        /// <response code="500">Internal server error during processing.</response>
        /// <remarks>
        /// ## Example Request Body
        /// ```json
        /// {
        ///     "messageType": "readVrm",
        ///     "cameraSerial": "CAM001",
        ///     "vrm": "1SZ8903",
        ///     "logicalDirection": "Unknown",
        ///     "confidence": "0.59108752"
        /// }
        /// ```
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> PostCameraData([FromBody] JsonElement data)
        {
            Console.WriteLine("PostCameraData called");
            Console.WriteLine("JSON root keys: " + string.Join(", ", data.EnumerateObject().Select(p => p.Name)));

            try
            {
                if (data.TryGetProperty("vrmMessages", out var vrmMessages) && vrmMessages.ValueKind == JsonValueKind.Array)
                {
                    // Handle multiple records
                    var messages = new List<CameraMessage>();
                    foreach (var item in vrmMessages.EnumerateArray())
                    {
                        var message = ParseCameraMessage(item);
                        if (message != null)
                        {
                            messages.Add(message);
                        }
                        else
                        {
                            _loggingService.LogWithColor("Failed to parse one of the messages in array", Avalonia.Media.Colors.Red);
                        }
                    }
                    int processedCount = 0;
                    foreach (var message in messages)
                    {
                        try
                        {
                            if (await ProcessCameraMessage(message))
                            {
                                processedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWithColor($"Error processing message for VRM {message.Vrm}: {ex.Message}", Avalonia.Media.Colors.Red);
                        }
                    }
                    _loggingService.LogWithColor($"Camera data received: {messages.Count} records, processed: {processedCount}", Avalonia.Media.Colors.Green);
                    return Ok(new { saved = true, count = messages.Count });
                }
                else
                {
                    // Handle single record
                    var message = ParseCameraMessage(data);
                    if (message == null)
                    {
                        Console.WriteLine("ParseCameraMessage returned null");
                        _loggingService.LogWithColor("Failed to parse single camera message", Avalonia.Media.Colors.Red);
                        _logger.LogError("Failed to parse JSON in ParseCameraMessage, returning 400");
                        return BadRequest("Invalid JSON format for camera message");
                    }
                    Console.WriteLine("Message parsed successfully: Vrm = " + message.Vrm + ", CameraSerial = " + message.CameraSerial);
                    bool processed;
                    try
                    {
                        processed = await ProcessCameraMessage(message);
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWithColor($"Error processing message for VRM {message.Vrm}: {ex.Message}", Avalonia.Media.Colors.Red);
                        _logger.LogError(ex, "Error in ProcessCameraMessage for VRM {Vrm}", message.Vrm);
                        return BadRequest(new { error = ex.Message });
                    }
                    _loggingService.LogWithColor($"Camera data received: VRM {message.Vrm}, processed: {processed}", Avalonia.Media.Colors.Green);
                    return Ok(new { saved = true, count = 1 });
                }
            }
            catch (System.Exception ex)
            {
                _loggingService.LogWithColor($"Camera data save failed: {ex.Message}", Avalonia.Media.Colors.Red);
                System.IO.Directory.CreateDirectory("logs");
                System.IO.File.AppendAllText("logs/camera-errors.log", $"[{DateTime.Now}] Camera data save failed: {ex.Message}\n{ex.StackTrace}\n");
                return BadRequest(new { error = ex.Message });
            }
        }

        private CameraMessage? ParseCameraMessage(JsonElement element)
        {
            try
            {
                var message = new CameraMessage();

                if (element.TryGetProperty("vrm", out var vrmProp) && vrmProp.ValueKind == JsonValueKind.String)
                {
                    message.Vrm = vrmProp.GetString();
                }

                if (element.TryGetProperty("cameraSerial", out var cameraSerialProp) && cameraSerialProp.ValueKind == JsonValueKind.String)
                {
                    message.CameraSerial = cameraSerialProp.GetString();
                }

                if (element.TryGetProperty("logicalDirection", out var logicalDirectionProp) && logicalDirectionProp.ValueKind == JsonValueKind.String)
                {
                    message.LogicalDirection = logicalDirectionProp.GetString();
                }

                if (element.TryGetProperty("confidence", out var confidenceProp) && confidenceProp.ValueKind == JsonValueKind.String)
                {
                    message.Confidence = confidenceProp.GetString();
                }

                // Optional fields can be added here if needed
                if (element.TryGetProperty("firstSeenWallClock", out var firstSeenProp) && firstSeenProp.ValueKind == JsonValueKind.String)
                {
                    message.FirstSeenWallClock = firstSeenProp.GetString();
                }

                if (element.TryGetProperty("direction", out var directionProp) && directionProp.ValueKind == JsonValueKind.String)
                {
                    message.Direction = directionProp.GetString();
                }

                if (element.TryGetProperty("images", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Object)
                {
                    var images = new Dictionary<string, string>();
                    foreach (var prop in imagesProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            images[prop.Name] = prop.Value.GetString() ?? string.Empty;
                        }
                    }
                    message.Images = images;
                }

                // Set other required fields to null or defaults
                message.MessageType = null;
                message.Exposure = null;
                message.Gain = null;
                message.CaptureTimeStamp = null;
                message.ConfidenceOfPresence = null;
                message.LastSeenWallClock = null;
                message.ImageFormat = null;
                message.PlatePosition = null;
                message.CharacterHeight = null;
                message.TrackingId = null;
                message.IsNewVehicle = null;
                message.Tracking = null;
                message.IsPartial = null;
                message.InstanceId = null;
                message.Country = null;
                message.CountryConfidence = null;
                message.PatchImageIndex = null;
                message.OverviewImageIndex = null;
                message.PrimaryImageIndex = null;
                message.CroppedPrimaryImageIndex = null;

                return message;
            }
            catch (Exception ex)
            {
                System.IO.Directory.CreateDirectory("logs");
                System.IO.File.AppendAllText("logs/camera-errors.log", $"[{DateTime.Now}] ParseCameraMessage error: {ex.Message}\n{ex.StackTrace}\n");
                return null;
            }
        }

        private async Task<bool> ProcessCameraMessage(CameraMessage message)
        {
            // Check for duplicates in memory first, even before processing
            var vm = MainWindowViewModel.Instance;
            if (vm == null) return false;

            // Find the barrier by matching camera serial
            BarrierViewModel? targetBarrier = null;
            int mappedLaneId = 1;
            foreach (var barrier in vm.Barriers)
            {
                if (barrier.BarrierConfig?.CameraSerial == (message.CameraSerial ?? string.Empty))
                {
                    targetBarrier = barrier;
                    mappedLaneId = barrier.LaneId;
                    break;
                }
            }
            if (targetBarrier == null)
            {
                targetBarrier = vm.Barriers.FirstOrDefault(b => b.IsEnabled);
                if (message.CameraSerial != null)
                {
                    _loggingService.LogWithColor($"No barrier found for camera serial '{message.CameraSerial}', using first enabled barrier", Avalonia.Media.Colors.Orange);
                }
            }

            var direction = string.IsNullOrWhiteSpace(message.LogicalDirection) ? (targetBarrier?.BarrierConfig?.Direction ?? 0) : (message.LogicalDirection == "1" ? 1 : 0);

            // Check for duplicates
            if (_duplicateSuppressorService.IsDuplicate(message.Vrm ?? string.Empty, mappedLaneId, direction))
            {
                _loggingService.LogWithColor($"Duplicate VRM '{message.Vrm}' in lane {mappedLaneId} direction {direction} within suppression window, skipping", Avalonia.Media.Colors.Orange);
                return false; // Not saved, but logged for visibility
            }

            await _transactionRepository.AddCameraDataAsync(message, mappedLaneId, direction);
            if (_config.PulseTriggerMode == "camera")
            {
                await TriggerBarrierValidationAndPulse(message, targetBarrier!, mappedLaneId, direction);
            }
            return true;
        }

        private async Task TriggerBarrierValidationAndPulse(CameraMessage message, BarrierViewModel targetBarrier, int mappedLaneId, int direction)
        {
            if (targetBarrier == null || !targetBarrier.IsEnabled)
            {
                return;
            }

            try
            {
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
