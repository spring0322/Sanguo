// 军师按钮点击测试代码
// 用于验证军师按钮是否能正确响应点击并显示候选人

using System;
using GameObjects;
using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class AdvisorButtonClickTest
{
    /// <summary>
    /// 测试军师按钮点击检测
    /// </summary>
    public static void TestButtonClickDetection()
    {
        Console.WriteLine("=== 军师按钮点击测试 ===");
        
        // 模拟鼠标点击
        MouseState currentMouse = Mouse.GetState();
        Point mousePos = currentMouse.Position;
        
        Console.WriteLine($"当前鼠标位置: {mousePos.X}, {mousePos.Y}");
        Console.WriteLine($"鼠标左键状态: {currentMouse.LeftButton}");
        
        // 检查StrategistUI是否存在
        var mainGameScreen = Session.MainGame?.mainGameScreen;
        if (mainGameScreen != null)
        {
            Console.WriteLine("✅ MainGameScreen 可用");
            
            // 检查当前势力
            var faction = Session.Current?.Scenario?.CurrentPlayer;
            if (faction != null)
            {
                Console.WriteLine($"✅ 当前势力: {faction.Name}");
                
                // 检查候选人
                var candidates = faction.AdvisorCandicate;
                Console.WriteLine($"候选人数量: {candidates.Count}");
                
                if (candidates.Count > 0)
                {
                    Console.WriteLine("候选人列表:");
                    foreach (Person p in candidates)
                    {
                        Console.WriteLine($"  - {p.Name} (智力:{p.Intelligence})");
                    }
                    
                    // 模拟调用ShowTabListInFrame
                    Console.WriteLine("\n🎯 模拟调用军师任命界面...");
                    try
                    {
                        string title = faction.Advisor != null ? "重新任命军师" : "任命军师";
                        
                        mainGameScreen.ShowTabListInFrame(
                            UndoneWorkKind.Frame,
                            FrameKind.Person,
                            FrameFunction.AppointAdvisor,
                            false, true, true, false,
                            candidates,
                            null,
                            title,
                            ""
                        );
                        
                        Console.WriteLine("✅ ShowTabListInFrame 调用成功");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ ShowTabListInFrame 调用失败: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 没有合适的候选人");
                    
                    // 详细分析原因
                    Console.WriteLine("\n分析候选人条件:");
                    foreach (Person p in faction.Persons)
                    {
                        bool isLeader = p == faction.Leader;
                        bool isAdvisor = p == faction.Advisor;
                        bool isAvailable = p.Available;
                        bool isAlive = p.Alive;
                        bool notCaptive = p.BelongedCaptive == null;
                        bool notInTroop = p.LocationTroop == null;
                        bool smartEnough = p.Intelligence >= 60;
                        
                        bool qualified = !isLeader && !isAdvisor && isAvailable && isAlive && 
                                       notCaptive && notInTroop && smartEnough;
                        
                        if (!qualified)
                        {
                            Console.WriteLine($"  {p.Name}: 不合格");
                            if (isLeader) Console.WriteLine($"    - 是君主");
                            if (isAdvisor) Console.WriteLine($"    - 是当前军师");
                            if (!isAvailable) Console.WriteLine($"    - 不可用");
                            if (!isAlive) Console.WriteLine($"    - 已死亡");
                            if (!notCaptive) Console.WriteLine($"    - 被俘虏");
                            if (!notInTroop) Console.WriteLine($"    - 在部队中");
                            if (!smartEnough) Console.WriteLine($"    - 智力不足({p.Intelligence} < 60)");
                        }
                        else
                        {
                            Console.WriteLine($"  {p.Name}: ✅ 合格 (智力:{p.Intelligence})");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("❌ 当前没有玩家势力");
            }
        }
        else
        {
            Console.WriteLine("❌ MainGameScreen 不可用");
        }
    }
    
    /// <summary>
    /// 测试按钮区域检测
    /// </summary>
    public static void TestButtonArea(Vector2 panelPosition, int buttonSize)
    {
        Console.WriteLine("\n=== 按钮区域测试 ===");
        
        int x = (int)panelPosition.X;
        int y = (int)panelPosition.Y;
        
        // 军师按钮位置：x - ButtonSize - 10, y
        int buttonX = x - buttonSize - 10;
        int buttonY = y;
        
        Rectangle buttonRect = new Rectangle(buttonX, buttonY, buttonSize, buttonSize);
        
        Console.WriteLine($"面板位置: {x}, {y}");
        Console.WriteLine($"按钮位置: {buttonX}, {buttonY}");
        Console.WriteLine($"按钮尺寸: {buttonSize} x {buttonSize}");
        Console.WriteLine($"按钮区域: {buttonRect}");
        
        // 检查鼠标是否在按钮区域内
        MouseState mouse = Mouse.GetState();
        Point mousePos = mouse.Position;
        bool inButtonArea = buttonRect.Contains(mousePos);
        
        Console.WriteLine($"鼠标位置: {mousePos.X}, {mousePos.Y}");
        Console.WriteLine($"在按钮区域内: {inButtonArea}");
        
        if (inButtonArea)
        {
            Console.WriteLine("🎯 鼠标在按钮区域内，应该能检测到点击");
        }
        else
        {
            Console.WriteLine("⚠️ 鼠标不在按钮区域内");
            
            // 计算距离
            int centerX = buttonX + buttonSize / 2;
            int centerY = buttonY + buttonSize / 2;
            double distance = Math.Sqrt(Math.Pow(mousePos.X - centerX, 2) + Math.Pow(mousePos.Y - centerY, 2));
            Console.WriteLine($"距离按钮中心: {distance:F1} 像素");
        }
    }
    
    /// <summary>
    /// 强制触发军师任命界面（用于测试）
    /// </summary>
    public static void ForceShowAdvisorInterface()
    {
        Console.WriteLine("\n=== 强制显示军师界面测试 ===");
        
        try
        {
            var faction = Session.Current?.Scenario?.CurrentPlayer;
            var mainGameScreen = Session.MainGame?.mainGameScreen;
            
            if (faction != null && mainGameScreen != null)
            {
                var candidates = faction.AdvisorCandicate;
                
                Console.WriteLine($"势力: {faction.Name}");
                Console.WriteLine($"候选人数: {candidates.Count}");
                
                if (candidates.Count > 0)
                {
                    string title = faction.Advisor != null ? "重新任命军师" : "任命军师";
                    
                    Console.WriteLine($"调用 ShowTabListInFrame: {title}");
                    
                    mainGameScreen.ShowTabListInFrame(
                        UndoneWorkKind.Frame,
                        FrameKind.Person,
                        FrameFunction.AppointAdvisor,
                        false, true, true, false,
                        candidates,
                        null,
                        title,
                        ""
                    );
                    
                    Console.WriteLine("✅ 界面应该已经显示");
                }
                else
                {
                    Console.WriteLine("❌ 没有候选人，无法显示界面");
                }
            }
            else
            {
                Console.WriteLine("❌ 势力或MainGameScreen不可用");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 强制显示失败: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }
}

// 使用方法：
// 在游戏中调用以下方法进行测试：
// AdvisorButtonClickTest.TestButtonClickDetection();
// AdvisorButtonClickTest.ForceShowAdvisorInterface();