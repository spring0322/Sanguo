using System;
using System.Linq;
using GameObjects;

namespace WorldOfTheThreeKingdoms.Diagnostics
{
    /// <summary>
    /// Architecture规模验证工具
    /// 确保城市规模与ArchitectureAreaString严格对应
    /// 核心原则：规模 = ArchitectureAreaString实际坐标数 ÷ 2
    /// </summary>
    public static class ArchitectureScaleValidator
    {
        /// <summary>
        /// 验证所有城市的规模一致性
        /// </summary>
        public static void ValidateAllArchitectures(GameScenario scenario)
        {
            if (scenario?.Architectures == null)
            {
                Console.WriteLine("❌ 场景或建筑列表为null");
                return;
            }

            Console.WriteLine("🔍 开始验证城市规模一致性...");
            Console.WriteLine("核心原则：规模 = ArchitectureAreaString实际坐标数 ÷ 2");
            Console.WriteLine();
            
            int totalCount = 0;
            int inconsistentCount = 0;
            int emptyStringCount = 0;
            var scaleDistribution = new System.Collections.Generic.Dictionary<int, int>();
            var problemCities = new System.Collections.Generic.List<string>();

            foreach (Architecture arch in scenario.Architectures.GetList())
            {
                if (arch == null) continue;
                
                totalCount++;

                if (string.IsNullOrEmpty(arch.ArchitectureAreaString))
                {
                    emptyStringCount++;
                    Console.WriteLine($"⚠️ {arch.Name}: ArchitectureAreaString为空");
                    continue;
                }

                // 🔥 关键：计算期望规模（基于ArchitectureAreaString实际内容）
                var coords = arch.ArchitectureAreaString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int expectedScale = coords.Length / 2;

                // 检查实际规模（基于GameArea）
                int actualScale = arch.ArchitectureArea?.Area?.Count ?? 0;

                // 统计规模分布
                if (!scaleDistribution.ContainsKey(expectedScale))
                    scaleDistribution[expectedScale] = 0;
                scaleDistribution[expectedScale]++;

                // 🔥 关键检查：一致性验证
                if (actualScale != expectedScale)
                {
                    inconsistentCount++;
                    string problem = $"{arch.Name}: 期望{expectedScale}坐标, 实际{actualScale}坐标";
                    problemCities.Add(problem);
                    
                    Console.WriteLine($"❌ {problem}");
                    Console.WriteLine($"   ArchitectureAreaString: \"{arch.ArchitectureAreaString}\"");
                    Console.WriteLine($"   坐标数组长度: {coords.Length}");
                    
                    // 🔥 尝试自动修复
                    try
                    {
                        Console.WriteLine("   🔧 尝试自动修复...");
                        arch.ArchitectureArea = new GameArea();
                        arch.LoadFromString(arch.ArchitectureArea, arch.ArchitectureAreaString);
                        int fixedScale = arch.ArchitectureArea.Area.Count;
                        
                        if (fixedScale == expectedScale)
                        {
                            Console.WriteLine($"   ✅ 修复成功！现在规模为{fixedScale}");
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ 修复失败，仍为规模{fixedScale}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ 修复时发生异常: {ex.Message}");
                    }
                    
                    Console.WriteLine();
                }
            }

            // 输出统计结果
            Console.WriteLine("📊 验证统计结果:");
            Console.WriteLine($"总城市数: {totalCount}");
            Console.WriteLine($"空字符串: {emptyStringCount}");
            Console.WriteLine($"规模不一致: {inconsistentCount}");
            Console.WriteLine();
            
            Console.WriteLine("规模分布统计:");
            foreach (var kvp in scaleDistribution.OrderBy(x => x.Key))
            {
                Console.WriteLine($"  规模{kvp.Key}: {kvp.Value}个城市");
            }
            Console.WriteLine();

            if (inconsistentCount == 0)
            {
                Console.WriteLine("✅ 所有城市规模一致！");
                Console.WriteLine("✅ ArchitectureAreaString与GameArea.Area.Count完全匹配");
            }
            else
            {
                Console.WriteLine($"⚠️ 发现{inconsistentCount}个规模不一致的城市:");
                foreach (var problem in problemCities)
                {
                    Console.WriteLine($"  - {problem}");
                }
                Console.WriteLine();
                Console.WriteLine("🔧 建议检查以下方面:");
                Console.WriteLine("1. Architecture.ArchitectureArea getter是否正确实现");
                Console.WriteLine("2. SimpleSerializer.FixSharedReferences是否正确处理");
                Console.WriteLine("3. 序列化/反序列化过程是否基于实际数据");
            }
        }

        /// <summary>
        /// 验证单个城市的规模
        /// </summary>
        public static bool ValidateSingleArchitecture(Architecture arch)
        {
            if (arch == null || string.IsNullOrEmpty(arch.ArchitectureAreaString))
                return false;

            var coords = arch.ArchitectureAreaString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int expectedScale = coords.Length / 2;
            int actualScale = arch.ArchitectureArea?.Area?.Count ?? 0;

            return actualScale == expectedScale;
        }

        /// <summary>
        /// 强制修复单个城市的规模
        /// </summary>
        public static bool FixSingleArchitecture(Architecture arch)
        {
            if (arch == null || string.IsNullOrEmpty(arch.ArchitectureAreaString))
                return false;

            try
            {
                // 🔥 强制重建GameArea，确保基于ArchitectureAreaString
                arch.ArchitectureArea = new GameArea();
                arch.LoadFromString(arch.ArchitectureArea, arch.ArchitectureAreaString);
                return ValidateSingleArchitecture(arch);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 分析JSON数据中的规模分布
        /// </summary>
        public static void AnalyzeJsonScaleDistribution(string jsonFilePath)
        {
            Console.WriteLine($"📊 分析JSON文件中的规模分布: {jsonFilePath}");
            
            if (!System.IO.File.Exists(jsonFilePath))
            {
                Console.WriteLine("❌ 文件不存在");
                return;
            }

            try
            {
                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
                var scaleStats = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>();
                
                // 简单的字符串匹配分析
                var lines = jsonContent.Split('\n');
                string currentArchName = "";
                
                foreach (var line in lines)
                {
                    if (line.Contains("\"Name\":"))
                    {
                        var nameMatch = System.Text.RegularExpressions.Regex.Match(line, "\"Name\":\\s*\"([^\"]+)\"");
                        if (nameMatch.Success)
                        {
                            currentArchName = nameMatch.Groups[1].Value;
                        }
                    }
                    else if (line.Contains("\"ArchitectureAreaString\":") && !string.IsNullOrEmpty(currentArchName))
                    {
                        var areaMatch = System.Text.RegularExpressions.Regex.Match(line, "\"ArchitectureAreaString\":\\s*\"([^\"]+)\"");
                        if (areaMatch.Success)
                        {
                            string areaString = areaMatch.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(areaString))
                            {
                                var coords = areaString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                int scale = coords.Length / 2;
                                
                                if (!scaleStats.ContainsKey(scale))
                                    scaleStats[scale] = new System.Collections.Generic.List<string>();
                                
                                scaleStats[scale].Add($"{currentArchName}({coords.Length}坐标)");
                            }
                        }
                        currentArchName = ""; // 重置
                    }
                }
                
                Console.WriteLine("JSON中的规模分布:");
                foreach (var kvp in scaleStats.OrderBy(x => x.Key))
                {
                    Console.WriteLine($"  规模{kvp.Key}: {kvp.Value.Count}个城市");
                    if (kvp.Value.Count <= 5)
                    {
                        foreach (var example in kvp.Value)
                        {
                            Console.WriteLine($"    - {example}");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Console.WriteLine($"    - {kvp.Value[i]}");
                        }
                        Console.WriteLine($"    - ... 还有{kvp.Value.Count - 3}个");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 分析失败: {ex.Message}");
            }
        }
    }
}