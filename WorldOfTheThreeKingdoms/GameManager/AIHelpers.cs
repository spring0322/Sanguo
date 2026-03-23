using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 战斗日志生成器
    /// </summary>
    public static class BattleLogGenerator
    {
        public static string GenerateFieldBattleLog(Faction attacker, Faction defender, string location, int turn)
        {
            if (attacker == null || defender == null) return "未知战斗";
            return $"{turn}年，{attacker.Name} 在 {location} 对 {defender.Name} 发起了进攻。";
        }
    }

    /// <summary>
    /// 确定性随机数生成器 (用于同步或回放)
    /// </summary>
    public static class DeterministicRNG
    {
        private static Random _random = new Random();
        private static int _turn = 0;

        public static void SetSeed(int seed)
        {
            _random = new Random(seed);
        }

        public static int Next(int max)
        {
            return _random.Next(max);
        }

        public static int Next(int min, int max)
        {
            return _random.Next(min, max);
        }

        public static void SetCurrentTurn(int turn)
        {
            _turn = turn;
        }

        public static int GetCurrentTurn()
        {
            // 如果有全局的游戏时间管理，应该从那里获取，这里作为fallback或mock
            if (Session.Current?.Scenario != null)
            {
                // 假设Scenario有Date或Turn属性，这里暂时用Session的Scenario时间
                // 注意：GameScenario日期转换逻辑可能不同，这里仅作示意
                return Session.Current.Scenario.Date.Year; 
            }
            return _turn > 0 ? _turn : 190; // Default 190年
        }
    }
}
