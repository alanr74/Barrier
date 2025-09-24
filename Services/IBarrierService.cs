using System.Threading.Tasks;

namespace Ava.Services
{
    public interface IBarrierService
    {
        Task SendPulseAsync(string apiUrl, string barrierName);
    }
}
