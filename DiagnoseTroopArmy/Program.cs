using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using WorldOfTheThreeKingdoms.Serialization.DTOs;

namespace DiagnoseTroopArmy;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("=== 诊断 Troop.Army 空引用问题 ===\n");

        if (!TryResolveSaveFilePath(args, out string saveFilePath, out string error))
        {
            Console.WriteLine(error);
            return 1;
        }

        Console.WriteLine($"✅ 使用存档: {saveFilePath}\n");
        return AnalyzeSaveFile(saveFilePath);
    }

    static int AnalyzeSaveFile(string saveFilePath)
    {
        Console.WriteLine($"📁 分析存档: {Path.GetFileName(saveFilePath)}\n");

        try
        {
            using var fileStream = File.OpenRead(saveFilePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);
            string json = reader.ReadToEnd();

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            Console.WriteLine("=== JSON 结构分析 ===");
            Console.WriteLine($"根元素类型: {root.ValueKind}");
            if (root.ValueKind == JsonValueKind.Object)
            {
                Console.WriteLine("根元素属性:");
                foreach (JsonProperty prop in root.EnumerateObject())
                {
                    Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"    数组长度: {prop.Value.GetArrayLength()}");
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object && (prop.Name == "Troops" || prop.Name == "Militaries"))
                    {
                        Console.WriteLine("    对象属性:");
                        int count = 0;
                        foreach (JsonProperty subProp in prop.Value.EnumerateObject())
                        {
                            Console.WriteLine($"      - {subProp.Name}: {subProp.Value.ValueKind}");
                            if (++count >= 3)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            Console.WriteLine();

            JsonSerializerOptions jsonOptions = new()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            List<TroopDTO>? troops = null;
            if (root.TryGetProperty("Troops", out JsonElement troopsElement))
            {
                try
                {
                    if (troopsElement.TryGetProperty("$values", out JsonElement troopsArray))
                    {
                        troops = JsonSerializer.Deserialize<List<TroopDTO>>(troopsArray.GetRawText(), jsonOptions);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 无法解析 Troops: {ex.Message}");
                }
            }

            List<MilitaryDTO>? militaries = null;
            if (root.TryGetProperty("Militaries", out JsonElement militariesElement))
            {
                try
                {
                    if (militariesElement.TryGetProperty("$values", out JsonElement militariesArray))
                    {
                        militaries = JsonSerializer.Deserialize<List<MilitaryDTO>>(militariesArray.GetRawText(), jsonOptions);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 无法解析 Militaries: {ex.Message}");
                }
            }

            Console.WriteLine("✅ 存档加载成功");
            Console.WriteLine($"   - Troops: {troops?.Count ?? 0}");
            Console.WriteLine($"   - Militaries: {militaries?.Count ?? 0}\n");

            if (militaries != null && militaries.Count > 0)
            {
                Console.WriteLine("=== Military 数据分析 ===\n");
                foreach (MilitaryDTO military in militaries)
                {
                    Console.WriteLine($"Military {military.ID} ({military.Name}):");
                    Console.WriteLine($"  - KindID: {military.KindID}");
                    Console.WriteLine($"  - BelongedArchitectureID: {military.BelongedArchitectureID}");
                    Console.WriteLine($"  - Quantity: {military.Quantity}");
                    Console.WriteLine($"  - ShelledMilitaryID: {military.ShelledMilitaryID}");
                    Console.WriteLine();
                }
            }

            if (troops == null || troops.Count == 0)
            {
                Console.WriteLine("⚠️ 存档中没有 Troop 数据（可能是旧格式存档）");
                Console.WriteLine("\n建议：请创建一个新存档后再次运行诊断。");
                return 0;
            }

            Console.WriteLine("=== Troop.MilitaryID 分析 ===\n");

            int validCount = 0;
            int zeroCount = 0;
            int negativeCount = 0;

            foreach (TroopDTO troop in troops)
            {
                if (troop.MilitaryID > 0)
                {
                    validCount++;
                    bool exists = militaries?.Exists(m => m.ID == troop.MilitaryID) ?? false;
                    if (!exists)
                    {
                        Console.WriteLine($"⚠️ Troop {troop.ID} ({troop.Name}): MilitaryID={troop.MilitaryID} 不存在！");
                    }
                }
                else if (troop.MilitaryID == 0)
                {
                    zeroCount++;
                    Console.WriteLine($"⚠️ Troop {troop.ID} ({troop.Name}): MilitaryID=0");
                }
                else
                {
                    negativeCount++;
                    Console.WriteLine($"⚠️ Troop {troop.ID} ({troop.Name}): MilitaryID={troop.MilitaryID}");
                }
            }

            int invalidCount = zeroCount + negativeCount;

            Console.WriteLine("\n=== 统计结果 ===");
            Console.WriteLine($"有效 MilitaryID (> 0): {validCount}");
            Console.WriteLine($"无效 MilitaryID (= 0): {zeroCount}");
            Console.WriteLine($"无效 MilitaryID (< 0): {negativeCount}");
            Console.WriteLine($"总计无效: {invalidCount} / {troops.Count}");

            if (invalidCount > 0)
            {
                Console.WriteLine($"\n❌ 发现 {invalidCount} 个 Troop 的 MilitaryID 无效！");
                Console.WriteLine("这会导致 Troop.Army 为 null，引发空引用异常。");
                return 1;
            }

            Console.WriteLine("\n✅ 所有 Troop 的 MilitaryID 都有效");

            if (militaries != null && militaries.Count > 0)
            {
                Console.WriteLine("\n=== 可用的 Military IDs ===");
                List<int> militaryIds = militaries.Select(m => m.ID).OrderBy(id => id).ToList();
                Console.WriteLine(string.Join(", ", militaryIds.Take(20)));
                if (militaryIds.Count > 20)
                {
                    Console.WriteLine($"... (共 {militaryIds.Count} 个)");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static bool TryResolveSaveFilePath(string[] args, out string saveFilePath, out string error)
    {
        string repoRoot = ResolveRepoRoot();

        if (args.Length > 0)
        {
            string candidate = ResolvePath(repoRoot, args[0]);
            return TryResolveCandidate(candidate, out saveFilePath, out error);
        }

        string[] saveFiles = Directory.GetFiles(repoRoot, "*.sav.gz", SearchOption.AllDirectories);
        if (saveFiles.Length == 0)
        {
            saveFilePath = string.Empty;
            error = $"❌ 未找到存档文件 (*.sav.gz)。请传入存档文件或存档目录路径。仓库根: {repoRoot}";
            return false;
        }

        saveFilePath = saveFiles
            .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
            .First();
        error = string.Empty;
        return true;
    }

    static bool TryResolveCandidate(string candidate, out string saveFilePath, out string error)
    {
        if (File.Exists(candidate))
        {
            saveFilePath = candidate;
            error = string.Empty;
            return true;
        }

        if (Directory.Exists(candidate))
        {
            string[] saveFiles = Directory.GetFiles(candidate, "*.sav.gz", SearchOption.TopDirectoryOnly);
            if (saveFiles.Length > 0)
            {
                saveFilePath = saveFiles
                    .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
                    .First();
                error = string.Empty;
                return true;
            }
        }

        saveFilePath = string.Empty;
        error = $"❌ 找不到有效存档文件或目录: {candidate}";
        return false;
    }

    static string ResolveRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(current, "WorldOfTheThreeKingdoms.sln")))
            {
                return current;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }

            current = parent.FullName;
        }

        return Directory.GetCurrentDirectory();
    }

    static string ResolvePath(string repoRoot, string value)
    {
        return Path.IsPathRooted(value) ? value : Path.GetFullPath(Path.Combine(repoRoot, value));
    }
}
