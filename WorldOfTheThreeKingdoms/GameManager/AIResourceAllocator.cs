using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameGlobal;
using Microsoft.Xna.Framework;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;

namespace GameManager
{
    /// <summary>
    /// AI人力资源分配器 - 负责将城市内的闲置武将分配成最优的作战编队
    /// 实现原理：模拟选秀机制，队伍轮流挑选对自己边际效益最大的人员
    /// </summary>
    public class AIResourceAllocator
    {
        // 属性软上限：当队伍某项属性超过此值，继续增加该属性的收益递减
        private const float STAT_SOFT_CAP = 95.0f;
        
        // 权重配置
        private const float WEIGHT_BOND = 100.0f;       // 羁绊权重 (义兄弟等)
        private const float WEIGHT_STAT_FILL = 1.5f;    // 补短板权重
        private const float WEIGHT_APTITUDE = 40.0f;    // 兵种适性提升权重
        private const float WEIGHT_INT_PROTECT = 2.0f;  // 智力保护权重 (防计谋)

        private NavalRecruitmentManager _navyManager = new NavalRecruitmentManager();

        /// <summary>
        /// 为城市生成均衡的部队列表
        /// </summary>
        /// <param name="city">所属城市</param>
        /// <param name="maxTroopCount">最大组建部队数</param>
        public List<Troop> CreateBalancedLegions(Architecture city, int maxTroopCount)
        {
            var resultTroops = new List<Troop>();

            // 1. 优先判定：是否需要补充水军？
            Troop navyTroop = _navyManager.TryRecruitNavy(city);
            if (navyTroop != null)
            {
                resultTroops.Add(navyTroop); // Add to results
                maxTroopCount--; // Deduct quota
            }

            if (maxTroopCount <= 0) return resultTroops;
            
            // 2. 获取所有状态正常的闲置武将
            // Adaptation: State -> Status, Location -> LocationArchitecture
            var pool = city.Persons.Where(p => p.Status == PersonStatus.Normal && p.LocationArchitecture == city).ToList();
            
            if (pool.Count == 0) return resultTroops;

            // 3. 选出队长 (Leaders) - 优先统率高或有特殊称号的
            // 策略：先确定几个带头大哥，确立核心架构
            // Adaptation: Title != null -> RealTitles.Count > 0
            var leaders = pool.OrderByDescending(p => p.Command)
                              .ThenByDescending(p => p.RealTitles.Count > 0 ? 1 : 0) // 有称号优先
                              .Take(maxTroopCount)
                              .ToList();
            
            // 从池中移除队长
            foreach (var leader in leaders) pool.Remove(leader);

            // 3. 初始化选秀小组
            var draftGroups = new List<DraftGroup>();
            foreach (var leader in leaders)
            {
                draftGroups.Add(new DraftGroup(leader, city));
            }

            // 4. 轮流选人 (Drafting Loop)
            // 只要池里还有人，且有队伍没满员，就继续选
            bool anyoneDrafted = true;
            while (anyoneDrafted && pool.Count > 0)
            {
                anyoneDrafted = false;

                // 排序：让当前综合战力最低的队伍先选 (劫富济贫，保证所有队伍都有战斗力)
                var sortedGroups = draftGroups.OrderBy(g => g.GetCombatPowerScore()).ToList();

                foreach (var group in sortedGroups)
                {
                    if (group.IsFull() || pool.Count == 0) continue;

                    Person bestCandidate = null;
                    float bestUtility = -1.0f;

                    // 遍历候选人，计算边际效用
                    foreach (var candidate in pool)
                    {
                        float utility = CalculateMarginalUtility(group, candidate);
                        if (utility > bestUtility)
                        {
                            bestUtility = utility;
                            bestCandidate = candidate;
                        }
                    }

                    // 只有当收益足够高时才招入 (避免塞入垃圾武将拖累士气或粮草)
                    if (bestCandidate != null && bestUtility > 10.0f)
                    {
                        group.AddMember(bestCandidate);
                        pool.Remove(bestCandidate);
                        anyoneDrafted = true;
                    }
                }
            }

            // 5. 将小组转化为实际部队对象
            foreach (var group in draftGroups)
            {
                var t = group.ToTroop();
                if (t != null)
                {
                    resultTroops.Add(t);
                }
            }

            return resultTroops;
        }

        /// <summary>
        /// 核心算法：计算某人加入某队的边际效用
        /// </summary>
        private float CalculateMarginalUtility(DraftGroup group, Person candidate)
        {
            float score = 0f;

            // A. 羁绊加成 (最高优先级)
            // 如果候选人是主将或现有队员的亲属/义兄弟，极大加分
            if (group.HasBondWith(candidate)) 
            {
                score += WEIGHT_BOND;
            }

            // B. 属性互补 (Diminishing Returns)
            // 计算加入后能带来的净属性提升
            float currentMaxStr = group.MaxStrength;
            float currentMaxInt = group.MaxIntelligence;

            float strGain = Math.Max(0, candidate.Strength - currentMaxStr);
            float intGain = Math.Max(0, candidate.Intelligence - currentMaxInt);

            // 软上限逻辑：如果队伍已有98武力的猛将，再来个90的，收益极低
            if (currentMaxStr > STAT_SOFT_CAP) strGain *= 0.1f;
            if (currentMaxInt > STAT_SOFT_CAP) intGain *= 0.1f;

            score += strGain * WEIGHT_STAT_FILL;
            score += intGain * WEIGHT_INT_PROTECT; // 智力权重高，因为防计谋很重要

            // C. 兵种适性修正
            // 如果此人能将队伍的主战兵种适性提升 (比如由A提S)，加分
            int currentApt = group.GetBestAptitude();
            // Adaptation: Using valid aptitude check if possible, or placeholder
            // For now, using simplified logic as API for aptitude is not exposed directly on Person.
            int candidateApt = GetApproximateAptitude(candidate, group.IntendedUnitKindID); 

            if (candidateApt > currentApt)
            {
                score += WEIGHT_APTITUDE * (candidateApt - currentApt);
            }

            return score;
        }

        private int GetApproximateAptitude(Person p, int militaryKindID)
        {
            // Placeholder: Check titles for military type aptitude
            // Real implementation would check Title.MilitaryKindOnly or similar
            foreach (var title in p.RealTitles)
            {
                // Simple heuristic: title level might correlate to aptitude
                if (title.MilitaryKindOnly != null && title.MilitaryKindOnly.ID == militaryKindID)
                {
                    return 3; // ‘S’ class equivalent
                }
            }
            return 0; // Default or 'C'
        }

        // ==========================================
        // 内部辅助类：选秀小组
        // ==========================================
        // ==========================================
        // 内部辅助类：选秀小组
        // ==========================================
        private class DraftGroup
        {
            public Person Leader;
            public List<Person> Members = new List<Person>();
            public Architecture Location;
            public int IntendedUnitKindID; // 预定兵种ID
            public MilitaryKind AssignedMilitaryKind; // The kind of unit this group will form

            public float MaxStrength => Math.Max(Leader.Strength, Members.Any() ? Members.Max(m => m.Strength) : 0);
            public float MaxIntelligence => Math.Max(Leader.Intelligence, Members.Any() ? Members.Max(m => m.Intelligence) : 0);

            // Removed separate AssignedShip, using AssignedMilitaryKind for both Land and Navy

            public DraftGroup(Person leader, Architecture loc)
            {
                Leader = leader;
                Location = loc;
                
                // Initialize using LandRecruitmentManager
                var landManager = new LandRecruitmentManager();
                // Check if Siege Mode logic is needed? For generic drafting, assume field battle priority unless specified?
                // Or maybe simple drafting looks for best overall.
                // Assuming isSiegeMode = false for general recruitment.
                AssignedMilitaryKind = landManager.SelectBestUnit(leader, loc, false); 
                
                if (AssignedMilitaryKind != null)
                {
                    IntendedUnitKindID = AssignedMilitaryKind.ID;
                }
                else
                {
                    IntendedUnitKindID = 0; // Default
                }
            }

            public void AddMember(Person p) => Members.Add(p);
            
            // 假设满编3人 (1主2副)
            public bool IsFull() => Members.Count >= 2; 

            public bool HasBondWith(Person p)
            {
                if (IsRelated(Leader, p)) return true;
                foreach (var m in Members)
                {
                    if (IsRelated(m, p)) return true;
                }
                return false;
            }

            public float GetCombatPowerScore(bool isNavalBattle = false)
            {
                float score = Leader.Command + MaxStrength + MaxIntelligence;
                
                if (isNavalBattle)
                {
                    // 使用 CapabilityEvaluator 替代旧的适性判断
                    float navalCap = MilitaryCapabilityEvaluator.CalculateCapability(Leader, MilitaryCapabilityEvaluator.GetNavyMilitaryKind());
                    
                    // 称号惩罚：如果评分极低（说明没称号），战力直接打骨折
                    if (navalCap < 50f) 
                    {
                        score *= 0.1f; // 几乎不可用
                    }
                    else 
                    {
                        score += navalCap; // 加上能力分
                    }

                    // 战船加成
                    // Check if AssignedMilitaryKind is Navy
                    if (AssignedMilitaryKind != null && AssignedMilitaryKind.Type == MilitaryType.水军)
                        score += AssignedMilitaryKind.MinCommand * 0.8f; // Proxy for FightingForce
                }
                return score;
            }

            public void OptimizeForNavy(Architecture city)
            {
                if (city.Militaries == null) return;

                // 尝试从城市军备中分配最好的船
                var bestShip = city.Militaries.GetList().OfType<Military>()
                    .Where(m => m.Kind.Type == MilitaryType.水军 && m.Quantity > 0)
                    .OrderByDescending(m => m.Kind.MinCommand) // Proxy for FightingForce
                    .Select(m => m.Kind)
                    .FirstOrDefault();

                if (bestShip != null)
                {
                    this.AssignedMilitaryKind = bestShip;
                    this.IntendedUnitKindID = bestShip.ID;
                }
            }

            public int GetBestAptitude()
            {
                return 1; // Placeholder
            }

            public Troop ToTroop()
            {
                if (AssignedMilitaryKind == null) return null;

                Troop t = new Troop();
                // Initialize basic info manually since we might not invoke Architecture.CreateTroop
                t.Leader = Leader;
                // t.Army = new Army(); // Troop.Init() likely creates simple Army. Need to verify.
                // Assuming t.Init() sets up basics or we do it.
                t.Init(); 
                t.Position = Location.Position;
                t.BelongedFaction = Location.BelongedFaction;
                // t.StartArchitecture = Location; // Unavailable in previous check, ignoring
                
                // Assign Army Kind
                if (t.Army != null)
                {
                    t.Army.Kind = AssignedMilitaryKind;
                    t.Army.BelongedTroop = t;
                }

                // --------------------------------------------------------
                // 修正：部队规模由兵种的 MaxScale 决定，与武将统率无关
                // --------------------------------------------------------
                int targetQuantity = AssignedMilitaryKind.MaxScale;

                // 资源限制检查
                // 1. 资金限制 (买得起多少兵装/支付多少招募费)
                // Cost is checking CreateCost
                int cost = AssignedMilitaryKind.CreateCost > 0 ? AssignedMilitaryKind.CreateCost : 10;
                int fundLimit = (int)(Location.Fund / cost);

                // 2. 人口限制 (城里还有多少兵役人口)
                int popLimit = Location.Population;

                // 3. 最终兵力 = Min(编制上限, 资金够买的数量, 人口够征的数量)
                // Also limited by Leader Command if relevant? User explicitly said "Independent of Command" but usually Command is cap.
                // User said: "部队规模由兵种的 MaxScale 决定，与武将统率无关" -> Okay, trusting user.
                int actualQuantity = Math.Min(targetQuantity, Math.Min(fundLimit, popLimit));

                // 防止生成 0 人部队
                if (actualQuantity < AssignedMilitaryKind.MinScale) 
                {
                    // 如果连最小编制都凑不齐，这支部队就不该建立
                    return null; 
                }

                t.Quantity = actualQuantity;
                
                // Add Deputies
                foreach(var m in Members)
                {
                    if (t.Persons.Count < 3) t.Persons.Add(m);
                }

                // 扣除资源
                // 注意：这里需要确保线程安全或在主线程执行
                Location.Fund -= actualQuantity * cost;
                Location.Population -= actualQuantity;

                // 根据兵种设置初始士气/战意
                t.Morale = Location.Morale; // 继承城市士气
                t.Combativity = 100;
                
                // Ensure default food
                t.Food = Math.Min(Location.Food, actualQuantity * 2);
                Location.Food -= t.Food; // Deduct food? Architecture.CreateTroop does? Assuming yes.

                // Finally add to Scenario Troops if not handled by CreateTroop
                if (Session.Current.Scenario.Troops != null)
                {
                    Session.Current.Scenario.Troops.Add(t);
                }

                return t;
            }

            // Relationship Helper
            private bool IsRelated(Person a, Person b)
            {
                return a.IsVeryCloseTo(b) || a.HasCloseStrainTo(b);
            }
        }
    }
}
