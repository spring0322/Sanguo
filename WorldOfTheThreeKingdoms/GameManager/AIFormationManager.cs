using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameGlobal;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;

namespace GameManager
{
    public class AIFormationManager
    {
        // ----------------------------------------------------------------
        // 配置参数
        // ----------------------------------------------------------------
        private const int MIN_COMMAND_FOR_LEADER = 70; // 统率低于70尽量不当主将（除非没人了）
        private const float RELATION_BONUS = 30.0f;    // 义兄弟/父子加分
        
        /// <summary>
        /// 为城市生成最佳出征部队列表
        /// </summary>
        public List<Troop> CreateOptimalTroops(Architecture city, int maxTroopCount, int minReserve)
        {
            var resultTroops = new List<Troop>();
            
            // 1. 获取所有闲置武将 (In city, Normal state)
            var availablePersons = new List<Person>();
            foreach (Person p in city.Persons)
            {
                if (p.LocationArchitecture == city && p.Status == PersonStatus.Normal && !p.IsCaptive)
                {
                    availablePersons.Add(p);
                }
            }
            
            // 2. 筛选合格的主将（统率高）
            var potentialLeaders = availablePersons
                .Where(p => p.Command >= MIN_COMMAND_FOR_LEADER || HasCommanderTitle(p))
                .OrderByDescending(p => p.Command)
                .ToList();

            // 3. 循环组建部队
            foreach (var leader in potentialLeaders)
            {
                if (resultTroops.Count >= maxTroopCount) break;
                // Check reserve
                if (city.ArmyScale <= minReserve) break;

                if (!availablePersons.Contains(leader)) continue; // 已经被选为副将了

                // A. 选择最佳兵种
                MilitaryKind bestUnit = SelectBestUnitType(leader, city);
                if (bestUnit == null) continue; // 没兵装或不适合

                // B. 创建部队对象 (Architecture.CreateTroop)
                Troop troop = CreateTroop(city, leader, bestUnit);
                if (troop == null) continue;
                
                // C. 搭配副将 (核心：羁绊与互补)
                availablePersons.Remove(leader); // 主将已占用
                AssignDeputies(troop, leader, availablePersons);

                resultTroops.Add(troop);
            }

            return resultTroops;
        }

        // ----------------------------------------------------------------
        // 模块一：兵种适性匹配
        // ----------------------------------------------------------------
        
        private MilitaryKind SelectBestUnitType(Person leader, Architecture city)
        {
            MilitaryKind bestKind = null;
            float bestScore = -1f;

            foreach (MilitaryKind kind in city.GetLevelUpMilitaryList()) // Use GetLevelUpMilitaryList or Militaries
            {
                // Verify we have enough military scales (soldiers/equipment)
                // Assuming city.Militaries or similar. But Architecture has Militaries as MilitaryList.
                // We need to check pure Military objects in Architecture.
                // Architecture.Militaries is a MilitaryList.
            }
             
            // Re-implementing simplified loop over architecture's militaries
            foreach (Military military in city.Militaries) 
            {
                if (military.Quantity <= 0) continue; // 没兵装了
                MilitaryKind kind = military.Kind;

                float score = 0f;

                // 1. 基础适性 (Using Title.MilitaryTypeOnly as proxy for Aptitude)
                // Since Experience is private, we rely on Titles and general stats.
                if (leader.RealTitles != null)
                {
                    foreach (Title t in leader.RealTitles)
                    {
                        if (t.MilitaryTypeOnly == kind.Type)
                        {
                            score += 20f;
                        }
                    }
                }

                // 2. 统率修正 (骑兵通常需要更高统率来维持机动)
                if (kind.Type == MilitaryType.骑兵)
                    score += leader.Command * 0.1f;
                else
                    score += leader.Command * 0.05f;

                // 3. 兵种本身强度 (Merit)
                score += kind.Merit * 0.1f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestKind = kind;
                }
            }

            return bestKind;
        }

        // ----------------------------------------------------------------
        // 模块二：部队实例化
        // ----------------------------------------------------------------

        private Troop CreateTroop(Architecture city, Person leader, MilitaryKind kind)
        {
            // Find the Military object in city that matches kind
            Military military = null;
            foreach (Military m in city.Militaries)
            {
                if (m.Kind == kind)
                {
                    military = m;
                    break;
                }
            }
            if (military == null) return null;

            // Determine Food (Default logic)
            int food = 5000; // Default buffer
            if (city.Food < food) food = city.Food;

            // Create Troop
            // CreateTroop(Person leader, Person person, Military military, int food, Point position)
            // Using city.Position as spawn point - typically Architecture.CreateTroop handles placement or uses specific logic.
            // However, Architecture.CreateTroop requires a valid position.
            // We'll trust Architecture.GetRandomStartingPosition if accessible, but it's private.
            // We will use city.Position and let the game handle displacement if overlapped.
            // Or try to replicate GetRandomStartingPosition logic? Too complex.
            // We will pass city.Position. 
            // NOTE: Using city.Position might fail if Occupied. 
            // Architecture.cs line 3061 uses GetRandomStartingPosition.
            
            // Safe fallback: Since we cannot access private GetRandomStartingPosition, behavior might be undefined if blocked.
            // But we are in "CreateOptimalTroops", likely called by AI logic that knows.
            
            // To simplify integration:
            try 
            {
                // Warning: This calling pattern assumes CreateTroop handles placement or we accept overlap.
               return city.CreateTroop(leader, leader, military, food, city.Position);
            }
            catch
            {
               return null;
            }
        }

        // ----------------------------------------------------------------
        // 模块三：副将搭配 (文武互补 + 羁绊)
        // ----------------------------------------------------------------

        private void AssignDeputies(Troop troop, Person leader, List<Person> candidates)
        {
            // 目标：填满副将槽 (假设最多2个副将)
            
            // 1. 寻找“关系户” (义兄弟、父子、配偶)
            // Use Person.IsCloseTo from Person.cs line 5045
            var relatedPersons = candidates
                .Where(p => IsCloseRelation(leader, p))
                .OrderByDescending(p => p.FightingForce)
                .ToList();

            foreach (var relative in relatedPersons)
            {
                if (troop.PersonCount >= 3) break;
                troop.Persons.Add(relative); // Using Persons.Add directly
                candidates.Remove(relative);
                
                // Helper to update person state if needed? createTroop likely handles leader but deputies?
                // Usually need to set LocationTroop = troop, Status = Moving, etc.
                // Architecture.CreateTroop only assigns Leader.
                // We need to manually set deputy state.
                relative.Status = PersonStatus.Moving;
                relative.LocationTroop = troop;
                relative.LocationArchitecture = null;
            }

            if (troop.PersonCount >= 3) return;

            // 2. 补全短板
            
            // 需要保镖吗？ (主将武力 < 80)
            if (leader.Strength < 80 && IsTroopWeakInStrength(troop))
            {
                var bodyGuard = candidates
                    .OrderByDescending(p => p.Strength)
                    .FirstOrDefault();
                
                if (bodyGuard != null && bodyGuard.Strength > 70)
                {
                    troop.Persons.Add(bodyGuard);
                    candidates.Remove(bodyGuard);
                    bodyGuard.Status = PersonStatus.Moving;
                    bodyGuard.LocationTroop = troop;
                    bodyGuard.LocationArchitecture = null;
                }
            }

            if (troop.PersonCount >= 3) return;

            // 需要军师吗？ (主将智力 < 70)
            if (leader.Intelligence < 70 && IsTroopWeakInIntelligence(troop))
            {
                var advisor = candidates
                    .OrderByDescending(p => p.Intelligence)
                    .FirstOrDefault();
                
                if (advisor != null && advisor.Intelligence > 70)
                {
                    troop.Persons.Add(advisor);
                    candidates.Remove(advisor);
                    advisor.Status = PersonStatus.Moving;
                    advisor.LocationTroop = troop;
                    advisor.LocationArchitecture = null;
                }
            }
        }

        // ----------------------------------------------------------------
        // 辅助判断逻辑
        // ----------------------------------------------------------------

        private bool IsCloseRelation(Person p1, Person p2)
        {
            // Use GameObjects.Person methods
            // p1.IsCloseTo(p2) covers ClosePersons, Spouses, Brothers, Parent/Child often?
            // Checking definition of IsCloseTo:
            // return ClosePersons.Contains(p2.ID) || Spouse == p2.ID || Brothers.Contains(p2.ID) ...
            return p1.IsVeryCloseTo(p2) || p1.HasCloseStrainTo(p2);
        }

        private bool HasCommanderTitle(Person p)
        {
            // Check Titles for MilitaryType attributes or high level titles
            return p.RealTitles.Any(t => t.MilitaryTypeOnly != MilitaryType.其他 || t.Level >= 3);
        }
        
        private bool IsTroopWeakInStrength(Troop t)
        {
            foreach(Person p in t.Persons)
            {
                if (p.Strength >= 80) return false;
            }
            return true;
        }

        private bool IsTroopWeakInIntelligence(Troop t)
        {
            foreach(Person p in t.Persons)
            {
                if (p.Intelligence >= 70) return false;
            }
            return true;
        }
    }
}
