using System;
using GameObjects;
using GameManager;

namespace GameManager
{
    /// <summary>
    /// 军师系统使用示例
    /// </summary>
    public static class StrategistManagerExample
    {
        /// <summary>
        /// 示例：在招募界面中使用军师预判
        /// </summary>
        public static void ExampleRecruitmentPrediction(Faction playerFaction, Person targetOfficer)
        {
            // 获取招募预测
            var (bestRecruiter, result) = StrategistManager.PredictRecruitment(playerFaction, targetOfficer);

            // 显示结果给玩家
            Console.WriteLine("=== 军师建议 ===");
            Console.WriteLine($"目标武将: {targetOfficer.Name}");
            
            if (bestRecruiter != null)
            {
                Console.WriteLine($"推荐派遣: {bestRecruiter.Name}");
                Console.WriteLine($"成功率: {result.SuccessRate}%");
            }
            
            Console.WriteLine($"军师评语: {result.Comment}");
            
            if (result.IsImpossible)
            {
                Console.WriteLine("⚠️ 军师认为此事不可能成功！");
            }

            // 获取详细分析
            string analysis = StrategistManager.GetRecruitmentAnalysis(playerFaction, targetOfficer);
            Console.WriteLine("\n" + analysis);
        }

        /// <summary>
        /// 示例：在战斗前使用军师预判
        /// </summary>
        public static void ExampleBattlePrediction(Faction playerFaction, Troop playerTroop, Troop enemyTroop)
        {
            var result = StrategistManager.PredictBattle(playerFaction, playerTroop, enemyTroop);

            Console.WriteLine("=== 战前分析 ===");
            Console.WriteLine($"胜率预测: {result.SuccessRate}%");
            Console.WriteLine($"军师评语: {result.Comment}");

            if (result.IsImpossible)
            {
                Console.WriteLine("⚠️ 军师强烈建议避战！");
            }
            else if (result.SuccessRate >= 80)
            {
                Console.WriteLine("✅ 军师建议立即开战！");
            }
            else if (result.SuccessRate < 40)
            {
                Console.WriteLine("⚠️ 军师建议谨慎考虑！");
            }
        }

        /// <summary>
        /// 示例：在外交界面中使用军师预判
        /// </summary>
        public static void ExampleDiplomacyPrediction(Faction playerFaction, Faction targetFaction, string action)
        {
            var result = StrategistManager.PredictDiplomacy(playerFaction, targetFaction, action);

            Console.WriteLine("=== 外交预测 ===");
            Console.WriteLine($"外交行动: {action}");
            Console.WriteLine($"目标势力: {targetFaction.Name}");
            Console.WriteLine($"成功率: {result.SuccessRate}%");
            Console.WriteLine($"军师评语: {result.Comment}");

            if (result.IsImpossible)
            {
                Console.WriteLine("⚠️ 军师认为此举毫无意义！");
            }
        }

        /// <summary>
        /// 示例：获取军师的综合战略建议
        /// </summary>
        public static void ExampleStrategicAdvice(Faction playerFaction)
        {
            string advice = StrategistManager.GetStrategicAdvice(playerFaction);
            Console.WriteLine(advice);
        }

        /// <summary>
        /// 示例：完整的军师咨询流程
        /// </summary>
        public static void ExampleFullConsultation(Faction playerFaction)
        {
            Console.WriteLine("=== 军师府 ===");
            
            if (playerFaction.Advisor == null)
            {
                Console.WriteLine("主公尚未任命军师，无法提供战略咨询。");
                Console.WriteLine("建议先任命一位智谋出众的武将为军师。");
                return;
            }

            Console.WriteLine($"军师 {playerFaction.Advisor.Name} 恭候主公询问。");
            Console.WriteLine();

            // 1. 综合战略建议
            ExampleStrategicAdvice(playerFaction);
            Console.WriteLine();

            // 2. 如果有敌军威胁，提供战斗建议
            // ExampleBattlePrediction(playerFaction, myTroop, enemyTroop);

            // 3. 如果有招募目标，提供招募建议
            // ExampleRecruitmentPrediction(playerFaction, targetOfficer);

            // 4. 如果有外交机会，提供外交建议
            // ExampleDiplomacyPrediction(playerFaction, targetFaction, "结盟");

            Console.WriteLine("=== 咨询结束 ===");
        }
    }
}