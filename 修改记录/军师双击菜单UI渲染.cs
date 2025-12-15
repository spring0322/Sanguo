// ===================================================================
// 军师双击菜单UI渲染 - 处理菜单的绘制和显示
// ===================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 军师双击菜单渲染器
    /// </summary>
    public class AdvisorDoubleClickMenuRenderer
    {
        #region 私有字段

        private SpriteBatch _spriteBatch;
        private SpriteFont _menuFont;
        private SpriteFont _titleFont;
        
        // 菜单样式
        private Texture2D _menuBackground;
        private Texture2D _menuItemBackground;
        private Texture2D _menuItemHover;
        private Texture2D _separatorTexture;
        
        // 颜色配置
        private Color _menuBackgroundColor = new Color(240, 248, 255, 230); // 半透明蓝白色
        private Color _menuBorderColor = new Color(70, 130, 180, 255);      // 钢蓝色
        private Color _menuTextColor = new Color(0, 0, 128, 255);           // 深蓝色
        private Color _menuHoverColor = new Color(135, 206, 235, 180);      // 天蓝色
        private Color _menuDisabledColor = new Color(128, 128, 128, 255);   // 灰色
        private Color _titleColor = new Color(25, 25, 112, 255);            // 深蓝色

        // 布局配置
        private const int MENU_PADDING = 8;
        private const int ITEM_HEIGHT = 25;
        private const int TITLE_HEIGHT = 30;
        private const int ICON_SIZE = 16;
        private const int ICON_MARGIN = 5;

        #endregion

        #region 构造函数

        public AdvisorDoubleClickMenuRenderer(GraphicsDevice graphicsDevice, ContentManager content)
        {
            _spriteBatch = new SpriteBatch(graphicsDevice);
            LoadContent(content, graphicsDevice);
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 加载渲染资源
        /// </summary>
        private void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            try
            {
                // 加载字体
                _menuFont = content.Load<SpriteFont>("Font/MenuFont");
                _titleFont = content.Load<SpriteFont>("Font/TitleFont");
            }
            catch
            {
                // 如果加载失败，使用默认字体
                Console.WriteLine("[军师菜单] 警告: 无法加载自定义字体，使用默认字体");
            }

            // 创建基础纹理
            CreateBasicTextures(graphicsDevice);
        }

        /// <summary>
        /// 创建基础纹理
        /// </summary>
        private void CreateBasicTextures(GraphicsDevice graphicsDevice)
        {
            // 创建1x1像素的纯色纹理
            _menuBackground = new Texture2D(graphicsDevice, 1, 1);
            _menuBackground.SetData(new[] { Color.White });

            _menuItemBackground = new Texture2D(graphicsDevice, 1, 1);
            _menuItemBackground.SetData(new[] { Color.White });

            _menuItemHover = new Texture2D(graphicsDevice, 1, 1);
            _menuItemHover.SetData(new[] { Color.White });

            _separatorTexture = new Texture2D(graphicsDevice, 1, 1);
            _separatorTexture.SetData(new[] { Color.Gray });
        }

        #endregion

        #region 菜单渲染

        /// <summary>
        /// 渲染军师菜单
        /// </summary>
        public void RenderMenu(AdvisorDoubleClickMenuManager menuManager, string title = "军师菜单")
        {
            if (!menuManager.IsMenuVisible) return;

            var menuBounds = menuManager.MenuBounds;
            var menuItems = menuManager.CurrentMenuItems;
            int selectedIndex = menuManager.SelectedItemIndex;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            try
            {
                // 渲染菜单背景
                RenderMenuBackground(menuBounds, title);

                // 渲染菜单项
                RenderMenuItems(menuBounds, menuItems, selectedIndex);

                // 渲染菜单边框
                RenderMenuBorder(menuBounds);
            }
            finally
            {
                _spriteBatch.End();
            }
        }

        /// <summary>
        /// 渲染菜单背景
        /// </summary>
        private void RenderMenuBackground(Rectangle bounds, string title)
        {
            // 主背景
            _spriteBatch.Draw(_menuBackground, bounds, _menuBackgroundColor);

            // 标题背景
            var titleBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, TITLE_HEIGHT);
            _spriteBatch.Draw(_menuBackground, titleBounds, _titleColor * 0.3f);

            // 标题文字
            if (_titleFont != null && !string.IsNullOrEmpty(title))
            {
                var titleSize = _titleFont.MeasureString(title);
                var titlePosition = new Vector2(
                    bounds.X + (bounds.Width - titleSize.X) / 2,
                    bounds.Y + (TITLE_HEIGHT - titleSize.Y) / 2
                );
                _spriteBatch.DrawString(_titleFont, title, titlePosition, _titleColor);
            }
        }

        /// <summary>
        /// 渲染菜单项
        /// </summary>
        private void RenderMenuItems(Rectangle menuBounds, IReadOnlyList<AdvisorMenuItem> items, int selectedIndex)
        {
            int currentY = menuBounds.Y + TITLE_HEIGHT;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var itemBounds = new Rectangle(
                    menuBounds.X + MENU_PADDING,
                    currentY,
                    menuBounds.Width - MENU_PADDING * 2,
                    ITEM_HEIGHT
                );

                if (item.IsSeparator)
                {
                    RenderSeparator(itemBounds);
                }
                else
                {
                    bool isSelected = i == selectedIndex;
                    bool isEnabled = item.IsEnabled;
                    RenderMenuItem(itemBounds, item, isSelected, isEnabled);
                }

                currentY += ITEM_HEIGHT;
            }
        }

        /// <summary>
        /// 渲染单个菜单项
        /// </summary>
        private void RenderMenuItem(Rectangle bounds, AdvisorMenuItem item, bool isSelected, bool isEnabled)
        {
            // 背景
            if (isSelected && isEnabled)
            {
                _spriteBatch.Draw(_menuItemHover, bounds, _menuHoverColor);
            }

            // 图标
            int iconX = bounds.X + ICON_MARGIN;
            int iconY = bounds.Y + (bounds.Height - ICON_SIZE) / 2;
            
            if (!string.IsNullOrEmpty(item.Icon))
            {
                // 这里可以渲染实际的图标纹理
                // 现在先用文字代替
                if (_menuFont != null)
                {
                    var iconPosition = new Vector2(iconX, iconY);
                    var iconColor = isEnabled ? _menuTextColor : _menuDisabledColor;
                    _spriteBatch.DrawString(_menuFont, item.Icon, iconPosition, iconColor);
                }
            }

            // 文字
            if (_menuFont != null && !string.IsNullOrEmpty(item.Text))
            {
                int textX = iconX + ICON_SIZE + ICON_MARGIN;
                int textY = bounds.Y + (bounds.Height - (int)_menuFont.MeasureString(item.Text).Y) / 2;
                
                var textPosition = new Vector2(textX, textY);
                var textColor = isEnabled ? _menuTextColor : _menuDisabledColor;
                
                _spriteBatch.DrawString(_menuFont, item.Text, textPosition, textColor);
            }

            // 描述文字（如果有空间）
            if (_menuFont != null && !string.IsNullOrEmpty(item.Description) && bounds.Width > 150)
            {
                var descFont = _menuFont; // 可以使用更小的字体
                var descSize = descFont.MeasureString(item.Description);
                
                if (descSize.X < bounds.Width - 100) // 确保有足够空间
                {
                    int descX = bounds.Right - (int)descSize.X - ICON_MARGIN;
                    int descY = bounds.Y + (bounds.Height - (int)descSize.Y) / 2;
                    
                    var descPosition = new Vector2(descX, descY);
                    var descColor = (isEnabled ? _menuTextColor : _menuDisabledColor) * 0.7f;
                    
                    _spriteBatch.DrawString(descFont, item.Description, descPosition, descColor);
                }
            }
        }

        /// <summary>
        /// 渲染分隔线
        /// </summary>
        private void RenderSeparator(Rectangle bounds)
        {
            var separatorBounds = new Rectangle(
                bounds.X + 10,
                bounds.Y + bounds.Height / 2 - 1,
                bounds.Width - 20,
                2
            );
            
            _spriteBatch.Draw(_separatorTexture, separatorBounds, Color.Gray * 0.5f);
        }

        /// <summary>
        /// 渲染菜单边框
        /// </summary>
        private void RenderMenuBorder(Rectangle bounds)
        {
            int borderWidth = 2;
            
            // 上边框
            _spriteBatch.Draw(_menuBackground, 
                new Rectangle(bounds.X, bounds.Y, bounds.Width, borderWidth), 
                _menuBorderColor);
            
            // 下边框
            _spriteBatch.Draw(_menuBackground, 
                new Rectangle(bounds.X, bounds.Bottom - borderWidth, bounds.Width, borderWidth), 
                _menuBorderColor);
            
            // 左边框
            _spriteBatch.Draw(_menuBackground, 
                new Rectangle(bounds.X, bounds.Y, borderWidth, bounds.Height), 
                _menuBorderColor);
            
            // 右边框
            _spriteBatch.Draw(_menuBackground, 
                new Rectangle(bounds.Right - borderWidth, bounds.Y, borderWidth, bounds.Height), 
                _menuBorderColor);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算菜单所需大小
        /// </summary>
        public Size CalculateMenuSize(IReadOnlyList<AdvisorMenuItem> items, string title)
        {
            int maxWidth = 200; // 最小宽度
            int totalHeight = TITLE_HEIGHT + MENU_PADDING * 2;

            if (_menuFont != null)
            {
                // 计算标题宽度
                if (!string.IsNullOrEmpty(title) && _titleFont != null)
                {
                    var titleSize = _titleFont.MeasureString(title);
                    maxWidth = Math.Max(maxWidth, (int)titleSize.X + MENU_PADDING * 2);
                }

                // 计算菜单项宽度
                foreach (var item in items)
                {
                    if (!item.IsSeparator && !string.IsNullOrEmpty(item.Text))
                    {
                        var textSize = _menuFont.MeasureString(item.Text);
                        int itemWidth = ICON_SIZE + ICON_MARGIN * 3 + (int)textSize.X;
                        
                        if (!string.IsNullOrEmpty(item.Description))
                        {
                            var descSize = _menuFont.MeasureString(item.Description);
                            itemWidth += (int)descSize.X + ICON_MARGIN;
                        }
                        
                        maxWidth = Math.Max(maxWidth, itemWidth + MENU_PADDING * 2);
                    }
                }
            }

            totalHeight += items.Count * ITEM_HEIGHT;

            return new Size(maxWidth, totalHeight);
        }

        /// <summary>
        /// 设置菜单样式
        /// </summary>
        public void SetMenuStyle(Color backgroundColor, Color borderColor, Color textColor, Color hoverColor)
        {
            _menuBackgroundColor = backgroundColor;
            _menuBorderColor = borderColor;
            _menuTextColor = textColor;
            _menuHoverColor = hoverColor;
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _spriteBatch?.Dispose();
            _menuBackground?.Dispose();
            _menuItemBackground?.Dispose();
            _menuItemHover?.Dispose();
            _separatorTexture?.Dispose();
        }

        #endregion
    }

    #region 辅助结构

    /// <summary>
    /// 尺寸结构
    /// </summary>
    public struct Size
    {
        public int Width { get; }
        public int Height { get; }

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    #endregion
}

// ===================================================================
// 游戏屏幕集成示例
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 在主游戏屏幕中集成军师双击菜单
    /// </summary>
    public partial class MainGameScreen
    {
        private AdvisorDoubleClickMenuIntegration _advisorMenuIntegration;
        private AdvisorDoubleClickMenuRenderer _advisorMenuRenderer;

        /// <summary>
        /// 初始化军师双击菜单
        /// </summary>
        private void InitializeAdvisorDoubleClickMenu()
        {
            _advisorMenuIntegration = new AdvisorDoubleClickMenuIntegration();
            _advisorMenuRenderer = new AdvisorDoubleClickMenuRenderer(GraphicsDevice, Content);
            
            Console.WriteLine("[主游戏屏幕] 军师双击菜单系统已初始化");
        }

        /// <summary>
        /// 处理鼠标输入 - 在现有的鼠标处理方法中添加
        /// </summary>
        private void HandleAdvisorMenuMouseInput()
        {
            var mouseState = Mouse.GetState();
            var previousMouseState = _previousMouseState; // 假设你有这个字段

            // 检测左键点击
            if (mouseState.LeftButton == ButtonState.Released && 
                previousMouseState.LeftButton == ButtonState.Pressed)
            {
                var clickPosition = new Point(mouseState.X, mouseState.Y);
                var clickedObject = GetGameObjectAtPosition(clickPosition);
                
                _advisorMenuIntegration.HandleGameMouseEvent(
                    MouseEventType.LeftClick, 
                    clickPosition, 
                    clickedObject
                );
            }

            // 处理鼠标移动
            if (mouseState.Position != previousMouseState.Position)
            {
                _advisorMenuIntegration.HandleGameMouseEvent(
                    MouseEventType.MouseMove, 
                    mouseState.Position
                );
            }
        }

        /// <summary>
        /// 渲染军师菜单 - 在Draw方法中调用
        /// </summary>
        private void RenderAdvisorMenu()
        {
            var menuManager = _advisorMenuIntegration.GetMenuManager();
            
            if (menuManager.IsMenuVisible)
            {
                _advisorMenuRenderer.RenderMenu(menuManager, "军师菜单");
            }
        }

        /// <summary>
        /// 获取指定位置的游戏对象
        /// </summary>
        private GameObject GetGameObjectAtPosition(Point position)
        {
            // 实现你的对象检测逻辑
            // 这里需要根据你的游戏架构来实现
            
            // 示例逻辑：
            // 1. 检查是否点击在势力区域
            // 2. 检查是否点击在人物头像
            // 3. 检查是否点击在建筑
            
            return null; // 临时返回null
        }

        /// <summary>
        /// 在Update方法中调用
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // 现有的更新逻辑...
            
            // 处理军师菜单输入
            HandleAdvisorMenuMouseInput();
            
            // 其他更新逻辑...
        }

        /// <summary>
        /// 在Draw方法中调用
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // 现有的绘制逻辑...
            
            // 渲染军师菜单（在最后渲染，确保在最上层）
            RenderAdvisorMenu();
        }
    }
}