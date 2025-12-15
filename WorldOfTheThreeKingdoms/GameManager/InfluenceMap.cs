using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 影响力地图 (Influence Map)
    /// 用于计算战场的"势"，帮助AI识别战线、安全区和突破口
    /// </summary>
    public class InfluenceMap
    {
        private float[,] _map;
        private int _width;
        private int _height;

        // 影响力衰减系数 (0.0 - 1.0)，每格衰减多少比例
        private const float DefaultDecay = 0.2f;

        public int Width => _width;
        public int Height => _height;

        public InfluenceMap(int width, int height)
        {
            _width = width;
            _height = height;
            _map = new float[width, height];
        }

        /// <summary>
        /// 清空地图数据
        /// </summary>
        public void Clear()
        {
            Array.Clear(_map, 0, _map.Length);
        }

        /// <summary>
        /// 刷新该势力的影响力地图
        /// </summary>
        /// <param name="viewerFaction">以哪个势力的视角来观察（该势力的部队为正值，敌对为负值）</param>
        public void Refresh(Faction viewerFaction)
        {
            Clear();

            if (Session.Current == null || Session.Current.Scenario == null) return;
            if (viewerFaction == null) return;

            // 遍历所有部队
            foreach (Troop troop in Session.Current.Scenario.Troops)
            {
                if (troop == null || troop.Destroyed) continue;
                if (troop.BelongedFaction == null) continue;

                // 基础影响力取决于战斗力
                float power = troop.FightingForce / 100.0f;
                
                // 影响范围 (半径) - 简化处理，所有部队统一半径
                int radius = 4; 

                bool isFriendly = viewerFaction.IsFriendly(troop.BelongedFaction);
                
                // 友军产生正向影响力，敌军产生负向影响力
                float influenceValue = isFriendly ? power : -power;

                AddSource(troop.Position, influenceValue, radius);
            }

            // 遍历建筑(Architecture)，建筑应该提供更强的静态据点影响力
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (architecture == null || architecture.BelongedFaction == null) continue;

                bool isFriendly = viewerFaction.IsFriendly(architecture.BelongedFaction);
                float basePower = 50.0f; // 建筑影响力很大
                float influenceValue = isFriendly ? basePower : -basePower;

                AddSource(architecture.Position, influenceValue, 6);
            }
        }

        /// <summary>
        /// 刷新影响力地图 - 兼容旧接口
        /// </summary>
        public void Refresh(object faction)
        {
            if (faction is Faction f)
            {
                Refresh(f);
            }
            else
            {
                Clear();
            }
        }

        /// <summary>
        /// 向地图添加一个辐射源
        /// </summary>
        /// <param name="center">中心坐标</param>
        /// <param name="power">中心强度</param>
        /// <param name="radius">辐射半径</param>
        private void AddSource(Point center, float power, int radius)
        {
            // 仅遍历受影响的矩形区域，通过性能优化
            int minX = Math.Max(0, center.X - radius);
            int maxX = Math.Min(_width - 1, center.X + radius);
            int minY = Math.Max(0, center.Y - radius);
            int maxY = Math.Min(_height - 1, center.Y + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // 使用曼哈顿距离计算
                    int distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);

                    if (distance <= radius)
                    {
                        // 线性衰减公式：强度 * (1 - 距离/半径)
                        float falloff = 1.0f - ((float)distance / (radius + 1));
                        _map[x, y] += power * falloff;
                    }
                }
            }
        }

        /// <summary>
        /// 设置指定位置的影响力值
        /// </summary>
        public void SetInfluence(int x, int y, float value)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _map[x, y] = value;
            }
        }

        /// <summary>
        /// 获取指定位置的影响力值 (int 重载)
        /// </summary>
        public float GetInfluence(int x, int y)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                return _map[x, y];
            }
            return 0f;
        }

        /// <summary>
        /// 获取指定位置的影响力值 (Point 重载)
        /// </summary>
        public float GetInfluence(Point pos)
        {
            return GetInfluence(pos.X, pos.Y);
        }

        /// <summary>
        /// 获取最佳位置
        /// </summary>
        public Point GetBestPosition(Point center, int radius)
        {
            Point bestPos = center;
            float bestValue = GetInfluence(center.X, center.Y);

            for (int x = Math.Max(0, center.X - radius); x <= Math.Min(_width - 1, center.X + radius); x++)
            {
                for (int y = Math.Max(0, center.Y - radius); y <= Math.Min(_height - 1, center.Y + radius); y++)
                {
                    float value = GetInfluence(x, y);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestPos = new Point(x, y);
                    }
                }
            }

            return bestPos;
        }

        /// <summary>
        /// 在给定区域内寻找最安全的位置（影响力最高的位置）
        /// </summary>
        public Point FindSafestPosition(List<Point> searchArea)
        {
            if (searchArea == null || searchArea.Count == 0)
                return new Point(_width / 2, _height / 2);

            Point bestPos = searchArea[0];
            float maxInf = float.MinValue;

            foreach (var pos in searchArea)
            {
                if (!IsValidPosition(pos)) continue;

                float inf = _map[pos.X, pos.Y];
                if (inf > maxInf)
                {
                    maxInf = inf;
                    bestPos = pos;
                }
            }

            return bestPos;
        }

        /// <summary>
        /// 在给定区域内寻找最危险的位置（影响力最低/负值最大的位置）
        /// </summary>
        public Point FindMostThreateningPosition(List<Point> searchArea)
        {
            if (searchArea == null || searchArea.Count == 0)
                return new Point(_width / 2, _height / 2);

            Point bestPos = searchArea[0];
            float minInf = float.MaxValue;

            foreach (var pos in searchArea)
            {
                if (!IsValidPosition(pos)) continue;

                float inf = _map[pos.X, pos.Y];
                if (inf < minInf)
                {
                    minInf = inf;
                    bestPos = pos;
                }
            }

            return bestPos;
        }

        /// <summary>
        /// 获取某一点的影响力梯度（用于判断应该向哪个方向移动以增加安全性）
        /// </summary>
        public Point GetGradientDirection(Point pos)
        {
            if (!IsValidPosition(pos)) return Point.Zero;

            float currentVal = _map[pos.X, pos.Y];
            Point bestDir = Point.Zero;
            float maxDiff = 0;

            // 检查上下左右四个方向
            Point[] dirs = { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };

            foreach (var dir in dirs)
            {
                Point next = new Point(pos.X + dir.X, pos.Y + dir.Y);
                if (IsValidPosition(next))
                {
                    float diff = _map[next.X, next.Y] - currentVal;
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        bestDir = dir;
                    }
                }
            }

            return bestDir;
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            float totalInfluence = 0f;
            int nonZeroCount = 0;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float value = _map[x, y];
                    if (value != 0f)
                    {
                        totalInfluence += Math.Abs(value);
                        nonZeroCount++;
                    }
                }
            }

            return $"影响力地图 ({_width}x{_height}): {nonZeroCount} 个活跃点, 总影响力: {totalInfluence:F2}";
        }

        private bool IsValidPosition(Point p)
        {
            return p.X >= 0 && p.X < _width && p.Y >= 0 && p.Y < _height;
        }
    }
}