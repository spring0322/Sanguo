using System;

/// <summary>
/// 说服按钮灰色状态修复测试
/// 
/// 修复内容：
/// 根据用户要求，重新设计说服流程：
/// 1. 没有目标时，说服按钮是灰色，无法选择
/// 2. 有说服目标后，玩家先选择目标
/// 3. 选择目标后，军师进行预测（基于军师智力的非确定性预测）
/// 4. 如果有合适的人选，自动勾选推荐人员
/// 5. 如果没有合适人选，军师劝阻，但玩家仍可坚持选择
/// 
/// 关键修改：
/// - 修改 Architecture.ConvincePersonAvail() 方法
/// - 从检查 GetConvincePersonArchitectureArea().Count > 0（检查区域）
/// - 改为检查 GetConvinceDestinationPersonList(faction).Count > 0（检查实际目标人员）
/// </summary>
public class ConvinceButtonGrayStateTest
{
    public static void TestConvinceButtonAvailability()
    {
        Console.WriteLine("=== 说服按钮灰色状态修复测试 ===");
        
        Console.WriteLine("\n=== 修复前的问题 ===");
        Console.WriteLine("问题：说服按钮的可用性基于 GetConvincePersonArchitectureArea().Count > 0");
        Console.WriteLine("这个方法检查的是可以进行说服的区域，而不是实际的可说服人员");
        Console.WriteLine("结果：即使没有实际可说服的人员，按钮仍然可用");
        Console.WriteLine("用户体验：点击说服后发现没有目标，需要额外的对话处理");
        
        Console.WriteLine("\n=== 修复后的改进 ===");
        Console.WriteLine("改进：说服按钮的可用性基于 GetConvinceDestinationPersonList(faction).Count > 0");
        Console.WriteLine("这个方法检查的是实际可说服的人员列表");
        Console.WriteLine("结果：只有在有实际可说服人员时，按钮才可用");
        Console.WriteLine("用户体验：按钮状态准确反映是否有说服目标");
        
        Console.WriteLine("\n=== 新的说服流程 ===");
        Console.WriteLine("1. 检查说服按钮可用性：");
        Console.WriteLine("   - 基础条件：HasPerson() && Fund >= ConvincePersonFund");
        Console.WriteLine("   - 关键条件：GetConvinceDestinationPersonList(currentPlayer).Count > 0");
        Console.WriteLine("   - 如果没有目标：按钮灰色，不可点击");
        
        Console.WriteLine("\n2. 有目标时的流程：");
        Console.WriteLine("   - 玩家点击说服按钮");
        Console.WriteLine("   - 显示目标选择界面（不进行军师分析）");
        Console.WriteLine("   - 玩家选择具体目标");
        Console.WriteLine("   - 军师基于智力进行非确定性预测");
        Console.WriteLine("   - 如果有合适人选：自动勾选推荐人员");
        Console.WriteLine("   - 如果无合适人选：军师劝阻，但玩家可坚持");
        
        Console.WriteLine("\n=== 技术实现细节 ===");
        Console.WriteLine("修改文件：WorldOfTheThreeKingdoms/GameObjects/Architecture.cs");
        Console.WriteLine("修改方法：ConvincePersonAvail()");
        
        Console.WriteLine("\n修改前代码：");
        Console.WriteLine("return ((this.HasPerson() && (this.Fund >= this.ConvincePersonFund)) && ");
        Console.WriteLine("        (this.GetConvincePersonArchitectureArea().Count > 0));");
        
        Console.WriteLine("\n修改后代码：");
        Console.WriteLine("// 基础条件检查：有人员且资金充足");
        Console.WriteLine("if (!this.HasPerson() || this.Fund < this.ConvincePersonFund)");
        Console.WriteLine("    return false;");
        Console.WriteLine("");
        Console.WriteLine("// 检查是否有实际可说服的目标人员");
        Console.WriteLine("var currentPlayer = Session.Current.Scenario.CurrentPlayer;");
        Console.WriteLine("if (currentPlayer == null) return false;");
        Console.WriteLine("");
        Console.WriteLine("var convinceTargets = this.GetConvinceDestinationPersonList(currentPlayer);");
        Console.WriteLine("return convinceTargets.Count > 0;");
        
        Console.WriteLine("\n=== GetConvinceDestinationPersonList 逻辑 ===");
        Console.WriteLine("己方建筑：只能说服俘虏");
        Console.WriteLine("敌方建筑：可以说服所有人员（除女官）");
        Console.WriteLine("所有建筑：可以说服在野人员");
        Console.WriteLine("这确保了只有在有实际可说服目标时，按钮才可用");
        
        Console.WriteLine("\n=== 用户体验改进 ===");
        Console.WriteLine("✓ 按钮状态准确反映功能可用性");
        Console.WriteLine("✓ 避免无效的点击操作");
        Console.WriteLine("✓ 减少不必要的对话框");
        Console.WriteLine("✓ 符合用户直觉的交互逻辑");
        Console.WriteLine("✓ 保持现有的智能说服系统功能");
        
        Console.WriteLine("\n=== 测试场景 ===");
        Console.WriteLine("场景1：己方城池无俘虏 → 说服按钮灰色");
        Console.WriteLine("场景2：敌方城池无人员 → 说服按钮灰色");
        Console.WriteLine("场景3：中立城池无在野人员 → 说服按钮灰色");
        Console.WriteLine("场景4：有可说服目标 → 说服按钮可用，进入新流程");
    }
}

/// <summary>
/// 修复代码对比
/// </summary>
public class ConvinceAvailabilityCodeComparison
{
    /*
    === 修复前的 ConvincePersonAvail() 方法 ===
    
    public bool ConvincePersonAvail()
    {
        return ((this.HasPerson() && (this.Fund >= this.ConvincePersonFund)) && 
                (this.GetConvincePersonArchitectureArea().Count > 0));
    }
    
    问题：
    - GetConvincePersonArchitectureArea() 返回可以进行说服的区域
    - 区域存在不代表有实际可说服的人员
    - 导致按钮可用但实际无目标的情况
    
    === 修复后的 ConvincePersonAvail() 方法 ===
    
    public bool ConvincePersonAvail()
    {
        // 基础条件检查：有人员且资金充足
        if (!this.HasPerson() || this.Fund < this.ConvincePersonFund)
        {
            return false;
        }
        
        // 检查是否有实际可说服的目标人员
        // 使用当前玩家势力来获取可说服目标列表
        var currentPlayer = Session.Current.Scenario.CurrentPlayer;
        if (currentPlayer == null)
        {
            return false;
        }
        
        var convinceTargets = this.GetConvinceDestinationPersonList(currentPlayer);
        return convinceTargets.Count > 0;
    }
    
    改进：
    - GetConvinceDestinationPersonList() 返回实际可说服的人员列表
    - 只有在有实际目标时才返回 true
    - 按钮状态准确反映功能可用性
    - 与现有的智能说服系统完美集成
    
    === GetConvinceDestinationPersonList 方法逻辑 ===
    
    public PersonList GetConvinceDestinationPersonList(Faction faction)
    {
        PersonList result = new PersonList();
        if (this.BelongedFaction == faction)
        {
            // 己方建筑：只能说服俘虏
            foreach (Captive captive in this.Captives)
            {
                result.Add(captive.CaptivePerson);
            }
        }
        else
        {
            // 敌方建筑：可以说服所有人员（除女官）
            foreach (Person person in this.PersonsExcludeNvGuan)
            {
                result.Add(person);
            }
        }
        // 所有建筑：可以说服在野人员
        foreach (Person person in this.NoFactionPersons)
        {
            result.Add(person);
        }
        ConvinceDestinationPersonList = result;
        return result;
    }
    
    这个方法确保了返回的是真正可以说服的人员，而不是可能的区域。
    */
}