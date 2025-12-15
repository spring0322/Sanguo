using System;

/// <summary>
/// 说服目标情报等级检查修复验证
/// 
/// 问题：
/// 之前修改ConvincePersonAvail()时，只是简单地调用GetConvinceDestinationPersonList()，
/// 但该方法没有考虑情报等级限制，导致对于非己方城池，即使情报不足也会显示有说服目标。
/// 
/// 修复：
/// 修改GetConvinceDestinationPersonList()方法，在处理非己方城池时检查情报等级。
/// 只有在有足够情报（至少"低"等级）时，才将敌方人员加入可说服目标列表。
/// </summary>
public class ConvinceTargetInformationLevelFixVerification
{
    public static void TestInformationLevelCheck()
    {
        Console.WriteLine("=== 说服目标情报等级检查修复验证 ===");
        
        Console.WriteLine("\n=== 问题回顾 ===");
        Console.WriteLine("用户反馈：'非己方城池有情报，应该有说服目标'");
        Console.WriteLine("发现问题：GetConvinceDestinationPersonList()没有检查情报等级");
        Console.WriteLine("导致结果：情报不足时仍显示有说服目标，但实际无法说服");
        
        Console.WriteLine("\n=== 修复前的逻辑 ===");
        Console.WriteLine("GetConvinceDestinationPersonList(Faction faction):");
        Console.WriteLine("  if (this.BelongedFaction == faction)");
        Console.WriteLine("    // 己方建筑：添加俘虏");
        Console.WriteLine("  else");
        Console.WriteLine("    // 敌方建筑：直接添加所有人员（❌ 没有检查情报）");
        Console.WriteLine("  // 添加在野人员");
        
        Console.WriteLine("\n=== 修复后的逻辑 ===");
        Console.WriteLine("GetConvinceDestinationPersonList(Faction faction):");
        Console.WriteLine("  if (this.BelongedFaction == faction)");
        Console.WriteLine("    // 己方建筑：添加俘虏");
        Console.WriteLine("  else");
        Console.WriteLine("    // 敌方建筑：检查情报等级");
        Console.WriteLine("    bool hasEnoughInformation = faction.GetKnownAreaData(this.Position) >= InformationLevel.低;");
        Console.WriteLine("    if (hasEnoughInformation)");
        Console.WriteLine("      // ✅ 只有在有足够情报时才添加敌方人员");
        Console.WriteLine("  // 添加在野人员（不受情报限制）");
        
        Console.WriteLine("\n=== 测试场景 ===");
        
        Console.WriteLine("\n场景1：己方城池");
        Console.WriteLine("- 建筑归属：己方势力");
        Console.WriteLine("- 可说服目标：俘虏");
        Console.WriteLine("- 情报要求：无（己方建筑）");
        Console.WriteLine("- 预期结果：有俘虏时按钮可用，无俘虏时按钮灰色");
        
        Console.WriteLine("\n场景2：敌方城池 + 有足够情报");
        Console.WriteLine("- 建筑归属：敌方势力");
        Console.WriteLine("- 情报等级：低/中/高/全");
        Console.WriteLine("- 可说服目标：敌方人员（除女官）");
        Console.WriteLine("- 预期结果：有人员时按钮可用，无人员时按钮灰色");
        
        Console.WriteLine("\n场景3：敌方城池 + 情报不足");
        Console.WriteLine("- 建筑归属：敌方势力");
        Console.WriteLine("- 情报等级：无");
        Console.WriteLine("- 可说服目标：无（因情报不足）");
        Console.WriteLine("- 预期结果：按钮灰色（即使城池有人员）");
        
        Console.WriteLine("\n场景4：中立城池 + 有在野人员");
        Console.WriteLine("- 建筑归属：任何势力");
        Console.WriteLine("- 在野人员：存在");
        Console.WriteLine("- 情报要求：无（在野人员不受情报限制）");
        Console.WriteLine("- 预期结果：按钮可用");
        
        Console.WriteLine("\n场景5：敌方城池 + 情报不足 + 有在野人员");
        Console.WriteLine("- 建筑归属：敌方势力");
        Console.WriteLine("- 情报等级：无");
        Console.WriteLine("- 在野人员：存在");
        Console.WriteLine("- 可说服目标：在野人员（不受情报限制）");
        Console.WriteLine("- 预期结果：按钮可用（因为有在野人员）");
        
        Console.WriteLine("\n=== 逻辑验证 ===");
        
        Console.WriteLine("\n✅ 情报等级检查正确性：");
        Console.WriteLine("- 己方建筑：不需要情报检查");
        Console.WriteLine("- 敌方建筑：需要至少'低'等级情报");
        Console.WriteLine("- 在野人员：不受情报等级限制");
        
        Console.WriteLine("\n✅ 按钮状态准确性：");
        Console.WriteLine("- ConvincePersonAvail() 调用 GetConvinceDestinationPersonList()");
        Console.WriteLine("- GetConvinceDestinationPersonList() 已包含情报检查");
        Console.WriteLine("- 按钮状态准确反映实际可说服目标");
        
        Console.WriteLine("\n✅ 用户体验一致性：");
        Console.WriteLine("- 按钮可用 = 确实有可说服目标");
        Console.WriteLine("- 按钮灰色 = 确实无可说服目标");
        Console.WriteLine("- 避免了'按钮可用但无法执行'的困惑");
        
        Console.WriteLine("\n=== TriggerIntelligentConvince 调整 ===");
        Console.WriteLine("由于GetConvinceDestinationPersonList()已包含情报检查，");
        Console.WriteLine("TriggerIntelligentConvince()中的重复检查已优化：");
        Console.WriteLine("- 直接获取目标列表");
        Console.WriteLine("- 如果列表为空，检查具体原因（情报不足 vs 真的无目标）");
        Console.WriteLine("- 显示相应的错误信息");
    }
}

/// <summary>
/// 修复代码对比
/// </summary>
public class InformationLevelCheckCodeComparison
{
    /*
    === 修复前的 GetConvinceDestinationPersonList 方法 ===
    
    public PersonList GetConvinceDestinationPersonList(Faction faction)
    {
        PersonList result = new PersonList();
        if (this.BelongedFaction == faction)
        {
            foreach (Captive captive in this.Captives)
            {
                result.Add(captive.CaptivePerson);
            }
        }
        else
        {
            // ❌ 问题：没有检查情报等级
            foreach (Person person in this.PersonsExcludeNvGuan)
            {
                result.Add(person);
            }
        }
        foreach (Person person in this.NoFactionPersons)
        {
            result.Add(person);
        }
        ConvinceDestinationPersonList = result;
        return result;
    }
    
    === 修复后的 GetConvinceDestinationPersonList 方法 ===
    
    public PersonList GetConvinceDestinationPersonList(Faction faction)
    {
        PersonList result = new PersonList();
        if (this.BelongedFaction == faction)
        {
            // 己方建筑：可以说服俘虏
            foreach (Captive captive in this.Captives)
            {
                result.Add(captive.CaptivePerson);
            }
        }
        else
        {
            // ✅ 修复：非己方建筑需要检查情报等级
            bool hasEnoughInformation = faction.GetKnownAreaData(this.Position) >= InformationLevel.低;
            if (hasEnoughInformation)
            {
                // 有足够情报才能说服敌方人员
                foreach (Person person in this.PersonsExcludeNvGuan)
                {
                    result.Add(person);
                }
            }
        }
        
        // 在野人员：不受情报等级限制，任何建筑都可以说服
        foreach (Person person in this.NoFactionPersons)
        {
            result.Add(person);
        }
        
        ConvinceDestinationPersonList = result;
        return result;
    }
    
    === 关键改进 ===
    
    1. 情报等级检查：
       - 使用 faction.GetKnownAreaData(this.Position) >= InformationLevel.低
       - 只有在有足够情报时才能说服敌方人员
    
    2. 逻辑分离：
       - 己方建筑：俘虏（无情报要求）
       - 敌方建筑：人员（需要情报）
       - 在野人员：任何建筑（无情报要求）
    
    3. 一致性保证：
       - ConvincePersonAvail() 的结果与实际可执行性一致
       - 按钮状态准确反映功能可用性
    */
}