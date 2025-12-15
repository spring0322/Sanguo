using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 动态难度调整系统 - 根据玩家表现自动调整AI难度
    /// </summary>
    public class DynamicDifficultySystem
    {
        public static DynamicDifficultySystem Instance { get; private set; }

        // 难度等级
        public enum DifficultyLevel
        {
            VeryEasy = 1,   // 非常简单
            Easy = 2,       // 简单
            Normal = 3,     // 普通
            Hard = 4,       // 困难
            VeryHard = 5,   // 非常困难
            Extreme = 6     // 极限
        }

        // 玩家表现指标
        public class PerformanceMetrics
        {
            public float WinRate { get; set; }              // 胜率
            public float AverageGameDuration { get; set; }   // 平均游戏时长
            public float ResourceEfficiency { get; set; }    // 资源利用效率
            public float TacticalSuccess { get; set; }       // 战术成功率
            public float ExpansionRate { get; set; }         // 扩张速度
            public float DiplomaticSuccess { get; set; }     // 外交成功率
            public DateTime LastUpdate { get; set; }
        }

        // 难度调整参数
        public class DifficultyParameters
        {
            public float AIResourceMultiplier { get; set; } = 1.0f;    // AI资源倍数
            public float AIUnitMultiplier { get; set; } = 1.0f;        // AI单位倍数
            public float AIIntelligenceBonus { get; set; } = 0.0f;     // AI智力加成
            public float AIReactionSpeed { get; set; } = 1.0f;         // AI反应速度
            public float AICoordinationLevel { get; set; } = 1.0f;     // AI协调水平
            public float PlayerResourcePenalty { get; set; } = 0.0f;   // 玩家资源惩罚
            public bool EnableAdvancedAI { get; set; } = true;         // 启用高级AI
            public bool EnableLearning { get; set; } = true;           // 启用学习系统
        }

        private Dictionary<int, PerformanceMetrics> _playerMetrics;
        private Dictionary<DifficultyLevel, DifficultyParameters> _difficultySettings;
        private DifficultyLevel _currentDifficulty;
        private DateTime _lastAdjustment;
        private int _adjustmentCooldown = 300000; // 5分钟冷却时间
        private List<float> _recentPerformanceScores;
        private int _maxPerformanceHistory = 20;

        public DifficultyLevel CurrentDifficulty => _currentDifficulty;

        public DynamicDifficultySystem()
        {
            Instance = this;
            _playerMetrics = new Dictionary<int, PerformanceMetrics>();
            _recentPerformanceScores = new List<float>();
            _currentDifficulty = DifficultyLevel.Normal;
            _lastAdjustment = DateTime.Now;
            InitializeDifficultySettings();
        }

        /// <summary>
        /// 初始化难度设置
        /// </summary>
        private void InitializeDifficultySettings()
        {
            _difficultySettings = new Dictionary<DifficultyLevel, DifficultyParameters>
            {
                [DifficultyLevel.VeryEasy] = new DifficultyParameters
                {
                    AIResourceMultiplier = 0.7f,
                    AIUnitMultiplier = 0.8f,
                    AIIntelligenceBonus = -2.0f,
                    AIReactionSpeed = 0.6f,
                    AICoordinationLevel = 0.5f,
                    PlayerResourcePenalty = -0.2f, // 玩家获得20%资源加成
                    EnableAdvancedAI = false,
                    EnableLearning = false
                },
                [DifficultyLevel.Easy] = new DifficultyParameters
                {
                    AIResourceMultiplier = 0.85f,
                    AIUnitMultiplier = 0.9f,
                    AIIntelligenceBonus = -1.0f,
                    AIReactionSpeed = 0.8f,
                    AICoordinationLevel = 0.7f,
                    PlayerResourcePenalty = -0.1f,
                    EnableAdvancedAI = false,
                    EnableLearning = false
                },
                [DifficultyLevel.Normal] = new DifficultyParameters
                {
                    AIResourceMultiplier = 1.0f,
                    AIUnitMultiplier = 1.0f,
                    AIIntelligenceBonus = 0.0f,
                    AIReactionSpeed = 1.0f,
                    AICoordinationLevel = 1.0f,
                    PlayerResourcePenalty = 0.0f,
                    EnableAdvancedAI = true,
                    EnableLearning = true
                },
                [DifficultyLevel.Hard] = new DifficultyParameters
                {
                    AIResourceMultiplier = 1.2f,
                    AIUnitMultiplier = 1.1f,
                    AIIntelligenceBonus = 1.0f,
                    AIReactionSpeed = 1.3f,
                    AICoordinationLevel = 1.3f,
                    PlayerResourcePenalty = 0.1f,
                    EnableAdvancedAI = true,
                    EnableLearning = true
                },
                [DifficultyLevel.VeryHard] = new DifficultyParameters
                {
                    AIResourceMultiplier = 1.4f,
                    AIUnitMultiplier = 1.25f,
                    AIIntelligenceBonus = 2.0f,
                    AIReactionSpeed = 1.5f,
                    AICoordinationLevel = 1.5f,
                    PlayerResourcePenalty = 0.15f,
                    EnableAdvancedAI = true,
                    EnableLearning = true
                },
                [DifficultyLevel.Extreme] = new DifficultyParameters
                {
                    AIResourceMultiplier = 1.6f,
                    AIUnitMultiplier = 1.4f,
                    AIIntelligenceBonus = 3.0f,
                    AIReactionSpeed = 2.0f,
                    AICoordinationLevel = 2.0f,
                    PlayerResourcePenalty = 0.2f,
                    EnableAdvancedAI = true,
                    EnableLearning = true
                }
            };
        }

        /// <summary>
        /// 更新玩家表现指标
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="metrics">表现指标</param>
        public void UpdatePlayerMetrics(int playerId, PerformanceMetrics metrics)
        {
            try
            {
                _playerMetrics[playerId] = metrics;
                metrics.LastUpdate = DateTime.Now;

                // 计算综合表现分数
                float performanceScore = CalculatePerformanceScore(metrics);
                
                // 添加到历史记录
                _recentPerformanceScores.Add(performanceScore);
                if (_recentPerformanceScores.Count > _maxPerformanceHistory)
                {
                    _recentPerformanceScores.RemoveAt(0);
                }

                // 检查是否需要调整难度
                CheckDifficultyAdjustment();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 更新玩家指标失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算玩家表现分数
        /// </summary>
        /// <param name="metrics">表现指标</param>
        /// <returns>表现分数 (0-100)</returns>
        private float CalculatePerformanceScore(PerformanceMetrics metrics)
        {
            float score = 0;

            // 胜率权重 (40%)
            score += metrics.WinRate * 40;

            // 战术成功率权重 (25%)
            score += metrics.TacticalSuccess * 25;

            // 资源效率权重 (15%)
            score += metrics.ResourceEfficiency * 15;

            // 扩张速度权重 (10%)
            score += metrics.ExpansionRate * 10;

            // 外交成功率权重 (10%)
            score += metrics.DiplomaticSuccess * 10;

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// 检查是否需要调整难度
        /// </summary>
        private void CheckDifficultyAdjustment()
        {
            try
            {
                // 检查冷却时间
                if ((DateTime.Now - _lastAdjustment).TotalMilliseconds < _adjustmentCooldown)
                {
                    return;
                }

                // 需要足够的数据样本
                if (_recentPerformanceScores.Count < 5)
                {
                    return;
                }

                float averageScore = _recentPerformanceScores.Average();
                DifficultyLevel newDifficulty = _currentDifficulty;

                // 根据平均表现调整难度
                if (averageScore > 75 && _currentDifficulty < DifficultyLevel.Extreme)
                {
                    // 表现太好，增加难度
                    newDifficulty = (DifficultyLevel)((int)_currentDifficulty + 1);
                }
                else if (averageScore < 35 && _currentDifficulty > DifficultyLevel.VeryEasy)
                {
                    // 表现不佳，降低难度
                    newDifficulty = (DifficultyLevel)((int)_currentDifficulty - 1);
                }

                if (newDifficulty != _currentDifficulty)
                {
                    AdjustDifficulty(newDifficulty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 检查难度调整失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 调整难度
        /// </summary>
        /// <param name="newDifficulty">新难度等级</param>
        public void AdjustDifficulty(DifficultyLevel newDifficulty)
        {
            try
            {
                var oldDifficulty = _currentDifficulty;
                _currentDifficulty = newDifficulty;
                _lastAdjustment = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 难度调整: {oldDifficulty} -> {newDifficulty}");

                // 通知AI系统应用新的难度参数
                ApplyDifficultyParameters();

                // 清空近期表现记录，重新开始评估
                _recentPerformanceScores.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 调整难度失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用难度参数
        /// </summary>
        private void ApplyDifficultyParameters()
        {
            try
            {
                var parameters = GetCurrentDifficultyParameters();
                
                // 这里可以通知其他系统应用新的难度参数
                // 例如：资源系统、AI系统等
                
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 应用难度参数: AI资源倍数={parameters.AIResourceMultiplier}, AI智力加成={parameters.AIIntelligenceBonus}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 应用难度参数失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前难度参数
        /// </summary>
        /// <returns>难度参数</returns>
        public DifficultyParameters GetCurrentDifficultyParameters()
        {
            return _difficultySettings.ContainsKey(_currentDifficulty) 
                ? _difficultySettings[_currentDifficulty] 
                : _difficultySettings[DifficultyLevel.Normal];
        }

        /// <summary>
        /// 手动设置难度
        /// </summary>
        /// <param name="difficulty">难度等级</param>
        public void SetDifficulty(DifficultyLevel difficulty)
        {
            AdjustDifficulty(difficulty);
        }

        /// <summary>
        /// 记录战斗结果
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="victory">是否胜利</param>
        /// <param name="battleDuration">战斗时长（秒）</param>
        /// <param name="tacticalRating">战术评分 (0-1)</param>
        public void RecordBattleResult(int playerId, bool victory, float battleDuration, float tacticalRating)
        {
            try
            {
                if (!_playerMetrics.ContainsKey(playerId))
                {
                    _playerMetrics[playerId] = new PerformanceMetrics();
                }

                var metrics = _playerMetrics[playerId];
                
                // 更新胜率（使用滑动平均）
                metrics.WinRate = metrics.WinRate * 0.9f + (victory ? 1.0f : 0.0f) * 0.1f;
                
                // 更新战术成功率
                metrics.TacticalSuccess = metrics.TacticalSuccess * 0.9f + tacticalRating * 0.1f;
                
                // 更新平均游戏时长
                metrics.AverageGameDuration = metrics.AverageGameDuration * 0.9f + battleDuration * 0.1f;

                UpdatePlayerMetrics(playerId, metrics);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 记录战斗结果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录经济表现
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="resourceEfficiency">资源效率 (0-1)</param>
        /// <param name="expansionRate">扩张速度 (0-1)</param>
        public void RecordEconomicPerformance(int playerId, float resourceEfficiency, float expansionRate)
        {
            try
            {
                if (!_playerMetrics.ContainsKey(playerId))
                {
                    _playerMetrics[playerId] = new PerformanceMetrics();
                }

                var metrics = _playerMetrics[playerId];
                
                // 使用滑动平均更新指标
                metrics.ResourceEfficiency = metrics.ResourceEfficiency * 0.95f + resourceEfficiency * 0.05f;
                metrics.ExpansionRate = metrics.ExpansionRate * 0.95f + expansionRate * 0.05f;

                UpdatePlayerMetrics(playerId, metrics);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 记录经济表现失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录外交表现
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="diplomaticSuccess">外交成功率 (0-1)</param>
        public void RecordDiplomaticPerformance(int playerId, float diplomaticSuccess)
        {
            try
            {
                if (!_playerMetrics.ContainsKey(playerId))
                {
                    _playerMetrics[playerId] = new PerformanceMetrics();
                }

                var metrics = _playerMetrics[playerId];
                metrics.DiplomaticSuccess = metrics.DiplomaticSuccess * 0.9f + diplomaticSuccess * 0.1f;

                UpdatePlayerMetrics(playerId, metrics);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficulty] 记录外交表现失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取难度调整建议
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>建议信息</returns>
        public string GetDifficultyRecommendation(int playerId)
        {
            if (!_playerMetrics.ContainsKey(playerId))
            {
                return "数据不足，无法提供建议";
            }

            var metrics = _playerMetrics[playerId];
            float score = CalculatePerformanceScore(metrics);

            if (score > 80)
            {
                return "表现优秀！建议提高难度以获得更大挑战";
            }
            else if (score > 60)
            {
                return "表现良好，当前难度适合";
            }
            else if (score > 40)
            {
                return "表现一般，可以考虑降低难度";
            }
            else
            {
                return "建议降低难度，专注于学习游戏机制";
            }
        }

        /// <summary>
        /// 获取系统状态
        /// </summary>
        /// <returns>状态信息</returns>
        public string GetSystemStatus()
        {
            var status = $"当前难度: {_currentDifficulty}\n";
            status += $"上次调整: {_lastAdjustment:HH:mm:ss}\n";
            status += $"表现样本数: {_recentPerformanceScores.Count}\n";
            
            if (_recentPerformanceScores.Count > 0)
            {
                status += $"平均表现: {_recentPerformanceScores.Average():F1}\n";
            }

            var parameters = GetCurrentDifficultyParameters();
            status += $"AI资源倍数: {parameters.AIResourceMultiplier:F2}\n";
            status += $"AI智力加成: {parameters.AIIntelligenceBonus:F1}";

            return status;
        }

        /// <summary>
        /// 重置系统
        /// </summary>
        public void Reset()
        {
            _playerMetrics.Clear();
            _recentPerformanceScores.Clear();
            _currentDifficulty = DifficultyLevel.Normal;
            _lastAdjustment = DateTime.Now;
        }

        /// <summary>
        /// 启用/禁用自动难度调整
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetAutoDifficultyEnabled(bool enabled)
        {
            if (!enabled)
            {
                // 禁用时清空表现记录，避免意外调整
                _recentPerformanceScores.Clear();
            }
        }
    }
}