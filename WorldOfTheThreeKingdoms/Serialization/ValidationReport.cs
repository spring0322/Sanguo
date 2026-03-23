using System;
using System.Collections.Generic;
using System.Text;
using WorldOfTheThreeKingdoms.Tools;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// Validation report for tracking errors, warnings, and fixes during data validation
    /// Used by ValidationPhase to report data integrity issues and sanitization operations
    /// </summary>
    public class ValidationReport
    {
        /// <summary>
        /// List of critical errors that prevent game from loading
        /// </summary>
        public List<string> Errors { get; } = new List<string>();
        
        /// <summary>
        /// List of warnings about data inconsistencies that were automatically fixed
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();
        
        /// <summary>
        /// List of automatic fixes applied to sanitize data
        /// </summary>
        public List<string> Fixes { get; } = new List<string>();
        
        /// <summary>
        /// List of informational messages (not errors or warnings)
        /// </summary>
        public List<string> Info { get; } = new List<string>();
        
        /// <summary>
        /// Whether the report contains any errors
        /// </summary>
        public bool HasErrors => Errors.Count > 0;
        
        /// <summary>
        /// Whether the report contains any warnings
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;
        
        /// <summary>
        /// Whether the report contains any fixes
        /// </summary>
        public bool HasFixes => Fixes.Count > 0;
        
        /// <summary>
        /// Whether the report contains any info messages
        /// </summary>
        public bool HasInfo => Info.Count > 0;
        
        /// <summary>
        /// Add an error to the report and log it
        /// </summary>
        /// <param name="message">Error message</param>
        public void AddError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            
            Errors.Add(message);
            DebugLogger.Error(DebugLogger.LogCategory.Validation, message);
        }
        
        /// <summary>
        /// Add a warning to the report and log it
        /// </summary>
        /// <param name="message">Warning message</param>
        public void AddWarning(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            
            Warnings.Add(message);
            DebugLogger.Warning(DebugLogger.LogCategory.Validation, message);
        }
        
        /// <summary>
        /// Add a fix to the report and log it
        /// </summary>
        /// <param name="message">Fix message describing what was corrected</param>
        public void AddFix(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            
            Fixes.Add(message);
            DebugLogger.Info(DebugLogger.LogCategory.Validation, $"自动修复: {message}");
        }
        
        /// <summary>
        /// Add an informational message to the report and log it
        /// </summary>
        /// <param name="message">Informational message</param>
        public void AddInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            
            Info.Add(message);
            
            // Log to debug output (only in verbose mode)
            // System.Diagnostics.Debug.WriteLine($"[Validation] INFO: {message}");
        }
        
        /// <summary>
        /// Get a formatted summary of the validation report
        /// </summary>
        /// <returns>Multi-line string with all errors, warnings, and fixes</returns>
        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Validation Report ===");
            sb.AppendLine();
            
            // Report errors
            if (Errors.Count > 0)
            {
                sb.AppendLine($"Errors: {Errors.Count}");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  - {error}");
                }
                sb.AppendLine();
            }
            
            // Report warnings
            if (Warnings.Count > 0)
            {
                sb.AppendLine($"Warnings: {Warnings.Count}");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
                sb.AppendLine();
            }
            
            // Report fixes
            if (Fixes.Count > 0)
            {
                sb.AppendLine($"Fixes Applied: {Fixes.Count}");
                foreach (var fix in Fixes)
                {
                    sb.AppendLine($"  - {fix}");
                }
                sb.AppendLine();
            }
            
            // Summary line
            if (!HasErrors && !HasWarnings && !HasFixes)
            {
                sb.AppendLine("No issues found. Data is valid.");
            }
            else
            {
                sb.AppendLine($"Total: {Errors.Count} errors, {Warnings.Count} warnings, {Fixes.Count} fixes, {Info.Count} info");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Clear all errors, warnings, and fixes
        /// </summary>
        public void Clear()
        {
            Errors.Clear();
            Warnings.Clear();
            Fixes.Clear();
            Info.Clear();
        }
    }
}
