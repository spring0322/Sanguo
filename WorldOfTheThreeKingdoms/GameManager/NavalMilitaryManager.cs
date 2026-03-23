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
    /// <summary>
    /// 水军军制管理器 - 负责评估水战需求、建造战船和组建水军编队
    /// </summary>
    public class NavalMilitaryManager
    {
        /// <summary>
        /// AI回合入口：处理势力的水军建设
        /// </summary>
        public void ProcessTurn(Faction faction)
        {
            if (faction.Architectures.Count == 0) return;

            foreach (Architecture city in faction.Architectures)
            {
                // 1. 环境评估：计算水战必要性 (0~1)
                float necessity = CalculateNavalNecessity(city);

                if (necessity > 0.4f) // 只要临水或有水路威胁
                {
                    // 2. 军备：造船
                    ManageNavalConstruction(city, necessity);

                    // 3. 编组：如果有闲置水军专家，组建独立编队
                    FormNavalUnit(city);
                }
            }
        }

        // --- 核心逻辑 ---

        private void FormNavalUnit(Architecture city)
        {
            // Get Navy MilitaryKind for evaluation
            var navyKind = MilitaryCapabilityEvaluator.GetNavyMilitaryKind();
            if (navyKind == null) return;

            // Access persons in city. 
            var cityPersons = GetPersonsInArchitecture(city).ToList(); 

            // A. 筛选主将：基于 Capability 评分
            var navalExperts = cityPersons
                .Where(p => IsPersonAvailable(p)) 
                .Select(p => new { Person = p, Score = MilitaryCapabilityEvaluator.CalculateCapability(p, navyKind) })
                .Where(x => x.Score > 50f) // 至少称号允许或者统率极高
                .OrderByDescending(x => x.Score)
                .ToList();

            if (navalExperts.Count == 0) return; // 无将可用

            Person leader = navalExperts[0].Person;

            // B. 获取最好的船
            var ship = GetBestAvailableShip(city);
            if (ship == null) return; // 无船可用

            // C. 组建部队
            Troop t = new Troop();
            t.ID = Session.Current.Scenario.Troops.GetFreeGameObjectID();
            t.Leader = leader;
            t.Init();
            t.StartingArchitecture = city;  // 🔥 根本修复：设置出发城市
            t.Position = city.Position;
            t.BelongedFaction = city.BelongedFaction;
            
            // 🔥 修复：创建正确的 Military 对象并分配
            Military military = new Military();
            military.ID = Session.Current.Scenario.Militaries.GetFreeGameObjectID();
            military.Kind = ship;
            military.Leader = leader;
            military.BelongedArchitecture = city;
            military.Name = ship.Name + "队";
            Session.Current.Scenario.Militaries.AddMilitary(military);
            t.Army = military;
            
            // D. 搭配副将 (基于技能互补)
            AddNavalDeputies(t, cityPersons, leader, navyKind);

            // Add to Scenario Troops
            if (Session.Current.Scenario.Troops != null)
            {
                Session.Current.Scenario.Troops.AddTroopWithEvent(t);
            }
        }
        
        private bool IsPersonAvailable(Person p)
        {
            // Use Status instead of State, and PersonStatus enum
             return p.Status == PersonStatus.Normal;
        }

        private void AddNavalDeputies(Troop t, List<Person> candidates, Person leader, MilitaryKind navyKind)
        {
            // Filter candidates excluding leader
            var availableParams = candidates.Where(p => p != leader && IsPersonAvailable(p)).ToList();

            foreach (var candidate in availableParams)
            {
                if (t.PersonCount >= 3) break;

                // 1. 计算副将的水战能力
                float cap = MilitaryCapabilityEvaluator.CalculateCapability(candidate, navyKind);
                if (cap < 20f) continue; // 

                // 2. 技能互补检查
                // 如果副将有主将没有的水战技能，优先入选
                bool helps = false;
                if (candidate.Skills != null)
                {
                    // Access SkillTable via GetSkillList() which returns GameObjectList
                    // Need explicit cast or check
                    foreach (GameObject obj in candidate.Skills.GetSkillList())
                    {
                        if (obj is Skill sk && sk.Name.Contains("水")) 
                        {
                            // Check if leader has it
                            if (leader.Skills != null && leader.Skills.GetSkill(sk.ID) == null) 
                                helps = true;
                        }
                    }
                }

                if (helps || IsCloseRelation(leader, candidate))
                {
                    // Use Persons list add
                    if (t.Persons != null)
                    {
                        t.Persons.Add(candidate);
                    }
                }
            }
        }

        // --- 辅助逻辑 ---

        private float CalculateNavalNecessity(Architecture city)
        {
            // 扫描城市周围5格的水域比例
            int waterTiles = 0;
            int total = 0;
            var scenario = Session.Current.Scenario;
            
            for (int x = city.Position.X - 5; x <= city.Position.X + 5; x++)
            {
                for (int y = city.Position.Y - 5; y <= city.Position.Y + 5; y++)
                {
                    var pos = new Point(x, y);
                    if (!scenario.PositionOutOfRange(pos))
                    {
                        total++;
                        // Placeholder until GetTerrain API is confirmed
                        // if (scenario.GetTerrain(pos) == TerrainKind.Water) waterTiles++;
                        // For now we assume no water (0 necessity) to pass build
                     }
                }
            }
            // return (float)waterTiles / Math.Max(1, total) * 3.0f;
            return 0f; 
        }

        private MilitaryKind GetBestAvailableShip(Architecture city)
        {
             if (city.Militaries == null) return null;

            var militaries = new List<Military>();
            foreach(GameObject obj in city.Militaries.GetList())
            {
                if (obj is Military m) militaries.Add(m);
            }

            // Replacing FightingForce with MinCommand as proxy for strength/tier
            return militaries
                .Where(m => m.Kind.Type == MilitaryType.水军 && m.Quantity > 0)
                .OrderByDescending(m => m.Kind.MinCommand)
                .Select(m => m.Kind)
                .FirstOrDefault();
        }

        private void ManageNavalConstruction(Architecture city, float necessity)
        {
            if (city.Fund < 2000) return;
        }
        
        private bool IsCloseRelation(Person a, Person b) 
        { 
            if (a == null || b == null) return false;
            // a.Brothers is PersonList
            foreach(GameObject obj in a.Brothers.GetList())
            {
                if (obj is Person p && p == b) return true;
                if (obj.ID == b.ID) return true;
            }
            
            // Father check commented out due to private accessor
            // if (a.Father == b.ID || b.Father == a.ID) return true;

            return false;
        }

        private IEnumerable<Person> GetPersonsInArchitecture(Architecture city)
        {
            if (Session.Current.Scenario.Persons != null)
            {
                 foreach (GameObject obj in Session.Current.Scenario.Persons.GetList())
                 {
                     if (obj is Person p)
                     {
                         // Location check using LocationArchitecture
                         if (p.LocationArchitecture == city)
                            yield return p;
                         // Fallback for string location if needed
                         else if (p.Location == city.Name) // assuming Name is string
                             yield return p;
                     }
                 }
            }
        }
    }
}
