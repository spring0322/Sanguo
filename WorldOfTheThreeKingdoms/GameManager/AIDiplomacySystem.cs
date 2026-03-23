using System;
using System.Collections.Generic;
using System.Linq;
using global::GameGlobal;
using global::GameManager;
using GameObjects;
using GameObjects.FactionDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI外交系统 - 智能外交决策和关系管理
    /// </summary>
    public class AIDiplomacySystem
    {
        public static AIDiplomacySystem Instance { get; private set; }

        public enum AIDiplomaticState
        {
            Hostile = -3,       // 敌对 (交战中)
            Unfriendly = -2,    // 不友好 (冷战/甚至断交)
            Neutral = 0,        // 中立
            Friendly = 1,       // 友好
            Allied = 2,         // 同盟
            Vassal = 3          // 附庸
        }

        public enum DiplomaticAction
        {
            DeclareWar,         // 宣战
            OfferPeace,         // 求和
            ProposeAlliance,    // 提议同盟
            BreakAlliance,      // 破坏同盟
            SendGift,           // 赠送礼物 (亲善)
            Threaten            // 威胁
        }

        public class DiplomaticRelationship
        {
            public int FactionA;
            public int FactionB;
            public AIDiplomaticState State;
            public float TrustLevel = 50.0f;           // 信任度 (0-100)
            public float FearLevel = 0.0f;             // 恐惧度 (0-100, B对A的恐惧)
            public float RespectLevel = 50.0f;         // 尊重度 (0-100)
            public int LastActionYear = 0;             // 上次外交行动年份
            public int WarStartYear = -1;              // 战争开始年份
        }

        public class DiplomaticStrategy
        {
            public string Name;
            // 性格系数 (0.0 - 2.0)
            public float Aggression = 1.0f;    // 侵略性
            public float Expansionism = 1.0f;  // 扩张欲
            public float Treachery = 1.0f;     // 背信弃义程度 (越高越容易背刺)
        }

        private Dictionary<string, DiplomaticRelationship> _relationships;
        private Dictionary<int, DiplomaticStrategy> _factionStrategies;
        private Random _rng = new Random();
        private DateTime _lastUpdate;
        private const int UpdateInterval = 5000; // 5秒检查一次

        public AIDiplomacySystem()
        {
            Instance = this;
            _relationships = new Dictionary<string, DiplomaticRelationship>();
            _factionStrategies = new Dictionary<int, DiplomaticStrategy>();
            _lastUpdate = DateTime.Now;
        }

        /// <summary>
        /// 初始化外交系统
        /// </summary>
        public void InitializeDiplomacy(List<Faction> factions)
        {
            _relationships.Clear();
            _factionStrategies.Clear();

            foreach (var f in factions)
            {
                // 初始化策略性格
                _factionStrategies[f.ID] = DetermineStrategy(f);

                foreach (var target in factions)
                {
                    if (f == target) continue;
                    string key = GetRelationKey(f.ID, target.ID);
                    if (!_relationships.ContainsKey(key))
                    {
                        var dr = new DiplomaticRelationship
                        {
                            FactionA = Math.Min(f.ID, target.ID),
                            FactionB = Math.Max(f.ID, target.ID),
                            State = AIDiplomaticState.Neutral,
                            TrustLevel = 50,
                            FearLevel = 0
                        };

                        // 从现有游戏数据同步初始化值
                        if (Session.Current.Scenario.DiplomaticRelations != null)
                        {
                            var existingRel = Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(f.ID, target.ID);
                            if (existingRel != null)
                            {
                                dr.TrustLevel = MathHelper.Clamp(existingRel.Relation, 0, 100);
                                // 简单映射
                                if (existingRel.Relation < 20) dr.State = AIDiplomaticState.Unfriendly;
                                if (existingRel.Relation <= 0) dr.State = AIDiplomaticState.Hostile;
                                if (existingRel.Relation >= 80) dr.State = AIDiplomaticState.Friendly;
                                // Alliance check? (Assuming Relation >= 100 or Truce means peace)
                            }
                        }

                        _relationships.Add(key, dr);
                    }
                }
            }
        }

        /// <summary>
        /// 核心循环：更新外交状态并执行决策
        /// </summary>
        public void UpdateDiplomacy()
        {
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds < UpdateInterval) return;
            _lastUpdate = DateTime.Now;

            if (Session.Current?.Scenario?.Factions == null) return;

            var factionsList = Session.Current.Scenario.Factions.GetList();
            var factions = factionsList.Cast<Faction>().ToList();

            if (_relationships.Count == 0)
            {
                InitializeDiplomacy(factions);
            }

            // 1. 更新所有关系的自然演变
            foreach (var rel in _relationships.Values)
            {
                UpdateRelationshipDynamics(rel, factions);
            }

            // 2. 每个AI势力进行决策
            foreach (var faction in factions)
            {
                // 只有电脑控制的势力才进行AI决策
                // if (!faction.IsPlayer) // Assuming IsPlayer property or check Session.Current.Scenario.IsPlayer(faction)
                if (!Session.Current.Scenario.IsPlayer(faction))
                {
                    ExecuteDiplomaticDecisions(faction, factions);
                }
            }
        }

        /// <summary>
        /// 更新关系的动态变化
        /// </summary>
        private void UpdateRelationshipDynamics(DiplomaticRelationship rel, List<Faction> factions)
        {
            var fA = factions.FirstOrDefault(f => f.ID == rel.FactionA);
            var fB = factions.FirstOrDefault(f => f.ID == rel.FactionB);
            if (fA == null || fB == null) return;

            float strengthA = CalculateFactionStrength(fA);
            float strengthB = CalculateFactionStrength(fB);

            if (strengthA > strengthB * 1.5f)
            {
                rel.FearLevel = Math.Min(100, rel.FearLevel + 0.5f);
            }
            else if (strengthB > strengthA * 1.5f)
            {
                rel.FearLevel = Math.Max(0, rel.FearLevel - 0.5f);
            }

            if (rel.State == AIDiplomaticState.Hostile)
            {
                rel.TrustLevel = Math.Max(0, rel.TrustLevel - 1.0f);
            }
            else if (rel.State == AIDiplomaticState.Allied)
            {
                rel.TrustLevel = Math.Min(100, rel.TrustLevel + 0.2f);
            }

            // Sync back to Game Object
            SyncToGameRelation(fA, fB, rel);
        }

        private void SyncToGameRelation(Faction fA, Faction fB, DiplomaticRelationship rel)
        {
            if (Session.Current.Scenario.DiplomaticRelations == null) return;
            var gameRel = Session.Current.Scenario.DiplomaticRelations.GetDiplomaticRelation(fA.ID, fB.ID);
            if (gameRel != null)
            {
                gameRel.Relation = (int)rel.TrustLevel;
                // If War, maybe set Relation to 0 or lower.
                // If Alliance, set to strict value?
                // For now, TrustLevel reflects Relation.
            }
        }

        /// <summary>
        /// 执行外交决策
        /// </summary>
        private void ExecuteDiplomaticDecisions(Faction aiFaction, List<Faction> allFactions)
        {
            // LastDiplomacyYear is not standard property. We might need to store it in strategy or unused field.
            // Or assume aiFaction has it (User code implies it). 
            // If doesn't exist, we skip checking it or use a Dictionary in this class.
            // Using internal dictionary for LastDiplomacyYear since Faction might not have it.
            // Wait, User code: "aiFaction.LastDiplomacyYear". 
            // If Faction.cs doesn't have it, I must handle it.
            // I'll check Faction.cs later or add extension/dictionary.

            var strategy = _factionStrategies.ContainsKey(aiFaction.ID)
                ? _factionStrategies[aiFaction.ID]
                : new DiplomaticStrategy();

            int currentYear = Session.Current.Scenario.Date.Year;

            // Check cooldown via local dictionary since Faction property might not exist
            if (_lastDiplomacyActionYear.ContainsKey(aiFaction.ID) && _lastDiplomacyActionYear[aiFaction.ID] >= currentYear)
                return;

            foreach (var target in allFactions)
            {
                if (target == aiFaction) continue;
                // Skipping Known check for simplicity as requested "Combine with existing"

                var relData = GetRelationship(aiFaction.ID, target.ID);
                if (relData == null) continue;

                EvaluateAndAct(aiFaction, target, relData, strategy);
            }
        }

        private Dictionary<int, int> _lastDiplomacyActionYear = new Dictionary<int, int>();

        private void EvaluateAndAct(Faction me, Faction target, DiplomaticRelationship rel, DiplomaticStrategy strategy)
        {
            float myStrength = CalculateFactionStrength(me);
            float targetStrength = CalculateFactionStrength(target);
            float strengthRatio = (targetStrength > 0) ? myStrength / targetStrength : 2.0f;

            float aggressionScore = strategy.Aggression * 30;
            float strengthScore = strengthRatio * 40;
            float trustPenalty = -rel.TrustLevel;
            float fearPenalty = -(rel.FearLevel * 0.5f);

            float warScore = aggressionScore + strengthScore + trustPenalty + fearPenalty;

            if (warScore > 50)
            {
                var scores = new Dictionary<string, float>
                    {
                        { "角色侵略性", aggressionScore },
                        { "实力对比", strengthScore },
                        { "信任惩罚", trustPenalty },
                        { "恐惧惩罚", fearPenalty }
                    };
                AIDebugger.LogDecision(DebugChannel.Diplomacy, $"对 {target.Name} 的宣战评估", me.Name, scores, warScore, 80f);
            }

            if (warScore > 80)
            {
                AIDebugger.Log(DebugChannel.Diplomacy, $"决定对 {target.Name} 发起战争！", me);
                PerformAction(me, target, DiplomaticAction.DeclareWar, rel);
                return;
            }

            // 2. Alliance
            if (rel.State == AIDiplomaticState.Friendly || rel.State == AIDiplomaticState.Neutral)
            {
                float allianceScore = rel.TrustLevel + rel.RespectLevel;
                if (strengthRatio < 0.8f) allianceScore += 20;

                if (allianceScore > 120 && strategy.Treachery < 1.5f)
                {
                    PerformAction(me, target, DiplomaticAction.ProposeAlliance, rel);
                    return;
                }
            }

            // 3. Peace
            if (rel.State == AIDiplomaticState.Hostile)
            {
                if (strengthRatio < 0.3f || rel.FearLevel > 80)
                {
                    PerformAction(me, target, DiplomaticAction.OfferPeace, rel);
                    return;
                }
            }

            // 4. Gift
            if (rel.State == AIDiplomaticState.Neutral || rel.State == AIDiplomaticState.Unfriendly)
            {
                if (me.Architectures.Count > 0 && GetTotalFund(me) > 5000 && strategy.Expansionism < 1.2f)
                {
                    PerformAction(me, target, DiplomaticAction.SendGift, rel);
                }
            }
        }

        private void PerformAction(Faction actor, Faction target, DiplomaticAction action, DiplomaticRelationship rel)
        {
            _lastDiplomacyActionYear[actor.ID] = Session.Current.Scenario.Date.Year;
            bool success = false;
            string message = "";

            switch (action)
            {
                case DiplomaticAction.DeclareWar:
                    rel.State = AIDiplomaticState.Hostile;
                    rel.TrustLevel = 0;
                    rel.WarStartYear = Session.Current.Scenario.Date.Year;
                    success = true;
                    message = $"{actor.Name} 对 {target.Name} 宣战了！";
                    break;

                case DiplomaticAction.OfferPeace:
                    if (_rng.Next(100) > 40)
                    {
                        rel.State = AIDiplomaticState.Neutral;
                        rel.WarStartYear = -1;
                        success = true;
                        message = $"{target.Name} 接受了 {actor.Name} 的停战请求。";
                    }
                    else
                    {
                        message = $"{target.Name} 拒绝了 {actor.Name} 的停战请求！";
                    }
                    break;

                case DiplomaticAction.ProposeAlliance:
                    if (rel.TrustLevel > 60 && _rng.Next(100) > 30)
                    {
                        rel.State = AIDiplomaticState.Allied;
                        success = true;
                        message = $"{actor.Name} 与 {target.Name} 结为同盟！";
                    }
                    else
                    {
                        message = $"{actor.Name} 向 {target.Name} 提议结盟，但被婉拒了。";
                    }
                    break;

                case DiplomaticAction.SendGift:
                    rel.TrustLevel += 10;
                    rel.RespectLevel += 5;
                    // Deduct Fund? 
                    if (actor.Architectures.Count > 0)
                    {
                        // Deduct from capital
                        if (actor.Capital != null) actor.Capital.Fund -= 1000;
                    }
                    success = true;
                    message = $"{actor.Name} 向 {target.Name} 赠送了厚礼，关系改善了。";
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                System.Diagnostics.Debug.WriteLine($"[外交事件] {message}");
            }
            // Sync logic
            SyncToGameRelation(actor, target, rel);
        }

        private DiplomaticStrategy DetermineStrategy(Faction faction)
        {
            var strategy = new DiplomaticStrategy();
            if (faction.Leader == null) return strategy;

            strategy.Aggression = 0.5f + (faction.Leader.Braveness / 10.0f);
            strategy.Expansionism = 0.5f + (faction.Leader.Ambition / 10.0f);
            strategy.Treachery = 2.0f - (faction.Leader.PersonalLoyalty / 10.0f);

            strategy.Name = strategy.Aggression > 1.2f ? "霸权主义" : "王道乐土";
            return strategy;
        }

        private float CalculateFactionStrength(Faction faction)
        {
            if (faction == null) return 0;
            float strength = 0;
            // Troops might be TroopListWithQueue
            foreach (Troop t in faction.Troops.GetList())
            {
                strength += t.FightingForce / 100.0f;
            }
            strength += faction.Architectures.Count * 50.0f;

            // Persons count. Iterate architectures?
            int personCount = 0;
            foreach (Architecture a in faction.Architectures.GetList())
            {
                personCount += a.Persons.Count;
            }
            strength += personCount * 10.0f;
            return strength;
        }

        private string GetRelationKey(int id1, int id2)
        {
            return id1 < id2 ? $"{id1}_{id2}" : $"{id2}_{id1}";
        }

        public DiplomaticRelationship GetRelationship(int id1, int id2)
        {
            string key = GetRelationKey(id1, id2);
            return _relationships.ContainsKey(key) ? _relationships[key] : null;
        }

        private int GetTotalFund(Faction f)
        {
            int total = 0;
            foreach (Architecture a in f.Architectures.GetList())
            {
                total += a.Fund;
            }
            return total;
        }
    }
}