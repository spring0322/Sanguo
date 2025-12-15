using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI.Helper;

namespace GameObjects.AI
{
    public static class AILogisticsHandler
    {
        public static void AutoCreateTransportTroops(Faction faction) { }

        public static bool IsTransportTroop(Troop troop)
        {
            if (troop.Army == null) return false;
            // 假设运输队ID是29
            return troop.Army.KindID == 29;
        }

        // 【关键】定义只接收 1 个参数。之前报错就是因为这里是1个，调用方传了3个。
        public static void ExecuteTransportAI(Troop troop)
        {
            if (troop.BelongedFaction == null) return;

            // 内部获取列表，不再依赖外部传参
            List<Architecture> myCities = AIHelper.GetArchitecturesFromList(troop.BelongedFaction.Architectures);

            if (CheckEnterCity(troop, myCities)) return;

            Architecture target = GetNearestCity(troop, myCities);

            if (target != null && target.Position != troop.Position)
            {
                troop.Destination = target.Position;
                troop.Will = TroopWill.移动;
            }
        }

        private static bool CheckEnterCity(Troop me, List<Architecture> cities)
        {
            foreach (var city in cities)
            {
                if (me.Position == city.Position)
                {
                    var scenario = AIHelper.GetScenario();
                    if (scenario != null)
                    {
                        me.BelongedFaction.RemoveTroop(me);
                        scenario.Troops.Remove(me);
                    }
                    return true;
                }
            }
            return false;
        }

        private static Architecture GetNearestCity(Troop me, List<Architecture> cities)
        {
            Architecture nearest = null;
            float maxScore = -9999f;

            foreach (var city in cities)
            {
                if (city.Position == me.Position) continue;

                int dist = AIHelper.GetManhattanDistance(me.Position, city.Position);

                // 【修复】手动加法，解决 "GameObjectList 未包含 Sum" 报错
                float currentRes = (float)city.Food + (float)city.Fund;
                float maxRes = (float)city.FoodCeiling + (float)city.FundCeiling;

                float score = (maxRes - currentRes) - (dist * 1000);

                if (score > maxScore)
                {
                    maxScore = score;
                    nearest = city;
                }
            }
            return nearest;
        }
    }
}