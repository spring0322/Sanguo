/// <summary>
/// AI人员管理系统  - 数据清洗与5人编制法
/// 核心改进：
/// 1. 数据清洗：只统计有效武将（排除女官/俘虏/未出仕）
/// 2. 5人编制法：每5个有效人设立1个核心城市
/// 3. 防止原地调动：确保调动的人不在目标城市
/// 4. 理想配额计算：核心城5人，副城3人，溢出回填
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;

namespace GameObjects
{
    public partial class Faction
    {
        #region 战略姿态状态（V8.1兼容）

        /// <summary>
        /// 当前战略姿态 - 由AIStrategicManager推送更新
        /// </summary>
        public WorldOfTheThreeKingdoms.GameManager.StrategicStanceLocal CurrentStrategicStance { get; set; }
            = WorldOfTheThreeKingdoms.GameManager.StrategicStanceLocal.Consolidation;

        /// // ========================================================================
        // 🚑 V9.0 救砖版 (Failsafe)
        // 特性：不依赖 LINQ 遍历，手动类型转换，解决 object 报错
        // ========================================================================

        private void RunPersonnel_V81(List<Architecture> archs)
        {
            if (archs == null || archs.Count <= 1) return;

            // Debug 标记
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[V9.0 SAFE] 势力: {this.Name} 开始执行人员调配...");
#endif

            // ---------------------------------------------------------
            // 步骤 1: 手动提取所有“有效人员” (解决 CS1061/CS1503)
            // ---------------------------------------------------------
            // 我们不直接操作 arch.Persons，而是先把它里面的“人”抓出来放到安全的 List<Person> 里

            // 战术可用人员 (闲人)
            List<Person> availablePersons = new List<Person>();

            // 战略总人员 (含出征)
            int strategicTotal = 0;

            // 1.1 统计战术闲人
            foreach (Architecture arch in archs)
            {
                if (arch.Persons == null) continue;

                // 🛑 核心修复：使用 object 遍历，然后强转
                foreach (object obj in arch.Persons)
                {
                    Person p = (obj is Person ? (Person)obj : null); // 强制转换
                    if (p == null) continue;  // 转换失败就跳过

                    // 过滤条件
                    if (!p.IsCaptive && p.BelongedCaptive == null && p.Status == PersonStatus.Normal && !p.NvGuan)
                    {
                        availablePersons.Add(p);
                    }
                }
            }

            // 1.2 统计战略总数
            if (this.Persons != null)
            {
                foreach (object obj in this.Persons)
                {
                    Person p = (obj is Person ? (Person)obj : null);
                    if (p == null) continue;

                    if (!p.IsCaptive && p.BelongedCaptive == null && p.Alive && !p.NvGuan)
                    {
                        strategicTotal++;
                    }
                }
            }

            // ---------------------------------------------------------
            // 步骤 2: 计算核心与配额
            // ---------------------------------------------------------
            if (strategicTotal < 8) return;

            int targetCores = (strategicTotal + 2) / 5;
            int mapLimit = (archs.Count + 1) / 2;
            targetCores = Math.Min(targetCores, mapLimit);
            if (targetCores < 1) targetCores = 1;
            if (targetCores > archs.Count) targetCores = archs.Count;

            // ---------------------------------------------------------
            // 步骤 3: 选拔核心 (手动评分排序)
            // ---------------------------------------------------------
            Dictionary<Architecture, float> scores = new Dictionary<Architecture, float>();
            foreach (Architecture a in archs)
            {
                scores[a] = CalculatePersonnelDemand(a);
            }

            List<Architecture> eliteCities = new List<Architecture>();
            if (this.Capital != null && archs.Contains(this.Capital))
            {
                eliteCities.Add(this.Capital);
            }

            // 筛选候选城市 (手动实现 Where/OrderBy)
            var potentialElites = new List<Architecture>();
            foreach (Architecture a in archs)
            {
                if (eliteCities.Contains(a)) continue;

                // 🛑 严防死守：基于功能判断，而非 ID
                // 只要有 农业 或 商业 属性，就是城市；关隘通常这两个都是 False
                if (a.Kind != null && (a.Kind.HasAgriculture || a.Kind.HasCommerce))
                {
                    // 使用统一的评分体系
                    float score = CalculatePersonnelDemand(a);

                    if (a.Population >= 3000 || score > 80f)
                    {
                        potentialElites.Add(a);
                    }
                }
            }
            // 排序并取前几名
            potentialElites.Sort((a, b) => scores[b].CompareTo(scores[a])); // 降序

            int needCount = targetCores - eliteCities.Count;
            for (int i = 0; i < needCount && i < potentialElites.Count; i++)
            {
                eliteCities.Add(potentialElites[i]);
            }

            // ---------------------------------------------------------
            // 步骤 4: 分配配额
            // ---------------------------------------------------------
            Dictionary<Architecture, int> idealQuotas = new Dictionary<Architecture, int>();
            int assignedCount = 0;

            foreach (Architecture arch in eliteCities)
            {
                idealQuotas[arch] = 4; // 基础配额
                assignedCount += 4;
            }

            int remainder = strategicTotal - assignedCount;

            if (remainder > 0)
            {
                // 筛选副城 (必须是 ID==1)
                var secondaryCities = new List<Architecture>();
                foreach (Architecture a in archs)
                {
                    if (!eliteCities.Contains(a) && a.Kind != null && a.Kind.ID == 1)
                    {
                        secondaryCities.Add(a);
                    }
                }
                secondaryCities.Sort((a, b) => scores[b].CompareTo(scores[a]));

                foreach (Architecture sec in secondaryCities)
                {
                    if (remainder <= 0) break;
                    int give = Math.Min(remainder, 3);
                    idealQuotas[sec] = give;
                    remainder -= give;
                }

                // 回填
                if (remainder > 0)
                {
                    foreach (Architecture elite in eliteCities)
                    {
                        if (remainder <= 0) break;
                        idealQuotas[elite]++;
                        remainder--;
                    }
                }

                if (remainder > 0 && this.Capital != null)
                {
                    if (!idealQuotas.ContainsKey(this.Capital)) idealQuotas[this.Capital] = 0;
                    idealQuotas[this.Capital] += remainder;
                }
            }

            // ---------------------------------------------------------
            // 步骤 5: 执行调动
            // ---------------------------------------------------------

            // 准备传输池
            List<Person> transferPool = new List<Person>();

            // 收集多余人员
            foreach (Architecture sup in archs)
            {
                if (!idealQuotas.ContainsKey(sup)) idealQuotas[sup] = 0;

                int surplus = sup.PersonCount - idealQuotas[sup];
                if (surplus <= 0) continue;

                // 从该城找闲人
                List<Person> cityCandidates = new List<Person>();
                if (sup.Persons != null)
                {
                    foreach (object obj in sup.Persons)
                    {
                        Person p = (obj is Person ? (Person)obj : null);
                        if (p != null && availablePersons.Contains(p) && CanMovePersonV90(p, sup))
                        {
                            cityCandidates.Add(p);
                        }
                    }
                }

                // 按功绩排序，取出多余的
                cityCandidates.Sort((a, b) => a.Merit.CompareTo(b.Merit));

                for (int i = 0; i < surplus && i < cityCandidates.Count; i++)
                {
                    transferPool.Add(cityCandidates[i]);
                }
            }

            // 填充缺人城市
            var consumers = new List<Architecture>();
            foreach (Architecture a in archs)
            {
                if (a.PersonCount < idealQuotas[a]) consumers.Add(a);
            }
            consumers.Sort((a, b) => scores[b].CompareTo(scores[a]));

            foreach (Architecture con in consumers)
            {
                int need = idealQuotas[con] - con.PersonCount;
                if (need <= 0) continue;

                for (int i = 0; i < need; i++)
                {
                    if (transferPool.Count == 0) break;

                    Person bestFit = null;
                    // 找一个不在目标城市的人
                    foreach (Person p in transferPool)
                    {
                        if (p.LocationArchitecture != con)
                        {
                            bestFit = p;
                            break;
                        }
                    }

                    if (bestFit != null)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[V9.0 调动] {bestFit.Name} -> {con.Name}");
#endif
                        MovePersonV90(bestFit, con);
                        transferPool.Remove(bestFit);
                    }
                }
            }
        }

        // --- 辅助方法 V9.0 ---

        private bool CanMovePersonV90(Person p, Architecture currentArch)
        {
            if (p == null) return false;
            if (p.Status != PersonStatus.Normal) return false;
            if (p.NvGuan) return false;
            if (p == currentArch.Mayor) return false;
            if (p == this.Leader) return false;
            if (p.LocationArchitecture != currentArch) return false;
            return true;
        }

        private float CalculatePersonnelDemandV90(Architecture arch)
        {
            if (arch == null) return 0f;
            float score = 0f;
            if (arch.HasHostileTroopsInView()) score += 100f;
            if (arch.FrontLine) score += 50f;
            score += arch.Population / 1000f;
            score += arch.Fund / 5000f;
            if (arch == this.Capital) score += 30f;
            return score;
        }

        private void MovePersonV90(Person person, Architecture target)
        {
            if (person == null || target == null) return;
            if (person.LocationArchitecture == target) return;
            person.MoveToArchitecture(target);
        }
        /// <summary>
        /// 判断武将是否可以被调动
        /// </summary>
        private bool CanMovePerson(Person p, Architecture currentArch)
        {
            if (p == null) return false;

            // 不能调动的情况：
            // 1. 不是正常状态
            if (p.Status != PersonStatus.Normal) return false;

            // 2. 是女官
            if (p.NvGuan) return false;

            // 3. 是太守
            if (p == currentArch.Mayor) return false;

            // 4. 是君主
            if (p == this.Leader) return false;

            // 5. 人不在这个城市
            if (p.LocationArchitecture != currentArch) return false;

            // 6. 有 DontMoveMeUnlessIMust 标记（如果存在）
            // 注意：需要检查 Person 类是否有这个属性
            // if (p.DontMoveMeUnlessIMust) return false;

            return true;
        }

        /// <summary>
        /// V8.1 简化版人员需求评估（用于核心城市选拔）
        /// </summary>
        private float CalculatePersonnelDemandV81(Architecture arch)
        {
            if (arch == null) return 0f;

            float score = 0f;

            // 战争状态 - 最高优先级
            if (arch.HasHostileTroopsInView())
            {
                score += 100f;
            }

            // 前线城市
            if (arch.FrontLine)
            {
                score += 50f;
            }
            
            // 🔥 新占领城市 - 高优先级（仅次于战争状态）
            // 日期：2026-03-12
            // 原因：新占领城市需要快速稳定，优先调配人员
            if (arch.IsRecentlyOccupied())
            {
                score += 80f;
            }

            // 人口规模
            score += arch.Population / 1000f;

            // 资金规模
            score += arch.Fund / 5000f;

            // 是首都
            if (arch == this.Capital)
            {
                score += 30f;
            }

            return score;
        }

        /// <summary>
        /// 移动武将到目标城市（使用现有的MovePerson方法）
        /// </summary>
        private void MovePerson(Person person, Architecture target)
        {
            if (person == null || target == null) return;
            if (person.LocationArchitecture == target) return;

            // 使用现有的MoveToArchitecture方法
            person.MoveToArchitecture(target);
        }

        /// <summary>
        /// 辅助：获取一个城市里能动的武将（兼容旧代码）
        /// </summary>
        private List<Person> GetMovableOfficers(Architecture arch)
        {
            var list = new List<Person>();
            foreach (Person p in arch.Persons)
            {
                if (CanMovePerson(p, arch))
                {
                    list.Add(p);
                }
            }
            return list;
        }

        /// <summary>
        /// 计算人员需求评分（兼容旧代码，调用V8.1版本）
        /// </summary>
        private float CalculatePersonnelDemand(Architecture arch)
        {
            return CalculatePersonnelDemandV81(arch);
        }

        #endregion

        #region V7 Merged Features - 自动登庸/探索/战略姿态

        /// <summary>
        /// 🔥 V7.0 战略姿态加成
        /// 根据当前战略姿态对人员需求评分进行调整
        /// </summary>
        private float ApplyStrategyMultiplier(Architecture arch, float baseScore)
        {
            float strategyMultiplier = 1.0f;

            switch (this.CurrentStrategicStance)
            {
                case WorldOfTheThreeKingdoms.GameManager.StrategicStanceLocal.Aggressive:
                    if (arch.FrontLine || arch.IsStrategicFrontline()) strategyMultiplier = 1.2f;
                    else if (arch == this.Capital) strategyMultiplier = 1.0f;
                    else strategyMultiplier = 0.8f;
                    break;

                case WorldOfTheThreeKingdoms.GameManager.StrategicStanceLocal.Defensive:
                case WorldOfTheThreeKingdoms.GameManager.StrategicStanceLocal.Panic:
                    if (arch == this.Capital) strategyMultiplier = 1.4f;
                    else if (arch.IsStrategicFrontline()) strategyMultiplier = 1.1f;
                    else strategyMultiplier = 0.9f;
                    break;

                case WorldOfTheThreeKingdoms.GameManager.StrategicStanceLocal.Consolidation:
                default:
                    if (arch.Population > 50000) strategyMultiplier = 1.1f;
                    break;
            }

            return baseScore * strategyMultiplier;
        }

        /// <summary>
        /// 获取当前的"三巨头"城市列表 (V7 Public API)
        /// </summary>
        public List<Architecture> GetEliteCities()
        {
            var myArchitectures = Session.Current.Scenario.Architectures.GameObjects
                .Where(a => a is Architecture arch && arch.BelongedFaction == this)
                .Cast<Architecture>()
                .ToList();

            if (myArchitectures.Count <= 1) return myArchitectures;

            Dictionary<Architecture, float> scores = new Dictionary<Architecture, float>();
            foreach (var a in myArchitectures)
            {
                scores[a] = CalculatePersonnelDemand(a);
            }

            List<Architecture> eliteCities = new List<Architecture>();

            if (this.Capital != null && myArchitectures.Contains(this.Capital))
            {
                eliteCities.Add(this.Capital);
            }

            var topScorers = scores
                .Where(x => !eliteCities.Contains(x.Key))
                .Where(x => x.Key.Population >= 10000 || x.Value > 80.0f)
                .OrderByDescending(x => x.Value)
                .Take(3 - eliteCities.Count)
                .Select(x => x.Key)
                .ToList();

            eliteCities.AddRange(topScorers);

            return eliteCities;
        }

        /// <summary>
        /// 判断是否为精英城市
        /// </summary>
        private bool IsEliteCity(Architecture arch)
        {
            return GetEliteCities().Contains(arch);
        }

        /// <summary>
        /// 自动登庸与探索逻辑 (V7 整合版)
        /// </summary>
        private void AutoSearchAndEmploy(List<Architecture> archs)
        {
            foreach (var arch in archs)
            {
                if (arch.PersonCount < 2) continue;

                foreach (Person freePerson in arch.NoFactionPersons)
                {
                    Person recruiter = GetBestRecruiter(arch);
                    if (recruiter != null)
                    {
                        this.Employ(recruiter, freePerson);
                    }
                }

                if (arch.NoFactionPersons.Count == 0 && arch.Fund > 500)
                {
                    Person searcher = GetBestSearcher(arch);
                    if (searcher != null)
                    {
                        this.Search(searcher);
                    }
                }
            }
        }

        private Person GetBestRecruiter(Architecture arch)
        {
            return GetMovableOfficers(arch)
                .OrderByDescending(p => p.Glamour)
                .FirstOrDefault();
        }

        private Person GetBestSearcher(Architecture arch)
        {
            return GetMovableOfficers(arch)
               .OrderByDescending(p => p.Politics)
               .FirstOrDefault();
        }

        private void Employ(Person recruiter, Person target)
        {
            if (recruiter.CanConvince(target))
            {
                int chance = recruiter.CanConvinceChance(target);
                if (GameObject.Chance(chance))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AutoSearchAndEmploy] {recruiter.Name} 登庸 {target.Name} [成功] (几率:{chance}%)");
#endif
                    recruiter.ConvincePersonSuccess(target);
                }
                else
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AutoSearchAndEmploy] {recruiter.Name} 登庸 {target.Name} [失败] (几率:{chance}%)");
#endif
                    if (target.BelongedFaction != null && target.TempLoyaltyChange < 10)
                    {
                        target.TempLoyaltyChange += GameObject.Random(1, 2);
                    }
                }
            }
        }

        private void Search(Person searcher)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AutoSearchAndEmploy] {searcher.Name} 在 {searcher.LocationArchitecture.Name} 执行探索");
#endif
            searcher.TargetArchitecture = searcher.LocationArchitecture;
            searcher.DoSearch();
        }

        /// <summary>
        /// 获取城市的人员需求评分（供外部调用）
        /// </summary>
        public float GetPersonnelDemandScore(Architecture arch)
        {
            return CalculatePersonnelDemand(arch);
        }

        /// <summary>
        /// 获取势力所有城市的人员需求排序
        /// </summary>
        public List<Architecture> GetArchitecturesByPersonnelPriority()
        {
            var myArchitectures = Session.Current.Scenario.Architectures.GameObjects
                .Where(a => a is Architecture arch && arch.BelongedFaction == this)
                .Cast<Architecture>()
                .ToList();

            return myArchitectures
                .OrderByDescending(a => CalculatePersonnelDemand(a))
                .ToList();
        }

        /// <summary>
        /// 兼容原有的 AITransfer 系统
        /// </summary>
        public void EnhancedPersonnelTransfer(ArchitectureList architectures)
        {
            var archList = architectures.GetList().Cast<Architecture>().ToList();
            RunPersonnel_V81(archList);
        }

        /// <summary>
        /// 兼容军区系统的人员管理
        /// </summary>
        public void SectionPersonnelManagement(List<Architecture> sectionArchitectures)
        {
            if (sectionArchitectures.Count > 1)
            {
                RunPersonnel_V81(sectionArchitectures);
            }
        }

        #endregion

        #region V8.5 战略视角版人员调配

        /// <summary>
        /// 势力人员自动调配逻辑 (V8.5 战略视角的修正版)
        /// 核心修复：核心数计算基于"势力总人数"(含出征/移动)，而非"闲置人数"
        /// </summary>
        /// <param name="archs">势力下属的城市列表</param>
        private void RunPersonnel_V85(List<Architecture> archs)
        {
            try
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"");
                System.Diagnostics.Debug.WriteLine($"╔═══════════════════════════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"║ [🚀 V8.5 ENTRY] 势力: {this.Name}");
                System.Diagnostics.Debug.WriteLine($"║ 城市数量: {archs?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"╚═══════════════════════════════════════════════════════════════");
#endif

                if (archs == null || archs.Count <= 1)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[V8.5 EXIT] 城市数不足，直接返回");
#endif
                    return;
                }

                // =================================================================
                // 📊 1. 战略统计 (Strategic Count) - 决定盘子多大
                // =================================================================
                var allPersons = this.Persons;
                int strategicTotal = 0;

                if (allPersons != null)
                {
                    foreach (GameObject obj in allPersons)
                    {
                        Person p = (obj is Person ? (Person)obj : null);
                        if (p != null && !p.IsCaptive && p.Alive)
                        {
                            strategicTotal++;
                        }
                    }
                }

                // =================================================================
                // 🧹 2. 战术清洗 (Tactical Pool) - 决定谁能干活
                // =================================================================
                var availablePersons = new List<Person>();

                foreach (var arch in archs)
                {
                    if (arch == null || arch.Persons == null) continue;

                    foreach (GameObject obj in arch.Persons)
                    {
                        Person p = (obj is Person ? (Person)obj : null);
                        if (p != null && !p.IsCaptive && p.Status == PersonStatus.Normal)
                        {
                            availablePersons.Add(p);
                        }
                    }
                }

                // =================================================================
                // 🛑 3. 贫困线检查 (用总人数判断)
                // =================================================================
                if (strategicTotal < 8)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[V8.5 EXIT] 战略总人数不足8人，跳过调配");
#endif
                    return;
                }

                // =================================================================
                // 📉 4. 核心数计算 (基于战略总数)
                // =================================================================
                int targetCores = (strategicTotal + 2) / 5;

                int mapLimit = (archs.Count + 1) / 2;
                targetCores = Math.Min(targetCores, mapLimit);

                if (targetCores < 1) targetCores = 1;
                if (targetCores > archs.Count) targetCores = archs.Count;

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[V8.5] 势力:{this.Name} 战略总数:{strategicTotal} (闲置:{availablePersons.Count}) -> 核心:{targetCores}");
#endif

                // =================================================================
                // 🏆 5. 选拔核心 (Scores)
                // =================================================================
                Dictionary<Architecture, float> scores = new Dictionary<Architecture, float>();
                foreach (var a in archs)
                {
                    if (a != null)
                    {
                        try
                        {
                            scores[a] = this.CalculatePersonnelDemand(a);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[V8.5 错误] 计算城市{a.Name}需求评分失败: {ex.Message}");
                            scores[a] = 0f;
                        }
                    }
                }

                List<Architecture> eliteCities = new List<Architecture>();

                if (this.Capital != null && archs.Contains(this.Capital))
                {
                    eliteCities.Add(this.Capital);
                }

                // 安全的候选城市选拔
                var candidatesList = new List<Architecture>();
                foreach (var kvp in scores)
                {
                    if (kvp.Key == null || eliteCities.Contains(kvp.Key)) continue;

                    if (kvp.Key.Kind != null &&
                        (kvp.Key.Kind.HasAgriculture || kvp.Key.Kind.HasCommerce) &&
                        kvp.Key.Population >= 3000)
                    {
                        candidatesList.Add(kvp.Key);
                    }
                    else if (kvp.Value > 80f)
                    {
                        candidatesList.Add(kvp.Key);
                    }
                }

                var selectedCandidates = candidatesList
                    .OrderByDescending(a => scores[a])
                    .Take(Math.Max(0, targetCores - eliteCities.Count))
                    .ToList();

                eliteCities.AddRange(selectedCandidates);

                // =================================================================
                // 🧊 6. 理想配额计算
                // =================================================================
                Dictionary<Architecture, int> idealQuotas = new Dictionary<Architecture, int>();
                int baseQuota = 4;
                int assignedCount = 0;

                foreach (var arch in eliteCities)
                {
                    if (arch != null)
                    {
                        idealQuotas[arch] = baseQuota;
                        assignedCount += baseQuota;
                    }
                }

                int remainder = strategicTotal - assignedCount;

                if (remainder > 0)
                {
                    // 🛑 核心修复：把漏掉的关口过滤器加回来！
                    // 只有具备 农业 或 商业 能力的据点，才能作为副城接收分流人员
                    var secondaryCities = archs
                        .Where(a => a != null && !eliteCities.Contains(a))
                        .Where(a => a.Kind != null && (a.Kind.HasAgriculture || a.Kind.HasCommerce)) // <--- 关口过滤器
                        .OrderByDescending(a => scores.ContainsKey(a) ? scores[a] : 0f)
                        .ToList();

                    foreach (var sec in secondaryCities)
                    {
                        if (remainder <= 0) break;
                        int give = Math.Min(remainder, 3);
                        idealQuotas[sec] = give;
                        remainder -= give;
                    }

                    if (remainder > 0)
                    {
                        foreach (var elite in eliteCities)
                        {
                            if (remainder <= 0 || elite == null) break;
                            idealQuotas[elite]++;
                            remainder--;
                        }
                    }

                    if (remainder > 0 && this.Capital != null && idealQuotas.ContainsKey(this.Capital))
                    {
                        idealQuotas[this.Capital] += remainder;
                    }
                }

                foreach (var arch in archs)
                {
                    if (arch != null && !idealQuotas.ContainsKey(arch))
                    {
                        idealQuotas[arch] = 0;
                    }
                }

                // =================================================================
                // 🚚 7. 执行调动 (仅使用 availablePersons)
                // =================================================================
                var suppliers = archs
                    .Where(a => a != null && idealQuotas.ContainsKey(a) && a.PersonCount > idealQuotas[a])
                    .ToList();

                List<Person> transferPool = new List<Person>();

                foreach (var sup in suppliers)
                {
                    if (sup == null || sup.Persons == null) continue;

                    int surplus = sup.PersonCount - idealQuotas[sup];
                    if (surplus <= 0) continue;

                    var personCandidates = new List<Person>();
                    foreach (GameObject obj in sup.Persons)
                    {
                        Person p = (obj is Person ? (Person)obj : null);
                        if (p != null && availablePersons.Contains(p))
                        {
                            personCandidates.Add(p);
                        }
                    }

                    var candidatesList2 = personCandidates
                        .OrderBy(p => p.Merit)
                        .Take(surplus)
                        .ToList();

                    transferPool.AddRange(candidatesList2);
                }

                var consumers = archs
                    .Where(a => a != null && idealQuotas.ContainsKey(a) && a.PersonCount < idealQuotas[a])
                    .OrderByDescending(a => scores.ContainsKey(a) ? scores[a] : 0f)
                    .ToList();

                foreach (var con in consumers)
                {
                    if (con == null) continue;

                    int need = idealQuotas[con] - con.PersonCount;
                    if (need <= 0) continue;

                    for (int i = 0; i < need; i++)
                    {
                        if (transferPool.Count == 0) break;

                        Person bestFit = null;
                        foreach (var p in transferPool)
                        {
                            if (p != null && p.LocationArchitecture != con)
                            {
                                bestFit = p;
                                break;
                            }
                        }

                        if (bestFit != null)
                        {
#if DEBUG
                            string originName = bestFit.LocationArchitecture != null ?
                                              bestFit.LocationArchitecture.Name : "在野";
                            System.Diagnostics.Debug.WriteLine($"[V8.5 调动] {bestFit.Name}: {originName} -> {con.Name}");
#endif
                            try
                            {
                                this.MovePerson(bestFit, con);
                                transferPool.Remove(bestFit);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[V8.5 错误] 调动{bestFit.Name}失败: {ex.Message}");
                                transferPool.Remove(bestFit);
                            }
                        }
                    }
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[V8.5 完成] 势力:{this.Name} 人员调配完成");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[V8.5 严重错误] 势力:{this.Name} 人员调配异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[V8.5 堆栈] {ex.StackTrace}");
            }
        }

        #endregion
    }
}

