using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using GameObjects;
using Tools;
using WorldOfTheThreeKingdoms.Tools;

namespace GameManager
{
    public static class GameLoader
    {
        /// <summary>
        /// 加载游戏场景，使用 SimpleSerializer 进行 System.Text.Json 兼容的反序列化
        /// </summary>
        /// <param name="path">场景文件路径</param>
        /// <returns>游戏场景对象</returns>
        public static GameScenario LoadScenario(string path)
        {
            // Fix for DirectoryNotFoundException / FileNotFoundException
            if (string.IsNullOrEmpty(path)) return null;

            System.Diagnostics.Debug.WriteLine($"[GameLoader] 开始加载场景: {path}");

            // 🔥 修复：区分剧本和存档路径
            bool isUserFile = path.Contains("Save") || path.Contains("GameData");
            System.Diagnostics.Debug.WriteLine($"[GameLoader] 文件类型: {(isUserFile ? "存档" : "剧本")}");

            // 🔥 首先尝试加载二进制版本 (在检查JSON文件之前!)
            // 🔥 首先尝试加载二进制版本 (优先检查标准 .bin, 其次检查旧版 Binary_ 前缀)
            try
            {
                string binaryPath = null;

                // 1. 直接匹配 (如果输入路径已经是 .bin)
                if (path.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                {
                    binaryPath = path;
                }
                // 2. 标准化匹配 (Save01.json -> Save01.bin)
                else
                {
                    string standardBinaryPath = Path.ChangeExtension(path, ".bin");
                    if (File.Exists(standardBinaryPath))
                    {
                        binaryPath = standardBinaryPath;
                    }
                    else
                    {
                        // 尝试在 Save 目录下查找
                        string saveDirBinary = Path.Combine("Save", Path.GetFileName(standardBinaryPath));
                        if (File.Exists(saveDirBinary))
                        {
                            binaryPath = saveDirBinary;
                        }
                        // 3. 旧版兼容匹配 (Save01.json -> Binary_Save01.bin)
                        else
                        {
                            string legacyBinaryName = "Binary_" + Path.GetFileNameWithoutExtension(path) + ".bin";
                            string legacyPath = Path.Combine(Path.GetDirectoryName(path) ?? "", legacyBinaryName);
                            
                            if (File.Exists(legacyPath))
                            {
                                binaryPath = legacyPath;
                            }
                            else
                            {
                                // 尝试在 Save 目录下查找旧版
                                string legacySaveDir = Path.Combine("Save", legacyBinaryName);
                                if (File.Exists(legacySaveDir))
                                {
                                    binaryPath = legacySaveDir;
                                }
                            }
                        }
                    }
                }

                // Binary serialization removed - using JSON only
                System.Diagnostics.Debug.WriteLine($"[GameLoader] Binary format no longer supported - using JSON only");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GameLoader] Load preparation failed: {ex.Message}");
            }

            // 🔥 然后才检查JSON文件存在性
            var dir2 = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir2) && !Directory.Exists(dir2))
            {
                Directory.CreateDirectory(dir2);
            }

            if (!File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"[GameLoader] JSON文件不存在: {path}");
                return null;
            }

            try
            {
                // 🔥 修复：使用新的 SerializationManager 加载 .sav.gz 格式
                System.Diagnostics.Debug.WriteLine($"[GameLoader] 加载文件类型: {(isUserFile ? "存档" : "剧本")}");
                
                GameScenario scenario = null;
                
                // 如果是 .sav.gz 格式，使用 SerializationManager
                if (path.EndsWith(".sav.gz", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"[GameLoader] 使用 SerializationManager 加载: {path}");
                    var serializationManager = new WorldOfTheThreeKingdoms.Serialization.SerializationManager();
                    scenario = serializationManager.LoadGame(path);
                }
                else
                {
                    // 旧格式或剧本文件，使用 SimpleSerializer
                    System.Diagnostics.Debug.WriteLine($"[GameLoader] 使用 SimpleSerializer 加载: {path}");
                    scenario = SimpleSerializer.DeserializeJsonFile<GameScenario>(path, isUserFile, false, false);
                }
                
                if (scenario == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameLoader] 场景加载失败: {path}");
                    throw new InvalidOperationException($"场景加载返回 null: {path}");
                }

                // 🔥 关键修复：新游戏路径必须调用 ProcessScenarioData
                // 日期：2026-03-16
                // 原因：SimpleSerializer 只反序列化 JSON，不处理数据（不调用 LoadPersonsFromString 等）
                //       导致 Architecture.Persons 为空、BelongedFaction 为 null
                // 解决：在返回之前调用 ProcessScenarioData，处理所有数据
                bool fromScenario = !isUserFile;  // 剧本文件 = true，存档文件 = false
                System.Diagnostics.Debug.WriteLine($"[GameLoader] 🔥 准备调用 ProcessScenarioData，fromScenario={fromScenario}");
                
                var errors = scenario.ProcessScenarioData(fromScenario, editing: false);
                
                if (errors != null && errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameLoader] ⚠️ ProcessScenarioData 返回了 {errors.Count} 个错误:");
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {error}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[GameLoader] ✅ ProcessScenarioData 完成，无错误");
                }

                System.Diagnostics.Debug.WriteLine($"[GameLoader] 场景加载成功: {path}");
                return scenario;
            }
            catch (Exception ex)
            {
                // 🔥 修复：不要吞掉异常，让调用者知道具体的错误
                // 日期：2026-02-10
                System.Diagnostics.Debug.WriteLine($"[GameLoader] 场景加载异常:\n{ex.ToString()}");
                
                // 重新抛出异常，让上层处理
                throw;
            }
        }
    }
}