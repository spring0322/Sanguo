using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// 联盟管理系统
    /// 处理动态联盟形成、目标选择、威胁评估等功能
    /// 集成军师预测系统进行智能决策
    /// </summary>
    public class CoalitionManager
    {
        public static CoalitionManager Instance;

        public bool IsActive = false;
        public Faction LeaderFaction; // 盟主
        public List<Faction> Members = new List<Faction>();
        public Architecture TargetArchitecture; // 盟主指定的集火目标

        private float _playerThreat = 0f;
        public float PlayerThreat => _playerThreat;
        
        public float ThreatThreshold = 100f; // 威胁度触发阈值
        public int PlayerCityThreshold = 20; // 玩家城市数量阈值
        public int MaxCoalitionSize = 8; // 最大联盟规模
        public float CoalitionDuration = 50f; // 联盟持续时间（回合）
        
        private float _coalitionTimer = 0f;
        private Dictionary<Faction, float> _factionRelations = new Dictionary<Faction, float>();
        private List<CoalitionEvent> _eventHistory = new List<CoalitionEvent>();

        public CoalitionManager()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void Initialize()
        {
            InitializeSystem();
        }

        /// <summary>
        /// 初始化联盟系统
        /// </summary>
        private void InitializeSystem()
        {
            try
            {
                _playerThreat = 0f;
                _coalitionTimer = 0f;
                _factionRelations.Clear();
                _eventHistory.Clear();
                
                // 初始化势力关系
                if (Session.Current?.Scenario?.Factions != null)
                {
                    foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                    {
                        if (faction != null && !faction.IsPlayer)
                        {
                            _factionRelations[faction] = new Random().Next(-50, 51);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("[联盟系统] 初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 修改玩家威胁度
        /// </summary>
        /// <param name="amount">威胁度变化量</param>
        /// <param name="reason">变化原因</param>
        public void ModifyThreat(float amount, string reason = "")
        {
            try
            {
                float oldThreat = _playerThreat;
                _playerThreat += amount;
                _playerThreat = Math.Max(0f, Math.Min(_playerThreat, 500f)); // 限制威胁度范围

                System.Diagnostics.Debug.WriteLine($"[威胁度] {reason}: {oldThreat:F1} → {_playerThreat:F1} (变化: {amount:+0.0;-0.0})");

                // 记录威胁度变化事件
                RecordEvent(new CoalitionEvent
                {
                    Type = CoalitionEventType.ThreatChange,
                    Description = $"{reason}: 威胁度变化 {amount:+0.0;-0.0}",
                    ThreatLevel = _playerThreat
                });

                CheckTriggerCondition();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] ModifyThreat 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查联盟触发条件
        /// </summary>
        private void CheckTriggerCondition()
        {
            try
            {
                if (IsActive) return;

                Faction playerFaction = GetPlayerFaction();
                if (playerFaction == null) return;

                // 触发条件：威胁度 > 阈值 且 玩家是大魔王
                bool threatCondition = _playerThreat > ThreatThreshold;
                bool powerCondition = playerFaction.Architectures.Count > PlayerCityThreshold;
                
                if (threatCondition && powerCondition)
                {
                    System.Diagnostics.Debug.WriteLine($"[联盟系统] 触发条件满足: 威胁度{_playerThreat:F1} > {ThreatThreshold}, 城市数{playerFaction.Architectures.Count} > {PlayerCityThreshold}");
                    FormCoalition();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] CheckTriggerCondition 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 形成联盟
        /// </summary>
        private void FormCoalition()
        {
            try
            {
                IsActive = true;
                _coalitionTimer = CoalitionDuration;
                Members.Clear();

                // 1. 选盟主 (排除玩家，选声望最高的)
                LeaderFaction = GetAllNonPlayerFactions()
                    .Where(f => f.Architectures.Count > 5) // 至少要有一定实力
                    .OrderByDescending(f => CalculateLeadershipScore(f))
                    .FirstOrDefault();

                if (LeaderFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[联盟系统] 无法找到合适的盟主");
                    IsActive = false;
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[联盟系统] 选定盟主: {LeaderFaction.Name}");

                // 2. 拉人入伙
                var potentialMembers = GetAllNonPlayerFactions()
                    .Where(f => f != LeaderFaction)
                    .OrderByDescending(f => CalculateJoinProbability(f))
                    .Take(MaxCoalitionSize - 1)
                    .ToList();

                foreach (var faction in potentialMembers)
                {
                    if (ShouldJoin(faction))
                    {
                        Members.Add(faction);
                        faction.EnterCoalitionMode(LeaderFaction); // 切换AI状态
                        System.Diagnostics.Debug.WriteLine($"[联盟系统] {faction.Name} 加入联盟");
                    }
                }

                // 3. 选择初始目标
                UpdateCoalitionTarget();

                // 4. 发布檄文 (UI通知)
                string memberNames = string.Join("、", Members.Select(m => m.Name));
                string announcement = $"【{Members.Count + 1}路诸侯联盟】\n" +
                                    $"盟主 {LeaderFaction.Name} 发起号召！\n" +
                                    $"成员：{memberNames}\n" +
                                    $"誓要讨伐无道，共击 {TargetArchitecture?.Name ?? "敌军"}！";

                ShowCoalitionEvent(announcement);

                // 5. 记录联盟形成事件
                RecordEvent(new CoalitionEvent
                {
                    Type = CoalitionEventType.CoalitionFormed,
                    Description = $"联盟形成，盟主: {LeaderFaction.Name}，成员: {Members.Count}",
                    ThreatLevel = _playerThreat
                });

                System.Diagnostics.Debug.WriteLine($"[联盟系统] 联盟形成完成，盟主: {LeaderFaction.Name}，成员数: {Members.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] FormCoalition 失败: {ex.Message}");
                IsActive = false;
            }
        }

        /// <summary>
        /// 计算领导力评分
        /// </summary>
        private float CalculateLeadershipScore(Faction faction)
        {
            try
            {
                float score = 0f;
                
                // 基础实力
                score += faction.Architectures.Count * 10f;
                score += faction.Reputation * 2f;
                
                // 君主能力
                if (faction.Leader != null)
                {
                    score += faction.Leader.Politics * 1.5f;
                    score += faction.Leader.Glamour * 1.2f;
                    score += faction.Leader.Intelligence * 1.0f;
                }

                // 军师加成
                if (faction.Advisor != null)
                {
                    score += faction.Advisor.Intelligence * 0.8f;
                    score += faction.Advisor.Politics * 0.6f;
                }

                // 地理位置加成（靠近玩家的更容易成为盟主）
                Faction playerFaction = GetPlayerFaction();
                if (playerFaction != null)
                {
                    float distance = CalculateAverageDistance(faction, playerFaction);
                    score += (100f - distance) * 0.5f; // 距离越近分数越高
                }

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] CalculateLeadershipScore 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算加入概率
        /// </summary>
        private float CalculateJoinProbability(Faction faction)
        {
            try
            {
                float probability = 50f; // 基础概率

                // 使用军师预测系统评估加入联盟的收益
                if (faction.Advisor != null)
                {
                    // 模拟"加入联盟"的成功率预测
                    Faction playerFaction = GetPlayerFaction();
                    if (playerFaction?.Leader != null)
                    {
                        // 使用外交预测系统评估对抗玩家的成功率
                        int battleChance = AdvisorPredictionSystem.GetDiplomacyChanceDisplay(
                            faction.Advisor, playerFaction, "AntiPlayerAlliance");
                        
                        // 军师预测成功率越高，越愿意加入
                        probability += (battleChance - 50f) * 0.8f;
                        
                        System.Diagnostics.Debug.WriteLine($"[联盟系统] {faction.Name} 军师 {faction.Advisor.Name} 预测对抗玩家成功率: {battleChance}%");
                    }
                }

                // 威胁感知
                probability += _playerThreat * 0.3f;

                // 实力对比
                Faction player = GetPlayerFaction();
                if (player != null)
                {
                    float powerRatio = (float)faction.Architectures.Count / player.Architectures.Count;
                    if (powerRatio < 0.5f) probability += 30f; // 弱势更愿意抱团
                }

                // 地理因素
                if (player != null)
                {
                    float distance = CalculateAverageDistance(faction, player);
                    if (distance < 50f) probability += 20f; // 邻近势力更有危机感
                }

                // 性格因素
                if (faction.Leader != null)
                {
                    switch (faction.Leader.CharacterKindID)
                    {
                        case 0: probability += 10f; break; // 仁德型更愿意合作
                        case 1: probability -= 15f; break; // 多疑型不太信任联盟
                        case 2: probability += 5f; break;  // 莽撞型容易冲动加入
                        case 3: probability += 15f; break; // 狡诈型善于利用联盟
                    }
                }

                return Math.Max(0f, Math.Min(probability, 100f));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] CalculateJoinProbability 失败: {ex.Message}");
                return 50f;
            }
        }

        /// <summary>
        /// 判断势力是否应该加入联盟
        /// </summary>
        private bool ShouldJoin(Faction faction)
        {
            try
            {
                if (faction == null || faction.IsPlayer) return false;

                float joinProbability = CalculateJoinProbability(faction);
                bool shouldJoin = new Random().NextDouble() < (joinProbability / 100.0);

                System.Diagnostics.Debug.WriteLine($"[联盟系统] {faction.Name} 加入概率: {joinProbability:F1}%, 结果: {(shouldJoin ? "加入" : "拒绝")}");

                return shouldJoin;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] ShouldJoin 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// AI 可以在每回合调用此方法获取"当前该打哪"
        /// </summary>
        public Architecture GetCoalitionTarget()
        {
            try
            {
                if (!IsActive || LeaderFaction == null) return null;

                // 如果目标已被占领或无效，重新选择
                if (TargetArchitecture == null || 
                    TargetArchitecture.BelongedFaction == null ||
                    !TargetArchitecture.BelongedFaction.IsPlayer)
                {
                    UpdateCoalitionTarget();
                }

                return TargetArchitecture;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] GetCoalitionTarget 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 更新联盟目标
        /// </summary>
        private void UpdateCoalitionTarget()
        {
            try
            {
                Faction playerFaction = GetPlayerFaction();
                if (playerFaction?.Architectures == null) return;

                // 使用军师预测系统选择最佳目标
                Architecture bestTarget = null;
                float bestScore = 0f;

                foreach (Architecture arch in playerFaction.Architectures.GetList())
                {
                    if (arch == null) continue;

                    float score = EvaluateTargetValue(arch);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = arch;
                    }
                }

                TargetArchitecture = bestTarget;
                
                if (TargetArchitecture != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[联盟系统] 更新目标: {TargetArchitecture.Name} (评分: {bestScore:F1})");
                    
                    // 通知所有联盟成员
                    string targetUpdate = $"【联盟目标更新】\n盟主 {LeaderFaction.Name} 指定新目标：{TargetArchitecture.Name}";
                    ShowCoalitionEvent(targetUpdate);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] UpdateCoalitionTarget 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 评估目标价值
        /// </summary>
        private float EvaluateTargetValue(Architecture target)
        {
            try
            {
                float score = 0f;

                // 基础价值
                score += target.Population * 0.01f;
                score += target.Fund * 0.001f;
                score += target.Food * 0.001f;

                // 战略价值
                if (target.Name.Contains("洛阳") || target.Name.Contains("长安")) score += 100f;
                if (target.Name.Contains("都") || target.Name.Contains("京")) score += 50f;

                // 防守强度（越弱越容易攻打）
                float defenseStrength = CalculateDefenseStrength(target);
                score += (100f - defenseStrength) * 0.5f;

                // 地理位置（靠近联盟成员的优先）
                float accessibility = CalculateAccessibility(target);
                score += accessibility * 0.3f;

                // 军师预测攻城成功率
                if (LeaderFaction?.Advisor != null && target.Mayor != null)
                {
                    // 使用计谋预测系统评估攻城成功率
                    int siegeChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(
                        LeaderFaction.Advisor, target.Mayor, "Siege");
                    score += siegeChance * 0.8f;
                }

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] EvaluateTargetValue 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 计算防守强度
        /// </summary>
        private float CalculateDefenseStrength(Architecture target)
        {
            try
            {
                float strength = 0f;

                // 驻军数量
                if (target.Persons != null)
                {
                    strength += target.Persons.Count * 10f;
                }

                // 太守能力
                if (target.Mayor != null)
                {
                    strength += target.Mayor.Command * 0.5f;
                    strength += target.Mayor.Intelligence * 0.3f;
                }

                // 建筑防御
                strength += target.Endurance * 0.1f;

                // 附近友军支援
                Faction playerFaction = GetPlayerFaction();
                if (playerFaction != null)
                {
                    int nearbySupport = CountNearbyFriendlyArchitectures(target, playerFaction);
                    strength += nearbySupport * 15f;
                }

                return Math.Max(0f, Math.Min(strength, 100f));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] CalculateDefenseStrength 失败: {ex.Message}");
                return 50f;
            }
        }

        /// <summary>
        /// 计算可达性
        /// </summary>
        private float CalculateAccessibility(Architecture target)
        {
            try
            {
                float accessibility = 0f;
                int memberCount = 0;

                foreach (Faction member in Members)
                {
                    if (member?.Architectures == null) continue;

                    float minDistance = float.MaxValue;
                    foreach (Architecture memberArch in member.Architectures.GetList())
                    {
                        if (memberArch == null) continue;
                        
                        float distance = CalculateDistance(memberArch, target);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }

                    if (minDistance < float.MaxValue)
                    {
                        accessibility += (100f - minDistance);
                        memberCount++;
                    }
                }

                return memberCount > 0 ? accessibility / memberCount : 0f;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] CalculateAccessibility 失败: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 每回合更新
        /// </summary>
        public void OnTurnUpdate()
        {
            try
            {
                if (!IsActive) return;

                _coalitionTimer -= 1f;

                // 联盟自然解散
                if (_coalitionTimer <= 0f)
                {
                    DissolveCoalition("联盟期限到期");
                    return;
                }

                // 定期更新目标
                if (new Random().NextDouble() < 0.3) // 30%概率更新目标
                {
                    UpdateCoalitionTarget();
                }

                // 检查成员状态
                CheckMemberStatus();

                // 威胁度自然衰减
                if (_playerThreat > 0f)
                {
                    ModifyThreat(-2f, "自然衰减");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] OnTurnUpdate 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查成员状态
        /// </summary>
        private void CheckMemberStatus()
        {
            try
            {
                var toRemove = new List<Faction>();

                foreach (Faction member in Members)
                {
                    if (member == null || member.Destroyed || member.Architectures.Count == 0)
                    {
                        toRemove.Add(member);
                        continue;
                    }

                    // 检查是否还愿意继续参与联盟
                    if (new Random().NextDouble() < 0.05) // 5%概率退出
                    {
                        float continueProbability = CalculateJoinProbability(member) * 0.8f; // 继续参与的概率稍低
                        if (new Random().NextDouble() > (continueProbability / 100.0))
                        {
                            toRemove.Add(member);
                            System.Diagnostics.Debug.WriteLine($"[联盟系统] {member.Name} 退出联盟");
                        }
                    }
                }

                // 移除退出的成员
                foreach (Faction member in toRemove)
                {
                    Members.Remove(member);
                    member?.ExitCoalitionMode(); // 恢复正常AI状态
                }

                // 如果成员太少，解散联盟
                if (Members.Count < 2)
                {
                    DissolveCoalition("成员不足");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] CheckMemberStatus 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 解散联盟
        /// </summary>
        private void DissolveCoalition(string reason)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] 联盟解散: {reason}");

                // 恢复所有成员的正常状态
                foreach (Faction member in Members)
                {
                    member?.ExitCoalitionMode();
                }

                // 清理状态
                IsActive = false;
                LeaderFaction = null;
                Members.Clear();
                TargetArchitecture = null;
                _coalitionTimer = 0f;

                // 通知玩家
                ShowCoalitionEvent($"【联盟解散】\n反玩家联盟已解散\n原因：{reason}");

                // 记录事件
                RecordEvent(new CoalitionEvent
                {
                    Type = CoalitionEventType.CoalitionDissolved,
                    Description = $"联盟解散: {reason}",
                    ThreatLevel = _playerThreat
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] DissolveCoalition 失败: {ex.Message}");
            }
        }

        #region 辅助方法

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
        /// 获取所有非玩家势力
        /// </summary>
        private IEnumerable<Faction> GetAllNonPlayerFactions()
        {
            try
            {
                return Session.Current?.Scenario?.Factions?.GetList()
                    ?.Where(f => f != null && !f.IsPlayer && !f.Destroyed) ?? new List<Faction>();
            }
            catch
            {
                return new List<Faction>();
            }
        }

        /// <summary>
        /// 计算两个势力间的平均距离
        /// </summary>
        private float CalculateAverageDistance(Faction faction1, Faction faction2)
        {
            try
            {
                if (faction1?.Architectures == null || faction2?.Architectures == null) return 100f;

                float totalDistance = 0f;
                int count = 0;

                foreach (Architecture arch1 in faction1.Architectures.GetList())
                {
                    if (arch1 == null) continue;
                    
                    foreach (Architecture arch2 in faction2.Architectures.GetList())
                    {
                        if (arch2 == null) continue;
                        
                        totalDistance += CalculateDistance(arch1, arch2);
                        count++;
                    }
                }

                return count > 0 ? totalDistance / count : 100f;
            }
            catch
            {
                return 100f;
            }
        }

        /// <summary>
        /// 计算两个建筑间的距离
        /// </summary>
        private float CalculateDistance(Architecture arch1, Architecture arch2)
        {
            try
            {
                if (arch1?.ArchitectureArea?.Centre == null || arch2?.ArchitectureArea?.Centre == null)
                    return 100f;

                var pos1 = arch1.ArchitectureArea.Centre;
                var pos2 = arch2.ArchitectureArea.Centre;
                
                float dx = pos1.X - pos2.X;
                float dy = pos1.Y - pos2.Y;
                
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }
            catch
            {
                return 100f;
            }
        }

        /// <summary>
        /// 计算附近友军建筑数量
        /// </summary>
        private int CountNearbyFriendlyArchitectures(Architecture target, Faction faction)
        {
            try
            {
                int count = 0;
                const float maxDistance = 50f;

                foreach (Architecture arch in faction.Architectures.GetList())
                {
                    if (arch == null || arch == target) continue;
                    
                    if (CalculateDistance(target, arch) <= maxDistance)
                    {
                        count++;
                    }
                }

                return count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 显示联盟事件
        /// </summary>
        private void ShowCoalitionEvent(string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[联盟事件] {message}");
                
                // 这里应该调用实际的UI系统
                // UIManager.ShowEvent(message);
                
                // 临时使用Debug输出
                System.Diagnostics.Debug.WriteLine($"[UI事件] {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] ShowCoalitionEvent 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录联盟事件
        /// </summary>
        private void RecordEvent(CoalitionEvent coalitionEvent)
        {
            try
            {
                coalitionEvent.Timestamp = DateTime.Now;
                _eventHistory.Add(coalitionEvent);
                
                // 限制历史记录数量
                if (_eventHistory.Count > 100)
                {
                    _eventHistory.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[联盟系统] RecordEvent 失败: {ex.Message}");
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 获取联盟状态信息
        /// </summary>
        public CoalitionStatus GetCoalitionStatus()
        {
            return new CoalitionStatus
            {
                IsActive = IsActive,
                LeaderName = LeaderFaction?.Name ?? "无",
                MemberCount = Members.Count,
                MemberNames = Members.Select(m => m.Name).ToList(),
                TargetName = TargetArchitecture?.Name ?? "无",
                PlayerThreat = _playerThreat,
                RemainingTime = _coalitionTimer
            };
        }

        /// <summary>
        /// 获取事件历史
        /// </summary>
        public List<CoalitionEvent> GetEventHistory()
        {
            return new List<CoalitionEvent>(_eventHistory);
        }

        /// <summary>
        /// 强制解散联盟（调试用）
        /// </summary>
        public void ForceDissolveCoalition()
        {
            if (IsActive)
            {
                DissolveCoalition("强制解散");
            }
        }

        /// <summary>
        /// 重置威胁度（调试用）
        /// </summary>
        public void ResetThreat()
        {
            _playerThreat = 0f;
            System.Diagnostics.Debug.WriteLine("[联盟系统] 威胁度已重置");
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 联盟事件类型
    /// </summary>
    public enum CoalitionEventType
    {
        ThreatChange,      // 威胁度变化
        CoalitionFormed,   // 联盟形成
        CoalitionDissolved, // 联盟解散
        MemberJoined,      // 成员加入
        MemberLeft,        // 成员离开
        TargetChanged      // 目标变更
    }

    /// <summary>
    /// 联盟事件
    /// </summary>
    [Serializable]
    public class CoalitionEvent
    {
        public CoalitionEventType Type;
        public string Description;
        public float ThreatLevel;
        public DateTime Timestamp;
    }

    /// <summary>
    /// 联盟状态
    /// </summary>
    [Serializable]
    public class CoalitionStatus
    {
        public bool IsActive;
        public string LeaderName;
        public int MemberCount;
        public List<string> MemberNames;
        public string TargetName;
        public float PlayerThreat;
        public float RemainingTime;
    }

    #endregion
}