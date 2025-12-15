using System;
using System.Collections.Generic;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 威胁度事件系统
    /// 自动监测游戏事件并调整玩家威胁度
    /// </summary>
    public static class ThreatEventSystem
    {
        /// <summary>
        /// 威胁度事件类型及其对应的威胁度变化
        /// </summary>
        private static readonly Dictionary<ThreatEventType, float> ThreatValues = new Dictionary<ThreatEventType, float>
        {
            // 军事行动
            { ThreatEventType.CaptureCity, 15f },
            { ThreatEventType.DestroyFaction, 25f },
            { ThreatEventType.WinMajorBattle, 10f },
            { ThreatEventType.CaptureImportantCity, 30f }, // 洛阳、长安等
            
            // 政治行动
            { ThreatEventType.RecruitTalent, 5f },
            { ThreatEventType.RecruitEnemyGeneral, 12f },
            { ThreatEventType.FormAlliance, -8f }, // 结盟降低威胁
            { ThreatEventType.BreakAlliance, 8f },
            
            // 经济发展
            { ThreatEventType.RapidExpansion, 20f }, // 短期内占领多个城市
            { ThreatEventType.EconomicDominance, 15f }, // 经济实力大幅领先
            
            // 特殊事件
            { ThreatEventType.ExecuteImportantPerson, 18f },
            { ThreatEventType.RefuseEmperorOrder, 22f },
            { ThreatEventType.DeclareIndependence, 35f },
            
            // 威胁度降低事件
            { ThreatEventType.SufferMajorDefeat, -15f },
            { ThreatEventType.LoseImportantCity, -12f },
            { ThreatEventType.InternalRebellion, -10f },
            { ThreatEventType.NaturalDisaster, -8f }
        };

        /// <summary>
        /// 处理城市占领事件
        /// </summary>
        /// <param name="capturedCity">被占领的城市</param>
        /// <param name="capturingFaction">占领方势力</param>
        public static void OnCityCaptured(Architecture capturedCity, Faction capturingFaction)
        {
            try
            {
                if (!IsPlayerFaction(capturingFaction)) return;

                float threatIncrease = ThreatValues[ThreatEventType.CaptureCity];
                
                // 重要城市额外威胁度
                if (IsImportantCity(capturedCity))
                {
                    threatIncrease += ThreatValues[ThreatEventType.CaptureImportantCity] - ThreatValues[ThreatEventType.CaptureCity];
                }

                // 基于城市规模调整
                float sizeMultiplier = CalculateCitySizeMultiplier(capturedCity);
                threatIncrease *= sizeMultiplier;

                CoalitionManager.Instance?.ModifyThreat(threatIncrease, $"占领 {capturedCity.Name}");
                
                Debug.Log($"[威胁事件] 玩家占领 {capturedCity.Name}，威胁度增加 {threatIncrease:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] OnCityCaptured 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理势力灭亡事件
        /// </summary>
        /// <param name="destroyedFaction">被灭亡的势力</param>
        /// <param name="destroyingFaction">灭亡方势力</param>
        public static void OnFactionDestroyed(Faction destroyedFaction, Faction destroyingFaction)
        {
            try
            {
                if (!IsPlayerFaction(destroyingFaction)) return;

                float threatIncrease = ThreatValues[ThreatEventType.DestroyFaction];
                
                // 基于被灭势力的实力调整威胁度
                float powerMultiplier = CalculateFactionPowerMultiplier(destroyedFaction);
                threatIncrease *= powerMultiplier;

                CoalitionManager.Instance?.ModifyThreat(threatIncrease, $"灭亡 {destroyedFaction.Name}");
                
                Debug.Log($"[威胁事件] 玩家灭亡 {destroyedFaction.Name}，威胁度增加 {threatIncrease:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] OnFactionDestroyed 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理重大战斗胜利事件
        /// </summary>
        /// <param name="winnerFaction">胜利方</param>
        /// <param name="loserFaction">失败方</param>
        /// <param name="battleScale">战斗规模</param>
        public static void OnMajorBattleWon(Faction winnerFaction, Faction loserFaction, BattleScale battleScale)
        {
            try
            {
                if (!IsPlayerFaction(winnerFaction)) return;

                float threatIncrease = ThreatValues[ThreatEventType.WinMajorBattle];
                
                // 基于战斗规模调整
                float scaleMultiplier = battleScale switch
                {
                    BattleScale.Small => 0.5f,
                    BattleScale.Medium => 1.0f,
                    BattleScale.Large => 1.5f,
                    BattleScale.Epic => 2.0f,
                    _ => 1.0f
                };
                
                threatIncrease *= scaleMultiplier;

                CoalitionManager.Instance?.ModifyThreat(threatIncrease, $"大胜 {loserFaction.Name}");
                
                Debug.Log($"[威胁事件] 玩家战胜 {loserFaction.Name}，威胁度增加 {threatIncrease:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] OnMajorBattleWon 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理人才招募事件
        /// </summary>
        /// <param name="recruitedPerson">被招募的人才</param>
        /// <param name="recruitingFaction">招募方势力</param>
        public static void OnPersonRecruited(Person recruitedPerson, Faction recruitingFaction)
        {
            try
            {
                if (!IsPlayerFaction(recruitingFaction)) return;

                float threatIncrease = ThreatValues[ThreatEventType.RecruitTalent];
                
                // 如果是从敌对势力招募的重要人才
                if (recruitedPerson.BelongedFaction != null && 
                    recruitedPerson.BelongedFaction != recruitingFaction &&
                    IsImportantPerson(recruitedPerson))
                {
                    threatIncrease = ThreatValues[ThreatEventType.RecruitEnemyGeneral];
                }

                // 基于人才能力调整
                float talentMultiplier = CalculateTalentMultiplier(recruitedPerson);
                threatIncrease *= talentMultiplier;

                CoalitionManager.Instance?.ModifyThreat(threatIncrease, $"招募 {recruitedPerson.Name}");
                
                Debug.Log($"[威胁事件] 玩家招募 {recruitedPerson.Name}，威胁度增加 {threatIncrease:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] OnPersonRecruited 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理结盟事件
        /// </summary>
        /// <param name="faction1">势力1</param>
        /// <param name="faction2">势力2</param>
        public static void OnAllianceFormed(Faction faction1, Faction faction2)
        {
            try
            {
                Faction playerFaction = null;
                Faction otherFaction = null;

                if (IsPlayerFaction(faction1))
                {
                    playerFaction = faction1;
                    otherFaction = faction2;
                }
                else if (IsPlayerFaction(faction2))
                {
                    playerFaction = faction2;
                    otherFaction = faction1;
                }

                if (playerFaction == null) return;

                float threatChange = ThreatValues[ThreatEventType.FormAlliance];
                
                // 基于盟友实力调整威胁度变化
                float allyPowerMultiplier = CalculateFactionPowerMultiplier(otherFaction);
                threatChange *= allyPowerMultiplier;

                CoalitionManager.Instance?.ModifyThreat(threatChange, $"与 {otherFaction.Name} 结盟");
                
                Debug.Log($"[威胁事件] 玩家与 {otherFaction.Name} 结盟，威胁度变化 {threatChange:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] OnAllianceFormed 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理快速扩张事件
        /// </summary>
        /// <param name="faction">扩张的势力</param>
        /// <param name="citiesGained">短期内获得的城市数量</param>
        /// <param name="timeSpan">时间跨度（回合数）</param>
        public static void OnRapidExpansion(Faction faction, int citiesGained, int timeSpan)
        {
            try
            {
                if (!IsPlayerFaction(faction)) return;

                // 只有在短时间内获得大量城市才触发
                if (citiesGained < 3 || timeSpan > 10) return;

                float threatIncrease = ThreatValues[ThreatEventType.RapidExpansion];
                
                // 基于扩张速度调整
                float expansionRate = (float)citiesGained / timeSpan;
                threatIncrease *= expansionRate;

                CoalitionManager.Instance?.ModifyThreat(threatIncrease, $"快速扩张 ({citiesGained}城/{timeSpan}回合)");
                
                Debug.Log($"[威胁事件] 玩家快速扩张，威胁度增加 {threatIncrease:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] OnRapidExpansion 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理重大失败事件
        /// </summary>
        /// <param name="faction">失败的势力</param>
        /// <param name="eventType">失败事件类型</param>
        /// <param name="severity">严重程度</param>
        public static void OnMajorSetback(Faction faction, ThreatEventType eventType, float severity = 1.0f)
        {
            try
            {
                if (!IsPlayerFaction(faction)) return;

                if (!ThreatValues.ContainsKey(eventType)) return;

                float threatDecrease = ThreatValues[eventType] * severity;

                CoalitionManager.Instance?.ModifyThreat(threatDecrease, GetEventDescription(eventType));
                
                Debug.Log($"[威胁事件] 玩家遭受挫折，威胁度变化 {threatDecrease:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] OnMajorSetback 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 定期评估玩家威胁度
        /// </summary>
        public static void EvaluatePeriodicThreat()
        {
            try
            {
                Faction playerFaction = GetPlayerFaction();
                if (playerFaction == null) return;

                // 经济统治力评估
                if (HasEconomicDominance(playerFaction))
                {
                    float threatIncrease = ThreatValues[ThreatEventType.EconomicDominance] * 0.1f; // 每回合小幅增加
                    CoalitionManager.Instance?.ModifyThreat(threatIncrease, "经济统治力");
                }

                // 军事实力评估
                if (HasMilitaryDominance(playerFaction))
                {
                    float threatIncrease = 5f; // 军事统治力威胁
                    CoalitionManager.Instance?.ModifyThreat(threatIncrease, "军事统治力");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[威胁事件] EvaluatePeriodicThreat 失败: {ex.Message}");
            }
        }

        #region 辅助方法

        /// <summary>
        /// 检查是否为玩家势力
        /// </summary>
        private static bool IsPlayerFaction(Faction faction)
        {
            return faction != null && faction.IsPlayer;
        }

        /// <summary>
        /// 获取玩家势力
        /// </summary>
        private static Faction GetPlayerFaction()
        {
            try
            {
                return Session.Current?.Scenario?.Factions?.GetList()
                    ?.FirstOrDefault(f => f != null && f.IsPlayer);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查是否为重要城市
        /// </summary>
        private static bool IsImportantCity(Architecture city)
        {
            if (city?.Name == null) return false;
            
            string[] importantCities = { "洛阳", "长安", "邺", "成都", "建业", "襄阳" };
            return Array.Exists(importantCities, name => city.Name.Contains(name));
        }

        /// <summary>
        /// 检查是否为重要人物
        /// </summary>
        private static bool IsImportantPerson(Person person)
        {
            if (person == null) return false;
            
            // 基于能力值判断
            int totalAbility = person.Intelligence + person.Command + person.Politics + person.Strength;
            return totalAbility >= 320 || // 总能力值高
                   person.Intelligence >= 90 || // 超高智力
                   person.Command >= 90 || // 超高统率
                   person.Reputation >= 80; // 高声望
        }

        /// <summary>
        /// 计算城市规模倍数
        /// </summary>
        private static float CalculateCitySizeMultiplier(Architecture city)
        {
            if (city == null) return 1.0f;
            
            float multiplier = 1.0f;
            
            // 基于人口
            if (city.Population > 100000) multiplier += 0.5f;
            else if (city.Population > 50000) multiplier += 0.3f;
            
            // 基于经济
            if (city.Fund > 10000) multiplier += 0.3f;
            
            return Math.Min(multiplier, 2.0f); // 最大2倍
        }

        /// <summary>
        /// 计算势力实力倍数
        /// </summary>
        private static float CalculateFactionPowerMultiplier(Faction faction)
        {
            if (faction == null) return 1.0f;
            
            float multiplier = 1.0f;
            
            // 基于城市数量
            int cityCount = faction.Architectures?.Count ?? 0;
            if (cityCount > 10) multiplier += 0.5f;
            else if (cityCount > 5) multiplier += 0.3f;
            
            // 基于声望
            if (faction.Reputation > 80) multiplier += 0.3f;
            
            return Math.Min(multiplier, 2.0f); // 最大2倍
        }

        /// <summary>
        /// 计算人才倍数
        /// </summary>
        private static float CalculateTalentMultiplier(Person person)
        {
            if (person == null) return 1.0f;
            
            float multiplier = 1.0f;
            
            // 基于最高能力值
            int maxAbility = Math.Max(Math.Max(person.Intelligence, person.Command), 
                                    Math.Max(person.Politics, person.Strength));
            
            if (maxAbility >= 95) multiplier += 0.8f;
            else if (maxAbility >= 85) multiplier += 0.5f;
            else if (maxAbility >= 75) multiplier += 0.3f;
            
            return Math.Min(multiplier, 2.0f); // 最大2倍
        }

        /// <summary>
        /// 检查是否具有经济统治力
        /// </summary>
        private static bool HasEconomicDominance(Faction playerFaction)
        {
            try
            {
                if (playerFaction?.Architectures == null) return false;

                // 计算玩家总经济实力
                long playerWealth = 0;
                foreach (Architecture arch in playerFaction.Architectures.GetList())
                {
                    if (arch != null)
                    {
                        playerWealth += arch.Fund + arch.Food;
                    }
                }

                // 计算所有其他势力的总经济实力
                long othersWealth = 0;
                var allFactions = Session.Current?.Scenario?.Factions?.GetList();
                if (allFactions != null)
                {
                    foreach (Faction faction in allFactions)
                    {
                        if (faction != null && !faction.IsPlayer && faction.Architectures != null)
                        {
                            foreach (Architecture arch in faction.Architectures.GetList())
                            {
                                if (arch != null)
                                {
                                    othersWealth += arch.Fund + arch.Food;
                                }
                            }
                        }
                    }
                }

                // 如果玩家经济实力超过其他所有势力的50%，认为具有经济统治力
                return othersWealth > 0 && playerWealth > othersWealth * 0.5f;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否具有军事统治力
        /// </summary>
        private static bool HasMilitaryDominance(Faction playerFaction)
        {
            try
            {
                if (playerFaction?.Troops == null) return false;

                // 简化的军事实力计算
                int playerMilitaryPower = 0;
                foreach (Troop troop in playerFaction.Troops.GetList())
                {
                    if (troop != null)
                    {
                        playerMilitaryPower += troop.Quantity;
                    }
                }

                // 计算其他势力的总军事实力
                int othersMilitaryPower = 0;
                var allFactions = Session.Current?.Scenario?.Factions?.GetList();
                if (allFactions != null)
                {
                    foreach (Faction faction in allFactions)
                    {
                        if (faction != null && !faction.IsPlayer && faction.Troops != null)
                        {
                            foreach (Troop troop in faction.Troops.GetList())
                            {
                                if (troop != null)
                                {
                                    othersMilitaryPower += troop.Quantity;
                                }
                            }
                        }
                    }
                }

                // 如果玩家军事实力超过其他所有势力的40%，认为具有军事统治力
                return othersMilitaryPower > 0 && playerMilitaryPower > othersMilitaryPower * 0.4f;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取事件描述
        /// </summary>
        private static string GetEventDescription(ThreatEventType eventType)
        {
            return eventType switch
            {
                ThreatEventType.SufferMajorDefeat => "遭受重大失败",
                ThreatEventType.LoseImportantCity => "失去重要城市",
                ThreatEventType.InternalRebellion => "内部叛乱",
                ThreatEventType.NaturalDisaster => "自然灾害",
                _ => "未知事件"
            };
        }

        #endregion
    }

    /// <summary>
    /// 威胁事件类型
    /// </summary>
    public enum ThreatEventType
    {
        // 正面威胁事件（增加威胁度）
        CaptureCity,
        DestroyFaction,
        WinMajorBattle,
        CaptureImportantCity,
        RecruitTalent,
        RecruitEnemyGeneral,
        BreakAlliance,
        RapidExpansion,
        EconomicDominance,
        ExecuteImportantPerson,
        RefuseEmperorOrder,
        DeclareIndependence,

        // 负面威胁事件（降低威胁度）
        FormAlliance,
        SufferMajorDefeat,
        LoseImportantCity,
        InternalRebellion,
        NaturalDisaster
    }

    /// <summary>
    /// 战斗规模
    /// </summary>
    public enum BattleScale
    {
        Small,   // 小规模战斗
        Medium,  // 中等规模战斗
        Large,   // 大规模战斗
        Epic     // 史诗级战斗
    }
}