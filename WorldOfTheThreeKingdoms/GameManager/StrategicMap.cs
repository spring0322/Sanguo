using System;
using Microsoft.Xna.Framework;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 战略地图系统 - 用于AI威胁评估和战略决策
    /// </summary>
    public class StrategicMap
    {
        public static StrategicMap Instance { get; private set; }

        // 使用 InfluenceMap 作为底层实现，复用通用影响力地图逻辑
        private WorldOfTheThreeKingdoms.GameManager.InfluenceMap _influenceMap;
        private int _width, _height;
        private int _scale = 1; // 如果地图太大，可以设为 10 (10x10 格子算一个点)

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="width">地图宽度</param>
        /// <param name="height">地图高度</param>
        public StrategicMap(int width, int height)
        {
            Initialize(width, height);
        }

        /// <summary>
        /// 初始化战略地图
        /// </summary>
        /// <param name="w">地图宽度</param>
        /// <param name="h">地图高度</param>
        public void Initialize(int w, int h)
        {
            _width = w;
            _height = h;
            _influenceMap = new WorldOfTheThreeKingdoms.GameManager.InfluenceMap(w, h);
            Instance = this;
        }

        /// <summary>
        /// 每回合调用一次，根据 AI 的记忆刷新地图
        /// </summary>
        /// <param name="aiFaction">AI势力</param>
        public void Refresh(Faction aiFaction)
        {
            if (_influenceMap == null) return;

            // 先清空
            _influenceMap.Clear();

            // 遍历记忆中的幽灵，生成威胁场
            if (aiFaction.MemoryMap != null && aiFaction.MemoryMap.Values != null)
            {
                // 注意：AIMemoryMap.Values 是 Dictionary<string, GhostUnit>
                // 这里需要遍历其 Values，才能拿到 GhostUnit 对象
                foreach (var ghost in aiFaction.MemoryMap.Values.Values)
                {
                    // 使用 GhostUnit 的当前强度作为威胁值
                    float currentStrength = ghost.GetCurrentStrength();
                    if (currentStrength <= 0.1f) continue; // 太弱/太旧的情报忽略

                    // 威胁值 = 当前强度的简单映射
                    float threatValue = currentStrength / 100f;

                    // 简单的印章法：向周围辐射 3 格
                    AddInfluence(ghost.LastPosition, threatValue, 3);
                }
            }
        }

        /// <summary>
        /// 在指定位置添加影响力
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="value">影响力值</param>
        /// <param name="radius">影响半径</param>
        private void AddInfluence(Point center, float value, int radius)
        {
            if (_influenceMap == null) return;

            // 边界检查
            int minX = Math.Max(0, center.X - radius);
            int maxX = Math.Min(_width - 1, center.X + radius);
            int minY = Math.Max(0, center.Y - radius);
            int maxY = Math.Min(_height - 1, center.Y + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // 简化的线性衰减
                    float dist = Vector2.Distance(new Vector2(center.X, center.Y), new Vector2(x, y));
                    if (dist <= radius)
                    {
                        float influence = value * (1.0f - dist / radius);

                        // 叠加到 InfluenceMap 上：先取出当前值再加
                        float current = _influenceMap.GetInfluence(x, y);
                        _influenceMap.SetInfluence(x, y, current + influence);
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定位置的威胁值
        /// </summary>
        /// <param name="pos">位置</param>
        /// <returns>威胁值</returns>
        public float GetThreat(Point pos)
        {
            if (_influenceMap == null) return 0;
            return _influenceMap.GetInfluence(pos);
        }

        /// <summary>
        /// 兼容旧代码：获取指定位置的影响力值
        /// </summary>
        public float GetInfluence(Point pos)
        {
            return GetThreat(pos);
        }

        /// <summary>
        /// 获取指定区域的平均威胁值
        /// </summary>
        /// <param name="area">区域矩形</param>
        /// <returns>平均威胁值</returns>
        public float GetAverageThreat(Rectangle area)
        {
            if (_influenceMap == null) return 0;

            float totalThreat = 0;
            int count = 0;

            int minX = Math.Max(0, area.X);
            int maxX = Math.Min(_width - 1, area.X + area.Width - 1);
            int minY = Math.Max(0, area.Y);
            int maxY = Math.Min(_height - 1, area.Y + area.Height - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    totalThreat += _influenceMap.GetInfluence(x, y);
                    count++;
                }
            }

            return count > 0 ? totalThreat / count : 0;
        }

        /// <summary>
        /// 查找最安全的位置
        /// </summary>
        /// <param name="searchArea">搜索区域</param>
        /// <returns>最安全的位置</returns>
        public Point FindSafestPosition(Rectangle searchArea)
        {
            if (_influenceMap == null) return new Point(-1, -1);

            Point safestPos = new Point(-1, -1);
            float lowestThreat = float.MaxValue;

            int minX = Math.Max(0, searchArea.X);
            int maxX = Math.Min(_width - 1, searchArea.X + searchArea.Width - 1);
            int minY = Math.Max(0, searchArea.Y);
            int maxY = Math.Min(_height - 1, searchArea.Y + searchArea.Height - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float threat = _influenceMap.GetInfluence(x, y);
                    if (threat < lowestThreat)
                    {
                        lowestThreat = threat;
                        safestPos = new Point(x, y);
                    }
                }
            }

            return safestPos;
        }

        /// <summary>
        /// 查找威胁最高的位置
        /// </summary>
        /// <param name="searchArea">搜索区域</param>
        /// <returns>威胁最高的位置</returns>
        public Point FindMostThreatenedPosition(Rectangle searchArea)
        {
            if (_influenceMap == null) return new Point(-1, -1);

            Point mostThreatenedPos = new Point(-1, -1);
            float highestThreat = float.MinValue;

            int minX = Math.Max(0, searchArea.X);
            int maxX = Math.Min(_width - 1, searchArea.X + searchArea.Width - 1);
            int minY = Math.Max(0, searchArea.Y);
            int maxY = Math.Min(_height - 1, searchArea.Y + searchArea.Height - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float threat = _influenceMap.GetInfluence(x, y);
                    if (threat > highestThreat)
                    {
                        highestThreat = threat;
                        mostThreatenedPos = new Point(x, y);
                    }
                }
            }

            return mostThreatenedPos;
        }

        /// <summary>
        /// 清空威胁地图
        /// </summary>
        public void Clear()
        {
            _influenceMap?.Clear();
        }

        /// <summary>
        /// 获取地图尺寸
        /// </summary>
        /// <returns>地图尺寸</returns>
        public Point GetMapSize()
        {
            return new Point(_width, _height);
        }
    }
}