using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameManager; // For FactionProfile, ResourceSnapshot, FactionNeighbors, StrategicStance, BattleSimulationResult

namespace GameManager
{
    /// <summary>
    /// 战略大脑 - 负责具体的战略决策逻辑
    /// </summary>
    public class StrategicBrain
    {
        public int FactionId { get; set; }

        public StrategicBrain(int factionId)
        {
            FactionId = factionId;
        }

        public void Update() { }
        public void Analyze() { }

        public StrategicStance DetermineStance(FactionProfile profile, ResourceSnapshot snapshot, FactionNeighbors neighbors)
        {
            // 简单实现：基于优先级的决策树
            
            // 1. 生存第一
            if (snapshot.EconomicHealth < 0.2f || snapshot.ThreatLevel > 0.8f)
            {
                return StrategicStance.Crisis;
            }

            // 2. 防御
            if (snapshot.ThreatLevel > 0.6f && snapshot.AverageFatigue > 50)
            {
                return StrategicStance.Defense;
            }

            // 3. 扩张
            if (profile.RulerAggression > 0.6f && snapshot.OpportunityLevel > 0.4f)
            {
                return StrategicStance.Expansion;
            }

            // 4. 机会主义
            if (neighbors.HasVulnerableTarget && profile.AdvisorWisdom > 0.7f)
            {
                return StrategicStance.Opportunistic;
            }

            // 5. 休养
            if (snapshot.AverageFatigue > 60 || snapshot.EconomicHealth < 0.5f)
            {
                return StrategicStance.Stabilization;
            }

            return StrategicStance.Idle;
        }

        public string GetStanceReasoning(FactionProfile profile, ResourceSnapshot snapshot, FactionNeighbors neighbors, StrategicStance stance)
        {
            // 返回决策理由
            switch (stance)
            {
                case StrategicStance.Crisis:
                    return $"经济危急({snapshot.EconomicHealth:P0})或威胁过高({snapshot.ThreatLevel:P0})";
                case StrategicStance.Defense:
                    return $"周边威胁较大({snapshot.ThreatLevel:P0})，转为防御态势";
                case StrategicStance.Expansion:
                    return $"君主进取({profile.RulerAggression:P0})且有扩张机会({snapshot.OpportunityLevel:P0})";
                case StrategicStance.Opportunistic:
                    return $"发现脆弱目标且军师建议把握机会";
                case StrategicStance.Stabilization:
                    return $"部队疲劳({snapshot.AverageFatigue:F0})或经济需要恢复";
                case StrategicStance.Idle:
                default:
                    return "当前局势平稳，维持现状";
            }
        }

        public BattleSimulationResult SimulateBattle(Faction attacker, Faction defender)
        {
            var result = new BattleSimulationResult();
            
            if (attacker == null || defender == null)
            {
                result.BattleLog = "无效的战斗参数";
                return result;
            }

            float attackerPower = attacker.TotalMilitaryPopulation;
            float defenderPower = defender.TotalMilitaryPopulation;
            
            // 简单的力量对比模拟
            float ratio = attackerPower / Math.Max(1.0f, defenderPower);
            
            result.PowerRatio = ratio;
            result.AttackerWins = ratio > 1.1f; // 1.1倍以上胜率较高
            result.Winner = result.AttackerWins ? attacker : defender;
            result.Loser = result.AttackerWins ? defender : attacker;
            
            // 估算伤亡率
            result.CasualtyRate = 1.0f / (ratio + 0.5f) * 30.0f; // 简易公式
            if (result.CasualtyRate > 50) result.CasualtyRate = 50;

            result.BattleLog = $"双方兵力比 {ratio:F2}:1，{(result.AttackerWins ? "攻击方" : "防守方")} 占据优势。";

            return result;
        }

        public float EvaluateAttackSuccessRate(Faction attacker, Faction defender)
        {
             if (attacker == null || defender == null) return 0f;

             float attackerPower = attacker.TotalMilitaryPopulation;
             float defenderPower = defender.TotalMilitaryPopulation;
             
             // 简单的胜率映射
             float ratio = attackerPower / Math.Max(1.0f, defenderPower);
             
             if (ratio < 0.5f) return 0.1f;
             if (ratio < 0.8f) return 0.3f;
             if (ratio < 1.2f) return 0.5f;
             if (ratio < 2.0f) return 0.8f;
             return 0.95f;
        }
    }
}
