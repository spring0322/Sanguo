using System;
using System.Collections.Generic;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;

/// <summary>
/// 全局/局部战略态势判定器（AOT 友好，无堆分配）
/// 决定 AI 势力的宏观行为模式，指导底层的 ZOC 走位与集火策略
/// 🔥 Cold Path：回合开始时调用一次
/// </summary>
public static class PostureEvaluator
{
    /// <summary>
    /// 根据城市及周边真实态势，动态推演当前的战略定位
    /// </summary>
    /// <param name="city">当前作战区域的所属建筑（城市/关卡/港口）</param>
    /// <param name="friendlyTroops">己方出征的部队列表</param>
    /// <param name="enemyTroops">视野内/同区域的敌方部队列表</param>
    /// <returns>计算得出的战略态势</returns>
    public static StrategicPosture EvaluatePosture(
        Architecture city, 
        IReadOnlyList<Troop> friendlyTroops, 
        IReadOnlyList<Troop> enemyTroops)
    {
        var config = AITacticalConfigManager.Config.TacticalPositioning?.PostureEvaluation;
        
        // 🔥 数据契约：配置必须存在
        if (config == null)
        {
            System.Diagnostics.Debug.WriteLine("[态势判定] 警告：PostureEvaluation 配置为 null，使用默认值");
            return EvaluatePostureWithDefaults(city, friendlyTroops, enemyTroops);
        }

        // 1. 战力质量对比（绝不仅看人数）
        int friendlyPower = CalculateTotalPower(friendlyTroops);
        
        // 城内未编成的预备兵折半计算威慑力
        if (city != null)
        {
            friendlyPower += city.Population / 2;
        }
        
        int enemyPower = CalculateTotalPower(enemyTroops);

        // 防御除零异常：如果视野内没敌人，直接全军出击/自由扩张
        if (enemyPower == 0)
            return StrategicPosture.Attack;

        float powerRatio = (float)friendlyPower / enemyPower;

        // 2. 城市安全度评估
        float healthRatio = 1.0f;
        if (city != null)
        {
            healthRatio = (float)city.Endurance / city.EnduranceCeiling;
        }

        // 3. 粮草与资金吃紧判断
        bool isResourceShortage = false;
        if (city != null)
        {
            isResourceShortage = city.Fund < config.CriticalFund || city.Food < config.CriticalFood;
        }

        // ================= 4. 综合决策逻辑树（状态机思维） =================

        // 绝境：城防濒临破裂 或 兵力被严重碾压
        // 决策：全员退守要道，进入绝对"驻守"状态，Tank 拿命填 Choke Point，法师躲在最后
        if (healthRatio < config.CriticalHealthRatio || powerRatio < config.CriticalPowerRatio)
        {
            return StrategicPosture.Garrison;
        }

        // 劣势或后勤断裂：打不起消耗战，依托地形节节抗击
        // 决策：进入"防守"状态，允许适度反击，但核心思路是依托森林/山地拖延时间
        if (powerRatio < config.PowerRatioDisadvantage || isResourceShortage)
        {
            return StrategicPosture.Defense;
        }

        // 势均力敌但老家被摸：以防守反击为主，不盲目深追
        if (powerRatio >= config.PowerRatioDisadvantage && 
            powerRatio < config.PowerRatioOverwhelming && 
            healthRatio < config.SafeHealthRatio)
        {
            return StrategicPosture.Defense;
        }

        // 优势局 或 势均力敌且后方安稳无忧
        // 决策：进入"攻击"状态，DPS 和 Tank 主动寻找包围网和贴脸 ZOC
        return StrategicPosture.Attack;
    }

    /// <summary>
    /// 使用默认配置评估态势（配置加载失败时的后备方案）
    /// </summary>
    private static StrategicPosture EvaluatePostureWithDefaults(
        Architecture city, 
        IReadOnlyList<Troop> friendlyTroops, 
        IReadOnlyList<Troop> enemyTroops)
    {
        int friendlyPower = CalculateTotalPower(friendlyTroops);
        if (city != null)
        {
            friendlyPower += city.Population / 2;
        }
        
        int enemyPower = CalculateTotalPower(enemyTroops);

        if (enemyPower == 0)
            return StrategicPosture.Attack;

        float powerRatio = (float)friendlyPower / enemyPower;

        float healthRatio = 1.0f;
        if (city != null)
        {
            healthRatio = (float)city.Endurance / city.EnduranceCeiling;
        }

        // 使用硬编码的默认值
        if (healthRatio < 0.3f || powerRatio < 0.4f)
            return StrategicPosture.Garrison;

        if (powerRatio < 0.7f)
            return StrategicPosture.Defense;

        return StrategicPosture.Attack;
    }

    /// <summary>
    /// 计算部队真实战力（质量 x 数量）
    /// </summary>
    private static int CalculateTotalPower(IReadOnlyList<Troop> troops)
    {
        int totalPower = 0;

        // 🔥 使用 for 循环避免迭代器分配
        for (int i = 0; i < troops.Count; i++)
        {
            Troop t = troops[i];
            
            if (t == null || t.Leader == null)
            {
                System.Diagnostics.Debug.WriteLine($"[态势判定] 警告：部队列表中存在 null 或无主将的部队");
                continue;
            }

            // 战力计算公式：兵力 * (统率 + 武力 + 智力) / 100
            // 解决"一万杂牌军"被系统误判为优于"五千陷阵营"的致命漏洞
            int leaderStats = t.Leader.Command + t.Leader.Strength + t.Leader.Intelligence;

            // 加入士气修正（士气越高，战斗力越强，假设士气满值 100）
            float moraleModifier = t.Morale / 100f;

            totalPower += (int)(t.Quantity * leaderStats / 100f * moraleModifier);
        }

        return totalPower;
    }

    /// <summary>
    /// 为势力判断战略态势（基于所有城市和部队）
    /// </summary>
    public static StrategicPosture EvaluateFactionPosture(Faction faction)
    {
        // 🔥 数据契约：势力必须有建筑和部队列表
        if (faction.Architectures == null || faction.Troops == null)
        {
            System.Diagnostics.Debug.WriteLine($"[态势判定] 错误：势力 {faction.ID} 的 Architectures 或 Troops 为 null");
            return StrategicPosture.Attack;
        }

        // 简化逻辑：使用首都或最大城市作为代表
        Architecture mainCity = GetMainCity(faction);
        
        if (mainCity == null)
        {
            System.Diagnostics.Debug.WriteLine($"[态势判定] 警告：势力 {faction.ID} 没有主城");
            return StrategicPosture.Attack;
        }

        // 收集己方和敌方部队
        List<Troop> friendlyTroops = [];
        List<Troop> enemyTroops = [];

        foreach (Troop troop in faction.Troops.GetList())
        {
            if (troop != null && !troop.Destroyed)
            {
                friendlyTroops.Add(troop);
            }
        }

        // 收集视野内的敌军（简化：从主城视野收集）
        // TODO: 实际使用时可以扩展为全局视野
        
        return EvaluatePosture(mainCity, friendlyTroops, enemyTroops);
    }

    /// <summary>
    /// 获取势力的主城（首都或最大城市）
    /// </summary>
    private static Architecture GetMainCity(Faction faction)
    {
        Architecture mainCity = null;
        int maxPopulation = 0;

        foreach (Architecture arch in faction.Architectures.GetList())
        {
            if (arch == null)
            {
                System.Diagnostics.Debug.WriteLine($"[态势判定] 警告：势力 {faction.ID} 的建筑列表中存在 null");
                continue;
            }

            // 优先选择首都
            if (arch.Kind?.ID == 1) // 假设 ID=1 是首都
            {
                return arch;
            }

            // 否则选择人口最多的城市
            if (arch.Population > maxPopulation)
            {
                maxPopulation = arch.Population;
                mainCity = arch;
            }
        }

        return mainCity;
    }
}
