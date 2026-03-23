using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;

namespace GameManager
{
    public class NavalRecruitmentManager
    {
        // 配置：临水城市维持水军的最低比例 (30%)
        private const float TARGET_NAVY_RATIO = 0.3f;

        /// <summary>
        /// 检查城市是否需要征召水军，如果需要，返回最佳的水军编制方案
        /// </summary>
        /// <returns>返回生成的部队，如果没有征兵条件则返回null</returns>
        public Troop TryRecruitNavy(Architecture city)
        {
            // 1. 基础检查：人口和资金
            if (city.Population < 5000 || city.Fund < 1000) return null;
            
            // Using Militaries count as proxy for troop count - AI shouldn't over-recruit
            int troopsCreated = city.Militaries != null ? city.Militaries.Count : 0;
            int troopLimit = 10; // Reasonable default limit
            if (troopsCreated >= troopLimit) return null;

            // 2. 环境与战略检查：我需要水军吗？
            float necessity = CalculateNavalNecessity(city);
            if (necessity < 0.3f) return null; // 内陆城市不需要专门编组水军

            // 3. 比例检查：现有水军够多了吗？
            // Count navy militaries from this city's military list
            int currentNavy = 0;
            int totalTroops = 0;
            if (city.Militaries != null)
            {
                foreach (GameObject obj in city.Militaries.GetList())
                {
                    if (obj is Military m && m.Kind != null)
                    {
                        totalTroops++;
                        if (m.Kind.Type == MilitaryType.水军)
                            currentNavy++;
                    }
                }
            }
            
            float currentRatio = (float)currentNavy / Math.Max(1, totalTroops);
            
            // 如果现有比例已经达标，就不急着征水军了（优先征陆军守城）
            if (currentRatio > TARGET_NAVY_RATIO * necessity) return null;

            // 4. 获取最佳水军编制 (Best Available Tech)
            // 只要科技解锁了，就能直接征，不需要库存
            MilitaryKind bestNavyKind = GetBestUnlockableNavy(city);
            if (bestNavyKind == null) return null; // 还没解锁任何水军编制

            // 5. 选将 (Drafting)
            Person bestLeader = FindBestNavalLeader(city);
            if (bestLeader == null) return null; // 有船无将，不征

            // 6. 执行征兵 (Instantiation)
            return CreateNavalTroop(city, bestLeader, bestNavyKind);
        }

        // --- 辅助方法 ---

        private MilitaryKind GetBestUnlockableNavy(Architecture city)
        {
            // 遍历城市所有可编制的兵种 (Assuming city.Militaries contains unlocked types)
            if (city.Militaries == null) return null;

            var candidates = new List<MilitaryKind>();
            foreach (GameObject obj in city.Militaries.GetList())
            {
                if (obj is Military m && m.Kind != null)
                {
                    candidates.Add(m.Kind);
                }
            }

            return candidates
                .Where(k => k.Type == MilitaryType.水军)
                .OrderByDescending(k => k.MinCommand) // 选战斗力最强的（Using MinCommand as proxy for Tier/Strength）
                .FirstOrDefault();
        }

        private Person FindBestNavalLeader(Architecture city)
        {
            if (city.Persons == null) return null;

            var candidates = new List<Person>();
            foreach (GameObject obj in city.Persons.GetList())
            {
                if (obj is Person p)
                {
                    if (p.Status == PersonStatus.Normal && p.LocationArchitecture == city)
                    {
                        candidates.Add(p);
                    }
                }
            }

            // Get representative kinds for comparison
            var navyKind = MilitaryCapabilityEvaluator.GetNavyMilitaryKind();
            var cavalryKind = GetRepresentativeKind(MilitaryType.骑兵);
            var infantryKind = GetRepresentativeKind(MilitaryType.步兵);

            if (navyKind == null) return null;

            Person best = null;
            float bestScore = -1f;

            foreach (var p in candidates)
            {
                float navyScore = MilitaryCapabilityEvaluator.CalculateCapability(p, navyKind);

                // --- Opportunity Cost Evaluation ---
                float landScore = 0f;
                if (cavalryKind != null)
                    landScore = Math.Max(landScore, MilitaryCapabilityEvaluator.CalculateCapability(p, cavalryKind));
                if (infantryKind != null)
                    landScore = Math.Max(landScore, MilitaryCapabilityEvaluator.CalculateCapability(p, infantryKind));

                // If land capability is significantly higher (1.5x), penalize naval score to reserve for land
                if (landScore > navyScore * 1.5f)
                {
                    navyScore *= 0.5f; 
                }

                if (navyScore > bestScore && navyScore > 80f)
                {
                    bestScore = navyScore;
                    best = p;
                }
            }

            return best;
        }

        private MilitaryKind GetRepresentativeKind(MilitaryType type)
        {
            if (Session.Current == null || Session.Current.Scenario == null) return null;
            var commonData = Session.Current.Scenario.GameCommonData;
            if (commonData != null && commonData.AllMilitaryKinds != null)
            {
                foreach (MilitaryKind k in commonData.AllMilitaryKinds.MilitaryKinds.Values)
                {
                    if (k.Type == type) return k; // Return first match as representative
                }
            }
            return null;
        }

        private Troop CreateNavalTroop(Architecture city, Person leader, MilitaryKind kind)
        {
            Troop t = new Troop();
            t.ID = Session.Current.Scenario.Troops.GetFreeGameObjectID();
            t.Leader = leader;
            t.Init();
            t.Position = city.Position;
            t.BelongedFaction = city.BelongedFaction;
            t.StartingArchitecture = city;  // 🔥 根本修复：设置出发城市
            
            // 🔥 修复：创建正确的 Military 对象并分配
            Military military = new Military();
            military.ID = Session.Current.Scenario.Militaries.GetFreeGameObjectID();
            military.Kind = kind;
            military.Leader = leader;
            military.BelongedArchitecture = city;
            military.Name = kind.Name + "队";
            Session.Current.Scenario.Militaries.AddMilitary(military);
            t.Army = military;

            // --------------------------------------------------------
            // 修正：摒弃 (Leader.Command * 100) 的错误逻辑
            // 读取 CommonData.json 中的 MaxScale
            // --------------------------------------------------------
            
            int maxCapacity = kind.MaxScale; // 例如：楼船可能上限 5000，走舸 2000
            
            // 计算实际能征多少
            int cost = kind.CreateCost > 0 ? kind.CreateCost : 10;
            int availableFund = (int)(city.Fund / Math.Max(1, cost));
            int availablePop = city.Population;
            
            // AI 在组建新部队时，通常希望它是满编的
            int recruitNum = Math.Min(maxCapacity, Math.Min(availableFund, availablePop));

            // 如果凑不够最小编制 (MinScale)，则取消组建
            if (recruitNum < kind.MinScale) return null;

            t.Quantity = recruitNum;
            
            // 扣除资源
            city.Fund -= recruitNum * cost;
            city.Population -= recruitNum;
            t.Food = Math.Min(city.Food, recruitNum * 2);
            city.Food -= t.Food;

            // Add to global troops
            if (Session.Current.Scenario.Troops != null)
            {
                Session.Current.Scenario.Troops.AddTroopWithEvent(t);
            }

            // 自动配置副将 (保持原逻辑)
            AddNavalDeputies(t, city, leader, kind);
            
            return t;
        }

        private void AddNavalDeputies(Troop t, Architecture city, Person leader, MilitaryKind kind)
        {
            // 逻辑同前：找有水战技能且与主将互补的闲置武将
            if (city.Persons == null) return;
            
            List<Person> availablePersons = new List<Person>();
            foreach(GameObject obj in city.Persons.GetList())
            {
                if (obj is Person p && p != leader && p.Status == PersonStatus.Normal && p.LocationArchitecture == city)
                {
                    availablePersons.Add(p);
                }
            }

            foreach (var candidate in availablePersons)
            {
                 if (t.PersonCount >= 3) break; // Max 3 persons

                 // 综合评分
                 float cap = MilitaryCapabilityEvaluator.CalculateCapability(candidate, kind);
                 if (cap < 50f) continue; // Minimum standard

                 bool helps = false;
                 // Check skill complementarity
                 if (candidate.Skills != null)
                 {
                     foreach (GameObject sObj in candidate.Skills.GetSkillList())
                     {
                         if (sObj is Skill sk && (sk.Name.Contains("水") || sk.MilitaryTypeOnly == MilitaryType.水军))
                         {
                             // Check if leader lacks it
                             if (leader.Skills != null && leader.Skills.GetSkill(sk.ID) == null)
                                 helps = true;
                         }
                     }
                 }

                 // Check relation
                 if (helps || IsCloseRelation(leader, candidate))
                 {
                     t.Persons.Add(candidate);
                 }
            }
        }
        
        private bool IsCloseRelation(Person a, Person b)
        {
             if (a == null || b == null) return false;
             // Check brothers
             if (a.Brothers != null)
             {
                 foreach(GameObject obj in a.Brothers.GetList())
                 {
                     if (obj == b || (obj is Person brother && brother == b)) return true;
                 }
             }
             return false;
        }

        private float CalculateNavalNecessity(Architecture city)
        {
            // 简单的地理判断：周围水格子多不多
            int waterTiles = 0;
            int total = 0;
            
            if (Session.Current == null || Session.Current.Scenario == null) return 0f;
            var scenario = Session.Current.Scenario;

            for (int x = city.Position.X - 5; x <= city.Position.X + 5; x++)
            {
                for (int y = city.Position.Y - 5; y <= city.Position.Y + 5; y++)
                {
                    Point pos = new Point(x, y);
                    if (!scenario.PositionOutOfRange(pos))
                    {
                        total++;
                        // 使用 GameScenario.GetTerrainKindByPosition (Verified API)
                        if (scenario.GetTerrainKindByPosition(pos) == TerrainKind.水域)
                        {
                            waterTiles++;
                        }
                    }
                }
            }
            
            if (total == 0) return 0f;
            // Normalize: If 20% tiles are water, high necessity?
            // Return value 0.0 to 1.0+
            // If waterTiles / total > 0.1 (e.g. 10/100 tiles), necessity > 0.3?
            return (float)waterTiles / total * 3.0f; 
        }
    }
}
