using System;
using System.Collections.Generic;
using System.Linq;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using GameObjects.SectionDetail;
using Microsoft.Xna.Framework;
using GameObjects.PersonDetail;

namespace GameManager
{
    /// <summary>
    /// 军团编制系统 - 负责自动划分势力版图和组建军团 (Section)
    /// </summary>
    public class LegionOrganizationSystem
    {
        public static LegionOrganizationSystem Instance { get; private set; }

        // 配置参数
        private const int MaxCitiesPerLegion = 6;      // 一个军团最多管理多少城
        private const int CentralControlRadius = 10;   // 首都直辖范围（距离单位）
        private const int InterCityConnectionDist = 6; // 城市间判定为“相邻”的距离

        public LegionOrganizationSystem()
        {
            Instance = this;
        }

        /// <summary>
        /// [核心入口] 重新组织势力的军团结构
        /// </summary>
        public void ReorganizeLegions(Faction faction)
        {
            if (faction == null || faction.Architectures.Count == 0 || faction.Capital == null) return;

            // 1. 准备工作：解散所有现有军团
            ClearLegionStructure(faction);

            // 2. 建立中央直辖军团 (第一军团)
            Section centralLegion = CreateLegion(faction, "中央军团");
           // centralLegion.ID = 1; // ID is usually auto-managed or read-only? 
           // If ID is managed by GameObject management, we shouldn't set it manually if possible.
           // But here we rely on CreateLegion to set it or system defaults.

            // 首都必然属于中央军团
            Architecture capital = faction.Capital;
            AssignCityToLegion(capital, centralLegion);

            // 3. 将首都附近的城市划入直辖
            var unassignedCities = new List<Architecture>();
            foreach(Architecture a in faction.Architectures.GetList())
            {
                if (a != capital) unassignedCities.Add(a);
            }

            // 找出所有距离首都在阈值内的城市
            var centralCities = unassignedCities
                .Where(c => Session.Current.Scenario.GetDistance(c.Position, capital.Position) <= CentralControlRadius)
                .OrderBy(c => Session.Current.Scenario.GetDistance(c.Position, capital.Position))
                .Take(MaxCitiesPerLegion - 1) 
                .ToList();

            foreach (var city in centralCities)
            {
                AssignCityToLegion(city, centralLegion);
                unassignedCities.Remove(city);
            }

            // 4. 处理剩余的“飞地”城市 - 聚类算法
            while (unassignedCities.Count > 0)
            {
                // 选取一个种子城市（离首都最近的未分配城市）
                // 🔥 安全修复：避免InvalidOperationException
                var orderedCities = unassignedCities
                    .OrderBy(c => Session.Current.Scenario.GetDistance(c.Position, capital.Position));
                
                Architecture seedCity = orderedCities.FirstOrDefault();
                if (seedCity == null) break; // 如果没有可用城市，退出循环

                // 创建新军团
                string legionName = $"{seedCity.Name}方面军";
                Section newLegion = CreateLegion(faction, legionName);
                AssignCityToLegion(seedCity, newLegion);
                unassignedCities.Remove(seedCity);

                // 贪婪算法：寻找种子城市周边的城市加入该军团
                bool cityAdded = true;
                while (cityAdded && newLegion.Architectures.Count < MaxCitiesPerLegion)
                {
                    cityAdded = false;
                    Architecture bestCandidate = null;
                    double minLocalDist = double.MaxValue;

                    Point legionCenter = GetSectionCenter(newLegion);

                    foreach (var candidate in unassignedCities)
                    {
                        // 计算候选城市与当前军团中心的距离
                        double dist = Session.Current.Scenario.GetDistance(candidate.Position, legionCenter);

                        if (dist <= InterCityConnectionDist && dist < minLocalDist)
                        {
                            minLocalDist = dist;
                            bestCandidate = candidate;
                        }
                    }

                    if (bestCandidate != null)
                    {
                        AssignCityToLegion(bestCandidate, newLegion);
                        unassignedCities.Remove(bestCandidate);
                        cityAdded = true;
                    }
                }

                // 5. 为新军团任命军团长 (都督)
                AppointBestViceroy(newLegion);
            }

            // 6. 最后的清理：如果有军团是空的，移除它
            // Iterate backwards or separate list
            var emptySections = new List<Section>();
            foreach(Section s in faction.Sections.GetList())
            {
                if (s.Architectures.Count == 0) emptySections.Add(s);
            }
            foreach(Section s in emptySections)
            {
                faction.Sections.Remove(s);
            }
            
            System.Diagnostics.Debug.WriteLine($"[军团编制] 势力 {faction.Name} 重组完成，共 {faction.Sections.Count} 个军团。");
        }

        /// <summary>
        /// 自动任命/更换军团长
        /// </summary>
        public void AppointBestViceroy(Section legion)
        {
            if (legion == null || legion.BelongedFaction == null) return;
            
            // 中央军团由君主亲自指挥 (Assuming first one or by name)
            bool isCentral = (legion.Name == "中央军团");
            if (!isCentral)
            {
                foreach (Architecture a in legion.Architectures.GetList())
                {
                    if (a == legion.BelongedFaction.Capital)
                    {
                        isCentral = true;
                        break;
                    }
                }
            }

            if (isCentral)
            {
                legion.SectionLeader = legion.BelongedFaction.Leader;
                return;
            }

            // 1. 获取辖区内所有候选人
            var candidates = new List<Person>();
            foreach(Architecture a in legion.Architectures.GetList())
            {
                foreach(Person p in a.Persons)
                {
                    if (p.Status == PersonStatus.Normal && !p.IsCaptive)
                    {
                        candidates.Add(p);
                    }
                }
            }

            if (candidates.Count == 0) return;

            // 2. 判断军团性质 (前线/后方)
            bool isFrontline = false;
            foreach(Architecture a in legion.Architectures.GetList())
            {
                if (SectionAIHelper.IsFrontLine(a))
                {
                    isFrontline = true;
                    break;
                }
            }

            // 3. 评分选择
            Person bestCandidate = null;
            float maxScore = -1;

            foreach (var p in candidates)
            {
                // 基础分：名声 + 功绩
                float score = (p.Reputation / 10.0f) + (p.Merit / 100.0f);

                // 忠诚度修正
                if (p.PersonalLoyalty < 90) score *= 0.5f;
                
                // 能力分
                if (isFrontline)
                {
                    // 前线看统率和武力
                    score += p.Command * 1.5f + p.Strength * 1.0f;
                }
                else
                {
                    // 后方看政治和魅力
                    score += p.Politics * 1.5f + p.Glamour * 1.0f;
                }

                if (score > maxScore)
                {
                    maxScore = score;
                    bestCandidate = p;
                }
            }

            if (bestCandidate != null && bestCandidate != legion.SectionLeader)
            {
                legion.SectionLeader = bestCandidate;
                
                // Policy
                if (isFrontline)
                {
                    legion.AIDetail.OrientationKind = SectionOrientationKind.势力; // Attack
                }
                else
                {
                    legion.AIDetail.OrientationKind = SectionOrientationKind.无; // Develop
                }
                
                System.Diagnostics.Debug.WriteLine($"[任命] {legion.Name} 新任都督: {bestCandidate.Name}");
            }
        }

        // --- 辅助方法 ---

        private Section CreateLegion(Faction faction, string name)
        {
            Section section = new Section();
            section.Name = name;
            section.BelongedFaction = faction;
            section.BelongedFactionID = faction.ID; // 🔥 修复：同步 ID
            
            // 🔥 修复：设置 AIDetail
            // 日期：2026-03-17
            // 原因：Section 必须有 AIDetail，否则读档时会抛出 InvalidOperationException
            // ANTI-BAND-AID：配置数据缺失是严重错误，必须 Fail Fast
            var aiDetails = Session.Current.Scenario.GameCommonData.AllSectionAIDetails
                .GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false);
            
            if (aiDetails == null || aiDetails.Count == 0)
            {
                throw new InvalidOperationException(
                    $"配置数据损坏：无法找到 SectionOrientationKind.无 的 SectionAIDetail 配置。" +
                    "游戏无法继续，请检查配置文件。");
            }
            
            section.AIDetail = aiDetails[0] as SectionAIDetail;
            
            if (section.AIDetail == null)
            {
                throw new InvalidOperationException(
                    "配置数据损坏：GetSectionAIDetailsByConditions 返回的对象不是 SectionAIDetail 类型。");
            }
            
            // Architectures list is usually auto-created in constructor
            
            // Add to faction
            faction.Sections.Add(section);
            return section;
        }

        private void AssignCityToLegion(Architecture city, Section legion)
        {
            if (city.BelongedSection != null)
            {
                city.BelongedSection.RemoveArchitecture(city);
            }
            legion.AddArchitecture(city);
            // AddArchitecture usually handles setting BelongedSection
        }

        private void ClearLegionStructure(Faction faction)
        {
            // We use SectionAIHelper.RemoveAllSections logic but implementation here
            // Removing architectures from sections
            var sections = new List<Section>();
            foreach(Section s in faction.Sections.GetList())
            {
                sections.Add(s);
            }
            
            foreach (Section s in sections)
            {
                // Remove all architectures from section
                var archs = new List<Architecture>();
                foreach(Architecture a in s.Architectures.GetList())
                {
                    archs.Add(a);
                }
                foreach(Architecture a in archs)
                {
                    s.RemoveArchitecture(a);
                }
                faction.Sections.Remove(s);
            }
        }
        
        private Point GetSectionCenter(Section s)
        {
             if (s.Architectures.Count == 0) return new Point(0,0);
             int sumX = 0, sumY = 0;
             int count = 0;
             foreach(Architecture a in s.Architectures.GetList())
             {
                 sumX += a.Position.X;
                 sumY += a.Position.Y;
                 count++;
             }
             return new Point(sumX / count, sumY / count);
        }
    }
}
