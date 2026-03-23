using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Result of validation operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string RecommendedAction { get; set; }
    }

    /// <summary>
    /// Validation warning details
    /// </summary>
    public class ValidationWarning
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string Recommendation { get; set; }
    }
}