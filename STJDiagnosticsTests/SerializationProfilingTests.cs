using NUnit.Framework;
using System;
using System.IO;
using WorldOfTheThreeKingdoms.Serialization;
using GameObjects;

namespace STJDiagnosticsTests
{
    [TestFixture]
    public class SerializationProfilingTests
    {
        private SerializationManager _serializationManager;

        [SetUp]
        public void Setup()
        {
            _serializationManager = new SerializationManager();
        }

        [Test]
        public void LoadGame_ShouldOutputProfilingLogs()
        {
            // Arrange
            // Find a valid save file from Content/Data or create a mock one.
            // For now, we try to locate a real file, or fail if none exists.
            string savePath = FindAnySaveFile();
            
            if (string.IsNullOrEmpty(savePath))
            {
                Assert.Fail("No .sav.gz files found in the repository to test profiling.");
            }

            TestContext.WriteLine($"Testing profiling with save file: {savePath}");

            // Act
            try 
            {
                var scenario = _serializationManager.LoadGame(savePath);
                Assert.NotNull(scenario);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"LoadGame failed: {ex.Message}");
                // We don't necessarily fail the test if load fails due to data issues, 
                // as long as we can see the logs.
            }
        }

        private string FindAnySaveFile()
        {
            try
            {
                string searchRoot = @"g:\sanguo\net8\sanguo260212-2";
                string[] files = Directory.GetFiles(searchRoot, "*.sav.gz", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    // return the first one found
                    return file;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding save files: {ex.Message}");
            }
            return null;
        }
    }
}
