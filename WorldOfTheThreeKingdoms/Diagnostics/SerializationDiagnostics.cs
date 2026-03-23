using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameObjects;
using GameManager;

namespace WorldOfTheThreeKingdoms.Diagnostics
{
    /// <summary>
    /// 序列化诊断工具类
    /// 用于检测和修复随机序列化异常问题
    /// </summary>
    public static class SerializationDiagnostics
    {
        /// <summary>
        /// 检查所有Architecture的ArchitectureArea是否存在共享引用问题
        /// </summary>
        public static void CheckSharedReferences()
        {
            System.Diagnostics.Debug.WriteLine("[共享引用检查] 开始检查所有Architecture的ArchitectureArea...");
            
            var areaInstances = new Dictionary<int, List<string>>();
            var areaDetails = new Dictionary<int, (int count, string firstAreaString)>();
            
            foreach (Architecture arch in Session.Current.Scenario.Architectures.GetList())
            {
                if (arch.ArchitectureArea != null)
                {
                    var id = arch.ArchitectureArea.InstanceId;
                    var count = arch.ArchitectureArea.Area.Count;
                    
                    if (!areaInstances.ContainsKey(id))
                    {
                        areaInstances[id] = new List<string>();
                        areaDetails[id] = (count, arch.ArchitectureAreaString);
                    }
                    areaInstances[id].Add(arch.Name);
                }
            }
            
            // 检查共享引用
            int sharedCount = 0;
            foreach (var kvp in areaInstances)
            {
                if (kvp.Value.Count > 1)
                {
                    sharedCount++;
                    var detail = areaDetails[kvp.Key];
                    System.Diagnostics.Debug.WriteLine($"[共享引用警告] GameArea ID:{kvp.Key} ({detail.count}坐标) 被以下{kvp.Value.Count}个城池共享:");
                    System.Diagnostics.Debug.WriteLine($"[共享引用警告] 城池: {string.Join(", ", kvp.Value)}");
                    System.Diagnostics.Debug.WriteLine($"[共享引用警告] AreaString: '{detail.firstAreaString}'");
                }
            }
            
            // 检查数据一致性
            int inconsistentCount = 0;
            foreach (Architecture arch in Session.Current.Scenario.Architectures.GetList())
            {
                if (!string.IsNullOrEmpty(arch.ArchitectureAreaString) && arch.ArchitectureArea != null)
                {
                    var expectedCount = arch.ArchitectureAreaString.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2;
                    var actualCount = arch.ArchitectureArea.Area.Count;
                    
                    if (actualCount != expectedCount)
                    {
                        inconsistentCount++;
                        System.Diagnostics.Debug.WriteLine($"[数据不一致] {arch.Name} 期望{expectedCount}个坐标，实际{actualCount}个");
                        System.Diagnostics.Debug.WriteLine($"[数据不一致] {arch.Name} AreaString: '{arch.ArchitectureAreaString}'");
                        System.Diagnostics.Debug.WriteLine($"[数据不一致] {arch.Name} Area实际坐标: {string.Join(", ", arch.ArchitectureArea.Area)}");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[共享引用检查] 完成。发现{sharedCount}个共享引用，{inconsistentCount}个数据不一致的城池");
        }
        
        /// <summary>
        /// 验证并修复所有序列化数据
        /// </summary>
        public static void ValidateAndFixAllSerializedData()
        {
            System.Diagnostics.Debug.WriteLine("[序列化验证] 开始验证所有序列化数据...");
            
            int fixedCount = 0;
            foreach (Architecture arch in Session.Current.Scenario.Architectures.GetList())
            {
                try
                {
                    // 验证并修复ArchitectureArea
                    if (!string.IsNullOrEmpty(arch.ArchitectureAreaString))
                    {
                        var expectedCount = arch.ArchitectureAreaString.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2;
                        var actualCount = arch.ArchitectureArea?.Area?.Count ?? 0;
                        
                        if (actualCount != expectedCount)
                        {
                            System.Diagnostics.Debug.WriteLine($"[序列化验证] 修复 {arch.Name} 的ArchitectureArea (期望{expectedCount}，实际{actualCount})");
                            
                            // 强制重新创建独立实例
                            var newArea = new GameArea();
                            arch.LoadFromString(newArea, arch.ArchitectureAreaString);
                            arch.ArchitectureArea = newArea;
                            
                            fixedCount++;
                        }
                    }
                    
                    // TODO: 验证其他集合（PersonsString, MilitariesString等）
                    // 这里可以扩展验证其他可能受影响的序列化数据
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[序列化验证] {arch.Name} 修复失败: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[序列化验证] 完成。修复了{fixedCount}个城池的数据");
        }
        
        /// <summary>
        /// 修复所有Architecture的ArchitectureArea，确保独立性
        /// </summary>
        public static void FixAllArchitectureAreas()
        {
            System.Diagnostics.Debug.WriteLine("[全局修复] 开始修复所有Architecture的ArchitectureArea...");
            
            int fixedCount = 0;
            foreach (Architecture arch in Session.Current.Scenario.Architectures.GetList())
            {
                try
                {
                    arch.EnsureUniqueAreaInstance();
                    fixedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[全局修复] {arch.Name} 修复失败: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[全局修复] 完成。处理了{fixedCount}个城池");
        }
        
        /// <summary>
        /// 输出所有Architecture的ArchitectureArea状态报告
        /// </summary>
        public static void GenerateAreaStatusReport()
        {
            System.Diagnostics.Debug.WriteLine("[状态报告] 生成ArchitectureArea状态报告...");
            
            var report = new Dictionary<string, int>();
            
            foreach (Architecture arch in Session.Current.Scenario.Architectures.GetList())
            {
                if (!string.IsNullOrEmpty(arch.ArchitectureAreaString) && arch.ArchitectureArea != null)
                {
                    var expectedCount = arch.ArchitectureAreaString.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2;
                    var actualCount = arch.ArchitectureArea.Area.Count;
                    
                    var status = actualCount == expectedCount ? "正常" : $"异常(期望{expectedCount}实际{actualCount})";
                    
                    if (!report.ContainsKey(status))
                        report[status] = 0;
                    report[status]++;
                    
                    if (actualCount != expectedCount)
                    {
                        System.Diagnostics.Debug.WriteLine($"[状态报告] {arch.Name}: {status}");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine("[状态报告] 汇总:");
            foreach (var kvp in report)
            {
                System.Diagnostics.Debug.WriteLine($"[状态报告] {kvp.Key}: {kvp.Value}个城池");
            }
        }
    }
}