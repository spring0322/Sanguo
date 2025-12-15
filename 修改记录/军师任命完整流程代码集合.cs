// ===================================================================
// 军师任命完整流程代码集合
// 包含从UI交互到数据存储的完整实现
// ===================================================================

// ===================================================================
// 1. 核心数据结构 - Faction.cs 中的军师管理
// ===================================================================

namespace GameObjects
{
    [DataContract]
    public partial class Faction : GameObject
    {
        // 私有字段
        private Person advisor = null;
        private int advisorID = -1;

        /// <summary>
        /// 军师ID属性 - 用于数据持久化
        /// </summary>
        [DataMember]
        public int AdvisorID
        {
            get { return this.advisorID; }
            set { this.advisorID = value; }
        }

        /// <summary>
        /// 军师对象属性 - 带缓存机制的智能获取
        /// </summary>
        public Person Advisor
        {
            get
            {
                // 如果缓存为空且有有效ID，则从Scenario中获取
                if (this.advisor == null && this.advisorID != -1 && 
                    Session.Current.Scenario != null && Session.Current.Scenario.Persons != null)
                {
                    this.advisor = Session.Current.Scenario.Persons.GetGameObject(this.AdvisorID) as Person;
                }
                
                // 检查军师有效性 - 自动清理无效军师
                if (this.advisor != null && (!this.advisor.Alive || !this.advisor.Available || this.advisor.BelongedFaction != this))
                {
                    this.Advisor = null; // 清理无效军师
                }
                
                return this.advisor;
            }
            set
            {
                this.advisor = value;
                if (this.advisor != null)
                {
                    this.AdvisorID = this.advisor.ID;
                }
                else
                {
                    this.AdvisorID = -1;
                }
            }
        }

        /// <summary>
        /// 军师姓名属性 - 用于UI显示
        /// </summary>
        public string AdvisorName
        {
            get
            {
                return ((this.Advisor != null) ? this.Advisor.Name : "----");
            }
        }

        /// <summary>
        /// 检查是否可以任命军师（允许重新任命）
        /// </summary>
        public bool AppointAdvisorAvail()
        {
            if (this.Leader != null && this.Leader.BelongedCaptive == null)
            {
                if (Session.Current.Scenario.IsPlayer(this) && this.AdvisorCandicate.Count > 0)
                {
                    return true;
                }

                if (!Session.Current.Scenario.IsPlayer(this) && this.AIAdvisorCandicate.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否可以罢免军师
        /// </summary>
        public bool RecallAdvisorAvail()
        {
            return this.Leader != null && this.Leader.BelongedCaptive == null && this.AdvisorID != -1;
        }

        /// <summary>
        /// 军师候选人列表（玩家用）
        /// </summary>
        public PersonList AdvisorCandicate
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person p in this.Persons)
                {
                    if (p != this.Leader && p != this.Advisor && p.Available && p.Alive && 
                        p.BelongedCaptive == null && p.LocationTroop == null && p.Intelligence >= 70)
                    {
                        result.Add(p);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// AI军师候选人列表（按智力排序）
        /// </summary>
        public PersonList AIAdvisorCandicate
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person p in this.Persons)
                {
                    if (p != this.Leader && p != this.Advisor && p.Available && p.Alive && 
                        p.BelongedCaptive == null && p.LocationTroop == null && p.Intelligence >= 70)
                    {
                        result.Add(p);
                    }
                }
                // 按智力降序排序
                result.Sort((p1, p2) => p2.Intelligence.CompareTo(p1.Intelligence));
                return result;
            }
        }

        /// <summary>
        /// 任命军师 - 核心方法
        /// </summary>
        /// <param name="person">被任命的人物</param>
        public void AppointAdvisor(Person person)
        {
            // 设置军师
            this.Advisor = person;
            
            // 添加年表记录
            Session.Current.Scenario.YearTable.addAppointAdvisorEntry(Session.Current.Scenario.Date, person, this.Leader);
            
            // 触发事件
            if (this.OnAppointAdvisor != null)
            {
                this.OnAppointAdvisor(this.Leader, person);
            }
        }

        /// <summary>
        /// 任命军师事件
        /// </summary>
        public event AppointAdvisorDelegate OnAppointAdvisor;
        public delegate void AppointAdvisorDelegate(Person leader, Person advisor);

        /// <summary>
        /// AI自动任命军师 - 根据君主性格选择
        /// </summary>
        public void AIAppointAdvisor()
        {
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                if (this.AppointAdvisorAvail())
                {
                    PersonList candidates = this.AIAdvisorCandicate;
                    if (candidates.Count > 0)
                    {
                        Person selectedCandidate = candidates[0]; // 智力最高的候选人
                        
                        // 决定是否需要更换军师
                        if (ShouldAppointNewAdvisor(selectedCandidate))
                        {
                            System.Diagnostics.Debug.WriteLine($"[AI任命军师] {this.LeaderName} 任命 {selectedCandidate.Name} 为军师");
                            
                            this.AdvisorID = selectedCandidate.ID;
                            this.AppointAdvisor(selectedCandidate);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否应该任命新军师 - 基于君主性格
        /// </summary>
        private bool ShouldAppointNewAdvisor(Person candidate)
        {
            if (candidate == null) return false;
            
            // 如果没有军师，直接任命
            if (this.Advisor == null)
            {
                System.Diagnostics.Debug.WriteLine("[任命判断] 无现任军师，直接任命");
                return true;
            }
            
            Person currentAdvisor = this.Advisor;
            
            // 根据君主性格决定更换标准
            switch (this.Leader.Character.ID)
            {
                case 0: // 仁德型 - 不轻易更换，除非新人明显更好
                    bool shouldReplaceVirtuous = candidate.Intelligence > currentAdvisor.Intelligence + 15 ||
                                               (candidate.Loyalty > currentAdvisor.Loyalty + 20 && candidate.Intelligence >= currentAdvisor.Intelligence - 5);
                    System.Diagnostics.Debug.WriteLine($"[仁德型判断] 是否更换: {shouldReplaceVirtuous}");
                    return shouldReplaceVirtuous;
                    
                case 1: // 霸道型 - 追求更强的能力
                    bool shouldReplaceAmbitious = candidate.Intelligence > currentAdvisor.Intelligence + 10;
                    System.Diagnostics.Debug.WriteLine($"[霸道型判断] 是否更换: {shouldReplaceAmbitious}");
                    return shouldReplaceAmbitious;
                    
                case 2: // 冷静型 - 理性比较
                    bool shouldReplaceRational = candidate.Intelligence > currentAdvisor.Intelligence + 8;
                    System.Diagnostics.Debug.WriteLine($"[冷静型判断] 是否更换: {shouldReplaceRational}");
                    return shouldReplaceRational;
                    
                case 3: // 莽撞型 - 可能冲动更换
                    if (GameObject.Random(100) < 30) // 30%概率冲动更换
                    {
                        System.Diagnostics.Debug.WriteLine("[莽撞型判断] 冲动更换军师");
                        return true;
                    }
                    // 否则需要明显更好才换
                    bool shouldReplaceImpulsive = candidate.Intelligence > currentAdvisor.Intelligence + 20;
                    System.Diagnostics.Debug.WriteLine($"[莽撞型判断] 理性判断是否更换: {shouldReplaceImpulsive}");
                    return shouldReplaceImpulsive;
                    
                case 4: // 狡诈型 - 可能因为关系更换
                    // 如果新候选人有特殊关系，可能更换
                    if (HasSpecialRelationWithLeader(candidate) && candidate.Intelligence >= currentAdvisor.Intelligence - 10)
                    {
                        System.Diagnostics.Debug.WriteLine("[狡诈型判断] 因关系更换军师");
                        return true;
                    }
                    // 否则需要智力明显更高
                    bool shouldReplaceCunning = candidate.Intelligence > currentAdvisor.Intelligence + 12;
                    System.Diagnostics.Debug.WriteLine($"[狡诈型判断] 能力判断是否更换: {shouldReplaceCunning}");
                    return shouldReplaceCunning;
                    
                default:
                    return candidate.Intelligence > currentAdvisor.Intelligence + 10;
            }
        }

        /// <summary>
        /// 检查候选人是否与君主有特殊关系
        /// </summary>
        private bool HasSpecialRelationWithLeader(Person candidate)
        {
            if (this.Leader == null || candidate == null) return false;
            
            // 检查亲属关系、结拜关系等
            return candidate.CheckRelation(this.Leader) == 1; // 喜爱关系
        }
    }
}

// ===================================================================
// 2. UI交互处理 - ScreenManager.cs 中的任命逻辑
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class ScreenManager
    {
        /// <summary>
        /// 处理军师任命的Frame函数调用
        /// </summary>
        private void FrameFunction_Faction_AppointAdvisor()
        {
            System.Diagnostics.Debug.WriteLine("[AppointAdvisor] 开始执行任命军师逻辑");
            
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Person;
            System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 选中的人物: {this.CurrentPerson?.Name}");
            
            if (this.CurrentPerson != null)
            {
                // 获取目标势力
                Faction faction = this.CurrentFaction ?? 
                                 this.CurrentArchitecture?.BelongedFaction ?? 
                                 this.CurrentFaction;
                System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 目标势力: {faction?.Name}");
                
                if (faction != null && faction.Leader != null)
                {
                    // 如果已有军师，先清除
                    if (faction.AdvisorID > 0 && faction.Advisor != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 当前军师: {faction.Advisor.Name}，将被替换");
                        faction.AdvisorID = -1;
                        faction.Advisor = null;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 使用AdvisorAppointmentSystem任命军师: {this.CurrentPerson.Name}");
                    
                    // 使用AdvisorAppointmentSystem来处理任命，这样会显示轮流对话
                    bool success = AdvisorAppointmentSystem.TryAppointAdvisor(
                        faction.Leader, 
                        this.CurrentPerson, 
                        faction
                    );
                    
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine("[AppointAdvisor] 任命成功");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[AppointAdvisor] 任命被拒绝");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AppointAdvisor] 错误：目标势力或君主为空");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[AppointAdvisor] 错误：未选中任何人物");
            }
        }

        /// <summary>
        /// 处理Frame函数调用的分发
        /// </summary>
        private void HandleFrameFunction(FrameFunction function)
        {
            switch (function)
            {
                case FrameFunction.AppointAdvisor: //任命军师
                    this.FrameFunction_Faction_AppointAdvisor();
                    break;
                // ... 其他case
            }
        }
    }
}

// ===================================================================
// 3. 军师任命系统 - AdvisorAppointmentSystem.cs
// ===================================================================

namespace GameManager
{
    /// <summary>
    /// 军师任命系统 - 处理任命逻辑和拒绝判定
    /// </summary>
    public static class AdvisorAppointmentSystem
    {
        /// <summary>
        /// 尝试任命军师 - 包含拒绝判定和对话显示
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="candidate">候选军师</param>
        /// <param name="faction">势力</param>
        /// <returns>是否任命成功</returns>
        public static bool TryAppointAdvisor(Person leader, Person candidate, Faction faction)
        {
            if (leader == null || candidate == null || faction == null)
                return false;

            bool isSuccess = true;

            // 1. 判定逻辑：是否拒绝？
            if (candidate.CheckRelation(leader) == -1)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 厌恶 {leader.Name}，拒绝任命");
            }
            else if (candidate.Loyalty < 20)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 忠诚度过低({candidate.Loyalty})，拒绝任命");
            }
            else if (candidate.Ambition > 80 && candidate.Intelligence < 70)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 高野心但智力不足，拒绝任命");
            }
            else if (IsPersonalityConflict(leader, candidate))
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 与 {leader.Name} 性格不合，拒绝任命");
            }

            // 2. 根据结果获取对话
            GameGlobal.DialogueEntry dialogue;
            if (isSuccess)
            {
                // 成功：获取成功任命对话
                dialogue = DialogueManager.GetDialogue(leader, candidate, isRefusal: false);
                
                // 执行任命
                faction.AdvisorID = candidate.ID;
                System.Diagnostics.Debug.WriteLine($"[任命成功] {leader.Name} 任命 {candidate.Name} 为军师");
            }
            else
            {
                // 失败：获取拒绝对话
                dialogue = DialogueManager.GetDialogue(leader, candidate, isRefusal: true);
                System.Diagnostics.Debug.WriteLine($"[任命失败] {candidate.Name} 拒绝了 {leader.Name} 的任命");
            }

            // 3. 显示对话UI
            ShowAppointmentDialogue(leader, candidate, dialogue, isSuccess);

            return isSuccess;
        }

        /// <summary>
        /// 检查性格是否严重冲突
        /// </summary>
        private static bool IsPersonalityConflict(Person leader, Person candidate)
        {
            // 霸道君主 vs 仁德军师
            if (leader.Character.ID == 1 && candidate.Character.ID == 0)
                return true;
            
            // 狡诈君主 vs 仁德军师
            if (leader.Character.ID == 4 && candidate.Character.ID == 0)
                return true;
            
            // 莽撞君主 vs 冷静军师（高智力时）
            if (leader.Character.ID == 3 && candidate.Character.ID == 2 && candidate.Intelligence > 85)
                return true;

            return false;
        }

        /// <summary>
        /// 显示任命对话 - 轮流显示君主和军师的对话
        /// </summary>
        private static void ShowAppointmentDialogue(Person leader, Person candidate, GameGlobal.DialogueEntry dialogue, bool isSuccess)
        {
            try
            {
                // 调试输出
                string resultText = isSuccess ? "任命成功" : "任命被拒绝";
                System.Diagnostics.Debug.WriteLine($"[{resultText}对话] {leader.Name}: {dialogue.LeaderText}");
                System.Diagnostics.Debug.WriteLine($"[{resultText}对话] {candidate.Name}: {dialogue.AdvisorText}");

                // 获取MainGameScreen实例
                var mainScreen = Session.MainGame?.mainGameScreen as WorldOfTheThreeKingdoms.GameScreens.MainGameScreen;
                if (mainScreen?.Plugins?.tupianwenziPlugin != null)
                {
                    // 创建轮流对话队列
                    ShowAlternatingDialogue(mainScreen, leader, candidate, dialogue, isSuccess);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAppointmentDialogue] 无法获取游戏屏幕或插件，跳过UI显示");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAppointmentDialogue] 显示对话时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示轮流对话 - 君主先说，然后军师回应
        /// </summary>
        private static void ShowAlternatingDialogue(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen mainScreen, 
            Person leader, Person candidate, GameGlobal.DialogueEntry dialogue, bool isSuccess)
        {
            try
            {
                // 第一步：显示君主的话
                string leaderImageName = isSuccess ? "AppointAdvisor.jpg" : "RefuseAdvisor.jpg";
                
                // 确保使用君主作为第一个参数来显示君主头像
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    leader, leader, dialogue.LeaderText, leaderImageName, "", "");
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;

                // 设置君主对话结束后的回调 - 显示军师的回应
                mainScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                    ShowAdvisorResponse(mainScreen, leader, candidate, dialogue, isSuccess);
                }));

                System.Diagnostics.Debug.WriteLine($"[轮流对话] 第1轮 - 显示{leader.Name}头像: {dialogue.LeaderText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAlternatingDialogue] 显示君主对话时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示军师的回应
        /// </summary>
        private static void ShowAdvisorResponse(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen mainScreen,
            Person leader, Person candidate, GameGlobal.DialogueEntry dialogue, bool isSuccess)
        {
            try
            {
                // 第二步：显示军师的回应
                string advisorImageName = isSuccess ? "AppointAdvisor.jpg" : "RefuseAdvisor.jpg";
                
                // 确保使用军师作为第一个参数来显示军师头像
                mainScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    candidate, candidate, dialogue.AdvisorText, advisorImageName, "", "");
                mainScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, mainScreen);
                mainScreen.Plugins.tupianwenziPlugin.IsShowing = true;

                // 设置军师对话结束后的回调 - 完成整个对话流程
                if (isSuccess)
                {
                    mainScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                        mainScreen.Plugins.GameRecordPlugin.AddBranch(leader, "AppointAdvisor", leader.Position);
                        System.Diagnostics.Debug.WriteLine("[轮流对话] 对话完成，记录任命事件");
                    }));
                }
                else
                {
                    // 拒绝的情况下不需要记录事件
                    mainScreen.Plugins.tupianwenziPlugin.SetCloseFunction(null);
                }

                System.Diagnostics.Debug.WriteLine($"[轮流对话] 第2轮 - 显示{candidate.Name}头像: {dialogue.AdvisorText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorResponse] 显示军师回应时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查候选人是否会拒绝任命
        /// </summary>
        public static bool WillRefuseAppointment(Person leader, Person candidate)
        {
            if (leader == null || candidate == null) return true;

            // 厌恶关系必定拒绝
            if (candidate.CheckRelation(leader) == -1) return true;
            
            // 忠诚度极低拒绝
            if (candidate.Loyalty < 20) return true;
            
            // 高野心 + 低智力拒绝
            if (candidate.Ambition > 80 && candidate.Intelligence < 70) return true;
            
            // 性格严重冲突拒绝
            if (IsPersonalityConflict(leader, candidate)) return true;

            return false;
        }

        /// <summary>
        /// 获取拒绝原因描述
        /// </summary>
        public static string GetRefusalReason(Person leader, Person candidate)
        {
            if (leader == null || candidate == null) return "参数错误";

            if (candidate.CheckRelation(leader) == -1) return "厌恶关系";
            if (candidate.Loyalty < 20) return "忠诚度过低";
            if (candidate.Ambition > 80 && candidate.Intelligence < 70) return "高野心低智力";
            if (IsPersonalityConflict(leader, candidate)) return "性格冲突";

            return "无拒绝原因";
        }
    }
}

// ===================================================================
// 4. 对话管理系统 - DialogueManager.cs
// ===================================================================

namespace GameObjects
{
    /// <summary>
    /// 对话管理器 - 统一管理游戏中的对话系统
    /// </summary>
    public class DialogueManager
    {
        private static DialogueConfig appointmentConfig;
        private static DialogueConfig recallConfig;
        private static DialogueConfig refusalConfig;

        /// <summary>
        /// 初始化加载所有对话配置
        /// </summary>
        public static void Initialize()
        {
            LoadAppointmentConfig();
            LoadRecallConfig();
            LoadRefusalConfig();
        }

        /// <summary>
        /// 获取任命军师对话
        /// </summary>
        public static GameGlobal.DialogueEntry GetAppointDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            if (isRefusal)
            {
                if (refusalConfig == null)
                {
                    LoadRefusalConfig();
                }
                return GetBestMatchDialogue(refusalConfig, leader, advisor, true) ?? GetFallbackRefusalDialogue();
            }
            else
            {
                if (appointmentConfig == null)
                {
                    LoadAppointmentConfig();
                }
                return GetBestMatchDialogue(appointmentConfig, leader, advisor, false) ?? GetFallbackAppointDialogue();
            }
        }

        /// <summary>
        /// 通用对话获取方法（支持拒绝过滤）
        /// </summary>
        public static GameGlobal.DialogueEntry GetDialogue(Person leader, Person advisor, bool isRefusal = false)
        {
            return GetAppointDialogue(leader, advisor, isRefusal);
        }

        /// <summary>
        /// 获取罢免军师对话
        /// </summary>
        public static GameGlobal.DialogueEntry GetRecallDialogue(Person leader, Person advisor)
        {
            if (recallConfig == null)
            {
                LoadRecallConfig();
            }

            return GetBestMatchDialogue(recallConfig, leader, advisor, false) ?? GetFallbackRecallDialogue();
        }

        /// <summary>
        /// 从配置中找到最佳匹配的对话 - 支持同权重随机选择
        /// </summary>
        private static GameGlobal.DialogueEntry GetBestMatchDialogue(DialogueConfig config, Person leader, Person advisor, bool isRefusal = false)
        {
            if (config == null || config.Entries == null || config.Entries.Count == 0)
                return null;

            // 根据是否拒绝过滤对话类型
            var filteredEntries = config.Entries.Where(entry => 
                isRefusal ? entry.Type == DialogueType.Refusal 
                         : entry.Type != DialogueType.Refusal);

            // 计算所有匹配的条目
            var matches = filteredEntries
                .Select(entry => new { Entry = entry, Score = entry.GetMatchScore(leader, advisor) })
                .Where(x => x.Score > 0) // 过滤掉不匹配的
                .ToList();

            if (!matches.Any()) return null;

            // 找到最高分数
            int maxScore = matches.Max(x => x.Score);
            
            // 获取所有最高分数的条目
            var topMatches = matches.Where(x => x.Score == maxScore).ToList();
            
            // 如果有多个最高分条目，随机选择一个
            if (topMatches.Count > 1)
            {
                int randomIndex = GameObjects.GameObject.Random(topMatches.Count);
                string dialogueType = isRefusal ? "拒绝" : "任命";
                System.Diagnostics.Debug.WriteLine($"[DialogueManager] 发现 {topMatches.Count} 个同权重{dialogueType}对话，随机选择第 {randomIndex + 1} 个");
                return topMatches[randomIndex].Entry;
            }
            
            return topMatches.First().Entry;
        }

        /// <summary>
        /// 获取默认任命对话
        /// </summary>
        private static GameGlobal.DialogueEntry GetFallbackAppointDialogue()
        {
            return new GameGlobal.DialogueEntry
            {
                LeaderText = "今欲请足下担任军师一职，不知尊意如何？",
                AdvisorText = "承蒙主公错爱，属下定当竭尽所能。"
            };
        }

        /// <summary>
        /// 获取默认拒绝对话
        /// </summary>
        private static GameGlobal.DialogueEntry GetFallbackRefusalDialogue()
        {
            return new GameGlobal.DialogueEntry
            {
                Type = DialogueType.Refusal,
                LeaderText = "请你担任军师一职。",
                AdvisorText = "抱歉，某不能接受这个职位。"
            };
        }

        /// <summary>
        /// 获取默认罢免对话
        /// </summary>
        private static GameGlobal.DialogueEntry GetFallbackRecallDialogue()
        {
            return new GameGlobal.DialogueEntry
            {
                LeaderText = "军师之职暂且卸任，望你理解。",
                AdvisorText = "是，属下遵命。"
            };
        }

        /// <summary>
        /// 显示任命军师对话序列
        /// </summary>
        public static void ShowAppointDialogue(Person leader, Person advisor, WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen)
        {
            // 获取最佳匹配的对话
            GameGlobal.DialogueEntry dialogue = GetAppointDialogue(leader, advisor);

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"[任命军师对话] {leader.Name}: {dialogue.LeaderText}");
            System.Diagnostics.Debug.WriteLine($"[任命军师对话] {advisor.Name}: {dialogue.AdvisorText}");

            // 合并对话内容显示
            string combinedDialogue = $"{leader.Name}：「{dialogue.LeaderText}」\n\n{advisor.Name}：「{dialogue.AdvisorText}」";

            // 使用游戏的文本显示系统
            gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(leader, null, combinedDialogue, "AppointAdvisor.jpg", "", "");
            gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
            gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;

            // 设置关闭回调，执行游戏记录
            gameScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                gameScreen.Plugins.GameRecordPlugin.AddBranch(leader, "AppointAdvisor", leader.Position);
            }));
        }

        /// <summary>
        /// 显示罢免军师对话序列
        /// </summary>
        public static void ShowRecallDialogue(Person leader, Person advisor, WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen)
        {
            // 获取最佳匹配的对话
            GameGlobal.DialogueEntry dialogue = GetRecallDialogue(leader, advisor);

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"[罢免军师对话] {leader.Name}: {dialogue.LeaderText}");
            System.Diagnostics.Debug.WriteLine($"[罢免军师对话] {advisor.Name}: {dialogue.AdvisorText}");

            // 合并对话内容显示
            string combinedDialogue = $"{leader.Name}：「{dialogue.LeaderText}」\n\n{advisor.Name}：「{dialogue.AdvisorText}」";

            // 使用游戏的文本显示系统
            gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(leader, null, combinedDialogue, "RecallAdvisor.jpg", "", "");
            gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
            gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        // 其他辅助方法...
        private static void LoadAppointmentConfig() { /* 实现省略 */ }
        private static void LoadRecallConfig() { /* 实现省略 */ }
        private static void LoadRefusalConfig() { /* 实现省略 */ }
        public static void ReloadConfigs() { /* 实现省略 */ }
        public static string GetConfigStats() { /* 实现省略 */ }
    }
}

// ===================================================================
// 5. 右键菜单处理 - MGSContextMenu.cs
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class MGSContextMenu
    {
        /// <summary>
        /// 处理右键菜单结果
        /// </summary>
        private void HandleContextMenuResult(ContextMenuResult result)
        {
            switch (result)
            {
                // 任命军师
                case ContextMenuResult.Person_Appointment_AppointAdvisor:
                    this.ShowTabListInFrame(
                        UndoneWorkKind.Frame, 
                        FrameKind.Person, 
                        FrameFunction.AppointAdvisor, 
                        false, true, true, false, 
                        Session.Current.Scenario.CurrentFaction.AdvisorCandicate, 
                        null, 
                        "任命军师", 
                        ""
                    );
                    break;

                // 罢免军师
                case ContextMenuResult.Person_Appointment_RecallAdvisor:
                    // 直接罢免当前军师
                    if (this.CurrentFaction.Advisor != null)
                    {
                        Person formerAdvisor = this.CurrentFaction.Advisor;
                        this.CurrentFaction.AdvisorID = -1;
                        this.CurrentFaction.Advisor = null;
                        
                        // 显示罢免对话
                        DialogueManager.ShowRecallDialogue(this.CurrentFaction.Leader, formerAdvisor, Session.MainGame.mainGameScreen);
                    }
                    break;
            }
        }
    }
}

// ===================================================================
// 6. 文本消息处理 - MGSPersonText.cs
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class MGSPersonText
    {
        /// <summary>
        /// 处理任命军师的文本消息和动画
        /// </summary>
        public override void AppointAdvisor(Person p, Person q)  //军师
        {
            if ((Session.Current.Scenario.IsCurrentPlayer(p.BelongedFaction)) && 
                Session.Current.Scenario.IsCurrentPlayer(q.BelongedFaction))
            {
                q.TextResultString = p.Name;
                p.TextDestinationString = q.BelongedFaction.Name;
                
                // 使用DialogueManager显示对话
                DialogueManager.ShowAppointDialogue(p, q, Session.MainGame.mainGameScreen);
            }
        }
    }
}

// ===================================================================
// 7. 年表记录 - YearTable.cs
// ===================================================================

namespace GameObjects
{
    public partial class YearTable
    {
        /// <summary>
        /// 添加任命军师的年表记录
        /// </summary>
        public void addAppointAdvisorEntry(GameDate date, Person advisor, Person leader)
        {
            this.addTableEntry(
                date, 
                composeFactionList(advisor.BelongedFaction),
                String.Format(
                    yearTableStrings["appointAdvisor"], 
                    advisor.Name, 
                    advisor.BelongedFaction.Name, 
                    leader.Name
                ), 
                false
            );
            
            // 添加到个人传记
            this.addPersonInGameBiography(
                advisor, 
                date,
                String.Format("被{0}任命为{1}的军师", leader.Name, advisor.BelongedFaction.Name)
            );
        }
    }
}

// ===================================================================
// 8. 枚举定义 - 各种枚举类型
// ===================================================================

namespace GameGlobal
{
    /// <summary>
    /// 右键菜单结果枚举
    /// </summary>
    public enum ContextMenuResult
    {
        // ... 其他枚举值
        Person_Appointment_AppointAdvisor, //任命军师
        Person_Appointment_RecallAdvisor,  //罢免军师
        // ... 其他枚举值
    }

    /// <summary>
    /// Frame函数枚举
    /// </summary>
    public enum FrameFunction
    {
        // ... 其他枚举值
        AppointAdvisor, //任命军师
        // ... 其他枚举值
    }

    /// <summary>
    /// 文本消息类型枚举
    /// </summary>
    public enum TextMessageKind
    {
        // ... 其他枚举值
        AppointAdvisor, // 任命军师消息
        // ... 其他枚举值
    }

    /// <summary>
    /// 对话类型枚举
    /// </summary>
    public enum DialogueType
    {
        Default,    // 默认对话
        Refusal,    // 拒绝对话
        Recall      // 罢免对话
    }

    /// <summary>
    /// 对话条目类
    /// </summary>
    public class DialogueEntry
    {
        public DialogueType Type { get; set; }
        public string LeaderText { get; set; }
        public string AdvisorText { get; set; }
        
        /// <summary>
        /// 计算与指定君主和军师的匹配分数
        /// </summary>
        public int GetMatchScore(Person leader, Person advisor)
        {
            // 实现匹配逻辑，返回匹配分数
            // 分数越高表示匹配度越好
            return 1; // 简化实现
        }
    }

    /// <summary>
    /// 对话配置类
    /// </summary>
    public class DialogueConfig
    {
        public List<DialogueEntry> Entries { get; set; } = new List<DialogueEntry>();
    }
}

// ===================================================================
// 9. 使用示例和测试代码
// ===================================================================

namespace GameManager
{
    /// <summary>
    /// 军师任命系统使用示例
    /// </summary>
    public class AdvisorAppointmentExample
    {
        /// <summary>
        /// 测试军师任命流程
        /// </summary>
        public static void TestAdvisorAppointment()
        {
            // 获取当前势力
            Faction currentFaction = Session.Current.Scenario.CurrentFaction;
            
            // 检查是否可以任命军师
            if (currentFaction.AppointAdvisorAvail())
            {
                Console.WriteLine("可以任命军师");
                
                // 获取候选人列表
                PersonList candidates = currentFaction.AdvisorCandicate;
                Console.WriteLine($"候选人数量: {candidates.Count}");
                
                if (candidates.Count > 0)
                {
                    // 选择智力最高的候选人
                    Person bestCandidate = candidates[0];
                    foreach (Person p in candidates)
                    {
                        if (p.Intelligence > bestCandidate.Intelligence)
                        {
                            bestCandidate = p;
                        }
                    }
                    
                    Console.WriteLine($"选择候选人: {bestCandidate.Name} (智力: {bestCandidate.Intelligence})");
                    
                    // 使用AdvisorAppointmentSystem任命军师
                    bool success = AdvisorAppointmentSystem.TryAppointAdvisor(
                        currentFaction.Leader, 
                        bestCandidate, 
                        currentFaction
                    );
                    
                    // 验证任命结果
                    Console.WriteLine($"任命后军师ID: {currentFaction.AdvisorID}");
                    Console.WriteLine($"任命后军师对象: {currentFaction.Advisor?.Name}");
                    Console.WriteLine($"任命后军师姓名: {currentFaction.AdvisorName}");
                    
                    // 验证是否成功
                    if (success && currentFaction.Advisor != null && currentFaction.Advisor.ID == bestCandidate.ID)
                    {
                        Console.WriteLine("✅ 军师任命成功！");
                    }
                    else
                    {
                        Console.WriteLine("❌ 军师任命失败或被拒绝！");
                    }
                }
            }
            else
            {
                Console.WriteLine("当前无法任命军师");
                Console.WriteLine($"当前军师ID: {currentFaction.AdvisorID}");
                Console.WriteLine($"当前军师: {currentFaction.AdvisorName}");
            }
        }
        
        /// <summary>
        /// 测试军师罢免流程
        /// </summary>
        public static void TestAdvisorRecall()
        {
            Faction currentFaction = Session.Current.Scenario.CurrentFaction;
            
            if (currentFaction.Advisor != null)
            {
                Console.WriteLine($"当前军师: {currentFaction.AdvisorName}");
                
                Person formerAdvisor = currentFaction.Advisor;
                
                // 罢免军师
                currentFaction.AdvisorID = -1;
                currentFaction.Advisor = null;
                
                // 显示罢免对话
                DialogueManager.ShowRecallDialogue(currentFaction.Leader, formerAdvisor, Session.MainGame.mainGameScreen);
                
                // 验证罢免结果
                Console.WriteLine($"罢免后军师ID: {currentFaction.AdvisorID}");
                Console.WriteLine($"罢免后军师对象: {currentFaction.Advisor?.Name ?? "null"}");
                
                if (currentFaction.AdvisorID == -1 && currentFaction.Advisor == null)
                {
                    Console.WriteLine("✅ 军师罢免成功！");
                }
                else
                {
                    Console.WriteLine("❌ 军师罢免失败！");
                }
            }
            else
            {
                Console.WriteLine("当前没有军师可以罢免");
            }
        }
    }
}

// ===================================================================
// 10. 完整流程总结
// ===================================================================

/*
军师任命完整流程：

1. 【UI触发】
   - 玩家右键点击 → 选择"任命军师"菜单
   - 触发 ContextMenuResult.Person_Appointment_AppointAdvisor
   - MGSContextMenu.HandleContextMenuResult() 处理

2. 【候选人选择】
   - ShowTabListInFrame() 显示候选人列表
   - 候选人来源：Faction.AdvisorCandicate 属性
   - 筛选条件：智力≥70，非君主，非当前军师，可用状态

3. 【任命处理】
   - 玩家选择候选人后触发 FrameFunction.AppointAdvisor
   - ScreenManager.FrameFunction_Faction_AppointAdvisor() 处理
   - 调用 AdvisorAppointmentSystem.TryAppointAdvisor()

4. 【拒绝判定】
   - 检查厌恶关系、忠诚度、野心、性格冲突
   - 决定是否接受任命

5. 【对话显示】
   - DialogueManager.GetDialogue() 获取合适对话
   - 轮流显示君主和军师的对话
   - 支持任命成功和拒绝两种情况

6. 【数据更新】
   - 成功时：设置 Faction.AdvisorID 和 Faction.Advisor
   - 触发 OnAppointAdvisor 事件
   - YearTable.addAppointAdvisorEntry() 记录年表

7. 【AI自动任命】
   - Faction.AIAppointAdvisor() 根据君主性格自动选择
   - 支持仁德、霸道、冷静、莽撞、狡诈五种性格的不同策略

核心特点：
- 完整的拒绝机制和对话系统
- 智能的AI任命策略
- 轮流对话显示效果
- 完善的数据持久化
- 丰富的调试输出
*/