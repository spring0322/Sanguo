using System;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameManager
{
    public enum UnitType
    {
        步兵,
        骑兵,
        弓兵,
        攻城器械,
        水军
    }

    public enum TerrainType
    {
        Plain,
        Forest,
        Mountain,
        River,
        City,
        Wall,
        Other
    }

    public interface IMapInfoProvider
    {
        bool IsInBounds(int x, int y);
        TerrainType GetTerrain(int x, int y);
        bool IsInEnemyZOC(int x, int y, int factionId);
        int GetTemporaryPenalty(Microsoft.Xna.Framework.Point position);
    }

    public class ScenarioMapProvider : IMapInfoProvider
    {
        public bool IsInBounds(int x, int y)
        {
            return Session.Current.Scenario != null && !Session.Current.Scenario.PositionOutOfRange(new Microsoft.Xna.Framework.Point(x, y));
        }

        public bool IsInEnemyZOC(int x, int y, int factionId)
        {
             // 简单的 ZOC 检查逻辑
             Point p = new Point(x, y);
             foreach (Troop t in Session.Current.Scenario.Troops)
             {
                 if (!t.Destroyed && t.BelongedFaction != null && t.BelongedFaction.ID != factionId)
                 {
                     if (Math.Max(Math.Abs(t.Position.X - x), Math.Abs(t.Position.Y - y)) <= 1) return true;
                 }
             }
             return false;
        }

        public int GetTemporaryPenalty(Point position)
        {
            return 0; // 默认零惩罚
        }

        public TerrainType GetTerrain(int x, int y)
        {
            // 🔥 修复 NullReferenceException：添加安全检查
            if (Session.Current?.Scenario?.MapTileData == null) return TerrainType.Plain;
            
            var position = new Microsoft.Xna.Framework.Point(x, y);
            if (Session.Current.Scenario.PositionOutOfRange(position)) return TerrainType.Plain;
            
            try
            {
                var tile = Session.Current.Scenario.MapTileData[x, y];
                if (tile.TileArchitecture == null && tile.TileTroop == null && tile.TileRouteways == null) return TerrainType.Plain; // 简单的判空逻辑
                
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPositionNoCheck(position);
                // 使用 Scenario 直接获取地形，因为 TileData 里面没有直接的 TerrainKind
                switch (terrainKind)
                {
                    case TerrainKind.平原: return TerrainType.Plain;
                    case TerrainKind.草原: return TerrainType.Plain;
                    case TerrainKind.森林: return TerrainType.Forest;
                case TerrainKind.山地: return TerrainType.Mountain;
                case TerrainKind.峻岭: return TerrainType.Mountain;
                case TerrainKind.水域: return TerrainType.River;
                case TerrainKind.湿地: return TerrainType.River; 
                default: return TerrainType.Other;
            }
            }
            catch
            {
                return TerrainType.Plain;
            }
        }
    }

    public static class World
    {
        public static IMapInfoProvider MapProvider { get; set; } = new ScenarioMapProvider();
    }
}
