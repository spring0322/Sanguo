// ===================================================================
// 军师双击菜单系统 - 双击左键弹出军师专用菜单
// ===================================================================

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameObjects;
using GameManager;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 军师双击菜单管理器
    /// </summary>
    public class AdvisorDoubleClickMenuManager
    {
        #region 私有字段
        
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickPosition = Point.Zero;
        private const int DOUBLE_CLICK_THRESHOLD_MS = 500; // 双击时间阈值
        private const int DOUBLE_CLICK_DISTANCE_THRESHOLD = 5; // 双击位置阈值
        
        private bool _isMenuVisible = false;
        private AdvisorContextMenuHandler _menuHandler;
        private List<AdvisorMenuItem> _currentMenuItems;
        
        // 菜单显示相关
        private Rectangle _menuBounds;
        private int _selectedItemIndex = -1;
        
        #endregion

        #region 构造函数

        public AdvisorDoubleClickMenuManager()
        {
            _menuHandler = new AdvisorContextMenuHandler();
            _currentMenuItems = new List<AdvisorMenuItem>();
        }

        #endregion

        #region 双击检测

        /// <summary>
        /// 处理鼠标左键点击事件
        /// </summary>
        /// <param name="clickPosition">点击位置</param>
        /// <param name="clickedObject">点击的对象</param>
        public void HandleLeftClick(Point clickPosition, GameObject clickedObject)
        {
            DateTime currentTime = DateTime.Now;
            
            // 检查是否为双击
            if (IsDoubleClick(currentTime, clickPosition))
            {
                HandleDoubleClick(clickPosition, clickedObject);
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

        #endregion

        #region 双击处理

        /// <summary>
        /// 处理双击事件
        /// </summary>
        private void HandleDoubleClick(Point clickPosition, GameObject clickedObject)
        {
            Console.WriteLine($"[军师菜单] 检测到双击: {clickPosition}, 对象: {clickedObject?.GetType().Name}");

            // 根据点击的对象类型显示不同的菜单
            switch (clickedObject)
            {
                case Faction faction:
                    ShowFactionAdvisorMenu(clickPosition, faction);
                    break;
                    
                case Person person:
                    ShowPersonAdvisorMenu(clickPosition, person);
                    break;
                    
                case Architecture architecture:
                    ShowArchitectureAdvisorMenu(clickPosition, architecture);
                    break;
                    
                default:
                    // 双击空白区域，显示当前势力的军师菜单
                    var currentFaction = Session.Current?.Scenario?.CurrentFaction;
                    if (currentFaction != null)
                    {
                        ShowFactionAdvisorMenu(clickPosition, currentFaction);
                    }
                    break;
            }
        }

        #endregion

        #region 菜单显示

        /// <summary>
        /// 显示势力军师菜单
        /// </summary>
        private void ShowFactionAdvisorMenu(Point position, Faction faction)
        {
            _currentMenuItems.Clear();

            var context = new MenuActionContext
            {
                Faction = faction,
                Person = null,
                Architecture = null
            };

            // 添加基础军师操作
            if (faction.AppointAdvisorAvail())
            {
                if (faction.Advisor != null)
                {
                    _currentMenuItems.Add(new AdvisorMenuItem
                    {
                        ID = "ReappointAdvisor",
                        Text = "重新任命军师",
                        Description = $"当前军师: {faction.AdvisorName}",
                        Action = () => _menuHandler.ExecuteAction("ShowAdvisorReappointmentList", context),
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
                        Description = "选择一位智力出众的人物担任军师",
                        Action = () => _menuHandler.ExecuteAction("ShowAdvisorCandidateList", context),
                        IsEnabled = true,
                        Icon = "👑"
                    });
                }
            }

            // 罢免军师
            if (faction.Advisor != null)
            {
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "RecallAdvisor",
                    Text = "罢免军师",
                    Description = $"解除 {faction.AdvisorName} 的军师职务",
                    Action = () => _menuHandler.ExecuteAction("RecallCurrentAdvisor", context),
                    IsEnabled = true,
                    Icon = "❌"
                });

                // 分隔线
                _currentMenuItems.Add(new AdvisorMenuItem { IsSeparator = true });

                // 军师功能
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "AdvisorAdvice",
                    Text = "军师建议",
                    Description = "听取军师的战略建议",
                    Action = () => _menuHandler.ExecuteAction("ShowAdvisorAdvice", context),
                    IsEnabled = true,
                    Icon = "💡"
                });

                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "AdvisorInfo",
                    Text = "军师信息",
                    Description = "查看军师的详细信息",
                    Action = () => _menuHandler.ExecuteAction("ShowAdvisorInfo", context),
                    IsEnabled = true,
                    Icon = "ℹ️"
                });
            }

            // 显示菜单
            ShowMenu(position, $"势力军师菜单 - {faction.Name}");
        }

        /// <summary>
        /// 显示人物军师菜单
        /// </summary>
        private void ShowPersonAdvisorMenu(Point position, Person person)
        {
            _currentMenuItems.Clear();

            var context = new MenuActionContext
            {
                Faction = person.BelongedFaction,
                Person = person,
                Architecture = null
            };

            // 检查是否可以任命为军师
            if (IsPersonAdvisorCandidate(person))
            {
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "AppointAsAdvisor",
                    Text = "任命为军师",
                    Description = $"任命 {person.Name} 为军师 (智力: {person.Intelligence})",
                    Action = () => _menuHandler.ExecuteAction("AppointPersonAsAdvisor", context),
                    IsEnabled = true,
                    Icon = "👑"
                });
            }

            // 检查是否是当前军师
            if (person == person.BelongedFaction?.Advisor)
            {
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "RecallFromAdvisor",
                    Text = "罢免军师职务",
                    Description = $"解除 {person.Name} 的军师职务",
                    Action = () => _menuHandler.ExecuteAction("RecallPersonFromAdvisor", context),
                    IsEnabled = true,
                    Icon = "❌"
                });
            }

            // 查看军师能力评估
            if (person.Intelligence >= 60)
            {
                if (_currentMenuItems.Count > 0)
                {
                    _currentMenuItems.Add(new AdvisorMenuItem { IsSeparator = true });
                }

                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "ViewAdvisorAbility",
                    Text = "查看军师能力",
                    Description = $"评估 {person.Name} 作为军师的能力",
                    Action = () => _menuHandler.ExecuteAction("ShowAdvisorAbilityAssessment", context),
                    IsEnabled = true,
                    Icon = "📊"
                });
            }

            // 显示菜单
            if (_currentMenuItems.Count > 0)
            {
                ShowMenu(position, $"人物军师菜单 - {person.Name}");
            }
        }

        /// <summary>
        /// 显示建筑军师菜单
        /// </summary>
        private void ShowArchitectureAdvisorMenu(Point position, Architecture architecture)
        {
            _currentMenuItems.Clear();

            var context = new MenuActionContext
            {
                Faction = architecture.BelongedFaction,
                Person = null,
                Architecture = architecture
            };

            var faction = architecture.BelongedFaction;
            if (faction?.Advisor != null)
            {
                // 派遣军师
                if (faction.Advisor.LocationArchitecture != architecture)
                {
                    _currentMenuItems.Add(new AdvisorMenuItem
                    {
                        ID = "SendAdvisor",
                        Text = "派遣军师",
                        Description = $"派遣 {faction.AdvisorName} 到 {architecture.Name}",
                        Action = () => _menuHandler.ExecuteAction("SendAdvisorToArchitecture", context),
                        IsEnabled = true,
                        Icon = "📤"
                    });
                }
                else
                {
                    // 召回军师
                    _currentMenuItems.Add(new AdvisorMenuItem
                    {
                        ID = "RecallAdvisorFromArchitecture",
                        Text = "召回军师",
                        Description = $"从 {architecture.Name} 召回 {faction.AdvisorName}",
                        Action = () => _menuHandler.ExecuteAction("RecallAdvisorFromArchitecture", context),
                        IsEnabled = true,
                        Icon = "📥"
                    });
                }
            }

            // 显示菜单
            if (_currentMenuItems.Count > 0)
            {
                ShowMenu(position, $"建筑军师菜单 - {architecture.Name}");
            }
        }

        /// <summary>
        /// 显示菜单
        /// </summary>
        private void ShowMenu(Point position, string title)
        {
            if (_currentMenuItems.Count == 0) return;

            // 计算菜单大小
            int menuWidth = 200;
            int itemHeight = 25;
            int menuHeight = _currentMenuItems.Count * itemHeight + 30; // 额外空间给标题

            // 调整菜单位置，确保不超出屏幕
            var screenBounds = Screen.PrimaryScreen.Bounds;
            int x = Math.Min(position.X, screenBounds.Width - menuWidth);
            int y = Math.Min(position.Y, screenBounds.Height - menuHeight);

            _menuBounds = new Rectangle(x, y, menuWidth, menuHeight);
            _isMenuVisible = true;
            _selectedItemIndex = -1;

            Console.WriteLine($"[军师菜单] 显示菜单: {title}, 位置: ({x}, {y}), 项目数: {_currentMenuItems.Count}");

            // 这里可以触发UI重绘事件
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

        #endregion

        #region 菜单交互

        /// <summary>
        /// 处理菜单鼠标移动
        /// </summary>
        public void HandleMenuMouseMove(Point mousePosition)
        {
            if (!_isMenuVisible) return;

            // 计算鼠标在哪个菜单项上
            int relativeY = mousePosition.Y - _menuBounds.Y - 30; // 减去标题高度
            int itemIndex = relativeY / 25; // 每项高度25

            if (itemIndex >= 0 && itemIndex < _currentMenuItems.Count)
            {
                var item = _currentMenuItems[itemIndex];
                if (!item.IsSeparator && item.IsEnabled)
                {
                    _selectedItemIndex = itemIndex;
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
                    HideMenu();
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查人物是否可以成为军师候选人
        /// </summary>
        private bool IsPersonAdvisorCandidate(Person person)
        {
            var faction = person.BelongedFaction;
            if (faction == null) return false;

            return person.Alive &&
                   person.Available &&
                   person.BelongedCaptive == null &&
                   person.LocationTroop == null &&
                   person != faction.Leader &&
                   person != faction.Advisor &&
                   person.Intelligence >= 70;
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

    #region 游戏集成

    /// <summary>
    /// 军师双击菜单游戏集成
    /// </summary>
    public class AdvisorDoubleClickMenuIntegration
    {
        private AdvisorDoubleClickMenuManager _menuManager;
        private bool _isEnabled = true;

        public AdvisorDoubleClickMenuIntegration()
        {
            _menuManager = new AdvisorDoubleClickMenuManager();
            
            // 订阅菜单事件
            _menuManager.OnMenuVisibilityChanged += OnMenuVisibilityChanged;
        }

        /// <summary>
        /// 处理游戏中的鼠标事件
        /// </summary>
        public void HandleGameMouseEvent(MouseEventType eventType, Point position, GameObject clickedObject = null)
        {
            if (!_isEnabled) return;

            switch (eventType)
            {
                case MouseEventType.LeftClick:
                    _menuManager.HandleLeftClick(position, clickedObject);
                    break;

                case MouseEventType.MouseMove:
                    _menuManager.HandleMenuMouseMove(position);
                    break;

                case MouseEventType.MenuClick:
                    _menuManager.HandleMenuClick(position);
                    break;
            }
        }

        /// <summary>
        /// 启用/禁用双击菜单
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                _menuManager.HideMenu();
            }
        }

        /// <summary>
        /// 菜单可见性变化处理
        /// </summary>
        private void OnMenuVisibilityChanged(bool isVisible)
        {
            // 这里可以通知游戏UI系统菜单状态变化
            Console.WriteLine($"[军师菜单集成] 菜单可见性: {isVisible}");
            
            // 可以在这里暂停游戏或改变鼠标光标等
            if (isVisible)
            {
                // 菜单显示时的处理
            }
            else
            {
                // 菜单隐藏时的处理
            }
        }

        /// <summary>
        /// 获取菜单管理器（用于渲染）
        /// </summary>
        public AdvisorDoubleClickMenuManager GetMenuManager()
        {
            return _menuManager;
        }
    }

    /// <summary>
    /// 鼠标事件类型
    /// </summary>
    public enum MouseEventType
    {
        LeftClick,
        MouseMove,
        MenuClick
    }

    #endregion
}

// ===================================================================
// 使用示例
// ===================================================================

public class AdvisorDoubleClickMenuExample
{
    private AdvisorDoubleClickMenuIntegration _menuIntegration;

    public void Initialize()
    {
        // 初始化双击菜单系统
        _menuIntegration = new AdvisorDoubleClickMenuIntegration();
        
        Console.WriteLine("军师双击菜单系统已初始化");
    }

    public void HandleMouseInput(MouseState mouseState, MouseState previousMouseState)
    {
        // 检测左键点击
        if (mouseState.LeftButton == ButtonState.Released && 
            previousMouseState.LeftButton == ButtonState.Pressed)
        {
            var clickPosition = new Point(mouseState.X, mouseState.Y);
            var clickedObject = GetObjectAtPosition(clickPosition); // 你的对象检测逻辑
            
            _menuIntegration.HandleGameMouseEvent(MouseEventType.LeftClick, clickPosition, clickedObject);
        }

        // 处理鼠标移动
        if (mouseState.X != previousMouseState.X || mouseState.Y != previousMouseState.Y)
        {
            var mousePosition = new Point(mouseState.X, mouseState.Y);
            _menuIntegration.HandleGameMouseEvent(MouseEventType.MouseMove, mousePosition);
        }
    }

    private GameObject GetObjectAtPosition(Point position)
    {
        // 实现你的对象检测逻辑
        // 返回在指定位置的游戏对象（Faction, Person, Architecture等）
        return null;
    }

    public void RenderMenu(/* 你的渲染参数 */)
    {
        var menuManager = _menuIntegration.GetMenuManager();
        
        if (menuManager.IsMenuVisible)
        {
            // 渲染菜单背景
            // RenderMenuBackground(menuManager.MenuBounds);
            
            // 渲染菜单项
            var menuItems = menuManager.CurrentMenuItems;
            for (int i = 0; i < menuItems.Count; i++)
            {
                var item = menuItems[i];
                bool isSelected = i == menuManager.SelectedItemIndex;
                
                // RenderMenuItem(item, isSelected);
            }
        }
    }
}