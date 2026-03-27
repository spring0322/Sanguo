using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization.SystemTextJson;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// Benchmark utility for testing different GZip compression levels
    /// Tests compression time, decompression time, and file size for each level
    /// </summary>
    public class CompressionBenchmark
    {
        /// <summary>
        /// Run compression benchmark with different compression levels
        /// </summary>
        /// <param name="scenario">The game scenario to test with</param>
        /// <param name="outputDirectory">Directory to write test files to</param>
        /// <returns>Benchmark results</returns>
        public static CompressionBenchmarkResult RunBenchmark(GameScenario scenario, string outputDirectory = null)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            
            // Use temp directory if not specified
            if (string.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = Path.Combine(Path.GetTempPath(), "CompressionBenchmark");
            }
            
            // Ensure directory exists
            Directory.CreateDirectory(outputDirectory);
            
            var result = new CompressionBenchmarkResult();
            
            // First, serialize to JSON (this is common for all tests)
            Debug.WriteLine("[CompressionBenchmark] Serializing scenario to JSON...");
            var savePhase = new Phases.SaveDataPhase();
            var dto = savePhase.ConvertToDTO(scenario);
            
            var options = GameJsonContext.GetDefaultOptions(indented: false);
            string json = System.Text.Json.JsonSerializer.Serialize(dto, JsonTypeInfoHelper.Resolve<DTOs.GameScenarioDTO>(options));
            
            long uncompressedSize = Encoding.UTF8.GetByteCount(json);
            result.UncompressedSize = uncompressedSize;
            
            Debug.WriteLine($"[CompressionBenchmark] Uncompressed JSON size: {FormatBytes(uncompressedSize)}");
            Debug.WriteLine($"[CompressionBenchmark] Testing compression levels...");
            
            // Test each compression level
            TestCompressionLevel(CompressionLevel.Fastest, json, outputDirectory, result);
            TestCompressionLevel(CompressionLevel.Optimal, json, outputDirectory, result);
            TestCompressionLevel(CompressionLevel.SmallestSize, json, outputDirectory, result);
            
            // Calculate recommendations
            result.CalculateRecommendation();
            
            // Print summary
            Debug.WriteLine($"\n[CompressionBenchmark] ===== BENCHMARK RESULTS =====");
            Debug.WriteLine($"Uncompressed Size: {FormatBytes(result.UncompressedSize)}");
            Debug.WriteLine($"\nFastest:");
            Debug.WriteLine($"  Compression Time: {result.FastestCompressionTime}ms");
            Debug.WriteLine($"  Decompression Time: {result.FastestDecompressionTime}ms");
            Debug.WriteLine($"  Compressed Size: {FormatBytes(result.FastestCompressedSize)}");
            Debug.WriteLine($"  Compression Ratio: {result.FastestCompressionRatio:F2}%");
            Debug.WriteLine($"\nOptimal:");
            Debug.WriteLine($"  Compression Time: {result.OptimalCompressionTime}ms");
            Debug.WriteLine($"  Decompression Time: {result.OptimalDecompressionTime}ms");
            Debug.WriteLine($"  Compressed Size: {FormatBytes(result.OptimalCompressedSize)}");
            Debug.WriteLine($"  Compression Ratio: {result.OptimalCompressionRatio:F2}%");
            Debug.WriteLine($"\nSmallestSize:");
            Debug.WriteLine($"  Compression Time: {result.SmallestSizeCompressionTime}ms");
            Debug.WriteLine($"  Decompression Time: {result.SmallestSizeDecompressionTime}ms");
            Debug.WriteLine($"  Compressed Size: {FormatBytes(result.SmallestSizeCompressedSize)}");
            Debug.WriteLine($"  Compression Ratio: {result.SmallestSizeCompressionRatio:F2}%");
            Debug.WriteLine($"\nRecommendation: {result.RecommendedLevel}");
            Debug.WriteLine($"  Reason: {result.RecommendationReason}");
            Debug.WriteLine($"===============================\n");
            
            return result;
        }
        
        /// <summary>
        /// Test a specific compression level
        /// </summary>
        private static void TestCompressionLevel(CompressionLevel level, string json, string outputDirectory, CompressionBenchmarkResult result)
        {
            string filePath = Path.Combine(outputDirectory, $"test_{level}.sav.gz");
            
            // Measure compression time and size
            var sw = Stopwatch.StartNew();
            
            using (var fileStream = File.Create(filePath))
            using (var gzipStream = new GZipStream(fileStream, level))
            using (var writer = new StreamWriter(gzipStream, Encoding.UTF8))
            {
                writer.Write(json);
            }
            
            sw.Stop();
            long compressionTime = sw.ElapsedMilliseconds;
            long compressedSize = new FileInfo(filePath).Length;
            
            // Measure decompression time
            sw.Restart();
            
            using (var fileStream = File.OpenRead(filePath))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
            {
                string decompressed = reader.ReadToEnd();
            }
            
            sw.Stop();
            long decompressionTime = sw.ElapsedMilliseconds;
            
            // Store results
            switch (level)
            {
                case CompressionLevel.Fastest:
                    result.FastestCompressionTime = compressionTime;
                    result.FastestDecompressionTime = decompressionTime;
                    result.FastestCompressedSize = compressedSize;
                    result.FastestCompressionRatio = (double)compressedSize / result.UncompressedSize * 100;
                    break;
                    
                case CompressionLevel.Optimal:
                    result.OptimalCompressionTime = compressionTime;
                    result.OptimalDecompressionTime = decompressionTime;
                    result.OptimalCompressedSize = compressedSize;
                    result.OptimalCompressionRatio = (double)compressedSize / result.UncompressedSize * 100;
                    break;
                    
                case CompressionLevel.SmallestSize:
                    result.SmallestSizeCompressionTime = compressionTime;
                    result.SmallestSizeDecompressionTime = decompressionTime;
                    result.SmallestSizeCompressedSize = compressedSize;
                    result.SmallestSizeCompressionRatio = (double)compressedSize / result.UncompressedSize * 100;
                    break;
            }
            
            Debug.WriteLine($"[CompressionBenchmark] {level}: Compression={compressionTime}ms, Decompression={decompressionTime}ms, Size={FormatBytes(compressedSize)} ({(double)compressedSize / result.UncompressedSize * 100:F2}%)");
        }
        
        /// <summary>
        /// Format bytes to human-readable string
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:F2} {sizes[order]}";
        }
    }
    
    /// <summary>
    /// Results from compression benchmark
    /// </summary>
    public class CompressionBenchmarkResult
    {
        // Uncompressed size
        public long UncompressedSize { get; set; }
        
        // Fastest level results
        public long FastestCompressionTime { get; set; }
        public long FastestDecompressionTime { get; set; }
        public long FastestCompressedSize { get; set; }
        public double FastestCompressionRatio { get; set; }
        
        // Optimal level results
        public long OptimalCompressionTime { get; set; }
        public long OptimalDecompressionTime { get; set; }
        public long OptimalCompressedSize { get; set; }
        public double OptimalCompressionRatio { get; set; }
        
        // SmallestSize level results
        public long SmallestSizeCompressionTime { get; set; }
        public long SmallestSizeDecompressionTime { get; set; }
        public long SmallestSizeCompressedSize { get; set; }
        public double SmallestSizeCompressionRatio { get; set; }
        
        // Recommendation
        public CompressionLevel RecommendedLevel { get; set; }
        public string RecommendationReason { get; set; }
        
        /// <summary>
        /// Calculate the recommended compression level based on benchmark results
        /// </summary>
        public void CalculateRecommendation()
        {
            // Calculate total time (compression + decompression) for each level
            long fastestTotalTime = FastestCompressionTime + FastestDecompressionTime;
            long optimalTotalTime = OptimalCompressionTime + OptimalDecompressionTime;
            long smallestSizeTotalTime = SmallestSizeCompressionTime + SmallestSizeDecompressionTime;
            
            // Calculate size savings compared to Fastest
            long optimalSavings = FastestCompressedSize - OptimalCompressedSize;
            long smallestSizeSavings = FastestCompressedSize - SmallestSizeCompressedSize;
            
            // Calculate time overhead compared to Fastest
            long optimalOverhead = optimalTotalTime - fastestTotalTime;
            long smallestSizeOverhead = smallestSizeTotalTime - fastestTotalTime;
            
            // Decision logic:
            // 1. If Optimal provides significant size savings (>5%) with minimal time overhead (<20%), recommend Optimal
            // 2. If SmallestSize provides significant additional savings (>10% over Optimal) with acceptable overhead (<50%), recommend SmallestSize
            // 3. Otherwise, recommend Fastest
            
            double optimalSavingsPercent = (double)optimalSavings / FastestCompressedSize * 100;
            double optimalOverheadPercent = (double)optimalOverhead / fastestTotalTime * 100;
            
            double smallestSizeSavingsPercent = (double)smallestSizeSavings / FastestCompressedSize * 100;
            double smallestSizeOverheadPercent = (double)smallestSizeOverhead / fastestTotalTime * 100;
            
            // Check SmallestSize first (most aggressive compression)
            if (smallestSizeSavingsPercent > 10 && smallestSizeOverheadPercent < 50)
            {
                RecommendedLevel = CompressionLevel.SmallestSize;
                RecommendationReason = $"SmallestSize provides {smallestSizeSavingsPercent:F1}% size savings with {smallestSizeOverheadPercent:F1}% time overhead - good balance for save files";
            }
            // Check Optimal (balanced compression)
            else if (optimalSavingsPercent > 5 && optimalOverheadPercent < 20)
            {
                RecommendedLevel = CompressionLevel.Optimal;
                RecommendationReason = $"Optimal provides {optimalSavingsPercent:F1}% size savings with only {optimalOverheadPercent:F1}% time overhead - best balance";
            }
            // Default to Fastest
            else
            {
                RecommendedLevel = CompressionLevel.Fastest;
                RecommendationReason = $"Fastest compression with minimal overhead - Optimal only saves {optimalSavingsPercent:F1}% but adds {optimalOverheadPercent:F1}% time";
            }
        }
    }
}
