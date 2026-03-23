using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Report on overall system health and diagnostic status
    /// </summary>
    public class SystemHealthReport
    {
        public bool IsHealthy { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<HealthCheck> HealthChecks { get; set; } = new List<HealthCheck>();
        public PerformanceMetrics Performance { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Individual health check result
    /// </summary>
    public class HealthCheck
    {
        public string Name { get; set; }
        public HealthStatus Status { get; set; }
        public string Message { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance metrics for the diagnostic system
    /// </summary>
    public class PerformanceMetrics
    {
        public long TotalDeserializationAttempts { get; set; }
        public long SuccessfulDeserializations { get; set; }
        public long FailedDeserializations { get; set; }
        public double AverageDeserializationTime { get; set; }
        public Dictionary<string, long> TypeUsageFrequency { get; set; } = new Dictionary<string, long>();
    }

    /// <summary>
    /// Status of health checks
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Warning,
        Unhealthy,
        Critical
    }
}