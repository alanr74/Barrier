using System.Collections.Generic;
using System.Threading.Tasks;
using Quartz;
using Ava.ViewModels;

namespace Ava.Services
{
    public interface ISchedulingService
    {
        void Initialize(IEnumerable<BarrierViewModel> barriers, string numberPlatesCron, INumberPlateService numberPlateService);
        Task StartAsync();
        Task StopAsync();
    }
}
