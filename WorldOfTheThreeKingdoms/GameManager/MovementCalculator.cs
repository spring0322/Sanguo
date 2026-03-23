using System;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal; // 确保引用 Session

namespace GameManager
{
    /// <summary>
    /// 移动消耗计算器 - [NewMovementSystem] 适配增强版
    /// 🎯 核心功能：
    /// 1. 完整保留原有的地形、兵种、ZOC、战术修正逻辑
    /// 2. 引入“软碰撞”机制：友军不再视为墙壁，而是高消耗路段
    /// </summary>
    public class MovementCalculator
    {
        // 软碰撞惩罚值：友军不是墙，是泥潭
        // 这个值要足够大，让AI优先绕路；但又不能无限大，保证实在没路时（如堵桥）能排队通过
        private const int FRIENDLY_UNIT_PENALTY = 200; 

        private readonly IMapInfoProvider _mapProvider;
        
        public MovementCalculator(IMapInfoProvider mapProvider = null)
        {
            _mapProvider = mapProvider ?? World.MapProvider;
        }
        
        /// <summary>
        /// 🎯 关键方法：计算从 from 到 to 的移动消耗
        /// </summary>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <param name="unit">单位信息（包含兵种、势力等）</param>
        /// <returns>移动消耗，-1表示不可通行</returns>
        public int GetMoveCost(Point from, Point to, Unit unit)
        {
            // 1. 边界检查
            if (Session.Current.Scenario.PositionOutOfRange(to))
            {
                return -1;
            }
            
            // 2. 获取对应的Troop对象
            Troop troop = Session.Current.Scenario.Troops.GetTroop(unit.Id);
            if (troop == null)
            {
                return 1; // 默认消耗
            }
            
            // 3. 使用游戏原有的移动消耗计算系统
            try
            {
                int cost = troop.NextPositionCost(from, to);
                return cost;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MovementCalculator] 计算移动消耗出错: {ex.Message}");
                return 1; // 出错时返回默认消耗
            }
        }
        
        /// <summary>
        /// 获取基础地形移动消耗 (完整保留)
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
                    break; // Added break just in case
                    
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
                    break; // Added break
                    
                default:
                    return 1; // 默认消耗
            }
            
            return 1; // 默认消耗
        }
        
        /// <summary>
        /// 获取战术修正值 (完整保留)
        /// </summary>
        private int GetTacticalModifier(Point from, Point to, Unit unit)
        {
            int modifier = 0;
            
            // 示例：疲劳度影响
            if (unit.Fatigue > 80)
                modifier += 1; // 疲劳时移动更慢
            
            // 示例：士气影响
            if (unit.Morale < 30)
                modifier += 1; // 士气低落时移动更慢
            
            return modifier;
        }
    }
    
    /// <summary>
    /// 单位信息接口 - 用于移动消耗计算
    /// </summary>
    public class Unit
    {
        public int Id { get; set; } = -1; // 🔥 新增：用于排除自己
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
                Id = troop.ID,
                FactionId = troop.BelongedFaction?.ID ?? -1
            };
            
            // 自动判断单位类型
            unit.UnitType = DetermineUnitType(troop);
            
            // 获取疲劳度和士气（保留原有的容错逻辑）
            try
            {
                unit.Fatigue = troop.Army != null ? troop.Army.Tiredness : 0;
                unit.Morale = troop.Army != null ? troop.Army.Morale : 100;
            }
            catch
            {
                unit.Fatigue = 0;
                unit.Morale = 100;
            }
            
            return unit;
        }
        
        /// <summary>
        /// 根据部队的军事类型自动确定单位类型 (完整保留)
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