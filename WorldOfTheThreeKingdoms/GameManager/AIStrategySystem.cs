using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameManager;

namespace GameManager
{
    /// <summary>
    /// AI战略系统 - 处理AI君主与军师的互动决策
    /// </summary>
    public static class AIStrategySystem
    {
        /// <summary>
        /// 计略类型枚举
        /// </summary>
        public enum StratagemType
        {
            FireAttack,     // 火攻
            WaterAttack,    // 水攻
            Ambush,         // 伏兵
            Provocation,    // 挑衅
            Confusion,      // 混乱
            Retreat,        // 撤退
            Rally,          // 鼓舞
            DirectAssault   // 直接强攻
        }

        /// <summary>
        /// 计略信息结构
        /// </summary>
        public struct Stratagem
        {
            public StratagemType Type;
            public string Name;
            public string Description;
            public int SuccessRate;
            public int Aggressiveness; // 激进程度 0-100
            public int IntelligenceRequired; // 需要的智力
        }

        /// <summary>
        /// AI执行计略的主要方法
        /// </summary>
        /// <param name="faction">AI势力</param>
        /// <param name="enemyTroop">敌方部队</param>
        public static void AI_ExecuteStratagem(Faction faction, Troop enemyTroop)
        {
            if (faction == null || enemyTroop == null || faction.Leader == null)
                return;

            System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 开始制定对 {enemyTroop.Leader?.Name} 的作战计划");

            // 1. AI计算最佳计略
            Stratagem? bestStratagem = CalculateBestStratagem(faction, enemyTroop);
            
            if (bestStratagem == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 未找到合适的计略，选择直接进攻");
                ExecuteDirectAssault(faction, enemyTroop);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[AI策略] 军师建议使用: {bestStratagem.Value.Name} (成功率{bestStratagem.Value.SuccessRate}%)");

            // 2. 检查是否有军师，以及君主是否听从建议
            if (faction.Advisor != null)
            {
                bool willListen = faction.AICheckListenToAdvisor();
                
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.LeaderName} 是否听从军师建议: {(willListen ? "是" : "否")}");

                if (!willListen)
                {
                    // AI君主拒绝了军师的最佳建议
                    HandleAdvisorRejection(faction, bestStratagem.Value, enemyTroop);
                    
                    // 根据君主性格选择替代方案
                    bestStratagem = GetAlternativeStratagem(faction, bestStratagem.Value);
                    
                    // 显示拒绝消息（已在AICheckListenToAdvisor中处理）
                    // 这里可以添加额外的战略相关消息
                    if (IsPlayerGivenFactionInfo(faction))
                    {
                        string strategyMessage = $"{faction.LeaderName} 拒绝了军师的 {bestStratagem?.Name ?? "计策"} 建议，改用其他策略！";
                        DisplayMessageToPlayer(strategyMessage, faction.Leader);
                    }
                }
                else
                {
                    // 君主接受军师建议
                    if (IsPlayerGivenFactionInfo(faction))
                    {
                        string message = $"{faction.LeaderName} 采纳了军师 {faction.AdvisorName} 的建议，准备使用 {bestStratagem.Value.Name}！";
                        DisplayMessageToPlayer(message, faction.Leader);
                    }
                }
            }
            else
            {
                // 无军师情况，君主独自决策
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 无军师，君主独自决策");
                
                // 无军师时，君主可能选择更保守或更激进的策略
                bestStratagem = AdjustStratagemWithoutAdvisor(faction, bestStratagem.Value);
            }

            // 3. 执行最终决定
            if (bestStratagem != null)
            {
                ExecuteStratagem(faction, enemyTroop, bestStratagem.Value);
            }
            else
            {
                // 如果所有计略都被拒绝，执行直接强攻
                ExecuteDirectAssault(faction, enemyTroop);
            }
        }

        /// <summary>
        /// 计算最佳计略
        /// </summary>
        private static Stratagem? CalculateBestStratagem(Faction faction, Troop enemyTroop)
        {
            var availableStratagems = GetAvailableStratagems(faction, enemyTroop);
            
            if (!availableStratagems.Any())
                return null;

            // 根据成功率和当前战况选择最佳计略
            var bestStratagem = availableStratagems
                .Where(s => s.SuccessRate >= 30) // 至少30%成功率才考虑
                .OrderByDescending(s => s.SuccessRate)
                .ThenByDescending(s => GetStratagemValue(s, faction, enemyTroop))
                .FirstOrDefault();

            return bestStratagem;
        }

        /// <summary>
        /// 获取可用的计略列表
        /// </summary>
        private static List<Stratagem> GetAvailableStratagems(Faction faction, Troop enemyTroop)
        {
            var stratagems = new List<Stratagem>();
            
            if (faction.Leader == null) return stratagems;

            // 火攻
            if (faction.Leader.Intelligence >= 70)
            {
                int fireSuccessRate = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.FirePlot);
                if (fireSuccessRate > 0)
                {
                    stratagems.Add(new Stratagem
                    {
                        Type = StratagemType.FireAttack,
                        Name = "火攻",
                        Description = "使用火计攻击敌军",
                        SuccessRate = fireSuccessRate,
                        Aggressiveness = 70,
                        IntelligenceRequired = 70
                    });
                }
            }

            // 水攻
            if (faction.Leader.Intelligence >= 70)
            {
                int waterSuccessRate = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.WaterPlot);
                if (waterSuccessRate > 0)
                {
                    stratagems.Add(new Stratagem
                    {
                        Type = StratagemType.WaterAttack,
                        Name = "水攻",
                        Description = "引水淹敌",
                        SuccessRate = waterSuccessRate,
                        Aggressiveness = 75,
                        IntelligenceRequired = 70
                    });
                }
            }

            // 伏兵
            if (faction.Leader.Command >= 70)
            {
                int ambushSuccessRate = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.Ambush);
                if (ambushSuccessRate > 0)
                {
                    stratagems.Add(new Stratagem
                    {
                        Type = StratagemType.Ambush,
                        Name = "伏兵",
                        Description = "设置伏兵偷袭",
                        SuccessRate = ambushSuccessRate,
                        Aggressiveness = 60,
                        IntelligenceRequired = 60
                    });
                }
            }

            // 挑衅
            if (faction.Leader.Charm >= 70)
            {
                int provokeSuccessRate = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.Provoke);
                if (provokeSuccessRate > 0)
                {
                    stratagems.Add(new Stratagem
                    {
                        Type = StratagemType.Provocation,
                        Name = "挑衅",
                        Description = "激怒敌将",
                        SuccessRate = provokeSuccessRate,
                        Aggressiveness = 80,
                        IntelligenceRequired = 50
                    });
                }
            }

            // 混乱
            if (faction.Leader.Intelligence >= 65)
            {
                int confuseSuccessRate = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.Confuse);
                if (confuseSuccessRate > 0)
                {
                    stratagems.Add(new Stratagem
                    {
                        Type = StratagemType.Confusion,
                        Name = "混乱",
                        Description = "扰乱敌军阵型",
                        SuccessRate = confuseSuccessRate,
                        Aggressiveness = 50,
                        IntelligenceRequired = 65
                    });
                }
            }

            // 撤退
            int retreatSuccessRate = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.Retreat);
            if (retreatSuccessRate > 0)
            {
                stratagems.Add(new Stratagem
                {
                    Type = StratagemType.Retreat,
                    Name = "撤退",
                    Description = "有序撤退保存实力",
                    SuccessRate = retreatSuccessRate,
                    Aggressiveness = 10,
                    IntelligenceRequired = 40
                });
            }

            // 鼓舞
            if (faction.Leader.Command >= 60)
            {
                int rallySuccessRate = StrategistManager.GetBattlePrediction(faction, enemyTroop, StrategistManager.SkillType.Rally);
                if (rallySuccessRate > 0)
                {
                    stratagems.Add(new Stratagem
                    {
                        Type = StratagemType.Rally,
                        Name = "鼓舞",
                        Description = "鼓舞士气提升战力",
                        SuccessRate = rallySuccessRate,
                        Aggressiveness = 30,
                        IntelligenceRequired = 50
                    });
                }
            }

            // 直接强攻（总是可用）
            stratagems.Add(new Stratagem
            {
                Type = StratagemType.DirectAssault,
                Name = "强攻",
                Description = "直接进攻敌军",
                SuccessRate = 60, // 基础成功率
                Aggressiveness = 90,
                IntelligenceRequired = 0
            });

            return stratagems;
        }

        /// <summary>
        /// 计算计略价值
        /// </summary>
        private static int GetStratagemValue(Stratagem stratagem, Faction faction, Troop enemyTroop)
        {
            int value = stratagem.SuccessRate;
            
            // 根据君主性格调整价值
            int personalityId = faction.Leader.Character?.ID ?? 0;
            switch (personalityId)
            {
                case 0: // 仁德型 - 偏好低风险策略
                    if (stratagem.Aggressiveness < 50) value += 20;
                    break;
                case 1: // 霸道型 - 偏好高攻击性策略
                    if (stratagem.Aggressiveness > 70) value += 25;
                    break;
                case 2: // 冷静型 - 偏好高成功率策略
                    if (stratagem.SuccessRate > 70) value += 15;
                    break;
                case 3: // 莽撞型 - 偏好激进策略
                    if (stratagem.Aggressiveness > 60) value += 30;
                    if (stratagem.Type == StratagemType.DirectAssault) value += 20;
                    break;
                case 4: // 狡诈型 - 偏好智谋策略
                    if (stratagem.IntelligenceRequired > 60) value += 20;
                    break;
            }

            return value;
        }

        /// <summary>
        /// 处理军师建议被拒绝的情况
        /// </summary>
        private static void HandleAdvisorRejection(Faction faction, Stratagem rejectedStratagem, Troop enemyTroop)
        {
            System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.LeaderName} 拒绝了军师的 {rejectedStratagem.Name} 建议");
            
            // 记录拒绝事件，可用于后续的AI学习或剧情触发
            // GameEventManager.RecordAdvisorRejection(faction, rejectedStratagem);
            
            // 军师忠诚度可能受到影响
            if (faction.Advisor != null)
            {
                // 如果经常被拒绝，军师可能会不满
                // faction.Advisor.Loyalty = Math.Max(0, faction.Advisor.Loyalty - 1);
            }
        }

        /// <summary>
        /// 获取替代计略（君主拒绝军师建议后的选择）
        /// </summary>
        private static Stratagem? GetAlternativeStratagem(Faction faction, Stratagem rejectedStratagem)
        {
            int personalityId = faction.Leader.Character?.ID ?? 0;
            
            switch (personalityId)
            {
                case 0: // 仁德型 - 可能选择更保守的策略
                    return GetConservativeAlternative(faction, rejectedStratagem);
                    
                case 1: // 霸道型 - 可能选择更激进的策略
                case 3: // 莽撞型 - 倾向于直接强攻
                    return GetAggressiveAlternative(faction, rejectedStratagem);
                    
                case 2: // 冷静型 - 可能重新分析选择其他策略
                    return GetRationalAlternative(faction, rejectedStratagem);
                    
                case 4: // 狡诈型 - 可能选择出人意料的策略
                    return GetCunningAlternative(faction, rejectedStratagem);
                    
                default:
                    // 默认选择直接强攻
                    return new Stratagem
                    {
                        Type = StratagemType.DirectAssault,
                        Name = "强攻",
                        Description = "君主决定直接进攻",
                        SuccessRate = 50,
                        Aggressiveness = 90,
                        IntelligenceRequired = 0
                    };
            }
        }

        /// <summary>
        /// 获取保守的替代策略
        /// </summary>
        private static Stratagem? GetConservativeAlternative(Faction faction, Stratagem rejectedStratagem)
        {
            // 仁德型君主可能选择撤退或鼓舞士气
            if (rejectedStratagem.Aggressiveness > 60)
            {
                return new Stratagem
                {
                    Type = StratagemType.Rally,
                    Name = "鼓舞",
                    Description = "君主选择鼓舞士气",
                    SuccessRate = 70,
                    Aggressiveness = 30,
                    IntelligenceRequired = 40
                };
            }
            return null; // 放弃使用计略
        }

        /// <summary>
        /// 获取激进的替代策略
        /// </summary>
        private static Stratagem? GetAggressiveAlternative(Faction faction, Stratagem rejectedStratagem)
        {
            // 霸道型/莽撞型君主倾向于直接强攻
            return new Stratagem
            {
                Type = StratagemType.DirectAssault,
                Name = "强攻",
                Description = "君主决定直接进攻",
                SuccessRate = 60,
                Aggressiveness = 95,
                IntelligenceRequired = 0
            };
        }

        /// <summary>
        /// 获取理性的替代策略
        /// </summary>
        private static Stratagem? GetRationalAlternative(Faction faction, Troop enemyTroop)
        {
            // 冷静型君主会重新分析，选择次优策略
            var alternatives = GetAvailableStratagems(faction, enemyTroop)
                .Where(s => s.SuccessRate >= 40)
                .OrderByDescending(s => s.SuccessRate)
                .Skip(1) // 跳过最优策略
                .FirstOrDefault();
                
            return alternatives;
        }

        /// <summary>
        /// 获取狡诈的替代策略
        /// </summary>
        private static Stratagem? GetCunningAlternative(Faction faction, Stratagem rejectedStratagem)
        {
            // 狡诈型君主可能选择出人意料的策略
            if (rejectedStratagem.Type != StratagemType.Confusion)
            {
                return new Stratagem
                {
                    Type = StratagemType.Confusion,
                    Name = "混乱",
                    Description = "君主选择扰乱敌军",
                    SuccessRate = 55,
                    Aggressiveness = 50,
                    IntelligenceRequired = 65
                };
            }
            
            return GetAggressiveAlternative(faction, rejectedStratagem);
        }

        /// <summary>
        /// 无军师时调整策略
        /// </summary>
        private static Stratagem? AdjustStratagemWithoutAdvisor(Faction faction, Stratagem originalStratagem)
        {
            int personalityId = faction.Leader.Character?.ID ?? 0;
            
            // 无军师时，君主的决策更受性格影响
            switch (personalityId)
            {
                case 0: // 仁德型 - 更保守
                    if (originalStratagem.Aggressiveness > 70)
                    {
                        originalStratagem.SuccessRate = Math.Max(30, originalStratagem.SuccessRate - 20);
                    }
                    break;
                    
                case 1: // 霸道型 - 更激进
                case 3: // 莽撞型 - 更冲动
                    if (originalStratagem.Aggressiveness < 70)
                    {
                        return GetAggressiveAlternative(faction, originalStratagem);
                    }
                    break;
                    
                case 2: // 冷静型 - 相对稳定
                    originalStratagem.SuccessRate = Math.Max(20, originalStratagem.SuccessRate - 10);
                    break;
                    
                case 4: // 狡诈型 - 可能过度自信
                    originalStratagem.SuccessRate = Math.Max(25, originalStratagem.SuccessRate - 15);
                    break;
            }
            
            return originalStratagem;
        }

        /// <summary>
        /// 执行计略
        /// </summary>
        private static void ExecuteStratagem(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 执行 {stratagem.Name}");
            
            // 根据计略类型执行相应的游戏逻辑
            switch (stratagem.Type)
            {
                case StratagemType.FireAttack:
                    ExecuteFireAttack(faction, enemyTroop, stratagem);
                    break;
                case StratagemType.WaterAttack:
                    ExecuteWaterAttack(faction, enemyTroop, stratagem);
                    break;
                case StratagemType.Ambush:
                    ExecuteAmbush(faction, enemyTroop, stratagem);
                    break;
                case StratagemType.Provocation:
                    ExecuteProvocation(faction, enemyTroop, stratagem);
                    break;
                case StratagemType.Confusion:
                    ExecuteConfusion(faction, enemyTroop, stratagem);
                    break;
                case StratagemType.Retreat:
                    ExecuteRetreat(faction, enemyTroop, stratagem);
                    break;
                case StratagemType.Rally:
                    ExecuteRally(faction, enemyTroop, stratagem);
                    break;
                case StratagemType.DirectAssault:
                    ExecuteDirectAssault(faction, enemyTroop);
                    break;
            }
        }

        /// <summary>
        /// 执行火攻
        /// </summary>
        private static void ExecuteFireAttack(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            Random random = new Random();
            bool success = random.Next(100) < stratagem.SuccessRate;
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的火攻成功！");
                // 应用火攻效果
                // enemyTroop.Quantity = (int)(enemyTroop.Quantity * 0.7f); // 减少敌军兵力
                // enemyTroop.Morale = Math.Max(0, enemyTroop.Morale - 30); // 降低士气
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的火攻失败！");
            }
        }

        /// <summary>
        /// 执行水攻
        /// </summary>
        private static void ExecuteWaterAttack(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            Random random = new Random();
            bool success = random.Next(100) < stratagem.SuccessRate;
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的水攻成功！");
                // 应用水攻效果
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的水攻失败！");
            }
        }

        /// <summary>
        /// 执行伏兵
        /// </summary>
        private static void ExecuteAmbush(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            Random random = new Random();
            bool success = random.Next(100) < stratagem.SuccessRate;
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的伏兵成功！");
                // 应用伏兵效果
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的伏兵被发现！");
            }
        }

        /// <summary>
        /// 执行挑衅
        /// </summary>
        private static void ExecuteProvocation(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            Random random = new Random();
            bool success = random.Next(100) < stratagem.SuccessRate;
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 成功激怒了敌将！");
                // 应用挑衅效果
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的挑衅没有效果！");
            }
        }

        /// <summary>
        /// 执行混乱
        /// </summary>
        private static void ExecuteConfusion(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            Random random = new Random();
            bool success = random.Next(100) < stratagem.SuccessRate;
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 成功扰乱了敌军！");
                // 应用混乱效果
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 的混乱计略失败！");
            }
        }

        /// <summary>
        /// 执行撤退
        /// </summary>
        private static void ExecuteRetreat(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 选择有序撤退！");
            // 执行撤退逻辑
        }

        /// <summary>
        /// 执行鼓舞
        /// </summary>
        private static void ExecuteRally(Faction faction, Troop enemyTroop, Stratagem stratagem)
        {
            System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 鼓舞士气！");
            // 应用鼓舞效果
            // faction.Troops.ForEach(t => t.Morale = Math.Min(100, t.Morale + 20));
        }

        /// <summary>
        /// 执行直接强攻
        /// </summary>
        private static void ExecuteDirectAssault(Faction faction, Troop enemyTroop)
        {
            System.Diagnostics.Debug.WriteLine($"[AI策略] {faction.Name} 选择直接强攻！");
            // 执行直接攻击逻辑
        }

        /// <summary>
        /// 生成拒绝军师建议的消息
        /// </summary>
        private static string GenerateRejectionMessage(Faction faction, Stratagem? alternativeStratagem)
        {
            string baseMessage = $"{faction.LeaderName} 拒绝了军师 {faction.AdvisorName} 的建议";
            
            if (alternativeStratagem != null)
            {
                baseMessage += $"，决定使用 {alternativeStratagem.Value.Name}！";
            }
            else
            {
                baseMessage += "，决定放弃使用计略！";
            }
            
            return baseMessage;
        }

        /// <summary>
        /// 检查玩家是否拥有该势力的情报
        /// </summary>
        private static bool IsPlayerGivenFactionInfo(Faction faction)
        {
            // 这里应该检查玩家是否有该势力的情报网络或间谍
            // 简化实现：如果是邻近势力或有外交关系，则可以获得情报
            return true; // 暂时返回true，实际游戏中需要根据情报系统判断
        }

        /// <summary>
        /// 向玩家显示消息
        /// </summary>
        private static void DisplayMessageToPlayer(string message, Person leader)
        {
            System.Diagnostics.Debug.WriteLine($"[玩家消息] {message}");
            
            // 这里应该调用游戏的消息系统
            // Session.MainGame.mainGameScreen.AddTextMessage(message, leader.Position);
            
            // 或者添加到消息队列
            // MessageSystem.AddMessage(message, MessageType.Intelligence, leader);
        }

        /// <summary>
        /// 获取AI决策的详细分析（用于调试和测试）
        /// </summary>
        public static string GetAIDecisionAnalysis(Faction faction, Troop enemyTroop)
        {
            var analysis = new System.Text.StringBuilder();
            analysis.AppendLine($"=== {faction.Name} 的AI决策分析 ===");
            analysis.AppendLine($"君主: {faction.LeaderName} (智{faction.Leader?.Intelligence} 统{faction.Leader?.Command} 魅{faction.Leader?.Charm})");
            
            if (faction.Advisor != null)
            {
                analysis.AppendLine($"军师: {faction.AdvisorName} (智{faction.Advisor.Intelligence})");
                
                bool willListen = faction.AICheckListenToAdvisor();
                analysis.AppendLine($"听从军师概率: {(willListen ? "会听从" : "会拒绝")}");
            }
            else
            {
                analysis.AppendLine("军师: 无");
            }
            
            analysis.AppendLine();
            analysis.AppendLine("可用计略分析:");
            
            var availableStratagems = GetAvailableStratagems(faction, enemyTroop);
            foreach (var stratagem in availableStratagems.OrderByDescending(s => s.SuccessRate))
            {
                analysis.AppendLine($"  {stratagem.Name}: 成功率{stratagem.SuccessRate}%, 激进度{stratagem.Aggressiveness}");
            }
            
            var bestStratagem = CalculateBestStratagem(faction, enemyTroop);
            if (bestStratagem != null)
            {
                analysis.AppendLine();
                analysis.AppendLine($"推荐计略: {bestStratagem.Value.Name}");
            }
            
            return analysis.ToString();
        }
    }
}