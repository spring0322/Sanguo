using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;
using GameManager; // Session and Utility are here
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI 智能防御与撤退系统
    /// </summary>
    public class AICoordinatedDefenseSystem
    {
        public static AICoordinatedDefenseSystem Instance { get; private set; } = new AICoordinatedDefenseSystem();

        private AICoordinatedDefenseSystem() { }

        public void CheckActiveDefense(Architecture architecture)
        {
            if (architecture == null) return;
            if (architecture.BelongedFaction == null || global::GameManager.Session.Current.Scenario.IsPlayer(architecture.BelongedFaction)) return;

            TroopList hostileTroops = architecture.GetHostileTroopsInView();
            if (hostileTroops.Count == 0) return;
            if (architecture.Persons.Count == 0 || architecture.Population < 3000) return;

            Troop targetTroop = null;
            foreach (Troop t in hostileTroops)
            {
                if (t.TargetArchitecture == architecture)
                {
                    targetTroop = t;
                    break;
                }
            }
            if (targetTroop == null)
            {
                foreach (Troop t in hostileTroops)
                {
                    if (targetTroop == null || t.FightingForce > targetTroop.FightingForce)
                    {
                        targetTroop = t;
                    }
                }
            }

            if (targetTroop == null) return;

            bool panicMode = architecture.Endurance < architecture.EnduranceCeiling * 0.3;
            bool enemyHasSiege = false;
            foreach (Troop t in hostileTroops)
            {
                if (t.Army != null && t.Army.Kind != null && t.Army.Kind.Type == MilitaryType.器械)
                {
                    enemyHasSiege = true;
                    break;
                }
            }

            bool shouldAttack = panicMode || enemyHasSiege || architecture.Endurance < 1000;
            if (!shouldAttack) return;

            Person bestPerson = null;
            foreach (Person p in architecture.Persons)
            {
                if (p.WorkKind != ArchitectureWorkKind.无 || p.Status != PersonStatus.Normal || p.Braveness < 60) continue;
                if (bestPerson == null || p.Braveness > bestPerson.Braveness)
                {
                    bestPerson = p;
                }
            }

            if (bestPerson == null) return;

            int quantity = Math.Min(architecture.Population, bestPerson.Command * 100);
            if (quantity < 2000) return;

            Military bestMilitary = null;
            foreach (Military m in architecture.Militaries)
            {
                if (m.Kind.Type == MilitaryType.步兵 || m.Kind.Type == MilitaryType.骑兵)
                {
                    if (bestMilitary == null || m.Quantity > bestMilitary.Quantity)
                    {
                        bestMilitary = m;
                    }
                }
            }

            if (bestMilitary == null && architecture.Militaries.Count > 0)
            {
                bestMilitary = architecture.Militaries[0] as Military;
            }

            if (bestMilitary == null) return;

            GameObjectList persons = new GameObjectList();
            persons.Add(bestPerson);
            Point? spawnPoint = architecture.GetRandomStartingPosition(bestMilitary);
            if (spawnPoint == null) spawnPoint = architecture.Position;

            int foodToTake = Math.Min(architecture.Food, 20000);

            // 🔥 重构：使用新的Kind+Mission系统
            // 日期：2026-03-09
            Legion defenseLegion = architecture.GetOrCreateDefensiveLegion();
            Troop activeDefenseTroop = architecture.CreateTroop(persons, bestPerson, bestMilitary, foodToTake, spawnPoint.Value, assignedLegion: defenseLegion);
            if (activeDefenseTroop != null)
            {
                activeDefenseTroop.TargetTroop = targetTroop;
                activeDefenseTroop.Operation = TroopAction.Attack;
                System.Diagnostics.Debug.WriteLine($"[CheckActiveDefense] 城池 {architecture.Name} 遭到攻击，派出 {bestPerson.Name} 殊死一搏！");
            }
        }

        public void CheckEmergencyEvacuation(Architecture architecture)
        {
            if (architecture == null) return;
            if (architecture.BelongedFaction == null || global::GameManager.Session.Current.Scenario.IsPlayer(architecture.BelongedFaction)) return;

            TroopList hostileTroops = architecture.GetHostileTroopsInView();
            if (hostileTroops.Count == 0) return;

            int enemyForce = 0;
            foreach (Troop t in hostileTroops) enemyForce += t.Quantity;
            
            int myForce = 0;
            foreach (Military m in architecture.Militaries) myForce += m.Quantity;
            
            foreach (Troop t in global::GameManager.Session.Current.Scenario.Troops) 
            {
                if (t.BelongedFaction == architecture.BelongedFaction && t.Position == architecture.Position) 
                {
                    myForce += t.Quantity;
                }
            }

            bool wallBroken = architecture.Endurance < 500 || architecture.Endurance < architecture.EnduranceCeiling * 0.1;
            bool overwhelmed = enemyForce > myForce * 5;

            if (wallBroken && overwhelmed)
            {
                List<Person> evacuees = new List<Person>();
                foreach (Person p in architecture.Persons)
                {
                    if (p.Status == PersonStatus.Normal) evacuees.Add(p);
                }

                if (evacuees.Count > 0)
                {
                    Architecture retreatTarget = null;
                    float minDistance = float.MaxValue;

                    foreach (GameObject obj in architecture.BelongedFaction.Architectures)
                    {
                        Architecture a = (obj is Architecture ? (Architecture)obj : null);
                        if (a != null && a != architecture)
                        {
                            float d = CalculateDistance(architecture.Position, a.Position);
                            if (d < minDistance)
                            {
                                minDistance = d;
                                retreatTarget = a;
                            }
                        }
                    }

                    if (retreatTarget != null)
                    {
                        foreach (Person p in evacuees)
                        {
                            p.MoveToArchitecture(retreatTarget); 
                        }
                         System.Diagnostics.Debug.WriteLine($"[CheckEmergencyEvacuation] 城池 {architecture.Name} 即将沦陷，全员撤退至 {retreatTarget.Name}");
                    }
                }
            }
        }
        private float CalculateDistance(Point p1, Point p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }
}

