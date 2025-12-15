// 🎯 回合管理器 - 处理回合开始时的军师风险扫描
// 在每回合开始时自动检查军师建议和风险评估

using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 🎯 回合管理器 - 处理回合开始时的各种逻辑
    /// </summary>
    public class TurnManager
    {
        /// <summary>
        /// 回合开始时的处理逻辑
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        public void OnTurnStart(Faction playerFaction)
        {
            if (playerFaction == null || playerFaction.Destroyed)
                return;
            
            try
            {
                // 1. 处理资源增长...
                ProcessResourceGrowth(playerFaction);
                
                // 2. 让军师扫描风险
                // 如果有危险，这里内部会触发 Event，导致 UI 弹窗
                var strategist = GetStrategist(playerFaction);
                if (strategist != null)
                {
                    System.Diagnostics.Debug.WriteLine($"回合开始 - 军师 {strategist.Name} 开始风险扫描");
                    StrategistManager.CheckCriticalRisks(playerFaction);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("回合开始 - 无军师，跳过风险扫描");
                }
                
                // 3. 其他回合开始逻辑...
                ProcessOtherTurnStartLogic(playerFaction);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"回合开始处理异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理资源增长
        /// </summary>
        /// <param name="faction">势力</param>
        private void ProcessResourceGrowth(Faction faction)
        {
            try
            {
                // 这里可以添加资源增长逻辑
                // 例如：城市收入、人口增长、技术研发等
                System.Diagnostics.Debug.WriteLine($"处理 {faction.Name} 的资源增长");
                
                // 示例：简单的资源增长
                foreach (Architecture arch in faction.Architectures.GetList())
                {
                    // 基础收入增长
                    int income = arch.Population / 1000; // 简化计算
                    arch.Fund += income;
                    
                    // 基础粮食增长
                    int foodGrowth = arch.Agriculture / 10; // 简化计算
                    arch.Food += foodGrowth;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"资源增长处理异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取势力的军师
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>军师武将，如果没有则返回null</returns>
        private Person GetStrategist(Faction faction)
        {
            try
            {
                // 查找智力最高的武将作为军师
                Person strategist = null;
                int maxIntelligence = 0;
                
                foreach (Person person in faction.Persons.GetList())
                {
                    if (person != null && person.Intelligence > maxIntelligence)
                    {
                        maxIntelligence = person.Intelligence;
                        strategist = person;
                    }
                }
                
                // 只有智力超过80的才能当军师
                return (strategist?.Intelligence >= 80) ? strategist : null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 处理其他回合开始逻辑
        /// </summary>
        /// <param name="faction">势力</param>
        private void ProcessOtherTurnStartLogic(Faction faction)
        {
            try
            {
                // 这里可以添加其他回合开始时需要处理的逻辑
                // 例如：外交关系变化、随机事件触发、AI行为更新等
                System.Diagnostics.Debug.WriteLine($"处理 {faction.Name} 的其他回合逻辑");
                
                // 示例：更新AI记忆（如果有的话）
                if (faction.MemoryMap != null)
                {
                    int currentDay = Session.Current.Scenario.Date.Day;
                    faction.MemoryMap.CleanExpiredMemories(currentDay);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"其他回合逻辑处理异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 回合结束时的处理逻辑
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        public void OnTurnEnd(Faction playerFaction)
        {
            if (playerFaction == null || playerFaction.Destroyed)
                return;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"回合结束 - {playerFaction.Name}");
                
                // 这里可以添加回合结束时的逻辑
                // 例如：保存游戏状态、统计数据更新等
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"回合结束处理异常: {ex.Message}");
            }
        }
    }
}