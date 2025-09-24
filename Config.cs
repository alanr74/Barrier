using System;
using System.Collections.Generic;

namespace Ava
{
    public class BarrierConfig
    {
        public string CronExpression { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public int LaneId { get; set; }
    }

    public class BarriersConfig
    {
        public int Count { get; set; }
        public Dictionary<string, BarrierConfig> Barriers { get; set; } = new();
    }

    public class AppConfig
    {
        public BarriersConfig Barriers { get; set; } = new();
        public string NumberPlatesApiUrl { get; set; } = string.Empty;
        public string NumberPlatesCronExpression { get; set; } = "0 0 * * * ?";
        public string ApiDownBehavior { get; set; } = "UseHistoric";
    }
}
