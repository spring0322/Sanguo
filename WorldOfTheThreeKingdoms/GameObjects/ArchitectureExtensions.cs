using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameObjects
{
    /// <summary>
    /// Architecture扩展方法 - 技术修复，保持兼容性
    /// </summary>
    public static class ArchitectureExtensions
    {
        /// <summary>
        /// 获取领导者的宝物列表
        /// 🔥 诊断模式：记录详细信息但不掩盖数据错误
        /// </summary>
        public static GameObjectList GetTreasureListOfLeader(this Architecture architecture)
        {
            System.Diagnostics.Debug.WriteLine($"[GetTreasureListOfLeader] 开始获取宝物列表");
            System.Diagnostics.Debug.WriteLine($"[GetTreasureListOfLeader] architecture={architecture?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[GetTreasureListOfLeader] BelongedFaction={architecture?.BelongedFaction?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[GetTreasureListOfLeader] Leader={architecture?.BelongedFaction?.Leader?.Name ?? "null"}");
            
            // 🔥 如果数据链条中有null，让它自然崩溃，暴露数据源问题
            // 不做防御性检查，遵循 Anti-Band-Aid Protocol
            int treasureCount = architecture.BelongedFaction.Leader.Treasures.Count;
            System.Diagnostics.Debug.WriteLine($"[GetTreasureListOfLeader] 领导者 {architecture.BelongedFaction.Leader.Name} 拥有 {treasureCount} 个宝物");
            
            var list = architecture.BelongedFaction.Leader.Treasures.GetList();
            System.Diagnostics.Debug.WriteLine($"[GetTreasureListOfLeader] 返回列表包含 {list.Count} 个宝物");
            
            return list;
        }


        /// <summary>
        /// 获取建筑中的所有宝物
        /// </summary>
        public static GameObjectList GetAllTreasureInArchitecture(this Architecture architecture)
        {
            var treasures = new GameObjectList();
            
            try
            {
                if (architecture?.Persons != null)
                {
                    // 收集建筑中所有人员的宝物
                    foreach (Person person in architecture.Persons)
                    {
                        if (person?.Treasures != null)
                        {
                            treasures.AddRange(person.Treasures.GetList());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ArchitectureExtensions] GetAllTreasureInArchitecture失败: {ex.Message}");
            }
            
            return treasures;
        }

        /// <summary>
        /// 获取势力的所有宝物
        /// </summary>
        public static GameObjectList GetAllTreasureInFaction(this Architecture architecture)
        {
            var treasures = new GameObjectList();
            
            try
            {
                if (architecture?.BelongedFaction != null)
                {
                    // 收集势力中所有建筑的宝物
                    foreach (Architecture arch in architecture.BelongedFaction.Architectures)
                    {
                        if (arch?.Persons != null)
                        {
                            foreach (Person person in arch.Persons)
                            {
                                if (person?.Treasures != null)
                                {
                                    treasures.AddRange(person.Treasures.GetList());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ArchitectureExtensions] GetAllTreasureInFaction失败: {ex.Message}");
            }
            
            return treasures;
        }

        /// <summary>
        /// 检查是否有宝物可以奖励
        /// </summary>
        public static bool HasTreasureToAward(this Architecture architecture)
        {
            // 🔥 业务逻辑：中立建筑没有势力，返回false是正确的
            if (architecture.BelongedFaction == null) 
                return false;
            
            if (architecture.BelongedFaction.Leader == null) 
                return false;
            
            // 🔥 如果Treasures为null，让它崩溃，暴露数据初始化问题
            return architecture.BelongedFaction.Leader.Treasures.Count > 0;
        }

        /// <summary>
        /// 检查是否有宝物可以没收（下属拥有的宝物）
        /// </summary>
        public static bool HasTreasureToConfiscate(this Architecture architecture)
        {
            // 🔥 业务逻辑：中立建筑没有势力，返回false是正确的
            if (architecture.BelongedFaction == null) 
                return false;
            
            // 🔥 如果AllTreasuresExceptLeader为null，让它崩溃，暴露数据问题
            return architecture.BelongedFaction.AllTreasuresExceptLeader.Count > 0;
        }

        /// <summary>
        /// 检查是否有宝物可以出售（君主拥有的宝物）
        /// </summary>
        public static bool HasTreasureToSell(this Architecture architecture)
        {
            // 与HasTreasureToAward相同逻辑
            return architecture.HasTreasureToAward();
        }

        /// <summary>
        /// 检查是否为前线
        /// </summary>
        public static bool IsFrontline(this Architecture architecture)
        {
            try
            {
                if (architecture?.BelongedFaction != null && architecture.AIAllLinkNodes != null)
                {
                    // 简单实现：检查是否有敌对势力在附近
                    foreach (var link in architecture.AIAllLinkNodes.Values)
                    {
                        if (link?.A?.BelongedFaction != null && 
                            link.A.BelongedFaction != architecture.BelongedFaction)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ArchitectureExtensions] IsFrontline失败: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// 获取最差的战斗官员
        /// </summary>
        public static Person GetWorstCombatOfficer(this Architecture architecture)
        {
            try
            {
                if (architecture?.Persons != null && architecture.Persons.Count > 0)
                {
                    // 简单实现：返回战斗力最低的人员
                    Person worstPerson = null;
                    int lowestCombat = int.MaxValue;
                    
                    foreach (Person person in architecture.Persons)
                    {
                        int combat = person.Command + person.Strength;
                        if (combat < lowestCombat)
                        {
                            lowestCombat = combat;
                            worstPerson = person;
                        }
                    }
                    
                    return worstPerson;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ArchitectureExtensions] GetWorstCombatOfficer失败: {ex.Message}");
            }
            
            return null;
        }
    }
}