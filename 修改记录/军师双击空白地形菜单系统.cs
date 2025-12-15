// ===================================================================
// 军师双击空白地形菜单系统 - 只在双击空白地形时弹出
// ===================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameObjects;
using GameManager;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 军师双击空白地形菜单管理器
    /// </summary>
    public class AdvisorEmptyTerrainMenuManager
    {
        #region 私有字段
        
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickPosition = Point.Zero;
        private const int DOUBLE_CLICK_THRESHOLD_MS = 500; // 双击时间阈值
        private const int DOUBLE_CLICK_DISTANCE_THRESHOLD = 5; // 双击位置阈值
        
        private bool _isMenuVisible = false;
        private List<AdvisorMenuItem> _currentMenuItems;
        
        // 菜单显示相关
        private Rectangle _menuBounds;
        private int _selectedItemIndex = -1;
        private Point _menuPosition;
        
        #endregion

        #region 构造函数

        public AdvisorEmptyTerrainMenuManager()
        {
            _currentMenuItems = new List<AdvisorMenuItem>();
        }

        #endregion

        #region 双击检测

        /// <summary>
        /// 处理鼠标左键点击事件
        /// </summary>
        /// <param name="clickPosition">点击位置</param>
        /// <param name="gameScreen">游戏屏幕引用</param>
        public void HandleLeftClick(Point clickPosition, MainGameScreen gameScreen)
        {
            DateTime currentTime = DateTime.Now;
            
            // 检查是否为双击
            if (IsDoubleClick(currentTime, clickPosition))
            {
                // 检查是否点击在空白地形上
                if (IsEmptyTerrain(clickPosition, gameScreen))
                {
                    HandleDoubleClickOnEmptyTerrain(clickPosition, gameScreen);
                }
                else
                {
                    Console.WriteLine("[军师菜单] 双击位置不是空白地形，忽略");
                    HideMenu();
                }
            }
            else
            {
                // 单击：隐藏菜单
                HideMenu();
            }
            
            // 更新最后点击信息
            _lastClickTime = currentTime;
            _lastClickPosition = clickPosition;
        }

        /// <summary>
        /// 检查是否为双击
        /// </summary>
        private bool IsDoubleClick(DateTime currentTime, Point currentPosition)
        {
            // 检查时间间隔
            double timeDiff = (currentTime - _lastClickTime).TotalMilliseconds;
            if (timeDiff > DOUBLE_CLICK_THRESHOLD_MS)
                return false;

            // 检查位置距离
            double distance = Math.Sqrt(
                Math.Pow(currentPosition.X - _lastClickPosition.X, 2) +
                Math.Pow(currentPosition.Y - _lastClickPosition.Y, 2)
            );
            
            return distance <= DOUBLE_CLICK_DISTANCE_THRESHOLD;
        }

        /// <summary>
        /// 检查是否为空白地形
        /// </summary>
        private bool IsEmptyTerrain(Point screenPosition, MainGameScreen gameScreen)
        {
            try
            {
                // 1. 检查是否有建筑
                var architecture = gameScreen.GetArchitectureAtScreenPosition(screenPosition);
                if (architecture != null)
                {
                    Console.WriteLine($"[军师菜单] 位置有建筑: {architecture.Name}");
                    return false;
                }

                // 2. 检查是否有部队
                var troop = gameScreen.GetTroopAtScreenPosition(screenPosition);
                if (troop != null)
                {
                    Console.WriteLine($"[军师菜单] 位置有部队: {troop.Name}");
                    return false;
                }

                // 3. 检查是否有人物（如果地图上显示人物）
                var person = gameScreen.GetPersonAtScreenPosition(screenPosition);
                if (person != null)
                {
                    Console.WriteLine($"[军师菜单] 位置有人物: {person.Name}");
                    return false;
                }

                // 4. 检查地形是否可通行（排除异常地形）
                var mapPosition = gameScreen.ScreenToMapPosition(screenPosition);
                if (!IsValidMapPosition(mapPosition, gameScreen))
                {
                    Console.WriteLine($"[军师菜单] 地图位置无效或不可通行");
                    return false;
                }

                // 5. 检查是否在UI元素上
                if (IsOverUIElement(screenPosition, gameScreen))
                {
                    Console.WriteLine($"[军师菜单] 位置在UI元素上");
                    return false;
                }

                Console.WriteLine($"[军师菜单] 位置是空白地形，可以显示菜单");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[军师菜单] 检查空白地形时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查地图位置是否有效
        /// </summary>
        private bool IsValidMapPosition(Point mapPosition, MainGameScreen gameScreen)
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return false;

            // 检查是否在地图范围内
            if (mapPosition.X < 0 || mapPosition.Y < 0 ||
                mapPosition.X >= scenario.GameMap.MapDimensions.X ||
                mapPosition.Y >= scenario.GameMap.MapDimensions.Y)
            {
                return false;
            }

            // 所有在地图范围内的地形都允许双击
            // 包括平原、山地、森林、水域等各种地形
            // 只要没有建筑、军队等对象即可
            var terrain = scenario.GameMap.GetTerrainAt(mapPosition);
            return terrain != null; // 只要地形存在就允许
        }

        /// <summary>
        /// 检查是否在UI元素上
        /// </summary>
        private bool IsOverUIElement(Point screenPosition, MainGameScreen gameScreen)
        {
            // 检查是否在各种UI面板上
            // 根据你的游戏UI布局调整
            
            // 示例：检查是否在底部工具栏上
            if (screenPosition.Y > gameScreen.ScreenHeight - 100)
            {
                return true;
            }

            // 检查是否在右侧面板上
            if (screenPosition.X > gameScreen.ScreenWidth - 200)
            {
                return true;
            }

            // 检查是否在小地图上
            // if (gameScreen.IsPositionOnMiniMap(screenPosition))
            // {
            //     return true;
            // }

            return false;
        }

        #endregion

        #region 双击处理

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
        /// 处理在空白地形上的双击（原版本）
        /// </summary>
        private void HandleDoubleClickOnEmptyTerrain(Point clickPosition, MainGameScreen gameScreen)
        {
            Console.WriteLine($"[军师菜单] 在空白地形双击: {clickPosition}");

            var currentFaction = Session.Current?.Scenario?.CurrentFaction;
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
        /// 构建军师菜单
        /// </summary>
        private void BuildAdvisorMenu(Faction faction)
        {
            _currentMenuItems.Clear();

            // 1. 任命/重新任命军师
            if (faction.AppointAdvisorAvail())
            {
                if (faction.Advisor != null)
                {
                    _currentMenuItems.Add(new AdvisorMenuItem
                    {
                        ID = "ReappointAdvisor",
                        Text = "重新任命军师",
                        Description = $"当前: {faction.AdvisorName}",
                        Action = () => ShowAdvisorCandidateList(faction),
                        IsEnabled = true,
                        Icon = "🔄"
                    });
                }
                else
                {
                    _currentMenuItems.Add(new AdvisorMenuItem
                    {
                        ID = "AppointAdvisor",
                        Text = "任命军师",
                        Description = "选择智力出众的人物",
                        Action = () => ShowAdvisorCandidateList(faction),
                        IsEnabled = true,
                        Icon = "👑"
                    });
                }
            }

            // 2. 罢免军师
            if (faction.Advisor != null)
            {
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "RecallAdvisor",
                    Text = "罢免军师",
                    Description = $"解除 {faction.AdvisorName}",
                    Action = () => RecallAdvisor(faction),
                    IsEnabled = true,
                    Icon = "❌"
                });

                // 分隔线
                _currentMenuItems.Add(new AdvisorMenuItem { IsSeparator = true });

                // 3. 军师建议
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "AdvisorAdvice",
                    Text = "军师建议",
                    Description = "听取战略建议",
                    Action = () => ShowAdvisorAdvice(faction),
                    IsEnabled = true,
                    Icon = "💡"
                });

                // 4. 军师信息
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "AdvisorInfo",
                    Text = "军师信息",
                    Description = "查看详细信息",
                    Action = () => ShowAdvisorInfo(faction),
                    IsEnabled = true,
                    Icon = "ℹ️"
                });
            }

            // 5. 如果没有军师且没有候选人，显示提示
            if (_currentMenuItems.Count == 0)
            {
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "NoOptions",
                    Text = "暂无可用选项",
                    Description = "没有合适的军师候选人",
                    Action = null,
                    IsEnabled = false,
                    Icon = "⚠️"
                });
            }
        }

        #endregion

        #region 菜单操作

        /// <summary>
        /// 显示军师候选人列表
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
        /// 罢免军师
        /// </summary>
        private void RecallAdvisor(Faction faction)
        {
            if (faction.Advisor == null)
            {
                ShowMessage("当前没有军师");
                return;
            }

            string advisorName = faction.AdvisorName;
            
            // 执行罢免
            faction.AdvisorID = -1;
            faction.Advisor = null;
            
            ShowMessage($"已罢免军师 {advisorName}");
            HideMenu();
        }

        /// <summary>
        /// 显示军师建议
        /// </summary>
        private void ShowAdvisorAdvice(Faction faction)
        {
            if (faction.Advisor == null)
            {
                ShowMessage("当前没有军师");
                return;
            }

            // 这里可以集成你的军师建议系统
            ShowMessage($"军师 {faction.AdvisorName} 的建议：\n\n" +
                       "1. 加强内政建设\n" +
                       "2. 招募更多人才\n" +
                       "3. 与邻国保持友好关系");
            
            HideMenu();
        }

        /// <summary>
        /// 显示军师信息
        /// </summary>
        private void ShowAdvisorInfo(Faction faction)
        {
            if (faction.Advisor == null)
            {
                ShowMessage("当前没有军师");
                return;
            }

            var advisor = faction.Advisor;
            string info = $"军师信息：\n\n" +
                         $"姓名: {advisor.Name}\n" +
                         $"智力: {advisor.Intelligence}\n" +
                         $"政治: {advisor.Politics}\n" +
                         $"魅力: {advisor.Charm}\n" +
                         $"所在地: {advisor.LocationArchitecture?.Name ?? "未知"}";

            ShowMessage(info);
            HideMenu();
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        private void ShowMessage(string message)
        {
            Console.WriteLine($"[军师系统] {message}");
            // 这里可以集成实际的消息显示系统
            // Session.MainGame.mainGameScreen.ShowMessage(message);
        }

        #endregion

        #region 菜单显示和交互

        /// <summary>
        /// 显示菜单
        /// </summary>
        private void ShowMenu(Point position, string title)
        {
            if (_currentMenuItems.Count == 0) return;

            // 计算菜单大小
            int menuWidth = 220;
            int itemHeight = 30;
            int titleHeight = 35;
            int menuHeight = _currentMenuItems.Count * itemHeight + titleHeight + 10;

            // 调整菜单位置，确保不超出屏幕
            var screenBounds = new Rectangle(0, 0, 1920, 1080); // 根据实际屏幕大小调整
            int x = Math.Min(position.X, screenBounds.Width - menuWidth);
            int y = Math.Min(position.Y, screenBounds.Height - menuHeight);

            // 确保菜单不会太靠近边缘
            x = Math.Max(10, x);
            y = Math.Max(10, y);

            _menuBounds = new Rectangle(x, y, menuWidth, menuHeight);
            _menuPosition = new Point(x, y);
            _isMenuVisible = true;
            _selectedItemIndex = -1;

            Console.WriteLine($"[军师菜单] 显示菜单: {title}, 位置: ({x}, {y}), 项目数: {_currentMenuItems.Count}");

            // 触发菜单显示事件
            OnMenuVisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        public void HideMenu()
        {
            if (_isMenuVisible)
            {
                _isMenuVisible = false;
                _selectedItemIndex = -1;
                Console.WriteLine("[军师菜单] 隐藏菜单");
                OnMenuVisibilityChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// 处理菜单鼠标移动
        /// </summary>
        public void HandleMenuMouseMove(Point mousePosition)
        {
            if (!_isMenuVisible) return;

            // 检查鼠标是否在菜单内
            if (!_menuBounds.Contains(mousePosition))
            {
                _selectedItemIndex = -1;
                return;
            }

            // 计算鼠标在哪个菜单项上
            int titleHeight = 35;
            int relativeY = mousePosition.Y - _menuBounds.Y - titleHeight;
            int itemIndex = relativeY / 30; // 每项高度30

            if (itemIndex >= 0 && itemIndex < _currentMenuItems.Count)
            {
                var item = _currentMenuItems[itemIndex];
                if (!item.IsSeparator && item.IsEnabled)
                {
                    _selectedItemIndex = itemIndex;
                }
                else
                {
                    _selectedItemIndex = -1;
                }
            }
            else
            {
                _selectedItemIndex = -1;
            }
        }

        /// <summary>
        /// 处理菜单点击
        /// </summary>
        public void HandleMenuClick(Point mousePosition)
        {
            if (!_isMenuVisible) return;

            // 检查是否点击在菜单内
            if (!_menuBounds.Contains(mousePosition))
            {
                HideMenu();
                return;
            }

            // 执行选中的菜单项
            if (_selectedItemIndex >= 0 && _selectedItemIndex < _currentMenuItems.Count)
            {
                var selectedItem = _currentMenuItems[_selectedItemIndex];
                if (!selectedItem.IsSeparator && selectedItem.IsEnabled && selectedItem.Action != null)
                {
                    Console.WriteLine($"[军师菜单] 执行菜单项: {selectedItem.Text}");
                    selectedItem.Action.Invoke();
                }
            }
        }

        #endregion

        #region 属性和事件

        /// <summary>
        /// 菜单是否可见
        /// </summary>
        public bool IsMenuVisible => _isMenuVisible;

        /// <summary>
        /// 当前菜单边界
        /// </summary>
        public Rectangle MenuBounds => _menuBounds;

        /// <summary>
        /// 当前菜单项
        /// </summary>
        public IReadOnlyList<AdvisorMenuItem> CurrentMenuItems => _currentMenuItems.AsReadOnly();

        /// <summary>
        /// 选中的菜单项索引
        /// </summary>
        public int SelectedItemIndex => _selectedItemIndex;

        /// <summary>
        /// 菜单可见性变化事件
        /// </summary>
        public event Action<bool> OnMenuVisibilityChanged;

        #endregion
    }

    #region 菜单项数据结构

    /// <summary>
    /// 军师菜单项
    /// </summary>
    public class AdvisorMenuItem
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsSeparator { get; set; } = false;
        public Action Action { get; set; }
    }

    #endregion
}

// ===================================================================
// 游戏屏幕扩展方法 - 需要在MainGameScreen中实现
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// MainGameScreen需要实现的扩展方法
    /// </summary>
    public partial class MainGameScreen
    {
        /// <summary>
        /// 获取屏幕位置上的建筑
        /// </summary>
        public Architecture GetArchitectureAtScreenPosition(Point screenPosition)
        {
            // 实现你的建筑检测逻辑
            // 示例：
            // var mapPosition = ScreenToMapPosition(screenPosition);
            // return Session.Current.Scenario.GetArchitectureByPosition(mapPosition);
            return null;
        }

        /// <summary>
        /// 获取屏幕位置上的部队
        /// </summary>
        public Troop GetTroopAtScreenPosition(Point screenPosition)
        {
            // 实现你的部队检测逻辑
            return null;
        }

        /// <summary>
        /// 获取屏幕位置上的人物
        /// </summary>
        public Person GetPersonAtScreenPosition(Point screenPosition)
        {
            // 实现你的人物检测逻辑
            return null;
        }

        /// <summary>
        /// 屏幕坐标转地图坐标
        /// </summary>
        public Point ScreenToMapPosition(Point screenPosition)
        {
            // 实现你的坐标转换逻辑
            return Point.Zero;
        }

        /// <summary>
        /// 屏幕宽度
        /// </summary>
        public int ScreenWidth => GraphicsDevice.Viewport.Width;

        /// <summary>
        /// 屏幕高度
        /// </summary>
        public int ScreenHeight => GraphicsDevice.Viewport.Height;
    }
}

// ===================================================================
// 使用示例
// ===================================================================

public class AdvisorEmptyTerrainMenuExample
{
    private AdvisorEmptyTerrainMenuManager _menuManager;
    private MainGameScreen _gameScreen;

    public void Initialize(MainGameScreen gameScreen)
    {
        _menuManager = new AdvisorEmptyTerrainMenuManager();
        _gameScreen = gameScreen;
        
        Console.WriteLine("军师双击空白地形菜单系统已初始化");
    }

    public void HandleMouseInput(MouseState mouseState, MouseState previousMouseState)
    {
        // 检测左键点击
        if (mouseState.LeftButton == ButtonState.Released && 
            previousMouseState.LeftButton == ButtonState.Pressed)
        {
            var clickPosition = new Point(mouseState.X, mouseState.Y);
            _menuManager.HandleLeftClick(clickPosition, _gameScreen);
        }

        // 处理鼠标移动（用于菜单高亮）
        if (_menuManager.IsMenuVisible)
        {
            var mousePosition = new Point(mouseState.X, mouseState.Y);
            _menuManager.HandleMenuMouseMove(mousePosition);
        }
    }
}