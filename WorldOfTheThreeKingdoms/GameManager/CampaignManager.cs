using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameManager
{
    public enum CampaignGoal { Siege, Intercept, FieldBattle }

    /// <summary>
    /// 战役管理器 - 负责指挥多支部队协同攻打一个目标
    /// </summary>
    public class CampaignManager
    {
        private List<Campaign> _activeCampaigns = new List<Campaign>();

        /// <summary>
        /// 发起一次围城战役
        /// </summary>
        public void StartSiegeCampaign(Faction attacker, Architecture targetCity)
        {
            // 1. 检查是否已经有针对该目标的战役
            if (_activeCampaigns.Any(c => c.TargetCity == targetCity)) return;

            var campaign = new Campaign(targetCity);
            
            // 2. 调集部队 (搜索目标周围20格内的友军)
            var availableTroops = GetAvailableTroops(attacker, targetCity.Position, 20);
            
            if (availableTroops.Count == 0) return;

            // 3. 将部队加入战役
            foreach (var t in availableTroops)
            {
                campaign.AddTroop(t);
            }

            _activeCampaigns.Add(campaign);
            
            // System.Diagnostics.Debug.WriteLine($"[战役] {attacker.Name} 对 {targetCity.Name} 发起了围城战役，投入部队 {availableTroops.Count} 支");
        }

        public void PrepareSiegeTroops(Faction attacker, Architecture sourceCity)
        {
            // 假设我们需要一支攻城特遣队
            var landManager = new LandRecruitmentManager();

            if (sourceCity.Persons == null) return;

            // 挑选智力高（适合操作器械）的将领
            var siegeMasters = sourceCity.Persons.GetList().OfType<Person>()
                .Where(p => p.Intelligence > 80 && p.Status == PersonStatus.Normal && p.LocationArchitecture == sourceCity)
                .OrderByDescending(p => p.Intelligence)
                .Take(2);

            foreach (var master in siegeMasters)
            {
                // 强制开启 isSiegeMode = true
                // 此时管理器会优先返回 冲车/井阑/投石车
                MilitaryKind siegeUnit = landManager.SelectBestUnit(master, sourceCity, isSiegeMode: true);
                
                // 如果城市确实解锁了器械，并且这人能带
                if (siegeUnit != null && siegeUnit.Type == MilitaryType.器械)
                {
                    Troop t = new Troop();
                    t.ID = Session.Current.Scenario.Troops.GetFreeGameObjectID();
                    // Initialize basics
                    t.Leader = master;
                    t.Init();
                    t.StartingArchitecture = sourceCity;  // 🔥 根本修复：设置出发城市
                    t.Position = sourceCity.Position;
                    t.BelongedFaction = sourceCity.BelongedFaction;
                    
                    // 🔥 修复：创建正确的 Military 对象并分配
                    Military military = new Military();
                    military.ID = Session.Current.Scenario.Militaries.GetFreeGameObjectID();
                    military.Kind = siegeUnit;
                    military.Leader = master;
                    military.BelongedArchitecture = sourceCity;
                    military.Name = siegeUnit.Name + "队";
                    Session.Current.Scenario.Militaries.AddMilitary(military);
                    t.Army = military;

                    t.Quantity = 5000; // 器械部队通常不需要太多人
                    
                    // Simple Resource Deduction (Safety check needed?)
                    if (sourceCity.Population < t.Quantity) t.Quantity = sourceCity.Population;
                    if (t.Quantity < siegeUnit.MinScale) continue; // Not enough pop

                    int cost = siegeUnit.CreateCost > 0 ? siegeUnit.CreateCost : 10;
                    if (sourceCity.Fund < t.Quantity * cost) continue; // Not enough fund

                    sourceCity.Fund -= t.Quantity * cost;
                    sourceCity.Population -= t.Quantity;
                    t.Food = Math.Min(sourceCity.Food, t.Quantity * 3);
                    sourceCity.Food -= t.Food;

                    // ... 加入场景
                    if (Session.Current.Scenario.Troops != null)
                    {
                        Session.Current.Scenario.Troops.Add(t);
                    }
                    
                    // Note: These troops will be picked up by GetAvailableTroops in StartSiegeCampaign 
                    // if this is called before Starting campaign, or we can look up active campaigns.
                    // For now, just spawning them is the request.
                }
            }
        }

        public void Update()
        {
            // 清理已结束的战役 (目标被攻下或部队全灭)
            _activeCampaigns.RemoveAll(c => c.IsFinished());

            var difficulty = Session.Current.Scenario.Parameters.AIDifficulty;

            foreach (var campaign in _activeCampaigns)
            {
                campaign.UpdateStrategy(difficulty);
            }
        }

        private List<Troop> GetAvailableTroops(Faction f, Point center, int radius)
        {
            var list = new List<Troop>();
            if (f.Troops != null)
            {
                foreach(Troop t in f.Troops.GetList())
                {
                    if (Session.Current.Scenario.GetSimpleDistance(t.Position, center) <= radius)
                    {
                         // 不调动防守部队
                        if (t.CurrentTask != TroopTask.Defend)
                            list.Add(t);
                    }
                }
            }
            return list;
        }

        // ==========================================
        // 内部类：单次战役实例
        // ==========================================
        private class Campaign
        {
            public Architecture TargetCity;
            public List<Troop> AssignedTroops = new List<Troop>();
            
            public Campaign(Architecture target)
            {
                TargetCity = target;
            }

            public void AddTroop(Troop t)
            {
                if (!AssignedTroops.Contains(t)) AssignedTroops.Add(t);
            }

            public bool IsFinished()
            {
                // 结束条件：城池易主 或 我方部队全灭
                if (TargetCity.BelongedFaction != null && AssignedTroops.Count > 0 && AssignedTroops[0].BelongedFaction != null)
                {
                    return TargetCity.BelongedFaction == AssignedTroops[0].BelongedFaction;
                }
                return AssignedTroops.All(t => t.Destroyed);
            }

            public void UpdateStrategy(AIDifficulty difficulty)
            {
                if (TargetCity == null) return;

                // 如果是简单难度，禁用"等待集结"逻辑，直接让部队冲
                if (difficulty == AIDifficulty.Easy)
                {
                    foreach (var t in AssignedTroops)
                    {
                        if (!t.Destroyed)
                        {
                            t.RealDestination = TargetCity.Position; // 无脑冲锋
                        }
                    }
                    return;
                }

                // 1. 获取围城点位 (目标周围可通行的格子)
                var siegeSpots = GetSiegeSpots(TargetCity);
                
                // 2. 对部队进行排序分配
                // 逻辑：兵力多的优先分配，且优先去还没被占的位置
                var activeTroops = AssignedTroops.Where(t => !t.Destroyed).OrderByDescending(t => t.Quantity).ToList();

                for (int i = 0; i < activeTroops.Count; i++)
                {
                    var troop = activeTroops[i];
                    
                    // 如果部队已经在战斗中或很接近目标，保持攻击指令
                    if (Session.Current.Scenario.GetSimpleDistance(troop.Position, TargetCity.Position) <= 1)
                    {
                        // 已经在攻击位
                        continue;
                    }

                    // 分配一个未被占用的站位
                    if (i < siegeSpots.Count)
                    {
                        Point targetSpot = siegeSpots[i];
                        
                        // 下达移动指令
                        troop.RealDestination = targetSpot;
                        troop.CurrentTask = TroopTask.SiegeCoordination; 
                    }
                    else
                    {
                        // 没有位置了，就往城池中心挤
                        troop.RealDestination = TargetCity.Position;
                        troop.CurrentTask = TroopTask.SiegeCoordination;
                    }
                }

                // 水陆协同逻辑
                if (HasWaterPort(TargetCity))
                {
                    var navalTroops = AssignedTroops.Where(t => t.Army.Kind.Type == MilitaryType.水军).ToList();
                    
                    // 获取港口入口点
                    var portEntries = GetPortEntryPoints(TargetCity);
                    
                    for (int i = 0; i < navalTroops.Count; i++)
                    {
                        if (i < portEntries.Count)
                        {
                            // 水军任务：封锁港口，截断补给或退路
                            navalTroops[i].RealDestination = portEntries[i];
                            navalTroops[i].CurrentTask = TroopTask.Blockade; // Assuming Blockade exists in TroopTask
                        }
                        else
                        {
                            // 多余水军：游弋攻击
                            navalTroops[i].CurrentTask = TroopTask.Attack;
                        }
                    }
                }
            }

            private List<Point> GetSiegeSpots(Architecture city)
            {
                var spots = new List<Point>();
                // 获取城市周边的坐标 (这里简化为取周围4格)
                // 实际应该读取 ScenarioMap 的邻接数据
                var offsets = new Point[] { new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) };
                
                foreach (var offset in offsets)
                {
                    Point p = new Point(city.Position.X + offset.X, city.Position.Y + offset.Y);
                    
                    // 检查地图范围和通行性
                    if (!Session.Current.Scenario.PositionOutOfRange(p))
                    {
                        // Check passability if possible, or assume valid based on adjacency
                        // Ideally: Session.Current.Scenario.IsPassable(p)
                        spots.Add(p);
                    }
                }
                return spots;
            }

            private bool HasWaterPort(Architecture city)
            {
                // Proxy: Check if city type allows ships or has adjacent water
                // Using Kind property if available
                if (city.Kind.ShipCanEnter) return true;

                // Or check neighbors for water (Simple 4-direction check)
                foreach(var p in GetSiegeSpots(city))
                {
                    if (Session.Current.Scenario.GetTerrainKindByPosition(p) == TerrainKind.水域)
                        return true;
                }
                return false;
            }

            private List<Point> GetPortEntryPoints(Architecture city)
            {
                var points = new List<Point>();
                // Reuse SiegeSpots but filter for Water
                // Using GetSiegeSpots logic which returns valid adjacent spots
                var adj = GetSiegeSpots(city);
                foreach(var p in adj)
                {
                    if (Session.Current.Scenario.GetTerrainKindByPosition(p) == TerrainKind.水域)
                        points.Add(p);
                }
                return points;
            }
        }
    }
}
