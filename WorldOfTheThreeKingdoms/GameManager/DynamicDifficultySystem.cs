using System;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using global::GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 动态难度系统 - 简化版本
    /// </summary>
    public class DynamicDifficultySystem
    {
        public static DynamicDifficultySystem Instance { get; private set; }
        
        private AIDifficulty _currentDifficulty;
        private float _difficultyMultiplier;
        
        public AIDifficulty CurrentDifficulty => _currentDifficulty;
        public float DifficultyMultiplier => _difficultyMultiplier;
        
        public DynamicDifficultySystem()
        {
            Instance = this;
            _currentDifficulty = AIDifficulty.Normal;
            _difficultyMultiplier = 1.0f;
        }
        
        /// <summary>
        /// 更新难度
        /// </summary>
        public void UpdateDifficulty()
        {
            try
            {
                // 基于玩家表现调整难度
                if (Session.Current?.Scenario?.CurrentPlayer != null)
                {
                    var player = Session.Current.Scenario.CurrentPlayer;
                    
                    // 简单的难度调整逻辑
                    if (player.ArchitectureCount > 10)
                    {
                        _currentDifficulty = AIDifficulty.Hard;
                        _difficultyMultiplier = 1.2f;
                    }
                    else if (player.ArchitectureCount < 3)
                    {
                        _currentDifficulty = AIDifficulty.Easy;
                        _difficultyMultiplier = 0.8f;
                    }
                    else
                    {
                        _currentDifficulty = AIDifficulty.Normal;
                        _difficultyMultiplier = 1.0f;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicDifficultySystem] 更新难度时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置难度
        /// </summary>
        public void SetDifficulty(AIDifficulty difficulty)
        {
            _currentDifficulty = difficulty;
            
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    _difficultyMultiplier = 0.7f;
                    break;
                case AIDifficulty.Normal:
                    _difficultyMultiplier = 1.0f;
                    break;
                case AIDifficulty.Hard:
                    _difficultyMultiplier = 1.3f;
                    break;
                case AIDifficulty.Nightmare:
                    _difficultyMultiplier = 1.5f;
                    break;
                default:
                    _difficultyMultiplier = 1.0f;
                    break;
            }
        }
        
        /// <summary>
        /// 获取AI加成
        /// </summary>
        public float GetAIBonus(string bonusType)
        {
            try
            {
                float baseBonus = 1.0f;
                
                switch (bonusType.ToLower())
                {
                    case "resource":
                        baseBonus = _difficultyMultiplier;
                        break;
                    case "combat":
                        baseBonus = _difficultyMultiplier * 0.8f;
                        break;
                    case "intelligence":
                        baseBonus = _difficultyMultiplier * 1.2f;
                        break;
                    default:
                        baseBonus = _difficultyMultiplier;
                        break;
                }
                
                return Math.Max(0.5f, Math.Min(2.0f, baseBonus));
            }
            catch
            {
                return 1.0f;
            }
        }
    }
}