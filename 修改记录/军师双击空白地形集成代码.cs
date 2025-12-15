// ===================================================================
// 军师双击空白地形集成代码 - 集成到MainGameScreen
// ===================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameObjects;
using GameManager;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// MainGameScreen的军师双击空白地形扩展
    /// </summary>
    public partial class MainGameScreen
    {
        #region 军师双击菜单字段

        // 军师双击菜单管理器
        private AdvisorEmptyTerrainMenuManager _advisorMenuManager;
        
        // 双击检测相关
        private DateTime _lastLeftClickTime = DateTime.MinValue;
        private Point _lastLeftClickPosition = Point.Zero;
        private const int DOUBLE_CLICK_THRESHOLD_MS = 500;
        private const int DOUBLE_CLICK_DISTANCE_THRESHOLD = 5;

        #endregion

        #region 初始化军师双击菜单

        /// <summary>
        /// 初始化军师双击菜单系统
        /// 在MainGameScreen的构造函数或Initialize方法中调用
        /// </summary>
        private void InitializeAdvisorDoubleClickMenu()
        {
            _advisorMenuManager = new AdvisorEmptyTerrainMenuManager();
            
            // 订阅菜单可见性变化事件
            _advisorMenuManager.OnMenuVisibilityChanged += OnAdvisorMenuVisibilityChanged;
            
            Console.WriteLine("[MainGameScreen] 军师双击空白地形菜单系统已初始化");
        }

        /// <summary>
        /// 菜单可见性变化事件处理
        /// </summary>
        private void OnAdvisorMenuVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                // 菜单显示时，可以暂停其他输入处理
                Console.WriteLine("[MainGameScreen] 军师菜单已显示");
            }
            else
            {
                Console.WriteLine("[MainGameScreen] 军师菜单已隐藏");
            }
        }

        #endregion

        #region 双击检测和处理

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
        /// 处理军师双击菜单的鼠标左键点击
        /// 在HandleLaterMouseLeftDown方法中调用
        /// </summary>
        private void HandleAdvisorDoubleClickMenu()
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
                    ShowAdvisorDoubleClickMenu(currentPosition);
                }
                else
                {
                    // 不是空白地形，隐藏菜单
                    _advisorMenuManager?.HideMenu();
                }
            }
            else
            {
                // 单击：隐藏菜单
                _advisorMenuManager?.HideMenu();
            }
            
            // 更新最后点击信息
            _lastLeftClickTime = currentTime;
            _lastLeftClickPosition = currentPosition;
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

                // 5. 检查地图位置是否有效
                var mapPosition = this.GetPositionByPoint(screenPosition);
                if (!IsValidMapPosition(mapPosition))
                {
                    Console.WriteLine($"[MainGameScreen] 地图位置无效");
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
        /// 检查地图位置是否有效
        /// </summary>
        private bool IsValidMapPosition(Point mapPosition)
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return false;

            // 检查是否在地图范围内
            if (mapPosition.X < 0 || mapPosition.Y < 0 ||
                mapPosition.X >= scenario.ScenarioMap.MapDimensions.X ||
                mapPosition.Y >= scenario.ScenarioMap.MapDimensions.Y)
            {
                return false;
            }

            // 所有在地图范围内的地形都允许双击
            return true;
        }

        /// <summary>
        /// 显示军师双击菜单
        /// </summary>
        private void ShowAdvisorDoubleClickMenu(Point clickPosition)
        {
            var currentFaction = Session.Current?.Scenario?.CurrentFaction;
            if (currentFaction == null)
            {
                Console.WriteLine("[MainGameScreen] 没有当前势力，无法显示军师菜单");
                return;
            }

            // 调用菜单管理器处理双击
            _advisorMenuManager?.HandleDoubleClickOnEmptyTerrain(clickPosition, currentFaction);
        }

        #endregion

        #region 鼠标移动和点击处理

        /// <summary>
        /// 处理军师菜单的鼠标移动
        /// 在HandleLaterMouseMove方法中调用
        /// </summary>
        private void HandleAdvisorMenuMouseMove()
        {
            if (_advisorMenuManager?.IsMenuVisible == true)
            {
                var mousePosition = new Point(InputManager.PoX, InputManager.PoY);
                _advisorMenuManager.HandleMenuMouseMove(mousePosition);
            }
        }

        /// <summary>
        /// 处理军师菜单的鼠标点击
        /// 在鼠标点击处理中调用
        /// </summary>
        private void HandleAdvisorMenuClick()
        {
            if (_advisorMenuManager?.IsMenuVisible == true)
            {
                var mousePosition = new Point(InputManager.PoX, InputManager.PoY);
                _advisorMenuManager.HandleMenuClick(mousePosition);
            }
        }

        #endregion

        #region 菜单渲染

        /// <summary>
        /// 渲染军师双击菜单
        /// 在Draw方法中调用
        /// </summary>
        private void DrawAdvisorDoubleClickMenu()
        {
            if (_advisorMenuManager?.IsMenuVisible == true)
            {
                // 这里需要实现菜单的渲染
                // 可以使用现有的UI系统或创建简单的渲染
                DrawSimpleAdvisorMenu();
            }
        }

        /// <summary>
        /// 简单的军师菜单渲染
        /// </summary>
        private void DrawSimpleAdvisorMenu()
        {
            var menuBounds = _advisorMenuManager.MenuBounds;
            var menuItems = _advisorMenuManager.CurrentMenuItems;
            int selectedIndex = _advisorMenuManager.SelectedItemIndex;

            // 使用游戏现有的绘制系统
            // 这里需要根据你的游戏UI系统来实现具体的绘制逻辑
            
            // 示例：使用CacheManager绘制菜单背景
            if (this.Textures?.SelectorTexture != null)
            {
                CacheManager.Draw(this.Textures.SelectorTexture.Name, 
                    menuBounds, null, Color.LightBlue * 0.8f, 
                    SpriteEffects.None, 0.9f);
            }

            // 绘制菜单项文字（这里需要根据实际的字体系统调整）
            // 示例代码，需要根据实际情况修改
            /*
            for (int i = 0; i < menuItems.Count; i++)
            {
                var item = menuItems[i];
                if (!item.IsSeparator)
                {
                    var itemBounds = new Rectangle(
                        menuBounds.X + 5,
                        menuBounds.Y + 25 + i * 25,
                        menuBounds.Width - 10,
                        20
                    );

                    Color textColor = item.IsEnabled ? Color.Black : Color.Gray;
                    if (i == selectedIndex) textColor = Color.Blue;

                    // 这里需要使用实际的字体绘制方法
                    // DrawText(item.Text, itemBounds, textColor);
                }
            }
            */
        }

        #endregion
    }

    #region 军师菜单管理器扩展

    /// <summary>
    /// 军师空白地形菜单管理器的MainGameScreen扩展
    /// </summary>
    public partial class AdvisorEmptyTerrainMenuManager
    {
        /// <summary>
        /// 处理在空白地形上的双击（MainGameScreen版本）
        /// </summary>
        public void HandleDoubleClickOnEmptyTerrain(Point clickPosition, Faction currentFaction)
        {
            Console.WriteLine($"[军师菜单] 在空白地形双击: {clickPosition}");

            if (currentFaction == null)
            {
                Console.WriteLine("[军师菜单] 没有当前势力");
                return;
            }

            // 构建军师菜单
            BuildAdvisorMenu(currentFaction);

            // 显示菜单
            if (_currentMenuItems.Count > 0)
            {
                ShowMenu(clickPosition, $"军师管理 - {currentFaction.Name}");
            }
            else
            {
                Console.WriteLine("[军师菜单] 没有可用的菜单项");
            }
        }

        /// <summary>
        /// 显示军师候选人列表（MainGameScreen版本）
        /// </summary>
        private void ShowAdvisorCandidateList(Faction faction)
        {
            var candidates = faction.AdvisorCandicate;
            if (candidates.Count == 0)
            {
                ShowMessage("没有合适的军师候选人");
                return;
            }

            // 使用MainGameScreen的现有UI系统
            var mainGameScreen = Session.MainGame?.mainGameScreen as MainGameScreen;
            if (mainGameScreen != null)
            {
                mainGameScreen.ShowTabListInFrame(
                    UndoneWorkKind.Frame,
                    FrameKind.Person,
                    FrameFunction.AppointAdvisor,
                    false, true, true, false,
                    candidates,
                    null,
                    faction.Advisor != null ? "重新任命军师" : "任命军师",
                    ""
                );
            }

            HideMenu();
        }

        /// <summary>
        /// 显示消息（MainGameScreen版本）
        /// </summary>
        private void ShowMessage(string message)
        {
            Console.WriteLine($"[军师系统] {message}");
            
            // 可以集成到游戏的消息系统
            // 例如：Session.MainGame?.mainGameScreen?.ShowMessage(message);
        }
    }

    #endregion
}

// ===================================================================
// 集成说明和使用指南
// ===================================================================

/*
集成步骤：

1. 在MainGameScreen的构造函数或Initialize方法中添加：
   InitializeAdvisorDoubleClickMenu();

2. 在HandleLaterMouseLeftDown方法中添加（在现有逻辑之前）：
   HandleAdvisorDoubleClickMenu();

3. 在HandleLaterMouseMove方法中添加：
   HandleAdvisorMenuMouseMove();

4. 在Draw方法的最后添加：
   DrawAdvisorDoubleClickMenu();

5. 确保引用了军师双击空白地形菜单系统的相关文件

具体修改位置：

MainGameScreen构造函数：
```csharp
public MainGameScreen(ScreenManager screenManager)
{
    // 现有初始化代码...
    
    // 添加军师双击菜单初始化
    InitializeAdvisorDoubleClickMenu();
}
```

MGSshubiao.cs的HandleLaterMouseLeftDown方法：
```csharp
private void HandleLaterMouseLeftDown()
{
    // 首先处理军师双击菜单
    HandleAdvisorDoubleClickMenu();
    
    // 现有的鼠标处理逻辑...
    if (InputManager.IsDown && this.viewMove == ViewMove.Stop)
    {
        // 现有代码...
    }
}
```

HandleLaterMouseMove方法中添加：
```csharp
private void HandleLaterMouseMove()
{
    // 处理军师菜单鼠标移动
    HandleAdvisorMenuMouseMove();
    
    // 现有的鼠标移动处理逻辑...
}
```

Draw方法中添加：
```csharp
public override void Draw(GameTime gameTime)
{
    // 现有的绘制逻辑...
    
    // 在最后绘制军师菜单（确保在最上层）
    DrawAdvisorDoubleClickMenu();
}
```

注意事项：
1. 需要确保AdvisorEmptyTerrainMenuManager类已经包含在项目中
2. 可能需要调整菜单渲染代码以适配游戏的UI系统
3. 双击检测的时间和距离阈值可以根据需要调整
4. 菜单样式和位置可以根据游戏UI风格进行调整
*/