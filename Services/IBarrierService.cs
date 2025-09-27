using System.Threading.Tasks;

namespace Ava.Services
{
    public interface IBarrierService
    {
        Task<bool> SendPulseAsync(string apiUrl, string barrierName);
    }
}
