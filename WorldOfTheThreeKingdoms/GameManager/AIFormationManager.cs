using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;
using Microsoft.Xna.Framework;

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
        /// 为城市生成最佳出征部队列表（含：君主后置、配额限制、分身过滤）
        /// </summary>
        public List<Troop> CreateOptimalTroops(Architecture city, int maxTroopCount, int minReserve, Legion assignedLegion = null)
        {
            var resultTroops = new List<Troop>();
            
            // 🔥 诊断：检查城市人员状态
            System.Diagnostics.Debug.WriteLine($"[CreateOptimalTroops] ===== 开始为 {city.Name} 创建部队 =====");
            System.Diagnostics.Debug.WriteLine($"[CreateOptimalTroops] city.Persons.Count: {city.Persons?.Count ?? 0}");
            
            // 0. 强制释放精英闲人
            FreeUpPersonnel(city);

            // 1. 获取所有闲置武将 - 直接从 city.Persons 获取，不依赖缓存
            int initialFactionTroopCount = (city.BelongedFaction != null) ? city.BelongedFaction.Troops.Count : 0;
            var availablePersons = new List<Person>();
            
            if (city.Persons == null || city.Persons.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateOptimalTroops] ⚠️ {city.Name} 没有任何人员！");
                return resultTroops;
            }
            
            foreach (Person p in city.Persons)
            {
                // 🔥 详细日志：每个人员的状态
                System.Diagnostics.Debug.WriteLine($"[CreateOptimalTroops]   检查 {p.Name}: Status={p.Status}, LocationArch={p.LocationArchitecture?.Name}, LocationTroop={p.LocationTroop?.DisplayName}, IsCaptive={p.IsCaptive}, NvGuan={p.NvGuan}");
                
                if (p.LocationArchitecture == city && p.Status == PersonStatus.Normal && !p.IsCaptive && !p.NvGuan)
                {
                    // 确保未出征
                    if (p.LocationTroop != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateOptimalTroops]     ❌ {p.Name} 已在部队中: {p.LocationTroop.DisplayName}");
                        continue;
                    }
                    availablePersons.Add(p);
                    System.Diagnostics.Debug.WriteLine($"[CreateOptimalTroops]     ✅ {p.Name} 可用");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[CreateOptimalTroops] {city.Name} 可用武将总数: {availablePersons.Count}");
            
            // [New] 水军判定 (保持不变)
            if (city.AIWaterLinks.Count > 0)
            {
                Troop navyTroop = TryRecruitNavy(city, availablePersons, maxTroopCount, initialFactionTroopCount + resultTroops.Count, assignedLegion);
               if (navyTroop != null) resultTroops.Add(navyTroop);
            }

            // 2. 【评分与排名阶段】 (Ranking Phase)
            // 筛选出所有至少有一个可用兵种的人，并计算最高分
            var rankedCandidates = availablePersons
                .Select(p => 
                {
                    MilitaryKind bestUnit = SelectBestUnitType(p, city);
                    if (bestUnit == null) return null;
                    
                    // 预判：静态能力检查（兵力、士气、局部粮食等）
                    // 我们需要这里的 Military 对象来做预判
                    Military militaryCandidate = city.Militaries.GameObjects.OfType<Military>().OrderByDescending(m => m.Kind == bestUnit ? m.Quantity : 0).FirstOrDefault();
                    if (militaryCandidate == null || militaryCandidate.Quantity <= 0) return null;

                    if (!Troop.CheckOffensiveCapabilityStatic(p, militaryCandidate, city.Food, city)) return null;

                    return new 
                    { 
                        Person = p, 
                        BestUnit = bestUnit,
                        Military = militaryCandidate,
                        Score = Troop.EstimateCombatScore(p, bestUnit) 
                    };
                })
                .Where(x => x != null && x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            // --- 🔥 君主后置逻辑 ---
            if (city.BelongedFaction != null)
            {
                Person monarch = city.BelongedFaction.Leader;
                var monarchEntry = rankedCandidates.FirstOrDefault(x => x.Person == monarch);
                if (monarchEntry != null)
                {
                    // 如果有比君主分更高的人，君主移到候补
                    bool hasBetterCandidate = rankedCandidates.Any(x => x.Person != monarch && x.Score > monarchEntry.Score);
                    if (hasBetterCandidate)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AI] 此时有比君主({monarch.Name})综合得分更高的大将，君主退至候补席。");
                        rankedCandidates.Remove(monarchEntry);
                        rankedCandidates.Add(monarchEntry);
                    }
                }
            }

            // 3. 【择优录取与创建阶段】 (Selection & Execution Phase)
            foreach (var selection in rankedCandidates)
            {
                if (resultTroops.Count >= maxTroopCount) break;
                if (city.ArmyScale <= minReserve) break;

                // 再次确认武将还是可用的（可能在之前的循环中被选为副将）
                if (!availablePersons.Contains(selection.Person)) continue;

                System.Diagnostics.Debug.WriteLine($"[AI] 选定主将: {selection.Person.Name} (分值:{selection.Score:F1}) 兵种:{selection.BestUnit.Name}");

                Troop troop = CreateTroop(city, selection.Person, selection.BestUnit, assignedLegion);
                if (troop == null) continue;

                availablePersons.Remove(selection.Person);

                // 计算配额（为后续主将预留人员）
                int troopsRemainingToBuild = maxTroopCount - resultTroops.Count - 1;
                if (troopsRemainingToBuild < 0) troopsRemainingToBuild = 0;

                int reservedForFutureLeaders = Math.Min(troopsRemainingToBuild, rankedCandidates.Count(x => availablePersons.Contains(x.Person)));
                int maxDeputiesAllowed = availablePersons.Count - reservedForFutureLeaders;

                if (maxDeputiesAllowed > 0)
                {
                    AssignDeputies(troop, selection.Person, availablePersons, maxDeputiesAllowed, troopsRemainingToBuild);
                }

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

            foreach (Military military in city.Militaries) 
            {
                if (military.Quantity <= 0) continue; // 没兵装了
                MilitaryKind kind = military.Kind;

                float score = 0f;

                // 1. 基础适性
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

                // 2. 统率修正
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
        
        // [New] 完整能力检查逻辑 (Source: User Request)
        private bool CheckOffensiveCapability(Troop troop)
        {
            float capability = GetLeaderCapabilityFactor(troop); 
            float leaderBaseRate = 0.4f + ((1.0f - capability) * 0.3f);
            
            int maxScale = 10000;
            if (troop.Army != null && troop.Army.Kind != null) 
                maxScale = troop.Army.Kind.MaxScale > 0 ? troop.Army.Kind.MaxScale : 10000;

            float scaleCorrection = (float)Math.Sqrt((float)maxScale / 100000.0f);
            if (scaleCorrection > 1.0f) scaleCorrection = 1.0f;
            
            int finalThreshold = (int)(maxScale * leaderBaseRate * scaleCorrection);
            int hardFloor = Math.Min(500, maxScale);
            if (finalThreshold < hardFloor) finalThreshold = hardFloor;

            if (troop.Army == null || troop.Army.Quantity < finalThreshold) return false;
            
            int dynamicMorale = 65 + (int)((1.0f - capability) * 25);
            if (troop.Morale < dynamicMorale) return false;

            int conservativeFoodCostPerDay = Troop.GetConservativePlanningFoodCostPerDay(troop.Army);
            
            // Food Check (Troop level)
            // assuming troop.FoodMax is correctly set or derived from Army
            if (troop.Food < troop.FoodMax) return false;
            
            // Architecture Food Check
            if (troop.BelongedLegion != null && troop.StartingArchitecture != null)
            {
                 if (troop.StartingArchitecture.Food < conservativeFoodCostPerDay * 30) return false;
            }
            // Note: If BelongedLegion is null (during creation), we might strip that check or check StartingArchitecture directly?
            // User code says: if (this.BelongedLegion != null && this.StartingArchitecture != null)
            // During creation, BelongedLegion is likely null. 
            // We should arguably check StartingArchitecture.Food anyway if we want to be strict,
            // but following user code strictly means skipping it if Legion is null.
            // However, AIFormationManager creates troops WITHOUT Legion initially.
            // So this check would be skipped. 
            // To be safe and effective, we likely want:
            if (troop.StartingArchitecture != null)
            {
                if (troop.StartingArchitecture.Food < conservativeFoodCostPerDay * 30) return false;
            }

            return true;
        }

        private bool CheckDefensiveCapability(Troop troop)
        {
            if (troop.Army == null || troop.Army.Kind == null) return false;
            int maxScale = (troop.Army.Kind.MaxScale > 0 ? troop.Army.Kind.MaxScale : 10000);
            if (troop.Army.Quantity < maxScale * 0.2f) return false;
            if (troop.Food < Troop.GetConservativePlanningFoodCostPerDay(troop.Army) * 3) return false;
            return true;
        }

        private float GetLeaderCapabilityFactor(Troop troop)
        {
            if (troop.Leader == null) return 0.5f;
            const float REF_MAX = 120.0f;
            float score = (troop.Leader.Command * 2.0f) + (troop.Leader.Strength * 2.0f) + (troop.Leader.Intelligence * 2.0f) + (troop.Leader.Glamour * 0.5f) + (troop.Leader.Politics * 0.5f);
            return Math.Min(Math.Max(score / (7.0f * REF_MAX), 0f), 1.0f);
        }

        private Troop CreateTroop(Architecture city, Person leader, MilitaryKind kind, Legion assignedLegion = null)
        {
            // ============================================================================
            // 🔥 修复：查找该兵种类型中兵力最多的编队，避免选择兵力为0的编队
            // 日期：2026-01-21
            // 问题：一个城市可能有多个相同兵种类型的编队，其中一些兵力为0
            // ============================================================================
            Military military = null;
            int maxQuantity = 0;
            
            foreach (Military m in city.Militaries)
            {
                if (m.Kind == kind && m.Quantity > maxQuantity)
                {
                    military = m;
                    maxQuantity = m.Quantity;
                }
            }
            
            if (military == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateTroop] {city.Name} 没有找到兵种 {kind.Name}");
                return null;
            }

            // 🔥 检查兵力是否为0（理论上不会，因为已经选择了兵力最多的）
            if (military.Quantity <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateTroop] {city.Name} 的 {military.Name} 兵力为0，无法创建部队");
                System.Diagnostics.Debug.WriteLine($"[CreateTroop]   城市总兵力: {city.ArmyQuantity}, 该编队兵力: {military.Quantity}");
                System.Diagnostics.Debug.WriteLine($"[CreateTroop]   城市编队数: {city.Militaries.Count}");
                
                // 列出所有编队的兵力
                foreach (Military m in city.Militaries)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateTroop]     {m.Name}: Quantity={m.Quantity}, Scales={m.Scales:F1}");
                }
                
                return null;
            }

            int food = -1; // -1 表示由 CreateTroop 内部自动分配粮草

            try 
            {
               // Note: This relies on Architecture.CreateTroop being public or internal accessible
               // and logic to find a valid position near city.Position
               Point? spawnPoint = city.GetRandomStartingPosition(military);
               if (!spawnPoint.HasValue)
               {
                   System.Diagnostics.Debug.WriteLine($"[CreateTroop] ❌ {city.Name} 无法找到有效的出生点");
                   return null;
               }

               // 🔥 AOT修复：显式创建GameObjectList，避免使用集合初始化器
               GameObjectList personsList = new GameObjectList();
               personsList.Add(leader);
               
               System.Diagnostics.Debug.WriteLine($"[CreateTroop] 准备创建部队: Leader={leader.Name}, Military={military.Name}, Food={food}, Position={spawnPoint.Value}");
               System.Diagnostics.Debug.WriteLine($"[CreateTroop] personsList.Count={personsList.Count}, personsList[0]={(personsList.Count > 0 ? (personsList.GetList()[0] as Person)?.Name : "null")}");
               
               Troop result = city.CreateTroop(personsList, leader, military, food, spawnPoint.Value, assignedLegion, silent: true);
               
               if (result == null)
               {
                   System.Diagnostics.Debug.WriteLine($"[CreateTroop] ❌ city.CreateTroop 返回 null");
               }
               else
               {
                   System.Diagnostics.Debug.WriteLine($"[CreateTroop] ✅ 成功创建部队: {result.DisplayName}, PersonCount={result.PersonCount}");
               }
               
               return result;
            }
            catch (Exception ex)
            {
               System.Diagnostics.Debug.WriteLine($"[CreateTroop] ❌ 异常: {ex.Message}");
               System.Diagnostics.Debug.WriteLine($"[CreateTroop] StackTrace: {ex.StackTrace}");
               return null;
            }
        }

        private void FreeUpPersonnel(Architecture city)
        {
            // 获取所有正在干活，但没出征的武将
            var workingPersons = new List<Person>();
            foreach(Person p in city.Persons)
            {
                if (p.WorkKind != ArchitectureWorkKind.无 && p.LocationArchitecture == city && p.Status == PersonStatus.Normal)
                {
                    workingPersons.Add(p);
                }
            }
            
            foreach (var p in workingPersons)
            {
                // 判定：如果城里闲人太少（<3个），或者这个人是猛将（统武>80），强制让他别干活了，准备打仗
                if (city.PersonCount < 10 || (p.Strength > 80 || p.Command > 80))
                {
                    p.WorkKind = ArchitectureWorkKind.无; // 强制停工
                }
            }
        }

        // ----------------------------------------------------------------
        // 模块三：副将搭配 (文武互补 + 羁绊 + 强制填坑)
        // ----------------------------------------------------------------

        /// <summary>
        /// 副将搭配 (含：君主保镖逻辑、硬性门槛、动态保留)
        /// </summary>
        private void AssignDeputies(Troop troop, Person leader, List<Person> candidates, int maxAllowed, int troopsRemainingToBuild)
        {
            System.Diagnostics.Debug.WriteLine($"[AssignDeputies] 为{troop.DisplayName}分配副将");
            
            int assignedCount = 0;
            
            // 判断主将是否为君主
            bool isMonarchLeading = (leader.BelongedFaction != null && leader == leader.BelongedFaction.Leader);

            foreach (var candidate in candidates.ToList()) 
            {
                if (troop.PersonCount >= 3) break;
                if (assignedCount >= maxAllowed) break;

                // 君主绝不当副将
                if (candidate.BelongedFaction != null && candidate == candidate.BelongedFaction.Leader) continue;

                // --- 🔥 核心修改：君主保镖优先逻辑 ---
                bool isRoyalGuard = false;
                if (isMonarchLeading)
                {
                    // 如果是君主带队，优先寻找高武(保镖)或高智(军师)
                    // 定义：武力 > 90 或 智力 > 90
                    if (candidate.Strength >= 90 || candidate.Intelligence >= 90)
                    {
                        isRoyalGuard = true; 
                        // 注意：这里我们让 isRoyalGuard = true，
                        // 后续逻辑会利用这个标志位直接跳过各种“保留”检查，直接录取。
                    }
                }
                // --- End ---

                // --- 过滤逻辑 (普通人要检查，御林军直接放行) ---
                if (!isRoyalGuard)
                {
                    // 1. 硬性门槛：统率 > 85
                    if (candidate.Command > 85 && !isMonarchLeading) continue; 

                    // 2. 动态门槛：统率 > 75
                    if (candidate.Command > 75 && !isMonarchLeading && troopsRemainingToBuild > 0)
                    {
                        int betterCandidatesCount = candidates.Count(p => p.Command > candidate.Command && p != candidate);
                        if (betterCandidatesCount < troopsRemainingToBuild)
                        {
                            // 还有主将坑位留给我，保留实力
                            continue;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AssignDeputies] 发现顶级保镖 {candidate.Name}，准备加入御林军");
                }

                // --- 选择判定 ---
                
                bool isSelected = false;

                // 0. 御林军特权 (保镖直接通过)
                if (isRoyalGuard)
                {
                    isSelected = true;
                }

                // 1. 阶段一：羁绊/互补
                if (!isSelected)
                {
                    if (IsCloseRelation(leader, candidate) || HasComplementarySkill(leader, candidate))
                        isSelected = true;
                }

                // 2. 阶段二：数值互补
                if (!isSelected && troop.PersonCount < 3)
                {
                    if (ShouldSupport(leader, candidate))
                        isSelected = true;
                }

                // 3. 阶段三：填坑
                if (!isSelected && troop.PersonCount < 3)
                {
                    float capability = MilitaryCapabilityEvaluator.CalculateCapability(candidate, troop.Army.Kind);
                    if (capability > 10f) isSelected = true;
                }

                // --- 执行添加 ---
                if (isSelected)
                {
                    troop.AddPerson(candidate);
                    candidates.Remove(candidate);
                    SetPersonStateForTroop(candidate, troop);
                    assignedCount++;
                    
                    System.Diagnostics.Debug.WriteLine($"[AssignDeputies] 已添加副将 {candidate.Name}");
                }
            }
            
            troop.RefreshAfterOrganize();
        }

        private void SetPersonStateForTroop(Person p, Troop t)
        {
            // Essential state updates usually handled by PostCreateTroop in Architecture,
            // but since we are manually adding deputies to a simulated/pre-spawned troop list,
            // or if the troop is already created, we might need to update status.
            // If this is for 'CreateOptimalTroops' which calls 'CreateTroop' (actual creation),
            // then we need to set status.
            p.Status = PersonStatus.Moving;
            p.LocationArchitecture = null;
            p.LocationTroop = t;
            p.WorkKind = ArchitectureWorkKind.无;
        }

        // ----------------------------------------------------------------
        // 辅助判断逻辑
        // ----------------------------------------------------------------

        private bool IsCloseRelation(Person p1, Person p2)
        {
            return p1.IsVeryCloseTo(p2) || p1.HasCloseStrainTo(p2);
        }

        private bool HasComplementarySkill(Person leader, Person candidate)
        {
             // 简单判定：如果有互补的特技（例如 leader 没有火神，candidate 有）
             // 暂时简化：如果 candidate 也是高统率或高智力且特技不同，算互补
             if (leader.Skills.Count == 0 && candidate.Skills.Count > 0) return true;
             return false;
        }

        private bool ShouldSupport(Person leader, Person candidate)
        {
            // 如果主将是文官(武力<60)，且候选人是武将(武力>70) -> 要！
            if (leader.Strength < 60 && candidate.Strength > 70) return true;

            // 如果主将是莽夫(智力<60)，且候选人是谋士(智力>70) -> 要！
            if (leader.Intelligence < 60 && candidate.Intelligence > 70) return true;

            return false;
        }

        private bool HasCommanderTitle(Person p)
        {
            if (p.RealTitles == null) return false;
            foreach (Title t in p.RealTitles)
            {
                if ((t.MilitaryTypeOnly != MilitaryType.其他) || t.Level >= 3) return true;
            }
            return false;
        }

        private Troop TryRecruitNavy(Architecture city, List<Person> availablePersons, int maxTroopCount, int currentFactionTroopCount, Legion assignedLegion = null)
        {
            // 1. 检查是否有水军编制
            Military navyMilitary = null;
            foreach (Military m in city.Militaries)
            {
                if (m.Kind.Type == MilitaryType.水军 && m.Quantity > 0)
                {
                    navyMilitary = m;
                    break;
                }
            }
            if (navyMilitary == null) return null;

            // 🌟 [User Request] 限制：前三队禁止创建最大规模 > 10000 的编队 (Sync with SelectBestUnitType)
            if (currentFactionTroopCount < 3 && navyMilitary.Kind.MaxScale > 10000)
            {
                return null;
            }

            // 2. 寻找最佳水军将领
            Person bestLeader = null;
            float bestScore = -1;

            foreach (Person p in availablePersons)
            {
                // 简单的评分逻辑：水军适性 + 统率
                // 注意：这里需要根据 Person 的具体属性来判断水军适性
                // 暂时假设 ShuijunExperience 代表经验，或者通过 Title 判断
                
                float score = p.Command;
                
                // 额外加分项
                // if (p.ShuijunExperience > 500) score += 20; 
                // 由于不知道确切的适性属性，暂时只用统率和特技判断
                
                if (p.RealTitles != null)
                {
                     foreach (Title t in p.RealTitles)
                     {
                         if (t.MilitaryTypeOnly == MilitaryType.水军)
                         {
                             score += 50; // 水军称号大幅加分
                         }
                     }
                }

                if (score > bestScore && score > 60) // 至少要有一定能力
                {
                    bestScore = score;
                    bestLeader = p;
                }
            }

            if (bestLeader == null) return null;

            // 3. 创建部队
            Troop troop = CreateTroop(city, bestLeader, navyMilitary.Kind, assignedLegion);
            if (troop != null)
            {
                availablePersons.Remove(bestLeader);
                AssignDeputies(troop, bestLeader, availablePersons, 2, maxTroopCount - 1);
                return troop;
            }

            return null;
        }
    }
}
