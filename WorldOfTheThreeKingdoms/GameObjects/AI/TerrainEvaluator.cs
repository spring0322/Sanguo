using System;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;

namespace GameObjects.AI;

/// <summary>
/// AI 战略态势枚举
/// </summary>
public enum StrategicPosture
{
    Attack,     // 进攻：主动寻找破绽，压制敌军
    Defense,    // 防守：依托有利地形，节节抗击
    Garrison    // 驻守：死守要道，稳如泰山（不轻易出击丢掉卡位点）
}

/// <summary>
/// 地形与要道评估器（高性能，全静态）
/// 🔥 Cold Path：AI 决策阶段调用，允许使用配置查询
/// </summary>
public static class TerrainEvaluator
{
    /// <summary>
    /// 获取角色与地形的基础匹配分数
    /// 🔥 使用 C# 12 模式匹配实现清晰的角色-地形评分
    /// </summary>
    public static float GetTerrainScore(TroopRole role, TerrainKind terrain)
    {
        // 🔥 ANTI-BAND-AID：配置必须在初始化时加载，不做防御性空检查
        var config = AITacticalConfigManager.Config.TacticalPositioning;

        string roleName = role switch
        {
            TroopRole.Tank => "Tank",
            TroopRole.DPS => "DPS",
            TroopRole.Mage => "Mage",
            TroopRole.Support => "Support",
            _ => null
        };

        if (roleName == null) return 0f;

        // 从配置中查询地形评分
        if (config.TerrainScores.TryGetValue(roleName, out var roleScores))
        {
            string terrainName = terrain.ToString();
            if (roleScores.TryGetValue(terrainName, out float score))
            {
                return score;
            }
        }

        return 0f;
    }

    /// <summary>
    /// 动态要道（Choke Point）检测算法
    /// 核心逻辑：检测目标格子的上下左右，如果形成"两边堵死，单线贯通"的通道，即判定为要道
    /// 极其适合在没有预设关卡的地图上动态寻找桥梁、峡口
    /// </summary>
    public static bool IsDynamicChokePoint(Point pos)
    {
        // 🔥 Anti-Band-Aid：直接访问，如果为null会崩溃，暴露游戏状态异常
        var scenario = Session.Current.Scenario;

        // 获取四周的通行状态
        bool up = IsWalkable(scenario, new Point(pos.X, pos.Y - 1));
        bool down = IsWalkable(scenario, new Point(pos.X, pos.Y + 1));
        bool left = IsWalkable(scenario, new Point(pos.X - 1, pos.Y));
        bool right = IsWalkable(scenario, new Point(pos.X + 1, pos.Y));

        // 判定条件：水平方向堵死且垂直贯通，或者垂直方向堵死且水平贯通
        bool isHorizontalPass = !up && !down && left && right;
        bool isVerticalPass = !left && !right && up && down;

        return isHorizontalPass || isVerticalPass;
    }

    /// <summary>
    /// 检查位置是否可通行
    /// </summary>
    private static bool IsWalkable(GameScenario scenario, Point pos)
    {
        // 边界检查
        if (scenario.PositionOutOfRange(pos))
            return false;

        // 获取地形类型
        TerrainKind terrain = scenario.GetTerrainKindByPosition(pos);

        // 水域通常不可通行（除非是水军）
        if (terrain == TerrainKind.水域)
            return false;

        // 检查是否有建筑占据
        if (scenario.GetArchitectureByPosition(pos) != null)
            return false;

        return true;
    }

    /// <summary>
    /// 获取战略态势下的要道加成
    /// </summary>
    public static float GetChokePointBonus(TroopRole role, StrategicPosture posture)
    {
        // 🔥 ANTI-BAND-AID：配置必须在初始化时加载，不做防御性空检查
        var config = AITacticalConfigManager.Config.TacticalPositioning;

        string postureName = posture switch
        {
            StrategicPosture.Attack => "Attack",
            StrategicPosture.Defense => "Defense",
            StrategicPosture.Garrison => "Garrison",
            _ => null
        };

        if (postureName == null) return 0f;

        if (config.StrategicPosture.TryGetValue(postureName, out var postureConfig))
        {
            if (postureConfig.ChokePointBonus == null)
                return 0f;

            string roleName = role switch
            {
                TroopRole.Tank => "Tank",
                TroopRole.DPS => "DPS",
                _ => null
            };

            if (roleName != null && postureConfig.ChokePointBonus.TryGetValue(roleName, out float bonus))
            {
                return bonus;
            }
        }

        return 0f;
    }

    /// <summary>
    /// 获取战略态势下的距离惩罚系数
    /// </summary>
    public static float GetDistancePenaltyMultiplier(StrategicPosture posture)
    {
        // 🔥 ANTI-BAND-AID：配置必须在初始化时加载，不做防御性空检查
        var config = AITacticalConfigManager.Config.TacticalPositioning;

        string postureName = posture switch
        {
            StrategicPosture.Attack => "Attack",
            StrategicPosture.Defense => "Defense",
            StrategicPosture.Garrison => "Garrison",
            _ => null
        };

        if (postureName != null && 
            config.StrategicPosture.TryGetValue(postureName, out var postureConfig))
        {
            return postureConfig.DistancePenaltyMultiplier;
        }

        return 0.1f;
    }
}
