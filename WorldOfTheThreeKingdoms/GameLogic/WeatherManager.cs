using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 天气管理器（按网格划分天气，支持州域级别的气候带配置）
/// 日期：2026-03-09
/// </summary>
public sealed class WeatherManager(GameEnvironmentConfig config, int mapWidth, int mapHeight)
{
    private readonly Random _rng = new();
    private readonly GameEnvironmentConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    
    // 🔥 天气网格：每 N 个格子共享同一个天气（减少内存和计算量）
    private const int GridSize = 4; // 4x4 格子共享一个天气
    
    // 天气网格数据（二维数组，O(1) 查询）
    private WeatherType[,] _weatherGrid = InitializeWeatherGrid(mapWidth, mapHeight);
    
    // 🔥 风况网格数据（与天气网格同步，O(1) 查询）
    // 日期：2026-03-10
    private WindDirection[,] _windDirectionGrid = InitializeWindDirectionGrid(mapWidth, mapHeight);
    private WindForce[,] _windForceGrid = InitializeWindForceGrid(mapWidth, mapHeight);
    
    // 州域到气候带的映射（用于快速查询）
    private readonly Dictionary<int, ClimateZone> _stateToZone = BuildStateZoneMapping(config);
    
    // 地图尺寸
    private readonly int _mapWidth = mapWidth;
    private readonly int _mapHeight = mapHeight;
    private readonly int _gridWidth = (mapWidth + GridSize - 1) / GridSize;
    private readonly int _gridHeight = (mapHeight + GridSize - 1) / GridSize;

    private struct FogDispersalStats
    {
        public int Count;
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;

        public void Record(int gridX, int gridY)
        {
            if (Count == 0)
            {
                Count = 1;
                MinX = gridX;
                MinY = gridY;
                MaxX = gridX;
                MaxY = gridY;
                return;
            }

            Count++;

            if (gridX < MinX)
            {
                MinX = gridX;
            }
            else if (gridX > MaxX)
            {
                MaxX = gridX;
            }

            if (gridY < MinY)
            {
                MinY = gridY;
            }
            else if (gridY > MaxY)
            {
                MaxY = gridY;
            }
        }
    }

    /// <summary>
    /// 初始化天气网格（全部设为晴天）
    /// </summary>
    private static WeatherType[,] InitializeWeatherGrid(int mapWidth, int mapHeight)
    {
        int gridWidth = (mapWidth + GridSize - 1) / GridSize;
        int gridHeight = (mapHeight + GridSize - 1) / GridSize;
        var grid = new WeatherType[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = WeatherType.Sunny;
            }
        }
        
        return grid;
    }

    /// <summary>
    /// 初始化风向网格（全部设为无风）
    /// 日期：2026-03-10
    /// </summary>
    private static WindDirection[,] InitializeWindDirectionGrid(int mapWidth, int mapHeight)
    {
        int gridWidth = (mapWidth + GridSize - 1) / GridSize;
        int gridHeight = (mapHeight + GridSize - 1) / GridSize;
        // byte 枚举默认值为 0（WindDirection.None），无需显式赋值
        return new WindDirection[gridWidth, gridHeight];
    }

    /// <summary>
    /// 初始化风力网格（全部设为无风）
    /// 日期：2026-03-10
    /// </summary>
    private static WindForce[,] InitializeWindForceGrid(int mapWidth, int mapHeight)
    {
        int gridWidth = (mapWidth + GridSize - 1) / GridSize;
        int gridHeight = (mapHeight + GridSize - 1) / GridSize;
        // byte 枚举默认值为 0（WindForce.None），无需显式赋值
        return new WindForce[gridWidth, gridHeight];
    }

    /// <summary>
    /// 构建州域到气候带的映射
    /// </summary>
    private static Dictionary<int, ClimateZone> BuildStateZoneMapping(GameEnvironmentConfig config)
    {
        var mapping = new Dictionary<int, ClimateZone>();
        
        foreach (var zone in config.Weather.StateZones)
        {
            // 🔥 关键：ID >= 0 是有效的（遵循 ID 判断规范）
            if (zone.StateID >= 0)
            {
                mapping[zone.StateID] = zone.Zone;
            }
        }
        
        return mapping;
    }

    /// <summary>
    /// 获取指定坐标的天气（O(1) 查询）
    /// 🔥 热路径：每帧可能被调用数百次
    /// </summary>
    public WeatherType GetWeatherAt(Point position)
    {
        int gridX = position.X / GridSize;
        int gridY = position.Y / GridSize;
        
        // 🔥 ANTI-BAND-AID：边界检查应该抛出异常，而不是静默返回默认值
        // 如果坐标超出范围，说明调用方有 bug，必须暴露问题
        if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"坐标 ({position.X}, {position.Y}) 超出地图范围！网格坐标：({gridX}, {gridY})，网格尺寸：({_gridWidth}, {_gridHeight})");
        }
        
        return _weatherGrid[gridX, gridY];
    }

    /// <summary>
    /// 获取指定坐标的风况（O(1) 查询）
    /// 🔥 热路径：每帧可能被调用数百次（粒子系统、AI 评分）
    /// 日期：2026-03-10
    /// </summary>
    public (WindDirection Direction, WindForce Force) GetWindAt(Point position)
    {
        int gridX = position.X / GridSize;
        int gridY = position.Y / GridSize;
        
        // 🔥 ANTI-BAND-AID：边界检查应该抛出异常
        if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"坐标 ({position.X}, {position.Y}) 超出地图范围！网格坐标：({gridX}, {gridY})，网格尺寸：({_gridWidth}, {_gridHeight})");
        }
        
        return (_windDirectionGrid[gridX, gridY], _windForceGrid[gridX, gridY]);
    }

    /// <summary>
    /// 更新天气（每回合或每天调用一次）
    /// 🧊 冷路径：使用 for 循环而非 LINQ（虽然允许 LINQ，但这里 for 更清晰）
    /// </summary>
    public void UpdateWeather(SeasonType currentSeason)
    {
        var fogDispersalStats = new FogDispersalStats();

        // TODO: 这里需要根据州域系统来更新天气
        // 当前实现：简单地为每个网格随机生成天气
        
        // 🔥 注意：这里使用 for 循环而非 LINQ，因为需要修改数组元素
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                // 获取当前网格的气候带（暂时使用默认值，后续需要根据州域查询）
                var zone = GetClimateZoneForGrid(x, y);
                
                // 计算下一个天气
                var current = _weatherGrid[x, y];
                _weatherGrid[x, y] = CalculateNextWeather(current, zone, currentSeason);
                
                // 🔥 同步更新风况（日期：2026-03-10）
                UpdateWindForGrid(x, y, currentSeason, ref fogDispersalStats);
            }
        }

        if (fogDispersalStats.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[\u6c14\u8c61\u7cfb\u7edf] \u672c\u8f6e\u5171\u6709 {fogDispersalStats.Count} \u4e2a\u5929\u6c14\u7f51\u683c\u56e0\u72c2\u98ce\u5439\u6563\u5927\u96fe\uff0c\u5f71\u54cd\u8303\u56f4 X[{fogDispersalStats.MinX}, {fogDispersalStats.MaxX}] Y[{fogDispersalStats.MinY}, {fogDispersalStats.MaxY}]\u3002");
        }
    }

    /// <summary>
    /// 更新指定网格的风况
    /// 日期：2026-03-10
    /// </summary>
    private void UpdateWindForGrid(int gridX, int gridY, SeasonType season, ref FogDispersalStats fogDispersalStats)
    {
        // 1. 风向惯性检查（类似于天气惯性）
        // 🔥 配置驱动：从配置读取风向惯性概率
        var currentDirection = _windDirectionGrid[gridX, gridY];
        if (currentDirection != WindDirection.None 
            && _rng.Next(100) < _config.Wind.DirectionInertiaChance)
        {
            // 风向保持不变，但仍需重新计算风力
            // 继续执行后续逻辑
        }
        else
        {
            // 2. 从配置读取当前季节的风向概率（AOT 友好的 switch 表达式）
            var directionProbs = season switch
            {
                SeasonType.Spring => _config.Wind.DirectionProbabilities.Spring,
                SeasonType.Summer => _config.Wind.DirectionProbabilities.Summer,
                SeasonType.Autumn => _config.Wind.DirectionProbabilities.Autumn,
                SeasonType.Winter => _config.Wind.DirectionProbabilities.Winter,
                _ => throw new InvalidOperationException($"未知季节类型：{season}")
            };
            
            int directionRoll = _rng.Next(100);
            
            // 🔥 使用配置驱动的风向判定（累加概率）
            int cumulative = 0;
            _windDirectionGrid[gridX, gridY] = WindDirection.None;  // 默认无风
            
            if ((cumulative += directionProbs.North) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.North;
            else if ((cumulative += directionProbs.South) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.South;
            else if ((cumulative += directionProbs.East) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.East;
            else if ((cumulative += directionProbs.West) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.West;
            else if ((cumulative += directionProbs.NorthWest) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.NorthWest;
            else if ((cumulative += directionProbs.NorthEast) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.NorthEast;
            else if ((cumulative += directionProbs.SouthWest) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.SouthWest;
            else if ((cumulative += directionProbs.SouthEast) > directionRoll)
                _windDirectionGrid[gridX, gridY] = WindDirection.SouthEast;
        }

        // 3. 根据季节和天气生成风力
        var weather = _weatherGrid[gridX, gridY];
        int forceRoll = _rng.Next(100);
        
        // 🔥 从配置读取季节修正（AOT 友好的 switch 表达式）
        int windChanceModifier = season switch
        {
            SeasonType.Spring => _config.Wind.SeasonModifiers.Spring,
            SeasonType.Summer => _config.Wind.SeasonModifiers.Summer,
            SeasonType.Autumn => _config.Wind.SeasonModifiers.Autumn,
            SeasonType.Winter => _config.Wind.SeasonModifiers.Winter,
            _ => 0
        };

        // 🔥 从配置读取天气修正（AOT 友好的 switch 表达式）
        windChanceModifier += weather switch
        {
            WeatherType.Rain => _config.Wind.WeatherModifiers.Rain,
            WeatherType.Snow => _config.Wind.WeatherModifiers.Snow,
            WeatherType.Fog => _config.Wind.WeatherModifiers.Fog,
            WeatherType.Cloudy => _config.Wind.WeatherModifiers.Cloudy,
            WeatherType.Sunny => _config.Wind.WeatherModifiers.Sunny,
            _ => 0
        };

        int adjustedRoll = forceRoll - windChanceModifier;
        
        // 🔥 使用配置驱动的风力判定（累加概率）
        var forceProbs = _config.Wind.ForceProbabilities;
        int cumulative2 = 0;
        
        if ((cumulative2 += forceProbs.None) > adjustedRoll)
            _windForceGrid[gridX, gridY] = WindForce.None;
        else if ((cumulative2 += forceProbs.Breeze) > adjustedRoll)
            _windForceGrid[gridX, gridY] = WindForce.Breeze;
        else if ((cumulative2 += forceProbs.Strong) > adjustedRoll)
            _windForceGrid[gridX, gridY] = WindForce.Strong;
        else
            _windForceGrid[gridX, gridY] = WindForce.Gale;

        // ---------------------------------------------------------
        // 🔥 4. 核心：气象互斥与修正（配置驱动）
        // 日期：2026-03-10
        // ---------------------------------------------------------
        
        var rules = _config.Wind.Rules;
        
        // 规则 A：大风吹散大雾（配置驱动）
        if (rules.EnableFogDispersal 
            && weather == WeatherType.Fog 
            && _windForceGrid[gridX, gridY] >= rules.FogDispersalMinWindForce)
        {
            // 雾被吹散，转为阴天或晴天（概率从配置读取）
            _weatherGrid[gridX, gridY] = _rng.Next(100) < rules.FogToSunnyChance 
                ? WeatherType.Sunny 
                : WeatherType.Cloudy;

            fogDispersalStats.Record(gridX, gridY);
        }

        // 规则 B：暴雨/大雪压制微风（配置驱动）
        // 🔥 注意：这里使用的是规则 A 之前的 weather 值
        // 如果规则 A 将雾天改为晴天，这里不会触发（这是正确的行为）
        if (rules.EnableExtremeWeatherWind
            && (weather == WeatherType.Rain || weather == WeatherType.Snow) 
            && _windForceGrid[gridX, gridY] < rules.ExtremeWeatherMinWindForce)
        {
            _windForceGrid[gridX, gridY] = rules.ExtremeWeatherMinWindForce;
            
            // 如果风向也是 None，随机给个风向
            if (_windDirectionGrid[gridX, gridY] == WindDirection.None)
            {
                // 🔥 C# 12 语法：使用强制转换而非 Enum.GetValues
                _windDirectionGrid[gridX, gridY] = (WindDirection)_rng.Next(1, 9);
            }
        }

        // 规则 C：数据一致性清洗
        // 无风时，风向也应该是 None
        if (_windForceGrid[gridX, gridY] == WindForce.None)
        {
            _windDirectionGrid[gridX, gridY] = WindDirection.None;
        }
    }

    /// <summary>
    /// 获取网格所属的气候带（TODO: 需要根据州域系统实现）
    /// </summary>
    private ClimateZone GetClimateZoneForGrid(int gridX, int gridY)
    {
        // TODO: 根据网格坐标查询所属州域，再查询气候带
        // 当前简化实现：根据地图位置粗略划分
        
        float normalizedY = (float)gridY / _gridHeight;
        
        if (normalizedY < 0.25f) return ClimateZone.Northern;
        if (normalizedY < 0.5f) return ClimateZone.Central;
        if (normalizedY < 0.75f) return ClimateZone.Southern;
        return ClimateZone.Western;
    }

    /// <summary>
    /// 计算下一个天气状态
    /// </summary>
    private WeatherType CalculateNextWeather(
        WeatherType current,
        ClimateZone climate,
        SeasonType season)
    {
        // 1. 惯性检查（独立随机数）
        if (current == WeatherType.Rain && _rng.Next(100) < _config.Weather.RainInertiaChance)
            return WeatherType.Rain;
        if (current == WeatherType.Snow && _rng.Next(100) < _config.Weather.SnowInertiaChance)
            return WeatherType.Snow;

        // 2. 匹配规则池
        int roll = _rng.Next(100);
        
        foreach (var rule in _config.Weather.ClimateRules)
        {
            if (rule.Zone != climate || rule.Season != season) continue;

            // 累加阈值，确保概率区间不重叠
            int threshold = 0;
            
            threshold += rule.RainChance;
            if (roll < threshold) return WeatherType.Rain;
            
            threshold += rule.CloudyChance;
            if (roll < threshold) return WeatherType.Cloudy;
            
            threshold += rule.FogChance;
            if (roll < threshold) return WeatherType.Fog;
            
            threshold += rule.SnowChance;
            if (roll < threshold) return WeatherType.Snow;

            // 剩余概率自动归为晴天
            return WeatherType.Sunny;
        }

        // 3. 兜底（未找到匹配规则）
        return roll switch
        {
            < 10 => WeatherType.Rain,
            < 30 => WeatherType.Cloudy,
            _ => WeatherType.Sunny
        };
    }

    /// <summary>
    /// 获取天气的中文显示名称
    /// </summary>
    public static string GetWeatherDisplayName(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Sunny => "晴",
            WeatherType.Cloudy => "阴",
            WeatherType.Rain => "雨",
            WeatherType.Snow => "雪",
            WeatherType.Fog => "雾",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取风向的中文显示名称
    /// 日期：2026-03-10
    /// </summary>
    public static string GetWindDirectionDisplayName(WindDirection direction)
    {
        return direction switch
        {
            WindDirection.None => "无风",
            WindDirection.North => "北风",
            WindDirection.South => "南风",
            WindDirection.East => "东风",
            WindDirection.West => "西风",
            WindDirection.NorthWest => "西北风",
            WindDirection.NorthEast => "东北风",
            WindDirection.SouthWest => "西南风",
            WindDirection.SouthEast => "东南风",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取风力的中文显示名称
    /// 日期：2026-03-10
    /// </summary>
    public static string GetWindForceDisplayName(WindForce force)
    {
        return force switch
        {
            WindForce.None => "无风",
            WindForce.Breeze => "微风",
            WindForce.Strong => "大风",
            WindForce.Gale => "狂风",
            _ => "未知"
        };
    }
}
