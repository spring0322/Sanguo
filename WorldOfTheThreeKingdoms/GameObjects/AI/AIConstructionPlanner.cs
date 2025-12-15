using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.AI.Helper;

namespace GameObjects.AI
{
    public static class AIConstructionPlanner
    {
        private static readonly Dictionary<int, int> BuildMap = new Dictionary<int, int>
        {
            { 601, 600 },
            { 621, 620 }
        };

        public static bool IsConstructionTroop(Troop troop)
        {
            if (troop.Army == null) return false;
            return BuildMap.ContainsKey(troop.Army.KindID);
        }

        // 【关键】定义只接收 1 个参数
        public static void ExecuteConstructionAI(Troop troop)
        {
            if (ShouldTransformNow(troop))
            {
                ExecuteTransformation(troop);
            }
        }

        public static bool ShouldTransformNow(Troop me)
        {
            if (me.BelongedFaction == null) return false;
            return me.BelongedFaction.Fund > 1000;
        }

        public static void ExecuteTransformation(Troop me)
        {
            if (me.Army == null) return;

            if (BuildMap.TryGetValue(me.Army.KindID, out int archId))
            {
                var scenario = AIHelper.GetScenario();
                if (scenario == null) return;

                var faction = me.BelongedFaction;

                faction.RemoveTroop(me);
                scenario.Troops.Remove(me);

                Architecture arch = new Architecture();
                arch.ID = scenario.Architectures.GetFreeGameObjectID();
                arch.BelongedFaction = faction;

                // arch.KindID = archId; 

                scenario.Architectures.Add(arch);
                faction.AddArchitecture(arch);

                // 【修复】Log 只传一个参数（拼接字符串），解决 Log 重载报错
                AIHelper.Log("部队变身建筑ID: " + archId.ToString());
            }
        }
    }
}