using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace DiagnoseStartingArchitecture
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("诊断存档中的 StartingArchitecture 数据");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 查找最新的存档文件
            string saveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "WorldOfTheThreeKingdoms", "Save"
            );

            if (!Directory.Exists(saveDir))
            {
                Console.WriteLine($"❌ 存档目录不存在: {saveDir}");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            var saveFiles = Directory.GetFiles(saveDir, "*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (saveFiles.Count == 0)
            {
                Console.WriteLine("❌ 未找到存档文件");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"找到 {saveFiles.Count} 个存档文件");
            Console.WriteLine($"检查最新存档: {Path.GetFileName(saveFiles[0])}");
            Console.WriteLine();

            try
            {
                string json = File.ReadAllText(saveFiles[0]);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // 检查 Troops 数组
                if (!root.TryGetProperty("Troops", out JsonElement troopsElement))
                {
                    Console.WriteLine("❌ 存档中没有 Troops 数据");
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey();
                    return;
                }

                int totalTroops = troopsElement.GetArrayLength();
                int withStartingArch = 0;
                int withoutStartingArch = 0;
                int invalidStartingArch = 0;

                Console.WriteLine($"📊 部队总数: {totalTroops}");
                Console.WriteLine();
                Console.WriteLine("检查每个部队的 StartingArchitectureID...");
                Console.WriteLine();

                foreach (JsonElement troop in troopsElement.EnumerateArray())
                {
                    int troopId = troop.GetProperty("ID").GetInt32();
                    string troopName = troop.TryGetProperty("Name", out var nameEl) ? nameEl.GetString() : "未命名";

                    if (troop.TryGetProperty("StartingArchitectureID", out JsonElement startArchEl))
                    {
                        int startArchId = startArchEl.GetInt32();
                        if (startArchId > 0)
                        {
                            withStartingArch++;
                            Console.WriteLine($"  ✅ 部队 {troopId} ({troopName}): StartingArchitectureID = {startArchId}");
                        }
                        else if (startArchId == -1)
                        {
                            withoutStartingArch++;
                            Console.WriteLine($"  ⚠️  部队 {troopId} ({troopName}): StartingArchitectureID = -1 (未设置)");
                        }
                        else
                        {
                            invalidStartingArch++;
                            Console.WriteLine($"  ❌ 部队 {troopId} ({troopName}): StartingArchitectureID = {startArchId} (无效)");
                        }
                    }
                    else
                    {
                        withoutStartingArch++;
                        Console.WriteLine($"  ❌ 部队 {troopId} ({troopName}): 缺少 StartingArchitectureID 字段");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("统计结果：");
                Console.WriteLine($"  有效的 StartingArchitectureID: {withStartingArch}");
                Console.WriteLine($"  未设置或缺失: {withoutStartingArch}");
                Console.WriteLine($"  无效值: {invalidStartingArch}");
                Console.WriteLine();

                if (withoutStartingArch > 0 || invalidStartingArch > 0)
                {
                    Console.WriteLine("⚠️  警告：部分部队缺少 StartingArchitecture 数据");
                    Console.WriteLine("   这可能是旧存档，或者序列化未正确实现");
                    Console.WriteLine("   建议：");
                    Console.WriteLine("   1. 确认 TroopDTO 包含 StartingArchitectureID 字段");
                    Console.WriteLine("   2. 确认 SaveDataPhase 正确保存该字段");
                    Console.WriteLine("   3. 重新保存游戏生成新存档");
                }
                else
                {
                    Console.WriteLine("✅ 所有部队都有有效的 StartingArchitecture 数据");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 解析存档失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
