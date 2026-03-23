using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.FactionDetail;
using Microsoft.Xna.Framework;
using GameManager; // Session is here

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI多方协同攻击系统
    /// </summary>
    public class AICoordinatedAttackSystem
    {
        public static AICoordinatedAttackSystem Instance { get; private set; } = new AICoordinatedAttackSystem();

        private Dictionary<int, DateTime> _lastCoordinationTime = new Dictionary<int, DateTime>();
        private const double COORDINATION_COOLDOWN_SECONDS = 30;

        private AICoordinatedAttackSystem() { }

        public void Update()
        {
            // Clean up expired entries
            var keysToRemove = new List<int>();
            foreach (var kvp in _lastCoordinationTime)
            {
                if ((DateTime.Now - kvp.Value).TotalMinutes > 10)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _lastCoordinationTime.Remove(key);
            }
        }

        public bool TryLaunchCoordinatedAttack(Faction faction)
        {
            // Session is in GameManager namespace
            if (global::GameManager.Session.Current.Scenario.Date.Day % 10 != 0) return false;

            // 1. 冷却检查
            if (_lastCoordinationTime.TryGetValue(faction.ID, out DateTime lastTime))
            {
                if ((DateTime.Now - lastTime).TotalSeconds < COORDINATION_COOLDOWN_SECONDS)
                    return false;
            }

            // 2. 寻找潜在目标
            var targets = IdentifyPotentialTargets(faction);
            if (targets.Count == 0) return false;

            foreach (var target in targets)
            {
                if (TryLaunchAttackOnTarget(faction, target))
                {
                    _lastCoordinationTime[faction.ID] = DateTime.Now;
                    return true;
                }
            }

            return false;
        }

        private List<Architecture> IdentifyPotentialTargets(Faction faction)
        {
            var potentialTargets = new List<Architecture>();
            
            foreach (GameObject obj in faction.Architectures)
            {
                var architecture = (obj is Architecture ? (Architecture)obj : null);
                if (architecture == null) continue;

                if (architecture.AILandLinks != null)
                {
                    foreach (GameObject linkObj in architecture.AILandLinks)
                    {
                        Architecture linkedArch = (linkObj is Architecture ? (Architecture)linkObj : null);
                        
                        if (linkedArch != null)
                        {
                            if (linkedArch.BelongedFaction != null && !faction.IsFriendly(linkedArch.BelongedFaction))
                            {
                                if (!potentialTargets.Contains(linkedArch))
                                {
                                    potentialTargets.Add(linkedArch);
                                }
                            }
                        }
                    }
                }
            }

            potentialTargets.Sort((a, b) =>
            {
                float scoreA = CalculateTargetScore(faction, a);
                float scoreB = CalculateTargetScore(faction, b);
                return scoreB.CompareTo(scoreA); // 降序
            });

            return potentialTargets.Cast<Architecture>().Take(3).ToList(); 
        }

        private float CalculateTargetScore(Faction faction, Architecture target)
        {
            float score = 0;
            score += target.Population / 1000f;
            score += target.Fund / 1000f;
            score += target.Food / 2000f;
            
            if (target.AILandLinks != null) score += target.AILandLinks.Count * 10;

            return score;
        }

        private bool TryLaunchAttackOnTarget(Faction faction, Architecture target)
        {
            // Implementation placeholder
            return false;
        }

        public Point GetEntityPosition(object obj)
        {
            var arch = (obj is Architecture ? (Architecture)obj : null);
            if (arch != null) return arch.Position;
            
            var troop = (obj is Troop ? (Troop)obj : null);
            if (troop != null) return troop.Position;
            
            return Point.Zero;
        }
    }
}

