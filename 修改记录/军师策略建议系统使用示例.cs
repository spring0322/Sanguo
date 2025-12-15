using System;
using GameObjects;
using GameGlobal;
using GameManager;
using WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 军师策略建议系统使用示例
/// 展示如何在游戏的不同场景中集成军师建议功能
/// </summary>
public class StrategyAdviceUsageExamples
{
    /// <summary>
    /// 示例1：在策略菜单中使用
    /// 当玩家点击"流言"按钮时
    /// </summary>
    public void OnClickGossipButton(Person targetEnemy)
    {
        // 使用集成类的简化方法
        StrategyAdviceIntegration.ShowGossipAdvice(targetEnemy);
        
        // 或者直接使用核心系统
        var faction = Session.Current.Scenario.CurrentPlayer;
        var advisor = faction?.Advisor;
        
        if (advisor != null)
        {
            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetEnemy, StrategyKind.Gossip);
            
            // 显示建议
            string title = "军师谏言 - 流言策略";
            string content = advice.AdvisorComment;
            
            if (advice.BestCandidate != null)
            {
                content += $"\n\n(已自动为您选中：{advice.BestCandidate.Name})";
                // 这里可以在UI中自动选中推荐的人选
                // UI列表.SetSelected(advice.BestCandidate);
            }
            
            ShowMessageBox(title, content);
        }
    }

    /// <summary>
    /// 示例2：在城市攻略界面中使用
    /// 当玩家选择攻城策略时
    /// </summary>
    public void OnPlanCityAttack(Architecture targetCity)
    {
        var faction = Session.Current.Scenario.CurrentPlayer;
        var advisor = faction?.Advisor;
        
        if (advisor == null)
        {
            ShowMessageBox("提示", "当前无军师，无法获得策略建议");
            return;
        }

        // 获取多种策略的建议
        string adviceText = $"=== {advisor.Name}对攻取{targetCity.Name}的建议 ===\n\n";
        
        // 放火策略
        var arsonAdvice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetCity, StrategyKind.Arson);
        adviceText += $"【放火】推荐：{arsonAdvice.BestCandidate?.Name ?? "无合适人选"} ";
        adviceText += $"(成功率{arsonAdvice.PredictedChance}%)\n";
        
        // 破坏策略
        var destructionAdvice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetCity, StrategyKind.Destruction);
        adviceText += $"【破坏】推荐：{destructionAdvice.BestCandidate?.Name ?? "无合适人选"} ";
        adviceText += $"(成功率{destructionAdvice.PredictedChance}%)\n\n";
        
        // 选择最佳策略
        if (arsonAdvice.PredictedChance > destructionAdvice.PredictedChance)
        {
            adviceText += $"军师建议：{arsonAdvice.AdvisorComment}";
        }
        else
        {
            adviceText += $"军师建议：{destructionAdvice.AdvisorComment}";
        }
        
        ShowMessageBox("攻城策略建议", adviceText);
    }

    /// <summary>
    /// 示例3：在外交界面中使用
    /// 当玩家考虑与其他势力结盟时
    /// </summary>
    public void OnConsiderAlliance(Faction targetFaction)
    {
        // 使用集成类的方法
        StrategyAdviceIntegration.ShowAllianceAdvice(targetFaction);
    }

    /// <summary>
    /// 示例4：在人才搜索界面中使用
    /// 当玩家准备派人搜索人才时
    /// </summary>
    public void OnPrepareSearch(Architecture searchArea)
    {
        var faction = Session.Current.Scenario.CurrentPlayer;
        var advisor = faction?.Advisor;
        
        if (advisor != null)
        {
            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, searchArea, StrategyKind.Search);
            
            string message = advice.AdvisorComment;
            if (advice.BestCandidate != null)
            {
                message += $"\n\n是否派遣 {advice.BestCandidate.Name} 前往搜索？";
                
                // 这里可以显示确认对话框
                if (ShowConfirmDialog("搜索确认", message))
                {
                    // 执行搜索任务
                    ExecuteSearchMission(advice.BestCandidate, searchArea);
                }
            }
            else
            {
                ShowMessageBox("军师建议", message);
            }
        }
    }

    /// <summary>
    /// 示例5：在策略规划界面中使用
    /// 显示当前势力的整体策略建议
    /// </summary>
    public void ShowOverallStrategyAdvice()
    {
        // 使用集成类的方法
        StrategyAdviceIntegration.ShowAllStrategyAdvice();
    }

    /// <summary>
    /// 示例6：在军师任命后显示能力评估
    /// </summary>
    public void OnAdvisorAppointed(Person newAdvisor)
    {
        // 显示新军师的策略建议能力
        string assessment = StrategyAdviceIntegration.GetAdvisorCapabilityAssessment();
        ShowMessageBox($"军师 {newAdvisor.Name} 已上任", assessment);
        
        // 可以进一步显示该军师擅长的策略类型
        ShowAdvisorSpecialties(newAdvisor);
    }

    /// <summary>
    /// 显示军师的专长分析
    /// </summary>
    private void ShowAdvisorSpecialties(Person advisor)
    {
        var faction = Session.Current.Scenario.CurrentPlayer;
        string specialties = $"=== {advisor.Name} 的策略专长分析 ===\n\n";
        
        // 分析各项策略的推荐成功率
        var strategies = new[]
        {
            StrategyKind.Gossip,
            StrategyKind.Arson,
            StrategyKind.Destruction,
            StrategyKind.Instigate,
            StrategyKind.Alliance,
            StrategyKind.Search
        };
        
        foreach (var strategy in strategies)
        {
            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, null, strategy);
            string strategyName = AdvisorStrategySystem.GetStrategyName(strategy);
            
            specialties += $"{strategyName}：";
            if (advice.PredictedChance >= 80)
                specialties += "★★★ 极其擅长\n";
            else if (advice.PredictedChance >= 60)
                specialties += "★★☆ 比较擅长\n";
            else if (advice.PredictedChance >= 40)
                specialties += "★☆☆ 一般水平\n";
            else
                specialties += "☆☆☆ 不太擅长\n";
        }
        
        ShowMessageBox("军师专长", specialties);
    }

    /// <summary>
    /// 示例7：在回合开始时的策略提醒
    /// </summary>
    public void OnTurnStart()
    {
        var faction = Session.Current.Scenario.CurrentPlayer;
        var advisor = faction?.Advisor;
        
        if (advisor != null && advisor.Intelligence >= 80) // 只有高智力军师才会主动建议
        {
            // 随机选择一个策略进行建议（模拟军师的主动建议）
            var strategies = Enum.GetValues(typeof(StrategyKind));
            var randomStrategy = (StrategyKind)strategies.GetValue(GameObject.Random(strategies.Length));
            
            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, null, randomStrategy);
            
            if (advice.BestCandidate != null && advice.PredictedChance >= 70)
            {
                string strategyName = AdvisorStrategySystem.GetStrategyName(randomStrategy);
                string reminder = $"主公，{advisor.Name}建议：\n\n";
                reminder += $"当前{strategyName}策略时机成熟，";
                reminder += $"可派{advice.BestCandidate.Name}执行，成功率约{advice.PredictedChance}%。";
                
                ShowMessageBox("军师建言", reminder);
            }
        }
    }

    // 以下是辅助方法，需要根据实际的UI系统进行实现
    
    private void ShowMessageBox(string title, string content)
    {
        // 实际实现中，这里应该调用游戏的消息框显示系统
        System.Diagnostics.Debug.WriteLine($"=== {title} ===");
        System.Diagnostics.Debug.WriteLine(content);
    }
    
    private bool ShowConfirmDialog(string title, string content)
    {
        // 实际实现中，这里应该显示确认对话框
        System.Diagnostics.Debug.WriteLine($"=== {title} ===");
        System.Diagnostics.Debug.WriteLine(content);
        return true; // 假设用户确认
    }
    
    private void ExecuteSearchMission(Person executor, Architecture area)
    {
        // 实际实现中，这里应该执行搜索任务的逻辑
        System.Diagnostics.Debug.WriteLine($"{executor.Name} 开始在 {area.Name} 搜索");
    }
}

/// <summary>
/// 在现有游戏界面中集成的示例
/// </summary>
public static class GameUIIntegrationExamples
{
    /// <summary>
    /// 在策略菜单中添加"军师建议"按钮
    /// </summary>
    public static void AddAdvisorAdviceButton()
    {
        // 伪代码示例，展示如何在UI中添加按钮
        /*
        var adviceButton = new Button("军师建议");
        adviceButton.OnClick = () => {
            StrategyAdviceIntegration.ShowAllStrategyAdvice();
        };
        strategyMenu.AddButton(adviceButton);
        */
        
        System.Diagnostics.Debug.WriteLine("[UI集成] 已添加军师建议按钮");
    }
    
    /// <summary>
    /// 在人员选择界面中显示推荐标记
    /// </summary>
    public static void HighlightRecommendedPerson(StrategyKind strategy)
    {
        var faction = Session.Current.Scenario.CurrentPlayer;
        var advisor = faction?.Advisor;
        
        if (advisor != null)
        {
            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, null, strategy);
            if (advice.BestCandidate != null)
            {
                // 伪代码：在人员列表中高亮推荐人选
                /*
                personList.HighlightPerson(advice.BestCandidate);
                personList.AddTooltip(advice.BestCandidate, 
                    $"军师推荐 (成功率{advice.PredictedChance}%)");
                */
                
                System.Diagnostics.Debug.WriteLine($"[UI集成] 高亮推荐人选: {advice.BestCandidate.Name}");
            }
        }
    }
}