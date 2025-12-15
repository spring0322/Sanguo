// ===================================================================
// 军师双击功能简化版 - 参考右键菜单实现
// ===================================================================

// 在MGSshubiao.cs的partial class MainGameScreen中添加以下代码：

#region 军师双击菜单字段

// 双击检测相关
private DateTime _lastLeftClickTime = DateTime.MinValue;
private Point _lastLeftClickPosition = Point.Zero;
private const int DOUBLE_CLICK_THRESHOLD_MS = 400;
private const int DOUBLE_CLICK_DISTANCE_THRESHOLD = 10;

#endregion

#region 军师双击菜单处理

/// <summary>
/// 处理军师双击菜单的鼠标左键点击
/// </summary>
private void HandleAdvisorDoubleClickMenu()
{
    // 检测左键按下
    if (InputManager.IsDown && this.viewMove == ViewMove.Stop)
    {
        DateTime currentTime = DateTime.Now;
        Point currentPosition = new Point(InputManager.PoX, InputManager.PoY);
        
        // 检查是否为双击
        if (IsDoubleClick(currentTime, currentPosition))
        {
            // 检查是否点击在空白地形上
            if (IsEmptyTerrainClick(currentPosition))
            {
                Console.WriteLine($"[MainGameScreen] 检测到双击空白地形: {currentPosition}");
                ShowAdvisorContextMenu();
            }
        }
        
        // 更新最后点击信息
        _lastLeftClickTime = currentTime;
        _lastLeftClickPosition = currentPosition;
    }
}

/// <summary>
/// 检查是否为双击
/// </summary>
private bool IsDoubleClick(DateTime currentTime, Point currentPosition)
{
    // 检查时间间隔
    double timeDiff = (currentTime - _lastLeftClickTime).TotalMilliseconds;
    if (timeDiff > DOUBLE_CLICK_THRESHOLD_MS)
        return false;

    // 检查位置距离
    double distance = Math.Sqrt(
        Math.Pow(currentPosition.X - _lastLeftClickPosition.X, 2) +
        Math.Pow(currentPosition.Y - _lastLeftClickPosition.Y, 2)
    );
    
    return distance <= DOUBLE_CLICK_DISTANCE_THRESHOLD;
}

/// <summary>
/// 检查是否为空白地形点击
/// </summary>
private bool IsEmptyTerrainClick(Point screenPosition)
{
    try
    {
        // 1. 检查是否有建筑
        if (this.CurrentArchitecture != null)
        {
            Console.WriteLine($"[MainGameScreen] 位置有建筑: {this.CurrentArchitecture.Name}");
            return false;
        }

        // 2. 检查是否有部队
        if (this.CurrentTroop != null)
        {
            Console.WriteLine($"[MainGameScreen] 位置有部队: {this.CurrentTroop.Name}");
            return false;
        }

        // 3. 检查是否有路径
        if (this.CurrentRouteway != null)
        {
            Console.WriteLine($"[MainGameScreen] 位置有路径");
            return false;
        }

        // 4. 检查是否在UI元素上
        if (IsOverUIElement(screenPosition))
        {
            Console.WriteLine($"[MainGameScreen] 位置在UI元素上");
            return false;
        }

        Console.WriteLine($"[MainGameScreen] 位置是空白地形，可以显示军师菜单");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MainGameScreen] 检查空白地形时出错: {ex.Message}");
        return false;
    }
}

/// <summary>
/// 检查是否在UI元素上
/// </summary>
private bool IsOverUIElement(Point screenPosition)
{
    // 检查是否在各种插件UI上
    if (this.Plugins != null)
    {
        // 检查右键菜单
        if (this.Plugins.ContextMenuPlugin?.IsShowing == true)
        {
            return true;
        }

        // 检查工具栏
        if (this.Plugins.ToolBarPlugin?.IsShowing == true)
        {
            return true;
        }

        // 检查右侧面板
        if (this.Plugins.youcelanPlugin?.IsShowing == true &&
            StaticMethods.PointInRectangle(screenPosition, this.Plugins.youcelanPlugin.FrameRectangle))
        {
            return true;
        }

        // 检查其他可能的UI面板
        if (this.Plugins.TabListPlugin?.IsShowing == true ||
            this.Plugins.GameFramePlugin?.IsShowing == true)
        {
            return true;
        }
    }

    // 检查是否在屏幕边缘（可能是UI区域）
    if (screenPosition.Y > base.viewportSize.Y - 50) // 底部工具栏区域
    {
        return true;
    }

    if (screenPosition.X > base.viewportSize.X - 200) // 右侧面板区域
    {
        return true;
    }

    return false;
}

/// <summary>
/// 显示军师右键菜单（参考ContextMenuRightClick的实现）
/// </summary>
private void ShowAdvisorContextMenu()
{
    if ((this.Plugins.ContextMenuPlugin != null) && (this.PeekUndoneWork().Kind == UndoneWorkKind.None))
    {
        if (!this.Plugins.ContextMenuPlugin.IsShowing)
        {
            this.Plugins.ContextMenuPlugin.IsShowing = true;
            this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this);
            this.Plugins.ContextMenuPlugin.SetMenuKindByName("AdvisorDoubleClick");
            this.Plugins.ContextMenuPlugin.Prepare(InputManager.PoX, InputManager.PoY, base.viewportSize);
            this.bianduiLiebiaoBiaoji = "AdvisorDoubleClick";
            
            Console.WriteLine("[MainGameScreen] 显示军师双击菜单");
        }
    }
}

#endregion

// ===================================================================
// 使用说明
// ===================================================================

/*
1. 将上述代码添加到MGSshubiao.cs的partial class MainGameScreen中

2. 在HandleLaterMouseLeftDown方法的开头添加：
   HandleAdvisorDoubleClickMenu();

3. 需要在游戏的菜单配置文件中添加"AdvisorDoubleClick"菜单类型的定义

4. 这个实现的优势：
   - 使用游戏现有的ContextMenuPlugin系统
   - 代码简洁，无复杂依赖
   - 参考了右键菜单的成熟实现
   - 编译稳定，无外部依赖问题

5. 测试方法：
   - 双击空白地形应该弹出军师菜单
   - 双击建筑/部队不应该弹出军师菜单
   - 控制台会输出调试信息
*/