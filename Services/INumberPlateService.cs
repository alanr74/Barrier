using System.Collections.Generic;
using System.Threading.Tasks;
using Ava.Models;

namespace Ava.Services
{
    public interface INumberPlateService
    {
        Task FetchNumberPlatesAsync();
        bool IsValidPlate(string plate, int direction);
        bool AllowAnyPlate { get; }
    }
}
