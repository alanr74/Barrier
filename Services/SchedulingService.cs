using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Ava.ViewModels;
using Avalonia.Media;

namespace Ava.Services
{
    public class SchedulingService : ISchedulingService
    {
        private IScheduler? _scheduler;
        private IEnumerable<BarrierViewModel>? _barriers;
        private string? _numberPlatesCron;
        private INumberPlateService? _numberPlateService;

        public void Initialize(IEnumerable<BarrierViewModel> barriers, string numberPlatesCron, INumberPlateService numberPlateService)
        {
            _barriers = barriers;
            _numberPlatesCron = numberPlatesCron;
            _numberPlateService = numberPlateService;
        }

        public async Task StartAsync()
        {
            if (_barriers == null || _numberPlatesCron == null) return;

            var factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler();
            await _scheduler.Start();

            foreach (var barrier in _barriers)
            {
                var job = JobBuilder.Create<BarrierPulseJob>()
                    .WithIdentity(barrier.Name)
                    .UsingJobData("BarrierName", barrier.Name)
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"{barrier.Name}Trigger")
                    .WithCronSchedule(barrier.CronExpression)
                    .Build();

                await _scheduler.ScheduleJob(job, trigger);
            }

            var numberPlatesJob = JobBuilder.Create<NumberPlatesFetchJob>()
                .WithIdentity("FetchNumberPlates")
                .Build();

            var numberPlatesTrigger = TriggerBuilder.Create()
                .WithIdentity("FetchNumberPlatesTrigger")
                .WithCronSchedule(_numberPlatesCron)
                .Build();

            await _scheduler.ScheduleJob(numberPlatesJob, numberPlatesTrigger);
        }

        public async Task StopAsync()
        {
            if (_scheduler != null)
            {
                await _scheduler.Shutdown();
            }
        }
    }

    public class BarrierPulseJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var barrierName = context.JobDetail.JobDataMap.GetString("BarrierName");
            // Note: This assumes MainWindowViewModel.Instance is accessible
            var instance = MainWindowViewModel.Instance;
            if (instance != null)
            {
                var barrier = instance.Barriers.FirstOrDefault(b => b.Name == barrierName);
                if (barrier != null && barrier.IsEnabled)
                {
                    await barrier.SendPulseAsync(true);
                }
            }
        }
    }

    public class NumberPlatesFetchJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            // Note: This assumes MainWindowViewModel.Instance.NumberPlateService is accessible
            var instance = MainWindowViewModel.Instance;
            if (instance != null)
            {
                instance.NumberPlateApiStatus = "Fetching...";
                instance.NumberPlateApiColor = Brushes.Orange;
                var success = await instance.NumberPlateService.FetchNumberPlatesAsync();
                if (success)
                {
                    instance.NumberPlateApiStatus = "Success";
                    instance.NumberPlateApiColor = Brushes.Green;
                }
                else
                {
                    instance.NumberPlateApiStatus = "Failed";
                    instance.NumberPlateApiColor = Brushes.Red;
                }
            }
        }
    }
}
