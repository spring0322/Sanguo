using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using WorldOfTheThreeKingdoms.GameGlobal;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Implementations
{
    /// <summary>
    /// Graceful error recovery and system stability maintenance.
    /// Implements Requirements 4.1, 4.2, 4.3, 4.4, 4.5
    /// </summary>
    public class FallbackHandler : IFallbackHandler
    {
        private readonly List<JsonSerializerOptions> _fallbackOptions;
        private readonly Dictionary<Type, object> _defaultValueCache;
        private readonly List<ErrorContext> _errorHistory;
        private readonly object _lockObject = new object();

        public FallbackHandler()
        {
            _fallbackOptions = CreateFallbackOptions();
            _defaultValueCache = new Dictionary<Type, object>();
            _errorHistory = new List<ErrorContext>();
        }

        /// <summary>
        /// Handles deserialization failures with progressive fallback strategy
        /// Requirement 4.1: Provide graceful error recovery
        /// Requirement 4.5: Maintain system stability and continue operation
        /// </summary>
        public async Task<T> HandleDeserializationFailure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            string jsonData, 
            JsonSerializerOptions primaryOptions,
            Exception originalException)
        {
            var errorContext = CreateErrorContext(originalException, jsonData, typeof(T), "HandleDeserializationFailure");
            
            try
            {
                // Progressive fallback strategy
                
                // Step 1: Try with relaxed JsonSerializerOptions
                foreach (var fallbackOption in _fallbackOptions)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<T>(jsonData, fallbackOption);
                        if (result != null)
                        {
                            LogDiagnosticInformation(originalException, 
                                $"Successfully recovered using fallback options: {GetOptionsDescription(fallbackOption)}");
                            return result;
                        }
                    }
                    catch (Exception fallbackException)
                    {
                        // Continue to next fallback option
                        errorContext.AdditionalData[$"FallbackAttempt_{GetOptionsDescription(fallbackOption)}"] = fallbackException.Message;
                    }
                }

                // Step 2: Attempt JSON data repair
                if (await AttemptDataRepair(jsonData, typeof(T)))
                {
                    var repairedJson = await RepairJsonData(jsonData, typeof(T));
                    if (!string.IsNullOrEmpty(repairedJson))
                    {
                        try
                        {
                            var result = JsonSerializer.Deserialize<T>(repairedJson, primaryOptions);
                            if (result != null)
                            {
                                LogDiagnosticInformation(originalException, 
                                    $"Successfully recovered using repaired JSON data");
                                return result;
                            }
                        }
                        catch (Exception repairException)
                        {
                            errorContext.AdditionalData["RepairAttemptException"] = repairException.Message;
                        }
                    }
                }

                // Step 3: Use default values as last resort
                var defaultValue = await GetDefaultValueSafely<T>();
                LogDiagnosticInformation(originalException, 
                    $"Using default values as final fallback for type {typeof(T).Name}");
                
                return defaultValue;
            }
            catch (Exception fallbackException)
            {
                // Final escalation with comprehensive error context
                errorContext.AdditionalData["FinalFallbackException"] = fallbackException.Message;
                LogDiagnosticInformation(fallbackException, 
                    $"All fallback mechanisms failed for type {typeof(T).Name}");
                
                // Requirement 4.4: Escalate with comprehensive error context
                throw new InvalidOperationException(
                    $"Complete deserialization failure for type {typeof(T).Name}. " +
                    $"Original error: {originalException.Message}. " +
                    $"All fallback mechanisms exhausted.", 
                    originalException);
            }
            finally
            {
                lock (_lockObject)
                {
                    _errorHistory.Add(errorContext);
                    // Keep only last 100 errors to prevent memory leaks
                    if (_errorHistory.Count > 100)
                    {
                        _errorHistory.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to repair corrupted JSON data
        /// Requirement 4.3: Attempt data repair or use default values
        /// </summary>
        public async Task<bool> AttemptDataRepair(string jsonData, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(jsonData))
                return false;

            try
            {
                var repairedJson = await RepairJsonData(jsonData, targetType);
                return !string.IsNullOrEmpty(repairedJson) && repairedJson != jsonData;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Provides default values when deserialization fails completely
        /// Requirement 4.3: Use default values when data repair fails
        /// </summary>
        public async Task<T> UseDefaultValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] T>() where T : new()
        {
            var type = typeof(T);
            
            lock (_lockObject)
            {
                if (_defaultValueCache.TryGetValue(type, out var cachedDefault))
                {
                    return (T)cachedDefault;
                }
            }

            T defaultValue;
            
            // Handle value types and reference types differently
            if (type.IsValueType)
            {
                defaultValue = default(T);
            }
            else
            {
                defaultValue = await CreateDefaultValue<T>();
            }
            
            lock (_lockObject)
            {
                _defaultValueCache[type] = defaultValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Logs comprehensive diagnostic information for failures
        /// Requirement 4.2: Log detailed diagnostic information
        /// Requirement 4.4: Escalate with comprehensive error context
        /// </summary>
        public void LogDiagnosticInformation(Exception exception, string context)
        {
            var errorContext = CreateErrorContext(exception, null, null, context);
            
            var logMessage = $"[STJ Fallback Handler] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n" +
                           $"Context: {context}\n" +
                           $"Exception: {exception.GetType().Name}\n" +
                           $"Message: {exception.Message}\n" +
                           $"Stack Trace: {exception.StackTrace}\n" +
                           $"Environment: {errorContext.Environment.MachineName} | {errorContext.Environment.ProcessName} ({errorContext.Environment.ProcessId})\n" +
                           $"Memory Usage: {errorContext.Environment.MemoryUsage:N0} bytes\n" +
                           $"Runtime Version: {errorContext.Environment.RuntimeVersion}\n";

            if (errorContext.AdditionalData.Any())
            {
                logMessage += "Additional Data:\n";
                foreach (var kvp in errorContext.AdditionalData)
                {
                    logMessage += $"  {kvp.Key}: {kvp.Value}\n";
                }
            }

            // Log to console for immediate visibility
            Console.WriteLine(logMessage);
            
            // Log to debug output for development
            Debug.WriteLine(logMessage);
            
            // Attempt to log to file for persistence
            try
            {
                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logDirectory);
                
                var logFile = Path.Combine(logDirectory, $"STJFallback_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, logMessage + "\n" + new string('=', 80) + "\n");
            }
            catch
            {
                // Ignore file logging errors to maintain system stability
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Safely gets default values for any type, handling both value and reference types
        /// </summary>
        private async Task<T> GetDefaultValueSafely<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] T>()
        {
            var type = typeof(T);
            
            // For value types, just return default
            if (type.IsValueType)
            {
                return default(T);
            }
            
            // For string type, return empty string instead of null
            if (type == typeof(string))
            {
                return (T)(object)string.Empty;
            }
            
            // For reference types, try to create an instance if it has a parameterless constructor
            if (type.GetConstructor(Type.EmptyTypes) != null && !type.IsAbstract)
            {
                try
                {
                    // Use reflection to call UseDefaultValues with the proper constraint
                    var method = typeof(FallbackHandler).GetMethod("UseDefaultValues", new Type[0]);
                    var genericMethod = method.MakeGenericMethod(type);
                    var task = (Task<T>)genericMethod.Invoke(this, new object[0]);
                    return await task;
                }
                catch
                {
                    // If creation fails, try to return a sensible default
                    if (type == typeof(string))
                        return (T)(object)string.Empty;
                    return default(T);
                }
            }
            
            // For types without parameterless constructors, return sensible defaults
            if (type == typeof(string))
                return (T)(object)string.Empty;
            
            return default(T);
        }

        /// <summary>
        /// Creates progressive fallback JsonSerializerOptions with increasingly relaxed validation
        /// </summary>
        private List<JsonSerializerOptions> CreateFallbackOptions()
        {
            return new List<JsonSerializerOptions>
            {
                // Option 1: Allow trailing commas and comments
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNameCaseInsensitive = true
                },
                
                // Option 2: More relaxed number handling
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNameCaseInsensitive = true
                },
                
                // Option 3: Maximum relaxation
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNameCaseInsensitive = true,
                    IgnoreReadOnlyProperties = true,
                    IgnoreReadOnlyFields = true
                }
            };
        }

        /// <summary>
        /// Attempts to repair common JSON corruption patterns
        /// </summary>
        private async Task<string> RepairJsonData(string jsonData, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(jsonData))
                return string.Empty;

            var repairedJson = jsonData;

            // Fix 1: Remove trailing commas
            repairedJson = RegexPatterns.TrailingComma().Replace(repairedJson, "$1");

            // Fix 2: Fix unescaped quotes in string values
            repairedJson = RegexPatterns.UnescapedQuotes().Replace(repairedJson, ": \"$1\\\"$2\"");

            // Fix 3: Add missing quotes around property names
            repairedJson = RegexPatterns.MissingQuotes().Replace(repairedJson, "$1\"$2\":");

            // Fix 4: Handle infinity and NaN values
            repairedJson = repairedJson.Replace("Infinity", "null")
                                     .Replace("-Infinity", "null")
                                     .Replace("NaN", "null");

            // Fix 5: Remove duplicate commas
            repairedJson = RegexPatterns.DuplicateCommas().Replace(repairedJson, ",");

            // Fix 6: Ensure proper array/object closure
            var openBraces = repairedJson.Count(c => c == '{');
            var closeBraces = repairedJson.Count(c => c == '}');
            var openBrackets = repairedJson.Count(c => c == '[');
            var closeBrackets = repairedJson.Count(c => c == ']');

            // Add missing closing braces/brackets
            for (int i = 0; i < openBraces - closeBraces; i++)
                repairedJson += "}";
            for (int i = 0; i < openBrackets - closeBrackets; i++)
                repairedJson += "]";

            return await Task.FromResult(repairedJson);
        }

        /// <summary>
        /// Creates a default value for the specified reference type with sensible defaults
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'", 
            Justification = "PropertyType comes from known game object types with public constructors")]
        private async Task<T> CreateDefaultValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] T>() where T : new()
        {
            var type = typeof(T);
            
            // Only handle reference types here
            if (type.IsValueType)
            {
                return default(T);
            }
            
            var instance = new T();

            // Special handling for common game data types
            if (type.Name.Contains("EventEffectKindTable"))
            {
                // For EventEffectKindTable, create an empty dictionary
                var dictProperty = type.GetProperties()
                    .FirstOrDefault(p => p.PropertyType.IsGenericType && 
                                        p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>));
                
                if (dictProperty != null)
                {
                    var dictType = dictProperty.PropertyType;
                    var emptyDict = Activator.CreateInstance(dictType);
                    dictProperty.SetValue(instance, emptyDict);
                }
            }

            // Initialize collections to empty rather than null
            var collectionProperties = type.GetProperties()
                .Where(p => p.CanWrite && 
                           (p.PropertyType.IsGenericType && 
                            (p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                             p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                             p.PropertyType.GetGenericTypeDefinition() == typeof(HashSet<>))));

            foreach (var prop in collectionProperties)
            {
                if (prop.GetValue(instance) == null)
                {
                    var emptyCollection = Activator.CreateInstance(prop.PropertyType);
                    prop.SetValue(instance, emptyCollection);
                }
            }

            return await Task.FromResult(instance);
        }

        /// <summary>
        /// Creates comprehensive error context for diagnostic purposes
        /// </summary>
        private ErrorContext CreateErrorContext(Exception exception, string jsonData, Type targetType, string operationContext)
        {
            var process = Process.GetCurrentProcess();
            
            return new ErrorContext
            {
                Exception = exception,
                JsonData = jsonData?.Length > 1000 ? jsonData.Substring(0, 1000) + "..." : jsonData,
                TargetType = targetType,
                Timestamp = DateTime.UtcNow,
                OperationContext = operationContext,
                StackTrace = exception?.StackTrace,
                Environment = new EnvironmentInfo
                {
                    MachineName = Environment.MachineName,
                    UserName = Environment.UserName,
                    ProcessName = process.ProcessName,
                    ProcessId = process.Id,
                    MemoryUsage = process.WorkingSet64,
                    RuntimeVersion = Environment.Version.ToString(),
                    EnvironmentVariables = Environment.GetEnvironmentVariables()
                        .Cast<System.Collections.DictionaryEntry>()
                        .Where(entry => entry.Key.ToString().StartsWith("DOTNET_") || 
                                       entry.Key.ToString().StartsWith("ASPNETCORE_"))
                        .ToDictionary(entry => entry.Key.ToString(), entry => entry.Value?.ToString() ?? "")
                },
                AdditionalData = new Dictionary<string, object>
                {
                    ["ErrorHistoryCount"] = _errorHistory.Count,
                    ["DefaultValueCacheSize"] = _defaultValueCache.Count,
                    ["FallbackOptionsCount"] = _fallbackOptions.Count
                }
            };
        }

        /// <summary>
        /// Gets a description of JsonSerializerOptions for logging
        /// </summary>
        private string GetOptionsDescription(JsonSerializerOptions options)
        {
            var features = new List<string>();
            
            if (options.AllowTrailingCommas) features.Add("TrailingCommas");
            if (options.ReadCommentHandling != JsonCommentHandling.Disallow) features.Add("Comments");
            if (options.PropertyNameCaseInsensitive) features.Add("CaseInsensitive");
            if (options.IgnoreReadOnlyProperties) features.Add("IgnoreReadOnly");
            
            return features.Any() ? string.Join("|", features) : "Default";
        }

        #endregion
    }
}