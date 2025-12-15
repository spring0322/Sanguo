using System;
using System.Collections.Generic;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    public class AIExpansionLogic
    {
        /// <summary>
        /// 计算对目标城市的进攻评分（分数越高，越想打）
        /// </summary>
        public float CalculateAttackScore(Faction attacker, Architecture targetCity)
        {
            if (attacker == null || targetCity == null) return -100f;

            // 0. 基础数据准备
            var common = Session.Current.Scenario.GameCommonData;
            StrategicDecisionWeights weights = AIStrategicEvaluator.CalculateFactionWeights(attacker);
            
            float myPower = GetFactionFightingForce(attacker); // Use helper since TotalFightingForce might not exist or be sufficient
            float targetPower = GetArchitectureDefensivePower(targetCity); // Use helper for Defensive Power
            
            // 1. 基础战力评分 (战力比越高，分越高)
            // 引入 RiskTolerance (风险承受度)：胆子大的，看敌人的战力就会觉得"偏低"
            float powerRatio = (myPower * weights.RiskTolerance) / Math.Max(1, targetPower);
            
            // 如果战力差距过大(比如我方只有对方的 0.5)，直接返回负分，除非是极度激进
            if (powerRatio < 0.6f) return -100f; 

            float baseScore = powerRatio * 10.0f;

            // 2. 距离评分 (距离越远，分越低)
            int distance = GetDistance(attacker.Capital, targetCity);
            float distancePenalty = distance / Math.Max(0.1f, weights.ExpansionDistance);
            
            // 3. --- 新增：汉室与外交倾向修正 ---
            float ideologyScore = CalculateIdeologyScore(attacker, targetCity);

            // 4. --- 新增：献帝相关修正 (勤王 vs 挟天子) ---
            float emperorScore = CalculateEmperorScore(attacker, targetCity, powerRatio);

            // 5. 最终汇总
            // 战力基础分 - 距离惩罚 + 意识形态加成 + 献帝加成
            float totalScore = baseScore - distancePenalty + ideologyScore + emperorScore;

            return totalScore;
        }

        /// <summary>
        /// 计算意识形态/阵营冲突评分
        /// </summary>
        private float CalculateIdeologyScore(Faction attacker, Architecture targetCity)
        {
            if (targetCity.BelongedFaction == null) return 50f; // 空城优先打

            Faction defender = targetCity.BelongedFaction;
            
            // 获取双方的汉室重视度 (0-100)
            int myValuation = GetValuationOnGovernment(attacker.Leader);
            int targetValuation = GetValuationOnGovernment(defender.Leader);

            float scoreModifier = 0f;

            // 情况A：我是忠臣 (重视度 > 70)
            if (myValuation >= 70)
            {
                if (targetValuation < 30) 
                {
                    // 对方是汉贼 -> 替天行道，极大加分
                    scoreModifier += 30f; 
                }
                else if (targetValuation >= 70)
                {
                    // 对方也是忠臣 -> 同室操戈，减分 (除非必须扩张)
                    scoreModifier -= 20f;
                }
            }
            // 情况B：我是汉贼/野心家 (重视度 < 30)
            else if (myValuation < 30)
            {
                if (targetValuation >= 70)
                {
                    // 对方是忠臣 -> 看不顺眼，加分
                    scoreModifier += 15f;
                }
                // 汉贼之间互咬，没有特殊加成，纯看利益
            }

            return scoreModifier;
        }

        /// <summary>
        /// 计算献帝目标的特殊修正 (勤王/解救/争夺)
        /// </summary>
        private float CalculateEmperorScore(Faction attacker, Architecture targetCity, float powerRatio)
        {
            // 如果皇帝不在这个城市，没分
            if (!HasEmperor(targetCity)) return 0f;

            // 如果自身实力不足，就算想救也没用，不要送死 (硬性判断)
            // 设定：至少要有 1.2 倍的相对优势才敢动皇帝的主意
            if (powerRatio < 1.2f) return -50f; 

            Faction defender = targetCity.BelongedFaction;
            int myValuation = GetValuationOnGovernment(attacker.Leader);
            int targetValuation = GetValuationOnGovernment(defender?.Leader);

            float bonus = 0f;

            // 逻辑分支 1: 勤王/解救 (我是忠臣，皇帝在逆贼手里)
            if (myValuation >= 70 && targetValuation < 40)
            {
                // 优先级极高！这比打普通城池重要得多
                // 只要打得过，这就是首选目标
                bonus += 100f; 
            }
            // 逻辑分支 2: 争夺/挟天子 (我是野心家，皇帝在任何人手里)
            else if (myValuation < 40)
            {
                // 抢夺皇帝以令诸侯
                bonus += 60f;
            }
            // 逻辑分支 3: 正常移驾 (我是忠臣，皇帝在普通诸侯手里)
            else if (myValuation >= 70 && targetValuation >= 40 && targetValuation < 70)
            {
                // 对方不算太坏，但我更适合辅佐皇帝
                bonus += 30f;
            }
            // 逻辑分支 4: 忠臣内战 (我是忠臣，皇帝在另一个大忠臣手里)
            else if (myValuation >= 80 && targetValuation >= 80)
            {
                // 没必要打，皇帝很安全
                bonus -= 50f;
            }

            return bonus;
        }

        // 简化的距离计算
        private int GetDistance(Architecture a, Architecture b) 
        { 
            if (a == null || b == null) return 999;
            // Using GetSimpleDistance as verified
            return Session.Current.Scenario.GetSimpleDistance(a.Position, b.Position); 
        }

        private float GetFactionFightingForce(Faction faction)
        {
            if (faction == null) return 0;
            
            float force = 0;
            // Sum stored force in architectures
            foreach (Architecture arch in faction.Architectures)
            {
                force += arch.TotalStoredForce;
            }
            // Sum troop strength (Quantity * Combativity / 100 approx)
            foreach (Troop troop in faction.Troops)
            {
                 // Using a raw estimation since Combativity scaling might differ
                 force += troop.Quantity * (troop.Combativity / 100.0f);
            }
            return force;
        }

        private float GetArchitectureDefensivePower(Architecture arch)
        {
            if (arch == null) return 0;
            // Sum Endurance + Stored Force + Garrisoned Troops
            float power = arch.Endurance;
            power += arch.TotalStoredForce;
            
            // Check for troops on top of the architecture or close by belonging to the faction
            // For simplicity, we just look at TotalStayForce if available, or just StoredForce.
            // Architecture.cs typically handles TotalStoredForce as defensive reserve.
            // We can also add a base value for the architecture kind / scale
            
            return power;
        }

        private int GetValuationOnGovernment(Person p)
        {
            if (p == null) return 50;
            
            switch (p.ValuationOnGovernment)
            {
                case PersonValuationOnGovernment.无视: return 10;
                case PersonValuationOnGovernment.普通: return 50;
                case PersonValuationOnGovernment.重视: return 90;
                default: return 50;
            }
        }

        private bool HasEmperor(Architecture arch)
        {
            // Check if Person 7000 is in this architecture
            // 7000 is the ID for the Emperor typically
            foreach (Person p in arch.Persons)
            {
                if (p.ID == 7000) return true;
            }
            return false;
        }
    }
}
