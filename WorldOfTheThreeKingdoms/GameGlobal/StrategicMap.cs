using System;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 战略地图分析器 - 提供城市威胁等级评估
    /// </summary>
    public static class StrategicMap
    {
        /// <summary>
        /// 获取城市威胁等级 (0.0 - 1.0)
        /// </summary>
        /// <param name="coordinates">城市坐标</param>
        /// <returns>威胁等级，0为安全，1为极度危险</returns>
        public static float GetThreatLevel(Point coordinates)
        {
            // 简化实现：基于坐标计算威胁等级
            // 实际游戏中应该考虑周围敌军、地形、历史战斗等因素
            
            float threat = 0.0f;
            
            // 基于坐标的简单威胁计算
            // 假设地图边缘更危险
            int mapSize = 100;
            float distanceFromCenter = Math.Abs(coordinates.X - mapSize/2) + Math.Abs(coordinates.Y - mapSize/2);
            float maxDistance = mapSize;
            
            // 距离中心越远，威胁越高（边境效应）
            threat += (distanceFromCenter / maxDistance) * 0.4f;
            
            // 添加一些随机性模拟动态战况
            var random = new Random(coordinates.X * 1000 + coordinates.Y);
            threat += (float)(random.NextDouble() * 0.3);
            
            return Math.Min(threat, 1.0f);
        }
        
        /// <summary>
        /// 获取城市威胁等级 - 重载方法，直接接受城市对象
        /// </summary>
        /// <param name="city">城市对象</param>
        /// <returns>威胁等级</returns>
        public static float GetThreatLevel(City city)
        {
            if (city == null) return 0.0f;
            
            float baseThreat = GetThreatLevel(city.Coordinates);
            
            // 城市特性修正
            if (city.IsBorderCity) baseThreat += 0.3f;
            if (city.HasRecentBattle) baseThreat += 0.4f;
            if (city.IsCapital) baseThreat += 0.1f; // 首都更重要，需要防护
            
            return Math.Min(baseThreat, 1.0f);
        }
        
        /// <summary>
        /// 判断城市是否为前线
        /// </summary>
        /// <param name="city">城市对象</param>
        /// <returns>是否为前线城市</returns>
        public static bool IsFrontlineCity(City city)
        {
            return GetThreatLevel(city) > 0.6f;
        }
        
        /// <summary>
        /// 获取城市战略重要性评分
        /// </summary>
        /// <param name="city">城市对象</param>
        /// <returns>重要性评分 (0-100)</returns>
        public static float GetStrategicImportance(City city)
        {
            if (city == null) return 0.0f;
            
            float importance = 50.0f; // 基础重要性
            
            // 首都最重要
            if (city.IsCapital) importance += 30.0f;
            
            // 港口城市重要
            if (city.IsPortCity) importance += 15.0f;
            
            // 边境城市重要
            if (city.IsBorderCity) importance += 20.0f;
            
            // 人口规模影响
            importance += (city.Population / 1000.0f) * 0.5f;
            
            // 繁荣度影响
            importance += city.Prosperity * 10.0f;
            
            return Math.Min(importance, 100.0f);
        }
    }
}