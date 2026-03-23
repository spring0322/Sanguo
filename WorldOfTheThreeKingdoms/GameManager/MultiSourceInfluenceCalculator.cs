// ============================================================
// 文件: WorldOfTheThreeKingdoms/GameManager/MultiSourceInfluenceCalculator.cs
// 创建日期: 2026-03-11
// 修改日期: 2026-03-13
// 功能: 多源 Max-Remaining Energy 势力范围计算器（阶段 3 优化）
// ============================================================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameObjects;
using GameObjects.FactionDetail;
using Microsoft.Xna.Framework;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 🆕 阶段 3：多源 Max-Remaining Energy 势力范围计算器
/// 一次性计算整个势力的能量分布
/// 🧊 Cold Path：天气变化或建筑变化时调用
/// </summary>
public static class MultiSourceInfluenceCalculator
{
    // 🔥 全局复用的优先队列（避免分配）
    // 🚨 关键：使用 Max-Heap（负能量作为优先级）
    private static readonly PriorityQueue<int, int> _frontier = new();
    
    // 🆕 源建筑 ID 记录（主权防波堤系统）
    // 日期：2026-03-13
    // 用途：记录每个格子的能量来自哪个建筑，用于判断是否跨城扩散
    // 🔥 关键：ID=0（洛阳）是有效的
    // 🔥 2026-03-14：改为 Dictionary，按势力分别存储城池归属
    private static readonly Dictionary<int, int[]> _sourceArchitectureMaps = new();
    
    /// <summary>
    /// 🔥 2026-03-14：获取指定势力在指定地块的源建筑 ID
    /// </summary>
    public static int GetSourceArchitectureId(int factionId, int tileIndex)
    {
        if (_sourceArchitectureMaps.TryGetValue(factionId, out int[] map))
        {
            return map[tileIndex];
        }
        return -1;
    }
    
    /// <summary>
    /// 🆕 2026-03-20：注入部队威压能量到全局地图（带扩散）
    /// 🧊 Cold Path：势力范围更新时调用
    /// 
    /// 核心逻辑：
    /// - 部队能量从所在格子向周围扩散
    /// - 使用与城池相同的 Max-Remaining Energy 扩散算法
    /// - 实现"部队推进导致边界变化"的需求
    /// - 🆕 部队扩散受天气影响更大（通过地形代价倍率）
    /// </summary>
    /// <param name="faction">部队所属势力</param>
    /// <param name="troop">部队对象</param>
    public static void InjectTroopEnergy(Faction faction, Troop troop)
    {
        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (faction == null)
        {
            throw new ArgumentNullException(nameof(faction),
                "[InjectTroopEnergy] faction 不能为 null");
        }
        
        if (troop == null)
        {
            throw new ArgumentNullException(nameof(troop),
                "[InjectTroopEnergy] troop 不能为 null");
        }
        
        if (faction.GlobalInfluenceMap == null || faction.GlobalInfluenceMap.Length == 0)
        {
            throw new InvalidOperationException(
                $"[InjectTroopEnergy] 势力 {faction.Name} 的 GlobalInfluenceMap 未初始化");
        }
        
        // 计算部队威压能量
        int energy = troop.CalculateZocEnergy();
        
        if (energy <= 0) return; // 无威压能量，跳过
        
        // 获取部队位置
        Point pos = troop.Position;
        
        // 边界检查
        if (pos.X < 0 || pos.X >= TerrainCostCache.MapWidth || 
            pos.Y < 0 || pos.Y >= TerrainCostCache.MapHeight)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[InjectTroopEnergy] ⚠️ 部队 {troop.DisplayName} 位置越界：({pos.X}, {pos.Y})");
            return;
        }
        
        // 🆕 获取部队所在位置的天气，用于计算地形代价倍率
        // 日期：2026-03-20
        // 🔥 ANTI-BAND-AID：Fail Fast
        var weatherManager = Session.Current.Scenario.WeatherManager 
            ?? throw new InvalidOperationException(
                "[InjectTroopEnergy] WeatherManager 未初始化！请检查 Scenario.Init() 是否正确调用。");
        
        var weather = weatherManager.GetWeatherAt(pos);
        string weatherName = weather.ToString();
        
        // 获取天气对部队扩散的地形代价倍率
        var influenceConfig = WorldOfTheThreeKingdoms.GameData.InfluenceConfig.Current;
        double weatherCostMultiplier = influenceConfig.GetWeatherTroopTerrainCostMultiplier(weatherName);
        
        // 🆕 获取部队能量传播配置（循环外获取一次，避免重复）
        // 日期：2026-03-21
        var troopConfig = influenceConfig.TroopEnergySpreadConfig;
        
        // 🔥 关键：部队能量也使用扩散算法
        // 日期：2026-03-20
        // 原因：实现"部队推进导致边界变化"的需求
        
        // 使用临时优先队列（避免污染全局队列）
        // 🔥 C# 12：使用集合表达式
        PriorityQueue<int, int> troopFrontier = new();
        HashSet<int> visited = [];
        
        int mapWidth = TerrainCostCache.MapWidth;
        int[] dirOffsets = [-mapWidth, mapWidth, -1, 1];  // 上下左右
        
        // 起点：部队所在位置
        int startIndex = GetIndex(pos.X, pos.Y);
        troopFrontier.Enqueue(startIndex, -energy);  // 负能量作为优先级
        
        int affectedCells = 0;
        
        while (troopFrontier.TryDequeue(out int currentIndex, out int priority))
        {
            // 跳过已访问的格子
            if (!visited.Add(currentIndex)) continue;
            
            int currentEnergy = -priority;  // 取反得到实际能量
            
            // 更新部队能量（只有更大时才覆盖）
            if (currentEnergy > faction.GlobalInfluenceMap[currentIndex].ArmyEnergy)
            {
                faction.GlobalInfluenceMap[currentIndex].ArmyFactionId = faction.ID;
                faction.GlobalInfluenceMap[currentIndex].ArmyEnergy = currentEnergy;
                
                // 🆕 关键：新能量覆盖残留能量
                // 日期：2026-03-21
                // 原因：部队回到原位置时，新的活跃能量应该覆盖旧的残留能量
                // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳）
                if (faction.GlobalInfluenceMap[currentIndex].ResidualFactionId == faction.ID)
                {
                    faction.GlobalInfluenceMap[currentIndex].ResidualFactionId = -1;
                    faction.GlobalInfluenceMap[currentIndex].ResidualEnergy = 0;
                }
                
                affectedCells++;
            }
            
            // 扩散到相邻格子
            int cx = currentIndex % mapWidth;
            
            for (int i = 0; i < 4; i++)
            {
                // 边缘检测
                if (i == 2 && cx == 0) continue;  // 左边界
                if (i == 3 && cx == mapWidth - 1) continue;  // 右边界
                
                int nextIndex = currentIndex + dirOffsets[i];
                if (nextIndex < 0 || nextIndex >= faction.GlobalInfluenceMap.Length) continue;
                
                // 跳过已访问的格子
                if (visited.Contains(nextIndex)) continue;
                
                // 获取地形代价
                int baseTerrainCost = TerrainCostCache.GetCostByIndex(nextIndex);
                if (baseTerrainCost >= 99999) continue;  // 不可通行
                
                // 🆕 应用天气倍率到地形代价
                // 日期：2026-03-20
                // 说明：恶劣天气（雾、雨、雪）会大幅增加部队扩散的地形代价
                int weatherAdjustedCost = (int)(baseTerrainCost * weatherCostMultiplier);
                
                // 🔥 关键修复：最小地形代价，避免在官道上无限蔓延
                // 日期：2026-03-21
                // 🔥 使用配置文件参数（可通过 InfluenceConfig.json 调整）
                int actualCost = Math.Max(weatherAdjustedCost, troopConfig.MinimumTerrainCost);
                
                // 🆕 防线 1：敌城阻力（与城池能量传播一致）
                // 日期：2026-03-21
                // 🔥 关键：ID=0（洛阳）是有效的，必须使用 >= 0
                int targetArchId = Session.Current.Scenario.ArchitectureCoreMap[nextIndex];
                if (targetArchId >= 0)
                {
                    // 🔥 ANTI-BAND-AID：Fail Fast
                    Architecture targetArch = Session.Current.Scenario.Architectures
                        .GetGameObject(targetArchId) as Architecture;
                    
                    if (targetArch == null)
                    {
                        throw new InvalidOperationException(
                            $"[InjectTroopEnergy] 数据损坏：ArchitectureCoreMap[{nextIndex}] " +
                            $"引用了不存在的建筑 ID={targetArchId}");
                    }
                    
                    // 跳过无主建筑
                    if (targetArch.BelongedFaction == null)
                    {
                        // 无主建筑视为中立，不增加额外阻力
                    }
                    else if (targetArch.BelongedFaction.IsHostile(faction))
                    {
                        // 🔥 使用配置文件参数（可通过 InfluenceConfig.json 调整）
                        actualCost += troopConfig.EnemyCityResistance;
                    }
                    else if (targetArch.BelongedFaction != faction)
                    {
                        // 🔥 使用配置文件参数（可通过 InfluenceConfig.json 调整）
                        actualCost += troopConfig.AllyBarrier;
                    }
                    // 同势力城池：不增加额外阻力（通过叠加机制处理）
                }
                
                // 🆕 防线 2：敌军部队阻断（与城池能量传播一致）
                // 日期：2026-03-21
                if (IsEnemyBlockingByIndex(nextIndex, faction))
                {
                    // 🔥 使用配置文件参数（可通过 InfluenceConfig.json 调整）
                    actualCost += troopConfig.EnemyBlocking;
                }
                
                // 计算衰减后的能量
                int nextEnergy = currentEnergy - actualCost;
                
                // 🔥 关键修复：最小能量阈值，低于此值停止扩散
                // 日期：2026-03-21
                // 🔥 使用配置文件参数（可通过 InfluenceConfig.json 调整）
                if (nextEnergy <= troopConfig.MinimumEnergyThreshold) continue;
                
                // 只有新能量更高时才加入队列
                if (nextEnergy > faction.GlobalInfluenceMap[nextIndex].ArmyEnergy)
                {
                    troopFrontier.Enqueue(nextIndex, -nextEnergy);
                }
            }
        }
        
        // 🔥 2026-03-20：移除调试输出，避免 ExecutionEngineException
    }
    
    /// <summary>
    /// 计算单个势力的全局能量地图
    /// 🧊 Cold Path：天气变化或建筑变化时调用
    /// 
    /// 🔥 算法：Max-Remaining Energy（最大剩余能量优先扩散）
    /// - 优先级：-energy（负能量值，确保能量越大越早出队）
    /// - 状态记录：直接使用 GlobalInfluenceMap.CityEnergy（无需额外 _costSoFar 数组）
    /// - 截断条件：nextEnergy <= 0（能量耗尽立即停止）
    /// - 更新条件：nextEnergy > GlobalInfluenceMap[nextIndex].CityEnergy（只有更强能量才能覆盖）
    /// 
    /// 🔥 日期：2026-03-16
    /// 🔥 重构：只更新 CityEnergy 和 CityFactionId，不触碰 ArmyEnergy
    /// </summary>
    public static void RecalculateFactionInfluence(Faction faction)
    {
        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (faction.GlobalInfluenceMap == null || faction.GlobalInfluenceMap.Length == 0)
        {
            throw new InvalidOperationException(
                $"[MultiSourceInfluenceCalculator] 势力 {faction.Name} 的 GlobalInfluenceMap 未初始化");
        }
        
        if (faction.Architectures == null)
        {
            throw new InvalidOperationException(
                $"[MultiSourceInfluenceCalculator] 势力 {faction.Name} 的 Architectures 为 null");
        }
        
        // 🔥 ANTI-BAND-AID：明确检查每个数据源，不使用 ?. 掩盖错误
        if (Session.Current == null)
        {
            throw new InvalidOperationException(
                "[MultiSourceInfluenceCalculator] Session.Current 为 null");
        }
        
        if (Session.Current.Scenario == null)
        {
            throw new InvalidOperationException(
                "[MultiSourceInfluenceCalculator] Session.Current.Scenario 为 null");
        }
        
        if (Session.Current.Scenario.ArchitectureCoreMap == null)
        {
            throw new InvalidOperationException(
                "[MultiSourceInfluenceCalculator] Session.Current.Scenario.ArchitectureCoreMap 未初始化。" +
                "请确保在调用 RecalculateFactionInfluence 之前已调用 GameScenario.AfterInit()");
        }
        
        // 1. 清空旧数据（只清空城池能量层，不触碰部队能量层）
        // 🔥 日期：2026-03-16
        // 🔥 重构：只重置 CityEnergy 和 CityFactionId
        for (int i = 0; i < faction.GlobalInfluenceMap.Length; i++)
        {
            faction.GlobalInfluenceMap[i].CityFactionId = -1;
            faction.GlobalInfluenceMap[i].CityEnergy = 0;
            // 🔥 关键：不触碰 ArmyEnergy 和 ArmyFactionId
        }
        _frontier.Clear();
        
        // 🔥 2026-03-14：初始化该势力的源建筑 ID 记录数组
        if (!_sourceArchitectureMaps.TryGetValue(faction.ID, out int[] sourceArchMap))
        {
            sourceArchMap = new int[faction.GlobalInfluenceMap.Length];
            _sourceArchitectureMaps[faction.ID] = sourceArchMap;
        }
        Array.Fill(sourceArchMap, -1);
        
        // 2. 多源初始化：将该势力所有建筑作为起点
        var architectures = faction.Architectures.GetList();
        int archCount = architectures.Count;
        
        int totalInitialCells = 0;  // 🔥 调试：统计初始占地格子数
        
        // 🔥 使用 for 循环，避免 LINQ
        for (int i = 0; i < archCount; i++)
        {
            // 🔥 ANTI-BAND-AID：不使用防御性空检查
            if (architectures[i] is not Architecture arch)
            {
                throw new InvalidOperationException(
                    $"[MultiSourceInfluenceCalculator] 势力 {faction.Name} 的建筑列表包含无效项（索引 {i}）");
            }
            
            int energy = arch.CalculateInfluenceEnergy();
            
            // 🔥 ANTI-BAND-AID：检查建筑区域
            if (arch.ArchitectureArea == null)
            {
                throw new InvalidOperationException(
                    $"[MultiSourceInfluenceCalculator] 建筑 {arch.Name} 的 ArchitectureArea 为 null");
            }
            
            if (arch.ArchitectureArea.Area == null)
            {
                throw new InvalidOperationException(
                    $"[MultiSourceInfluenceCalculator] 建筑 {arch.Name} 的 ArchitectureArea.Area 为 null");
            }
            
            var area = arch.ArchitectureArea.Area;
            int areaCount = area.Count;
            totalInitialCells += areaCount;
            
            for (int j = 0; j < areaCount; j++)
            {
                Point p = area[j];
                int idx = GetIndex(p.X, p.Y);
                
                // 🔥 修复：建筑初始化阶段不应该有叠加逻辑
                // 日期：2026-03-21
                // 原因：叠加逻辑会导致能量无限增长，最终溢出变成负值
                // 解决：只在 ApplyGlobalEnergyCompetition 中实现友军支援加成
                
                // 🔥 关键：如果多个建筑共享同一个格子，取最大能量
                // 这是正常的业务逻辑（建筑区域重叠）
                if (energy > faction.GlobalInfluenceMap[idx].CityEnergy)
                {
                    faction.GlobalInfluenceMap[idx].CityFactionId = faction.ID;
                    faction.GlobalInfluenceMap[idx].CityEnergy = energy;
                    sourceArchMap[idx] = arch.ID; // 记录源建筑 ID
                    _frontier.Enqueue(idx, -energy);
                }
            }
        }
        
        // 3. 执行 Max-Remaining Energy 扩散
        int mapWidth = TerrainCostCache.MapWidth;
        int[] dirOffsets = [-mapWidth, mapWidth, -1, 1];  // 🔥 C# 12 集合表达式
        
        int iterationCount = 0;  // 🔥 调试：统计迭代次数
        int energyUpdateCount = 0;  // 🔥 调试：统计能量更新次数
        int staleStateCount = 0;  // 🔥 调试：统计过期状态数
        
        while (_frontier.TryDequeue(out int currentIndex, out int priority))
        {
            iterationCount++;
            
            // 🚨 关键：priority 是负能量值，需要取反
            int currentEnergy = -priority;
            
            // 🔥 过滤过期状态：如果出队的能量小于当前记录的能量，说明是旧状态
            // 🔥 日期：2026-03-16
            // 🔥 重构：比较 CityEnergy
            if (currentEnergy < faction.GlobalInfluenceMap[currentIndex].CityEnergy)
            {
                staleStateCount++;
                continue;
            }
            
            int cx = currentIndex % mapWidth;
            
            // 🔥 使用 for 循环，避免 foreach 的枚举器分配
            for (int i = 0; i < 4; i++)
            {
                // 边缘检测
                if (i == 2 && cx == 0) continue;
                if (i == 3 && cx == mapWidth - 1) continue;
                
                int nextIndex = currentIndex + dirOffsets[i];
                if (nextIndex < 0 || nextIndex >= faction.GlobalInfluenceMap.Length) continue;
                
                // 🔥 防线 1：获取地形代价（O(1) 查表）
                int terrainCost = TerrainCostCache.GetCostByIndex(nextIndex);
                if (terrainCost >= 99999) continue;
                
                // 🔥 防线 2：动态行政摩擦（2026-03-20 重构）
                // 🆕 细粒度倍率控制系统
                var config = GameData.InfluenceConfig.Current;
                var spreadConfig = config.CityEnergySpreadConfig;
                
                // 基础地形代价（应用地形倍率）
                double baseCost = terrainCost * spreadConfig.TerrainCostMultiplier;
                
                // 天气影响（应用天气倍率）
                // 注意：天气已经在 TerrainCostCache 中应用了 TerrainCostMultiplier
                // 这里的 WeatherEffectMultiplier 是额外的调整
                // 如果需要进一步增强天气影响，可以在这里添加额外计算
                
                // 最小消耗：30（允许官道快速传播，但不至于无限蔓延）
                int actualCost = Math.Max((int)baseCost, 30);
                
                // 🔥 防线 3：核心主权防波堤（防吞噬漏洞）
                // 🔥 关键：ID=0（洛阳）是有效的，必须使用 >= 0
                int targetArchId = Session.Current.Scenario.ArchitectureCoreMap[nextIndex];
                if (targetArchId >= 0)
                {
                    // 获取当前扩散源建筑的 ID
                    int sourceArchId = sourceArchMap[currentIndex];
                    
                    if (targetArchId != sourceArchId)
                    {
                        // 🔥 ANTI-BAND-AID：不使用防御性空检查，Fail Fast
                        Architecture targetArch = Session.Current.Scenario.Architectures
                            .GetGameObject(targetArchId) as Architecture;
                        
                        if (targetArch == null)
                        {
                            throw new InvalidOperationException(
                                $"[MultiSourceInfluenceCalculator] 数据损坏：ArchitectureCoreMap[{nextIndex}] " +
                                $"引用了不存在的建筑 ID={targetArchId}");
                        }
                        
                        // 🔥 临时处理：跳过无主建筑（BelongedFaction 为 null）
                        // 日期：2026-03-13
                        // 原因：剧本数据中存在未分配势力的建筑
                        // TODO: 修复剧本数据，确保所有建筑都有归属势力
                        if (targetArch.BelongedFaction == null)
                        {
                            // 无主建筑视为中立，不增加额外阻力
                            // 静默跳过，避免异常刷屏
                            continue;
                        }
                        
                        // 🆕 2026-03-20：应用细粒度倍率
                        if (targetArch.BelongedFaction.IsHostile(faction))
                        {
                            // 敌城阻力（应用敌城倍率）
                            int enemyCityResistance = (int)(300 * spreadConfig.EnemyCityResistanceMultiplier);
                            actualCost += enemyCityResistance;
                        }
                        else if (targetArch.BelongedFaction != faction)
                        {
                            // 友军行政壁垒（应用友军倍率）
                            int allyBarrier = (int)(100 * spreadConfig.AllyBarrierMultiplier);
                            actualCost += allyBarrier;
                        }
                        // 同势力不同城池：不额外增加阻力，允许能量自然竞争（取最高值）
                    }
                }
                
                // 🔥 防线 4：敌军部队动态阻断（2026-03-20 重构）
                if (IsEnemyBlockingByIndex(nextIndex, faction))
                {
                    // 敌军部队阻断（应用敌军倍率）
                    int enemyBlocking = (int)(150 * spreadConfig.EnemyBlockingMultiplier);
                    actualCost += enemyBlocking;
                }
                
                // 🆕 2026-03-20：应用整体倍率
                actualCost = (int)(actualCost * spreadConfig.OverallMultiplier);
                
                // 🔥 扩散与阻力扣减
                int nextEnergy = currentEnergy - actualCost;
                
                // 🚨 绝对终止条件：能量耗尽，停止蔓延
                if (nextEnergy <= 0) continue;
                
                // 🔥 修复：扩散阶段不应该有叠加逻辑
                // 日期：2026-03-21
                // 原因：叠加逻辑会导致能量无限增长（重复入队），最终溢出变成负值
                // 解决：只在 ApplyGlobalEnergyCompetition 中实现友军支援加成
                
                // 🔥 关键：只有新能量更高时才更新（Max-Remaining Energy 算法）
                // 这是正常的扩散逻辑，不会导致能量无限增长
                if (nextEnergy > faction.GlobalInfluenceMap[nextIndex].CityEnergy)
                {
                    faction.GlobalInfluenceMap[nextIndex].CityFactionId = faction.ID;
                    faction.GlobalInfluenceMap[nextIndex].CityEnergy = nextEnergy;
                    sourceArchMap[nextIndex] = sourceArchMap[currentIndex]; // 继承源建筑 ID
                    _frontier.Enqueue(nextIndex, -nextEnergy);
                    energyUpdateCount++;
                }
            }
        }
        
        // 统计最终结果（仅在需要时启用）
        // int totalCellsWithEnergy = 0;
        // for (int i = 0; i < faction.GlobalInfluenceMap.Length; i++)
        // {
        //     if (faction.GlobalInfluenceMap[i] > 0)
        //     {
        //         totalCellsWithEnergy++;
        //     }
        // }
        // System.Diagnostics.Debug.WriteLine(
        //     $"[MultiSourceInfluenceCalculator] 势力 {faction.Name} 完成，" +
        //     $"共 {totalCellsWithEnergy} 个格子有能量");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndex(int x, int y) => y * TerrainCostCache.MapWidth + x;
    
    /// <summary>
    /// 检查指定位置是否有敌军阻断
    /// 🔥 Hot Path：内联优化
    /// 日期：2026-03-14
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEnemyBlockingByIndex(int index, Faction faction)
    {
        // 🔥 实现敌军部队阻断检测
        // 获取该位置的部队
        int x = index % TerrainCostCache.MapWidth;
        int y = index / TerrainCostCache.MapWidth;
        Point pos = new Point(x, y);
        
        var troop = Session.Current.Scenario.GetTroopByPosition(pos);
        
        // 🔥 关键：GetTroopByPosition 设计为返回 null（无部队时）
        // 这不是数据错误，是正常的业务逻辑
        if (troop == null)
        {
            return false;
        }
        
        // 🔥 ANTI-BAND-AID：部队必须有归属势力
        // 如果 BelongedFaction 为 null，说明数据损坏
        if (troop.BelongedFaction == null)
        {
            throw new InvalidOperationException(
                $"[MultiSourceInfluenceCalculator] 数据损坏：部队 {troop.DisplayName} " +
                $"(ID={troop.ID}, 位置={pos}) 的 BelongedFaction 为 null");
        }
        
        // 只有敌对势力的部队才会阻断
        return troop.BelongedFaction.IsHostile(faction);
    }
}
