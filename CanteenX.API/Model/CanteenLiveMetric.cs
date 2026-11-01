namespace CanteenX.API.Model
{
    public class CanteenLiveMetric
    {
        public int MetricId { get; set; }
        public int CanteenId { get; set; }
        public string CrowdStatus { get; set; } = string.Empty;
        public string EstimatedWaitTime { get; set; } = string.Empty;
        public double Rating { get; set; }

        public int ReviewCount { get; set; }
        public virtual Canteen? Canteen { get; set; }
    }
}
