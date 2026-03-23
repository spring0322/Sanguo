using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces
{
    /// <summary>
    /// Isolated testing of STJ deserialization without external dependencies.
    /// Validates Requirements 3.1, 3.2, 3.3, 3.4, 3.5
    /// </summary>
    public interface IDeserializationTester
    {
        /// <summary>
        /// Tests EventEffectKindTable deserialization with provided JSON data
        /// Requirement 3.1: Create minimal test cases for EventEffectKindTable
        /// </summary>
        /// <param name="jsonData">The JSON data to test deserialization with</param>
        /// <returns>Test result with detailed information</returns>
        Task<TestResult> TestEventEffectKindTableDeserialization(string jsonData);
        
        /// <summary>
        /// Tests both TroopDetail and ArchitectureDetail namespace variants
        /// Requirement 3.2: Test both TroopDetail and ArchitectureDetail variants
        /// </summary>
        /// <returns>Test result covering both namespace variants</returns>
        Task<TestResult> TestBothNamespaceVariants();
        
        /// <summary>
        /// Tests deserialization using actual game data samples
        /// Requirement 3.3: Use actual game data samples for testing
        /// </summary>
        /// <returns>Test result using real game data</returns>
        Task<TestResult> TestWithActualGameData();
        
        /// <summary>
        /// Tests edge cases including empty dictionaries and null values
        /// Requirement 3.4: Handle empty dictionaries and null values
        /// </summary>
        /// <returns>Test result covering edge cases</returns>
        Task<TestResult> TestEdgeCases();
        
        /// <summary>
        /// Generates a comprehensive test suite for all scenarios
        /// </summary>
        /// <returns>Complete test suite with all test cases</returns>
        TestSuite GenerateComprehensiveTestSuite();
    }
}