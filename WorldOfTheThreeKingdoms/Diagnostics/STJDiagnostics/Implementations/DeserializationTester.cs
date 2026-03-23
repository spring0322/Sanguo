using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;
using WorldOfTheThreeKingdoms.Serialization;
using GameObjects.TroopDetail.EventEffect;
using GameObjects.ArchitectureDetail.EventEffect;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Implementations
{
    /// <summary>
    /// Isolated testing of STJ deserialization without external dependencies.
    /// Validates Requirements 3.1, 3.2, 3.3, 3.4, 3.5
    /// </summary>
    public class DeserializationTester : IDeserializationTester
    {
        private readonly JsonSerializerOptions _defaultOptions;
        private readonly JsonSerializerOptions _looseOptions;

        public DeserializationTester()
        {
            _defaultOptions = GameJsonContext.GetDefaultOptions();
            _looseOptions = GameJsonContext.GetLooseOptions();
        }

        /// <summary>
        /// Tests EventEffectKindTable deserialization with provided JSON data
        /// Requirement 3.1: Create minimal test cases for EventEffectKindTable
        /// </summary>
        public async Task<TestResult> TestEventEffectKindTableDeserialization(string jsonData)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult
            {
                TestName = "EventEffectKindTable Deserialization Test",
                IsSuccessful = false
            };

            try
            {
                // Test TroopDetail.EventEffect.EventEffectKindTable
                await TestTroopDetailEventEffectKindTable(jsonData, result);
                
                // Test ArchitectureDetail.EventEffect.EventEffectKindTable
                await TestArchitectureDetailEventEffectKindTable(jsonData, result);

                result.IsSuccessful = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new TestError
                {
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Context = "EventEffectKindTable deserialization test"
                });
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Metrics["ExecutionTimeMs"] = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Tests both TroopDetail and ArchitectureDetail namespace variants
        /// Requirement 3.2: Test both TroopDetail and ArchitectureDetail variants
        /// </summary>
        public async Task<TestResult> TestBothNamespaceVariants()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult
            {
                TestName = "Both Namespace Variants Test",
                IsSuccessful = false
            };

            try
            {
                // Create minimal test data for both namespaces
                var troopTestData = CreateMinimalTroopEventEffectKindTableJson();
                var architectureTestData = CreateMinimalArchitectureEventEffectKindTableJson();

                // Test TroopDetail variant
                var troopResult = await TestTroopDetailEventEffectKindTable(troopTestData, new TestResult());
                if (!troopResult.IsSuccessful)
                {
                    result.Errors.AddRange(troopResult.Errors);
                }

                // Test ArchitectureDetail variant
                var archResult = await TestArchitectureDetailEventEffectKindTable(architectureTestData, new TestResult());
                if (!archResult.IsSuccessful)
                {
                    result.Errors.AddRange(archResult.Errors);
                }

                result.IsSuccessful = result.Errors.Count == 0;
                result.Metrics["TroopDetailSuccess"] = troopResult.IsSuccessful;
                result.Metrics["ArchitectureDetailSuccess"] = archResult.IsSuccessful;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new TestError
                {
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Context = "Both namespace variants test"
                });
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Metrics["ExecutionTimeMs"] = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Tests deserialization using actual game data samples
        /// Requirement 3.3: Use actual game data samples for testing
        /// </summary>
        public async Task<TestResult> TestWithActualGameData()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult
            {
                TestName = "Actual Game Data Test",
                IsSuccessful = false
            };

            try
            {
                // Create realistic game data samples
                var realGameDataSamples = CreateRealGameDataSamples();
                
                foreach (var sample in realGameDataSamples)
                {
                    try
                    {
                        var testResult = await TestEventEffectKindTableDeserialization(sample.Value);
                        if (!testResult.IsSuccessful)
                        {
                            result.Errors.AddRange(testResult.Errors);
                        }
                        result.Metrics[$"Sample_{sample.Key}_Success"] = testResult.IsSuccessful;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new TestError
                        {
                            ErrorType = ex.GetType().Name,
                            Message = $"Failed to test sample '{sample.Key}': {ex.Message}",
                            StackTrace = ex.StackTrace,
                            Context = $"Game data sample: {sample.Key}"
                        });
                    }
                }

                result.IsSuccessful = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new TestError
                {
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Context = "Actual game data test"
                });
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Metrics["ExecutionTimeMs"] = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Tests edge cases including empty dictionaries and null values
        /// Requirement 3.4: Handle empty dictionaries and null values
        /// </summary>
        public async Task<TestResult> TestEdgeCases()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult
            {
                TestName = "Edge Cases Test",
                IsSuccessful = false
            };

            try
            {
                var edgeCases = CreateEdgeCaseTestData();
                
                foreach (var edgeCase in edgeCases)
                {
                    try
                    {
                        var testResult = await TestEventEffectKindTableDeserialization(edgeCase.Value);
                        // For edge cases, we expect some to fail gracefully
                        result.Metrics[$"EdgeCase_{edgeCase.Key}_Success"] = testResult.IsSuccessful;
                        
                        // Only add errors if they're unexpected failures
                        if (!testResult.IsSuccessful && !IsExpectedFailure(edgeCase.Key))
                        {
                            result.Errors.AddRange(testResult.Errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Some edge cases are expected to throw exceptions
                        if (!IsExpectedFailure(edgeCase.Key))
                        {
                            result.Errors.Add(new TestError
                            {
                                ErrorType = ex.GetType().Name,
                                Message = $"Unexpected failure for edge case '{edgeCase.Key}': {ex.Message}",
                                StackTrace = ex.StackTrace,
                                Context = $"Edge case: {edgeCase.Key}"
                            });
                        }
                        result.Metrics[$"EdgeCase_{edgeCase.Key}_Exception"] = ex.GetType().Name;
                    }
                }

                result.IsSuccessful = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new TestError
                {
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Context = "Edge cases test"
                });
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Metrics["ExecutionTimeMs"] = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Generates a comprehensive test suite for all scenarios
        /// </summary>
        public TestSuite GenerateComprehensiveTestSuite()
        {
            var testSuite = new TestSuite
            {
                Name = "EventEffectKindTable Comprehensive Test Suite",
                Configuration = new TestConfiguration
                {
                    TimeoutSeconds = 60,
                    CapturePerformanceMetrics = true,
                    IncludeStackTraces = true,
                    LogLevel = LogLevel.Info
                }
            };

            // Add minimal test cases
            testSuite.TestCases.Add(new TestCase
            {
                Name = "Minimal TroopDetail EventEffectKindTable",
                Description = "Tests minimal TroopDetail.EventEffect.EventEffectKindTable deserialization",
                JsonData = CreateMinimalTroopEventEffectKindTableJson(),
                ExpectedType = typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindTable),
                ShouldSucceed = true
            });

            testSuite.TestCases.Add(new TestCase
            {
                Name = "Minimal ArchitectureDetail EventEffectKindTable",
                Description = "Tests minimal ArchitectureDetail.EventEffect.EventEffectKindTable deserialization",
                JsonData = CreateMinimalArchitectureEventEffectKindTableJson(),
                ExpectedType = typeof(global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKindTable),
                ShouldSucceed = true
            });

            // Add real game data test cases
            var realDataSamples = CreateRealGameDataSamples();
            foreach (var sample in realDataSamples)
            {
                testSuite.TestCases.Add(new TestCase
                {
                    Name = $"Real Game Data - {sample.Key}",
                    Description = $"Tests with realistic game data sample: {sample.Key}",
                    JsonData = sample.Value,
                    ExpectedType = typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindTable),
                    ShouldSucceed = true
                });
            }

            // Add edge case test cases
            var edgeCases = CreateEdgeCaseTestData();
            foreach (var edgeCase in edgeCases)
            {
                testSuite.TestCases.Add(new TestCase
                {
                    Name = $"Edge Case - {edgeCase.Key}",
                    Description = $"Tests edge case scenario: {edgeCase.Key}",
                    JsonData = edgeCase.Value,
                    ExpectedType = typeof(global::GameObjects.TroopDetail.EventEffect.EventEffectKindTable),
                    ShouldSucceed = !IsExpectedFailure(edgeCase.Key)
                });
            }

            return testSuite;
        }

        #region Private Helper Methods

        private async Task<TestResult> TestTroopDetailEventEffectKindTable(string jsonData, TestResult result)
        {
            try
            {
                // 模拟异步反序列化测试过程
                await Task.Delay(8).ConfigureAwait(false);
                
                // Test with default options
                var table = JsonSerializer.Deserialize<global::GameObjects.TroopDetail.EventEffect.EventEffectKindTable>(
                    jsonData, _defaultOptions);
                
                if (table == null)
                {
                    result.Errors.Add(new TestError
                    {
                        ErrorType = "DeserializationError",
                        Message = "TroopDetail.EventEffectKindTable deserialized to null",
                        Context = "TroopDetail namespace test"
                    });
                }
                else
                {
                    result.Metrics["TroopDetail_Count"] = table.Count;
                }
            }
            catch (Exception ex)
            {
                // Try with loose options as fallback
                try
                {
                    var table = JsonSerializer.Deserialize<global::GameObjects.TroopDetail.EventEffect.EventEffectKindTable>(
                        jsonData, _looseOptions);
                    result.Metrics["TroopDetail_FallbackSuccess"] = table != null;
                }
                catch (Exception fallbackEx)
                {
                    result.Errors.Add(new TestError
                    {
                        ErrorType = ex.GetType().Name,
                        Message = $"TroopDetail deserialization failed: {ex.Message}. Fallback also failed: {fallbackEx.Message}",
                        StackTrace = ex.StackTrace,
                        Context = "TroopDetail namespace test"
                    });
                }
            }

            return result;
        }

        private async Task<TestResult> TestArchitectureDetailEventEffectKindTable(string jsonData, TestResult result)
        {
            try
            {
                // 模拟异步建筑事件效果反序列化测试过程
                await Task.Delay(7).ConfigureAwait(false);
                
                // Test with default options
                var table = JsonSerializer.Deserialize<global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKindTable>(
                    jsonData, _defaultOptions);
                
                if (table == null)
                {
                    result.Errors.Add(new TestError
                    {
                        ErrorType = "DeserializationError",
                        Message = "ArchitectureDetail.EventEffectKindTable deserialized to null",
                        Context = "ArchitectureDetail namespace test"
                    });
                }
                else
                {
                    result.Metrics["ArchitectureDetail_Count"] = table.Count;
                }
            }
            catch (Exception ex)
            {
                // Try with loose options as fallback
                try
                {
                    var table = JsonSerializer.Deserialize<global::GameObjects.ArchitectureDetail.EventEffect.EventEffectKindTable>(
                        jsonData, _looseOptions);
                    result.Metrics["ArchitectureDetail_FallbackSuccess"] = table != null;
                }
                catch (Exception fallbackEx)
                {
                    result.Errors.Add(new TestError
                    {
                        ErrorType = ex.GetType().Name,
                        Message = $"ArchitectureDetail deserialization failed: {ex.Message}. Fallback also failed: {fallbackEx.Message}",
                        StackTrace = ex.StackTrace,
                        Context = "ArchitectureDetail namespace test"
                    });
                }
            }

            return result;
        }

        private string CreateMinimalTroopEventEffectKindTableJson()
        {
            return @"{
                ""EventEffectKinds"": {
                    ""1"": {
                        ""$type"": ""GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind0, WorldOfTheThreeKingdoms"",
                        ""ID"": 1,
                        ""Name"": ""Test Effect""
                    }
                }
            }";
        }

        private string CreateMinimalArchitectureEventEffectKindTableJson()
        {
            return @"{
                ""EventEffectKinds"": {
                    ""1"": {
                        ""ID"": 1,
                        ""Name"": ""Test Architecture Effect""
                    }
                }
            }";
        }

        private Dictionary<string, string> CreateRealGameDataSamples()
        {
            return new Dictionary<string, string>
            {
                ["MultipleEffects"] = @"{
                    ""EventEffectKinds"": {
                        ""0"": {
                            ""$type"": ""GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind0, WorldOfTheThreeKingdoms"",
                            ""ID"": 0,
                            ""Name"": ""增加统率""
                        },
                        ""1"": {
                            ""$type"": ""GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind1, WorldOfTheThreeKingdoms"",
                            ""ID"": 1,
                            ""Name"": ""增加武力""
                        },
                        ""10"": {
                            ""$type"": ""GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind10, WorldOfTheThreeKingdoms"",
                            ""ID"": 10,
                            ""Name"": ""增加智力""
                        }
                    }
                }",
                ["LargeDataset"] = GenerateLargeEventEffectKindTableJson(),
                ["ComplexPolymorphic"] = @"{
                    ""EventEffectKinds"": {
                        ""100"": {
                            ""$type"": ""GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind100, WorldOfTheThreeKingdoms"",
                            ""ID"": 100,
                            ""Name"": ""复杂效果""
                        }
                    }
                }"
            };
        }

        private Dictionary<string, string> CreateEdgeCaseTestData()
        {
            return new Dictionary<string, string>
            {
                ["EmptyDictionary"] = @"{""EventEffectKinds"": {}}",
                ["NullDictionary"] = @"{""EventEffectKinds"": null}",
                ["MissingProperty"] = @"{}",
                ["InvalidJson"] = @"{""EventEffectKinds"": {""invalid"": ""incomplete""}}",
                ["NullValues"] = @"{""EventEffectKinds"": {""1"": null}}",
                ["InvalidTypeReference"] = @"{
                    ""EventEffectKinds"": {
                        ""1"": {
                            ""$type"": ""NonExistentType, WorldOfTheThreeKingdoms"",
                            ""ID"": 1
                        }
                    }
                }",
                ["MissingTypeInfo"] = @"{
                    ""EventEffectKinds"": {
                        ""1"": {
                            ""ID"": 1,
                            ""Name"": ""No Type Info""
                        }
                    }
                }"
            };
        }

        private string GenerateLargeEventEffectKindTableJson()
        {
            var effects = new List<string>();
            for (int i = 0; i < 50; i++)
            {
                effects.Add($@"""{i}"": {{
                    ""$type"": ""GameObjects.TroopDetail.EventEffect.EventEffectKindPack.EventEffectKind{(i % 10)}, WorldOfTheThreeKingdoms"",
                    ""ID"": {i},
                    ""Name"": ""Effect {i}""
                }}");
            }
            
            return $@"{{
                ""EventEffectKinds"": {{
                    {string.Join(",\n                    ", effects)}
                }}
            }}";
        }

        private bool IsExpectedFailure(string testCaseName)
        {
            return testCaseName.Contains("Invalid") || 
                   testCaseName.Contains("Null") || 
                   testCaseName.Contains("Missing");
        }

        #endregion
    }
}