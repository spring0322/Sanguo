using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI.Helper;

namespace GameObjects.AI
{
    public static class TroopAIExecutor
    {
        public static void ExecuteTurn(Troop troop)
        {
            if (troop == null || troop.OperationDone || troop.Destroyed) return;

            // 1. 后勤逻辑
            if (AILogisticsHandler.IsTransportTroop(troop))
            {
                // 【绝对修正】这里强制只传 1 个参数！
                // 之前报错 "没有采用 3 个参数的重载" 就是因为这里还在传3个
                AILogisticsHandler.ExecuteTransportAI(troop);
                return;
            }

            // 2. 工程逻辑
            if (AIConstructionPlanner.IsConstructionTroop(troop))
            {
                // 【绝对修正】强制只传 1 个参数！
                AIConstructionPlanner.ExecuteConstructionAI(troop);
                return;
            }

            // 3. 战斗逻辑
            DoCombatAI(troop);
        }

        private static void DoCombatAI(Troop troop)
        {
            var scenario = AIHelper.GetScenario();
            if (scenario == null) return;

            // 【绝对修正】使用 Helper 获取 List，解决 "无法将 GameObjectList 转换为 List"
            List<Troop> allTroops = AIHelper.GetTroopsFromList(scenario.Troops);

            // 筛选敌人
            List<Troop> enemies = new List<Troop>();
            foreach (Troop t in allTroops)
            {
                if (t.BelongedFaction != null &&
                    troop.BelongedFaction != null &&
                    !t.BelongedFaction.IsFriendly(troop.BelongedFaction))
                {
                    enemies.Add(t);
                }
            }

            // 找最近敌人
            Troop target = null;
            int minDist = 9999;
            foreach (var e in enemies)
            {
                int d = AIHelper.GetManhattanDistance(troop.Position, e.Position);
                if (d < minDist)
                {
                    minDist = d;
                    target = e;
                }
            }

            if (target != null)
            {
                if (AIHelper.CanAttackTarget(troop, target))
                {
                    troop.Destination = target.Position;
                    troop.Will = TroopWill.移动;
                }
                else
                {
                    troop.Destination = target.Position;
                    troop.Will = TroopWill.移动;
                }
            }
        }
    }
}