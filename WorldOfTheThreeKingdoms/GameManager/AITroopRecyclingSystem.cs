using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;
using GameManager; // Session is here

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI 紧急裁军系统
    /// </summary>
    public class AITroopRecyclingSystem
    {
        public static AITroopRecyclingSystem Instance { get; private set; } = new AITroopRecyclingSystem();

        private AITroopRecyclingSystem() { }

        public void RecycleWeakTroopsForManpower(Architecture architecture)
        {
            if (architecture == null) return;

            if (architecture.BelongedFaction != null && global::GameManager.Session.Current.Scenario.IsPlayer(architecture.BelongedFaction)) return;

            if (architecture.Population > 15000) return;

            if (architecture.HasHostileTroopsInView()) return;

            bool isStrategicPoint = architecture.IsImportant || architecture.FrontLine || (architecture.BelongedFaction != null && architecture == architecture.BelongedFaction.Capital);
            if (isStrategicPoint && architecture.Population > 8000) return;

            List<Troop> candidates = new List<Troop>();
            foreach (Troop t in global::GameManager.Session.Current.Scenario.Troops)
            {
                if (t.StartingArchitecture == architecture && t.Position == architecture.Position && !t.Destroyed)
                {
                    candidates.Add(t);
                }
            }

            if (candidates.Count == 0) return;

            Troop bestVictim = null;
            int lowestScore = int.MaxValue;

            foreach (Troop t in candidates)
            {
                if (t.Action != TroopAction.Stop) continue;

                if (t.IsTransport) continue;

                bool isElite = t.FightingForce > 5000 ||
                               (t.Leader != null && (t.Leader.Strength > 75 || t.Leader.Command > 75 || t.Leader.Merit > 1500));

                bool isHighValue = (t.Army != null && (t.Army.Experience >= 300 || t.Army.Kind.CreateCost >= 500)) ||
                                   (t.Leader != null && t.Leader.Merit >= 2000);

                if (isElite || isHighValue) continue;

                bool isTrashUnit = t.Army != null &&
                                   t.Army.Kind.CreateCost < 400 &&
                                   t.Army.Experience < 200 &&
                                   t.Army.Kind.RecruitLimit == 0;

                if (!isTrashUnit) continue;

                int score = t.FightingForce;
                if (score < lowestScore)
                {
                    lowestScore = score;
                    bestVictim = t;
                }
            }

            if (bestVictim != null)
            {
                RecycleTroop(architecture, bestVictim);
            }
        }

        private void RecycleTroop(Architecture architecture, Troop victim)
        {
            try 
            {
                int recoveredPopulation = victim.Quantity;
                architecture.Population += recoveredPopulation;

                if (victim.Army != null)
                {
                    int recoveredFund = (int)(victim.Army.Kind.CreateCost * 0.3);
                    architecture.Fund += recoveredFund;
                }

                if (victim.Food > 0) 
                {
                    architecture.Food += victim.Food;
                }
                
                if (victim.Leader != null)
                {
                    victim.Leader.Status = PersonStatus.Normal;
                    victim.Leader.LocationArchitecture = architecture;
                }
                foreach (Person p in victim.Persons)
                {
                    if (p != victim.Leader)
                    {
                        p.Status = PersonStatus.Normal;
                        p.LocationArchitecture = architecture;
                    }
                }

                global::GameManager.Session.Current.Scenario.Troops.RemoveTroop(victim);
                
                System.Diagnostics.Debug.WriteLine($"[RecycleWeakTroops] 城市 {architecture.Name} 人口不足，解散弱兵 {victim.DisplayName} (+{recoveredPopulation} 人口)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecycleTroop] 裁军发生异常: {ex.Message}");
            }
        }
    }
}
