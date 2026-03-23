using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 年度推荐管理器 - 优化版本
    /// </summary>
    public class YearlyRecommendationManager
    {
        private static YearlyRecommendationManager _instance;
        public static YearlyRecommendationManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new YearlyRecommendationManager();
                return _instance;
            }
        }

        public Faction CurrentFaction { get; private set; }
        private AdvisorRecommendationSystem _advisorSystem = new AdvisorRecommendationSystem();
        
        // 🎯 优化：专门的年度推荐完成缓存
        private HashSet<int> _completedRecommendations = new HashSet<int>();
        private int _cacheYear = 0; // 记录缓存对应的年份

        private YearlyRecommendationManager()
        {
        }

        /// <summary>
        /// 回合开始时调用 - 优化版年度推荐检测
        /// </summary>
        public void OnTurnStart(Faction faction)
        {
            CurrentFaction = faction;
            
            try
            {
                int currentYear = Session.Current.Scenario.Date.Year;
                int currentMonth = Session.Current.Scenario.Date.Month;
                
                // 🎯 优化1：检查并更新缓存年份（每年2月自动清理）
                if (currentMonth == 2 && _cacheYear < currentYear)
                {
                    ClearCompletedCache(currentYear);
                    return; // 清理完缓存后直接返回，2月不执行推荐
                }
                
                // 🎯 优化2：只在每年1月检测
                if (currentMonth != 1)
                {
                    return; // 直接返回，不输出日志，节省性能
                }
                
                // 🎯 优化3：快速检查是否已完成（O(1)操作）
                if (_completedRecommendations.Contains(faction.ID))
                {
                    return; // 已完成，直接返回，不输出日志
                }
                
                // 🎯 优化4：提前检查军师，没有军师直接跳过
                if (faction?.Advisor == null)
                {
                    return; // 无军师，直接返回，不输出日志
                }
                
                // 只有真正需要执行推荐时才输出详细日志
                bool isPlayer = Session.Current.Scenario.IsPlayer(faction);
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] ===== 年度推荐开始 =====");
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 势力: {faction.Name}");
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 势力类型: {(isPlayer ? "玩家势力" : "AI势力")}");
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 军师: {faction.Advisor.Name}");
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 军师智力: {faction.Advisor.Intelligence}");
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 当前年份: {currentYear}");
                
                // 🎯 优化5：标记为已完成（在执行前标记，避免异常时重复执行）
                _completedRecommendations.Add(faction.ID);
                
                // 🔥 关键修复：直接使用统一的推荐方法，避免重复调用
                Person foundPerson;
                int initialLoyalty;
                var result = _advisorSystem.AttemptRecommendation(faction, out foundPerson, out initialLoyalty);
                
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 推荐结果: {result}");
                if (foundPerson != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 发现人才: {foundPerson.Name}");
                }
                
                // 🔥 关键修复：只有玩家势力才显示对话，AI势力静默处理
                if (isPlayer)
                {
                    // 玩家势力：显示对话让玩家选择
                    System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 玩家势力，调用HandleRecommendationResult显示对话");
                    AdvisorRecommendationSystem.HandleRecommendationResult(faction, result, foundPerson, initialLoyalty);
                }
                else
                {
                    // AI势力：静默处理，不显示对话
                    System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] AI势力，静默处理推荐结果");
                }
                
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 年度推荐流程执行完成");
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] ===== 年度推荐结束 =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] OnTurnStart异常: {ex.Message}");
                // 异常时从缓存中移除，允许重试
                _completedRecommendations.Remove(faction.ID);
            }
        }

        /// <summary>
        /// 处理人才选择 - 使用完整的推荐系统
        /// </summary>
        public void HandleTalentSelection(Faction faction, Person selectedTalent)
        {
            try
            {
                if (faction != null && selectedTalent != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 处理人才选择: {selectedTalent.Name}");
                    
                    // 使用完整的推荐系统处理人才选择
                    _advisorSystem.HandleTalentSelection(faction, selectedTalent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] HandleTalentSelection异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理完成缓存 - 每年2月自动调用
        /// </summary>
        private void ClearCompletedCache(int currentYear)
        {
            int previousCount = _completedRecommendations.Count;
            _completedRecommendations.Clear();
            _cacheYear = currentYear;
            
            System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 清理年度推荐缓存，年份: {currentYear}");
            System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 清理了 {previousCount} 个完成记录");
        }

        /// <summary>
        /// 手动清理年度记录 - 兼容原有接口
        /// </summary>
        public void ClearYearlyRecords()
        {
            int currentYear = Session.Current.Scenario.Date.Year;
            ClearCompletedCache(currentYear);
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"年度推荐缓存: {_completedRecommendations.Count} 个已完成记录, 缓存年份: {_cacheYear}";
        }

        /// <summary>
        /// 检查指定势力是否已完成年度推荐
        /// </summary>
        /// <param name="factionId">势力ID</param>
        /// <returns>true表示已完成，false表示未完成</returns>
        public bool IsRecommendationCompleted(int factionId)
        {
            return _completedRecommendations.Contains(factionId);
        }

        /// <summary>
        /// 强制重置某个势力的推荐状态（用于调试）
        /// </summary>
        public void ResetFactionRecommendation(int factionId)
        {
            _completedRecommendations.Remove(factionId);
            System.Diagnostics.Debug.WriteLine($"[YearlyRecommendationManager] 重置势力 {factionId} 的推荐状态");
        }
    }
}