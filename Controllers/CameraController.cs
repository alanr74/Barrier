using Microsoft.AspNetCore.Mvc;
using Ava.Repositories;
using Ava.Services;
using Ava.ViewModels;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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
                            await TriggerBarrierPulseForCamera(message);
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
                        await TriggerBarrierPulseForCamera(message);
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

        private async Task TriggerBarrierPulseForCamera(CameraMessage message)
        {
            try
            {
                // Get the camera serial and determine lane (default to 1 for now)
                int cameraId = int.TryParse(message.CameraSerial, out var id) ? id : 1;

                // Access the current main window view model via static instance
                var vm = MainWindowViewModel.Instance;
                if (vm == null) return;

                // Find the barrier that handles this lane (for now, assume lane = cameraId or default to 1)
                var barrierLaneId = 1; // Default, could be mapped from cameraId in future

                var availableBarriers = vm.Barriers;
                var targetBarrier = availableBarriers?.FirstOrDefault(b => b.LaneId == barrierLaneId);

                if (targetBarrier != null && targetBarrier.IsEnabled)
                {
                    // Send pulse with "Camera" source identifier
                    var success = await targetBarrier.SendPulseAsync(false, "Camera");

                    if (success)
                    {
                        await _transactionRepository.MarkTransactionSentDirectly(message, barrierLaneId);
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
    }
}
