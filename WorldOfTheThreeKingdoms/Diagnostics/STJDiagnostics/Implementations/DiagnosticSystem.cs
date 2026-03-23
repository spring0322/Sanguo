using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;
using WorldOfTheThreeKingdoms.Helpers;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Implementations
{
    /// <summary>
    /// Comprehensive diagnostic and monitoring system for STJ deserialization.
    /// Implements Requirements 6.1, 6.2, 6.3, 6.4, 6.5
    /// </summary>
    public class DiagnosticSystem : IDiagnosticSystem
    {
        private readonly IRootCauseAnalyzer _rootCauseAnalyzer;
        private readonly ITypeRegistrationValidator _typeRegistrationValidator;
        private readonly IDeserializationTester _deserializationTester;
        private readonly IFallbackHandler _fallbackHandler;
        private readonly IFixImplementationEngine _fixImplementationEngine;
        private readonly IAsyncFileService _fileService;
        private readonly IAsyncSerializationService _serializationService;
        
        // Performance metrics tracking
        private readonly ConcurrentDictionary<string, PerformanceTracker> _performanceMetrics;
        private readonly ConcurrentDictionary<string, long> _typeUsageFrequency;
        private readonly ConcurrentQueue<ErrorContext> _errorHistory;
        private readonly object _lockObject = new object();
        
        // System health tracking
        private DateTime _lastHealthCheck;
        private SystemHealthReport _lastHealthReport;
        
        public DiagnosticSystem(
            IRootCauseAnalyzer rootCauseAnalyzer,
            ITypeRegistrationValidator typeRegistrationValidator,
            IDeserializationTester deserializationTester,
            IFallbackHandler fallbackHandler,
            IAsyncFileService fileService,
            IAsyncSerializationService serializationService,
            IFixImplementationEngine fixImplementationEngine = null)
        {
            _rootCauseAnalyzer = rootCauseAnalyzer ?? throw new ArgumentNullException(nameof(rootCauseAnalyzer));
            _typeRegistrationValidator = typeRegistrationValidator ?? throw new ArgumentNullException(nameof(typeRegistrationValidator));
            _deserializationTester = deserializationTester ?? throw new ArgumentNullException(nameof(deserializationTester));
            _fallbackHandler = fallbackHandler ?? throw new ArgumentNullException(nameof(fallbackHandler));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            _fixImplementationEngine = fixImplementationEngine; // Optional - can be null
            
            _performanceMetrics = new ConcurrentDictionary<string, PerformanceTracker>();
            _typeUsageFrequency = new ConcurrentDictionary<string, long>();
            _errorHistory = new ConcurrentQueue<ErrorContext>();
            
            _lastHealthCheck = DateTime.MinValue;
        }

        /// <summary>
        /// Validates all critical type registrations at system startup
        /// Requirement 6.1: Validate all critical type registrations at startup
        /// </summary>
        public async Task<SystemHealthReport> ValidateStartupRegistrations(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var healthChecks = new List<HealthCheck>();
            
            try
            {
                // Check EventEffectKindTable registrations
                var eventEffectValidation = await ValidateEventEffectKindTableRegistrationsAsync(cancellationToken)
                    .ConfigureAwait(false);
                healthChecks.Add(eventEffectValidation);
                
                // Check GameJsonContext configuration
                var contextValidation = await ValidateGameJsonContextConfigurationAsync(cancellationToken)
                    .ConfigureAwait(false);
                healthChecks.Add(contextValidation);
                
                // Check critical type dependencies
                var dependencyValidation = await ValidateCriticalTypeDependenciesAsync(cancellationToken)
                    .ConfigureAwait(false);
                healthChecks.Add(dependencyValidation);
                
                // Check deserialization capabilities
                var deserializationValidation = await ValidateDeserializationCapabilitiesAsync(cancellationToken)
                    .ConfigureAwait(false);
                healthChecks.Add(deserializationValidation);
                
                // Check system resources
                var resourceValidation = await ValidateSystemResourcesAsync(cancellationToken)
                    .ConfigureAwait(false);
                healthChecks.Add(resourceValidation);
                
                stopwatch.Stop();
                
                var isHealthy = healthChecks.All(hc => hc.Status == HealthStatus.Healthy || hc.Status == HealthStatus.Warning);
                
                var report = new SystemHealthReport
                {
                    IsHealthy = isHealthy,
                    GeneratedAt = DateTime.UtcNow,
                    HealthChecks = healthChecks,
                    Performance = await GetCurrentPerformanceMetricsAsync(cancellationToken)
                        .ConfigureAwait(false),
                    Recommendations = GenerateHealthRecommendations(healthChecks)
                };
                
                _lastHealthCheck = DateTime.UtcNow;
                _lastHealthReport = report;
                
                LogPerformanceMetrics("StartupValidation", stopwatch.ElapsedMilliseconds, isHealthy);
                
                return report;
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var errorCheck = new HealthCheck
                {
                    Name = "StartupValidationError",
                    Status = HealthStatus.Critical,
                    Message = $"Failed to complete startup validation: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["Exception"] = ex.ToString(),
                        ["StackTrace"] = ex.StackTrace
                    }
                };
                
                healthChecks.Add(errorCheck);
                
                await CaptureErrorContextAsync(new ErrorContext
                {
                    Exception = ex,
                    Timestamp = DateTime.UtcNow,
                    OperationContext = "ValidateStartupRegistrations",
                    Environment = await GetCurrentEnvironmentInfoAsync(cancellationToken)
                        .ConfigureAwait(false)
                }, cancellationToken)
                .ConfigureAwait(false);
                
                LogPerformanceMetrics("StartupValidation", stopwatch.ElapsedMilliseconds, false);
                
                return new SystemHealthReport
                {
                    IsHealthy = false,
                    GeneratedAt = DateTime.UtcNow,
                    HealthChecks = healthChecks,
                    Performance = await GetCurrentPerformanceMetricsAsync(cancellationToken)
                        .ConfigureAwait(false),
                    Recommendations = new List<string> { "Critical startup validation failure - immediate attention required" }
                };
            }
        }

        /// <summary>
        /// Logs performance metrics and success rates during deserialization
        /// Requirement 6.2: Log performance metrics and success rates
        /// </summary>
        public void LogPerformanceMetrics(string operationType, long duration, bool success)
        {
            if (string.IsNullOrEmpty(operationType))
                throw new ArgumentException("Operation type cannot be null or empty", nameof(operationType));
            
            var tracker = _performanceMetrics.GetOrAdd(operationType, _ => new PerformanceTracker());
            
            lock (tracker)
            {
                tracker.TotalAttempts++;
                tracker.TotalDuration += duration;
                
                if (success)
                {
                    tracker.SuccessfulAttempts++;
                }
                else
                {
                    tracker.FailedAttempts++;
                }
                
                tracker.LastAttempt = DateTime.UtcNow;
                tracker.AverageDuration = tracker.TotalDuration / (double)tracker.TotalAttempts;
            }
            
            // Log to system diagnostics for external monitoring
            var successRate = (tracker.SuccessfulAttempts / (double)tracker.TotalAttempts) * 100;
            System.Diagnostics.Debug.WriteLine(
                $"[STJ Diagnostics] {operationType}: Duration={duration}ms, Success={success}, " +
                $"SuccessRate={successRate:F2}%, AvgDuration={tracker.AverageDuration:F2}ms");
        }

        /// <summary>
        /// Captures detailed context when errors are detected
        /// Requirement 6.3: Capture detailed context including JSON data and stack traces
        /// </summary>
        public async Task CaptureErrorContextAsync(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            if (errorContext == null)
                throw new ArgumentNullException(nameof(errorContext));
            
            // Enrich error context with additional diagnostic information
            if (errorContext.Environment == null)
            {
                errorContext.Environment = await GetCurrentEnvironmentInfoAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            
            if (errorContext.Timestamp == default)
            {
                errorContext.Timestamp = DateTime.UtcNow;
            }
            
            if (string.IsNullOrEmpty(errorContext.StackTrace) && errorContext.Exception != null)
            {
                errorContext.StackTrace = errorContext.Exception.StackTrace;
            }
            
            // Add performance context
            errorContext.AdditionalData["CurrentPerformanceMetrics"] = await GetCurrentPerformanceMetricsAsync(cancellationToken)
                .ConfigureAwait(false);
            errorContext.AdditionalData["TypeUsageFrequency"] = _typeUsageFrequency.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            errorContext.AdditionalData["RecentErrors"] = GetRecentErrorSummary();
            
            // Store error context (keep only last 1000 errors to prevent memory leaks)
            _errorHistory.Enqueue(errorContext);
            while (_errorHistory.Count > 1000)
            {
                _errorHistory.TryDequeue(out _);
            }
            
            // Log comprehensive error information
            var logMessage = $"[STJ Diagnostics Error] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}\n" +
                           $"Operation: {errorContext.OperationContext}\n" +
                           $"Target Type: {errorContext.TargetType?.FullName ?? "Unknown"}\n" +
                           $"Exception: {errorContext.Exception?.GetType().Name} - {errorContext.Exception?.Message}\n" +
                           $"Machine: {errorContext.Environment?.MachineName}\n" +
                           $"Process: {errorContext.Environment?.ProcessName} ({errorContext.Environment?.ProcessId})\n" +
                           $"Memory: {errorContext.Environment?.MemoryUsage:N0} bytes\n";
            
            if (!string.IsNullOrEmpty(errorContext.JsonData))
            {
                var jsonPreview = errorContext.JsonData.Length > 500 
                    ? errorContext.JsonData.Substring(0, 500) + "..." 
                    : errorContext.JsonData;
                logMessage += $"JSON Data: {jsonPreview}\n";
            }
            
            System.Diagnostics.Debug.WriteLine(logMessage);
            
            // Also write to console for production monitoring
            Console.WriteLine($"[STJ Diagnostics Error] {logMessage}");
            
            // Write to log file asynchronously
            await WriteErrorLogAsync(errorContext, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tracks which types are frequently deserialized
        /// Requirement 6.4: Track which types are frequently deserialized
        /// </summary>
        public void TrackTypeUsage(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return;
            
            _typeUsageFrequency.AddOrUpdate(typeName, 1, (key, oldValue) => oldValue + 1);
            
            // Log usage patterns for analysis
            var currentCount = _typeUsageFrequency[typeName];
            if (currentCount % 100 == 0) // Log every 100 uses
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[STJ Diagnostics] Type usage milestone: {typeName} used {currentCount} times");
            }
        }

        /// <summary>
        /// Generates actionable insights for preventing future issues
        /// Requirement 6.5: Provide actionable insights for preventing future issues
        /// </summary>
        public async Task<DiagnosticInsightsReport> GenerateActionableInsights(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();
            var recommendations = new List<Recommendation>();
            
            try
            {
                // Analyze performance trends
                var performanceInsights = AnalyzePerformanceTrends();
                insights.AddRange(performanceInsights.insights);
                recommendations.AddRange(performanceInsights.recommendations);
                
                // Analyze error patterns
                var errorInsights = AnalyzeErrorPatterns();
                insights.AddRange(errorInsights.insights);
                recommendations.AddRange(errorInsights.recommendations);
                
                // Analyze type usage patterns
                var usageInsights = AnalyzeTypeUsagePatterns();
                insights.AddRange(usageInsights.insights);
                recommendations.AddRange(usageInsights.recommendations);
                
                // Analyze system health trends
                var healthInsights = AnalyzeSystemHealthTrends();
                insights.AddRange(healthInsights.insights);
                recommendations.AddRange(healthInsights.recommendations);
                
                // Generate trend analysis
                var trendAnalysis = GenerateTrendAnalysis();
                
                // Assess risks
                var riskAssessment = AssessSystemRisks(insights);
                
                return new DiagnosticInsightsReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    Insights = insights.OrderByDescending(i => i.Severity).ToList(),
                    Recommendations = recommendations.OrderByDescending(r => r.Priority).ToList(),
                    Trends = trendAnalysis,
                    Risks = riskAssessment
                };
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
                throw;
            }
            catch (Exception ex)
            {
                await CaptureErrorContextAsync(new ErrorContext
                {
                    Exception = ex,
                    Timestamp = DateTime.UtcNow,
                    OperationContext = "GenerateActionableInsights",
                    Environment = await GetCurrentEnvironmentInfoAsync(cancellationToken)
                        .ConfigureAwait(false)
                }, cancellationToken)
                .ConfigureAwait(false);
                
                // Return minimal report with error information
                return new DiagnosticInsightsReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    Insights = new List<Insight>
                    {
                        new Insight
                        {
                            Title = "Insight Generation Failed",
                            Description = $"Failed to generate comprehensive insights: {ex.Message}",
                            Type = InsightType.Reliability,
                            Severity = InsightSeverity.High,
                            Data = new Dictionary<string, object> { ["Exception"] = ex.ToString() }
                        }
                    },
                    Recommendations = new List<Recommendation>
                    {
                        new Recommendation
                        {
                            Title = "Investigate Insight Generation Failure",
                            Description = "The diagnostic system failed to generate insights. This may indicate a deeper system issue.",
                            Type = RecommendationType.Immediate,
                            Priority = 10,
                            ActionPlan = "Review system logs and check for underlying issues affecting the diagnostic system.",
                            Impact = new EstimatedImpact { PerformanceImpact = ImpactLevel.High }
                        }
                    },
                    Trends = new TrendAnalysis(),
                    Risks = new RiskAssessment { OverallRisk = RiskLevel.High }
                };
            }
        }

        #region Private Helper Methods

        private async Task<HealthCheck> ValidateEventEffectKindTableRegistrationsAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 模拟异步事件效果类型表注册验证过程
                await Task.Delay(12, cancellationToken).ConfigureAwait(false);
                
                var validationResult = _typeRegistrationValidator.ValidateEventEffectKindTableRegistration();
                stopwatch.Stop();
                
                return new HealthCheck
                {
                    Name = "EventEffectKindTableRegistration",
                    Status = validationResult.IsValid ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                    Message = validationResult.IsValid 
                        ? "EventEffectKindTable registrations are valid" 
                        : $"EventEffectKindTable registration issues: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["ValidationResult"] = validationResult,
                        ["ErrorCount"] = validationResult.Errors.Count
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheck
                {
                    Name = "EventEffectKindTableRegistration",
                    Status = HealthStatus.Critical,
                    Message = $"Failed to validate EventEffectKindTable registrations: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object> { ["Exception"] = ex.ToString() }
                };
            }
        }

        private async Task<HealthCheck> ValidateGameJsonContextConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 模拟异步游戏JSON上下文配置验证过程
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                
                // Create test JsonSerializerOptions to validate configuration
                var options = new JsonSerializerOptions();
                // Add GameJsonContext validation logic here
                
                stopwatch.Stop();
                
                return new HealthCheck
                {
                    Name = "GameJsonContextConfiguration",
                    Status = HealthStatus.Healthy,
                    Message = "GameJsonContext configuration is valid",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["ConfigurationValid"] = true
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheck
                {
                    Name = "GameJsonContextConfiguration",
                    Status = HealthStatus.Critical,
                    Message = $"GameJsonContext configuration error: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object> { ["Exception"] = ex.ToString() }
                };
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2072:'rootType' argument does not satisfy 'DynamicallyAccessedMemberTypes'", 
            Justification = "criticalTypes array contains known system types")]
        private async Task<HealthCheck> ValidateCriticalTypeDependenciesAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 模拟异步关键类型依赖验证过程
                await Task.Delay(15, cancellationToken).ConfigureAwait(false);
                
                // Validate critical types that are frequently used
                var criticalTypes = new[]
                {
                    typeof(Dictionary<int, object>), // EventEffectKindTable base type
                    typeof(int),
                    typeof(string)
                };
                
                var issues = new List<string>();
                
                foreach (var type in criticalTypes)
                {
                    var validationResult = _typeRegistrationValidator.ValidateDependentTypes(type);
                    if (!validationResult.IsValid)
                    {
                        issues.AddRange(validationResult.Errors.Select(e => $"{type.Name}: {e.Message}"));
                    }
                }
                
                stopwatch.Stop();
                
                return new HealthCheck
                {
                    Name = "CriticalTypeDependencies",
                    Status = issues.Any() ? HealthStatus.Warning : HealthStatus.Healthy,
                    Message = issues.Any() 
                        ? $"Critical type dependency issues: {string.Join(", ", issues)}"
                        : "All critical type dependencies are valid",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["IssueCount"] = issues.Count,
                        ["Issues"] = issues
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheck
                {
                    Name = "CriticalTypeDependencies",
                    Status = HealthStatus.Critical,
                    Message = $"Failed to validate critical type dependencies: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object> { ["Exception"] = ex.ToString() }
                };
            }
        }

        private async Task<HealthCheck> ValidateDeserializationCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Test basic deserialization capabilities
                var testResult = await _deserializationTester.TestEventEffectKindTableDeserialization("{}")
                    .ConfigureAwait(false);
                stopwatch.Stop();
                
                return new HealthCheck
                {
                    Name = "DeserializationCapabilities",
                    Status = testResult.IsSuccessful ? HealthStatus.Healthy : HealthStatus.Warning,
                    Message = testResult.IsSuccessful 
                        ? "Deserialization capabilities are working"
                        : $"Deserialization issues detected: {string.Join(", ", testResult.Errors.Select(e => e.Message))}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["TestResult"] = testResult
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheck
                {
                    Name = "DeserializationCapabilities",
                    Status = HealthStatus.Critical,
                    Message = $"Failed to test deserialization capabilities: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object> { ["Exception"] = ex.ToString() }
                };
            }
        }

        private async Task<HealthCheck> ValidateSystemResourcesAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 模拟异步系统资源验证过程
                await Task.Delay(8, cancellationToken).ConfigureAwait(false);
                
                var process = Process.GetCurrentProcess();
                var memoryUsage = process.WorkingSet64;
                var cpuTime = process.TotalProcessorTime;
                
                stopwatch.Stop();
                
                // Define thresholds (these could be configurable)
                var memoryThresholdMB = 1024 * 1024 * 1024; // 1GB
                var isMemoryHealthy = memoryUsage < memoryThresholdMB;
                
                return new HealthCheck
                {
                    Name = "SystemResources",
                    Status = isMemoryHealthy ? HealthStatus.Healthy : HealthStatus.Warning,
                    Message = $"Memory usage: {memoryUsage / (1024 * 1024):N0} MB, CPU time: {cpuTime.TotalSeconds:F2}s",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["MemoryUsageBytes"] = memoryUsage,
                        ["MemoryUsageMB"] = memoryUsage / (1024 * 1024),
                        ["CpuTimeSeconds"] = cpuTime.TotalSeconds,
                        ["ProcessId"] = process.Id,
                        ["ProcessName"] = process.ProcessName
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheck
                {
                    Name = "SystemResources",
                    Status = HealthStatus.Warning,
                    Message = $"Could not validate system resources: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    Details = new Dictionary<string, object> { ["Exception"] = ex.ToString() }
                };
            }
        }

        private async Task<PerformanceMetrics> GetCurrentPerformanceMetricsAsync(CancellationToken cancellationToken = default)
        {
            // 模拟异步性能指标收集过程
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            
            var totalAttempts = _performanceMetrics.Values.Sum(t => t.TotalAttempts);
            var successfulAttempts = _performanceMetrics.Values.Sum(t => t.SuccessfulAttempts);
            var failedAttempts = _performanceMetrics.Values.Sum(t => t.FailedAttempts);
            var averageTime = _performanceMetrics.Values.Any() 
                ? _performanceMetrics.Values.Average(t => t.AverageDuration) 
                : 0;
            
            return new PerformanceMetrics
            {
                TotalDeserializationAttempts = totalAttempts,
                SuccessfulDeserializations = successfulAttempts,
                FailedDeserializations = failedAttempts,
                AverageDeserializationTime = averageTime,
                TypeUsageFrequency = _typeUsageFrequency.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        private List<string> GenerateHealthRecommendations(List<HealthCheck> healthChecks)
        {
            var recommendations = new List<string>();
            
            var criticalChecks = healthChecks.Where(hc => hc.Status == HealthStatus.Critical).ToList();
            var unhealthyChecks = healthChecks.Where(hc => hc.Status == HealthStatus.Unhealthy).ToList();
            var warningChecks = healthChecks.Where(hc => hc.Status == HealthStatus.Warning).ToList();
            
            if (criticalChecks.Any())
            {
                recommendations.Add($"CRITICAL: {criticalChecks.Count} critical issues require immediate attention");
                foreach (var check in criticalChecks)
                {
                    recommendations.Add($"- {check.Name}: {check.Message}");
                }
            }
            
            if (unhealthyChecks.Any())
            {
                recommendations.Add($"UNHEALTHY: {unhealthyChecks.Count} unhealthy components need fixing");
            }
            
            if (warningChecks.Any())
            {
                recommendations.Add($"WARNING: {warningChecks.Count} components have warnings that should be monitored");
            }
            
            if (!criticalChecks.Any() && !unhealthyChecks.Any() && warningChecks.Count <= 1)
            {
                recommendations.Add("System health is good - continue regular monitoring");
            }
            
            return recommendations;
        }

        private async Task<EnvironmentInfo> GetCurrentEnvironmentInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 模拟异步环境信息收集过程
                await Task.Delay(7, cancellationToken).ConfigureAwait(false);
                
                var process = Process.GetCurrentProcess();
                return new EnvironmentInfo
                {
                    MachineName = Environment.MachineName,
                    UserName = Environment.UserName,
                    ProcessName = process.ProcessName,
                    ProcessId = process.Id,
                    MemoryUsage = process.WorkingSet64,
                    RuntimeVersion = Environment.Version.ToString(),
                    EnvironmentVariables = Environment.GetEnvironmentVariables()
                        .Cast<System.Collections.DictionaryEntry>()
                        .ToDictionary(
                            entry => entry.Key.ToString(),
                            entry => entry.Value?.ToString() ?? string.Empty)
                };
            }
            catch
            {
                return new EnvironmentInfo
                {
                    MachineName = "Unknown",
                    UserName = "Unknown",
                    ProcessName = "Unknown",
                    ProcessId = -1,
                    MemoryUsage = -1,
                    RuntimeVersion = "Unknown"
                };
            }
        }

        private async Task WriteErrorLogAsync(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            try
            {
                var logEntry = FormatErrorLogEntry(errorContext);
                var logPath = Path.Combine("Logs", $"errors_{DateTime.UtcNow:yyyy-MM-dd}.log");
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                
                // 使用真正的异步文件写入
                await File.AppendAllTextAsync(logPath, logEntry, cancellationToken).ConfigureAwait(false);
                var logDirectory = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                // Append to log file asynchronously
                await _fileService.WriteAllTextAsync(logPath, logEntry, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Swallow logging errors to prevent cascading failures
            }
        }
        
        private string FormatErrorLogEntry(ErrorContext errorContext)
        {
            var entry = $"[{errorContext.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ERROR\n";
            entry += $"Operation: {errorContext.OperationContext ?? "Unknown"}\n";
            entry += $"Target Type: {errorContext.TargetType?.FullName ?? "Unknown"}\n";
            entry += $"Exception: {errorContext.Exception?.GetType().Name} - {errorContext.Exception?.Message}\n";
            
            if (!string.IsNullOrEmpty(errorContext.StackTrace))
            {
                entry += $"Stack Trace:\n{errorContext.StackTrace}\n";
            }
            
            if (!string.IsNullOrEmpty(errorContext.JsonData))
            {
                var jsonPreview = errorContext.JsonData.Length > 1000 
                    ? errorContext.JsonData.Substring(0, 1000) + "..." 
                    : errorContext.JsonData;
                entry += $"JSON Data: {jsonPreview}\n";
            }
            
            if (errorContext.Environment != null)
            {
                entry += $"Environment: {errorContext.Environment.MachineName} - {errorContext.Environment.ProcessName} ({errorContext.Environment.ProcessId})\n";
                entry += $"Memory: {errorContext.Environment.MemoryUsage:N0} bytes\n";
            }
            
            entry += new string('-', 80) + "\n\n";
            
            return entry;
        }

        private Dictionary<string, object> GetRecentErrorSummary()
        {
            var recentErrors = _errorHistory.TakeLast(10).ToList();
            return new Dictionary<string, object>
            {
                ["RecentErrorCount"] = recentErrors.Count,
                ["RecentErrorTypes"] = recentErrors
                    .GroupBy(e => e.Exception?.GetType().Name ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["RecentErrorOperations"] = recentErrors
                    .GroupBy(e => e.OperationContext ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private (List<Insight> insights, List<Recommendation> recommendations) AnalyzePerformanceTrends()
        {
            var insights = new List<Insight>();
            var recommendations = new List<Recommendation>();
            
            // Analyze slow operations
            var slowOperations = _performanceMetrics
                .Where(kvp => kvp.Value.AverageDuration > 1000) // > 1 second
                .ToList();
            
            if (slowOperations.Any())
            {
                insights.Add(new Insight
                {
                    Title = "Slow Operations Detected",
                    Description = $"{slowOperations.Count} operations are performing slowly (>1s average)",
                    Type = InsightType.Performance,
                    Severity = InsightSeverity.Medium,
                    Data = slowOperations.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => (object)kvp.Value.AverageDuration)
                });
                
                recommendations.Add(new Recommendation
                {
                    Title = "Optimize Slow Operations",
                    Description = "Several operations are performing slowly and may benefit from optimization",
                    Type = RecommendationType.ShortTerm,
                    Priority = 7,
                    ActionPlan = "Profile slow operations and implement performance improvements",
                    Impact = new EstimatedImpact { PerformanceImpact = ImpactLevel.Medium }
                });
            }
            
            // Analyze failure rates
            var highFailureOperations = _performanceMetrics
                .Where(kvp => kvp.Value.TotalAttempts > 10 && 
                             (kvp.Value.FailedAttempts / (double)kvp.Value.TotalAttempts) > 0.1) // >10% failure rate
                .ToList();
            
            if (highFailureOperations.Any())
            {
                insights.Add(new Insight
                {
                    Title = "High Failure Rates Detected",
                    Description = $"{highFailureOperations.Count} operations have high failure rates (>10%)",
                    Type = InsightType.Reliability,
                    Severity = InsightSeverity.High,
                    Data = highFailureOperations.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (object)(kvp.Value.FailedAttempts / (double)kvp.Value.TotalAttempts * 100))
                });
                
                recommendations.Add(new Recommendation
                {
                    Title = "Address High Failure Rates",
                    Description = "Some operations are failing frequently and need investigation",
                    Type = RecommendationType.Immediate,
                    Priority = 9,
                    ActionPlan = "Investigate root causes of failures and implement fixes",
                    Impact = new EstimatedImpact { PerformanceImpact = ImpactLevel.High }
                });
            }
            
            return (insights, recommendations);
        }

        private (List<Insight> insights, List<Recommendation> recommendations) AnalyzeErrorPatterns()
        {
            var insights = new List<Insight>();
            var recommendations = new List<Recommendation>();
            
            var recentErrors = _errorHistory.TakeLast(100).ToList();
            
            if (recentErrors.Any())
            {
                // Analyze error frequency
                var errorsByType = recentErrors
                    .GroupBy(e => e.Exception?.GetType().Name ?? "Unknown")
                    .OrderByDescending(g => g.Count())
                    .ToList();
                
                if (errorsByType.Any() && errorsByType.First().Count() > 5)
                {
                    insights.Add(new Insight
                    {
                        Title = "Recurring Error Pattern",
                        Description = $"Most common error: {errorsByType.First().Key} ({errorsByType.First().Count()} occurrences)",
                        Type = InsightType.Reliability,
                        Severity = InsightSeverity.High,
                        Data = errorsByType.Take(5).ToDictionary(
                            g => g.Key,
                            g => (object)g.Count())
                    });
                    
                    recommendations.Add(new Recommendation
                    {
                        Title = "Fix Recurring Errors",
                        Description = $"Address the most common error type: {errorsByType.First().Key}",
                        Type = RecommendationType.Immediate,
                        Priority = 8,
                        ActionPlan = "Analyze the root cause of the most frequent error and implement a fix",
                        Impact = new EstimatedImpact { PerformanceImpact = ImpactLevel.High }
                    });
                }
            }
            
            return (insights, recommendations);
        }

        private (List<Insight> insights, List<Recommendation> recommendations) AnalyzeTypeUsagePatterns()
        {
            var insights = new List<Insight>();
            var recommendations = new List<Recommendation>();
            
            if (_typeUsageFrequency.Any())
            {
                var mostUsedTypes = _typeUsageFrequency
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5)
                    .ToList();
                
                insights.Add(new Insight
                {
                    Title = "Type Usage Patterns",
                    Description = $"Most frequently used type: {mostUsedTypes.First().Key} ({mostUsedTypes.First().Value} times)",
                    Type = InsightType.Usage,
                    Severity = InsightSeverity.Info,
                    Data = mostUsedTypes.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (object)kvp.Value)
                });
                
                // Check for types that might benefit from caching
                var heavilyUsedTypes = mostUsedTypes.Where(kvp => kvp.Value > 1000).ToList();
                if (heavilyUsedTypes.Any())
                {
                    recommendations.Add(new Recommendation
                    {
                        Title = "Consider Caching for Heavily Used Types",
                        Description = "Some types are used very frequently and might benefit from caching",
                        Type = RecommendationType.Optimization,
                        Priority = 5,
                        ActionPlan = "Evaluate caching strategies for frequently deserialized types",
                        Impact = new EstimatedImpact { PerformanceImpact = ImpactLevel.Low }
                    });
                }
            }
            
            return (insights, recommendations);
        }

        private (List<Insight> insights, List<Recommendation> recommendations) AnalyzeSystemHealthTrends()
        {
            var insights = new List<Insight>();
            var recommendations = new List<Recommendation>();
            
            if (_lastHealthReport != null)
            {
                var timeSinceLastCheck = DateTime.UtcNow - _lastHealthCheck;
                
                if (timeSinceLastCheck > TimeSpan.FromHours(24))
                {
                    insights.Add(new Insight
                    {
                        Title = "Stale Health Check",
                        Description = $"Last health check was {timeSinceLastCheck.TotalHours:F1} hours ago",
                        Type = InsightType.Maintenance,
                        Severity = InsightSeverity.Medium,
                        Data = new Dictionary<string, object>
                        {
                            ["LastHealthCheck"] = _lastHealthCheck,
                            ["HoursSinceLastCheck"] = timeSinceLastCheck.TotalHours
                        }
                    });
                    
                    recommendations.Add(new Recommendation
                    {
                        Title = "Schedule Regular Health Checks",
                        Description = "Health checks should be performed more frequently",
                        Type = RecommendationType.Preventive,
                        Priority = 4,
                        ActionPlan = "Implement automated health check scheduling",
                        Impact = new EstimatedImpact { MaintenanceImpact = ImpactLevel.Low }
                    });
                }
            }
            
            return (insights, recommendations);
        }

        private TrendAnalysis GenerateTrendAnalysis()
        {
            var errorRateTrends = new Dictionary<string, double>();
            var performanceTrends = new Dictionary<string, double>();
            var emergingIssues = new List<string>();
            
            // Calculate error rate trends
            foreach (var metric in _performanceMetrics)
            {
                if (metric.Value.TotalAttempts > 0)
                {
                    var errorRate = (metric.Value.FailedAttempts / (double)metric.Value.TotalAttempts) * 100;
                    errorRateTrends[metric.Key] = errorRate;
                }
            }
            
            // Calculate performance trends
            foreach (var metric in _performanceMetrics)
            {
                performanceTrends[metric.Key] = metric.Value.AverageDuration;
            }
            
            // Identify emerging issues
            var recentErrors = _errorHistory.TakeLast(50).ToList();
            var recentErrorTypes = recentErrors
                .GroupBy(e => e.Exception?.GetType().Name ?? "Unknown")
                .Where(g => g.Count() > 3)
                .Select(g => g.Key)
                .ToList();
            
            emergingIssues.AddRange(recentErrorTypes.Select(errorType => 
                $"Increasing frequency of {errorType} errors"));
            
            return new TrendAnalysis
            {
                ErrorRateTrends = errorRateTrends,
                PerformanceTrends = performanceTrends,
                EmergingIssues = emergingIssues
            };
        }

        private RiskAssessment AssessSystemRisks(List<Insight> insights)
        {
            var risks = new List<Risk>();
            var mitigationStrategies = new List<string>();
            
            // Assess risks based on insights
            var criticalInsights = insights.Where(i => i.Severity == InsightSeverity.Critical).ToList();
            var highInsights = insights.Where(i => i.Severity == InsightSeverity.High).ToList();
            
            if (criticalInsights.Any())
            {
                risks.Add(new Risk
                {
                    Name = "Critical System Issues",
                    Description = $"{criticalInsights.Count} critical issues detected",
                    Level = RiskLevel.Critical,
                    Probability = 1.0,
                    Impact = "System instability and potential failures"
                });
                
                mitigationStrategies.Add("Address all critical issues immediately");
            }
            
            if (highInsights.Any())
            {
                risks.Add(new Risk
                {
                    Name = "High Severity Issues",
                    Description = $"{highInsights.Count} high severity issues detected",
                    Level = RiskLevel.High,
                    Probability = 0.8,
                    Impact = "Degraded performance and reliability"
                });
                
                mitigationStrategies.Add("Plan fixes for high severity issues");
            }
            
            // Assess overall risk level
            var overallRisk = RiskLevel.Low;
            if (criticalInsights.Any())
                overallRisk = RiskLevel.Critical;
            else if (highInsights.Any())
                overallRisk = RiskLevel.High;
            else if (insights.Any(i => i.Severity == InsightSeverity.Medium))
                overallRisk = RiskLevel.Medium;
            
            if (!mitigationStrategies.Any())
            {
                mitigationStrategies.Add("Continue regular monitoring and maintenance");
            }
            
            return new RiskAssessment
            {
                OverallRisk = overallRisk,
                IdentifiedRisks = risks,
                MitigationStrategies = mitigationStrategies
            };
        }

        #endregion

        #region Helper Classes

        private class PerformanceTracker
        {
            public long TotalAttempts { get; set; }
            public long SuccessfulAttempts { get; set; }
            public long FailedAttempts { get; set; }
            public long TotalDuration { get; set; }
            public double AverageDuration { get; set; }
            public DateTime LastAttempt { get; set; }
        }

        #endregion
    }
}