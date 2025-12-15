using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameGlobal;
using GameObjects.TroopDetail;

namespace GameManager
{
    public static class TacticalScorer
    {
        public static float EvaluatePosition(Troop troop, Point targetPos, Architecture siegeTarget = null)
        {
            float score = 0f;
            var scenario = Session.Current.Scenario;

            // --- 规则 1: 夹击/包围加成 ---
            // 检查目标位置对面是否有友军
            if (siegeTarget != null) // Flanking usually implies flanking *something*, assume siegeTarget for now or check generic melee flanking
            {
                 if (HasAllyFlanking(troop, targetPos, siegeTarget))
                 {
                    score += scenario.GameCommonData.FlankBonus * 2.0f;
                 }
            }

            // --- 规则 2: 地形适性 ---
            // 不要让骑兵走进森林
            // 使用 TerrainKind 枚举
            // 需要获取目标点的地形 ID
            int terrainId = scenario.MapTileData[targetPos.X, targetPos.Y].TerrainID;
            // 假设 TerrainDetail ID 和 TerrainKind 对应 (通常是)
            // Safety check
            if (terrainId < scenario.GameCommonData.AllTerrainDetails.Count)
            {
                var terrainDetail = scenario.GameCommonData.AllTerrainDetails.GetGameObject(terrainId) as GameObjects.MapDetail.TerrainDetail;
                
                // Check if Cavalry (Qibing)
                // MilitaryType.骑兵 is usually implied by specific MilitaryKinds. 
                // We check troop.Army.Kind.Type 
                if (troop.Army != null && troop.Army.Kind != null && troop.Army.Kind.Type == MilitaryType.骑兵)
                {
                    // Forest is usually ID 3 per TerrainDetail source (case 3: ForrestAdaptability)
                    // Or check TerrainKind enum directly if we can map it.
                    // TerrainDetail ID 3 = Forest.
                    if (terrainDetail.ID == (int)TerrainKind.森林)
                    {
                        score -= 50f; 
                    }
                }
            }

            // --- 规则 3: 围城断粮 ---
            // 如果目标位置能切断敌城的补给线
            if (siegeTarget != null && IsCuttingSupply(targetPos, siegeTarget))
            {
                score += 100f; // 极高的战略价值
            }

            // --- 规则 4: 避免ZOC (控制领域) 惩罚 ---
            // 如果移动到该点会被多个敌人借机攻击
            int threatCount = CountEnemiesAround(troop, targetPos);
            score -= threatCount * 20f;

            return score;
        }

        private static bool HasAllyFlanking(Troop me, Point pos, Architecture target)
        {
            if (me.BelongedFaction == null) return false;

            // Simple vector math: The "Opposite" side of the target from 'pos'
            // Target Center: T
            // My potential Pos: P
            // Opposite Pos: O = T + (T - P) = 2T - P
            
            // Note: Integer points might not align perfectly on grid, check vicinity
            Point vector = new Point(target.Position.X - pos.X, target.Position.Y - pos.Y);
            Point oppositePos = new Point(target.Position.X + vector.X, target.Position.Y + vector.Y);

            // Check if any ally is near oppositePos (radius 1)
            var ally = GetAllyAt(me, oppositePos, 1);
            return ally != null;
        }
        
        private static Troop GetAllyAt(Troop me, Point pos, int radius)
        {
             foreach (Troop t in Session.Current.Scenario.Troops)
             {
                 if (t.Destroyed || t == me) continue;
                 if (me.BelongedFaction.IsFriendly(t.BelongedFaction))
                 {
                     if (Math.Abs(t.Position.X - pos.X) <= radius && Math.Abs(t.Position.Y - pos.Y) <= radius)
                     {
                         return t;
                     }
                 }
             }
             return null;
        }

        private static bool IsCuttingSupply(Point pos, Architecture city)
        {
            // Simplified Logic: 
            // If the city is under siege (enemies nearby), and we occupy a position on its Routeway list?
            // Architecture usually has Routeways.
            // Let's assume generic logic: Block path to Faction Leader's location?
            // "Cutting Supply" in this game context might mean blocking Routeways derived from Connected architectures.
            
            // For now, implementing a placeholder check:
            // If checking 'pos' makes it harder for city to connect to others?
            // Too complex for single function without pathfinding access.
            
            // Alternative: Simply verify if 'pos' is on a key routeway node.
            // We can check city.Routeways (if exposed) or assume cutting off from Capital.
            
            // Current Implementation: Return false until Routeway system is more exposed/understood.
            // Or check if pos has 'Routeway' graphic/tile property.
            
            return false;
        }

        private static int CountEnemiesAround(Troop me, Point pos) 
        {
            int count = 0;
            // Check 4 adjacencies
            var adj = new Point[] { new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) };
            
            foreach (var offset in adj)
            {
                var checkPos = new Point(pos.X + offset.X, pos.Y + offset.Y);
                 if (!Session.Current.Scenario.PositionOutOfRange(checkPos))
                 {
                     var t = Session.Current.Scenario.GetTroopByPosition(checkPos);
                     if (t != null && !me.BelongedFaction.IsFriendly(t.BelongedFaction))
                     {
                         count++;
                     }
                 }
            }
            return count; 
        }
    }
}
