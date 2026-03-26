#nullable disable

using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.FactionDetail;
using GameManager;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 🆕 势力范围更新管理器（分帧计算，避免卡顿）
/// 🧊 Cold Path：不在 Update() 主循环中频繁运行
/// 日期：2026-03-11
/// </summary>
public class InfluenceUpdateManager(List<Architecture> architectures)
{
    // 🔥 C# 12：使用主构造函数
    private readonly List<Architecture> _architectures = architectures
        ?? throw new ArgumentNullException(nameof(architectures));
    private int _currentBatchIndex = 0;
    private int _frameCounter = 0;
    private string _lastWeather = "Sunny";

    // 🆕 阶段 2：分帧计算状态
    private bool _needsFullRecalculation = false;
    private int _factionCalculationIndex = 0;
    private Faction[] _factionsToRecalculate = [];
    
    // 🔥 新增：暂停标志（回合切换时暂停分帧重算）
    private bool _isPaused = false;
    
    // 🔥 新增：能量竞争前的事件（用于通知水墨渲染器）
    // 日期：2026-03-21
    public event Action? OnBeforeEnergyCompetition;

    /// <summary>
    /// 初始化势力范围系统
    /// 🧊 Cold Path：游戏启动时调用一次
    /// </summary>
    public void Initialize()
    {
        // 🆕 阶段 1：初始化全局地形代价缓存
        var scenario = Session.Current.Scenario;
        int mapWidth = scenario.ScenarioMap.MapDimensions.X;
        int mapHeight = scenario.ScenarioMap.MapDimensions.Y;

        TerrainCostCache.Initialize(mapWidth, mapHeight);

        // 🆕 阶段 3：初始化所有势力的全局能量地图
        if (scenario.Factions == null)
        {
            throw new InvalidOperationException(
                "[InfluenceUpdateManager] Scenario.Factions 为 null，数据未正确初始化");
        }

        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;

        for (int i = 0; i < factionCount; i++)
        {
            if (factions[i] is not Faction faction)
            {
                throw new InvalidOperationException(
                    $"[InfluenceUpdateManager] Factions 列表包含无效项（索引 {i}）");
            }

            faction.InitializeInfluenceMap(mapWidth, mapHeight);
        }

        // 🔥 修复：延迟初始计算，避免启动时卡顿
        // 改为在第一次 Update 时分帧计算
        MarkAllFactionsDirty();
    }

    public void SyncAfterFactionTopologyChange(string reason)
    {
        var scenario = Session.Current.Scenario;
        if (scenario == null)
        {
            throw new InvalidOperationException(
                "[InfluenceUpdateManager] Session.Current.Scenario is null while syncing faction topology.");
        }

        if (scenario.ScenarioMap == null)
        {
            throw new InvalidOperationException(
                "[InfluenceUpdateManager] ScenarioMap is null while syncing faction topology.");
        }

        int mapWidth = scenario.ScenarioMap.MapDimensions.X;
        int mapHeight = scenario.ScenarioMap.MapDimensions.Y;
        if (mapWidth <= 0 || mapHeight <= 0)
        {
            throw new InvalidOperationException(
                $"[InfluenceUpdateManager] Invalid map size while syncing faction topology: {mapWidth}x{mapHeight}");
        }

        if (TerrainCostCache.MapWidth != mapWidth || TerrainCostCache.MapHeight != mapHeight)
        {
            TerrainCostCache.Initialize(mapWidth, mapHeight);
        }

        EnsureAllFactionInfluenceMapsInitialized(scenario, mapWidth, mapHeight);
        scenario.InvalidateInfluenceEnergyCache();

        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;

        for (int i = 0; i < factionCount; i++)
        {
            if (factions[i] is not Faction faction)
            {
                throw new InvalidOperationException(
                    $"[InfluenceUpdateManager] Factions contains invalid entry at index {i} while syncing faction topology.");
            }

            RecalculateFactionInfluence(faction);
        }

        for (int i = 0; i < factionCount; i++)
        {
            if (factions[i] is not Faction faction)
            {
                throw new InvalidOperationException(
                    $"[InfluenceUpdateManager] Factions contains invalid entry at index {i} while syncing faction topology.");
            }

            faction.ClearEnergyBasedIntelligence();
        }

        OnBeforeEnergyCompetition?.Invoke();
        ApplyGlobalEnergyCompetition();

        _needsFullRecalculation = false;
        _factionCalculationIndex = 0;
        _factionsToRecalculate = [];
        _currentBatchIndex = 0;

        System.Diagnostics.Debug.WriteLine(
            $"[InfluenceUpdateManager] Completed immediate sync after faction topology change: {reason}");
    }

    /// <summary>
    /// 🔥 新增：暂停分帧重算（回合切换时调用）
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
    }

    /// <summary>
    /// 🔥 新增：恢复分帧重算（回合切换完成后调用）
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
    }

    private static void EnsureAllFactionInfluenceMapsInitialized(GameScenario scenario, int mapWidth, int mapHeight)
    {
        if (scenario.Factions == null)
        {
            throw new InvalidOperationException(
                "[InfluenceUpdateManager] Scenario.Factions is null while ensuring faction influence maps.");
        }

        int expectedLength = mapWidth * mapHeight;
        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;

        for (int i = 0; i < factionCount; i++)
        {
            if (factions[i] is not Faction faction)
            {
                throw new InvalidOperationException(
                    $"[InfluenceUpdateManager] Factions contains invalid entry at index {i} while ensuring influence maps.");
            }

            if (faction.GlobalInfluenceMap == null || faction.GlobalInfluenceMap.Length != expectedLength)
            {
                faction.InitializeInfluenceMap(mapWidth, mapHeight);
            }
        }
    }

    /// <summary>
    /// 🆕 能量自然衰减（每回合开始前调用）
    /// 日期：2026-03-21
    /// 用途：修复部队能量永久残留 Bug
    /// 
    /// 核心逻辑（水流系统类比）：
    /// - 活跃部队能量不衰减（部队在场，持续注入能量 = 活水）
    /// - 城池能量不衰减（城池在场，持续注入能量 = 活水）
    /// - 残留能量逐渐衰减（部队/城池离开后，能量转为残留 = 死水，逐渐蒸发）
    /// </summary>
    public void DecayAllFactionsEnergy()
    {
        var scenario = Session.Current.Scenario;
        
        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (scenario.Factions == null)
        {
            throw new InvalidOperationException(
                "[DecayAllFactionsEnergy] Scenario.Factions 为 null");
        }
        
        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;
        
        // 🔥 使用 for 循环，避免 LINQ
        for (int i = 0; i < factionCount; i++)
        {
            // 🔥 ANTI-BAND-AID：Fail Fast
            if (factions[i] is not Faction faction)
            {
                throw new InvalidOperationException(
                    $"[DecayAllFactionsEnergy] 势力列表包含无效项（索引 {i}）");
            }
            
            DecayFactionEnergy(faction);
        }
    }

    /// <summary>
    /// 🆕 衰减单个势力的能量
    /// 日期：2026-03-21
    /// 🔥 Hot Path：每回合调用一次，但处理大量数据
    /// 🆕 新增：残留能量衰减（死水蒸发）
    /// 
    /// 核心逻辑（水流系统类比）：
    /// - 活跃部队能量不衰减（部队在场，持续注入能量 = 活水）
    /// - 城池能量不衰减（城池在场，持续注入能量 = 活水）
    /// - 残留能量逐渐衰减（部队/城池离开后，能量转为残留 = 死水，逐渐蒸发）
    /// </summary>
    private static void DecayFactionEnergy(Faction faction)
    {
        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (faction.GlobalInfluenceMap == null)
        {
            throw new InvalidOperationException(
                $"[DecayFactionEnergy] 势力 {faction.Name} 的 GlobalInfluenceMap 为 null");
        }
        
        int mapSize = faction.GlobalInfluenceMap.Length;
        
        // 🆕 获取残留能量配置（循环外获取一次）
        // 日期：2026-03-21
        var config = GameData.InfluenceConfig.Current;
        var residualConfig = config.ResidualEnergyConfig;
        
        // 🔥 使用 for 循环，零 GC 分配
        for (int i = 0; i < mapSize; i++)
        {
            // 🔥 关键修复：活跃部队能量不衰减
            // 日期：2026-03-21
            // 原因：部队在场时，持续注入能量（活水），不应该衰减
            //       只有部队离开后，能量转为残留能量（死水），才逐渐蒸发
            // 说明：部队能量每回合通过 InjectTroopEnergy 重新计算，不需要衰减
            
            // 🔥 关键修复：城池能量不衰减
            // 日期：2026-03-21
            // 原因：城池在场时，持续注入能量（活水），不应该衰减
            //       只要城池还在，能量就会持续扩散
            // 说明：城池能量每回合通过 RecalculateFactionInfluence 重新计算，不需要衰减
            
            // 🆕 残留能量衰减（死水蒸发）
            // 日期：2026-03-21
            // 原因：残留能量是"死水"（与水源断开），逐渐蒸发
            // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳）
            if (faction.GlobalInfluenceMap[i].ResidualEnergy > 0)
            {
                // 残留能量衰减速度（直接使用配置值）
                int residualDecay = residualConfig.ResidualEnergyDecayPerTurn;
                faction.GlobalInfluenceMap[i].ResidualEnergy -= residualDecay;
                
                if (faction.GlobalInfluenceMap[i].ResidualEnergy <= 0)
                {
                    faction.GlobalInfluenceMap[i].ResidualEnergy = 0;
                    faction.GlobalInfluenceMap[i].ResidualFactionId = -1;
                }
            }
        }
    }

    /// <summary>
    /// 每帧调用，但只在特定间隔执行更新
    /// 🔥 Hot Path：大部分时间直接返回，性能影响极小
    /// </summary>
    public void Update()
    {
        // 🔥 新增：暂停时直接返回
        if (_isPaused)
        {
            return;
        }

        // 🆕 阶段 2：优先处理分帧重算
        if (_needsFullRecalculation)
        {
            UpdateFactionRecalculation();
            return;
        }

        var config = GameData.InfluenceConfig.Current;

        if (!config.EnableAutoUpdate) return;

        // 🆕 检测天气变化：天气改变时强制更新所有建筑
        // 🔥 ANTI-BAND-AID：明确检查数据源
        var weatherManager = Session.Current.Scenario.WeatherManager;
        if (weatherManager == null)
        {
            throw new InvalidOperationException(
                "[InfluenceUpdateManager] WeatherManager 未初始化，请检查 Scenario 加载流程");
        }

        // 获取地图中心点的天气作为全局天气代表
        var mapCenter = new Microsoft.Xna.Framework.Point(
            Session.Current.Scenario.ScenarioMap.MapDimensions.X / 2,
            Session.Current.Scenario.ScenarioMap.MapDimensions.Y / 2
        );

        string currentWeather = weatherManager.GetWeatherAt(mapCenter).ToString();

        if (currentWeather != _lastWeather)
        {
            // 🆕 阶段 1：更新全局地形代价缓存
            TerrainCostCache.UpdateCache();

            // 🆕 阶段 2：启动分帧重算（而非立即全部重算）
            MarkAllFactionsDirty();
            _lastWeather = currentWeather;
            return;
        }

        // 🔥 Hot Path 优化：大部分时间直接返回
        if (++_frameCounter % config.UpdateIntervalFrames != 0) return;

        // 🧊 Cold Path：分批更新（避免单帧卡顿）
        UpdateBatch(config.BatchSize);
    }

    private void UpdateBatch(int batchSize)
    {
        int start = _currentBatchIndex * batchSize;
        int end = Math.Min(start + batchSize, _architectures.Count);

        // 🔥 使用 for 循环而非 LINQ
        for (int i = start; i < end; i++)
        {
            var arch = _architectures[i];

            // 清除缓存，触发重新计算
            arch.InvalidateInfluenceCache();

            // ❌ 移除预热逻辑：不应该在 Update 中主动计算
            // 势力范围应该在真正需要时（渲染/Buff）才懒加载
        }

        // 循环批次索引
        _currentBatchIndex = (end >= _architectures.Count) ? 0 : _currentBatchIndex + 1;
    }

    /// <summary>
    /// 强制更新所有建筑（仅在回合结束时调用）
    /// 🧊 Cold Path：回合结束时可以接受短暂卡顿
    /// </summary>
    public void ForceUpdateAll()
    {
        // 🧊 Cold Path：回合结束时可以接受短暂卡顿
        for (int i = 0; i < _architectures.Count; i++)
        {
            _architectures[i].InvalidateInfluenceCache();
        }
        
        // 🔥 关键：触发分帧重算，确保能量情报更新
        // 日期：2026-03-20
        // 说明：回合结束时需要重新计算所有势力的能量和视野
        MarkAllFactionsDirty();
    }

    /// <summary>
    /// 🆕 阶段 2：标记所有势力需要重算（启动分帧计算）
    /// 🧊 Cold Path：天气变化时调用
    /// </summary>
    private void MarkAllFactionsDirty()
    {
        _needsFullRecalculation = true;
        _factionCalculationIndex = 0;

        // 收集所有势力
        var scenario = Session.Current.Scenario;

        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (scenario.Factions == null)
        {
            throw new InvalidOperationException(
                "[InfluenceUpdateManager] Scenario.Factions 为 null，数据未正确初始化");
        }

        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;

        // 🔥 预分配数组（避免 List.Add 的动态扩容）
        _factionsToRecalculate = new Faction[factionCount];
        int validCount = 0;

        for (int i = 0; i < factionCount; i++)
        {
            // 🔥 ANTI-BAND-AID：不使用防御性空检查
            if (factions[i] is not Faction faction)
            {
                throw new InvalidOperationException(
                    $"[InfluenceUpdateManager] Factions 列表包含无效项（索引 {i}），请检查数据加载");
            }

            _factionsToRecalculate[validCount++] = faction;
        }

        // 🔥 调整数组大小（如果有无效项）
        if (validCount < factionCount)
        {
            Array.Resize(ref _factionsToRecalculate, validCount);
        }
    }

    /// <summary>
    /// 🆕 阶段 2：分帧重算势力范围
    /// 🔥 Hot Path：每帧调用，但只处理 1 个势力
    /// </summary>
    private void UpdateFactionRecalculation()
    {
        // 🔥 每帧只算 1 个势力
        if (_factionCalculationIndex < _factionsToRecalculate.Length)
        {
            var faction = _factionsToRecalculate[_factionCalculationIndex];
            RecalculateFactionInfluence(faction);
            _factionCalculationIndex++;
        }
        else
        {
            // 🔥 关键步骤 1：清除所有势力的旧能量情报
            // 日期：2026-03-20
            // 说明：必须在重新计算前清除，否则会累积旧数据
            var scenario = Session.Current.Scenario;
            
            // 🔥 ANTI-BAND-AID：明确检查数据源
            if (scenario.Factions == null)
            {
                throw new InvalidOperationException(
                    "[InfluenceUpdateManager] Scenario.Factions 为 null");
            }
            
            var factions = scenario.Factions.GetList();
            int factionCount = factions.Count;
            
            for (int i = 0; i < factionCount; i++)
            {
                // 🔥 ANTI-BAND-AID：Fail Fast，不使用防御性空检查
                if (factions[i] is not Faction faction)
                {
                    throw new InvalidOperationException(
                        $"[InfluenceUpdateManager] Factions 列表包含无效项（索引 {i}）");
                }
                
                faction.ClearEnergyBasedIntelligence();
            }
            
            // 🔥 关键步骤 2：执行全局能量竞争并生成新的视野情报
            // 日期：2026-03-13
            // 说明：每个地块只有能量最高的势力保留（最高能量 - 第二高能量）
            // 
            // 🔥 关键修复：在 ApplyGlobalEnergyCompetition 之前通知水墨渲染器
            // 日期：2026-03-21
            // 原因：ApplyGlobalEnergyCompetition 会清零非胜出势力的能量
            //       水墨渲染器需要在清零前读取完整的能量数据
            OnBeforeEnergyCompetition?.Invoke();
            
            ApplyGlobalEnergyCompetition();
            
            // 算完了
            _needsFullRecalculation = false;
            _factionCalculationIndex = 0;
            _factionsToRecalculate = [];  // 🔥 C# 12 集合表达式：字段类型已明确为 Faction[]
        }
    }

    /// <summary>
    /// 🆕 阶段 2+3：重算单个势力的势力范围
    /// 🧊 Cold Path：不在 Update 主循环中
    /// </summary>
    private void RecalculateFactionInfluence(Faction faction)
    {
        // 阶段 3：使用多源 Dijkstra 计算全局能量地图
        MultiSourceInfluenceCalculator.RecalculateFactionInfluence(faction);

        // 🆕 阶段 4：注入部队威压能量
        // 日期：2026-03-16
        // 说明：部队能量直接注入到所在格子，不扩散
        InjectTroopEnergies(faction);

        // 🆕 阶段 3.5：统计势力领土总能量
        // 日期：2026-03-13
        // 用途：计算势力的总能量值，用于势力实力评估
        TallyFactionTerritoryEnergy(faction);

        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (faction.Architectures == null)
        {
            throw new InvalidOperationException(
                $"[InfluenceUpdateManager] 势力 {faction.Name} 的 Architectures 为 null");
        }

        // 清除所有建筑的缓存（因为全局地图已更新）
        var architectures = faction.Architectures.GetList();
        int archCount = architectures.Count;

        for (int i = 0; i < archCount; i++)
        {
            // 🔥 ANTI-BAND-AID：不使用防御性空检查
            if (architectures[i] is not Architecture arch)
            {
                throw new InvalidOperationException(
                    $"[InfluenceUpdateManager] 势力 {faction.Name} 的建筑列表包含无效项（索引 {i}）");
            }

            arch.InvalidateInfluenceCache();
        }
    }
    
    /// <summary>
    /// 🆕 阶段 4：注入所有部队的威压能量
    /// 🧊 Cold Path：势力范围更新时调用
    /// 日期：2026-03-16
    /// 
    /// 🔥 关键修复：先将旧的部队能量转为残留能量，然后重新注入
    /// 日期：2026-03-21
    /// 原因：部队移动后，旧位置的部队能量应该转为残留能量（余威），而不是直接清零
    /// 机制：活水 → 死水 → 逐渐蒸发
    /// </summary>
    private static void InjectTroopEnergies(Faction faction)
    {
        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (faction.Troops == null)
        {
            throw new InvalidOperationException(
                $"[InjectTroopEnergies] 势力 {faction.Name} 的 Troops 为 null");
        }
        
        if (faction.GlobalInfluenceMap == null)
        {
            throw new InvalidOperationException(
                $"[InjectTroopEnergies] 势力 {faction.Name} 的 GlobalInfluenceMap 为 null");
        }
        
        // 🆕 关键修复：先将旧的部队能量转为残留能量
        // 日期：2026-03-21
        // 原因：部队离开后，能量不是立即消失，而是转为残留能量（余威）
        // 说明：RecalculateFactionInfluence 只清空城池能量，不清空部队能量
        //       所以必须在这里手动处理旧的部队能量
        for (int i = 0; i < faction.GlobalInfluenceMap.Length; i++)
        {
            // 只处理该势力的部队能量（不影响其他势力）
            // 🔥 关键：ID >= 0 是有效的（ID=0 是洛阳）
            if (faction.GlobalInfluenceMap[i].ArmyFactionId == faction.ID)
            {
                // 转为残留能量（而不是直接清零）
                faction.GlobalInfluenceMap[i].ResidualFactionId = faction.ID;
                faction.GlobalInfluenceMap[i].ResidualEnergy = faction.GlobalInfluenceMap[i].ArmyEnergy;
                
                // 清空活跃能量
                faction.GlobalInfluenceMap[i].ArmyFactionId = -1;
                faction.GlobalInfluenceMap[i].ArmyEnergy = 0;
            }
        }
        
        GameObjectList troops = faction.Troops.GetList();
        int troopCount = troops.Count;
        
        if (troopCount == 0) return; // 无部队，跳过
        
        // 🔥 使用 for 循环，避免 LINQ
        for (int i = 0; i < troopCount; i++)
        {
            // 🔥 ANTI-BAND-AID：不使用防御性空检查
            if (troops[i] is not Troop troop)
            {
                throw new InvalidOperationException(
                    $"[InjectTroopEnergies] 势力 {faction.Name} 的部队列表包含无效项（索引 {i}）");
            }
            
            // 注入部队能量（会覆盖残留能量）
            MultiSourceInfluenceCalculator.InjectTroopEnergy(faction, troop);
        }
    }

    /// <summary>
    /// 🆕 阶段 3.5：统计单个势力的领土总能量
    /// 🧊 Cold Path：势力能量计算完成后调用
    /// 
    /// 核心逻辑：
    /// - 遍历该势力的 GlobalInfluenceMap
    /// - 累加所有格子的剩余能量
    /// - 存储到 Faction.TotalTerritoryEnergy
    /// 
    /// 日期：2026-03-13
    /// </summary>
    private static void TallyFactionTerritoryEnergy(Faction faction)
    {
        // 🔥 ANTI-BAND-AID：明确检查数据源
        if (faction.GlobalInfluenceMap == null)
        {
            throw new InvalidOperationException(
                $"[InfluenceUpdateManager] 势力 {faction.Name} 的 GlobalInfluenceMap 为 null");
        }

        // 🔥 使用 ReadOnlySpan 优化性能（零分配）
        // 🔥 日期：2026-03-16
        // 🔥 重构：从 int[] 改为 TileInfluenceState[]，只统计城池能量
        ReadOnlySpan<WorldOfTheThreeKingdoms.GameManager.TileInfluenceState> energyMap = faction.GlobalInfluenceMap.AsSpan();
        
        long totalEnergy = 0;  // 🔥 使用 long 防止溢出（最大 63118 格 × 650 能量 = 41,026,700）
        
        // 🔥 使用 for 循环，避免 LINQ
        for (int i = 0; i < energyMap.Length; i++)
        {
            // 🔥 关键：只统计城池能量（战略归属），不统计部队能量
            // 原因：领土总能量用于势力实力评估，应该看战略归属而非战术控制
            int energy = energyMap[i].CityEnergy;
            if (energy > 0)
            {
                totalEnergy += energy;
            }
        }

        // 🔥 存储到势力对象（需要在 Faction 类中添加该属性）
        faction.TotalTerritoryEnergy = (int)Math.Min(totalEnergy, int.MaxValue);
    }

    /// <summary>
    /// 🆕 阶段 4：全局能量竞争结算系统
    /// 🧊 Cold Path：所有势力能量计算完成后调用一次
    /// 
    /// 核心机制：
    /// 1. 每个地块只有能量最高的势力获得控制权
    /// 2. 能量值 = 第一名能量 - 第二名能量（只对比敌对势力）
    /// 3. 其他势力在该地块的能量清零
    /// 
    /// 三大补丁：
    /// - 补丁 1：盟军/友军不参与能量对抗
    /// - 补丁 2：能量差值 < 10 时维持历史归属（防抖动）
    /// - 补丁 3：建筑中心 ±2 格为绝对领域（免疫削减）
    /// 
    /// 日期：2026-03-13
    /// </summary>
    /// <summary>
    /// 🔥 全局能量竞争：两段式对冲
    /// 日期：2026-03-16
    /// 重构：实现"先抵消部队能量，再抵消城池能量"的原则
    /// 
    /// 算法：
    /// 1. 第一段对冲：部队 vs 部队（表土层）
    /// 2. 第二段对冲：部队 vs 城池（基岩层）
    /// 3. 城池能量竞争：最高 - 次高
    /// 4. 归属判断：只看城池能量
    /// </summary>
    /// <summary>
    /// 🔥 全局能量竞争（统一对冲机制）
    /// 日期：2026-03-20 重构
    /// 
    /// 核心逻辑：
    /// 1. 每个地块只有一个能量归属（能量最高的势力）
    /// 2. 计算方式：最高能量 - 第二高能量 = 净能量
    /// 3. 对冲顺序：部队能量优先抵消，然后城池能量
    /// 4. 归属判断：净能量 > 0 的势力占据地块并获得视野
    /// </summary>
    private void ApplyGlobalEnergyCompetition()
    {
        var scenario = Session.Current.Scenario;
        if (scenario.Factions == null)
        {
            throw new InvalidOperationException(
                "[InfluenceUpdateManager] Scenario.Factions 为 null");
        }

        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;
        int mapWidth = TerrainCostCache.MapWidth;
        int mapHeight = TerrainCostCache.MapHeight;
        int totalTiles = mapWidth * mapHeight;

        // 🔥 性能优化：在循环外获取配置（2026-03-21）
        var stackingConfig = GameData.InfluenceConfig.Current.EnergyStackingConfig;
        
        // 🔥 性能优化：预分配缓冲区，避免每个地块都分配 List（2026-03-21）
        // 使用数组池或预分配数组，避免 stackalloc 超过栈限制
        // 🆕 2026-03-21：添加 residualEnergy 字段，用于能量写回
        var energyBuffer = new (Faction faction, int armyEnergy, int cityEnergy, int residualEnergy, int totalEnergy)[factionCount];

        // 🔥 遍历地图上的每一个格子
        for (int i = 0; i < totalTiles; i++)
        {
            int x = i % mapWidth;
            int y = i / mapWidth;
            Point pos = new(x, y);  // 🔥 C# 12：目标类型推断

            // ========================================
            // 🔥 步骤 1：收集所有势力的总能量（部队 + 城池 + 残留）
            // 🆕 2026-03-21：残留能量参与竞争，但效果减半
            // ========================================
            
            // 使用预分配的数组作为缓冲区
            int energyCount = 0;
            
            for (int fIdx = 0; fIdx < factionCount; fIdx++)
            {
                if (factions[fIdx] is not Faction faction)
                {
                    throw new InvalidOperationException(
                        $"[ApplyGlobalEnergyCompetition] 势力列表包含无效项（索引 {fIdx}）");
                }
                
                int armyEnergy = faction.GlobalInfluenceMap[i].ArmyEnergy;
                int cityEnergy = faction.GlobalInfluenceMap[i].CityEnergy;
                
                // 🆕 残留能量参与竞争，但效果减半（默认 50%）
                // 日期：2026-03-21
                // 原因：残留能量是"死水"，影响力应该比活跃能量弱
                int residualEnergy = faction.GlobalInfluenceMap[i].ResidualEnergy;
                int effectiveResidualEnergy = (int)(residualEnergy * stackingConfig.ResidualEffectiveness);
                
                int totalEnergy = armyEnergy + cityEnergy + effectiveResidualEnergy;
                
                if (totalEnergy > 0)
                {
                    // 🔥 关键修复：存储残留能量，用于能量写回
                    // 日期：2026-03-21
                    // 原因：如果胜出势力只有残留能量（没有活跃能量），需要保留残留能量
                    energyBuffer[energyCount++] = (faction, armyEnergy, cityEnergy, residualEnergy, totalEnergy);
                }
            }
            
            // 如果没有任何势力有能量，跳过
            if (energyCount == 0) continue;
            
            // 获取有效数据的 Span
            Span<(Faction faction, int armyEnergy, int cityEnergy, int residualEnergy, int totalEnergy)> energyList = energyBuffer.AsSpan(0, energyCount);
            
            // ========================================
            // 🔥 步骤 2：按总能量排序，找出最高和第二高
            // ========================================
            
            // 🔥 使用 Span.Sort（零分配，原地排序）
            energyList.Sort((a, b) => b.totalEnergy.CompareTo(a.totalEnergy));
            
            var top = energyList[0];
            Faction topFaction = top.faction;
            int topArmyEnergy = top.armyEnergy;
            int topCityEnergy = top.cityEnergy;
            int topResidualEnergy = top.residualEnergy;
            int topTotalEnergy = top.totalEnergy;
            
            // 找出第二高的敌对势力能量
            int secondTotalEnergy = 0;
            for (int j = 1; j < energyList.Length; j++)
            {
                if (energyList[j].faction.IsHostile(topFaction))
                {
                    secondTotalEnergy = energyList[j].totalEnergy;
                    break;
                }
            }
            
            // 🆕 2026-03-21：友军支援加成
            // 如果有友军势力，提供防御加成（主能量 + 副能量 × 15%）
            int friendlySupportBonus = 0;
            
            for (int j = 1; j < energyList.Length; j++)
            {
                var otherFaction = energyList[j].faction;
                
                // 跳过敌对势力
                if (otherFaction.IsHostile(topFaction)) continue;
                
                // 友军支援：副能量 × 15%
                int supportEnergy = (int)(energyList[j].totalEnergy * stackingConfig.FriendlySupportMultiplier);
                friendlySupportBonus += supportEnergy;
            }
            
            // 应用友军支援加成到主势力能量
            topTotalEnergy += friendlySupportBonus;
            
            // ========================================
            // 🔥 步骤 3：计算净能量（部队优先抵消）
            // 🔥 关键修复：确保能量永远不会变负
            // 日期：2026-03-21
            // ========================================
            
            int remainingOpponentEnergy = secondTotalEnergy;
            int finalArmyEnergy = topArmyEnergy;
            int finalCityEnergy = topCityEnergy;
            
            if (remainingOpponentEnergy > 0)
            {
                // 先用部队能量抵消
                if (finalArmyEnergy >= remainingOpponentEnergy)
                {
                    // 部队能量足够抵消全部敌方能量
                    finalArmyEnergy -= remainingOpponentEnergy;
                    remainingOpponentEnergy = 0;
                }
                else
                {
                    // 部队能量不够，全部消耗，继续用城池能量抵消
                    remainingOpponentEnergy -= finalArmyEnergy;
                    finalArmyEnergy = 0;
                    
                    if (finalCityEnergy >= remainingOpponentEnergy)
                    {
                        // 城池能量足够抵消剩余敌方能量
                        finalCityEnergy -= remainingOpponentEnergy;
                        remainingOpponentEnergy = 0;
                    }
                    else
                    {
                        // 🔥 关键修复：城池能量也不够，全部清零（不产生负值）
                        // 日期：2026-03-21
                        // 原因：能量不足以抵抗敌方，该地块应该被敌方占据
                        remainingOpponentEnergy -= finalCityEnergy;
                        finalCityEnergy = 0;
                        // 注意：此时 remainingOpponentEnergy > 0，说明敌方能量更强
                    }
                }
            }
            
            int netEnergy = finalArmyEnergy + finalCityEnergy;
            
            // 🔥 安全检查：确保净能量不为负（防御性编程）
            // 日期：2026-03-21
            if (netEnergy < 0)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[ApplyGlobalEnergyCompetition] ⚠️ 警告：地块 {i} 净能量为负 ({netEnergy})，" +
                    $"强制清零。topArmyEnergy={topArmyEnergy}, topCityEnergy={topCityEnergy}, " +
                    $"secondTotalEnergy={secondTotalEnergy}");
                #endif
                netEnergy = 0;
                finalArmyEnergy = 0;
                finalCityEnergy = 0;
            }
            
            // ========================================
            // 🔥 步骤 4：更新所有势力的能量地图
            // ========================================
            
            for (int j = 0; j < factionCount; j++)
            {
                if (factions[j] is not Faction faction)
                {
                    throw new InvalidOperationException(
                        $"[ApplyGlobalEnergyCompetition] 势力列表包含无效项（索引 {j}）");
                }
                
                if (faction == topFaction && netEnergy > 0)
                {
                    // 胜出的势力保留净能量
                    faction.GlobalInfluenceMap[i].ArmyFactionId = finalArmyEnergy > 0 ? faction.ID : -1;
                    faction.GlobalInfluenceMap[i].ArmyEnergy = finalArmyEnergy;
                    faction.GlobalInfluenceMap[i].CityFactionId = finalCityEnergy > 0 ? faction.ID : -1;
                    faction.GlobalInfluenceMap[i].CityEnergy = finalCityEnergy;
                    
                    // 🔥 关键修复：如果胜出势力只有残留能量（没有活跃能量），保留残留能量
                    // 日期：2026-03-21
                    // 原因：部队离开后，残留能量应该保留，不应该消失
                    // 说明：残留能量通过 DecayAllFactionsEnergy 自然衰减
                    if (finalArmyEnergy == 0 && finalCityEnergy == 0 && topResidualEnergy > 0)
                    {
                        // 胜出势力只有残留能量，保留残留能量
                        faction.GlobalInfluenceMap[i].ResidualFactionId = faction.ID;
                        faction.GlobalInfluenceMap[i].ResidualEnergy = topResidualEnergy;
                    }
                    else if (finalArmyEnergy > 0 || finalCityEnergy > 0)
                    {
                        // 胜出势力有活跃能量，清零残留能量（活跃能量覆盖残留能量）
                        faction.GlobalInfluenceMap[i].ResidualFactionId = -1;
                        faction.GlobalInfluenceMap[i].ResidualEnergy = 0;
                    }
                    
                    // 🔥 关键：占据地块的势力获得视野和情报
                    // 日期：2026-03-20
                    // 说明：根据净能量决定情报等级
                    InformationLevel level = EnergyToInformationLevel(netEnergy);
                    faction.AddEnergyBasedIntelligence(pos, level);
                }
                else
                {
                    // 🔥 关键修复：非胜出势力清零活跃能量，但保留残留能量
                    // 日期：2026-03-21
                    // 原因：非胜出势力失去该地块的控制权，活跃能量清零
                    //       但残留能量保留，逐渐衰减（余威）
                    // 说明：InjectTroopEnergies 已经将旧的部队能量转为残留能量
                    //       这里只清零活跃能量，不触碰残留能量
                    faction.GlobalInfluenceMap[i].ArmyFactionId = -1;
                    faction.GlobalInfluenceMap[i].ArmyEnergy = 0;
                    faction.GlobalInfluenceMap[i].CityFactionId = -1;
                    faction.GlobalInfluenceMap[i].CityEnergy = 0;
                    // 🔥 关键：不清零 ResidualEnergy 和 ResidualFactionId
                    //       让残留能量通过 DecayAllFactionsEnergy 自然衰减
                }
            }
        }
    }

    /// <summary>
    /// 🔥 补丁 3：检查坐标是否在建筑的绝对领域内
    /// 绝对领域：建筑中心 ±2 格范围（曼哈顿距离）
    /// </summary>
    private static bool IsInArchitectureSanctuary(Point pos, Faction faction)
    {
        var architectures = faction.Architectures.GetList();
        int archCount = architectures.Count;

        for (int i = 0; i < archCount; i++)
        {
            // 🔥 ANTI-BAND-AID：不使用防御性空检查
            if (architectures[i] is not Architecture arch)
            {
                throw new InvalidOperationException(
                    $"[IsInArchitectureSanctuary] 势力 {faction.Name} 的建筑列表包含无效项（索引 {i}）");
            }

            Point center = arch.ArchitectureArea.Centre;
            int distance = Math.Abs(pos.X - center.X) + Math.Abs(pos.Y - center.Y);
            
            if (distance <= 2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 🔥 能量值转换为情报等级
    /// </summary>
    private static InformationLevel EnergyToInformationLevel(int energy)
    {
        return energy switch
        {
            >= 300 => InformationLevel.高,
            >= 100 => InformationLevel.中,
            > 0 => InformationLevel.低,
            _ => InformationLevel.无
        };
    }
}
