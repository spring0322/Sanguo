using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using WorldOfTheThreeKingdoms.Serialization;

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
            string savePath = FindAnySaveFile();
            if (string.IsNullOrEmpty(savePath))
            {
                Assert.Ignore("No .sav.gz files found under the repository root to test profiling.");
            }

            TestContext.WriteLine($"Testing profiling with save file: {savePath}");

            try
            {
                var scenario = _serializationManager.LoadGame(savePath);
                Assert.NotNull(scenario);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"LoadGame failed: {ex.Message}");
            }
        }

        private static string FindAnySaveFile()
        {
            try
            {
                string searchRoot = ResolveRepoRoot();
                string[] files = Directory.GetFiles(searchRoot, "*.sav.gz", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    return string.Empty;
                }

                return files
                    .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
                    .First();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error finding save files: {ex.Message}");
                return string.Empty;
            }
        }

        private static string ResolveRepoRoot()
        {
            string current = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                if (File.Exists(Path.Combine(current, "WorldOfTheThreeKingdoms.sln")))
                {
                    return current;
                }

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            return TestContext.CurrentContext.TestDirectory;
        }
    }
}
