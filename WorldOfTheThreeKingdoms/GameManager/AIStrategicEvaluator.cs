using System;
using System.Collections.Generic;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    public static class AIStrategicEvaluator
    {
        public static StrategicDecisionWeights CalculateFactionWeights(Faction faction)
        {
            var weights = new StrategicDecisionWeights();
            var args = Session.Current.Scenario.GameCommonData;
            var parameters = Session.Current.Scenario.Parameters;

            if (faction == null || faction.Leader == null) return weights;

            ApplyStrategyTendency(faction.Leader.StrategyTendency, weights, parameters);
            
            // Using the new logic provided
            int valuation = GetValuationOnGovernment(faction.Leader);
            ApplyHanRoomValuation(valuation, weights, parameters);

            CalculateArmyQualityScore(faction, weights, parameters);

            return weights;
        }

        private static void ApplyStrategyTendency(PersonStrategyTendency tendency, StrategicDecisionWeights weights, Parameters parameters)
        {
            // Mapping Game Enum to Weights based on User's concept
            // Unification -> Aggressive
            // Separatist -> Defensive/Balanced
            // SelfPreservation -> Defensive
            
            switch (tendency)
            {
                case PersonStrategyTendency.统一全国: // Unification
                    weights.Aggression = parameters.AIWeightUnificationAggression;
                    weights.ExpansionDistance = 1.0f;
                    weights.DiplomacyBias = 0.5f;
                    break;
                case PersonStrategyTendency.统一地区: // Regional Unification
                    weights.Aggression = 1.2f;
                    weights.ExpansionDistance = 0.8f;
                    weights.DiplomacyBias = 0.6f;
                    break;
                case PersonStrategyTendency.统一州: // State Unification
                    weights.Aggression = 1.0f;
                    weights.ExpansionDistance = 0.5f;
                    weights.DiplomacyBias = 0.7f;
                    break;
                case PersonStrategyTendency.维持现状: // SelfPreservation
                    weights.Aggression = 0.8f;
                    weights.Defense = parameters.AIWeightSelfPreservationDefense;
                    weights.RiskTolerance = 0.8f;
                    weights.DiplomacyBias = 0.9f;
                    break;
            }
        }

        private static void ApplyHanRoomValuation(int valuation, StrategicDecisionWeights weights, Parameters parameters)
        {
            // 旧逻辑：忠臣倾向结盟，汉贼倾向破坏
            // 新逻辑补充：
            
            if (valuation >= 70) // 忠臣
            {
                // 忠臣并非软弱，为了维护汉室，他们需要具备一定的远征能力(勤王)
                weights.DiplomacyBias += 0.4f; // 依然容易结盟
                weights.ExpansionDistance *= 1.2f; // 为了救皇帝，愿意跑得更远！
            }
            else if (valuation < 30) // 汉贼
            {
                weights.Aggression += 0.3f; // 侵略性更强
                weights.DiplomacyBias -= 0.3f; // 难以结盟
            }
        }

        private static void CalculateArmyQualityScore(Faction faction, StrategicDecisionWeights weights, Parameters parameters)
        {
            // Calculate overall army quality based on troop experience and kind grade
            if (faction.Troops.Count == 0) return;

            float totalScore = 0;
            int eliteCount = 0;

            foreach (Troop troop in faction.Troops)
            {
                int grade = GetTroopGrade(troop);
                
                // Using troop.Army.Experience as verified
                float experience = 0;
                if (troop.Army != null)
                {
                    experience = troop.Army.Experience;
                }

                if (grade >= parameters.AIWeightEliteTroopGradeThreshold)
                {
                    eliteCount++;
                }
                
                totalScore += (grade * 100) + (experience * parameters.AIWeightExpToPowerRatio);
            }

            float averageScore = totalScore / faction.Troops.Count;
            
            // Adjust weights based on army quality
            if (averageScore > 500) // High quality
            {
                weights.Aggression += 0.2f;
                weights.RiskTolerance += 0.1f;
            }
            else if (averageScore < 200) // Low quality
            {
                 weights.Defense += 0.2f;
                 weights.RiskTolerance -= 0.1f;
            }
        }

        private static int GetTroopGrade(Troop troop)
        {
            if (troop.Army == null || troop.Army.Kind == null) return 1;
            
            // Estimate grade based on CreateCost or other properties since Grade is missing
            // This is a proxy logic
            int cost = troop.Army.Kind.CreateCost;
            if (cost > 2000) return 3; // Elite
            if (cost > 1000) return 2; // Veteran
            return 1; // Regular
        }
        
        private static int GetValuationOnGovernment(Person p)
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
    }
}
