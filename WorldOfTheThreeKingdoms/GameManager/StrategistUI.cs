using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using GameObjects;
using GameGlobal;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    public class StrategistUI
    {
        private SpriteFont font;
        private Texture2D pixelTexture;

        // 悬浮窗状态
        private Vector2 panelPosition;
        private bool isDragging = false;
        private Vector2 dragOffset;

        // 布局尺寸
        private const int Width = 300;
        private const int Height = 180;
        private const int HeaderHeight = 30;
        private const int ButtonSize = 80;

        // 文本内容
        private string currentAdvice = "军师正在观察局势...";

        // 鼠标状态
        private MouseState prevMouse;
        private KeyboardState prevKeyboardState;

        // 缓存逻辑坐标，用于调试显示
        private Point logicalMousePos;

        public StrategistUI()
        {
        }

        public void LoadContent(ContentManager content, GraphicsDevice device)
        {
            // 加载字体
            try
            {
                font = content.Load<SpriteFont>("FontS");
            }
            catch
            {
                try
                {
                    font = content.Load<SpriteFont>("Fonts/FontS");
                }
                catch
                {
                }
            }

            // 设置初始位置 - 放在建筑菜单附近
            // 注意：这里使用的是逻辑坐标
            panelPosition = new Vector2(200, 600); 
        }

        private Texture2D GetPixelTexture(GraphicsDevice device)
        {
            if (pixelTexture == null || pixelTexture.IsDisposed || pixelTexture.GraphicsDevice != device)
            {
                try
                {
                    pixelTexture = new Texture2D(device, 1, 1);
                    pixelTexture.SetData(new[] { Color.White });
                }
                catch
                {
                    return null;
                }
            }
            return pixelTexture;
        }

        public void Update(GameTime gameTime)
        {
            // 禁用独立的军师按钮功能，改用建筑菜单
            return;
            
            MouseState mouse = Mouse.GetState();
            
            // --- 核心修复开始 ---
            // 获取转换后的逻辑坐标
            logicalMousePos = GetLogicalMousePosition(mouse.Position);
            // --- 核心修复结束 ---

            Rectangle headerRect = new Rectangle((int)panelPosition.X, (int)panelPosition.Y, Width, HeaderHeight);

            // 调试信息：同时显示物理坐标和转换后的逻辑坐标
            // System.Diagnostics.Debug.WriteLine($"[StrategistUI] 物理鼠标: {mouse.Position}, 逻辑鼠标: {logicalMousePos}");

            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.F1) && !prevKeyboardState.IsKeyDown(Keys.F1))
            {
                OnAdvisorButtonClicked();
            }

            // 使用转换后的 logicalMousePos 进行点击检测
            HandleAdvisorButtonClick(mouse, logicalMousePos);

            // 拖拽逻辑 (同样使用逻辑坐标)
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (prevMouse.LeftButton == ButtonState.Released && headerRect.Contains(logicalMousePos))
                {
                    isDragging = true;
                    dragOffset = new Vector2(logicalMousePos.X - panelPosition.X, logicalMousePos.Y - panelPosition.Y);
                }

                if (isDragging)
                {
                    panelPosition = new Vector2(logicalMousePos.X - dragOffset.X, logicalMousePos.Y - dragOffset.Y);
                }
            }
            else
            {
                isDragging = false;
            }

            prevMouse = mouse;
            prevKeyboardState = keyboardState;
        }

        // --- 新增辅助方法：坐标转换 ---
        private Point GetLogicalMousePosition(Point screenPos)
        {
            // 安全检查：如果 Session 或 GraphicsDevice 不可用，直接返回原始坐标
            if (Session.MainGame == null || Session.MainGame.GraphicsDevice == null)
            {
                return screenPos;
            }

            GraphicsDevice device = Session.MainGame.GraphicsDevice;
            PresentationParameters pp = device.PresentationParameters;

            // 获取游戏设计的逻辑分辨率 (BackBuffer)
            int logicalWidth = pp.BackBufferWidth;
            int logicalHeight = pp.BackBufferHeight;

            // 获取当前窗口/屏幕的实际物理分辨率
            int screenWidth = device.Viewport.Width;
            int screenHeight = device.Viewport.Height;

            // 防止除以零
            if (screenWidth == 0 || screenHeight == 0) return screenPos;

            // 计算缩放比例 (物理 / 逻辑)
            float scaleX = (float)screenWidth / logicalWidth;
            float scaleY = (float)screenHeight / logicalHeight;

            // 将屏幕坐标 映射回 逻辑坐标
            // 逻辑坐标 = 屏幕坐标 / 缩放比
            return new Point((int)(screenPos.X / scaleX), (int)(screenPos.Y / scaleY));
        }

        private void HandleAdvisorButtonClick(MouseState mouse, Point currentMousePos)
        {
            try
            {
                // 按钮位置保持不变 (逻辑坐标)
                int buttonX = 400;
                int buttonY = 900;
                
                Rectangle buttonRect = new Rectangle(buttonX, buttonY, ButtonSize, ButtonSize);
                
                // 使用传入的 currentMousePos (已经是转换过的逻辑坐标) 进行判断
                bool mouseInButton = buttonRect.Contains(currentMousePos);
                
                // 仅在点击时输出调试，避免刷屏
                if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
                {
                    System.Diagnostics.Debug.WriteLine($"[StrategistUI] 点击 - 按钮: {buttonRect}, 鼠标(逻辑): {currentMousePos}, 命中: {mouseInButton}");
                    
                    if (mouseInButton)
                    {
                        System.Diagnostics.Debug.WriteLine("[StrategistUI] 军师按钮被点击！");
                        OnAdvisorButtonClicked();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StrategistUI] 按钮点击处理异常: {ex.Message}");
            }
        }

        private void OnAdvisorButtonClicked()
        {
            try
            {
                // 确保 Session 和 Scenario 有效
                if (Session.Current == null || Session.Current.Scenario == null) return;

                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction == null) return;

                if (!faction.AppointAdvisorAvail())
                {
                    // 可以添加简单的屏幕提示
                    return;
                }

                var candidates = faction.AdvisorCandicate;
                if (candidates == null || candidates.Count == 0) return;

                var mainGameScreen = Session.MainGame?.mainGameScreen;
                if (mainGameScreen != null)
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StrategistUI] 军师按钮点击处理异常: {ex.Message}");
            }
        }

        public void UpdateAdvice(string text)
        {
            currentAdvice = text;
        }

        public void UpdateAdvisorButton(GameScenario scenario)
        {
            var currentFaction = Session.Current?.Scenario?.CurrentPlayer;
            if (currentFaction == null) return;

            if (currentFaction.Advisor != null)
            {
                currentAdvice = $"当前军师: {currentFaction.Advisor.Name}";
            }
            else
            {
                currentAdvice = "暂无军师，点击按钮任命";
            }
        }

        public void UpdateAdvisorButtonSafe(GameScenario scenario, GraphicsDevice device)
        {
            UpdateAdvisorButton(scenario);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // 禁用独立的军师按钮绘制，改用建筑菜单
            return;
            
            GraphicsDevice device = spriteBatch.GraphicsDevice;
            Texture2D tex = GetPixelTexture(device);
            if (tex == null || font == null) return;

            try
            {
                // 注意：这里不需要 spriteBatch.Begin() 的 transformMatrix，
                // 因为我们是在 UI 层绘制，通常 UI 层的 Draw 调用在 MainGameScreen 中可能已经开启了 batch，
                // 或者我们假设是在逻辑坐标系上绘制。
                // 如果外部已经 Begin 了，这里再次 Begin 会报错。
                // 既然你在原代码里写了 Begin，假设这是一个独立的 Draw 调用。
                
                // 为了适应逻辑坐标绘制，我们需要应用缩放矩阵
                // 获取缩放比
                PresentationParameters pp = device.PresentationParameters;
                float scaleX = (float)device.Viewport.Width / pp.BackBufferWidth;
                float scaleY = (float)device.Viewport.Height / pp.BackBufferHeight;
                Matrix scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1.0f);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);

                int x = (int)panelPosition.X;
                int y = (int)panelPosition.Y;

                // 绘制军师按钮
                DrawAdvisorButton(spriteBatch, tex, 400, 900);

                // 绘制背景
                if (Width > 0 && Height > 0)
                {
                    Rectangle bodyRect = new Rectangle(x, y, Width, Height);
                    spriteBatch.Draw(tex, bodyRect, new Color(30, 30, 40, 230));
                    DrawBorder(spriteBatch, tex, bodyRect, 2, Color.Gray);
                }

                // 绘制标题栏
                if (Width > 0 && HeaderHeight > 0)
                {
                    Color headerColor = isDragging ? new Color(100, 80, 0) : new Color(60, 40, 0);
                    Rectangle headerRect = new Rectangle(x, y, Width, HeaderHeight);
                    spriteBatch.Draw(tex, headerRect, headerColor);
                    DrawBorder(spriteBatch, tex, headerRect, 1, Color.Gold);
                }

                // 标题文字
                if (font != null)
                {
                    string title = "军师 (拖动我)";
                    Vector2 titleSize = font.MeasureString(title);
                    spriteBatch.DrawString(font, title, new Vector2(x + (Width - titleSize.X) / 2, y + 5), Color.Gold);
                }

                // 建议内容
                if (Width > 20 && Height > HeaderHeight + 20)
                {
                    Rectangle textRect = new Rectangle(x + 10, y + HeaderHeight + 10, Width - 20, Height - HeaderHeight - 20);
                    string wrappedText = WrapText(font, currentAdvice, textRect.Width);
                    spriteBatch.DrawString(font, wrappedText, new Vector2(textRect.X, textRect.Y), Color.White);
                }
                
                // 绘制调试点（可选，用于确认鼠标逻辑位置）
                // Rectangle debugMouse = new Rectangle(logicalMousePos.X - 2, logicalMousePos.Y - 2, 4, 4);
                // spriteBatch.Draw(tex, debugMouse, Color.Red);

                spriteBatch.End();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StrategistUI] 绘制异常: {ex.Message}");
            }
        }

        private void DrawAdvisorButton(SpriteBatch spriteBatch, Texture2D fallbackTexture, int x, int y)
        {
            if (ButtonSize <= 0) return;
            
            Rectangle buttonRect = new Rectangle(x, y, ButtonSize, ButtonSize);

            // 移除 Viewport 检查，因为我们在逻辑坐标下绘制，Viewport 检查可能会误判
            
            if (fallbackTexture != null && !fallbackTexture.IsDisposed)
            {
                try
                {
                    // 悬停效果
                    bool isHover = buttonRect.Contains(logicalMousePos);
                    Color btnColor = isHover ? new Color(80, 60, 0, 200) : new Color(60, 40, 0, 200);

                    spriteBatch.Draw(fallbackTexture, buttonRect, btnColor);
                    DrawBorder(spriteBatch, fallbackTexture, buttonRect, 2, Color.Brown);
                    
                    if (font != null)
                    {
                        string text = "军";
                        Vector2 textSize = font.MeasureString(text);
                        Vector2 textPos = new Vector2(x + (ButtonSize - textSize.X) / 2, y + (ButtonSize - textSize.Y) / 2);
                        spriteBatch.DrawString(font, text, textPos, Color.Gold);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[StrategistUI] 绘制按钮异常: {ex.Message}");
                }
            }
        }

        private void DrawBorder(SpriteBatch sb, Texture2D tex, Rectangle r, int t, Color c)
        {
            if (sb == null || tex == null || tex.IsDisposed || t <= 0 || 
                r.Width <= 0 || r.Height <= 0) return;

            try
            {
                // 确保边框厚度不超过矩形尺寸
                int borderThickness = Math.Min(t, Math.Min(r.Width / 2, r.Height / 2));
                if (borderThickness <= 0) return;
                
                // Top - 确保尺寸有效
                if (r.Width > 0 && borderThickness > 0)
                    sb.Draw(tex, new Rectangle(r.X, r.Y, r.Width, borderThickness), c);
                
                // Bottom - 确保不会产生负坐标或零尺寸
                if (r.Width > 0 && borderThickness > 0 && r.Bottom - borderThickness >= r.Y)
                    sb.Draw(tex, new Rectangle(r.X, r.Bottom - borderThickness, r.Width, borderThickness), c);
                
                // Left - 确保尺寸有效
                if (r.Height > 0 && borderThickness > 0)
                    sb.Draw(tex, new Rectangle(r.X, r.Y, borderThickness, r.Height), c);
                
                // Right - 确保不会产生负坐标或零尺寸
                if (r.Height > 0 && borderThickness > 0 && r.Right - borderThickness >= r.X)
                    sb.Draw(tex, new Rectangle(r.Right - borderThickness, r.Y, borderThickness, r.Height), c);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StrategistUI] DrawBorder异常: {ex.Message}");
            }
        }

        private string WrapText(SpriteFont font, string text, float maxLineWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";

            string result = "";
            float currentLineLen = 0f;

            foreach (char c in text)
            {
                float w = font.MeasureString(c.ToString()).X;
                if (currentLineLen + w > maxLineWidth || c == '\n')
                {
                    result += "\n";
                    currentLineLen = 0f;
                    if (c == '\n') continue;
                }
                result += c;
                currentLineLen += w;
            }
            return result;
        }
    }
}