using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// 行动执行器系统
    /// 根据战略立场执行具体的AI行动，集成联盟协调和军师预测
    /// </summary>
    public class ActionExecutor
    {
        private readonly Faction _faction;
        private readonly StrategicBrain _strategicBrain;
        private readonly Dictionary<string, float> _actionCooldowns;
        private readonly List<string> _executionLog;

        public ActionExecutor(Faction faction, StrategicBrain strategicBrain)
        {
            _faction = faction ?? throw new ArgumentNullException(nameof(faction));
            _strategicBrain = strategicBrain ?? throw new ArgumentNullException(nameof(strategicBrain));
            _actionCooldowns = new Dictionary<string, float>();
            _executionLog = new List<string>();
        }

        /// <summary>
        /// 执行战略行动
        /// </summary>
        /// <param name="gameContext">游戏上下文</param>
        public void ExecuteStrategicActions(GameContext gameContext)
        {
            try
            {
                StrategicStance currentStance = _strategicBrain.GetCurrentStance();
                
                Debug.Log($"[行动执行] {_faction.Name} 执行 {currentStance} 立场行动");

                // 更新冷却时间
                UpdateCooldowns();

                // 根据立场执行相应行动
                switch (currentStance)
                {
                    case StrategicStance.CoalitionCrusade:
                        ExecuteCoalitionCrusade();
                        break;

                    case StrategicStance.CoalitionSupport:
                        ExecuteCoalitionSupport();
                        break;

                    case StrategicStance.Aggressive:
                        ExecuteAggressiveActions(gameContext);
                        break;

                    case StrategicStance.Defensive:
                        ExecuteDefensiveActions(gameContext);
                        break;

                    case StrategicStance.Consolidation:
                        ExecuteConsolidationActions(gameContext);
                        break;

                    case StrategicStance.Panic:
                        ExecutePanicActions(gameContext);
                        break;

                    case StrategicStance.Cautious:
                        ExecuteCautiousActions(gameContext);
                        break;

                    case StrategicStance.Neutral:
                    default:
                        ExecuteNeutralActions(gameContext);
                        break;
                }

                // 记录执行结果
                LogExecution($"执行 {currentStance} 立场行动完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteStrategicActions 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行联盟十字军行动
        /// </summary>
        private void ExecuteCoalitionCrusade()
        {
            try
            {
                var coalitionManager = CoalitionManager.Instance;
                if (coalitionManager == null || !coalitionManager.IsActive)
                {
                    Debug.LogWarning($"[行动执行] {_faction.Name}: 联盟不活跃，无法执行十字军行动");
                    return;
                }

                // 获取全军集火目标
                Architecture globalTarget = coalitionManager.GetCoalitionTarget();
                if (globalTarget == null)
                {
                    Debug.LogWarning($"[行动执行] {_faction.Name}: 无联盟目标");
                    return;
                }

                Debug.Log($"[行动执行] {_faction.Name} 执行联盟十字军，目标: {globalTarget.Name}");

                // 1. 借道逻辑：无视盟友国境线
                SetupFriendlyPassage();

                // 2. 进攻逻辑
                if (IsBorderingPlayer())
                {
                    // 如果我接壤，直接出兵打玩家
                    LaunchInvasion(globalTarget);
                }
                else
                {
                    // 如果我不接壤，派"远征军"穿过盟友领土去前线
                    DispatchExpeditionForce(globalTarget);
                }

                // 3. 支援行动
                ProvideCoalitionSupport(globalTarget);

                LogExecution($"联盟十字军行动: 目标 {globalTarget.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteCoalitionCrusade 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行联盟支援行动
        /// </summary>
        private void ExecuteCoalitionSupport()
        {
            try
            {
                var coalitionManager = CoalitionManager.Instance;
                if (coalitionManager?.GetCoalitionTarget() == null) return;

                Architecture target = coalitionManager.GetCoalitionTarget();
                
                // 提供间接支援
                ProvideLogisticalSupport(target);
                ProvideIntelligenceSupport(target);
                ProvideEconomicSupport();

                LogExecution($"联盟支援行动: 支援攻打 {target.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteCoalitionSupport 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行主动扩张行动
        /// </summary>
        private void ExecuteAggressiveActions(GameContext gameContext)
        {
            try
            {
                // 1. 寻找最佳攻击目标
                Architecture bestTarget = FindBestAttackTarget();
                if (bestTarget != null)
                {
                    LaunchInvasion(bestTarget);
                }

                // 2. 积极招募人才
                ExecuteAggressiveRecruitment();

                // 3. 扩军备战
                ExecuteMilitaryExpansion();

                LogExecution("主动扩张行动");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteAggressiveActions 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行防御行动
        /// </summary>
        private void ExecuteDefensiveActions(GameContext gameContext)
        {
            try
            {
                // 1. 加强边境防务
                FortifyBorders();

                // 2. 招募防御型人才
                ExecuteDefensiveRecruitment();

                // 3. 寻求外交保护
                SeekDiplomaticProtection();

                LogExecution("防御保守行动");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteDefensiveActions 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行休养生息行动
        /// </summary>
        private void ExecuteConsolidationActions(GameContext gameContext)
        {
            try
            {
                // 1. 发展内政
                FocusOnInternalDevelopment();

                // 2. 恢复军力
                RecoverMilitaryStrength();

                // 3. 改善外交关系
                ImproveDiplomaticRelations();

                LogExecution("休养生息行动");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteConsolidationActions 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行恐慌求生行动
        /// </summary>
        private void ExecutePanicActions(GameContext gameContext)
        {
            try
            {
                // 1. 紧急外交求援
                SeekEmergencyAlliances();

                // 2. 全面防御
                ImplementTotalDefense();

                // 3. 资源集中
                ConcentrateResources();

                LogExecution("恐慌求生行动");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecutePanicActions 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行谨慎观望行动
        /// </summary>
        private void ExecuteCautiousActions(GameContext gameContext)
        {
            try
            {
                // 1. 情报收集
                GatherIntelligence();

                // 2. 小规模试探
                ConductLimitedProbes();

                // 3. 保持灵活性
                MaintainFlexibility();

                LogExecution("谨慎观望行动");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteCautiousActions 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行中性发展行动
        /// </summary>
        private void ExecuteNeutralActions(GameContext gameContext)
        {
            try
            {
                // 1. 平衡发展
                BalancedDevelopment();

                // 2. 机会主义招募
                OpportunisticRecruitment();

                // 3. 维持现状
                MaintainStatus();

                LogExecution("中性发展行动");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteNeutralActions 失败: {ex.Message}");
            }
        }

        #region 联盟行动实现

        /// <summary>
        /// 设置友好通道
        /// </summary>
        private void SetupFriendlyPassage()
        {
            try
            {
                var coalitionManager = CoalitionManager.Instance;
                if (coalitionManager?.Members == null) return;

                // 设置寻路层级，将所有包围网成员的领土视为"友好通过区"
                var friendlyTerritories = new List<Faction>(coalitionManager.Members);
                if (coalitionManager.LeaderFaction != null)
                {
                    friendlyTerritories.Add(coalitionManager.LeaderFaction);
                }

                // 这里需要与寻路系统集成
                // Pathfinding.SetFriendlyArea(friendlyTerritories);
                
                Debug.Log($"[行动执行] {_faction.Name} 设置友好通道，包含 {friendlyTerritories.Count} 个盟友领土");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] SetupFriendlyPassage 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否与玩家接壤
        /// </summary>
        private bool IsBorderingPlayer()
        {
            try
            {
                Faction playerFaction = GetPlayerFaction();
                if (playerFaction?.Architectures == null) return false;

                foreach (Architecture myArch in _faction.Architectures.GetList())
                {
                    if (myArch == null) continue;

                    foreach (Architecture playerArch in playerFaction.Architectures.GetList())
                    {
                        if (playerArch == null) continue;

                        if (AreAdjacent(myArch, playerArch))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] IsBorderingPlayer 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发动入侵
        /// </summary>
        private void LaunchInvasion(Architecture target)
        {
            try
            {
                if (target == null) return;

                // 使用军师预测系统评估入侵成功率
                if (_faction.Advisor != null && target.Mayor != null)
                {
                    int siegeChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(
                        _faction.Advisor, target.Mayor, "Siege");
                    
                    Debug.Log($"[行动执行] {_faction.Name} 军师预测攻打 {target.Name} 成功率: {siegeChance}%");
                    
                    // 如果成功率太低，可能会放弃或寻求支援
                    if (siegeChance < 40)
                    {
                        Debug.Log($"[行动执行] {_faction.Name} 因成功率过低暂缓攻击 {target.Name}");
                        return;
                    }
                }

                // 选择最佳攻击部队
                Troop attackForce = SelectBestAttackForce(target);
                if (attackForce == null)
                {
                    Debug.LogWarning($"[行动执行] {_faction.Name} 无可用攻击部队");
                    return;
                }

                // 执行攻击
                ExecuteAttack(attackForce, target);
                
                LogExecution($"发动入侵: {attackForce.Name} 攻击 {target.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] LaunchInvasion 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 派遣远征军
        /// </summary>
        private void DispatchExpeditionForce(Architecture target)
        {
            try
            {
                if (target == null) return;

                // 组建远征军
                List<Troop> expeditionForce = AssembleExpeditionForce();
                if (expeditionForce.Count == 0)
                {
                    Debug.LogWarning($"[行动执行] {_faction.Name} 无法组建远征军");
                    return;
                }

                // 规划远征路线（穿过盟友领土）
                var route = PlanExpeditionRoute(target);
                
                // 派遣远征军
                foreach (Troop troop in expeditionForce)
                {
                    DispatchTroop(troop, target, route);
                }

                LogExecution($"派遣远征军: {expeditionForce.Count} 支部队前往 {target.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] DispatchExpeditionForce 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 提供联盟支援
        /// </summary>
        private void ProvideCoalitionSupport(Architecture target)
        {
            try
            {
                // 提供后勤支援
                ProvideLogisticalSupport(target);
                
                // 提供情报支援
                ProvideIntelligenceSupport(target);
                
                // 提供经济支援
                ProvideEconomicSupport();

                LogExecution($"提供联盟支援: 支援攻打 {target.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ProvideCoalitionSupport 失败: {ex.Message}");
            }
        }

        #endregion

        #region 具体行动实现

        /// <summary>
        /// 寻找最佳攻击目标
        /// </summary>
        private Architecture FindBestAttackTarget()
        {
            try
            {
                if (_faction.Advisor == null) return null;

                Architecture bestTarget = null;
                float bestScore = 0f;

                var potentialTargets = GetPotentialTargets();
                
                foreach (Architecture target in potentialTargets)
                {
                    if (target?.Mayor == null) continue;

                    // 使用军师预测系统评估
                    int successChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(
                        _faction.Advisor, target.Mayor, "Siege");
                    
                    // 计算目标价值
                    float targetValue = EvaluateTargetValue(target);
                    
                    // 综合评分
                    float score = (successChance * 0.6f) + (targetValue * 0.4f);
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = target;
                    }
                }

                if (bestTarget != null)
                {
                    Debug.Log($"[行动执行] {_faction.Name} 选择攻击目标: {bestTarget.Name} (评分: {bestScore:F1})");
                }

                return bestTarget;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] FindBestAttackTarget 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 执行积极招募
        /// </summary>
        private void ExecuteAggressiveRecruitment()
        {
            try
            {
                if (_faction.Advisor == null) return;

                var recruitTargets = GetRecruitmentTargets();
                
                foreach (Person target in recruitTargets.Take(3)) // 限制每回合招募数量
                {
                    int recruitChance = AdvisorPredictionSystem.GetRecruitChanceDisplay(_faction.Advisor, target);
                    
                    if (recruitChance >= 60) // 成功率阈值
                    {
                        AttemptRecruitment(target);
                        LogExecution($"尝试招募: {target.Name} (成功率 {recruitChance}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteAggressiveRecruitment 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行军事扩张
        /// </summary>
        private void ExecuteMilitaryExpansion()
        {
            try
            {
                // 增加军队数量
                RecruitMoreTroops();
                
                // 提升军队质量
                UpgradeTroopEquipment();
                
                // 训练军队
                TrainTroops();

                LogExecution("军事扩张");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] ExecuteMilitaryExpansion 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加强边境防务
        /// </summary>
        private void FortifyBorders()
        {
            try
            {
                var borderCities = GetBorderCities();
                
                foreach (Architecture city in borderCities)
                {
                    // 增加驻军
                    ReinforceCityDefense(city);
                    
                    // 修建防御设施
                    BuildDefensiveStructures(city);
                }

                LogExecution($"加强边境防务: {borderCities.Count} 个边境城市");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] FortifyBorders 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 寻求外交保护
        /// </summary>
        private void SeekDiplomaticProtection()
        {
            try
            {
                if (_faction.Advisor == null) return;

                var potentialAllies = GetPotentialAllies();
                
                foreach (Faction ally in potentialAllies.Take(2))
                {
                    int diplomacyChance = AdvisorPredictionSystem.GetDiplomacyChanceDisplay(
                        _faction.Advisor, ally, "Alliance");
                    
                    if (diplomacyChance >= 50)
                    {
                        AttemptDiplomacy(ally, "Alliance");
                        LogExecution($"寻求结盟: {ally.Name} (成功率 {diplomacyChance}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] SeekDiplomaticProtection 失败: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新冷却时间
        /// </summary>
        private void UpdateCooldowns()
        {
            var keys = _actionCooldowns.Keys.ToList();
            foreach (string key in keys)
            {
                _actionCooldowns[key] = Math.Max(0f, _actionCooldowns[key] - 1f);
            }
        }

        /// <summary>
        /// 检查行动是否在冷却中
        /// </summary>
        private bool IsOnCooldown(string actionType)
        {
            return _actionCooldowns.GetValueOrDefault(actionType, 0f) > 0f;
        }

        /// <summary>
        /// 设置行动冷却
        /// </summary>
        private void SetCooldown(string actionType, float cooldownTurns)
        {
            _actionCooldowns[actionType] = cooldownTurns;
        }

        /// <summary>
        /// 记录执行日志
        /// </summary>
        private void LogExecution(string action)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {_faction.Name}: {action}";
            _executionLog.Add(logEntry);
            
            // 限制日志长度
            if (_executionLog.Count > 50)
            {
                _executionLog.RemoveAt(0);
            }
            
            Debug.Log($"[行动执行] {logEntry}");
        }

        /// <summary>
        /// 获取玩家势力
        /// </summary>
        private Faction GetPlayerFaction()
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
        /// 检查两个建筑是否相邻
        /// </summary>
        private bool AreAdjacent(Architecture arch1, Architecture arch2)
        {
            // 简化实现，实际需要基于地图邻接关系
            return true;
        }

        /// <summary>
        /// 选择最佳攻击部队
        /// </summary>
        private Troop SelectBestAttackForce(Architecture target)
        {
            try
            {
                return _faction.Troops?.GetList()?.FirstOrDefault(t => t != null && t.Quantity > 0);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        private void ExecuteAttack(Troop attackForce, Architecture target)
        {
            // 这里需要与实际的战斗系统集成
            Debug.Log($"[行动执行] {attackForce.Name} 攻击 {target.Name}");
        }

        /// <summary>
        /// 获取潜在目标
        /// </summary>
        private List<Architecture> GetPotentialTargets()
        {
            var targets = new List<Architecture>();
            
            try
            {
                var allFactions = Session.Current?.Scenario?.Factions?.GetList();
                if (allFactions == null) return targets;

                foreach (Faction faction in allFactions)
                {
                    if (faction == null || faction == _faction) continue;
                    
                    foreach (Architecture arch in faction.Architectures.GetList())
                    {
                        if (arch != null)
                        {
                            targets.Add(arch);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] GetPotentialTargets 失败: {ex.Message}");
            }

            return targets;
        }

        /// <summary>
        /// 评估目标价值
        /// </summary>
        private float EvaluateTargetValue(Architecture target)
        {
            try
            {
                float value = 0f;
                
                value += target.Population * 0.001f;
                value += target.Fund * 0.0001f;
                value += target.Food * 0.0001f;
                
                // 战略位置加成
                if (target.Name.Contains("洛阳") || target.Name.Contains("长安"))
                {
                    value += 50f;
                }
                
                return value;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// 获取招募目标
        /// </summary>
        private List<Person> GetRecruitmentTargets()
        {
            var targets = new List<Person>();
            
            try
            {
                var allPersons = Session.Current?.Scenario?.Persons?.GetList();
                if (allPersons == null) return targets;

                foreach (Person person in allPersons)
                {
                    if (person != null && person.BelongedFaction != _faction && person.Alive)
                    {
                        targets.Add(person);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] GetRecruitmentTargets 失败: {ex.Message}");
            }

            return targets;
        }

        /// <summary>
        /// 尝试招募
        /// </summary>
        private void AttemptRecruitment(Person target)
        {
            // 这里需要与实际的招募系统集成
            Debug.Log($"[行动执行] 尝试招募 {target.Name}");
        }

        /// <summary>
        /// 获取边境城市
        /// </summary>
        private List<Architecture> GetBorderCities()
        {
            // 简化实现，返回所有城市
            return _faction.Architectures?.GetList()?.ToList() ?? new List<Architecture>();
        }

        /// <summary>
        /// 获取潜在盟友
        /// </summary>
        private List<Faction> GetPotentialAllies()
        {
            var allies = new List<Faction>();
            
            try
            {
                var allFactions = Session.Current?.Scenario?.Factions?.GetList();
                if (allFactions == null) return allies;

                foreach (Faction faction in allFactions)
                {
                    if (faction != null && faction != _faction && !faction.IsPlayer)
                    {
                        allies.Add(faction);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[行动执行] GetPotentialAllies 失败: {ex.Message}");
            }

            return allies;
        }

        /// <summary>
        /// 尝试外交
        /// </summary>
        private void AttemptDiplomacy(Faction target, string diplomacyType)
        {
            // 这里需要与实际的外交系统集成
            Debug.Log($"[行动执行] 与 {target.Name} 进行 {diplomacyType} 外交");
        }

        // 其他简化的辅助方法
        private List<Troop> AssembleExpeditionForce() => new List<Troop>();
        private List<Architecture> PlanExpeditionRoute(Architecture target) => new List<Architecture>();
        private void DispatchTroop(Troop troop, Architecture target, List<Architecture> route) { }
        private void ProvideLogisticalSupport(Architecture target) { }
        private void ProvideIntelligenceSupport(Architecture target) { }
        private void ProvideEconomicSupport() { }
        private void RecruitMoreTroops() { }
        private void UpgradeTroopEquipment() { }
        private void TrainTroops() { }
        private void ReinforceCityDefense(Architecture city) { }
        private void BuildDefensiveStructures(Architecture city) { }
        private void SeekEmergencyAlliances() { }
        private void ImplementTotalDefense() { }
        private void ConcentrateResources() { }
        private void GatherIntelligence() { }
        private void ConductLimitedProbes() { }
        private void MaintainFlexibility() { }
        private void BalancedDevelopment() { }
        private void OpportunisticRecruitment() { }
        private void MaintainStatus() { }
        private void FocusOnInternalDevelopment() { }
        private void RecoverMilitaryStrength() { }
        private void ImproveDiplomaticRelations() { }
        private void ExecuteDefensiveRecruitment() { }

        #endregion

        /// <summary>
        /// 获取执行日志
        /// </summary>
        public List<string> GetExecutionLog()
        {
            return new List<string>(_executionLog);
        }

        /// <summary>
        /// 清理执行日志
        /// </summary>
        public void ClearExecutionLog()
        {
            _executionLog.Clear();
        }
    }
}