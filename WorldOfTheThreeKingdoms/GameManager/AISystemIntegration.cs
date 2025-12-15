// ================================================================
// AI系统完整集成包 - 整合到现有项目
// 文件位置: WorldOfTheThreeKingdoms/GameManager/AISystemIntegration.cs
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameObjects.PersonDetail;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    // ================================================================
    // 1. AI决策管理器增强版 - 协调不同的AI行为模式和决策逻辑
    // ================================================================
    
    /// <summary>
    /// AI决策管理器增强版 - 协调不同的AI行为模式和决策逻辑
    /// </summary>
    public class EnhancedAIDecisionManager
    {
        public static EnhancedAIDecisionManager Instance { get; private set; }

        // AI行为模式枚举
        public enum AIBehaviorMode
        {
            Aggressive,     // 攻击性
            Defensive,      // 防御性
            Balanced,       // 平衡型
            Opportunistic,  // 机会主义
            Cautious        // 谨慎型
        }

        // AI决策权重配置
        public class AIDecisionWeights
        {
            public float DistanceToGoal { get; set; } = 1.0f;
            public float ThreatAvoidance { get; set; } = 1.0f;
            public float TerrainAdvantage { get; set; } = 0.5f;
            public float UnknownAreaPenalty { get; set; } = 0.3f;
            public float EnemyProximity { get; set; } = 0.8f;
            public float AllySupport { get; set; } = 0.6f;
        }

        private Dictionary<AIBehaviorMode, AIDecisionWeights> _behaviorWeights;
        
        public EnhancedAIDecisionManager()
        {
            Instance = this;
            _behaviorWeights = new Dictionary<AIBehaviorMode, AIDecisionWeights>();
            InitializeBehaviorWeights();
        }

        private void InitializeBehaviorWeights()
        {
            // Initialize default weights for each behavior mode
            _behaviorWeights[AIBehaviorMode.Aggressive] = new AIDecisionWeights { ThreatAvoidance = 0.3f, EnemyProximity = 1.5f };
            _behaviorWeights[AIBehaviorMode.Defensive] = new AIDecisionWeights { ThreatAvoidance = 1.5f, AllySupport = 1.0f };
            _behaviorWeights[AIBehaviorMode.Balanced] = new AIDecisionWeights();
            _behaviorWeights[AIBehaviorMode.Opportunistic] = new AIDecisionWeights { UnknownAreaPenalty = 0.1f };
            _behaviorWeights[AIBehaviorMode.Cautious] = new AIDecisionWeights { ThreatAvoidance = 1.3f, UnknownAreaPenalty = 0.5f };
        }

        public AIDecisionWeights GetWeights(AIBehaviorMode mode)
        {
            return _behaviorWeights.ContainsKey(mode) ? _behaviorWeights[mode] : new AIDecisionWeights();
        }
    }
}