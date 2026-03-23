using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Result of deserialization testing
    /// </summary>
    public class TestResult
    {
        public bool IsSuccessful { get; set; }
        public string TestName { get; set; }
        public TimeSpan Duration { get; set; }
        public List<TestError> Errors { get; set; } = new List<TestError>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public string DetailedReport { get; set; }
    }

    /// <summary>
    /// Error encountered during testing
    /// </summary>
    public class TestError
    {
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Context { get; set; }
    }

    /// <summary>
    /// Comprehensive test suite containing multiple test cases
    /// </summary>
    public class TestSuite
    {
        public string Name { get; set; }
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public TestConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// Individual test case within a test suite
    /// </summary>
    public class TestCase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string JsonData { get; set; }
        public Type ExpectedType { get; set; }
        public bool ShouldSucceed { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration for test execution
    /// </summary>
    public class TestConfiguration
    {
        public int TimeoutSeconds { get; set; } = 30;
        public bool CapturePerformanceMetrics { get; set; } = true;
        public bool IncludeStackTraces { get; set; } = true;
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
    }

    /// <summary>
    /// Logging levels for test execution
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}