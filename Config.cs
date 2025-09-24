using System.Collections.Generic;

public class BarrierConfig
{
    public string CronExpression { get; set; }
    public string ApiUrl { get; set; }
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
}
