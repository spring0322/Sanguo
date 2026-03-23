using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.SectionDetail;
using GameManager;

namespace GameObjects
{
    public static class SectionAIHelper
    {
        // 配置常量
        private const int FACTION_SIZE_THRESHOLD = 12; 
        private const int RULER_CAPACITY = 6;
        private const int IDEAL_SECTION_SIZE = 4; // 理想军团规模
        
        // 调试开关 - 开启调试输出以便诊断军区AI问题
        public static bool EnableDebugOutput = false;

        public static void AutoOrganizeSections(Faction faction)
        {
#if DEBUG
            if (EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine("[AutoOrganizeSections] 开始为势力 " + faction.Name + " 自动划分军区");
                System.Diagnostics.Debug.WriteLine("[AutoOrganizeSections] 势力建筑数量: " + faction.ArchitectureCount);
                System.Diagnostics.Debug.WriteLine("[AutoOrganizeSections] 阈值: " + FACTION_SIZE_THRESHOLD);
            }
#endif
            
            if (faction.ArchitectureCount < FACTION_SIZE_THRESHOLD)
            {
#if DEBUG
                if (EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine("[AutoOrganizeSections] 势力太小，不分封，全部解散回归中央");
                }
#endif
                // 势力太小，不分封，全部解散回归中央
                RemoveAllSections(faction); 
                return;
            }

#if DEBUG
            if (EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine("[AutoOrganizeSections] 势力规模足够，开始划分军区");
            }
#endif

            // 1. 保护首都圈 (君主直辖)
            Architecture capital = faction.Leader.LocationArchitecture;
            if (capital == null) return; // 异常情况

            var coreArchitectures = GetCapitalZone(capital, faction.Architectures.GetList().Cast<Architecture>().ToList(), RULER_CAPACITY);
            
            // 候选城市池：除去首都圈的所有城市
            var pool = faction.Architectures.GetList().Cast<Architecture>().Except(coreArchitectures).ToList();

            // 2. 优先建立【前线战区军团】
            // 只要池子里还有前线城市，就尝试建立军团
            while (pool.Any(a => IsFrontLine(a)))
            {
                // 找出一个最危险的前线城市作为“种子”
                Architecture seed = GetMostDangerousFrontline(pool);
                if (seed == null) break;

                // 基于这个种子，建立一个有共同战略目标的军团
                CreateStrategicSection(faction, seed, pool);
            }

            // 3. 剩余的后方城市处理
            // 如果还有剩下的安全城市，看情况是否需要建立【后方资源军团】
            if (pool.Count >= IDEAL_SECTION_SIZE)
            {
                CreateLogisticSection(faction, pool);
            }
        }

        /// <summary>
        /// 创建战略军团（核心逻辑）
        /// </summary>
        private static void CreateStrategicSection(Faction faction, Architecture seed, List<Architecture> pool)
        {
            // 1. 确定战略目标：这个种子城主要面对哪个敌人？
            Faction targetEnemy = GetMainEnemy(seed);
            if (targetEnemy == null)
            {
                 // 如果找不到主要敌人，但又是前线（可能是面对空城或中立），也需要处理
                 // 暂且归为无目标，或跳过
            }

            List<Architecture> sectionMembers = new List<Architecture>();
            sectionMembers.Add(seed);
            pool.Remove(seed);

            // 2. 广度优先搜索 (BFS) 扩张军团
            // 目标：吸纳拥有【相同假想敌】的前线，以及【直接相连】的后方
            Queue<Architecture> queue = new Queue<Architecture>();
            queue.Enqueue(seed);

            int currentSize = 1;

            while (queue.Count > 0 && currentSize < IDEAL_SECTION_SIZE)
            {
                Architecture current = queue.Dequeue();
                
                // 获取所有邻居
                foreach (Architecture neighbor in GetLinkedArchitectures(current))
                {
                    // 必须是在候选池里的（不能抢首都圈的，也不能抢已经分封的）
                    if (!pool.Contains(neighbor)) continue;

                    bool shouldAdd = false;

                    if (IsFrontLine(neighbor))
                    {
                        // Case A: 它是前线，且面对同一个敌人 -> 加入
                        // (防止把抗魏的前线和抗吴的前线混在一起)
                        if (targetEnemy != null && GetMainEnemy(neighbor) == targetEnemy)
                        {
                            shouldAdd = true;
                        }
                        else if (targetEnemy == null) 
                        {
                            shouldAdd = true; // 如果种子没有特定敌人，只要是前线就拉进来
                        }
                    }
                    else
                    {
                        // Case B: 它是后方，且紧挨着当前的前线 -> 加入作为补给腹地
                        // 只有当军团还比较空的时候才吸纳后方，优先吸纳前线
                        if (currentSize < IDEAL_SECTION_SIZE - 1) 
                        {
                            shouldAdd = true;
                        }
                    }

                    if (shouldAdd)
                    {
                        sectionMembers.Add(neighbor);
                        pool.Remove(neighbor); // 从池中移除
                        queue.Enqueue(neighbor); // 继续向下搜索
                        currentSize++;
                        if (currentSize >= IDEAL_SECTION_SIZE) break;
                    }
                }
            }

            // 3. 如果凑出来的规模太小（比如是个孤城），暂不成立军团，或者强制划给最近的已有军团
            if (sectionMembers.Count < 2) 
            {
                // 这里简单处理：放回池子（实际上这会导致孤城一直留在池子里直到最后被Logistic或者直辖）
                // 为了避免死循环，这里不放回了，或者直接成立小军团？
                // 现在的逻辑是：如果不足2个，就不创建Section，这些城会被遗留在Pool里，
                // 后续CreateLogisticSection可能会捡漏，或者归君主直辖。
                return; 
            }

            // 4. 正式实例化
            Section newSection = new Section();
            newSection.BelongedFaction = faction;
            newSection.BelongedFactionID = faction.ID; // 🔥 修复：同步 ID
            
            // 🔥 修复：从配置表获取 AIDetail
            // 日期：2026-03-17
            // 原因：直接 new SectionAIDetail() 会导致 ID=0，且没有正确的配置
            // ANTI-BAND-AID：配置数据缺失是严重错误，必须 Fail Fast
            var aiDetails = Session.Current.Scenario.GameCommonData.AllSectionAIDetails
                .GetSectionAIDetailsByConditions(SectionOrientationKind.势力, true, false, true, true, false);
            
            if (aiDetails == null || aiDetails.Count == 0)
            {
                throw new InvalidOperationException(
                    $"配置数据损坏：无法找到 SectionOrientationKind.势力 的 SectionAIDetail 配置。" +
                    "游戏无法继续，请检查配置文件。");
            }
            
            newSection.AIDetail = aiDetails[0] as SectionAIDetail;
            
            if (newSection.AIDetail == null)
            {
                throw new InvalidOperationException(
                    "配置数据损坏：GetSectionAIDetailsByConditions 返回的对象不是 SectionAIDetail 类型。");
            }
            
            // 设定方针：非常明确的战略方向
            newSection.AIDetail.OrientationKind = SectionOrientationKind.势力; // 攻略势力

            foreach (var member in sectionMembers)
            {
                if (member.BelongedSection != null)
                {
                    member.BelongedSection.RemoveArchitecture(member);
                }
                newSection.AddArchitecture(member);
            }
            
            faction.AddSection(newSection);
            
            // 自动选帅
            newSection.AutoAppointLeader();
        }

        /// <summary>
        /// 创建后勤军团
        /// </summary>
        private static void CreateLogisticSection(Faction faction, List<Architecture> pool)
        {
            // 简单的贪婪逻辑：把剩下的挨在一起的城打包
            // 必须要是剩下的
            if (pool.Count == 0) return;

            // 🔥 安全修复：避免InvalidOperationException
            Architecture seed = pool.FirstOrDefault();
            if (seed == null) return; // 如果没有可用的种子城市，直接返回
            
            List<Architecture> members = new List<Architecture> { seed };
            pool.Remove(seed);

            // 简单的BFS把周围的后方城市拉进来
            Queue<Architecture> q = new Queue<Architecture>();
            q.Enqueue(seed);
            
            while(q.Count > 0 && members.Count < IDEAL_SECTION_SIZE)
            {
                var cur = q.Dequeue();
                foreach (var neighbor in GetLinkedArchitectures(cur))
                {
                    if (pool.Contains(neighbor))
                    {
                        members.Add(neighbor);
                        pool.Remove(neighbor);
                        q.Enqueue(neighbor);
                        if (members.Count >= IDEAL_SECTION_SIZE) break;
                    }
                }
            }

            if (members.Count >= 2)
            {
                Section newSection = new Section();
                newSection.BelongedFaction = faction;
                newSection.BelongedFactionID = faction.ID; // 🔥 修复：同步 ID
                
                // 🔥 修复：从配置表获取 AIDetail
                // 日期：2026-03-17
                // 原因：直接 new SectionAIDetail() 会导致 ID=0，且没有正确的配置
                // ANTI-BAND-AID：配置数据缺失是严重错误，必须 Fail Fast
                var aiDetails = Session.Current.Scenario.GameCommonData.AllSectionAIDetails
                    .GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false);
                
                if (aiDetails == null || aiDetails.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"配置数据损坏：无法找到 SectionOrientationKind.无 的 SectionAIDetail 配置。" +
                        "游戏无法继续，请检查配置文件。");
                }
                
                newSection.AIDetail = aiDetails[0] as SectionAIDetail;
                
                if (newSection.AIDetail == null)
                {
                    throw new InvalidOperationException(
                        "配置数据损坏：GetSectionAIDetailsByConditions 返回的对象不是 SectionAIDetail 类型。");
                }

                // 设定方针：支援/内政
                newSection.AIDetail.OrientationKind = SectionOrientationKind.无; // 无特定攻略目标，即内政/支援
                
                foreach (var m in members)
                {
                    if (m.BelongedSection != null) m.BelongedSection.RemoveArchitecture(m);
                    newSection.AddArchitecture(m);
                }
                faction.AddSection(newSection);
                newSection.AutoAppointLeader();
            }
        }

        // --- 辅助判断方法 ---
        public static bool IsFrontLine(Architecture a)
        {
            // 只要有一个敌对邻居就是前线
            return GetLinkedArchitectures(a).Any(n => 
                n.BelongedFaction != null && n.BelongedFaction != a.BelongedFaction);
        }

        private static Architecture GetMostDangerousFrontline(List<Architecture> list)
        {
            // 找出邻接敌军兵力最多的那个城 (压力最大的城)
            return list
                .Where(a => IsFrontLine(a))
                .OrderByDescending(a => GetEnemyTroopsAround(a))
                .FirstOrDefault();
        }

        private static int GetEnemyTroopsAround(Architecture a)
        {
            return GetLinkedArchitectures(a)
                .Where(n => n.BelongedFaction != null && n.BelongedFaction != a.BelongedFaction)
                .Sum(n => GetTotalSoldiers(n));
        }

        private static int GetTotalSoldiers(Architecture a)
        {
            if (a.Militaries == null) return 0;
            try
            {
                return a.Militaries.Cast<Military>().Sum(m => m.Quantity);
            }
            catch
            {
                return 0;
            }
        }

        private static Faction GetMainEnemy(Architecture a)
        {
            // 这个城面对的主要敌人是谁？(根据敌方兵力判断)
            // 先Group By Faction
            var enemies = GetLinkedArchitectures(a)
                .Where(n => n.BelongedFaction != null && n.BelongedFaction != a.BelongedFaction)
                .GroupBy(n => n.BelongedFaction)
                .OrderByDescending(g => g.Sum(n => GetTotalSoldiers(n)))
                .FirstOrDefault();

            return enemies?.Key;
        }

        private static List<Architecture> GetCapitalZone(Architecture capital, IList<Architecture> all, int count)
        {
            // 使用 BFS 获取距离首都最近的 N 个城
            // 保证直辖区是连在一起的
            List<Architecture> result = new List<Architecture> { capital };
            Queue<Architecture> q = new Queue<Architecture>();
            q.Enqueue(capital);

            // 为了防止死循环或无限扩散，最好限制范围或已访问集合
            HashSet<Architecture> visited = new HashSet<Architecture>();
            visited.Add(capital);

            while (q.Count > 0 && result.Count < count)
            {
                Architecture curr = q.Dequeue();
                foreach (var next in GetLinkedArchitectures(curr))
                {
                    if (next.BelongedFaction == capital.BelongedFaction && !result.Contains(next))
                    {
                        result.Add(next);
                        visited.Add(next);
                        q.Enqueue(next);
                        if (result.Count >= count) break;
                    }
                }
            }
            return result;
        }
        
        // 清空军团辅助
        public static void RemoveAllSections(Faction faction)
        {
            // 需要实现具体的清理逻辑
            // 需要实现具体的清理逻辑
            foreach(Section section in faction.Sections.GetList())
            {
                // Move archs back to no section (Leader direct control)
                foreach(Architecture a in section.Architectures.GetList())
                {
                    a.BelongedSection = null;
                }
                section.Architectures.Clear();
                // Remove section from faction happens implicitly? No.
                // faction.Sections is a SectionList.
            }
            faction.Sections.Clear();
        }

        private static List<Architecture> GetLinkedArchitectures(Architecture a)
        {
            List<Architecture> result = new List<Architecture>();
            if (a.AILandLinks != null)
            {
                foreach(var obj in a.AILandLinks)
                {
                    if (obj is Architecture n) result.Add(n);
                }
            }
            if (a.AIWaterLinks != null)
            {
                foreach (var obj in a.AIWaterLinks)
                {
                    if (obj is Architecture n) result.Add(n);
                }
            }
            return result.Distinct().ToList();
        }
        
        // Helper to get distances if needed, already available in GameScenario
        // private static double GetDistance(Architecture a, Architecture b) ...
    }
}
