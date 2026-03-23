using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.SectionDetail;
using GameObjects.PersonDetail;
using Microsoft.Xna.Framework; // For Point

namespace GameManager
{
    /// <summary>
    /// 军团人事调动管理器 - 负责武将的自动分配
    /// </summary>
    public class LegionTransferManager
    {
        public static LegionTransferManager Instance { get { return instance; } }
        private static LegionTransferManager instance = new LegionTransferManager();

        private const int TransferCooldownDays = 30; // 调动冷却时间
        private Dictionary<int, GameDate> _lastTransferTime = new Dictionary<int, GameDate>();

        // 评分权重
        private const float CombatWeight = 1.5f;   // 战斗属性权重
        private const float DomesticWeight = 1.2f; // 内政属性权重

        public LegionTransferManager()
        {
        }

        /// <summary>
        /// 执行势力的自动人事调动
        /// </summary>
        public void AutoRedistributeOfficers(Faction faction)
        {
            // 1. 基础检查：是否处于冷却期
            if (_lastTransferTime.ContainsKey(faction.ID))
            {
                int currentDays = GetTotalDays(Session.Current.Scenario.Date);
                int lastDays = GetTotalDays(_lastTransferTime[faction.ID]);
                if (currentDays - lastDays < TransferCooldownDays)
                    return;
            }

            // 2. 获取该势力的所有军团
            var legions = GetFactionLegions(faction);
            if (legions.Count < 2) return; // 只有一个军团不需要调动

            // 3. 评估每个军团的需求
            var legionNeeds = new Dictionary<Section, LegionNeedProfile>();
            foreach (var legion in legions)
            {
                legionNeeds[legion] = AnalyzeLegionNeeds(legion);
            }

            // 4. 寻找可以调动的武将 (候选池)
            var transferCandidates = new List<Person>();
            foreach (var legion in legions)
            {
                transferCandidates.AddRange(IdentifyTransferableOfficers(legion, legionNeeds[legion]));
            }

            // 5. 执行分配逻辑
            foreach (var person in transferCandidates)
            {
                Section bestTarget = FindBestLegionForPerson(person, legions, legionNeeds);
                
                // 如果最佳去处不是当前所在地，且不是“无地可去”
                if (bestTarget != null && !IsPersonInLegion(person, bestTarget))
                {
                    ExecuteTransfer(person, bestTarget);
                }
            }

            _lastTransferTime[faction.ID] = new GameDate(Session.Current.Scenario.Date); // Copy date
        }

        /// <summary>
        /// 军团需求画像
        /// </summary>
        public class LegionNeedProfile
        {
            public float CombatNeed;   // 对战斗力的渴求度 (0-100)
            public float PoliticsNeed; // 对内政的渴求度 (0-100)
            public bool IsFrontline;   // 是否是前线
        }

        /// <summary>
        /// 分析军团需求
        /// </summary>
        private LegionNeedProfile AnalyzeLegionNeeds(Section legion)
        {
            var profile = new LegionNeedProfile();
            
            // A. 判断是否是前线
            // 使用 SectionAIHelper 的逻辑：只要 section 内有任何一个前线城市，就算前线 section
            bool isFrontLine = false;
            foreach(Architecture a in legion.Architectures.GetList())
            {
                if (SectionAIHelper.IsFrontLine(a))
                {
                    isFrontLine = true;
                    break;
                }
            }
            profile.IsFrontline = isFrontLine;

            // B. 计算战斗需求
            // OrientationKind.势力 means attacking a faction -> Attack
            // OrientationKind.无 means development -> Develop
            bool isAttack = legion.AIDetail.OrientationKind == SectionOrientationKind.势力;
            bool isDevelop = legion.AIDetail.OrientationKind == SectionOrientationKind.无;

            if (isAttack || profile.IsFrontline)
            {
                profile.CombatNeed = 90.0f; // 极度渴求武将
                profile.PoliticsNeed = 30.0f;
            }
            else if (isDevelop)
            {
                profile.CombatNeed = 20.0f;
                profile.PoliticsNeed = 90.0f; // 极度渴求文官
            }
            else // Balanced/Auto
            {
                profile.CombatNeed = 50.0f;
                profile.PoliticsNeed = 50.0f;
            }

            // C. 根据现有人员数量微调
            int officerCount = 0;
            foreach (Architecture a in legion.Architectures.GetList())
            {
                officerCount += a.Persons.Count;
            }
            int cityCount = legion.Architectures.Count;
            
            // 如果平均每城不足3人，全面渴求
            if (cityCount > 0 && officerCount < cityCount * 3)
            {
                profile.CombatNeed += 20;
                profile.PoliticsNeed += 20;
            }

            return profile;
        }

        /// <summary>
        /// 识别某个军团中“可以被调走”的人
        /// </summary>
        private List<Person> IdentifyTransferableOfficers(Section legion, LegionNeedProfile needs)
        {
            var list = new List<Person>();
            
            // SectionLeader (Viceroy) cannot move
            var viceroy = legion.SectionLeader;

            foreach (Architecture a in legion.Architectures.GetList())
            {
                foreach (Person p in a.Persons)
                {
                    if (p == viceroy) continue;
                    // 排除正在执行任务的人
                    if (p.Status != PersonStatus.Normal) continue;
                    // 排除君主 (Faction Leader)
                    if (p == p.BelongedFaction.Leader) continue;

                    // 核心逻辑：冗余判断
                    // 如果这是个后方种田军团 (CombatNeed低)，但此人是猛将 (Command > 80)，则是冗余资源，应该调走
                    if (needs.CombatNeed < 40 && p.Command > 80)
                    {
                        list.Add(p);
                    }
                    // 如果这是个前线打仗军团 (PoliticsNeed低)，但此人是纯文官 (Politics > 80, Command < 40)，调走保护
                    else if (needs.PoliticsNeed < 40 && p.Politics > 80 && p.Command < 50)
                    {
                        list.Add(p);
                    }
                    // 如果是通用人才，随机抽取一部分作为流动资金
                    else if (p.Command < 70 && p.Politics < 70)
                    {
                        // 20% 概率成为流动人口
                        if (GameObject.Random(100) < 20) list.Add(p);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 为武将寻找最佳归宿
        /// </summary>
        private Section FindBestLegionForPerson(Person p, List<Section> legions, Dictionary<Section, LegionNeedProfile> needs)
        {
            Section bestLegion = null;
            float maxScore = -1;

            foreach (var legion in legions)
            {
                var need = needs[legion];
                float score = 0;

                // 评分公式：能力 * 需求权重
                // 猛将去前线得分高，文官去后方得分高
                float combatScore = (p.Command + p.Strength) * need.CombatNeed * CombatWeight;
                float domesticScore = (p.Politics + p.Intelligence) * need.PoliticsNeed * DomesticWeight;

                score = combatScore + domesticScore;

                // 距离惩罚
                if (p.LocationArchitecture != null)
                {
                    Point center = GetSectionCenter(legion);
                    double dist = Session.Current.Scenario.GetDistance(p.LocationArchitecture.Position, center);
                    score -= (float)dist * 0.5f;
                }

                if (score > maxScore)
                {
                    maxScore = score;
                    bestLegion = legion;
                }
            }

            if (bestLegion != null)
            {
                AIDebugger.Log(DebugChannel.Legion, $"{p.Name} 最佳归属计算: 前往 {bestLegion.Name} 得分 {maxScore:0.0}", p.BelongedFaction, p);
            }

            return bestLegion;
        }

        /// <summary>
        /// 执行具体的调动操作
        /// </summary>
        private void ExecuteTransfer(Person p, Section targetLegion)
        {
            if (p == null || targetLegion == null) return;
            if (targetLegion.Architectures.Count == 0) return;

            // 找一个目标军团中人员最少的城市，或者首府
            // 先转为 List<Architecture>
            var archs = new List<Architecture>();
            foreach(Architecture a in targetLegion.Architectures.GetList())
            {
                archs.Add(a);
            }

            Architecture targetCity = archs
                .OrderBy(a => a.Persons.Count)
                .FirstOrDefault();

            if (targetCity != null)
            {
                // 调用游戏底层移动逻辑
                p.MoveToArchitecture(targetCity);
                
                AIDebugger.Log(DebugChannel.Personnel, 
                    $"执行人事调动: 将 {p.Name}(统{p.Command}/政{p.Politics}) 从 {p.LocationArchitecture?.Name ?? "未知"} 调往 {targetLegion.Name} 的 {targetCity.Name}", 
                    p.BelongedFaction, 
                    p);
            }
        }

        // 辅助方法：判断人是否已经在该军团
        private bool IsPersonInLegion(Person p, Section l)
        {
            if (p.LocationArchitecture == null) return false;
            foreach (Architecture a in l.Architectures.GetList())
            {
                if (a == p.LocationArchitecture) return true;
            }
            return false;
        }
        
        // 辅助方法：获取势力军团列表
        private List<Section> GetFactionLegions(Faction f)
        {
            var list = new List<Section>();
            foreach (Section s in f.Sections.GetList())
            {
                list.Add(s);
            }
            return list;
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

        private int GetTotalDays(GameDate date)
        {
            return date.Year * 360 + date.Month * 30 + date.Day;
        }
    }
}
