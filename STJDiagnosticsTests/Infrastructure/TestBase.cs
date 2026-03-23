using NUnit.Framework;
using FsCheck;
using System.Collections.Generic;
using System.Text.Json;

namespace STJDiagnosticsTests.Infrastructure
{
    /// <summary>
    /// Base class for all diagnostic tests providing common setup and utilities
    /// </summary>
    [TestFixture]
    public abstract class TestBase
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Configure FsCheck for property-based testing
            // Note: TestArbitraries registration will be handled per test as needed
        }

        [SetUp]
        public virtual void SetUp()
        {
            // Common setup for each test
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Common cleanup for each test
        }

        /// <summary>
        /// Helper method to create test JSON data
        /// </summary>
        protected string CreateTestJsonData(Dictionary<string, object> data)
        {
            return JsonSerializer.Serialize(data);
        }
    }
}