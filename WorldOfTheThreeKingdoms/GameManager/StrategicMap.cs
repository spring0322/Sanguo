using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using global::GameGlobal;
using global::GameManager;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 战略影响力地图 - 用于AI战略决策
    /// </summary>
    public class StrategicMap
    {
        private float[,] _influenceMap;
        private float[,] _threatMap;
        private int _width;
        private int _height;
        private float _scale = 1.0f;
        private DateTime _lastUpdate = DateTime.MinValue;

        public int Width => _width;
        public int Height => _height;

        public StrategicMap(int width, int height)
        {
            _width = width;
            _height = height;
            _influenceMap = new float[width, height];
            _threatMap = new float[width, height];
        }

        /// <summary>
        /// 初始化战略地图
        /// </summary>
        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;
            _influenceMap = new float[width, height];
            _threatMap = new float[width, height];
            Clear();
        }

        /// <summary>
        /// 刷新战略地图
        /// </summary>
        public void Refresh(Faction faction)
        {
            try
            {
                if (faction == null) return;
                var influenceConfig = WorldOfTheThreeKingdoms.GameData.InfluenceConfig.Current;
                influenceConfig.ValidateLegacyEnergyFormulaMirror();
                var aiConfig = influenceConfig.GetValidatedAIStrategicConfig();

                if (aiConfig.UseEnergyMapForStrategicMap)
                {
                    RefreshFromEnergyMap(faction, aiConfig);
                }
                else
                {
                    RefreshLegacyModel(faction);
                }
            }
            catch (InvalidOperationException)
            {
                // 配置或数据源错误必须 Fail Fast，避免 AI 在错误口径下继续决策
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StrategicMap] 刷新失败: {ex.Message}");
            }
        }

        private void RefreshFromEnergyMap(Faction viewerFaction, WorldOfTheThreeKingdoms.GameData.AIStrategicConfig aiConfig)
        {
            if (Session.Current?.Scenario?.ScenarioMap == null)
            {
                throw new InvalidOperationException("[StrategicMap] ScenarioMap 未初始化，无法刷新能量战略图。");
            }

            if (Session.Current.Scenario.Factions == null)
            {
                throw new InvalidOperationException("[StrategicMap] Scenario.Factions 为 null。");
            }

            int mapWidth = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
            int mapHeight = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
            if (mapWidth <= 0 || mapHeight <= 0)
            {
                throw new InvalidOperationException($"[StrategicMap] 地图尺寸无效: {mapWidth}x{mapHeight}");
            }

            int expectedLength = mapWidth * mapHeight;
            if (viewerFaction.GlobalInfluenceMap == null || viewerFaction.GlobalInfluenceMap.Length != expectedLength)
            {
                if (!aiConfig.FallbackToLegacyModel)
                {
                    throw new InvalidOperationException(
                        $"[StrategicMap] 势力 {viewerFaction.Name} 的 GlobalInfluenceMap 无效，长度={viewerFaction.GlobalInfluenceMap?.Length ?? -1}，期望={expectedLength}");
                }

                RefreshLegacyModel(viewerFaction);
                return;
            }

            if (_width != mapWidth || _height != mapHeight)
            {
                Initialize(mapWidth, mapHeight);
            }
            else
            {
                Clear();
            }

            GameObjectList factionObjects = Session.Current.Scenario.Factions.GetList();
            int factionCount = factionObjects.Count;
            Faction[] allFactions = new Faction[factionCount];

            for (int i = 0; i < factionCount; i++)
            {
                if (factionObjects[i] is not Faction faction)
                {
                    throw new InvalidOperationException($"[StrategicMap] Factions 列表存在无效项，索引={i}");
                }

                if (faction.GlobalInfluenceMap == null || faction.GlobalInfluenceMap.Length != expectedLength)
                {
                    if (!aiConfig.FallbackToLegacyModel)
                    {
                        throw new InvalidOperationException(
                            $"[StrategicMap] 势力 {faction.Name} 的 GlobalInfluenceMap 无效，长度={faction.GlobalInfluenceMap?.Length ?? -1}，期望={expectedLength}");
                    }

                    RefreshLegacyModel(viewerFaction);
                    return;
                }

                allFactions[i] = faction;
            }

            float influenceScale = aiConfig.NetEnergyToInfluenceScale;
            float threatScale = aiConfig.NetEnergyToThreatScale;
            float threatClamp = aiConfig.ThreatClamp;

            for (int y = 0; y < mapHeight; y++)
            {
                int rowOffset = y * mapWidth;
                for (int x = 0; x < mapWidth; x++)
                {
                    int index = rowOffset + x;
                    int ownEnergy = viewerFaction.GlobalInfluenceMap[index].EffectiveTotalEnergy;

                    int maxEnemyEnergy = 0;
                    for (int i = 0; i < factionCount; i++)
                    {
                        Faction otherFaction = allFactions[i];
                        if (otherFaction == viewerFaction) continue;

                        int otherEnergy = otherFaction.GlobalInfluenceMap[index].EffectiveTotalEnergy;
                        if (otherEnergy > maxEnemyEnergy)
                        {
                            maxEnemyEnergy = otherEnergy;
                        }
                    }

                    int netEnergy = ownEnergy - maxEnemyEnergy;
                    float influence = netEnergy * influenceScale;
                    float threat = netEnergy < 0 ? Math.Min((-netEnergy) * threatScale, threatClamp) : 0f;

                    _influenceMap[x, y] = influence;
                    _threatMap[x, y] = threat;
                }
            }

            _lastUpdate = DateTime.Now;
        }

        private void RefreshLegacyModel(Faction faction)
        {
            Clear();

            // Legacy 模型：基于人口/兵力扩散估算势能
            foreach (var obj in Session.Current.Scenario.Factions.GetList())
            {
                if (!(obj is Faction otherFaction) || otherFaction.Destroyed) continue;
                CalculateFactionInfluence(faction, otherFaction);
            }

            _lastUpdate = DateTime.Now;
        }

        /// <summary>
        /// 计算势力影响力
        /// </summary>
        private void CalculateFactionInfluence(Faction viewerFaction, Faction targetFaction)
        {
            try
            {
                // 计算建筑影响力
                foreach (var obj in targetFaction.Architectures.GetList())
                {
                    if (!(obj is Architecture arch) || arch.BelongedFaction == null) continue;

                    Point pos = arch.Position;
                    float influence = CalculateArchitectureInfluence(arch);
                    
                    // 如果是敌对势力，设为负值（威胁）
                    if (arch.BelongedFaction != viewerFaction && !viewerFaction.IsFriendly(arch.BelongedFaction))
                    {
                        influence = -influence;
                        SetThreatValue(pos.X, pos.Y, Math.Abs(influence));
                    }
                    
                    SetInfluenceValue(pos.X, pos.Y, influence);
                    
                    // 扩散影响力到周围区域
                    SpreadInfluence(pos.X, pos.Y, influence * 0.5f, 3);
                }

                // 计算部队影响力
                foreach (var obj in targetFaction.Troops.GetList())
                {
                    if (!(obj is Troop troop) || troop.Destroyed) continue;

                    Point pos = troop.Position;
                    float influence = CalculateTroopInfluence(troop);
                    
                    // 如果是敌对势力，设为负值（威胁）
                    if (troop.BelongedFaction != viewerFaction && !viewerFaction.IsFriendly(troop.BelongedFaction))
                    {
                        influence = -influence;
                        SetThreatValue(pos.X, pos.Y, Math.Abs(influence));
                    }
                    
                    SetInfluenceValue(pos.X, pos.Y, influence);
                    
                    // 扩散影响力到周围区域
                    SpreadInfluence(pos.X, pos.Y, influence * 0.3f, 2);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CalculateFactionInfluence] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算建筑影响力
        /// </summary>
        private float CalculateArchitectureInfluence(Architecture arch)
        {
            if (arch == null) return 0f;

            float influence = 0f;
            
            // 基础影响力（基于人口和设施）
            influence += arch.Population * 0.01f;
            influence += arch.Fund * 0.001f;
            influence += arch.Food * 0.001f;
            
            // 军事影响力
            influence += arch.RecruitmentMilitaryList.Count * 0.1f;
            
            // 防御影响力
            influence += arch.Endurance * 0.05f;
            
            return Math.Min(influence, 100f); // 限制最大值
        }

        /// <summary>
        /// 计算部队影响力
        /// </summary>
        private float CalculateTroopInfluence(Troop troop)
        {
            if (troop?.Leader == null) return 0f;

            float influence = 0f;
            
            // 基础战斗力
            influence += troop.FightingForce * 0.1f;
            
            // 指挥官能力
            influence += troop.Leader.Command * 0.5f;
            influence += troop.Leader.Strength * 0.3f;
            
            // 士气影响
            influence += troop.Morale * 0.1f;
            
            return Math.Min(influence, 50f); // 限制最大值
        }

        /// <summary>
        /// 扩散影响力
        /// </summary>
        private void SpreadInfluence(int centerX, int centerY, float baseInfluence, int radius)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (!IsValidPosition(x, y)) continue;
                    
                    int distance = Math.Max(Math.Abs(x - centerX), Math.Abs(y - centerY));
                    if (distance == 0) continue; // 跳过中心点
                    
                    float decayFactor = 1.0f / (distance + 1);
                    float spreadInfluence = baseInfluence * decayFactor;
                    
                    AddInfluenceValue(x, y, spreadInfluence);
                }
            }
        }

        /// <summary>
        /// 寻找最受威胁的位置
        /// </summary>
        public Point FindMostThreatenedPosition(Faction faction)
        {
            Point mostThreatened = Point.Zero;
            float maxThreat = 0f;

            try
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        float threat = GetThreatValue(x, y);
                        if (threat > maxThreat)
                        {
                            maxThreat = threat;
                            mostThreatened = new Point(x, y);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindMostThreatenedPosition] 错误: {ex.Message}");
            }

            return mostThreatened;
        }

        /// <summary>
        /// 获取位置的影响力值
        /// </summary>
        public float GetInfluenceValue(int x, int y)
        {
            if (!IsValidPosition(x, y)) return 0f;
            return _influenceMap[x, y];
        }

        /// <summary>
        /// 获取位置的威胁值
        /// </summary>
        public float GetThreatValue(int x, int y)
        {
            if (!IsValidPosition(x, y)) return 0f;
            return _threatMap[x, y];
        }

        /// <summary>
        /// 设置位置的影响力值
        /// </summary>
        private void SetInfluenceValue(int x, int y, float value)
        {
            if (!IsValidPosition(x, y)) return;
            _influenceMap[x, y] = value;
        }

        /// <summary>
        /// 设置位置的威胁值
        /// </summary>
        private void SetThreatValue(int x, int y, float value)
        {
            if (!IsValidPosition(x, y)) return;
            _threatMap[x, y] = value;
        }

        /// <summary>
        /// 添加影响力值
        /// </summary>
        private void AddInfluenceValue(int x, int y, float value)
        {
            if (!IsValidPosition(x, y)) return;
            _influenceMap[x, y] += value;
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        /// <summary>
        /// 清空地图
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _influenceMap[x, y] = 0f;
                    _threatMap[x, y] = 0f;
                }
            }
        }

        /// <summary>
        /// 获取地图统计信息
        /// </summary>
        public string GetMapStats()
        {
            float totalInfluence = 0f;
            float totalThreat = 0f;
            int activePoints = 0;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float influence = Math.Abs(_influenceMap[x, y]);
                    float threat = _threatMap[x, y];
                    
                    if (influence > 0.1f || threat > 0.1f)
                    {
                        activePoints++;
                        totalInfluence += influence;
                        totalThreat += threat;
                    }
                }
            }

            return $"战略地图统计:\n" +
                   $"地图尺寸: {_width}x{_height}\n" +
                   $"活跃点数: {activePoints}\n" +
                   $"总影响力: {totalInfluence:F1}\n" +
                   $"总威胁值: {totalThreat:F1}\n" +
                   $"上次更新: {_lastUpdate:HH:mm:ss}";
        }

        /// <summary>
        /// 获取指定位置的影响力值
        /// </summary>
        public float GetInfluence(Point position)
        {
            try
            {
                if (position.X >= 0 && position.X < _width && position.Y >= 0 && position.Y < _height)
                {
                    return _influenceMap[position.X, position.Y];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StrategicMap] GetInfluence error: {ex.Message}");
            }
            
            return 0f;
        }

        /// <summary>
        /// 获取指定位置的威胁值
        /// </summary>
        public float GetThreat(Point position)
        {
            try
            {
                if (position.X >= 0 && position.X < _width && position.Y >= 0 && position.Y < _height)
                {
                    return _threatMap[position.X, position.Y];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StrategicMap] GetThreat error: {ex.Message}");
            }
            
            return 0f;
        }
    }
}
