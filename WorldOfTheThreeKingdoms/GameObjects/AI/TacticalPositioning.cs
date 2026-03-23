using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;

/// <summary>
/// ZOC战术卡位选择器
/// 根据角色分配最佳战术位置
/// </summary>
public static class TacticalPositioning
{
    /// <summary>
    /// 获取最佳的卡位/ZOC压制坐标（增强版：支持地形和战略态势）
    /// </summary>
    /// <param name="activeTroop">当前行动的部队（不能为 null）</param>
    /// <param name="enemies">视野内/局部的敌军列表（不能为 null）</param>
    /// <param name="allies">局部的友军列表（不能为 null）</param>
    /// <param name="posture">战略态势（进攻/防守/驻守）</param>
    /// <returns>计算出的最佳坐标，如果无合适位置返回原位</returns>
    public static Point2D GetBestTacticalPosition(
        Troop activeTroop, 
        IReadOnlyList<Troop> enemies, 
        IReadOnlyList<Troop> allies,
        StrategicPosture posture = StrategicPosture.Attack)
    {
        // 🔥 数据契约：调用方必须保证参数非 null
        // 如果这里崩溃，说明调用方的数据流有问题，需要追溯源头
        if (enemies.Count == 0)
            return activeTroop.Position;

        var config = AITacticalConfigManager.Config.TacticalPositioning;
        
        // 1. 计算矩形包围盒 (Bounding Box)
        Point2D currentPos = activeTroop.Position;
        var (minX, maxX, minY, maxY) = CalculateBoundingBox(currentPos, enemies, config.BoundingBoxExpansion);

        Point2D bestPosition = currentPos;
        float highestScore = float.MinValue;
        int mobility = activeTroop.Movability;

        // 获取战略态势下的距离惩罚系数
        float distancePenalty = TerrainEvaluator.GetDistancePenaltyMultiplier(posture);

        // 2. 遍历移动范围与包围盒的交集
        for (int x = Math.Max(minX, currentPos.X - mobility); 
             x <= Math.Min(maxX, currentPos.X + mobility); 
             x++)
        {
            for (int y = Math.Max(minY, currentPos.Y - mobility); 
                 y <= Math.Min(maxY, currentPos.Y + mobility); 
                 y++)
            {
                Point2D targetPos = new(x, y);

                // 🔥 基础物理验证：地形是否可走
                if (!IsPositionWalkable(targetPos))
                    continue;

                // 3. 基础战术得分（ZOC 逻辑）
                float score = EvaluatePositionScore(activeTroop, targetPos, enemies, allies);

                // 4. 地形属性加成
                var terrain = GetTerrainAt(targetPos);
                score += TerrainEvaluator.GetTerrainScore(activeTroop.CurrentRole, terrain);

                // 5. 战略态势与要道控制（核心新增逻辑）
                if (TerrainEvaluator.IsDynamicChokePoint(targetPos))
                {
                    // 如果是驻守或防守状态，肉盾和部分强力 DPS 会死死卡住要道
                    if (posture is StrategicPosture.Defense or StrategicPosture.Garrison)
                    {
                        float chokeBonus = TerrainEvaluator.GetChokePointBonus(activeTroop.CurrentRole, posture);
                        score += chokeBonus;
                    }
                }
                else if (posture == StrategicPosture.Garrison)
                {
                    // 驻守态势下，严禁所有部队乱跑，大幅度惩罚离开原位的行为
                    score -= currentPos.ManhattanDistance(targetPos) * 100f;
                }

                // 距离惩罚（省机动力，战略态势影响系数）
                score -= currentPos.ManhattanDistance(targetPos) * distancePenalty;

                if (score > highestScore)
                {
                    highestScore = score;
                    bestPosition = targetPos;
                }
            }
        }

        return highestScore > float.MinValue ? bestPosition : currentPos;
    }
    /// <summary>
    /// 评估位置分数（公开接口，供UnifiedTacticalAI使用）
    /// </summary>
    public static float EvaluatePositionScore(
        Point2D position,
        Troop troop,
        IReadOnlyList<Troop> enemies,
        IReadOnlyList<Troop> allies,
        StrategicPosture posture)
    {
        if (troop == null)
        {
            return float.MinValue;
        }

        TroopRole role = troop.CurrentRole;
        if (role == TroopRole.None)
        {
            role = AIRoleSelector.DetermineRole(troop);
        }

        // 基础战术得分
        float score = EvaluatePositionScore(troop, position, enemies, allies);

        // 地形加成
        var terrain = GetTerrainAt(position);
        score += TerrainEvaluator.GetTerrainScore(role, terrain);

        // 战略态势加成
        if (TerrainEvaluator.IsDynamicChokePoint(position))
        {
            if (posture is StrategicPosture.Defense or StrategicPosture.Garrison)
            {
                float chokeBonus = TerrainEvaluator.GetChokePointBonus(role, posture);
                score += chokeBonus;
            }
        }

        // 距离惩罚
        float distancePenalty = TerrainEvaluator.GetDistancePenaltyMultiplier(posture);
        Point2D troopPos = troop.Position;
        score -= troopPos.ManhattanDistance(position) * distancePenalty;

        return score;
    }

    /// <summary>
    /// 核心评分器：根据角色采用不同的战术评估逻辑
    /// </summary>
    private static float EvaluatePositionScore(
        Troop troop, 
        Point2D pos, 
        IReadOnlyList<Troop> enemies, 
        IReadOnlyList<Troop> allies)
    {
        return troop.CurrentRole switch
        {
            TroopRole.Tank => EvaluateTankScore(pos, enemies, allies),
            TroopRole.DPS => EvaluateDpsZocScore(troop, pos, enemies),
            TroopRole.Support or TroopRole.Mage => EvaluateSafePositionScore(pos, enemies, allies),
            _ => 0f
        };
    }

    /// <summary>
    /// Tank 卡位逻辑：找敌军与我方脆皮的连线点
    /// </summary>
    private static float EvaluateTankScore(
        Point2D pos, 
        IReadOnlyList<Troop> enemies, 
        IReadOnlyList<Troop> allies)
    {
        var config = AITacticalConfigManager.GetRoleScoreConfig("Tank");
        float score = 0;

        foreach (var ally in allies)
        {
            if (ally.CurrentRole is TroopRole.Mage or TroopRole.Support)
            {
                Point2D allyPos = ally.Position;
                
                foreach (var enemy in enemies)
                {
                    Point2D enemyPos = enemy.Position;
                    
                    int distEnemyToAlly = enemyPos.ManhattanDistance(allyPos);
                    int distPosToAlly = pos.ManhattanDistance(allyPos);
                    int distPosToEnemy = pos.ManhattanDistance(enemyPos);

                    // 拦截判定：点在敌我连线的曼哈顿路径上
                    if (distPosToAlly + distPosToEnemy == distEnemyToAlly)
                    {
                        score += config.ProtectLineBonus;
                    }

                    // ZOC 判定：贴脸（距离1）限制敌方机动
                    if (distPosToEnemy == config.MinDistanceToEnemy)
                    {
                        score += config.ZocLockBonus;
                    }
                }
            }
        }

        // 抱团加成：靠近其他坦克
        foreach (var ally in allies)
        {
            if (ally.CurrentRole == TroopRole.Tank)
            {
                int dist = pos.ManhattanDistance(ally.Position);
                if (dist <= 2)
                {
                    score += config.ClusterBonus;
                }
            }
        }

        return score;
    }

    /// <summary>
    /// DPS (包含骑兵/弓兵) 压制逻辑：利用 ZOC 废除敌方冲锋/远程
    /// </summary>
    private static float EvaluateDpsZocScore(
        Troop troop,
        Point2D pos, 
        IReadOnlyList<Troop> enemies)
    {
        var config = AITacticalConfigManager.GetRoleScoreConfig("DPS");
        float score = 0;

        foreach (var enemy in enemies)
        {
            int kindID = GetTroopKindID(enemy);
            
            // 检查是否需要 ZOC 压制的兵种
            if (AITacticalConfigManager.RequiresZocSuppression(kindID))
            {
                int dist = pos.ManhattanDistance(enemy.Position);
                
                if (dist == 1)
                {
                    score += config.ZocLockBonus;
                }
            }
        }

        return score;
    }

    /// <summary>
    /// 脆皮 (法师/辅助) 的避战走位逻辑
    /// </summary>
    private static float EvaluateSafePositionScore(
        Point2D pos, 
        IReadOnlyList<Troop> enemies, 
        IReadOnlyList<Troop> allies)
    {
        var config = AITacticalConfigManager.GetRoleScoreConfig("Support");
        float score = 0;

        // 远离所有敌军
        foreach (var enemy in enemies)
        {
            Point2D enemyPos = enemy.Position;
            int dist = pos.ManhattanDistance(enemyPos);
            
            if (dist <= 2)
            {
                score += config.DangerZonePenalty;
            }
            else
            {
                score += dist * config.AllyProximityBonus;
            }
        }

        // 尽量靠近 Tank 寻求保护
        foreach (var ally in allies)
        {
            if (ally.CurrentRole == TroopRole.Tank)
            {
                int dist = pos.ManhattanDistance(ally.Position);
                
                if (dist == 1)
                {
                    score += config.TankProximityBonus;
                }
            }
        }

        return score;
    }

    /// <summary>
    /// 计算包含中心点及目标群的矩形包围盒，避免全图搜索
    /// </summary>
    private static (int MinX, int MaxX, int MinY, int MaxY) CalculateBoundingBox(
        Point2D center, 
        IReadOnlyList<Troop> entities,
        int expansion)
    {
        int minX = center.X, maxX = center.X;
        int minY = center.Y, maxY = center.Y;

        // 循环比 LINQ 的 Min/Max 更适合 AOT 且减少迭代器开销
        for (int i = 0; i < entities.Count; i++)
        {
            Point2D pos = entities[i].Position;
            
            if (pos.X < minX) minX = pos.X;
            if (pos.X > maxX) maxX = pos.X;
            if (pos.Y < minY) minY = pos.Y;
            if (pos.Y > maxY) maxY = pos.Y;
        }

        // 外扩作为战术纵深缓冲
        return (minX - expansion, maxX + expansion, minY - expansion, maxY + expansion);
    }

    /// <summary>
    /// 获取部队兵种ID
    /// </summary>
    private static int GetTroopKindID(Troop troop)
    {
        // 🔥 Anti-Band-Aid：直接访问，不做防御性检查
        // 如果为null会崩溃，暴露数据初始化问题，这是正确的行为
        return troop.Army.Kind.ID;
    }

    /// <summary>
    /// 检查位置是否可通行
    /// </summary>
    private static bool IsPositionWalkable(Point2D pos)
    {
        // 🔥 Anti-Band-Aid：直接访问，不做防御性检查
        // 如果Scenario为null会崩溃，暴露游戏状态异常，这是正确的行为
        var scenario = GameManager.Session.Current.Scenario;
        Point point = pos;

        // 边界检查（这是业务逻辑，不是防御性检查）
        if (scenario.PositionOutOfRange(point))
            return false;

        // 获取地形类型
        var terrain = scenario.GetTerrainKindByPosition(point);

        // 水域通常不可通行（除非是水军，这里简化处理）
        if (terrain == WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.水域)
            return false;

        // 检查是否有建筑占据（业务逻辑检查）
        if (scenario.GetArchitectureByPosition(point) != null)
            return false;

        // 检查是否有其他部队占据（业务逻辑检查）
        if (scenario.GetTroopByPosition(point) != null)
            return false;

        return true;
    }

    /// <summary>
    /// 获取位置的地形类型
    /// </summary>
    private static WorldOfTheThreeKingdoms.GameGlobal.TerrainKind GetTerrainAt(Point2D pos)
    {
        // 🔥 Anti-Band-Aid：直接访问，不做防御性检查
        // 如果Scenario为null会崩溃，暴露游戏状态异常，这是正确的行为
        var scenario = GameManager.Session.Current.Scenario;
        Point point = pos;
        return scenario.GetTerrainKindByPosition(point);
    }
}
