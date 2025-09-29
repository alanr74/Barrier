using System.Threading.Tasks;

namespace Ava.Services
{
    public enum ApiStatus
    {
        Up,
        Down,
        Unknown
    }

    public interface IBarrierService
    {
        Task<bool> SendPulseAsync(string apiUrl, string barrierName);
        Task<ApiStatus> CheckApiStatusAsync(string apiUrl, string barrierName);
    }
}
