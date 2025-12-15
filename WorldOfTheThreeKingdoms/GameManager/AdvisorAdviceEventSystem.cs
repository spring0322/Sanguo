using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameManager;
using GameGlobal;

namespace GameManager
{
    /// <summary>
    /// 军师谏言事件系统 - 每回合自动弹窗显示谏言
    /// </summary>
    public static class AdvisorAdviceEventSystem
    {
        // 缓存已警告的敌军，避免重复报告
        private static HashSet<int> _warnedEnemyTroops = new HashSet<int>();
        
        // 记录上次检查的日期，避免同一天重复触发
        private static int _lastCheckDay = -1;
        
        /// <summary>
        /// 每日检查军师谏言（在Date_OnDayStarting中调用）
        /// </summary>
        /// <param name="gameScreen">主游戏屏幕</param>
        public static void CheckDailyAdvice(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen)
        {
            try
            {
                if (gameScreen == null || Session.Current?.Scenario?.CurrentPlayer == null)
                    return;

                // 检查是否已经处理过今天的谏言
                int currentDay = Session.Current.Scenario.Date.Day;
                if (_lastCheckDay == currentDay)
                    return;

                _lastCheckDay = currentDay;

                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction.Advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 无军师，跳过谏言检查");
                    return;
                }

                // 按优先级检查谏言
                var suggestion = GetHighestPriorityAdvice(faction);
                if (suggestion != AdvisorSuggestionKind.None)
                {
                    // 对于AI势力，检查君主是否听从军师建议
                    if (!Session.Current.Scenario.IsPlayer(faction))
                    {
                        bool willListen = faction.AICheckListenToAdvisor();
                        if (!willListen)
                        {
                            System.Diagnostics.Debug.WriteLine($"[军师谏言] AI君主 {faction.Leader.Name} 拒绝听从军师 {faction.Advisor.Name} 的建议");
                            return;
                        }
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] AI君主 {faction.Leader.Name} 决定听从军师 {faction.Advisor.Name} 的建议");
                        
                        // AI自动执行建议，不显示界面
                        ExecuteAIAdvice(faction, suggestion);
                        return;
                    }

                    // 玩家势力显示谏言界面
                    ShowAdviceEvent(gameScreen, faction, suggestion);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师谏言事件] 检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取最高优先级的建议
        /// </summary>
        private static AdvisorSuggestionKind GetHighestPriorityAdvice(Faction faction)
        {
            // 1. 优先检查敌情 (智力越高，预警范围越大)
            if (CheckEnemyApproachingWithCache(faction, faction.Advisor.Intelligence))
                return AdvisorSuggestionKind.EnemyAttack;

            // 2. 检查是否有强力在野武将
            if (AdvisorSuggestionSystem.CheckUnfoundPerson(faction))
                return AdvisorSuggestionKind.PersonRecruit;

            // 3. 检查忠诚度 (智力低可能漏报)
            if (AdvisorSuggestionSystem.CheckLoyaltyIssues(faction) && 
                GameObjects.GameObject.Random(100) < faction.Advisor.Intelligence)
                return AdvisorSuggestionKind.LoyaltyWarning;

            return AdvisorSuggestionKind.None;
        }

        /// <summary>
        /// 带缓存的敌军检查，避免重复报告同一支敌军
        /// </summary>
        private static bool CheckEnemyApproachingWithCache(Faction faction, int advisorIntelligence)
        {
            if (faction?.Architectures == null) return false;

            int detectionRange = advisorIntelligence / 10;
            
            foreach (var architecture in faction.Architectures.GetList().Cast<Architecture>())
            {
                var nearbyTroops = Session.Current?.Scenario?.Troops?.GetList()
                    ?.Cast<Troop>()
                    ?.Where(t => t.BelongedFaction != faction && 
                               Math.Abs(t.Position.X - architecture.Position.X) <= detectionRange &&
                               Math.Abs(t.Position.Y - architecture.Position.Y) <= detectionRange);
                
                if (nearbyTroops?.Any() == true)
                {
                    // 检查是否有新的敌军（未警告过的）
                    foreach (var troop in nearbyTroops)
                    {
                        if (!_warnedEnemyTroops.Contains(troop.ID))
                        {
                            // 记录这支敌军已被警告
                            _warnedEnemyTroops.Add(troop.ID);
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// AI自动执行军师建议
        /// </summary>
        private static void ExecuteAIAdvice(Faction faction, AdvisorSuggestionKind suggestion)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AI军师谏言] {faction.Leader.Name} 开始执行 {faction.Advisor.Name} 的建议: {suggestion}");
                
                switch (suggestion)
                {
                    case AdvisorSuggestionKind.EnemyAttack:
                        // AI收到敌军预警，可能会调整防御策略
                        System.Diagnostics.Debug.WriteLine($"[AI军师谏言] {faction.Name} 收到敌军预警，加强防备");
                        // 这里可以添加AI防御逻辑，比如：
                        // - 召回在外部队
                        // - 加强城防
                        // - 准备应战
                        break;
                        
                    case AdvisorSuggestionKind.PersonRecruit:
                        // AI自动招募推荐的武将
                        ExecuteAIRecruitment(faction);
                        break;
                        
                    case AdvisorSuggestionKind.LoyaltyWarning:
                        // AI处理忠诚度问题
                        ExecuteAILoyaltyManagement(faction);
                        break;
                        
                    default:
                        System.Diagnostics.Debug.WriteLine($"[AI军师谏言] 未知建议类型: {suggestion}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI军师谏言] 执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// AI自动执行招募
        /// </summary>
        private static void ExecuteAIRecruitment(Faction faction)
        {
            try
            {
                var recommendedPersons = GetRecommendedPersons();
                if (recommendedPersons?.GameObjects?.Any() == true)
                {
                    // AI选择能力最高的武将进行招募
                    var bestPerson = recommendedPersons.GameObjects
                        .Cast<Person>()
                        .OrderByDescending(p => p.Command + p.Intelligence + p.Politics + p.Glamour)
                        .FirstOrDefault();
                    
                    if (bestPerson != null && bestPerson.BelongedFaction == null)
                    {
                        bool success = PerformRecruitment(bestPerson, faction);
                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AI军师谏言] {faction.Name} 成功招募了 {bestPerson.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[AI军师谏言] {faction.Name} 招募 {bestPerson.Name} 失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI招募] 执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// AI自动处理忠诚度问题
        /// </summary>
        private static void ExecuteAILoyaltyManagement(Faction faction)
        {
            try
            {
                var lowLoyaltyPersons = GetLowLoyaltyPersons(faction);
                if (lowLoyaltyPersons?.GameObjects?.Any() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI军师谏言] {faction.Name} 发现 {lowLoyaltyPersons.Count} 名低忠诚武将，开始处理");
                    
                    // AI可以采取的措施：
                    // 1. 褒奖低忠诚武将
                    // 2. 调整职位
                    // 3. 赐予宝物
                    // 这里只是记录，具体实现需要根据游戏的AI系统来扩展
                    
                    foreach (Person person in lowLoyaltyPersons.GameObjects.Cast<Person>())
                    {
                        System.Diagnostics.Debug.WriteLine($"[AI忠诚度管理] 关注武将: {person.Name} (忠诚度: {person.Loyalty})");
                        // 这里可以添加具体的AI处理逻辑
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI忠诚度管理] 执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示谏言事件弹窗
        /// </summary>
        private static void ShowAdviceEvent(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, 
            Faction faction, AdvisorSuggestionKind suggestion)
        {
            try
            {
                var advisor = faction.Advisor;
                
                // 检查建言准确性
                bool isAccurate = faction.IsAdviceAccurate();
                
                string adviceText = GetAdviceEventText(advisor, suggestion, isAccurate);
                string imageName = GetAdviceEventImage(suggestion);
                
                // 根据建议类型决定是否需要选择框
                if (suggestion == AdvisorSuggestionKind.PersonRecruit || suggestion == AdvisorSuggestionKind.LoyaltyWarning)
                {
                    // 需要选择的建议 - 显示确认对话框
                    gameScreen.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                        gameScreen.Plugins.ConfirmationDialogPlugin,
                        new GameDelegates.VoidFunction(() => HandleAdviceCallback(gameScreen, faction, suggestion, advisor)),
                        null  // 选择"否"时不执行任何操作
                    );
                    gameScreen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                }
                
                // 使用事件系统显示谏言
                advisor.TextResultString = adviceText;  // 设置文本内容
                gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, adviceText, imageName, "", "");
                gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
                gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;

                // 对于不需要选择的建议（如敌军预警），直接设置关闭回调
                if (suggestion == AdvisorSuggestionKind.EnemyAttack)
                {
                    gameScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                        System.Diagnostics.Debug.WriteLine("[军师谏言] 敌军预警完成");
                    }));
                }

                System.Diagnostics.Debug.WriteLine($"[军师谏言事件] {advisor.Name}: {adviceText} (准确性: {isAccurate})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师谏言事件] 显示失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理谏言对话结束后的回调
        /// </summary>
        private static void HandleAdviceCallback(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen,
            Faction faction, AdvisorSuggestionKind suggestion, Person advisor)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[军师谏言回调] 开始处理回调，建议类型: {suggestion}");
                
                switch (suggestion)
                {
                    case AdvisorSuggestionKind.PersonRecruit:
                        System.Diagnostics.Debug.WriteLine("[军师谏言回调] 处理人才招募建议");
                        // 跳转到招募界面并推荐人选
                        ShowRecruitmentInterface(gameScreen, faction);
                        break;
                        
                    case AdvisorSuggestionKind.LoyaltyWarning:
                        System.Diagnostics.Debug.WriteLine("[军师谏言回调] 处理忠诚度警告");
                        // 跳转到低忠诚武将详情
                        ShowLoyaltyWarningInterface(gameScreen, faction);
                        break;
                        
                    case AdvisorSuggestionKind.EnemyAttack:
                        // 敌军预警不需要特殊跳转，只是提醒
                        System.Diagnostics.Debug.WriteLine("[军师谏言] 敌军预警完成");
                        break;
                        
                    default:
                        System.Diagnostics.Debug.WriteLine($"[军师谏言回调] 未知建议类型: {suggestion}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师谏言回调] 处理失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[军师谏言回调] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 显示招募界面并推荐人选
        /// </summary>
        private static void ShowRecruitmentInterface(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, Faction faction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[军师谏言] 开始显示招募界面");
                
                // 获取推荐的在野武将
                var recommendedPersons = GetRecommendedPersons();
                
                if (recommendedPersons?.GameObjects?.Any() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[军师谏言] 找到 {recommendedPersons.GameObjects.Count} 名推荐武将");
                    
                    // 确保关闭所有当前显示的对话框和界面
                    try
                    {
                        // 1. 安全关闭已有插件（这会自动正确处理栈）
                        if (gameScreen.Plugins.tupianwenziPlugin.IsShowing)
                        {
                            gameScreen.Plugins.tupianwenziPlugin.IsShowing = false;
                        }
                        
                        if (gameScreen.Plugins.ConfirmationDialogPlugin.IsShowing)
                        {
                            gameScreen.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        }
                        
                        // 【删除】原来的 while (cleanupCount < 10) 循环！
                        // 理由：手动 Pop 容易导致栈偏移，引发 "The UndoneWork is not a Frame" 错误。
                    }
                    catch (Exception cleanupEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 清理界面时发生异常: {cleanupEx.Message}");
                        // 继续执行，不要因为清理失败而中断
                    }
                    
                    // 尝试显示招募界面，如果失败则回退到自动招募
                    try
                    {
                        // 2. 设置单选模式 - 每次只能选择一个武将
                        if (gameScreen.Plugins.TabListPlugin != null)
                        {
                            gameScreen.Plugins.TabListPlugin.SetSelectedItemMaxCount(1);
                            System.Diagnostics.Debug.WriteLine("[军师谏言] 设置为单选模式");
                        }
                        
                        // 3. 显示招募人物列表界面
                        // 注意：第一个参数务必保持 UndoneWorkKind.Frame
                        gameScreen.ShowTabListInFrame(
                            UndoneWorkKind.Frame,            // 工作类型
                            FrameKind.Person,                // 显示类型：人物列表
                            FrameFunction.PersonManualHire,  // 功能：手动招募
                            false,                           // OKEnabled: 禁用默认确定按钮
                            true,                            // CancelEnabled: 启用取消按钮
                            true,                            // showCheckBox: 显示选择框
                            false,                           // multiselecting: 单选模式
                            recommendedPersons,              // 显示的人物列表
                            null,                            // 预选列表
                            "军师推荐招募",                   // 界面标题
                            "Ability"                        // 排序方式：按能力排序
                        );
                        
                        System.Diagnostics.Debug.WriteLine("[军师谏言] ShowTabListInFrame调用成功");
                        
                        // 4. 设置确认按钮的回调函数
                        if (gameScreen.Plugins.GameFramePlugin != null)
                        {
                            gameScreen.Plugins.GameFramePlugin.SetOKFunction(new GameDelegates.VoidFunction(() => {
                                HandleRecruitmentConfirm(gameScreen, faction);
                            }));
                            System.Diagnostics.Debug.WriteLine("[军师谏言] 设置招募确认回调");
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[军师谏言] 招募界面显示成功");
                    }
                    catch (Exception showEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] ShowTabListInFrame调用失败: {showEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 堆栈跟踪: {showEx.StackTrace}");
                        
                        // 如果显示招募界面失败，回退到自动招募
                        System.Diagnostics.Debug.WriteLine("[军师谏言] 回退到自动招募模式");
                        
                        // 自动招募能力最高的武将
                        var bestPerson = recommendedPersons.GameObjects
                            .Cast<Person>()
                            .OrderByDescending(p => p.Command + p.Intelligence + p.Politics + p.Glamour)
                            .FirstOrDefault();
                        
                        if (bestPerson != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[军师谏言] 自动选择最佳武将: {bestPerson.Name}");
                            
                            // 直接执行招募，不显示选择界面
                            bool recruitSuccess = PerformRecruitment(bestPerson, faction);
                            
                            if (recruitSuccess)
                            {
                                System.Diagnostics.Debug.WriteLine($"[军师谏言] 自动招募成功: {bestPerson.Name}");
                                ShowRecruitmentResult(gameScreen, faction, bestPerson, true, "");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[军师谏言] 自动招募失败: {bestPerson.Name}");
                                ShowRecruitmentResult(gameScreen, faction, bestPerson, false, "招募失败");
                            }
                        }
                        
                        return; // 提前返回，不执行后续代码
                    }
                    
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 招募界面调用完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 未找到可推荐的武将");
                    
                    // 显示提示信息
                    try
                    {
                        gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                            faction.Advisor, faction.Advisor, 
                            $"军师 {faction.Advisor.Name}：\n\n主公，经过仔细搜寻，暂时未发现合适的在野贤才。", 
                            "", "", "");
                        gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
                        gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                    }
                    catch (Exception msgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 显示提示消息时发生异常: {msgEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募界面] 显示失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[招募界面] 堆栈跟踪: {ex.StackTrace}");
                
                // 最后的备用错误处理
                try
                {
                    if (faction?.Advisor != null)
                    {
                        gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                            faction.Advisor, faction.Advisor, 
                            $"军师 {faction.Advisor.Name}：\n\n主公，臣在执行任务时遇到了意外困难，请稍后再试。", 
                            "", "", "");
                        gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
                        gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 连备用错误处理都失败了");
                }
            }
        }

        /// <summary>
        /// 处理招募确认操作
        /// </summary>
        private static void HandleRecruitmentConfirm(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, Faction faction)
        {
            try
            {
                // 获取选中的人
                Person selectedPerson = gameScreen.Plugins.TabListPlugin?.SelectedItem as Person;
                
                if (selectedPerson != null)
                {
                    // 执行招募
                    bool recruitSuccess = PerformRecruitment(selectedPerson, faction);
                    
                    // 重要：在显示结果前，先让当前的人物列表 Frame 正常关闭
                    // GameFrame 的 DoOK 逻辑通常会自动处理 PopUndoneWork，
                    // 此时我们直接调用结果显示即可。
                    ShowRecruitmentResult(gameScreen, faction, selectedPerson, recruitSuccess, 
                        recruitSuccess ? "" : "拒绝加入");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师谏言] 回调异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行招募操作
        /// </summary>
        private static bool PerformRecruitment(Person person, Faction faction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[招募操作] 开始招募 {person?.Name ?? "null"} 到势力 {faction?.Name ?? "null"}");
                
                // 检查基本条件
                if (person == null || faction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[招募操作] person或faction为null");
                    return false;
                }
                
                if (person.BelongedFaction != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募操作] {person.Name} 已属于势力 {person.BelongedFaction.Name}");
                    return false;
                }
                
                if (faction.Capital == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募操作] 势力 {faction.Name} 没有首都");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"[招募操作] 招募前状态:");
                System.Diagnostics.Debug.WriteLine($"  BelongedFaction: {person.BelongedFaction?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"  Status: {person.Status}");
                System.Diagnostics.Debug.WriteLine($"  PersonalLoyalty: {person.PersonalLoyalty}");
                System.Diagnostics.Debug.WriteLine($"  Loyalty: {person.Loyalty}");
                System.Diagnostics.Debug.WriteLine($"  TempLoyaltyChange: {person.TempLoyaltyChange}");
                
                // 【步骤1】先让武将加入势力 - 这是关键！
                System.Diagnostics.Debug.WriteLine($"[招募操作] 步骤1: 将武将加入势力");
                
                // 使用MoveToArchitecture将武将移动到势力首都
                // 这会自动设置BelongedFaction属性
                person.MoveToArchitecture(faction.Capital);
                
                System.Diagnostics.Debug.WriteLine($"[招募操作] 加入势力后状态:");
                System.Diagnostics.Debug.WriteLine($"  BelongedFaction: {person.BelongedFaction?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"  Status: {person.Status}");
                System.Diagnostics.Debug.WriteLine($"  Loyalty: {person.Loyalty}");
                
                // 【步骤2】设置武将状态
                System.Diagnostics.Debug.WriteLine($"[招募操作] 步骤2: 设置武将状态");
                person.Status = GameObjects.PersonDetail.PersonStatus.Normal;
                
                // 确保位置正确
                if (faction.Advisor?.LocationArchitecture != null)
                {
                    person.LocationArchitecture = faction.Advisor.LocationArchitecture;
                    System.Diagnostics.Debug.WriteLine($"  设置位置到军师所在地: {faction.Advisor.LocationArchitecture}");
                }
                else if (faction.Leader?.LocationArchitecture != null)
                {
                    person.LocationArchitecture = faction.Leader.LocationArchitecture;
                    System.Diagnostics.Debug.WriteLine($"  设置位置到君主所在地: {faction.Leader.LocationArchitecture}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[招募操作] 设置状态后:");
                System.Diagnostics.Debug.WriteLine($"  Status: {person.Status}");
                System.Diagnostics.Debug.WriteLine($"  LocationArchitecture: {person.LocationArchitecture?.ToString() ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"  Loyalty: {person.Loyalty}");
                
                // 【步骤3】最后计算并应用忠诚度
                System.Diagnostics.Debug.WriteLine($"[招募操作] 步骤3: 计算并应用忠诚度");
                
                Person ruler = faction.Leader;
                Person recruiter = faction.Advisor ?? faction.Leader; // 军师推荐，如果没有军师则君主亲自
                
                // 计算初始忠诚度（使用军师举荐方式）
                int calculatedLoyalty = RecruitmentCalculator.CalculateInitialLoyalty(
                    ruler, recruiter, person, RecruitMethod.Recommendation);
                
                System.Diagnostics.Debug.WriteLine($"  计算出的目标忠诚度: {calculatedLoyalty}");
                
                // 现在武将已经加入势力，可以安全设置忠诚度了
                System.Diagnostics.Debug.WriteLine($"[招募操作] 开始忠诚度调整:");
                System.Diagnostics.Debug.WriteLine($"  当前忠诚度: {person.Loyalty}");
                System.Diagnostics.Debug.WriteLine($"  目标忠诚度: {calculatedLoyalty}");
                
                // 重置TempLoyaltyChange
                person.TempLoyaltyChange = 0;
                int currentLoyalty = person.Loyalty;
                System.Diagnostics.Debug.WriteLine($"  重置后当前忠诚度: {currentLoyalty}");
                
                // 计算需要的调整量
                int diff = calculatedLoyalty - currentLoyalty;
                System.Diagnostics.Debug.WriteLine($"  需要调整: {diff}");
                
                // 应用调整
                person.TempLoyaltyChange = diff;
                System.Diagnostics.Debug.WriteLine($"  设置TempLoyaltyChange: {person.TempLoyaltyChange}");
                
                // 验证结果
                int finalLoyalty = person.Loyalty;
                System.Diagnostics.Debug.WriteLine($"  调整后忠诚度: {finalLoyalty}");
                
                // 如果还是不够，尝试更大的调整
                if (Math.Abs(finalLoyalty - calculatedLoyalty) > 5)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募操作] 标准调整不够，尝试强化调整");
                    
                    // 尝试更大的TempLoyaltyChange值
                    person.TempLoyaltyChange = calculatedLoyalty + 50; // 额外加50确保达到目标
                    finalLoyalty = person.Loyalty;
                    System.Diagnostics.Debug.WriteLine($"  强化调整后忠诚度: {finalLoyalty}");
                    
                    // 如果还是不行，继续增加
                    int attempts = 0;
                    while (finalLoyalty < calculatedLoyalty * 0.8f && attempts < 5)
                    {
                        attempts++;
                        person.TempLoyaltyChange += 50;
                        finalLoyalty = person.Loyalty;
                        System.Diagnostics.Debug.WriteLine($"  第{attempts}次额外调整，TempLoyaltyChange: {person.TempLoyaltyChange}, Loyalty: {finalLoyalty}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[招募操作] 最终状态:");
                System.Diagnostics.Debug.WriteLine($"  BelongedFaction: {person.BelongedFaction?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"  Status: {person.Status}");
                System.Diagnostics.Debug.WriteLine($"  PersonalLoyalty: {person.PersonalLoyalty}");
                System.Diagnostics.Debug.WriteLine($"  Loyalty: {person.Loyalty}");
                System.Diagnostics.Debug.WriteLine($"  TempLoyaltyChange: {person.TempLoyaltyChange}");
                System.Diagnostics.Debug.WriteLine($"[招募操作] 武将 {person.Name} 已加入势力 {faction.Name}，当前忠诚度: {person.Loyalty}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募操作] 执行失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[招募操作] 异常堆栈: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 显示招募结果
        /// </summary>
        private static void ShowRecruitmentResult(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, 
            Faction faction, Person person, bool success, string failureReason)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[招募结果] 开始显示结果: 成功={success}, 武将={person?.Name ?? "null"}");
                
                // 验证必要的参数
                if (gameScreen == null)
                {
                    System.Diagnostics.Debug.WriteLine("[招募结果] gameScreen为null，无法显示结果");
                    return;
                }
                
                if (faction?.Advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[招募结果] faction或Advisor为null，无法显示结果");
                    return;
                }
                
                if (person == null)
                {
                    System.Diagnostics.Debug.WriteLine("[招募结果] person为null，无法显示结果");
                    return;
                }
                
                string resultText;
                
                try
                {
                    if (success)
                    {
                        resultText = $"军师 {faction.Advisor.Name}：\n\n" +
                                   $"恭喜主公！{person.Name} 已成功加入我军！\n\n" +
                                   $"武将能力：\n" +
                                   $"统率：{person.Command}\n" +
                                   $"智力：{person.Intelligence}\n" +
                                   $"政治：{person.Politics}\n" +
                                   $"魅力：{person.Glamour}\n" +
                                   $"忠诚：{person.Loyalty}";
                    }
                    else
                    {
                        resultText = $"军师 {faction.Advisor.Name}：\n\n" +
                                   $"很遗憾，{person.Name} 的招募失败了。\n\n" +
                                   $"原因：{failureReason ?? "未知原因"}";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[招募结果] 结果文本生成完成，长度: {resultText.Length}");
                }
                catch (Exception textEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募结果] 生成结果文本时发生异常: {textEx.Message}");
                    resultText = $"军师 {faction.Advisor.Name}：\n\n招募操作已完成，请查看武将列表确认结果。";
                }
                
                // 安全关闭当前界面
                try
                {
                    var currentWork = gameScreen.PeekUndoneWork();
                    System.Diagnostics.Debug.WriteLine($"[招募结果] 当前UndoneWork类型: {currentWork.Kind}");
                    
                    if (currentWork.Kind == UndoneWorkKind.Frame)
                    {
                        gameScreen.PopUndoneWork();
                        System.Diagnostics.Debug.WriteLine("[招募结果] 已关闭Frame界面");
                    }
                }
                catch (Exception closeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募结果] 关闭界面时发生异常: {closeEx.Message}");
                    // 继续执行，不要因为关闭界面失败而中断
                }
                
                // 临时禁用对话框显示，直接在日志中记录结果
                // 这样可以避免对话框相关的异常
                System.Diagnostics.Debug.WriteLine("[招募结果] === 招募结果 ===");
                System.Diagnostics.Debug.WriteLine($"[招募结果] {resultText}");
                System.Diagnostics.Debug.WriteLine("[招募结果] ================");
                
                // 如果需要显示对话框，可以取消下面的注释
                /*
                // 安全显示结果对话
                try
                {
                    if (gameScreen.Plugins?.tupianwenziPlugin != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[招募结果] 开始调用SetGameObjectBranch");
                        
                        // 清除所有可能的回调函数，防止冲突
                        try
                        {
                            gameScreen.Plugins.tupianwenziPlugin.SetCloseFunction(null);
                            System.Diagnostics.Debug.WriteLine("[招募结果] 已清除默认关闭回调");
                        }
                        catch (Exception clearEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[招募结果] 清除回调时异常: {clearEx.Message}");
                        }
                        
                        // 使用更简单的参数调用，避免复杂的图片加载
                        gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                            faction.Advisor, faction.Advisor, 
                            resultText, 
                            "", "", "");  // 暂时不使用图片，避免图片加载问题
                        
                        System.Diagnostics.Debug.WriteLine("[招募结果] SetGameObjectBranch调用完成");
                        
                        gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
                        System.Diagnostics.Debug.WriteLine("[招募结果] SetPosition调用完成");
                        
                        // 设置一个完全安全的关闭回调
                        gameScreen.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine("[招募结果] 安全关闭回调执行");
                                // 什么都不做，只是安全关闭
                            }
                            catch (Exception closeCallbackEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[招募结果] 关闭回调异常: {closeCallbackEx.Message}");
                            }
                        }));
                        
                        System.Diagnostics.Debug.WriteLine("[招募结果] 设置安全关闭回调完成");
                        
                        gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                        System.Diagnostics.Debug.WriteLine("[招募结果] 对话框显示状态已设置");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[招募结果] tupianwenziPlugin为null，无法显示对话框");
                    }
                }
                catch (Exception dialogEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募结果] 显示对话框时发生异常: {dialogEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[招募结果] 异常堆栈: {dialogEx.StackTrace}");
                }
                */
                
                System.Diagnostics.Debug.WriteLine($"[招募结果] 显示结果完成: 成功={success}, 武将={person.Name}");
                
                // 添加更长的延迟，确保所有UI操作和事件处理完成
                System.Threading.Thread.Sleep(500); // 增加到500ms
                
                // 强制垃圾回收，清理可能的资源问题
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect(); // 执行两次确保彻底清理
                    System.Diagnostics.Debug.WriteLine("[招募结果] 执行垃圾回收完成");
                }
                catch (Exception gcEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募结果] 垃圾回收异常: {gcEx.Message}");
                }
                
                // 添加额外的安全措施
                try
                {
                    // 确保所有UI状态都已稳定
                    if (gameScreen?.Plugins != null)
                    {
                        // 检查并清理可能的UI状态
                        System.Diagnostics.Debug.WriteLine("[招募结果] 检查UI状态完成");
                    }
                }
                catch (Exception uiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募结果] UI状态检查异常: {uiEx.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine("[招募结果] 方法执行完成，准备返回");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募结果] 显示失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[招募结果] 异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[招募结果] 异常堆栈: {ex.StackTrace}");
                
                // 最后的安全措施：确保不会因为显示结果而崩溃游戏
                try
                {
                    System.Diagnostics.Debug.WriteLine("[招募结果] 执行最后的安全措施");
                    // 可以在这里添加最基本的错误恢复逻辑
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("[招募结果] 连最后的安全措施都失败了");
                }
            }
        }

        /// <summary>
        /// 显示忠诚度警告界面
        /// </summary>
        private static void ShowLoyaltyWarningInterface(WorldOfTheThreeKingdoms.GameScreens.MainGameScreen gameScreen, Faction faction)
        {
            try
            {
                // 获取低忠诚度武将
                var lowLoyaltyPersons = GetLowLoyaltyPersons(faction);
                
                if (lowLoyaltyPersons?.GameObjects?.Any() == true)
                {
                    // 显示低忠诚武将列表
                    gameScreen.ShowTabListInFrame(
                        UndoneWorkKind.Frame, 
                        FrameKind.Person, 
                        FrameFunction.Browse, 
                        false, true, false, false, 
                        lowLoyaltyPersons, 
                        null, 
                        "忠诚度警告", 
                        "Loyalty"  // 按忠诚度排序
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"[军师谏言] 显示忠诚度警告，涉及 {lowLoyaltyPersons.Count} 名武将");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 未找到低忠诚武将");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[忠诚度警告界面] 显示失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取推荐的在野武将
        /// </summary>
        private static GameObjectList GetRecommendedPersons()
        {
            var result = new PersonList();
            
            if (Session.Current?.Scenario?.Persons == null) return result;
            
            // 获取强力在野武将（能力值总和 > 200）
            var unfoundPersons = Session.Current.Scenario.Persons.GetList()
                ?.Cast<Person>()
                ?.Where(p => p.BelongedFaction == null && 
                           (p.Command + p.Intelligence + p.Politics + p.Glamour) > 200)
                ?.OrderByDescending(p => p.Command + p.Intelligence + p.Politics + p.Glamour)
                ?.Take(10);  // 最多推荐10个
            
            if (unfoundPersons != null)
            {
                foreach (var person in unfoundPersons)
                {
                    result.Add(person);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 获取低忠诚度武将
        /// </summary>
        private static GameObjectList GetLowLoyaltyPersons(Faction faction)
        {
            var result = new PersonList();
            
            if (faction?.Persons == null) return result;
            
            // 获取忠诚度过低的武将（< 60 且非君主）
            var lowLoyaltyPersons = faction.Persons.GetList()
                ?.Cast<Person>()
                ?.Where(p => p.Loyalty < 60 && p != faction.Leader)
                ?.OrderBy(p => p.Loyalty);  // 按忠诚度从低到高排序
            
            if (lowLoyaltyPersons != null)
            {
                foreach (var person in lowLoyaltyPersons)
                {
                    result.Add(person);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 获取谏言事件文本
        /// </summary>
        private static string GetAdviceEventText(Person advisor, AdvisorSuggestionKind suggestion, bool isAccurate)
        {
            string prefix = $"军师 {advisor.Name} 求见：\n\n";
            
            if (!isAccurate && suggestion != AdvisorSuggestionKind.None)
            {
                // 不准确的建言
                return prefix + GetInaccurateAdviceEventText(suggestion);
            }
            
            // 准确的建言
            switch (suggestion)
            {
                case AdvisorSuggestionKind.EnemyAttack:
                    return prefix + "主公，据某观察，敌军正在向我方逼近！请速做防备！";
                
                case AdvisorSuggestionKind.PersonRecruit:
                    return prefix + "发现附近有贤才在野，是否前去招揽？";
                
                case AdvisorSuggestionKind.LoyaltyWarning:
                    return prefix + "部分将领忠诚度过低，是否查看详情？";
                
                default:
                    return prefix + "主公，当前形势尚好，可按既定方针继续行事。";
            }
        }

        /// <summary>
        /// 获取不准确的谏言事件文本
        /// </summary>
        private static string GetInaccurateAdviceEventText(AdvisorSuggestionKind suggestion)
        {
            switch (suggestion)
            {
                case AdvisorSuggestionKind.EnemyAttack:
                    return "主公，某观察四周，暂无异常，可安心发展。";
                
                case AdvisorSuggestionKind.PersonRecruit:
                    return "主公，当前人才济济，暂无招揽之需。";
                
                case AdvisorSuggestionKind.LoyaltyWarning:
                    return "主公，将士们忠心耿耿，无需担忧。";
                
                default:
                    return "主公，某才疏学浅，难以判断当前形势。";
            }
        }

        /// <summary>
        /// 获取谏言事件图片
        /// </summary>
        private static string GetAdviceEventImage(AdvisorSuggestionKind suggestion)
        {
            switch (suggestion)
            {
                case AdvisorSuggestionKind.EnemyAttack:
                    return "EnemyWarning.jpg";
                case AdvisorSuggestionKind.PersonRecruit:
                    return "RecruitAdvice.jpg";
                case AdvisorSuggestionKind.LoyaltyWarning:
                    return "LoyaltyWarning.jpg";
                default:
                    return "AdvisorAdvice.jpg";
            }
        }

        /// <summary>
        /// 清理缓存（在新游戏或加载游戏时调用）
        /// </summary>
        public static void ClearCache()
        {
            _warnedEnemyTroops.Clear();
            _lastCheckDay = -1;
            System.Diagnostics.Debug.WriteLine("[军师谏言事件] 缓存已清理");
        }

        /// <summary>
        /// 清理已销毁的敌军缓存
        /// </summary>
        public static void CleanupDestroyedTroops()
        {
            if (Session.Current?.Scenario?.Troops == null) return;
            
            var existingTroopIds = new HashSet<int>(
                Session.Current.Scenario.Troops.GetList()
                    .Cast<Troop>()
                    .Where(t => !t.Destroyed)
                    .Select(t => t.ID)
            );
            
            _warnedEnemyTroops.RemoveWhere(id => !existingTroopIds.Contains(id));
        }
    }
}