using System;
using Microsoft.Xna.Framework;
using GameObjects;
using GameManager;

namespace GameObjects
{
    public static class GameScenarioExtensions
    {
        /// <summary>
        /// 扩展方法：计算移动消耗
        /// </summary>
        public static int GetMoveCost(this GameScenario scenario, Point from, Point to, Troop troop)
        {
            if (scenario == null || troop == null) return -1;

            var calculator = new MovementCalculator();
            var unit = Unit.FromTroop(troop);
            return calculator.GetMoveCost(from, to, unit);
        }

        public static bool IsPositionFreeOfTroop(this GameScenario scenario, Point position)
        {
            return scenario.IsPositionEmpty(position);
        }
    }
}
