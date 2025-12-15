using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// AI战术协调器 - 管理多个部队之间的协作和战术配合
    /// </summary>
    public class AITacticalCoordinator
    {
        public static AITacticalCoordinator Instance { get; private set; }

        // 战术编队类型
        public enum FormationType
        {
            None,           // 无编队
            Line,           // 一字长蛇阵
            Wedge,          // 楔形阵
            Box,            // 方阵
            Pincer,         // 钳形攻击
            Ambush,         // 伏击阵型
            Defensive       // 防御阵型
        }

        // 战术任务类型
        public enum TacticalTask
        {
            Attack,         // 攻击
            Defend,         // 防御
            Flank,          // 侧翼包抄
            Support,        // 支援
            Retreat,        // 撤退
            Patrol,         // 巡逻
            Ambush          // 伏击
        }

        // 战术小组
        public class TacticalGroup
        {
            public int GroupId { get; set; }
            public List<Troop> Troops { get; set; } = new List<Troop>();
            public FormationType Formation { get; set; } = FormationType.None;
            public TacticalTask CurrentTask { get; set; } = TacticalTask.Attack;
            public Point TargetPosition { get; set; }
            public Troop Commander { get; set; } // 指挥官（通常是能力最高的）
            public DateTime LastUpdate { get; set; } = DateTime.Now;
        }

        private Dictionary<int, TacticalGroup> _tacticalGroups;
        private int _nextGroupId = 1;

        public AITacticalCoordinator()
        {
            Instance = this;
            _tacticalGroups = new Dictionary<int, TacticalGroup>();
        }

        /// <summary>
        /// 为势力的所有部队创建战术编组
        /// </summary>
        /// <param name="faction">势力</param>
        public void OrganizeTacticalGroups(Faction faction)
        {
            try
            {
                if (faction?.Troops == null) return;

                // 清理该势力的现有编组
                ClearFactionGroups(faction);

                var availableTroops = faction.Troops.GetList()
                    .Where(t => t.Status == TroopStatus.一般 && t.Leader != null)
                    .ToList();

                if (availableTroops.Count == 0) return;

                // 根据部队数量和位置创建编组
                if (availableTroops.Count <= 3)
                {
                    // 小规模：单一编组
                    CreateTacticalGroup(availableTroops, FormationType.Line);
                }
                else if (availableTroops.Count <= 6)
                {
                    // 中等规模：分为两个编组
                    var group1 = availableTroops.Take(availableTroops.Count / 2).ToList();
                    var group2 = availableTroops.Skip(availableTroops.Count / 2).ToList();
                    
                    CreateTacticalGroup(group1, FormationType.Wedge);
                    CreateTacticalGroup(group2, FormationType.Line);
                }
                else
                {
                    // 大规模：分为多个专业化编组
                    var groups = SplitIntoSpecializedGroups(availableTroops);
                    foreach (var group in groups)
                    {
                        CreateTacticalGroup(group.troops, group.formation);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrganizeTacticalGroups] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建战术编组
        /// </summary>
        private TacticalGroup CreateTacticalGroup(List<Troop> troops, FormationType formation)
        {
            if (troops == null || troops.Count == 0) return null;

            var group = new TacticalGroup
            {
                GroupId = _nextGroupId++,
                Troops = new List<Troop>(troops),
                Formation = formation,
                Commander = SelectCommander(troops),
                LastUpdate = DateTime.Now
            };

            // 设置初始目标位置（编组中心）
            group.TargetPosition = CalculateGroupCenter(troops);

            _tacticalGroups[group.GroupId] = group;
            return group;
        }

        /// <summary>
        /// 将部队分为专业化编组
        /// </summary>
        private List<(List<Troop> troops, FormationType formation)> SplitIntoSpecializedGroups(List<Troop> troops)
        {
            var groups = new List<(List<Troop>, FormationType)>();

            // 按军种分类
            var cavalry = troops.Where(t => t.Army.Kind.Type == MilitaryType.骑兵).ToList();
            var infantry = troops.Where(t => t.Army.Kind.Type == MilitaryType.步兵).ToList();
            var archers = troops.Where(t => t.Army.Kind.Type == MilitaryType.弩兵).ToList();
            var siege = troops.Where(t => t.Army.Kind.Type == MilitaryType.器械).ToList();

            // 骑兵编组 - 用于快速突击
            if (cavalry.Count > 0)
            {
                groups.Add((cavalry, FormationType.Wedge));
            }

            // 步兵编组 - 主力部队
            if (infantry.Count > 0)
            {
                if (infantry.Count > 4)
                {
                    // 分为两个步兵编组
                    var inf1 = infantry.Take(infantry.Count / 2).ToList();
                    var inf2 = infantry.Skip(infantry.Count / 2).ToList();
                    groups.Add((inf1, FormationType.Line));
                    groups.Add((inf2, FormationType.Box));
                }
                else
                {
                    groups.Add((infantry, FormationType.Line));
                }
            }

            // 弓兵编组 - 远程支援
            if (archers.Count > 0)
            {
                groups.Add((archers, FormationType.Defensive));
            }

            // 攻城器械编组 - 特殊任务
            if (siege.Count > 0)
            {
                groups.Add((siege, FormationType.Box));
            }

            return groups;
        }

        /// <summary>
        /// 选择编组指挥官
        /// </summary>
        private Troop SelectCommander(List<Troop> troops)
        {
            if (troops == null || troops.Count == 0) return null;

            // 选择综合能力最高的将领作为指挥官
            return troops.OrderByDescending(t => 
                t.Leader.Command + t.Leader.Intelligence + t.Leader.Braveness + t.Leader.Calmness
            ).First();
        }

        /// <summary>
        /// 计算编组中心位置
        /// </summary>
        private Point CalculateGroupCenter(List<Troop> troops)
        {
            if (troops == null || troops.Count == 0) return Point.Zero;

            int avgX = (int)troops.Average(t => t.Position.X);
            int avgY = (int)troops.Average(t => t.Position.Y);
            return new Point(avgX, avgY);
        }

        /// <summary>
        /// 更新战术编组的行动
        /// </summary>
        /// <param name="faction">势力</param>
        public void UpdateTacticalGroups(Faction faction)
        {
            try
            {
                var factionGroups = GetFactionGroups(faction);
                
                foreach (var group in factionGroups)
                {
                    UpdateGroupFormation(group);
                    AssignGroupTasks(group);
                    CoordinateGroupMovement(group);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateTacticalGroups] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新编组阵型
        /// </summary>
        private void UpdateGroupFormation(TacticalGroup group)
        {
            if (group?.Troops == null || group.Troops.Count == 0) return;

            var targetPositions = CalculateFormationPositions(group);
            
            for (int i = 0; i < Math.Min(group.Troops.Count, targetPositions.Count); i++)
            {
                var troop = group.Troops[i];
                var targetPos = targetPositions[i];
                
                // 如果部队距离目标位置太远，设置移动目标
                int distance = Session.Current.Scenario.GetSimpleDistance(troop.Position, targetPos);
                if (distance > 2)
                {
                    troop.Destination = targetPos;
                }
            }
        }

        /// <summary>
        /// 计算阵型位置
        /// </summary>
        private List<Point> CalculateFormationPositions(TacticalGroup group)
        {
            var positions = new List<Point>();
            var center = group.TargetPosition;
            var troopCount = group.Troops.Count;

            switch (group.Formation)
            {
                case FormationType.Line:
                    // 一字长蛇阵
                    for (int i = 0; i < troopCount; i++)
                    {
                        positions.Add(new Point(center.X + i - troopCount / 2, center.Y));
                    }
                    break;

                case FormationType.Wedge:
                    // 楔形阵
                    positions.Add(center); // 尖端
                    for (int i = 1; i < troopCount; i++)
                    {
                        int row = (i + 1) / 2;
                        int side = (i % 2 == 1) ? -1 : 1;
                        positions.Add(new Point(center.X + side * row, center.Y + row));
                    }
                    break;

                case FormationType.Box:
                    // 方阵
                    int sideLength = (int)Math.Ceiling(Math.Sqrt(troopCount));
                    for (int i = 0; i < troopCount; i++)
                    {
                        int row = i / sideLength;
                        int col = i % sideLength;
                        positions.Add(new Point(
                            center.X + col - sideLength / 2,
                            center.Y + row - sideLength / 2
                        ));
                    }
                    break;

                case FormationType.Defensive:
                    // 防御阵型 - 弧形
                    for (int i = 0; i < troopCount; i++)
                    {
                        double angle = (Math.PI / (troopCount - 1)) * i - Math.PI / 2;
                        int radius = 3;
                        positions.Add(new Point(
                            center.X + (int)(Math.Cos(angle) * radius),
                            center.Y + (int)(Math.Sin(angle) * radius)
                        ));
                    }
                    break;

                default:
                    // 默认：简单散开
                    for (int i = 0; i < troopCount; i++)
                    {
                        positions.Add(new Point(center.X + i % 3 - 1, center.Y + i / 3 - 1));
                    }
                    break;
            }

            return positions;
        }

        /// <summary>
        /// 分配编组任务
        /// </summary>
        private void AssignGroupTasks(TacticalGroup group)
        {
            if (group?.Commander == null) return;

            // 根据指挥官性格和战场情况分配任务
            var commander = group.Commander.Leader;
            var nearbyEnemies = GetNearbyEnemies(group, 10);

            if (nearbyEnemies.Count > 0)
            {
                if (commander.Braveness > 7)
                {
                    group.CurrentTask = TacticalTask.Attack;
                }
                else if (commander.Calmness > 7)
                {
                    group.CurrentTask = TacticalTask.Defend;
                }
                else
                {
                    // 根据兵力对比决定
                    float ourStrength = group.Troops.Sum(t => t.FightingForce);
                    float enemyStrength = nearbyEnemies.Sum(t => t.FightingForce);
                    
                    if (ourStrength > enemyStrength * 1.2f)
                    {
                        group.CurrentTask = TacticalTask.Attack;
                    }
                    else
                    {
                        group.CurrentTask = TacticalTask.Defend;
                    }
                }
            }
            else
            {
                group.CurrentTask = TacticalTask.Patrol;
            }
        }

        /// <summary>
        /// 协调编组移动
        /// </summary>
        private void CoordinateGroupMovement(TacticalGroup group)
        {
            if (group?.Troops == null) return;

            switch (group.CurrentTask)
            {
                case TacticalTask.Attack:
                    CoordinateAttackMovement(group);
                    break;
                case TacticalTask.Defend:
                    CoordinateDefensiveMovement(group);
                    break;
                case TacticalTask.Patrol:
                    CoordinatePatrolMovement(group);
                    break;
            }
        }

        /// <summary>
        /// 协调攻击移动
        /// </summary>
        private void CoordinateAttackMovement(TacticalGroup group)
        {
            var nearbyEnemies = GetNearbyEnemies(group, 15);
            if (nearbyEnemies.Count > 0)
            {
                // 选择最近的敌人作为目标
                var target = nearbyEnemies.OrderBy(e => 
                    Session.Current.Scenario.GetSimpleDistance(group.TargetPosition, e.Position)
                ).First();

                group.TargetPosition = target.Position;
            }
        }

        /// <summary>
        /// 协调防御移动
        /// </summary>
        private void CoordinateDefensiveMovement(TacticalGroup group)
        {
            // 寻找有利的防御位置
            // 这里可以添加地形分析逻辑
            
            // 暂时保持当前位置
            group.TargetPosition = CalculateGroupCenter(group.Troops);
        }

        /// <summary>
        /// 协调巡逻移动
        /// </summary>
        private void CoordinatePatrolMovement(TacticalGroup group)
        {
            // 简单的巡逻逻辑 - 可以扩展为更复杂的路径
            var currentCenter = CalculateGroupCenter(group.Troops);
            
            // 每隔一段时间改变巡逻目标
            if ((DateTime.Now - group.LastUpdate).TotalSeconds > 30)
            {
                var random = new Random();
                group.TargetPosition = new Point(
                    currentCenter.X + random.Next(-10, 11),
                    currentCenter.Y + random.Next(-10, 11)
                );
                group.LastUpdate = DateTime.Now;
            }
        }

        /// <summary>
        /// 获取编组附近的敌军
        /// </summary>
        private List<Troop> GetNearbyEnemies(TacticalGroup group, int radius)
        {
            var enemies = new List<Troop>();
            
            if (group?.Troops == null || group.Troops.Count == 0) return enemies;

            var faction = group.Troops[0].BelongedFaction;
            if (faction == null) return enemies;

            var center = group.TargetPosition;
            
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    var checkPos = new Point(x, y);
                    var troop = Session.Current.Scenario.GetTroopByPosition(checkPos);
                    
                    if (troop != null && !faction.IsFriendly(troop.BelongedFaction))
                    {
                        enemies.Add(troop);
                    }
                }
            }

            return enemies;
        }

        /// <summary>
        /// 获取势力的所有战术编组
        /// </summary>
        private List<TacticalGroup> GetFactionGroups(Faction faction)
        {
            return _tacticalGroups.Values
                .Where(g => g.Troops.Any(t => t.BelongedFaction == faction))
                .ToList();
        }

        /// <summary>
        /// 清理势力的编组
        /// </summary>
        private void ClearFactionGroups(Faction faction)
        {
            var groupsToRemove = _tacticalGroups.Values
                .Where(g => g.Troops.Any(t => t.BelongedFaction == faction))
                .Select(g => g.GroupId)
                .ToList();

            foreach (var groupId in groupsToRemove)
            {
                _tacticalGroups.Remove(groupId);
            }
        }

        /// <summary>
        /// 获取编组信息（用于调试）
        /// </summary>
        public string GetGroupInfo(int groupId)
        {
            if (!_tacticalGroups.ContainsKey(groupId)) return "编组不存在";

            var group = _tacticalGroups[groupId];
            return $"编组 {groupId}: {group.Troops.Count} 部队, 阵型: {group.Formation}, 任务: {group.CurrentTask}";
        }

        /// <summary>
        /// 清理所有编组
        /// </summary>
        public void ClearAllGroups()
        {
            _tacticalGroups.Clear();
            _nextGroupId = 1;
        }
    }
}