using System;
using Microsoft.Xna.Framework;

namespace GameManager
{
    /// <summary>
    /// 视线检测工具类 - 使用Bresenham算法进行高效的线性清除检测
    /// 🎯 用途：
    /// 1. 音频遮挡检测 - 判断声音是否被墙体阻挡
    /// 2. 视线检测 - 判断两点之间是否有障碍物
    /// 3. 射线检测 - 用于弓箭、法术等直线攻击
    /// </summary>
    public static class LineOfSightChecker
    {
        /// <summary>
        /// 检查两点之间的直线是否畅通（无障碍物）
        /// 使用Bresenham直线算法进行高效检测
        /// </summary>
        /// <param name="start">起始点</param>
        /// <param name="end">结束点</param>
        /// <param name="isWallFunc">检查指定坐标是否为墙体的函数</param>
        /// <returns>true表示路径畅通，false表示有障碍物</returns>
        public static bool IsLineClear(Point start, Point end, Func<int, int, bool> isWallFunc)
        {
            int x0 = start.X;
            int y0 = start.Y;
            int x1 = end.X;
            int y1 = end.Y;
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                // 检查当前格子是否有障碍
                if (isWallFunc(x0, y0)) 
                    return false;

                if (x0 == x1 && y0 == y1) 
                    break;

                int e2 = 2 * err;
                if (e2 > -dy) 
                { 
                    err -= dy; 
                    x0 += sx; 
                }
                if (e2 < dx) 
                { 
                    err += dx; 
                    y0 += sy; 
                }
            }
            
            return true;
        }

        /// <summary>
        /// 检查两点之间的直线是否畅通（使用游戏地图数据）
        /// </summary>
        /// <param name="start">起始点</param>
        /// <param name="end">结束点</param>
        /// <returns>true表示路径畅通，false表示有障碍物</returns>
        public static bool IsLineClearInGame(Point start, Point end)
        {
            return IsLineClear(start, end, (x, y) =>
            {
                // 检查边界
                if (x < 0 || y < 0 || 
                    x >= Session.Current.Scenario.ScenarioMap.MapDimensions.X || 
                    y >= Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                {
                    return true; // 边界外视为墙体
                }

                // 检查地形是否为障碍物
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(new Point(x, y));
                
                // 根据地形类型判断是否为障碍物
                switch (terrainKind)
                {
                    case GameGlobal.TerrainKind.山地:
                    case GameGlobal.TerrainKind.森林:
                        return true; // 山地和森林阻挡视线和声音
                    case GameGlobal.TerrainKind.平原:
                    case GameGlobal.TerrainKind.草原:
                    case GameGlobal.TerrainKind.水域:
                        return false; // 平原、草原、水域不阻挡
                    default:
                        return false;
                }
            });
        }

        /// <summary>
        /// 计算声音传播的衰减系数（考虑障碍物）
        /// </summary>
        /// <param name="soundPos">声音源位置</param>
        /// <param name="listenerPos">监听者位置</param>
        /// <returns>衰减系数 (0.0 - 1.0)</returns>
        public static float CalculateSoundAttenuation(Vector2 soundPos, Vector2 listenerPos)
        {
            Point start = new Point((int)soundPos.X, (int)soundPos.Y);
            Point end = new Point((int)listenerPos.X, (int)listenerPos.Y);

            // 如果直线畅通，不衰减
            if (IsLineClearInGame(start, end))
            {
                return 1.0f;
            }

            // 如果有障碍物，计算绕行路径的衰减
            float distance = Vector2.Distance(soundPos, listenerPos);
            
            // 简单的障碍物衰减模型
            if (distance < 50f)
            {
                return 0.7f; // 近距离，即使有障碍物也能听到一些
            }
            else if (distance < 150f)
            {
                return 0.3f; // 中距离，声音被大幅衰减
            }
            else
            {
                return 0.1f; // 远距离，几乎听不到
            }
        }

        /// <summary>
        /// 检查射击路径是否畅通（用于弓箭攻击等）
        /// </summary>
        /// <param name="shooterPos">射手位置</param>
        /// <param name="targetPos">目标位置</param>
        /// <returns>true表示可以射击，false表示被阻挡</returns>
        public static bool CanShoot(Point shooterPos, Point targetPos)
        {
            return IsLineClear(shooterPos, targetPos, (x, y) =>
            {
                // 检查边界
                if (x < 0 || y < 0 || 
                    x >= Session.Current.Scenario.ScenarioMap.MapDimensions.X || 
                    y >= Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                {
                    return true;
                }

                // 检查是否有建筑物阻挡
                var architecture = Session.Current.Scenario.GetArchitectureByPositionNoCheck(new Point(x, y));
                if (architecture != null)
                {
                    return true; // 建筑物阻挡射击
                }

                // 检查地形
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(new Point(x, y));
                return terrainKind == GameGlobal.TerrainKind.山地; // 只有山地阻挡射击
            });
        }

        /// <summary>
        /// 获取直线路径上的所有点（用于特效显示等）
        /// </summary>
        /// <param name="start">起始点</param>
        /// <param name="end">结束点</param>
        /// <returns>路径上的所有点</returns>
        public static Point[] GetLinePoints(Point start, Point end)
        {
            var points = new System.Collections.Generic.List<Point>();
            
            int x0 = start.X;
            int y0 = start.Y;
            int x1 = end.X;
            int y1 = end.Y;
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                points.Add(new Point(x0, y0));

                if (x0 == x1 && y0 == y1) 
                    break;

                int e2 = 2 * err;
                if (e2 > -dy) 
                { 
                    err -= dy; 
                    x0 += sx; 
                }
                if (e2 < dx) 
                { 
                    err += dx; 
                    y0 += sy; 
                }
            }
            
            return points.ToArray();
        }
    }
}