using System;
using GameObjects;
using GameManager;

/// <summary>
/// 军师策略建议系统测试代码
/// </summary>
public class StrategyAdviceSystemTest
{
    /// <summary>
    /// 测试军师策略建议系统
    /// </summary>
    public static void TestStrategyAdviceSystem()
    {
        Console.WriteLine("=== 军师策略建议系统测试 ===\n");

        // 创建测试数据
        var testFaction = CreateTestFaction();
        var testAdvisor = CreateTestAdvisor();
        var testTarget = CreateTestTarget();

        // 测试所有策略类型
        var strategies = Enum.GetValues(typeof(StrategyKind));
        
        foreach (StrategyKind strategy in strategies)
        {
            Console.WriteLine($"--- 测试策略: {AdvisorStrategySystem.GetStrategyName(strategy)} ---");
            Console.WriteLine($"策略说明: {AdvisorStrategySystem.GetStrategyDescription(strategy)}");
            
            var advice = AdvisorStrategySystem.GetAdvice(testAdvisor, testFaction, testTarget, strategy);
            
            Console.WriteLine($"推荐人选: {advice.BestCandidate?.Name ?? "无"}");
            Console.WriteLine($"预测成功率: {advice.PredictedChance}%");
            Console.WriteLine($"军师点评: {advice.AdvisorComment}");
            Console.WriteLine();
        }

        // 测试无军师情况
        Console.WriteLine("--- 测试无军师情况 ---");
        var noAdvisorAdvice = AdvisorStrategySystem.GetAdvice(null, testFaction, testTarget, StrategyKind.Gossip);
        Console.WriteLine($"军师点评: {noAdvisorAdvice.AdvisorComment}");
        Console.WriteLine();

        // 测试不同智力军师的预测准确度
        Console.WriteLine("--- 测试不同智力军师的预测差异 ---");
        var lowIntAdvisor = CreateLowIntelligenceAdvisor();
        var highIntAdvisor = CreateHighIntelligenceAdvisor();

        for (int i = 0; i < 3; i++)
        {
            var lowAdvice = AdvisorStrategySystem.GetAdvice(lowIntAdvisor, testFaction, testTarget, StrategyKind.Gossip);
            var highAdvice = AdvisorStrategySystem.GetAdvice(highIntAdvisor, testFaction, testTarget, StrategyKind.Gossip);
            
            Console.WriteLine($"第{i+1}次预测:");
            Console.WriteLine($"  低智力军师({lowIntAdvisor.Name}): {lowAdvice.PredictedChance}%");
            Console.WriteLine($"  高智力军师({highIntAdvisor.Name}): {highAdvice.PredictedChance}%");
        }
    }

    /// <summary>
    /// 创建测试势力
    /// </summary>
    private static Faction CreateTestFaction()
    {
        var faction = new Faction();
        faction.Name = "测试势力";
        
        // 添加不同属性的武将
        faction.Persons.Add(CreatePerson("张飞", 30, 20, 95, 15)); // 高统率
        faction.Persons.Add(CreatePerson("诸葛亮", 100, 80, 40, 95)); // 高智力高政治
        faction.Persons.Add(CreatePerson("赵云", 85, 90, 85, 70)); // 全能型
        faction.Persons.Add(CreatePerson("糜竺", 60, 95, 30, 85)); // 高魅力高政治
        faction.Persons.Add(CreatePerson("普通士兵", 40, 30, 50, 25)); // 普通属性
        
        return faction;
    }

    /// <summary>
    /// 创建测试军师
    /// </summary>
    private static Person CreateTestAdvisor()
    {
        return CreatePerson("军师", 85, 70, 60, 80);
    }

    /// <summary>
    /// 创建低智力军师
    /// </summary>
    private static Person CreateLowIntelligenceAdvisor()
    {
        return CreatePerson("庸才军师", 45, 50, 60, 40);
    }

    /// <summary>
    /// 创建高智力军师
    /// </summary>
    private static Person CreateHighIntelligenceAdvisor()
    {
        return CreatePerson("神算军师", 98, 75, 65, 90);
    }

    /// <summary>
    /// 创建测试目标
    /// </summary>
    private static Person CreateTestTarget()
    {
        return CreatePerson("敌方武将", 70, 60, 80, 65);
    }

    /// <summary>
    /// 创建人物
    /// </summary>
    private static Person CreatePerson(string name, int intelligence, int glamour, int command, int politics)
    {
        var person = new Person();
        person.Name = name;
        person.Intelligence = intelligence;
        person.Glamour = glamour;
        person.Command = command;
        person.Politics = politics;
        person.Alive = true;
        person.IsCaptive = false;
        person.Available = true;
        person.PersonalLoyalty = 2; // 中等忠诚度
        
        return person;
    }

    /// <summary>
    /// 在游戏中集成测试
    /// </summary>
    public static void IntegrateIntoGame()
    {
        // 这个方法展示如何在实际游戏中使用军师策略建议系统
        
        // 获取当前势力和军师
        var currentFaction = Session.Current.Scenario.CurrentPlayer;
        var advisor = currentFaction?.Advisor;
        
        if (advisor == null)
        {
            Console.WriteLine("当前势力没有军师，无法提供策略建议");
            return;
        }

        // 示例：获取流言策略的建议
        var gossipAdvice = AdvisorStrategySystem.GetAdvice(
            advisor, 
            currentFaction, 
            null, // 目标可以为空，系统会处理
            StrategyKind.Gossip
        );

        // 显示建议
        Console.WriteLine("=== 军师策略建议 ===");
        Console.WriteLine(gossipAdvice.AdvisorComment);
        
        if (gossipAdvice.BestCandidate != null)
        {
            Console.WriteLine($"推荐执行人: {gossipAdvice.BestCandidate.Name}");
            Console.WriteLine($"预测成功率: {gossipAdvice.PredictedChance}%");
        }
    }
}

/// <summary>
/// 在MainGameScreen中集成军师建议功能的示例代码
/// </summary>
public class MainGameScreenIntegration
{
    // 在MainGameScreen类中添加这些成员
    private StrategyAdviceUI strategyAdviceUI = new StrategyAdviceUI();

    /// <summary>
    /// 显示军师策略建议界面
    /// </summary>
    public void ShowStrategyAdvice()
    {
        var currentFaction = Session.Current.Scenario.CurrentPlayer;
        var advisor = currentFaction?.Advisor;
        
        if (advisor == null)
        {
            // 显示"无军师"提示
            ShowMessage("当前势力没有军师，无法提供策略建议");
            return;
        }

        // 显示策略建议界面
        strategyAdviceUI.Show(currentFaction, advisor);
    }

    /// <summary>
    /// 在Update方法中添加
    /// </summary>
    public void UpdateStrategyAdviceUI()
    {
        if (strategyAdviceUI.IsVisible)
        {
            strategyAdviceUI.Update();
        }
    }

    /// <summary>
    /// 在HandleMouseClick方法中添加
    /// </summary>
    public bool HandleStrategyAdviceClick(int x, int y)
    {
        if (strategyAdviceUI.IsVisible)
        {
            return strategyAdviceUI.HandleMouseClick(x, y);
        }
        return false;
    }

    /// <summary>
    /// 在Draw方法中添加
    /// </summary>
    public void DrawStrategyAdviceUI(SpriteBatch spriteBatch)
    {
        if (strategyAdviceUI.IsVisible)
        {
            // 需要提供字体和纹理资源
            strategyAdviceUI.Draw(spriteBatch, defaultFont, backgroundTexture, buttonTexture);
        }
    }

    /// <summary>
    /// 显示消息的辅助方法
    /// </summary>
    private void ShowMessage(string message)
    {
        // 这里应该调用游戏的消息显示系统
        Console.WriteLine(message);
    }
}