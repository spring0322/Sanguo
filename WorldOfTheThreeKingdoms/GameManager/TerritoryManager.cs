using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameManager
{
    // 领土管理器 - 简化版本
    public class TerritoryManager
    {
        private int[,] territoryMap;
        private int width;
        private int height;

        public TerritoryManager(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.territoryMap = new int[width, height];
        }

        // 目前实现并未真正使用 mapData，保持参数以便将来扩展
        public void RecalculateTerritory(ArchitectureList architectures, int[,] mapData)
        {
            // 简化的领土计算逻辑
            // 实际实现中会根据城市位置计算势力范围
            
            // 清空地图
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    territoryMap[x, y] = -1; // -1表示无主之地
                }
            }

            // 简单的城市影响范围计算
            if (architectures != null)
            {
                foreach (Architecture arch in architectures)
                {
                    if (arch.BelongedFaction != null)
                    {
                        int factionId = arch.BelongedFaction.ID;
                        Point pos = arch.Position;
                        
                        // 在城市周围设置势力范围
                        int radius = 5; // 简化的影响半径
                        for (int x = Math.Max(0, pos.X - radius); x <= Math.Min(width - 1, pos.X + radius); x++)
                        {
                            for (int y = Math.Max(0, pos.Y - radius); y <= Math.Min(height - 1, pos.Y + radius); y++)
                            {
                                territoryMap[x, y] = factionId;
                            }
                        }
                    }
                }
            }
        }

        public int GetTerritoryOwner(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return territoryMap[x, y];
            }
            return -1;
        }

        public bool IsBorderPixel(int x, int y)
        {
            if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1)
                return false;

            int currentOwner = GetTerritoryOwner(x, y);
            if (currentOwner == -1) return false;

            // 检查四个方向是否有不同的势力
            return GetTerritoryOwner(x - 1, y) != currentOwner ||
                   GetTerritoryOwner(x + 1, y) != currentOwner ||
                   GetTerritoryOwner(x, y - 1) != currentOwner ||
                   GetTerritoryOwner(x, y + 1) != currentOwner;
        }

        public float GetTerritoryStrength(int x, int y)
        {
            // 简化的领土强度计算
            return GetTerritoryOwner(x, y) != -1 ? 1.0f : 0.0f;
        }
    }
}