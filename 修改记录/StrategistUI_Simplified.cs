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
    public class StrategistUI_Simplified
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

        public StrategistUI_Simplified()
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

            // 设置初始位置
            panelPosition = new Vector2(device.Viewport.Width - Width - 20, 60);
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
            MouseState mouse = Mouse.GetState();
            Point mousePos = mouse.Position;
            Rectangle headerRect = new Rectangle((int)panelPosition.X, (int)panelPosition.Y, Width, HeaderHeight);

            // 军师按钮点击检测
            HandleAdvisorButtonClick(mouse, mousePos);

            // 拖拽逻辑
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (prevMouse.LeftButton == ButtonState.Released && headerRect.Contains(mousePos))
                {
                    isDragging = true;
                    dragOffset = new Vector2(mousePos.X - panelPosition.X, mousePos.Y - panelPosition.Y);
                }

                if (isDragging)
                {
                    panelPosition = new Vector2(mousePos.X - dragOffset.X, mousePos.Y - dragOffset.Y);
                }
            }
            else
            {
                isDragging = false;
            }

            prevMouse = mouse;
        }

        private void HandleAdvisorButtonClick(MouseState mouse, Point mousePos)
        {
            try
            {
                int x = (int)panelPosition.X;
                int y = (int)panelPosition.Y;
                
                int buttonX = x - ButtonSize - 10;
                int buttonY = y;
                
                Rectangle buttonRect = new Rectangle(buttonX, buttonY, ButtonSize, ButtonSize);
                
                System.Diagnostics.Debug.WriteLine($"[StrategistUI] 按钮区域: {buttonRect}, 鼠标: {mousePos}");
                
                if (mouse.LeftButton == ButtonState.Pressed && 
                    prevMouse.LeftButton == ButtonState.Released && 
                    buttonRect.Contains(mousePos))
                {
                    System.Diagnostics.Debug.WriteLine("[StrategistUI] 军师按钮被点击！");
                    OnAdvisorButtonClicked();
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
                System.Diagnostics.Debug.WriteLine("[StrategistUI] 处理军师按钮点击");
                
                var faction = Session.Current?.Scenario?.CurrentPlayer;
                if (faction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StrategistUI] 当前没有玩家势力");
                    return;
                }

                if (!faction.AppointAdvisorAvail())
                {
                    System.Diagnostics.Debug.WriteLine("[StrategistUI] 当前不能任命军师");
                    return;
                }

                var candidates = faction.AdvisorCandicate;
                if (candidates == null || candidates.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[StrategistUI] 没有可用的军师候选人");
                    return;
                }

                var mainGameScreen = Session.MainGame?.mainGameScreen;
                if (mainGameScreen != null)
                {
                    string title = faction.Advisor != null ? "重新任命军师" : "任命军师";
                    
                    System.Diagnostics.Debug.WriteLine($"[StrategistUI] 打开军师任命界面: {title}, 候选人数: {candidates.Count}");
                    
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

        public void Draw(SpriteBatch spriteBatch)
        {
            GraphicsDevice device = spriteBatch.GraphicsDevice;
            Texture2D tex = GetPixelTexture(device);
            if (tex == null || font == null) return;

            try
            {
                spriteBatch.Begin();

                int x = (int)panelPosition.X;
                int y = (int)panelPosition.Y;

                // 绘制军师按钮
                DrawAdvisorButton(spriteBatch, tex, x - ButtonSize - 10, y);

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

            var viewport = spriteBatch.GraphicsDevice.Viewport;
            if (x + ButtonSize < 0 || y + ButtonSize < 0 || 
                x > viewport.Width || y > viewport.Height)
            {
                return;
            }

            if (fallbackTexture != null && !fallbackTexture.IsDisposed)
            {
                try
                {
                    spriteBatch.Draw(fallbackTexture, buttonRect, new Color(60, 40, 0, 200));
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
                if (r.Width > 0 && t > 0)
                    sb.Draw(tex, new Rectangle(r.X, r.Y, r.Width, t), c);
                
                if (r.Width > 0 && t > 0 && r.Bottom - t >= r.Y)
                    sb.Draw(tex, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
                
                if (r.Height > 0 && t > 0)
                    sb.Draw(tex, new Rectangle(r.X, r.Y, t, r.Height), c);
                
                if (r.Height > 0 && t > 0 && r.Right - t >= r.X)
                    sb.Draw(tex, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
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