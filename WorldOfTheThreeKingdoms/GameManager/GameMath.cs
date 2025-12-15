using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    public static class GameMath
    {
        public static float CalculateDamage(Troop attacker, Troop defender)
        {
            if (attacker == null || defender == null) return 0f;
            
            // Simplified damage estimation for AI decision making
            // Base Damage based on FightingForce and Morale
            float baseDamage = attacker.FightingForce * (attacker.Morale / 100f) * 0.1f;
            
            // Leader modifier
            float attackerStats = attacker.Leader != null ? attacker.Leader.Command + attacker.Leader.Strength : 100f;
            float defenderStats = defender.Leader != null ? defender.Leader.Command + defender.Leader.Strength : 100f;
            
            float statRatio = attackerStats / Math.Max(1f, defenderStats);
            
            // Influence of Unit Types could be added here if UnitKind was fully exposed with counters
            
            return baseDamage * statRatio;
        }
    }
}
