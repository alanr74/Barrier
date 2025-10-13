using Microsoft.AspNetCore.Mvc;
using Ava.Repositories;
using Ava.Services;
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

        public CameraController(ITransactionRepository transactionRepository, ILoggingService loggingService)
        {
            _transactionRepository = transactionRepository;
            _loggingService = loggingService;
        }

        [HttpPost]
        public async Task<IActionResult> PostCameraData([FromBody] JsonElement data)
        {
            try
            {
                if (data.TryGetProperty("vrmMessages", out var vrmMessages) && vrmMessages.ValueKind == JsonValueKind.Array)
                {
                    // Handle multiple records
                    var messages = JsonSerializer.Deserialize<List<CameraMessage>>(vrmMessages.GetRawText());
                    foreach (var message in messages)
                    {
                        await _transactionRepository.AddCameraDataAsync(message);
                    }
                    _loggingService.LogWithColor($"Camera data received: {messages?.Count ?? 0} records, first VRM: {messages?.FirstOrDefault()?.Vrm}", Avalonia.Media.Colors.Green);
                    return Ok(new { saved = true, count = messages?.Count ?? 0 });
                }
                else
                {
                    // Handle single record
                    var message = JsonSerializer.Deserialize<CameraMessage>(data.GetRawText());
                    await _transactionRepository.AddCameraDataAsync(message);
                    _loggingService.LogWithColor($"Camera data received: VRM {message?.Vrm}", Avalonia.Media.Colors.Green);
                    return Ok(new { saved = true, count = 1 });
                }
            }
            catch (System.Exception ex)
            {
                _loggingService.LogWithColor($"Camera data save failed: {ex.Message}", Avalonia.Media.Colors.Red);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
