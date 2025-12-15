using System;
using System.Linq;
using GameGlobal;
using GameManager;

namespace GameObjects
{
    /// <summary>
    /// 势力联盟扩展方法
    /// 为Faction类添加联盟相关的AI行为
    /// </summary>
    public static class FactionCoalitionExtensions
    {
        /// <summary>
        /// 进入联盟模式
        /// </summary>
        /// <param name="faction">当前势力</param>
        /// <param name="leader">盟主势力</param>
        public static void EnterCoalitionMode(this Faction faction, Faction leader)
        {
            try
            {
                if (faction == null || leader == null) return;

                // 设置联盟状态标记
                faction.SetCoalitionMode(true, leader);
                
                // 调整AI行为参数
                faction.SetCoalitionAIParameters();
                
                System.Diagnostics.Debug.WriteLine($"[联盟AI] {faction.Name} 进入联盟模式，盟主: {leader.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟AI] EnterCoalitionMode 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 退出联盟模式
        /// </summary>
        /// <param name="faction">当前势力</param>
        public static void ExitCoalitionMode(this Faction faction)
        {
            try
            {
                if (faction == null) return;

                // 清除联盟状态标记
                faction.SetCoalitionMode(false, null);
                
                // 恢复正常AI行为参数
                faction.RestoreNormalAIParameters();
                
                System.Diagnostics.Debug.WriteLine($"[联盟AI] {faction.Name} 退出联盟模式");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟AI] ExitCoalitionMode 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置联盟模式状态
        /// </summary>
        private static void SetCoalitionMode(this Faction faction, bool isInCoalition, Faction leader)
        {
            // 这里需要在Faction类中添加相应的字段
            // 由于无法直接修改Faction类，我们使用扩展属性的方式
            
            // 可以考虑使用静态字典来存储联盟状态
            CoalitionStateManager.SetCoalitionState(faction, isInCoalition, leader);
        }

        /// <summary>
        /// 设置联盟AI参数
        /// </summary>
        private static void SetCoalitionAIParameters(this Faction faction)
        {
            try
            {
                // 提高对玩家的敌对度
                // faction.PlayerHostility = Math.Min(100, faction.PlayerHostility + 30);
                
                // 降低对其他联盟成员的敌对度
                // 这需要在AI决策中实现
                
                System.Diagnostics.Debug.WriteLine($"[联盟AI] {faction.Name} 联盟AI参数设置完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟AI] SetCoalitionAIParameters 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复正常AI参数
        /// </summary>
        private static void RestoreNormalAIParameters(this Faction faction)
        {
            try
            {
                // 恢复正常的敌对度设置
                // faction.PlayerHostility = faction.OriginalPlayerHostility;
                
                System.Diagnostics.Debug.WriteLine($"[联盟AI] {faction.Name} 正常AI参数恢复完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟AI] RestoreNormalAIParameters 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 联盟AI决策：选择行动目标
        /// </summary>
        /// <param name="faction">当前势力</param>
        /// <returns>推荐的行动目标</returns>
        public static Architecture GetCoalitionActionTarget(this Faction faction)
        {
            try
            {
                if (!CoalitionStateManager.IsInCoalition(faction))
                    return null;

                // 优先攻击联盟指定的目标
                Architecture coalitionTarget = CoalitionManager.Instance?.GetCoalitionTarget();
                if (coalitionTarget != null && CanAttackTarget(faction, coalitionTarget))
                {
                    System.Diagnostics.Debug.WriteLine($"[联盟AI] {faction.Name} 选择联盟目标: {coalitionTarget.Name}");
                    return coalitionTarget;
                }

                // 如果联盟目标不可达，选择玩家的其他城市
                Architecture alternativeTarget = FindAlternativePlayerTarget(faction);
                if (alternativeTarget != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[联盟AI] {faction.Name} 选择替代目标: {alternativeTarget.Name}");
                    return alternativeTarget;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟AI] GetCoalitionActionTarget 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 联盟AI决策：军事行动优先级
        /// </summary>
        /// <param name="faction">当前势力</param>
        /// <returns>行动优先级评分</returns>
        public static float GetCoalitionActionPriority(this Faction faction, string actionType, object target)
        {
            try
            {
                if (!CoalitionStateManager.IsInCoalition(faction))
                    return 0f;

                float priority = 0f;

                switch (actionType.ToLower())
                {
                    case "attack":
                        if (target is Architecture arch)
                        {
                            priority = EvaluateAttackPriority(faction, arch);
                        }
                        break;

                    case "recruit":
                        if (target is Person person)
                        {
                            priority = EvaluateRecruitPriority(faction, person);
                        }
                        break;

                    case "diplomacy":
                        if (target is Faction targetFaction)
                        {
                            priority = EvaluateDiplomacyPriority(faction, targetFaction);
                        }
                        break;

                    case "develop":
                        priority = EvaluateDevelopmentPriority(faction);
                        break;
                }

                return priority;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟AI] GetCoalitionActionPriority 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 联盟AI决策：使用军师预测系统
        /// </summary>
        /// <param name="faction">当前势力</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="target">目标</param>
        /// <returns>是否应该执行该行动</returns>
        public static bool ShouldExecuteCoalitionAction(this Faction faction, string actionType, object target)
        {
            try
            {
                if (!CoalitionStateManager.IsInCoalition(faction) || faction.Advisor == null)
                    return false;

                // 使用军师预测系统评估行动成功率
                int successChance = 0;

                switch (actionType.ToLower())
                {
                    case "attack":
                        if (target is Architecture arch && arch.Mayor != null)
                        {
                            successChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(
                                faction.Advisor, arch.Mayor, "Siege");
                        }
                        break;

                    case "recruit":
                        if (target is Person person)
                        {
                            successChance = AdvisorPredictionSystem.GetRecruitChanceDisplay(
                                faction.Advisor, person);
                        }
                        break;

                    case "diplomacy":
                        if (target is Faction targetFaction)
                        {
                            successChance = AdvisorPredictionSystem.GetDiplomacyChanceDisplay(
                                faction.Advisor, targetFaction, "Alliance");
                        }
                        break;
                }

                // 联盟模式下，AI更愿意冒险
                int threshold = GetCoalitionRiskThreshold(faction);
                bool shouldExecute = successChance >= threshold;

                System.Diagnostics.Debug.WriteLine($"[联盟AI] {faction.Name} 军师预测 {actionType} 成功率: {successChance}%, 阈值: {threshold}%, 决策: {(shouldExecute ? "执行" : "放弃")}");

                return shouldExecute;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟AI] ShouldExecuteCoalitionAction 失败: {ex.Message}");
                return false;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 检查是否可以攻击目标
        /// </summary>
        private static bool CanAttackTarget(Faction faction, Architecture target)
        {
            try
            {
                if (faction?.Architectures == null || target == null) return false;

                // 检查是否有邻近的己方城市
                foreach (Architecture ownArch in faction.Architectures.GetList())
                {
                    if (ownArch == null) continue;
                    
                    // 简化的距离检查
                    if (IsAdjacent(ownArch, target))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 寻找替代的玩家目标
        /// </summary>
        private static Architecture FindAlternativePlayerTarget(Faction faction)
        {
            try
            {
                Faction playerFaction = Session.Current?.Scenario?.Factions?.GetList()
                    ?.FirstOrDefault(f => f != null && f.IsPlayer);

                if (playerFaction?.Architectures == null) return null;

                // 寻找最近的玩家城市
                Architecture closestTarget = null;
                float minDistance = float.MaxValue;

                foreach (Architecture playerArch in playerFaction.Architectures.GetList())
                {
                    if (playerArch == null) continue;

                    foreach (Architecture ownArch in faction.Architectures.GetList())
                    {
                        if (ownArch == null) continue;

                        float distance = CalculateDistance(ownArch, playerArch);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestTarget = playerArch;
                        }
                    }
                }

                return closestTarget;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 评估攻击优先级
        /// </summary>
        private static float EvaluateAttackPriority(Faction faction, Architecture target)
        {
            float priority = 50f; // 基础优先级

            // 如果是联盟目标，大幅提高优先级
            if (CoalitionManager.Instance?.GetCoalitionTarget() == target)
            {
                priority += 40f;
            }

            // 如果是玩家城市，提高优先级
            if (target.BelongedFaction?.IsPlayer == true)
            {
                priority += 30f;
            }

            // 基于城市价值调整
            priority += target.Population * 0.001f;
            priority += target.Fund * 0.0001f;

            return priority;
        }

        /// <summary>
        /// 评估招募优先级
        /// </summary>
        private static float EvaluateRecruitPriority(Faction faction, Person person)
        {
            float priority = 30f; // 基础优先级

            // 联盟模式下，优先招募高能力人才
            if (person.Intelligence >= 80 || person.Command >= 80)
            {
                priority += 20f;
            }

            // 如果是玩家势力的人，优先级更高
            if (person.BelongedFaction?.IsPlayer == true)
            {
                priority += 15f;
            }

            return priority;
        }

        /// <summary>
        /// 评估外交优先级
        /// </summary>
        private static float EvaluateDiplomacyPriority(Faction faction, Faction target)
        {
            float priority = 20f; // 基础优先级

            // 联盟模式下，不与玩家外交
            if (target.IsPlayer)
            {
                return 0f;
            }

            // 与其他联盟成员保持友好
            if (CoalitionStateManager.IsInCoalition(target))
            {
                priority += 30f;
            }

            return priority;
        }

        /// <summary>
        /// 评估发展优先级
        /// </summary>
        private static float EvaluateDevelopmentPriority(Faction faction)
        {
            // 联盟模式下，降低发展优先级，专注军事
            return 10f;
        }

        /// <summary>
        /// 获取联盟风险阈值
        /// </summary>
        private static int GetCoalitionRiskThreshold(Faction faction)
        {
            int baseThreshold = 50;

            // 根据君主性格调整风险承受度
            if (faction.Leader != null)
            {
                switch (faction.Leader.CharacterKindID)
                {
                    case 0: return baseThreshold + 10; // 仁德型更谨慎
                    case 1: return baseThreshold + 15; // 多疑型很谨慎
                    case 2: return baseThreshold - 20; // 莽撞型很冒险
                    case 3: return baseThreshold - 5;  // 狡诈型稍微冒险
                }
            }

            return baseThreshold;
        }

        /// <summary>
        /// 检查两个建筑是否相邻
        /// </summary>
        private static bool IsAdjacent(Architecture arch1, Architecture arch2)
        {
            try
            {
                if (arch1?.ArchitectureArea?.Centre == null || arch2?.ArchitectureArea?.Centre == null)
                    return false;

                float distance = CalculateDistance(arch1, arch2);
                return distance <= 30f; // 简化的相邻判断
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 计算两个建筑间的距离
        /// </summary>
        private static float CalculateDistance(Architecture arch1, Architecture arch2)
        {
            try
            {
                if (arch1?.ArchitectureArea?.Centre == null || arch2?.ArchitectureArea?.Centre == null)
                    return float.MaxValue;

                var pos1 = arch1.ArchitectureArea.Centre;
                var pos2 = arch2.ArchitectureArea.Centre;
                
                float dx = pos1.X - pos2.X;
                float dy = pos1.Y - pos2.Y;
                
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }
            catch
            {
                return float.MaxValue;
            }
        }

        #endregion
    }

    /// <summary>
    /// 联盟状态管理器
    /// 用于管理势力的联盟状态，因为无法直接修改Faction类
    /// </summary>
    public static class CoalitionStateManager
    {
        private static readonly System.Collections.Generic.Dictionary<int, CoalitionMemberInfo> _coalitionStates 
            = new System.Collections.Generic.Dictionary<int, CoalitionMemberInfo>();

        /// <summary>
        /// 设置联盟状态
        /// </summary>
        public static void SetCoalitionState(Faction faction, bool isInCoalition, Faction leader)
        {
            if (faction == null) return;

            if (isInCoalition && leader != null)
            {
                _coalitionStates[faction.ID] = new CoalitionMemberInfo
                {
                    IsInCoalition = true,
                    LeaderID = leader.ID,
                    JoinTime = DateTime.Now
                };
            }
            else
            {
                _coalitionStates.Remove(faction.ID);
            }
        }

        /// <summary>
        /// 检查是否在联盟中
        /// </summary>
        public static bool IsInCoalition(Faction faction)
        {
            return faction != null && _coalitionStates.ContainsKey(faction.ID);
        }

        /// <summary>
        /// 获取盟主ID
        /// </summary>
        public static int GetLeaderID(Faction faction)
        {
            if (faction != null && _coalitionStates.TryGetValue(faction.ID, out var info))
            {
                return info.LeaderID;
            }
            return -1;
        }

        /// <summary>
        /// 清理已销毁势力的状态
        /// </summary>
        public static void CleanupDestroyedFactions()
        {
            var toRemove = _coalitionStates.Where(kvp => 
            {
                var faction = Session.Current?.Scenario?.Factions?.GetGameObject(kvp.Key);
                return faction == null || faction.Destroyed;
            }).Select(kvp => kvp.Key).ToList();

            foreach (int factionID in toRemove)
            {
                _coalitionStates.Remove(factionID);
            }
        }
    }

    /// <summary>
    /// 联盟成员信息
    /// </summary>
    public class CoalitionMemberInfo
    {
        public bool IsInCoalition;
        public int LeaderID;
        public DateTime JoinTime;
    }
}