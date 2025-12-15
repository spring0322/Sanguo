using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// 战术AI系统
    /// 基于部队角色分配和军师预测，制定智能的战术决策
    /// </summary>
    public class TacticalAI
    {
        private readonly Faction _faction;
        private readonly Dictionary<Troop, TroopRole> _troopRoles;
        private readonly Dictionary<string, TacticalMemory> _tacticalMemory;
        private TacticalFormation _currentFormation;

        public TacticalAI(Faction faction)
        {
            _faction = faction ?? throw new ArgumentNullException(nameof(faction));
            _troopRoles = new Dictionary<Troop, TroopRole>();
            _tacticalMemory = new Dictionary<string, TacticalMemory>();
            _currentFormation = new TacticalFormation();
            
            AnalyzeTroopRoles();
        }

        /// <summary>
        /// 分析部队角色
        /// </summary>
        private void AnalyzeTroopRoles()
        {
            try
            {
                _troopRoles.Clear();
                var roleAnalysis = AIRoleSelector.AnalyzeFactionTroops(_faction);
                
                foreach (var kvp in roleAnalysis)
                {
                    _troopRoles[kvp.Key] = kvp.Value;
                }

                System.Diagnostics.Debug.WriteLine($"[战术AI] {_faction.Name} 完成部队角色分析，共 {_troopRoles.Count} 支部队");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] AnalyzeTroopRoles 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 制定联盟作战计划
        /// </summary>
        /// <param name="target">攻击目标</param>
        /// <param name="coalitionAllies">联盟盟友</param>
        /// <returns>作战计划</returns>
        public CoalitionBattlePlan CreateCoalitionBattlePlan(Architecture target, List<Faction> coalitionAllies)
        {
            try
            {
                var battlePlan = new CoalitionBattlePlan
                {
                    Target = target,
                    PrimaryAttacker = _faction,
                    Allies = coalitionAllies,
                    CreatedTime = DateTime.Now
                };

                // 1. 评估目标防御力
                float targetDefense = EvaluateTargetDefense(target);
                battlePlan.TargetDefenseRating = targetDefense;

                // 2. 分析己方战力
                var myForces = AnalyzeOwnForces();
                battlePlan.OwnForces = myForces;

                // 3. 评估盟友支援
                var allySupport = EvaluateAllySupport(coalitionAllies, target);
                battlePlan.AllySupport = allySupport;

                // 4. 使用军师预测系统评估胜率
                if (_faction.Advisor != null && target.Mayor != null)
                {
                    int siegeChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(
                        _faction.Advisor, target.Mayor, "Siege");
                    battlePlan.PredictedSuccessRate = siegeChance;
                    
                    System.Diagnostics.Debug.WriteLine($"[战术AI] {_faction.Name} 军师预测攻打 {target.Name} 成功率: {siegeChance}%");
                }

                // 5. 制定具体战术
                battlePlan.TacticalPlan = CreateTacticalPlan(target, myForces, allySupport);

                // 6. 分配部队角色
                battlePlan.RoleAssignments = AssignBattleRoles(battlePlan.TacticalPlan);

                System.Diagnostics.Debug.WriteLine($"[战术AI] {_faction.Name} 完成联盟作战计划: 目标{target.Name}, 预测成功率{battlePlan.PredictedSuccessRate}%");

                return battlePlan;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] CreateCoalitionBattlePlan 失败: {ex.Message}");
                return new CoalitionBattlePlan { Target = target, PrimaryAttacker = _faction };
            }
        }

        /// <summary>
        /// 执行联盟协同攻击
        /// </summary>
        /// <param name="battlePlan">作战计划</param>
        /// <returns>是否成功发起攻击</returns>
        public bool ExecuteCoalitionAttack(CoalitionBattlePlan battlePlan)
        {
            try
            {
                if (battlePlan?.Target == null) return false;

                System.Diagnostics.Debug.WriteLine($"[战术AI] {_faction.Name} 开始执行联盟攻击: {battlePlan.Target.Name}");

                // 1. 按战术计划部署部队
                bool deploymentSuccess = DeployTroopsForBattle(battlePlan);
                if (!deploymentSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"[战术AI] {_faction.Name} 部队部署失败");
                    return false;
                }

                // 2. 协调盟友行动
                CoordinateAllyActions(battlePlan);

                // 3. 执行分阶段攻击
                ExecutePhaseAttack(battlePlan, BattlePhase.Opening);
                ExecutePhaseAttack(battlePlan, BattlePhase.Engagement);
                ExecutePhaseAttack(battlePlan, BattlePhase.Cleanup);

                // 4. 记录战术经验
                RecordTacticalExperience(battlePlan);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] ExecuteCoalitionAttack 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 评估目标防御力
        /// </summary>
        private float EvaluateTargetDefense(Architecture target)
        {
            try
            {
                float defense = 0f;

                // 基础防御
                defense += target.Endurance * 0.1f;
                defense += target.Population * 0.0001f;

                // 守军实力
                if (target.Persons != null)
                {
                    foreach (Person person in target.Persons.GetList())
                    {
                        if (person != null)
                        {
                            defense += person.Command * 0.5f;
                            defense += person.Strength * 0.3f;
                        }
                    }
                }

                // 太守能力
                if (target.Mayor != null)
                {
                    defense += target.Mayor.Command * 1.0f;
                    defense += target.Mayor.Intelligence * 0.5f;
                }

                // 地理位置（简化）
                if (target.Name.Contains("关") || target.Name.Contains("险"))
                {
                    defense += 50f; // 关隘加成
                }

                return Math.Max(defense, 10f);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] EvaluateTargetDefense 失败: {ex.Message}");
                return 50f;
            }
        }

        /// <summary>
        /// 分析己方战力
        /// </summary>
        private BattleForceAnalysis AnalyzeOwnForces()
        {
            try
            {
                var analysis = new BattleForceAnalysis();

                foreach (var kvp in _troopRoles)
                {
                    Troop troop = kvp.Key;
                    TroopRole role = kvp.Value;

                    if (troop == null || troop.Quantity <= 0) continue;

                    var troopInfo = new TroopBattleInfo
                    {
                        Troop = troop,
                        Role = role,
                        CombatPower = CalculateTroopCombatPower(troop),
                        Mobility = CalculateTroopMobility(troop),
                        Survivability = CalculateTroopSurvivability(troop)
                    };

                    analysis.TroopInfos.Add(troopInfo);

                    // 按角色分类统计
                    switch (role)
                    {
                        case TroopRole.Tank:
                            analysis.TankCount++;
                            analysis.TotalDefensePower += troopInfo.Survivability;
                            break;
                        case TroopRole.DPS:
                            analysis.DPSCount++;
                            analysis.TotalAttackPower += troopInfo.CombatPower;
                            break;
                        case TroopRole.Mage:
                            analysis.MageCount++;
                            analysis.TotalUtilityPower += troopInfo.CombatPower * 0.8f;
                            break;
                        case TroopRole.Support:
                            analysis.SupportCount++;
                            analysis.TotalUtilityPower += troopInfo.CombatPower * 1.2f;
                            break;
                        case TroopRole.Balanced:
                            analysis.BalancedCount++;
                            analysis.TotalAttackPower += troopInfo.CombatPower * 0.7f;
                            analysis.TotalDefensePower += troopInfo.Survivability * 0.7f;
                            break;
                    }
                }

                analysis.TotalTroops = analysis.TroopInfos.Count;
                analysis.OverallRating = (analysis.TotalAttackPower + analysis.TotalDefensePower + analysis.TotalUtilityPower) / 3f;

                System.Diagnostics.Debug.WriteLine($"[战术AI] {_faction.Name} 战力分析: 总评{analysis.OverallRating:F1}, Tank{analysis.TankCount}, DPS{analysis.DPSCount}, Mage{analysis.MageCount}, Support{analysis.SupportCount}");

                return analysis;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] AnalyzeOwnForces 失败: {ex.Message}");
                return new BattleForceAnalysis();
            }
        }

        /// <summary>
        /// 评估盟友支援
        /// </summary>
        private AllySupportAnalysis EvaluateAllySupport(List<Faction> allies, Architecture target)
        {
            try
            {
                var analysis = new AllySupportAnalysis();

                foreach (Faction ally in allies)
                {
                    if (ally == null || ally.Destroyed) continue;

                    var allyInfo = new AllyInfo
                    {
                        Faction = ally,
                        Distance = CalculateDistanceToTarget(ally, target),
                        MilitaryStrength = CalculateFactionMilitaryStrength(ally),
                        Reliability = CalculateAllyReliability(ally)
                    };

                    // 评估支援能力
                    if (allyInfo.Distance < 100f) // 距离足够近
                    {
                        allyInfo.SupportCapability = allyInfo.MilitaryStrength * allyInfo.Reliability * (100f - allyInfo.Distance) / 100f;
                        analysis.EffectiveAllies.Add(allyInfo);
                    }
                    else
                    {
                        allyInfo.SupportCapability = 0f;
                        analysis.DistantAllies.Add(allyInfo);
                    }

                    analysis.TotalSupportPower += allyInfo.SupportCapability;
                }

                System.Diagnostics.Debug.WriteLine($"[战术AI] 盟友支援分析: 有效盟友{analysis.EffectiveAllies.Count}, 总支援力{analysis.TotalSupportPower:F1}");

                return analysis;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] EvaluateAllySupport 失败: {ex.Message}");
                return new AllySupportAnalysis();
            }
        }

        /// <summary>
        /// 制定战术计划
        /// </summary>
        private TacticalPlan CreateTacticalPlan(Architecture target, BattleForceAnalysis ownForces, AllySupportAnalysis allySupport)
        {
            try
            {
                var plan = new TacticalPlan
                {
                    Target = target,
                    Strategy = DetermineOptimalStrategy(ownForces, allySupport),
                    Formation = AIRoleSelector.RecommendFormation(_troopRoles),
                    PhaseActions = new Dictionary<BattlePhase, List<TacticalAction>>()
                };

                // 为每个阶段制定行动
                plan.PhaseActions[BattlePhase.Opening] = CreateOpeningActions(plan.Formation, ownForces);
                plan.PhaseActions[BattlePhase.Engagement] = CreateEngagementActions(plan.Formation, ownForces);
                plan.PhaseActions[BattlePhase.Cleanup] = CreateCleanupActions(plan.Formation, ownForces);

                System.Diagnostics.Debug.WriteLine($"[战术AI] 制定战术计划: 策略={plan.Strategy}, 阵型平衡度={plan.Formation.GetBalanceScore():F1}%");

                return plan;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] CreateTacticalPlan 失败: {ex.Message}");
                return new TacticalPlan { Target = target, Strategy = BattleStrategy.Balanced };
            }
        }

        /// <summary>
        /// 确定最优策略
        /// </summary>
        private BattleStrategy DetermineOptimalStrategy(BattleForceAnalysis ownForces, AllySupportAnalysis allySupport)
        {
            try
            {
                // 基于部队构成决定策略
                float tankRatio = (float)ownForces.TankCount / Math.Max(ownForces.TotalTroops, 1);
                float dpsRatio = (float)ownForces.DPSCount / Math.Max(ownForces.TotalTroops, 1);
                float mageRatio = (float)ownForces.MageCount / Math.Max(ownForces.TotalTroops, 1);
                float supportRatio = (float)ownForces.SupportCount / Math.Max(ownForces.TotalTroops, 1);

                // 如果有强力盟友支援，倾向于协同作战
                if (allySupport.TotalSupportPower > ownForces.OverallRating * 0.5f)
                {
                    return BattleStrategy.Coalition;
                }

                // 根据部队构成选择策略
                if (tankRatio > 0.4f) return BattleStrategy.Defensive;
                if (dpsRatio > 0.4f) return BattleStrategy.Aggressive;
                if (mageRatio > 0.3f) return BattleStrategy.Control;
                if (supportRatio > 0.2f) return BattleStrategy.Attrition;

                return BattleStrategy.Balanced;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] DetermineOptimalStrategy 失败: {ex.Message}");
                return BattleStrategy.Balanced;
            }
        }

        /// <summary>
        /// 分配战斗角色
        /// </summary>
        private Dictionary<Troop, BattleAssignment> AssignBattleRoles(TacticalPlan plan)
        {
            var assignments = new Dictionary<Troop, BattleAssignment>();

            try
            {
                // 前排部队分配
                foreach (Troop troop in plan.Formation.FrontLine)
                {
                    assignments[troop] = new BattleAssignment
                    {
                        Position = BattlePosition.Front,
                        Priority = AIRoleSelector.GetRolePriority(_troopRoles.GetValueOrDefault(troop, TroopRole.Balanced), BattlePhase.Opening),
                        SpecialInstructions = GetSpecialInstructions(troop, BattlePosition.Front)
                    };
                }

                // 中排部队分配
                foreach (Troop troop in plan.Formation.MiddleLine)
                {
                    assignments[troop] = new BattleAssignment
                    {
                        Position = BattlePosition.Middle,
                        Priority = AIRoleSelector.GetRolePriority(_troopRoles.GetValueOrDefault(troop, TroopRole.Balanced), BattlePhase.Engagement),
                        SpecialInstructions = GetSpecialInstructions(troop, BattlePosition.Middle)
                    };
                }

                // 后排部队分配
                foreach (Troop troop in plan.Formation.BackLine)
                {
                    assignments[troop] = new BattleAssignment
                    {
                        Position = BattlePosition.Back,
                        Priority = AIRoleSelector.GetRolePriority(_troopRoles.GetValueOrDefault(troop, TroopRole.Balanced), BattlePhase.Engagement),
                        SpecialInstructions = GetSpecialInstructions(troop, BattlePosition.Back)
                    };
                }

                System.Diagnostics.Debug.WriteLine($"[战术AI] 完成战斗角色分配: 前排{plan.Formation.FrontLine.Count}, 中排{plan.Formation.MiddleLine.Count}, 后排{plan.Formation.BackLine.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] AssignBattleRoles 失败: {ex.Message}");
            }

            return assignments;
        }

        #region 辅助方法

        /// <summary>
        /// 计算部队战斗力
        /// </summary>
        private float CalculateTroopCombatPower(Troop troop)
        {
            try
            {
                if (troop?.Leader == null) return 0f;

                float power = troop.Quantity * 0.1f;
                power += troop.Leader.Command * 2f;
                power += troop.Leader.Strength * 1.5f;
                power += troop.Leader.Intelligence * 1f;

                return power;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// 计算部队机动性
        /// </summary>
        private float CalculateTroopMobility(Troop troop)
        {
            try
            {
                if (troop?.Kind == null) return 50f;

                // 基于兵种类型的简化机动性
                return troop.Kind.ID switch
                {
                    2 or 3 or 16 or 17 => 90f,  // 骑兵类高机动
                    1 or 15 => 70f,             // 远程类中等机动
                    _ => 50f                     // 其他兵种基础机动
                };
            }
            catch
            {
                return 50f;
            }
        }

        /// <summary>
        /// 计算部队生存能力
        /// </summary>
        private float CalculateTroopSurvivability(Troop troop)
        {
            try
            {
                if (troop?.Leader == null) return 0f;

                float survivability = troop.Leader.Command * 1.5f;
                survivability += troop.Quantity * 0.05f;

                // 肉盾类兵种加成
                if (_troopRoles.GetValueOrDefault(troop, TroopRole.Balanced) == TroopRole.Tank)
                {
                    survivability *= 1.3f;
                }

                return survivability;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// 计算到目标的距离
        /// </summary>
        private float CalculateDistanceToTarget(Faction faction, Architecture target)
        {
            // 简化实现，实际需要基于地图计算
            return 50f;
        }

        /// <summary>
        /// 计算势力军事实力
        /// </summary>
        private float CalculateFactionMilitaryStrength(Faction faction)
        {
            try
            {
                float strength = 0f;
                
                if (faction.Troops != null)
                {
                    foreach (Troop troop in faction.Troops.GetList())
                    {
                        if (troop != null)
                        {
                            strength += CalculateTroopCombatPower(troop);
                        }
                    }
                }

                return strength;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// 计算盟友可靠性
        /// </summary>
        private float CalculateAllyReliability(Faction ally)
        {
            // 简化实现，实际需要基于历史行为、关系等计算
            return 0.8f;
        }

        /// <summary>
        /// 获取特殊指令
        /// </summary>
        private string GetSpecialInstructions(Troop troop, BattlePosition position)
        {
            TroopRole role = _troopRoles.GetValueOrDefault(troop, TroopRole.Balanced);
            
            return (role, position) switch
            {
                (TroopRole.Tank, BattlePosition.Front) => "保持阵型，吸引火力",
                (TroopRole.DPS, BattlePosition.Middle) => "寻找输出机会，优先击杀",
                (TroopRole.Mage, BattlePosition.Back) => "施放控制技能，保持距离",
                (TroopRole.Support, BattlePosition.Back) => "支援友军，保护后排",
                _ => "根据战况灵活应对"
            };
        }

        // 简化的行动创建方法
        private List<TacticalAction> CreateOpeningActions(TacticalFormation formation, BattleForceAnalysis forces)
        {
            return new List<TacticalAction>
            {
                new TacticalAction { Type = "Deploy", Description = "部署前排肉盾", Priority = 100 },
                new TacticalAction { Type = "Position", Description = "后排就位", Priority = 90 }
            };
        }

        private List<TacticalAction> CreateEngagementActions(TacticalFormation formation, BattleForceAnalysis forces)
        {
            return new List<TacticalAction>
            {
                new TacticalAction { Type = "Attack", Description = "主力输出", Priority = 100 },
                new TacticalAction { Type = "Control", Description = "法师控制", Priority = 80 }
            };
        }

        private List<TacticalAction> CreateCleanupActions(TacticalFormation formation, BattleForceAnalysis forces)
        {
            return new List<TacticalAction>
            {
                new TacticalAction { Type = "Pursue", Description = "追击残敌", Priority = 90 },
                new TacticalAction { Type = "Secure", Description = "占领目标", Priority = 100 }
            };
        }

        private bool DeployTroopsForBattle(CoalitionBattlePlan battlePlan) => true;
        private void CoordinateAllyActions(CoalitionBattlePlan battlePlan) { }
        private void ExecutePhaseAttack(CoalitionBattlePlan battlePlan, BattlePhase phase) { }
        private void RecordTacticalExperience(CoalitionBattlePlan battlePlan) { }

        #endregion

        #region ZOC战术集成

        /// <summary>
        /// 为部队选择最佳ZOC卡位点
        /// </summary>
        /// <param name="troop">需要移动的部队</param>
        /// <param name="enemies">敌军列表</param>
        /// <param name="protectionTarget">需要保护的目标</param>
        /// <returns>最佳移动位置</returns>
        public System.Drawing.Point SelectOptimalZOCPosition(Troop troop, List<Troop> enemies, Troop protectionTarget = null)
        {
            try
            {
                if (troop == null) return troop?.Position ?? new System.Drawing.Point(0, 0);

                // 获取部队的移动范围
                List<System.Drawing.Point> moveRange = GetTroopMoveRange(troop);
                
                // 使用ZOC评估器选择最佳位置
                System.Drawing.Point bestPosition = ZOCEvaluator.GetBestBlockingPosition(troop, moveRange, enemies, protectionTarget);
                
                System.Diagnostics.Debug.WriteLine($"[战术AI] {troop.Leader?.Name} ZOC最佳位置: ({bestPosition.X}, {bestPosition.Y})");
                
                return bestPosition;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] SelectOptimalZOCPosition 失败: {ex.Message}");
                return troop?.Position ?? new System.Drawing.Point(0, 0);
            }
        }

        /// <summary>
        /// 获取部队的移动范围
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>可移动的位置列表</returns>
        private List<System.Drawing.Point> GetTroopMoveRange(Troop troop)
        {
            var moveRange = new List<System.Drawing.Point>();
            
            try
            {
                if (troop == null) return moveRange;

                // 简化的移动范围计算
                // 实际实现应该基于部队的移动力和地形
                int movePoints = GetTroopMovePoints(troop);
                System.Drawing.Point currentPos = troop.Position;

                // 生成曼哈顿距离内的所有可达点
                for (int x = currentPos.X - movePoints; x <= currentPos.X + movePoints; x++)
                {
                    for (int y = currentPos.Y - movePoints; y <= currentPos.Y + movePoints; y++)
                    {
                        int distance = Math.Abs(x - currentPos.X) + Math.Abs(y - currentPos.Y);
                        if (distance <= movePoints)
                        {
                            moveRange.Add(new System.Drawing.Point(x, y));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] GetTroopMoveRange 失败: {ex.Message}");
            }

            return moveRange;
        }

        /// <summary>
        /// 获取部队的移动力
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>移动点数</returns>
        private int GetTroopMovePoints(Troop troop)
        {
            try
            {
                if (troop?.Kind == null) return 3; // 默认移动力

                // 基于兵种类型的移动力
                int kindID = troop.Kind.ID;
                return kindID switch
                {
                    2 or 3 or 16 or 17 or 91 or 400 or 401 => 5, // 骑兵类高移动力
                    1 or 15 or 32 or 301 => 3, // 远程类中等移动力
                    11 or 51 or 52 or 101 or 102 => 2, // 重步兵低移动力
                    _ => 3 // 默认移动力
                };
            }
            catch
            {
                return 3;
            }
        }

        /// <summary>
        /// 制定ZOC控制战术
        /// </summary>
        /// <param name="enemies">敌军列表</param>
        /// <param name="keyTargets">关键保护目标</param>
        /// <returns>ZOC战术计划</returns>
        public ZOCTacticalPlan CreateZOCTacticalPlan(List<Troop> enemies, List<Troop> keyTargets)
        {
            var plan = new ZOCTacticalPlan();
            
            try
            {
                // 为每个部队分配ZOC任务
                foreach (var kvp in _troopRoles)
                {
                    Troop troop = kvp.Key;
                    TroopRole role = kvp.Value;

                    if (troop == null) continue;

                    // 根据角色分配不同的ZOC任务
                    switch (role)
                    {
                        case TroopRole.Tank:
                            // 肉盾负责前线卡位
                            AssignFrontlineZOCTask(plan, troop, enemies);
                            break;
                            
                        case TroopRole.Support:
                            // 辅助负责保护关键目标
                            AssignProtectionZOCTask(plan, troop, keyTargets, enemies);
                            break;
                            
                        case TroopRole.DPS:
                            // DPS在保证输出的前提下进行侧翼控制
                            AssignFlankingZOCTask(plan, troop, enemies);
                            break;
                            
                        case TroopRole.Mage:
                            // 法师保持安全距离，控制关键区域
                            AssignControlZOCTask(plan, troop, enemies);
                            break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[战术AI] 制定ZOC战术计划: {plan.Assignments.Count} 个任务分配");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] CreateZOCTacticalPlan 失败: {ex.Message}");
            }

            return plan;
        }

        /// <summary>
        /// 分配前线卡位任务
        /// </summary>
        private void AssignFrontlineZOCTask(ZOCTacticalPlan plan, Troop troop, List<Troop> enemies)
        {
            var assignment = new ZOCAssignment
            {
                Troop = troop,
                TaskType = ZOCTaskType.Frontline,
                Priority = 100,
                TargetPosition = SelectOptimalZOCPosition(troop, enemies),
                Description = "前线卡位控制"
            };
            
            plan.Assignments.Add(assignment);
        }

        /// <summary>
        /// 分配保护任务
        /// </summary>
        private void AssignProtectionZOCTask(ZOCTacticalPlan plan, Troop troop, List<Troop> keyTargets, List<Troop> enemies)
        {
            // 选择最需要保护的目标
            Troop mostVulnerableTarget = GetMostVulnerableTarget(keyTargets, enemies);
            
            var assignment = new ZOCAssignment
            {
                Troop = troop,
                TaskType = ZOCTaskType.Protection,
                Priority = 90,
                TargetPosition = SelectOptimalZOCPosition(troop, enemies, mostVulnerableTarget),
                ProtectionTarget = mostVulnerableTarget,
                Description = $"保护 {mostVulnerableTarget?.Leader?.Name}"
            };
            
            plan.Assignments.Add(assignment);
        }

        /// <summary>
        /// 分配侧翼控制任务
        /// </summary>
        private void AssignFlankingZOCTask(ZOCTacticalPlan plan, Troop troop, List<Troop> enemies)
        {
            var assignment = new ZOCAssignment
            {
                Troop = troop,
                TaskType = ZOCTaskType.Flanking,
                Priority = 70,
                TargetPosition = SelectOptimalZOCPosition(troop, enemies),
                Description = "侧翼控制"
            };
            
            plan.Assignments.Add(assignment);
        }

        /// <summary>
        /// 分配区域控制任务
        /// </summary>
        private void AssignControlZOCTask(ZOCTacticalPlan plan, Troop troop, List<Troop> enemies)
        {
            var assignment = new ZOCAssignment
            {
                Troop = troop,
                TaskType = ZOCTaskType.AreaControl,
                Priority = 60,
                TargetPosition = SelectOptimalZOCPosition(troop, enemies),
                Description = "区域控制"
            };
            
            plan.Assignments.Add(assignment);
        }

        /// <summary>
        /// 获取最脆弱的保护目标
        /// </summary>
        private Troop GetMostVulnerableTarget(List<Troop> targets, List<Troop> enemies)
        {
            try
            {
                Troop mostVulnerable = null;
                float highestThreat = 0f;

                foreach (Troop target in targets)
                {
                    if (target == null) continue;

                    // 计算目标面临的威胁程度
                    float threat = CalculateThreatLevel(target, enemies);
                    if (threat > highestThreat)
                    {
                        highestThreat = threat;
                        mostVulnerable = target;
                    }
                }

                return mostVulnerable;
            }
            catch
            {
                return targets?.FirstOrDefault();
            }
        }

        /// <summary>
        /// 计算目标的威胁等级
        /// </summary>
        private float CalculateThreatLevel(Troop target, List<Troop> enemies)
        {
            float threat = 0f;
            
            try
            {
                foreach (Troop enemy in enemies)
                {
                    if (enemy == null) continue;

                    int distance = Math.Abs(target.Position.X - enemy.Position.X) + 
                                  Math.Abs(target.Position.Y - enemy.Position.Y);
                    
                    // 距离越近威胁越大
                    if (distance <= 3)
                    {
                        float enemyThreat = CalculateTroopCombatPower(enemy) / Math.Max(distance, 1);
                        threat += enemyThreat;
                    }
                }
            }
            catch
            {
                // 忽略错误，返回当前威胁值
            }

            return threat;
        }

        #endregion

        /// <summary>
        /// 获取战术状态报告
        /// </summary>
        public string GetTacticalStatusReport()
        {
            try
            {
                var report = new System.Text.StringBuilder();
                report.AppendLine($"=== {_faction.Name} 战术状态报告 ===");
                
                // 部队角色分布
                var roleStats = _troopRoles.Values.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
                report.AppendLine("部队角色分布:");
                foreach (var stat in roleStats)
                {
                    report.AppendLine($"  {AIRoleSelector.GetRoleDescription(stat.Key)}: {stat.Value} 支");
                }

                // 当前阵型
                report.AppendLine($"当前阵型: {_currentFormation.GetFormationDescription()}");

                // 战术记忆
                report.AppendLine($"战术经验记录: {_tacticalMemory.Count} 条");

                return report.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[战术AI] GetTacticalStatusReport 失败: {ex.Message}");
                return "报告生成失败";
            }
        }
    }

    #region 数据结构

    /// <summary>
    /// 联盟作战计划
    /// </summary>
    public class CoalitionBattlePlan
    {
        public Architecture Target { get; set; }
        public Faction PrimaryAttacker { get; set; }
        public List<Faction> Allies { get; set; } = new List<Faction>();
        public float TargetDefenseRating { get; set; }
        public BattleForceAnalysis OwnForces { get; set; }
        public AllySupportAnalysis AllySupport { get; set; }
        public int PredictedSuccessRate { get; set; }
        public TacticalPlan TacticalPlan { get; set; }
        public Dictionary<Troop, BattleAssignment> RoleAssignments { get; set; } = new Dictionary<Troop, BattleAssignment>();
        public DateTime CreatedTime { get; set; }
    }

    /// <summary>
    /// 战力分析
    /// </summary>
    public class BattleForceAnalysis
    {
        public List<TroopBattleInfo> TroopInfos { get; set; } = new List<TroopBattleInfo>();
        public int TotalTroops { get; set; }
        public int TankCount { get; set; }
        public int DPSCount { get; set; }
        public int MageCount { get; set; }
        public int SupportCount { get; set; }
        public int BalancedCount { get; set; }
        public float TotalAttackPower { get; set; }
        public float TotalDefensePower { get; set; }
        public float TotalUtilityPower { get; set; }
        public float OverallRating { get; set; }
    }

    /// <summary>
    /// 部队战斗信息
    /// </summary>
    public class TroopBattleInfo
    {
        public Troop Troop { get; set; }
        public TroopRole Role { get; set; }
        public float CombatPower { get; set; }
        public float Mobility { get; set; }
        public float Survivability { get; set; }
    }

    /// <summary>
    /// 盟友支援分析
    /// </summary>
    public class AllySupportAnalysis
    {
        public List<AllyInfo> EffectiveAllies { get; set; } = new List<AllyInfo>();
        public List<AllyInfo> DistantAllies { get; set; } = new List<AllyInfo>();
        public float TotalSupportPower { get; set; }
    }

    /// <summary>
    /// 盟友信息
    /// </summary>
    public class AllyInfo
    {
        public Faction Faction { get; set; }
        public float Distance { get; set; }
        public float MilitaryStrength { get; set; }
        public float Reliability { get; set; }
        public float SupportCapability { get; set; }
    }

    /// <summary>
    /// 战术计划
    /// </summary>
    public class TacticalPlan
    {
        public Architecture Target { get; set; }
        public BattleStrategy Strategy { get; set; }
        public TacticalFormation Formation { get; set; }
        public Dictionary<BattlePhase, List<TacticalAction>> PhaseActions { get; set; } = new Dictionary<BattlePhase, List<TacticalAction>>();
    }

    /// <summary>
    /// 战斗策略
    /// </summary>
    public enum BattleStrategy
    {
        Aggressive,  // 主动进攻
        Defensive,   // 防御反击
        Balanced,    // 均衡作战
        Control,     // 控制流
        Attrition,   // 消耗战
        Coalition    // 联盟协同
    }

    /// <summary>
    /// 战术行动
    /// </summary>
    public class TacticalAction
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// 战斗分配
    /// </summary>
    public class BattleAssignment
    {
        public BattlePosition Position { get; set; }
        public int Priority { get; set; }
        public string SpecialInstructions { get; set; }
    }

    /// <summary>
    /// 战斗位置
    /// </summary>
    public enum BattlePosition
    {
        Front,  // 前排
        Middle, // 中排
        Back    // 后排
    }

    /// <summary>
    /// 战术记忆
    /// </summary>
    public class TacticalMemory
    {
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public float SuccessRate { get; set; }
    }

    /// <summary>
    /// ZOC战术计划
    /// </summary>
    public class ZOCTacticalPlan
    {
        public List<ZOCAssignment> Assignments { get; set; } = new List<ZOCAssignment>();
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public string PlanName { get; set; } = "ZOC控制计划";
    }

    /// <summary>
    /// ZOC任务分配
    /// </summary>
    public class ZOCAssignment
    {
        public Troop Troop { get; set; }
        public ZOCTaskType TaskType { get; set; }
        public int Priority { get; set; }
        public System.Drawing.Point TargetPosition { get; set; }
        public Troop ProtectionTarget { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// ZOC任务类型
    /// </summary>
    public enum ZOCTaskType
    {
        Frontline,    // 前线卡位
        Protection,   // 保护目标
        Flanking,     // 侧翼控制
        AreaControl   // 区域控制
    }

    #endregion
}