using System;
using GameGlobal;
using GameObjects;

/// <summary>
/// 说服条件情报等级修复测试
/// 
/// 修复内容：
/// 1. 原来的说服条件只检查城池是否属于不同势力
/// 2. 现在增加了情报等级的判断：需要至少"低"等级的情报才能在非己方城池说服
/// 3. 在UI层面也增加了相应的检查，避免显示无法执行的说服选项
/// 
/// 测试场景：
/// - 己方势力对目标城池有"低"或以上等级情报 -> 可以说服
/// - 己方势力对目标城池只有"无"等级情报 -> 不能说服
/// - 目标是俘虏或在野人员 -> 不受情报等级限制，可以说服
/// </summary>
public class ConvinceInformationLevelTest
{
    public static void TestConvinceConditions()
    {
        Console.WriteLine("=== 说服条件情报等级修复测试 ===");
        
        // 测试场景1：有足够情报等级的非己方城池
        Console.WriteLine("\n场景1：己方对敌方城池有'低'等级情报");
        Console.WriteLine("预期结果：可以尝试说服敌方城池中的武将");
        Console.WriteLine("修复前：只要是敌方城池就可以说服");
        Console.WriteLine("修复后：需要至少'低'等级情报才能说服");
        
        // 测试场景2：情报等级不足的非己方城池
        Console.WriteLine("\n场景2：己方对敌方城池只有'无'等级情报");
        Console.WriteLine("预期结果：不能尝试说服敌方城池中的武将");
        Console.WriteLine("修复前：只要是敌方城池就可以说服");
        Console.WriteLine("修复后：情报等级不足，不能说服");
        
        // 测试场景3：俘虏和在野人员
        Console.WriteLine("\n场景3：目标是俘虏或在野人员");
        Console.WriteLine("预期结果：不受情报等级限制，可以说服");
        Console.WriteLine("修复前后：都可以说服（不受影响）");
        
        Console.WriteLine("\n=== 修复代码位置 ===");
        Console.WriteLine("1. WorldOfTheThreeKingdoms/GameObjects/Person.cs - DoConvince()方法");
        Console.WriteLine("2. WorldOfTheThreeKingdoms/GameScreens/MainGameScreen.cs - UI层检查");
        
        Console.WriteLine("\n=== 情报等级说明 ===");
        Console.WriteLine("无：没有情报，不能说服");
        Console.WriteLine("低：基础情报，可以说服");
        Console.WriteLine("中：详细情报，可以说服");
        Console.WriteLine("高：精确情报，可以说服");
        Console.WriteLine("全：完整情报，可以说服");
        
        Console.WriteLine("\n=== 游戏逻辑说明 ===");
        Console.WriteLine("这个修改使得说服功能更加符合现实逻辑：");
        Console.WriteLine("- 需要对目标城池有一定了解才能进行说服");
        Console.WriteLine("- 鼓励玩家先派遣人员收集情报");
        Console.WriteLine("- 增加了策略深度和游戏平衡性");
    }
}

/// <summary>
/// 修复前后的代码对比
/// </summary>
public class CodeComparison
{
    /*
    === Person.cs - DoConvince()方法修复 ===
    
    修复前的条件：
    if ((architectureByPosition != null) && (
        (this.ConvincingPerson.IsCaptive || 
         this.ConvincingPerson.Status == PersonStatus.NoFaction || 
         (architectureByPosition.BelongedFaction != this.BelongedFaction))))
    
    修复后的条件：
    bool canAttemptConvince = this.ConvincingPerson.IsCaptive || 
                            this.ConvincingPerson.Status == PersonStatus.NoFaction ||
                            (architectureByPosition.BelongedFaction != this.BelongedFaction && 
                             this.BelongedFaction.GetKnownAreaData(this.OutsideDestination.Value) >= InformationLevel.低);
    
    if ((architectureByPosition != null) && canAttemptConvince)
    
    === MainGameScreen.cs - UI层修复 ===
    
    修复前：
    直接显示说服选项，不检查情报等级
    
    修复后：
    // 检查是否有足够的情报等级来进行说服
    Faction playerFaction = (this.CurrentPersons[0] as Person).BelongedFaction;
    bool hasEnoughInformation = architectureByPosition.BelongedFaction == playerFaction || 
                              playerFaction.GetKnownAreaData(this.selectingLayer.SelectedPoint) >= InformationLevel.低;
    
    if (hasEnoughInformation)
    {
        // 显示说服选项
    }
    // 如果情报不足，不显示说服选项
    
    关键变化：
    1. 增加了情报等级检查：GetKnownAreaData(position) >= InformationLevel.低
    2. 只有在有足够情报的情况下才能在敌方城池说服
    3. 俘虏和在野人员不受此限制
    4. UI层面也进行了相应的检查
    */
}