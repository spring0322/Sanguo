using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 回合资源管理器 - 统一管理所有势力的资源结算
    /// 整合AI难度系统和玩家腐败系统，实现双向平衡
    /// </summary>
    public class TurnResourceManager
    {
        private static TurnResourceManager _instance;
        public static TurnResourceManager Instance => _instance ?? (_instance = new TurnResourceManager());

        /// <summary>
        /// 回合开始：结算所有势力的收入
        /// </summary>
        public void CalculateIncomePhase()
        {
            try
            {
                if (Session.Current?.Scenario?.Factions == null)
                    return;

                var allFactions = Session.Current.Scenario.Factions.GetList().Cast<Faction>().ToList();
                
                // 1. 获取玩家当前的城市数（用于计算全局系数）
                Faction playerFaction = allFactions.FirstOrDefault(f => Session.Current.Scenario.IsPlayer(f));
                if (playerFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[回合资源] 未找到玩家势力，跳过资源结算");
                    return;
                }

                int playerCityCount = playerFaction.Architectures?.Count ?? 0;

                // 2. 预计算本回合的全局系数
                float aiBuffMultiplier = DifficultyManager.GetAIResourceMultiplier(playerCityCount);
                var playerCorruptionStatus = PlayerCorruptionSystem.GetPlayerCorruptionStatus();

                // 记录系统状态
                if (AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[回合资源] ========== 第{GetCurrentTurn()}回合资源结算 ==========");
                    System.Diagnostics.Debug.WriteLine($"[回合资源] 玩家城池数: {playerCityCount}");
                    System.Diagnostics.Debug.WriteLine($"[回合资源] AI资源倍率: {aiBuffMultiplier:F3}x");
                    System.Diagnostics.Debug.WriteLine($"[回合资源] 玩家行政效率: {playerCorruptionStatus.Efficiency:P1}");
                    System.Diagnostics.Debug.WriteLine($"[回合资源] 腐败描述: {PlayerCorruptionSystem.GetCorruptionDescription(playerCorruptionStatus)}");
                }

                // UI 显示提示 (给玩家看)
                if (playerCorruptionStatus.Efficiency < 1.0f)
                {
                    string message = PlayerCorruptionSystem.GetCorruptionDescription(playerCorruptionStatus);
                    System.Diagnostics.Debug.WriteLine($"[回合资源] 玩家提示: {message}");
                    // TODO: 实际游戏中应该调用UI系统显示
                    // UIManager.ShowToast(message);
                }

                // 3. 遍历所有势力发放资源
                int playerIncomeChange = 0;
                int totalAIBonus = 0;
                int processedFactions = 0;

                foreach (var faction in allFactions)
                {
                    if (!faction.IsAlive)
                        continue;

                    int baseIncome = CalculateBaseIncome(faction);
                    int finalIncome = 0;

                    if (Session.Current.Scenario.IsPlayer(faction))
                    {
                        // --- 玩家逻辑：应用腐败衰减 ---
                        finalIncome = PlayerCorruptionSystem.CalculateActualIncome(faction, baseIncome);
                        playerIncomeChange = finalIncome - baseIncome;

                        ApplyIncomeToFaction(faction, finalIncome);

                        if (AIStrategicConfig.EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[回合资源] 玩家 {faction.Name}: 基础收入{baseIncome} -> 实际收入{finalIncome} (效率{playerCorruptionStatus.Efficiency:P1})");
                        }
                    }
                    else
                    {
                        // --- AI逻辑：应用动态补强 ---
                        finalIncome = (int)(baseIncome * aiBuffMultiplier);
                        int aiBonus = finalIncome - baseIncome;
                        totalAIBonus += aiBonus;

                        ApplyIncomeToFaction(faction, finalIncome);

                        if (AIStrategicConfig.EnableDebugLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[回合资源] AI {faction.Name}: 基础收入{baseIncome} -> 实际收入{finalIncome} (倍率{aiBuffMultiplier:F3}x, 加成+{aiBonus})");
                        }
                    }

                    processedFactions++;
                }

                // 4. 记录回合总结
                if (AIStrategicConfig.EnableDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[回合资源] 处理势力数: {processedFactions}");
                    System.Diagnostics.Debug.WriteLine($"[回合资源] 玩家收入变化: {playerIncomeChange:+#;-#;0}");
                    System.Diagnostics.Debug.WriteLine($"[回合资源] AI总加成: +{totalAIBonus}");
                    System.Diagnostics.Debug.WriteLine($"[回合资源] ========== 资源结算完成 ==========");
                }

                // 5. 触发后续系统更新
                OnIncomePhaseCompleted(playerFaction, playerCorruptionStatus, aiBuffMultiplier);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[回合资源] 资源结算时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算势力的基础收入
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>基础收入</returns>
        private int CalculateBaseIncome(Faction faction)
        {
            if (faction?.Architectures == null)
                return 0;

            int totalIncome = 0;

            foreach (Architecture arch in faction.Architectures.GetList().Cast<Architecture>())
            {
                // 简化计算：城市收入 = 人口/100 + 商业/10 + 农业/10
                int cityIncome = (arch.Population / 100) + (arch.Commerce / 10) + (arch.Agriculture / 10);
                totalIncome += Math.Max(100, cityIncome); // 最低100收入
            }

            return totalIncome;
        }

        /// <summary>
        /// 将收入应用到势力
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="income">收入</param>
        private void ApplyIncomeToFaction(Faction faction, int income)
        {
            if (faction == null || income <= 0)
                return;

            try
            {
                // 资金和粮食按比例分配 (7:3)
                int goldIncome = (int)(income * 0.7f);
                int foodIncome = income - goldIncome;

                faction.Fund += goldIncome;
                faction.Food += foodIncome;

                if (AIStrategicConfig.EnableDetailedDecisionLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[回合资源] {faction.Name} 获得: 资金+{goldIncome}, 粮食+{foodIncome}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[回合资源] 应用收入到势力 {faction.Name} 时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 收入阶段完成后的处理
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <param name="corruptionStatus">腐败状态</param>
        /// <param name="aiMultiplier">AI倍率</param>
        private void OnIncomePhaseCompleted(Faction playerFaction, PlayerCorruptionStatus corruptionStatus, float aiMultiplier)
        {
            try
            {
                // 更新统计数据
                UpdateResourceStatistics(playerFaction, corruptionStatus, aiMultiplier);

                // 触发相关系统的回合更新
                if (AIStrategicManager.Instance != null)
                {
                    // AI战略系统会在这里应用难度调整
                    // 这里不需要重复调用，因为AIStrategicManager.UpdateStrategicDecisions()会处理
                }

                // 检查是否需要显示特殊提示
                CheckForSpecialNotifications(corruptionStatus, aiMultiplier);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[回合资源] 收入阶段完成处理时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新资源统计数据
        /// </summary>
        /// <param name="playerFaction">玩家势力</param>
        /// <param name="corruptionStatus">腐败状态</param>
        /// <param name="aiMultiplier">AI倍率</param>
        private void UpdateResourceStatistics(Faction playerFaction, PlayerCorruptionStatus corruptionStatus, float aiMultiplier)
        {
            // 这里可以记录历史数据，用于分析和调试
            // 例如：记录每回合的效率变化、AI加成变化等
            
            if (AIStrategicConfig.EnableDetailedDecisionLog)
            {
                System.Diagnostics.Debug.WriteLine($"[回合资源] 统计更新:");
                System.Diagnostics.Debug.WriteLine($"  玩家资源: 资金{playerFaction.Fund}, 粮食{playerFaction.Food}");
                System.Diagnostics.Debug.WriteLine($"  行政效率: {corruptionStatus.Efficiency:P1}");
                System.Diagnostics.Debug.WriteLine($"  AI加成倍率: {aiMultiplier:F3}x");
            }
        }

        /// <summary>
        /// 检查特殊通知
        /// </summary>
        /// <param name="corruptionStatus">腐败状态</param>
        /// <param name="aiMultiplier">AI倍率</param>
        private void CheckForSpecialNotifications(PlayerCorruptionStatus corruptionStatus, float aiMultiplier)
        {
            // 检查是否首次触发腐败
            if (corruptionStatus.ExcessCities == 1 && corruptionStatus.Efficiency < 1.0f)
            {
                string message = "警告：领土扩张开始影响行政效率！建议任命高政治能力的太守。";
                System.Diagnostics.Debug.WriteLine($"[回合资源] 特殊提示: {message}");
                // TODO: 显示特殊UI提示
            }

            // 检查是否达到效率下限
            if (corruptionStatus.IsAtMinimumFloor)
            {
                string message = "警告：行政效率已降至最低！考虑放弃部分偏远城池或提升太守能力。";
                System.Diagnostics.Debug.WriteLine($"[回合资源] 特殊提示: {message}");
                // TODO: 显示特殊UI提示
            }

            // 检查AI是否获得显著加成
            if (aiMultiplier > 1.2f)
            {
                var difficultyStatus = DifficultyManager.Instance.GetCurrentDifficultyStatus();
                string message = $"天下诸侯感受到了威胁，开始加强实力！当前阶段：{difficultyStatus.GamePhase}";
                System.Diagnostics.Debug.WriteLine($"[回合资源] 特殊提示: {message}");
                // TODO: 显示特殊UI提示
            }
        }

        /// <summary>
        /// 获取当前回合数
        /// </summary>
        /// <returns>当前回合数</returns>
        private int GetCurrentTurn()
        {
            if (Session.Current?.Scenario?.Date != null)
            {
                return Session.Current.Scenario.Date.Year * 12 + Session.Current.Scenario.Date.Month;
            }
            return 0;
        }

        /// <summary>
        /// 获取资源管理状态概览
        /// </summary>
        /// <returns>状态概览</returns>
        public ResourceManagerStatus GetStatus()
        {
            var status = new ResourceManagerStatus();

            try
            {
                if (Session.Current?.Scenario?.Factions != null)
                {
                    var allFactions = Session.Current.Scenario.Factions.GetList().Cast<Faction>().ToList();
                    var playerFaction = allFactions.FirstOrDefault(f => Session.Current.Scenario.IsPlayer(f));

                    if (playerFaction != null)
                    {
                        int playerCityCount = playerFaction.Architectures?.Count ?? 0;
                        
                        status.PlayerCityCount = playerCityCount;
                        status.AIMultiplier = DifficultyManager.GetAIResourceMultiplier(playerCityCount);
                        status.PlayerCorruptionStatus = PlayerCorruptionSystem.GetPlayerCorruptionStatus();
                        status.DifficultyStatus = DifficultyManager.Instance.GetCurrentDifficultyStatus();
                        status.CurrentTurn = GetCurrentTurn();
                        status.TotalFactions = allFactions.Count(f => f.IsAlive);
                        status.AIFactions = allFactions.Count(f => f.IsAlive && !Session.Current.Scenario.IsPlayer(f));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[回合资源] 获取状态时发生异常: {ex.Message}");
            }

            return status;
        }

        /// <summary>
        /// 手动触发资源结算（用于测试）
        /// </summary>
        public void ForceCalculateIncome()
        {
            System.Diagnostics.Debug.WriteLine("[回合资源] 手动触发资源结算");
            CalculateIncomePhase();
        }
    }

    /// <summary>
    /// 资源管理器状态信息
    /// </summary>
    public class ResourceManagerStatus
    {
        public int PlayerCityCount { get; set; }
        public float AIMultiplier { get; set; }
        public PlayerCorruptionStatus PlayerCorruptionStatus { get; set; }
        public DifficultyStatus DifficultyStatus { get; set; }
        public int CurrentTurn { get; set; }
        public int TotalFactions { get; set; }
        public int AIFactions { get; set; }

        public ResourceManagerStatus()
        {
            AIMultiplier = 1.0f;
            PlayerCorruptionStatus = new PlayerCorruptionStatus();
            DifficultyStatus = new DifficultyStatus();
        }
    }
}