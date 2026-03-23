using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace DiagnosePortraitAndBiography
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("头像和列传显示问题诊断工具");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 检查剧本文件
            string scenarioPath = "Content/Data/Scenario/";
            if (!Directory.Exists(scenarioPath))
            {
                Console.WriteLine("❌ 剧本目录不存在: " + scenarioPath);
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            var scenarioFiles = Directory.GetFiles(scenarioPath, "*.json", SearchOption.AllDirectories);
            if (scenarioFiles.Length == 0)
            {
                Console.WriteLine("❌ 未找到剧本文件");
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"✓ 找到 {scenarioFiles.Length} 个剧本文件");
            Console.WriteLine();

            // 分析第一个剧本
            string firstScenario = scenarioFiles[0];
            Console.WriteLine($"分析剧本: {Path.GetFileName(firstScenario)}");
            Console.WriteLine();

            try
            {
                string jsonContent = File.ReadAllText(firstScenario);
                
                // 使用JsonDocument进行简单解析
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    var root = doc.RootElement;

                    // 检查Persons数组
                    if (root.TryGetProperty("Persons", out JsonElement personsElement))
                    {
                        int personCount = personsElement.GetArrayLength();
                        Console.WriteLine($"✓ 武将数量: {personCount}");

                        // 检查前10个武将的PictureIndex
                        var pictureIndexes = new HashSet<int>();
                        var biographyIds = new HashSet<int>();
                        int checkedCount = Math.Min(10, personCount);

                        Console.WriteLine();
                        Console.WriteLine($"检查前 {checkedCount} 个武将:");
                        Console.WriteLine("----------------------------------------");

                        for (int i = 0; i < checkedCount; i++)
                        {
                            var person = personsElement[i];
                            
                            string name = person.TryGetProperty("Name", out var nameEl) ? nameEl.GetString() : "未知";
                            int pictureIndex = person.TryGetProperty("PictureIndex", out var picEl) ? picEl.GetInt32() : -1;
                            int biographyId = person.TryGetProperty("PersonBiographyID", out var bioEl) ? bioEl.GetInt32() : -1;

                            pictureIndexes.Add(pictureIndex);
                            if (biographyId >= 0)
                            {
                                biographyIds.Add(biographyId);
                            }

                            Console.WriteLine($"{i + 1}. {name}");
                            Console.WriteLine($"   PictureIndex: {pictureIndex}");
                            Console.WriteLine($"   PersonBiographyID: {biographyId}");
                        }

                        Console.WriteLine();
                        Console.WriteLine("统计结果:");
                        Console.WriteLine($"  不同的头像索引数量: {pictureIndexes.Count}");
                        Console.WriteLine($"  不同的列传ID数量: {biographyIds.Count}");

                        if (pictureIndexes.Count == 1)
                        {
                            Console.WriteLine();
                            Console.WriteLine("⚠️ 警告：所有武将的PictureIndex都相同！");
                            Console.WriteLine("   这可能是剧本数据问题，请检查剧本文件。");
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("✓ 武将头像索引正常（各不相同）");
                        }

                        if (biographyIds.Count == 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("⚠️ 警告：没有武将有PersonBiographyID！");
                            Console.WriteLine("   这可能是旧版本剧本，需要重新生成。");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ 剧本中没有Persons数组");
                    }

                    Console.WriteLine();
                    Console.WriteLine("----------------------------------------");

                    // 检查AllBiographies
                    if (root.TryGetProperty("AllBiographies", out JsonElement biographiesElement))
                    {
                        if (biographiesElement.TryGetProperty("Biographys", out JsonElement biographysArray))
                        {
                            int bioCount = biographysArray.GetArrayLength();
                            Console.WriteLine($"✓ 列传数量: {bioCount}");

                            if (bioCount > 0)
                            {
                                // 检查第一个列传
                                var firstBio = biographysArray[0];
                                int bioId = firstBio.TryGetProperty("ID", out var idEl) ? idEl.GetInt32() : -1;
                                string brief = firstBio.TryGetProperty("Brief", out var briefEl) ? briefEl.GetString() : "";
                                string history = firstBio.TryGetProperty("History", out var histEl) ? histEl.GetString() : "";
                                string romance = firstBio.TryGetProperty("Romance", out var romEl) ? romEl.GetString() : "";
                                string inGame = firstBio.TryGetProperty("InGame", out var gameEl) ? gameEl.GetString() : "";

                                Console.WriteLine();
                                Console.WriteLine($"示例列传 (ID: {bioId}):");
                                Console.WriteLine($"  Brief长度: {brief?.Length ?? 0}");
                                Console.WriteLine($"  History长度: {history?.Length ?? 0}");
                                Console.WriteLine($"  Romance长度: {romance?.Length ?? 0}");
                                Console.WriteLine($"  InGame长度: {inGame?.Length ?? 0}");

                                if (string.IsNullOrEmpty(brief) && string.IsNullOrEmpty(history) && 
                                    string.IsNullOrEmpty(romance) && string.IsNullOrEmpty(inGame))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("⚠️ 警告：列传内容为空！");
                                }
                                else
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("✓ 列传内容正常");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ AllBiographies中没有Biographys数组");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ 剧本中没有AllBiographies");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 解析剧本文件失败: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("诊断完成");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("如果发现问题：");
            Console.WriteLine("1. PictureIndex都相同 → 剧本数据问题，需要重新生成剧本");
            Console.WriteLine("2. 没有PersonBiographyID → 旧版本剧本，需要更新");
            Console.WriteLine("3. 列传内容为空 → 需要添加列传数据");
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
