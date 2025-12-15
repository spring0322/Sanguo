using System;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 移动消耗计算器 - 基于你的反馈实现的 SLG 核心逻辑
    /// 🎯 核心功能：
    /// 1. 传入 unit 信息，计算具体的消耗 (包含 ZOC 和地形)
    /// 2. 返回 -1 表示不可通行，其他值表示移动消耗
    /// 3. 完美集成到 A* 寻路算法中
    /// </summary>
    public class MovementCalculator
    {
        private readonly IMapInfoProvider _mapProvider;
        
        public MovementCalculator(IMapInfoProvider mapProvider = null)
        {
            _mapProvider = mapProvider ?? World.MapProvider;
        }
        
        /// <summary>
        /// 🎯 关键方法：计算从 from 到 to 的移动消耗
        /// 这是你在反馈中提到的核心修改点
        /// </summary>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <param name="unit">单位信息（包含兵种、势力等）</param>
        /// <returns>移动消耗，-1表示不可通行</returns>
        public int GetMoveCost(Point from, Point to, Unit unit)
        {
            // 1. 边界检查
            if (!_mapProvider.IsInBounds(to.X, to.Y))
                return -1;
            
            // 2. 单位占据检查
            if (_mapProvider.IsUnitAt(to.X, to.Y))
                return -1; // 有其他单位占据
            
            // 3. 基础地形消耗计算
            int baseCost = GetBaseTerrainCost(to, unit.UnitType);
            if (baseCost == -1)
                return -1; // 地形不可通行
            
            // 4. ZOC (控制区) 惩罚 - SLG 的灵魂
            if (_mapProvider.IsInEnemyZOC(to.X, to.Y, unit.FactionId))
            {
                baseCost += 5; // ZOC 惩罚
            }
            
            // 5. 其他战术因素（可扩展）
            baseCost += GetTacticalModifier(from, to, unit);
            
            return baseCost;
        }
        
        /// <summary>
        /// 获取基础地形移动消耗
        /// </summary>
        private int GetBaseTerrainCost(Point position, UnitType unitType)
        {
            var terrain = _mapProvider.GetTerrain(position.X, position.Y);
            
            switch (terrain)
            {
                case TerrainType.Plain:
                    switch (unitType)
                    {
                        case UnitType.骑兵: return 1;      // 骑兵在平原最快
                        case UnitType.步兵: return 1;      // 步兵标准
                        case UnitType.弓兵: return 1;      // 弓兵标准
                        case UnitType.攻城器械: return 3;   // 攻城器械慢
                        case UnitType.水军: return -1;     // 水军不能上陆
                    }
                    break;
                    
                case TerrainType.Forest:
                    switch (unitType)
                    {
                        case UnitType.骑兵: return 3;      // 骑兵在森林受限
                        case UnitType.步兵: return 2;      // 步兵适中
                        case UnitType.弓兵: return 2;      // 弓兵适中
                        case UnitType.攻城器械: return 5;   // 攻城器械很慢
                        case UnitType.水军: return -1;     // 水军不能上陆
                    }
                    break;
                    
                case TerrainType.Mountain:
                    switch (unitType)
                    {
                        case UnitType.骑兵: return 5;      // 骑兵在山地很慢
                        case UnitType.步兵: return 3;      // 步兵较慢
                        case UnitType.弓兵: return 3;      // 弓兵较慢
                        case UnitType.攻城器械: return -1;  // 攻城器械不能上山
                        case UnitType.水军: return -1;     // 水军不能上陆
                    }
                    break;
                    
                case TerrainType.River:
                    switch (unitType)
                    {
                        case UnitType.水军: return 1;      // 水军在水上正常
                        default: return -1;                // 其他兵种不能下水
                    }
                    
                case TerrainType.City:
                    switch (unitType)
                    {
                        case UnitType.步兵: return 1;      // 步兵在城市正常
                        case UnitType.弓兵: return 1;      // 弓兵在城市正常
                        case UnitType.骑兵: return 2;      // 骑兵在城市较慢
                        case UnitType.攻城器械: return 3;   // 攻城器械在城市慢
                        case UnitType.水军: return -1;     // 水军不能上陆
                    }
                    break;
                    
                case TerrainType.Wall:
                    switch (unitType)
                    {
                        case UnitType.步兵: return 2;      // 步兵可以走城墙
                        case UnitType.弓兵: return 2;      // 弓兵可以走城墙
                        case UnitType.骑兵: return 4;      // 骑兵在城墙很慢
                        default: return -1;                // 攻城器械不能上城墙
                    }
                    
                default:
                    return 1; // 默认消耗
            }
            
            return 1; // 默认消耗
        }
        
        /// <summary>
        /// 获取战术修正值（可扩展的战术因素）
        /// </summary>
        private int GetTacticalModifier(Point from, Point to, Unit unit)
        {
            int modifier = 0;
            
            // 示例：疲劳度影响
            if (unit.Fatigue > 80)
                modifier += 1; // 疲劳时移动更慢
            
            // 示例：天气影响
            // if (Weather.IsRaining)
            //     modifier += 1;
            
            // 示例：士气影响
            if (unit.Morale < 30)
                modifier += 1; // 士气低落时移动更慢
            
            return modifier;
        }
    }
    
    /// <summary>
    /// 单位信息接口 - 用于移动消耗计算
    /// 🎯 设计原则：只暴露寻路需要的单位信息
    /// </summary>
    public class Unit
    {
        public UnitType UnitType { get; set; }
        public int FactionId { get; set; }
        public int Fatigue { get; set; } = 0;      // 疲劳度 (0-100)
        public int Morale { get; set; } = 100;     // 士气 (0-100)
        
        /// <summary>
        /// 从 Troop 对象创建 Unit 信息
        /// </summary>
        public static Unit FromTroop(Troop troop)
        {
            var unit = new Unit
            {
                FactionId = troop.BelongedFaction?.ID ?? -1
            };
            
            // 自动判断单位类型
            unit.UnitType = DetermineUnitType(troop);
            
            // 获取疲劳度和士气（如果 Troop 有这些属性）
            try
            {
                // 这里需要根据实际的 Troop 属性调整
                // unit.Fatigue = troop.Fatigue ?? 0;
                // unit.Morale = troop.Morale ?? 100;
            }
            catch
            {
                // 使用默认值
                unit.Fatigue = 0;
                unit.Morale = 100;
            }
            
            return unit;
        }
        
        /// <summary>
        /// 根据部队的军事类型自动确定单位类型
        /// </summary>
        private static UnitType DetermineUnitType(Troop troop)
        {
            if (troop?.Army?.Kind == null)
                return UnitType.步兵; // 默认为步兵
            
            var militaryKind = troop.Army.Kind;
            
            try
            {
                string kindName = militaryKind.ToString();
                
                // 检查是否是水军
                if (kindName.Contains("水军") || kindName.Contains("舰") || kindName.Contains("船"))
                    return UnitType.水军;
                
                // 检查是否是骑兵
                if (kindName.Contains("骑兵") || kindName.Contains("骑") || kindName.Contains("马"))
                    return UnitType.骑兵;
                
                // 检查是否是弓兵
                if (kindName.Contains("弓兵") || kindName.Contains("弩") || kindName.Contains("射"))
                    return UnitType.弓兵;
                
                // 检查是否是攻城器械
                if (kindName.Contains("器械") || kindName.Contains("投石") || kindName.Contains("冲车"))
                    return UnitType.攻城器械;
                
                return UnitType.步兵; // 默认为步兵
            }
            catch
            {
                return UnitType.步兵; // 出错时默认为步兵
            }
        }
    }
}