using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.TroopDetail;
using GameManager;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;

/// <summary>
/// 能力评估器 - 评估部队的综合潜力
/// </summary>
public static class CapabilityEvaluator
{
    /// <summary>
    /// 评估部队的综合潜力向量
    /// ✅ Anti-Band-Aid：不做防御性空检查，让调用方保证数据有效性
    /// </summary>
    public static RoleProfile EvaluateProfile(Troop troop)
    {
        // 复用现有的角色评分逻辑
        float tankScore = CalculateTankScore(troop);
        float dpsScore = CalculateDpsScore(troop);
        float mageScore = CalculateMageScore(troop);
        float supportScore = CalculateSupportScore(troop);

        return new RoleProfile(tankScore, dpsScore, mageScore, supportScore);
    }

    /// <summary>
    /// 计算坦克倾向分数
    /// </summary>
    private static float CalculateTankScore(Troop troop)
    {
        float score = 0;

        // 基础属性
        score += troop.Leader.Command * 0.8f;
        score += troop.Leader.Strength * 0.5f;
        score += troop.Defence * 0.3f;

        // 兵种加成
        // 🔥 Anti-Band-Aid：直接访问，如果为null会崩溃，暴露数据问题
        if (troop.Army.Kind.Type == MilitaryType.步兵)
        {
            score += 30f;
        }

        // 士气和规模
        score += troop.Morale * 0.2f;
        score += Math.Min(troop.Quantity / 100f, 50f);

        return score;
    }

    /// <summary>
    /// 计算DPS倾向分数
    /// </summary>
    private static float CalculateDpsScore(Troop troop)
    {
        float score = 0;

        // 基础属性
        score += troop.Leader.Strength * 1.0f;
        score += troop.Leader.Command * 0.3f;
        score += troop.Offence * 0.5f;

        // 兵种加成
        // 🔥 Anti-Band-Aid：直接访问，如果为null会崩溃，暴露数据问题
        if (troop.Army.Kind.Type == MilitaryType.骑兵)
        {
            score += 40f;
        }
        else if (troop.Army.Kind.Type == MilitaryType.弩兵)
        {
            score += 35f;
        }

        // 士气和规模
        score += troop.Morale * 0.3f;
        score += Math.Min(troop.Quantity / 80f, 60f);

        return score;
    }

    /// <summary>
    /// 计算法师倾向分数
    /// </summary>
    private static float CalculateMageScore(Troop troop)
    {
        float score = 0;

        // 基础属性
        score += troop.Leader.Intelligence * 1.2f;
        score += troop.Leader.Command * 0.2f;

        // 计略数量加成
        if (troop.Stratagems != null)
        {
            score += troop.Stratagems.Stratagems.Count * 15f;
        }

        // 士气（施放计略需要士气）
        score += troop.Morale * 0.4f;

        return score;
    }

    /// <summary>
    /// 计算辅助倾向分数
    /// </summary>
    private static float CalculateSupportScore(Troop troop)
    {
        float score = 0;

        // 基础属性
        score += troop.Leader.Intelligence * 0.6f;
        score += troop.Leader.Command * 0.6f;
        score += troop.Leader.Politics * 0.4f;

        // 特殊技能加成（如果有辅助类技能）
        // TODO: 根据实际技能系统调整

        return score;
    }
}

/// <summary>
/// Utility AI 执行器 - 为部队寻找当前局势下的全局最优解
/// 🔥 注意：这是一个高级AI模块，计算开销较大
/// 建议仅在关键决策点使用，或者为玩家提供AI建议
/// </summary>
public static class UtilityAIExecutor
{
    /// <summary>
    /// 为部队寻找最佳行动方案
    /// ✅ Anti-Band-Aid：不做防御性空检查
    /// 🧊 Cold Path：AI决策，允许LINQ，优先可读性
    /// </summary>
    public static ActionProposal FindBestAction(
        Troop activeTroop, 
        IReadOnlyList<Troop> enemies, 
        IReadOnlyList<Troop> allies,
        StrategicPosture posture)
    {
        // 评估部队能力倾向
        RoleProfile profile = CapabilityEvaluator.EvaluateProfile(activeTroop);

        ActionProposal bestProposal = new(
            activeTroop.Position, 
            null, 
            ActionType.Wait, 
            0, 
            float.MinValue
        );

        // 获取可移动范围
        var validPositions = GetValidMovePositions(activeTroop, enemies);

        // 穷举：每一个能走的位置 -> 每一个能打的目标 -> 每一个能用的技能
        foreach (var pos in validPositions)
        {
            // 1. 评估位置的基础战术价值
            float posScore = TacticalPositioning.EvaluatePositionScore(
                pos, 
                activeTroop, 
                enemies, 
                allies, 
                posture
            );

            foreach (var target in enemies)
            {
                // 距离校验：在这个位置，能打到这个目标吗？
                if (!IsInRange(pos, target.Position, activeTroop.OffenceRadius))
                {
                    continue;
                }

                // 2. 评估各种行动的效用分
                
                // 尝试普通攻击
                float normalAtkScore = EvaluateAttackUtility(
                    activeTroop, 
                    target, 
                    profile.DpsAptitude,
                    in profile
                ) + posScore;

                if (normalAtkScore > bestProposal.UtilityScore)
                {
                    bestProposal = new ActionProposal(
                        pos, 
                        target, 
                        ActionType.NormalAttack, 
                        0, 
                        normalAtkScore
                    );
                }

                // 尝试战法（如果士气足够）
                if (activeTroop.Morale >= 20)
                {
                    float tacticScore = EvaluateTacticUtility(
                        activeTroop, 
                        target, 
                        profile.DpsAptitude
                    ) + posScore;

                    if (tacticScore > bestProposal.UtilityScore)
                    {
                        bestProposal = new ActionProposal(
                            pos, 
                            target, 
                            ActionType.Tactic, 
                            1, 
                            tacticScore
                        );
                    }
                }

                // 尝试计略（如果智力够高且有士气）
                if (activeTroop.Morale >= 15 && profile.MageAptitude > 50)
                {
                    float stratagemScore = EvaluateStratagemUtility(
                        activeTroop, 
                        target, 
                        profile.MageAptitude
                    ) + posScore;

                    if (stratagemScore > bestProposal.UtilityScore)
                    {
                        bestProposal = new ActionProposal(
                            pos, 
                            target, 
                            ActionType.Stratagem, 
                            11, 
                            stratagemScore
                        );
                    }
                }
            }

            // ================= 辅助性效用穷举（效用AI的精髓）=================
            // 如果这支部队有辅助倾向，并且有士气，它会考虑给队友加血或加buff
            if (profile.SupportAptitude > 40 && activeTroop.Morale >= 15)
            {
                foreach (var ally in allies)
                {
                    // 检查是否在施法距离内（假设辅助技能距离与攻击距离相同）
                    if (!IsInRange(pos, ally.Position, activeTroop.OffenceRadius))
                    {
                        continue;
                    }

                    float supportScore = EvaluateSupportUtility(
                        activeTroop, 
                        ally, 
                        profile.SupportAptitude
                    ) + posScore;

                    if (supportScore > bestProposal.UtilityScore)
                    {
                        // 注意：这里使用ally作为目标，而不是enemy
                        bestProposal = new ActionProposal(
                            pos, 
                            ally, 
                            ActionType.Stratagem, 
                            12, // 假设12是治疗/恢复的ID（需要从配置读取）
                            supportScore
                        );
                    }
                }
            }
        }

        return bestProposal;
    }

    /// <summary>
    /// 获取有效的移动位置
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// </summary>
    private static List<Point2D> GetValidMovePositions(
        Troop troop, 
        IReadOnlyList<Troop> enemies)
    {
        Point2D currentPos = troop.Position;
        int moveRange = Math.Min(troop.MovabilityLeft / 20, 5);

        // 使用LINQ生成候选位置（Cold Path优先可读性）
        var candidates = from dx in Enumerable.Range(-moveRange, moveRange * 2 + 1)
                        from dy in Enumerable.Range(-moveRange, moveRange * 2 + 1)
                        let candidate = new Point2D(currentPos.X + dx, currentPos.Y + dy)
                        where !Session.Current.Scenario.PositionOutOfRange((Point)candidate)
                        where !enemies.Any(e => (Point2D)e.Position == candidate)
                        select candidate;

        return [.. candidates];
    }

    /// <summary>
    /// 评估计略效用
    /// </summary>
    private static float EvaluateStratagemUtility(
        Troop attacker, 
        Troop target, 
        float mageAptitude)
    {
        float score = 0;

        // 动态判断：如果目标已经被混乱，放控制技能的收益直接归零
        if (!HasStatus(target, 391))
        {
            // 收益 = 法师潜力 * 目标的脆弱度
            float successRate = Math.Max(0, attacker.Leader.Intelligence - target.Leader.Intelligence);
            score = mageAptitude * 2.0f + successRate * 10f;
        }
        else
        {
            // 目标已被控，转而评估放火的收益
            score = mageAptitude * 1.5f + 100f; // 简化的火焰伤害评估
        }

        return score;
    }

    /// <summary>
    /// 评估战法效用
    /// </summary>
    private static float EvaluateTacticUtility(
        Troop attacker, 
        Troop target, 
        float dpsAptitude)
    {
        float score = dpsAptitude * 1.5f;

        // 动态判断：如果目标处于被控状态或被Tank卡死，物理输出收益极高
        if (HasStatus(target, 391))
        {
            score += 2000f; // 痛打落水狗加成
        }

        // 斩杀线诱惑：如果这发战法能直接把对面带走，分数拉满
        if (target.Quantity < 1500)
        {
            score += 3000f;
        }

        return score;
    }

    /// <summary>
    /// 评估普通攻击效用
    /// 🔥 增强版：区分Tank和DPS的普攻收益，考虑目标状态
    /// </summary>
    private static float EvaluateAttackUtility(
        Troop attacker, 
        Troop target, 
        float dpsAptitude,
        in RoleProfile profile)
    {
        // 普攻的基准分由 DPS 倾向和 Tank 倾向共同决定
        // Tank 绝大多数时候都在平A，所以Tank倾向权重更高
        float score = (profile.DpsAptitude * 0.8f) + (profile.TankAptitude * 1.0f);

        // 基于目标剩余兵力的评估
        // 防止存档/运行时异常数据导致除零，避免 NaN 传播污染效用评分
        int targetMaxQuantity = Math.Max(1, target.Army.Quantity);
        float healthRatio = (float)target.Quantity / targetMaxQuantity;
        score += (1.0f - healthRatio) * 500f; // 优先攻击残血

        // 如果对面处于混乱状态，普攻收益也会增加（但不如战法加得多）
        if (HasStatus(target, 391))
        {
            score += 500f;
        }

        return score;
    }

    /// <summary>
    /// 检查部队是否有指定状态
    /// 🧊 Cold Path：使用LINQ提高可读性
    /// ✅ Anti-Band-Aid：第一个?.是业务逻辑（无影响列表=无状态）
    /// 🔥 第二个?.已移除，如果Kind为null会崩溃，暴露数据初始化问题
    /// </summary>
    private static bool HasStatus(Troop troop, int statusId)
    {
        // 业务逻辑：如果部队没有影响列表，说明没有任何状态
        if (troop.InfluencesApplying == null)
        {
            return false;
        }

        // 🔥 Anti-Band-Aid：直接访问Kind.ID，如果Kind为null会崩溃
        // 这会暴露Influence对象初始化时的数据问题
        return troop.InfluencesApplying.Any(inf => inf.Kind.ID == statusId);
    }

    /// <summary>
    /// 检查是否在攻击范围内
    /// </summary>
    private static bool IsInRange(Point2D from, Point2D to, int range)
    {
        return from.ManhattanDistance(to) <= range;
    }

    /// <summary>
    /// 🔥 新增：评估辅助/治疗的效用
    /// 这是效用AI的核心：让辅助单位能够智能判断何时治疗比攻击更有价值
    /// </summary>
    private static float EvaluateSupportUtility(
        Troop attacker, 
        Troop target, 
        float supportAptitude)
    {
        // 如果队友满血，治疗收益为负数（避免浪费行动）
        int targetMaxQuantity = Math.Max(1, target.Army.Quantity);

        if (target.Quantity >= targetMaxQuantity)
        {
            return -1000f;
        }

        // 队友损失的兵力越多，治疗的效用分呈指数级飙升
        float lostRatio = 1.0f - ((float)target.Quantity / targetMaxQuantity);

        // 基础分数 = 辅助倾向 * 2.0 + 损失比例 * 3000
        // 假设残血队友就在身边，这个分数可能会瞬间碾压去打人的分数
        float score = supportAptitude * 2.0f + (lostRatio * 3000f);

        // 额外考虑：如果队友是关键角色（Tank或高价值DPS），治疗优先级更高
        if (target.CurrentRole == TroopRole.Tank)
        {
            score += 500f; // Tank是团队的盾牌，优先保护
        }
        else if (target.CurrentRole == TroopRole.DPS && target.Leader.Strength > 80)
        {
            score += 300f; // 高武力DPS也值得保护
        }

        return score;
    }
}
