using System;
using System.Collections.Generic;

namespace Ava
{
    public class BarrierConfig
    {
        public string CronExpression { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public int LaneId { get; set; }
        public string ApiDownBehavior { get; set; } = "UseHistoric";
        public bool IsEnabled { get; set; } = true;
    }

    public class BarriersConfig
    {
        public int Count { get; set; }
        public Dictionary<string, BarrierConfig> Barriers { get; set; } = new();
    }

    public class Config
    {
        public string ApiUsername { get; set; } = "admin";
        public string ApiPassword { get; set; } = "secret";
        public string DatabasePath { get; set; } = "sync.db";
        public string DatabaseInitMode { get; set; } = "keep"; // "keep" or "recreate"
        public string LogFilePath { get; set; } = string.Empty;
        public string BarrierIpAddress { get; set; } = string.Empty;
        public int BarrierPort { get; set; }
        public int BarrierTimeout { get; set; }
    }

    public class AppConfig
    {
        public BarriersConfig Barriers { get; set; } = new();
        public string NumberPlatesApiUrl { get; set; } = string.Empty;
        public string NumberPlatesCronExpression { get; set; } = "0 0 * * * ?";
        public List<string> WhitelistIds { get; set; } = new();
        public bool SendInitialPulse { get; set; } = true;
        public bool SkipInitialCronPulse { get; set; } = false;
        public bool PerformInitialApiStatusCheck { get; set; } = true;
        public bool AutostartNumberPlates { get; set; } = true;
        public bool StartOpenOnLaunch { get; set; } = false;
        public string ScreenMode { get; set; } = "System";
    }
}
