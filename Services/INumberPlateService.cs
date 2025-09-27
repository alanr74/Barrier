using System.Collections.Generic;
using System.Threading.Tasks;
using Ava.Models;

namespace Ava.Services
{
    public interface INumberPlateService
    {
        Task<bool> FetchNumberPlatesAsync();
        bool IsValidPlate(string plate, int direction, string apiDownBehavior);
        string? GetValidationReason(string plate, int direction, string apiDownBehavior);
        bool AllowAnyPlate { get; }
    }
}
