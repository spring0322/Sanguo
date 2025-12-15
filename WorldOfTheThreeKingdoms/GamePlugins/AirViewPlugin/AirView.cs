using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameFreeText;
using GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameManager;

namespace AirViewPlugin
{
    public class AirView : Tool
    {
#pragma warning disable CS0649 // Field 'AirView.ArchitectureTexture' is never assigned to, and will always have its default value null
        internal PlatformTexture ArchitectureTexture;
#pragma warning restore CS0649 // Field 'AirView.ArchitectureTexture' is never assigned to, and will always have its default value null
        internal PlatformTexture ArchitectureUnitTexture;
        internal FreeText Conment;
        internal PlatformTexture ConmentBackgroundTexture;
        internal int DefaultTileLength;
        private Rectangle framePosition;
        internal PlatformTexture FrameTexture;
        private bool isMapShowing;
        private bool isPreparedToJump = false;
        private Point MapDisplayOffset;
        internal int MapMaxHeight;
        internal int MapMaxWidth;
        internal ShowPosition MapShowPosition = ShowPosition.BottomRight;
        private Point mapSize;
        internal PlatformTexture MapTexture;
        
        internal int TileLength;
        internal int TileLengthMax;
        internal PlatformTexture ToolDisplayTexture;
        internal Rectangle ToolPosition;
        internal PlatformTexture ToolSelectedTexture;
        internal PlatformTexture ToolTexture;

        internal PlatformTexture TroopToolDisplayTexture;
        internal Rectangle TroopToolPosition;
        internal PlatformTexture TroopToolSelectedTexture;
        internal PlatformTexture TroopToolTexture;

        internal float Transparent = 1f;
#pragma warning disable CS0649 // Field 'AirView.TroopTexture' is never assigned to, and will always have its default value null
        internal PlatformTexture TroopTexture;
#pragma warning restore CS0649 // Field 'AirView.TroopTexture' is never assigned to, and will always have its default value null
        internal PlatformTexture TroopFactionColorTexture;
        int timeSinceLastFrame = 0;
        int millisecondsPerFrame = 240;
        bool drawTroopFlag = true;
        bool showTroop = true;

        // 小地图缩放功能相关字段
        // 基础尺寸 (例如 200像素)
        private const int BASE_SIZE = 250;
        // 当前缩放倍率 (1.0 = 正常, 2.0 = 两倍大)
        private float _currentScale = 1.0f;
        // 缩放范围限制
        private const float MIN_SCALE = 0.8f;
        private const float MAX_SCALE = 2.5f;
        // 记录鼠标上一帧的状态，用于判断滚轮滚动
        private int _previousScrollValue;

        internal void AddDisableRects()
        {
            Session.MainGame.mainGameScreen.AddDisableRectangle(Session.MainGame.mainGameScreen.LaterMouseEventDisableRects, this.MapPosition);
            Session.MainGame.mainGameScreen.AddDisableRectangle(Session.MainGame.mainGameScreen.SelectingDisableRects, this.MapPosition);
        }

        public override void Draw(GameTime gameTime)
        {
            // 每一帧都重新计算位置，虽然有一点点点点性能损耗(几乎不计)，
            // 但能保证小地图永远贴在边框上，不会跑偏。
            UpdateDisplayRect();

            Rectangle? sourceRectangle = null;
            CacheManager.Draw(this.ToolDisplayTexture, this.ToolDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.099f);
            if (this.IsMapShowing)
            {
                CacheManager.Draw(this.TroopToolDisplayTexture, this.TroopToolDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.099f);

                // --- 修复问题1：防丢失保险机制 ---
                // 如果发现图片没了，或者被释放了，立刻重新生成
                if (this.MapTexture == null || (this.MapTexture.Name != null && !CacheManager.TextureTempDics.ContainsKey(this.MapTexture.Name)))
                {
                    System.Diagnostics.Debug.WriteLine("[AirView] 检测到小地图纹理丢失，正在重新生成...");
                    
                    // 通过插件重新生成战略小地图
                    if (Session.Current?.Scenario != null && Session.MainGame?.mainGameScreen?.Plugins?.AirViewPlugin != null)
                    {
                        var airViewPlugin = Session.MainGame.mainGameScreen.Plugins.AirViewPlugin as global::AirViewPlugin.AirViewPlugin;
                        airViewPlugin?.CreateStrategicMinimap(Session.Current.Scenario);
                        System.Diagnostics.Debug.WriteLine("[AirView] 小地图重新生成完成");
                    }
                    
                    // 如果生成失败，直接返回，避免报错
                    if (this.MapTexture == null) 
                    {
                        System.Diagnostics.Debug.WriteLine("[AirView] 小地图生成失败，跳过绘制");
                        return;
                    }
                }

                if (this.MapTexture != null)
                {
                    sourceRectangle = null;
                    // 确保 Alpha 值不为 0 (Color.White 表示完全不透明)
                    CacheManager.Draw(this.MapTexture, this.MapPosition, sourceRectangle, new Color(1f, 1f, 1f, this.Transparent), 0f, Vector2.Zero, SpriteEffects.None, 0.1f);
                }
                /*
                if (this.TroopTexture != null)
                {
                    sourceRectangle = null;
                    CacheManager.Draw(this.TroopTexture, this.MapPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.09999f);
                }
                */
                if (this.showTroop)
                {
                    timeSinceLastFrame += gameTime.ElapsedGameTime.Milliseconds;
                    if (timeSinceLastFrame > millisecondsPerFrame)
                    {
                        //timeSinceLastFrame -= millisecondsPerFrame;
                        timeSinceLastFrame = 0;
                        this.drawTroopFlag = !this.drawTroopFlag;
                    }
                    if (this.drawTroopFlag)
                    {
                        this.drawTroop( gameTime);
                    }
                }
                foreach (Architecture architecture in Session.Current.Scenario.Architectures)
                {
                    Color white = Color.White;
                    if (architecture.BelongedFaction != null)
                    {
                        white = architecture.BelongedFaction.FactionColor;
                    }
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        // 【核心修复】计算坐标并限制范围 (Clamping)
                        // 1. 原始计算：将大地图坐标转为屏幕坐标
                        float xRatio = (float)point.X / Session.Current.Scenario.ScenarioMap.MapDimensions.X;
                        float yRatio = (float)point.Y / Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
                        
                        // 加上 MapDisplayOffset 是因为小地图不一定在 (0,0)
                        float drawX = this.MapDisplayOffset.X + (xRatio * this.mapSize.X);
                        float drawY = this.MapDisplayOffset.Y + (yRatio * this.mapSize.Y);
                        
                        // 2. 限制坐标范围 (Clamping)
                        int dotSize = this.TileLength + 2; // 城市点的大小
                        drawX = MathHelper.Clamp(drawX, this.MapDisplayOffset.X, this.MapDisplayOffset.X + this.mapSize.X - dotSize);
                        drawY = MathHelper.Clamp(drawY, this.MapDisplayOffset.Y, this.MapDisplayOffset.Y + this.mapSize.Y - dotSize);
                        
                        sourceRectangle = null;
                        CacheManager.Draw(this.ArchitectureUnitTexture, new Rectangle((int)drawX - 1, (int)drawY - 1, this.TileLength + 2, this.TileLength + 2), sourceRectangle, white, 0f, Vector2.Zero, SpriteEffects.None, 0.09999f);
                    }
                }
                sourceRectangle = null;
                //CacheManager.Draw(this.FrameTexture, this.FrameDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.0998f);
                CacheManager.Draw(this.FrameTexture, this.frameTopPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.0998f);
                CacheManager.Draw(this.FrameTexture, this.frameLeftPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.0998f);
                CacheManager.Draw(this.FrameTexture, this.frameBottomPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.0998f);
                CacheManager.Draw(this.FrameTexture, this.frameRightPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.0998f);
                if (this.Conment.Text != "")
                {
                    CacheManager.Draw(this.ConmentBackgroundTexture, this.Conment.AlignedPosition, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.09999f);
                    this.Conment.Draw(0.0999f);
                }
            }
        }

        private void renderTroop(GameTime gameTime, Troop troop)
        {
            Color color = Color.White;
            if (troop.Destroyed) return;
            if (troop.Status == TroopStatus.埋伏) return;
            if (troop.BelongedFaction != null)
            {
                color = troop.BelongedFaction.FactionColor;
            }

            // 【核心修复】使用浮点数比例计算坐标，避免因 TileLength 精度丢失导致坐标偏移
            // 参考 Architecture 绘制逻辑，确保与地图底图对齐
            float xRatio = (float)troop.Position.X / Session.Current.Scenario.ScenarioMap.MapDimensions.X;
            float yRatio = (float)troop.Position.Y / Session.Current.Scenario.ScenarioMap.MapDimensions.Y;

            float drawX = this.MapDisplayOffset.X + (xRatio * this.mapSize.X);
            float drawY = this.MapDisplayOffset.Y + (yRatio * this.mapSize.Y);

            // 确保部队图标大小至少为 3 像素，防止在大地图缩放时不可见
            int dotSize = Math.Max(this.TileLength * 4, 3);

            // 计算目标绘制区域
            Rectangle destRect = new Rectangle(
                (int)drawX - 1,
                (int)drawY - 1, 
                dotSize, 
                dotSize);

            // 【核心修复】边界裁切 (Clipping)
            // 防止部队图标渲染出小地图边界 (因为图标通常比一个格子大)
            Rectangle clipRect = Rectangle.Intersect(destRect, this.MapPosition);
            if (clipRect.Width > 0 && clipRect.Height > 0)
            {
                CacheManager.Draw(TroopFactionColorTexture, clipRect, null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.09998f);
            }
        }

        private void drawTroop(GameTime gameTime)
        {
            if (Session.GlobalVariables.SkyEye)
            {
                // 【核心修复】SkyEye模式下直接遍历所有部队
                foreach (Troop t in Session.Current.Scenario.Troops.GetList())
                {
                    if (t.Destroyed) continue;
                    if (t.Simulating) continue; // 过滤模拟运算中的临时部队
                    if (Session.Current.Scenario.PositionOutOfRange(t.Position)) continue; // 过滤坐标非法的部队

                    // 必须检查 DrawAnimation，防止显示本应隐藏的部队 (如入城、剧情隐藏等)
                    if (t.DrawAnimation) 
                    {
                        renderTroop(gameTime, t);
                    }
                }
            }
            else if (Session.Current.Scenario.CurrentPlayer != null)
            {
                foreach (Troop t in Session.Current.Scenario.CurrentPlayer.GetVisibleTroops())
                {
                    if (t.Destroyed) continue;
                    if (t.Simulating) continue;
                    if (Session.Current.Scenario.PositionOutOfRange(t.Position)) continue;

                    // 【核心修复】再次检查该部队当前位置是否真的可见
                    // 1. 必须达到“高”或以上的情报等级（active vision）
                    // 2. 必须 DrawAnimation 为 true (参考 TroopLayer 逻辑)
                    if (Session.Current.Scenario.CurrentPlayer.GetInformationLevel(t.Position) >= InformationLevel.高 && t.DrawAnimation)
                    {
                        renderTroop( gameTime, t);
                    }
                }
            }
        }

        private Point GetTranslatedPosition(Point position)
        {
            int num = position.X - this.MapDisplayOffset.X;
            int num2 = position.Y - this.MapDisplayOffset.Y;
            return new Point((Session.Current.Scenario.ScenarioMap.MapDimensions.X * num) / this.mapSize.X, (Session.Current.Scenario.ScenarioMap.MapDimensions.Y * num2) / this.mapSize.Y);
        }

        internal void Initialize(Screen screen)
        {
            screen.OnMouseLeftDown += new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
            screen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
            
            // 延迟设置小地图显示状态，避免空引用异常
            System.Diagnostics.Debug.WriteLine("[AirView] 延迟设置小地图显示状态");
        }
        
        // 新增方法：在游戏完全初始化后设置小地图显示
        internal void EnableMapDisplay()
        {
            try
            {
                if (Session.MainGame != null && Session.MainGame.mainGameScreen != null)
                {
                    System.Diagnostics.Debug.WriteLine("[AirView] 设置小地图为显示状态");
                    this.IsMapShowing = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AirView] Session或mainGameScreen未初始化，跳过设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AirView] 设置小地图显示时发生异常: {ex.Message}");
            }
        }

        private void JumpTo(Point position)
        {
            if (this.isPreparedToJump)
            {
                Session.MainGame.mainGameScreen.JumpTo(this.GetTranslatedPosition(position));
            }
        }

        internal void RemoveDisableRects()
        {
            Session.MainGame.mainGameScreen.RemoveDisableRectangle(Session.MainGame.mainGameScreen.LaterMouseEventDisableRects, this.MapPosition);
            Session.MainGame.mainGameScreen.RemoveDisableRectangle(Session.MainGame.mainGameScreen.SelectingDisableRects, this.MapPosition);
        }

        internal void ResetFramePosition(Point viewportSize, int leftEdge, int topEdge, Point totalMapSize)
        {
            if ((this.framePosition.Width + this.framePosition.Height) == 0)
            {
                this.ResetFrameSize(viewportSize, totalMapSize);
            }
            int x = ((((viewportSize.X / 2) - leftEdge) * this.mapSize.X) / totalMapSize.X) - (this.framePosition.Width / 2);
            int y = ((((viewportSize.Y / 2) - topEdge) * this.mapSize.Y) / totalMapSize.Y) - (this.framePosition.Height / 2);
            if (x < 0)
            {
                x = 0;
            }
            if ((x + this.framePosition.Width) > this.mapSize.X)
            {
                x = this.mapSize.X - this.framePosition.Width;
            }
            if (y < 0)
            {
                y = 0;
            }
            if ((y + this.framePosition.Height) > this.mapSize.Y)
            {
                y = this.mapSize.Y - this.framePosition.Height;
            }
            this.framePosition = new Rectangle(x, y, this.framePosition.Width, this.framePosition.Height);
        }

        internal void ResetFrameSize(Point viewportSize, Point totalMapSize)
        {
            int width = (this.mapSize.X * viewportSize.X) / totalMapSize.X;
            int height = (this.mapSize.Y * viewportSize.Y) / totalMapSize.Y;
            this.framePosition = new Rectangle(0, 0, width, height);
        }

        private void ResetMapSize()
        {
            int x = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
            int y = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
            if (x > y)
            {
                if (x > this.MapMaxWidth)
                {
                    y = (y * this.MapMaxWidth) / x;
                    x = this.MapMaxWidth;
                }
                if (y > this.MapMaxHeight)
                {
                    x = (x * this.MapMaxHeight) / y;
                    y = this.MapMaxHeight;
                }
            }
            else
            {
                if (y > this.MapMaxHeight)
                {
                    x = (x * this.MapMaxHeight) / y;
                    y = this.MapMaxHeight;
                }
                if (x > this.MapMaxWidth)
                {
                    y = (y * this.MapMaxWidth) / x;
                    x = this.MapMaxWidth;
                }
            }
            if ((x < this.MapMaxWidth) && (y < this.MapMaxHeight))
            {
                int tileLengthMax = this.MapMaxWidth / x;
                int num4 = this.MapMaxHeight / y;
                if (tileLengthMax < num4)
                {
                    tileLengthMax = num4;
                }
                if (tileLengthMax > this.TileLengthMax)
                {
                    tileLengthMax = this.TileLengthMax;
                }
                this.TileLength = tileLengthMax;
            }
            this.mapSize = new Point(x * this.TileLength, y * this.TileLength);
        }

        private void screen_OnMouseLeftDown(Point position)
        {
            if (base.IsDrawing)
            {
                if (StaticMethods.PointInRectangle(position, this.ToolDisplayPosition))
                {
                    if (InputManager.IsDownPre == false)
                    {
                        this.IsMapShowing = !this.IsMapShowing;
                    }
                    if (base.Enabled)
                    {
                        if (this.IsMapShowing)
                        {
                                this.ToolDisplayTexture = this.ToolSelectedTexture;
                        }
                        else
                        {
                            this.ToolDisplayTexture = this.ToolTexture;
                        }
                    }
                }
                else if (this.IsMapShowing && StaticMethods.PointInRectangle(position, this.TroopToolDisplayPosition))
                {
                    if (InputManager.IsDownPre == false)
                    {
                        this.showTroop = !this.showTroop;
                    }
                    if (this.showTroop)
                    {
                        this.TroopToolDisplayTexture = this.TroopToolSelectedTexture;
                    }
                    else
                    {
                        this.TroopToolDisplayTexture = this.TroopToolTexture;
                    }
                }
                else if (this.IsMapShowing && StaticMethods.PointInRectangle(position, this.MapPosition))
                {
                    this.JumpTo(position);
                }
            }
        }

        private void screen_OnMouseMove(Point position, bool leftDown)
        {
            if (base.IsDrawing && !Session.MainGame.mainGameScreen.DrawingSelector)
            {
                if (!this.IsMapShowing)
                {
                    if (base.Enabled)
                    {

                            this.ToolDisplayTexture = this.ToolTexture;

                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.MapPosition))
                {
                    this.isPreparedToJump = true;
                    Session.MainGame.mainGameScreen.ResetMouse();
                    if (leftDown)
                    {
                        this.JumpTo(position);
                    }
                    else if (this.isPreparedToJump)
                    {
                        Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.GetTranslatedPosition(position));
                        if (architectureByPosition != null)
                        {
                            this.Conment.DisplayOffset = position;
                            this.Conment.Text = architectureByPosition.Name + " " + architectureByPosition.FactionString;
                        }
                        else
                        {
                            this.Conment.Text = "";
                        }
                    }
                }
                else
                {
                    this.isPreparedToJump = false;
                }
            }
        }

        private void screen_OnMouseRightUp(Point position)
        {
            if ((base.IsDrawing && this.IsMapShowing) && StaticMethods.PointInRectangle(position, this.MapPosition))
            {
                this.IsMapShowing = false;
                this.ToolDisplayTexture = this.ToolTexture;
            }
        }

        internal void SetDisplayOffset(Screen screen, ShowPosition showPosition)
        {
            this.TileLength = this.DefaultTileLength;
            this.ResetMapSize();
            Rectangle rectDes = new Rectangle(0, 0, screen.viewportSize.X, screen.viewportSize.Y);
            Rectangle rect = new Rectangle(0, 0, this.mapSize.X, this.mapSize.Y);
            switch (showPosition)
            {
                case ShowPosition.Center:
                    rect = StaticMethods.GetCenterRectangle(rectDes, rect);
                    break;

                case ShowPosition.Top:
                    rect = StaticMethods.GetTopRectangle(rectDes, rect);
                    break;

                case ShowPosition.Left:
                    rect = StaticMethods.GetLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.Right:
                    rect = StaticMethods.GetRightRectangle(rectDes, rect);
                    break;

                case ShowPosition.Bottom:
                    rect = StaticMethods.GetBottomRectangle(rectDes, rect);
                    break;

                case ShowPosition.TopLeft:
                    rect = StaticMethods.GetTopLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.TopRight:
                    rect = StaticMethods.GetTopRightRectangle(rectDes, rect);
                    break;

                case ShowPosition.BottomLeft:
                    rect = StaticMethods.GetBottomLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.BottomRight:
                    rect = StaticMethods.GetBottomRightRectangle(rectDes, rect);
                    break;
            }
            this.MapDisplayOffset = new Point(rect.X, rect.Y);
        }

        public override void Update()
        {
            // 获取鼠标状态
            MouseState mouse = Mouse.GetState();

            // 1. 判断鼠标是否悬停在小地图上 (可选：只有鼠标指着小地图时才缩放)
            bool isHovering = this.MapPosition.Contains(mouse.Position);
            if (isHovering)
            {
                // 2. 检测滚轮变化
                int delta = mouse.ScrollWheelValue - _previousScrollValue;
                if (delta != 0)
                {
                    if (delta > 0)
                        _currentScale += 0.1f; // 向上滚放大
                    else
                        _currentScale -= 0.1f; // 向下滚缩小

                    // 限制缩放范围
                    _currentScale = MathHelper.Clamp(_currentScale, MIN_SCALE, MAX_SCALE);

                    // 3. 应用新大小
                    UpdateDisplayRect();
                }
            }

            // 更新滚轮记录
            _previousScrollValue = mouse.ScrollWheelValue;
        }

        // 专门用来计算显示区域的方法
        private void UpdateDisplayRect()
        {
            // 1. 获取期望的基础大小
            float targetSize = BASE_SIZE * _currentScale;

            // 2. 获取大地图的纵横比
            float mapRatio = (float)Session.Current.Scenario.ScenarioMap.MapDimensions.X / Session.Current.Scenario.ScenarioMap.MapDimensions.Y;

            // 3. 计算修正后的宽高
            int finalW, finalH;
            if (mapRatio > 1.0f) // 大地图更宽
            {
                finalW = (int)targetSize;
                finalH = (int)(targetSize / mapRatio);
            }
            else // 大地图更高 或 正方形
            {
                finalW = (int)(targetSize * mapRatio);
                finalH = (int)targetSize;
            }

            // 4. 获取游戏画面的实际渲染区域（不是整个窗口）
            // 使用mainGameScreen的viewportSize来获取游戏画面的实际大小
            Point gameViewSize = Session.MainGame.mainGameScreen.viewportSize;
            int gameW = gameViewSize.X;
            int gameH = gameViewSize.Y;

            // 定义边距 (Padding) - 距离游戏画面边缘的距离
            int margin = 0; // 稍微增加边距，让小地图不要太贴边 -> 改为0，贴合边缘

            // ---------------------------------------------------
            // 【停靠在游戏画面的右下角】
            // ---------------------------------------------------
            // X = 游戏画面宽 - 地图宽 - 边距
            // Y = 游戏画面高 - 地图高 - 边距 (保证底部对齐)
            int x = gameW - finalW - margin;
            int y = gameH - finalH - margin;

            // ---------------------------------------------------
            // 5. 安全钳制 (Clamping) - 防止意外跑出屏幕
            // ---------------------------------------------------
            // 即使缩放特别大，也不允许左上角坐标小于 0
            if (x < 0) x = 0;
            if (y < 0) y = 0;

            // 6. 应用坐标
            // 更新地图大小和显示偏移
            this.mapSize = new Point(finalW, finalH);
            this.MapDisplayOffset = new Point(x, y);
        }

        private Rectangle FrameDisplayPosition
        {
            get
            {
                return new Rectangle(this.MapDisplayOffset.X + this.framePosition.X, this.MapDisplayOffset.Y + this.framePosition.Y, this.framePosition.Width, this.framePosition.Height);
            }
        }

        private Rectangle frameTopPosition
        {
            get
            {
                return new Rectangle(this.MapDisplayOffset.X + this.framePosition.X, this.MapDisplayOffset.Y + this.framePosition.Y, this.framePosition.Width, 2);
            }
        }

        private Rectangle frameLeftPosition
        {
            get
            {
                return new Rectangle(this.MapDisplayOffset.X + this.framePosition.X, this.MapDisplayOffset.Y + this.framePosition.Y, 2, this.framePosition.Height);
            }
        }

        private Rectangle frameBottomPosition
        {
            get
            {
                return new Rectangle(this.MapDisplayOffset.X + this.framePosition.X, this.MapDisplayOffset.Y + this.framePosition.Y + this.framePosition.Height-1, this.framePosition.Width+1, 2);
            }
        }

        private Rectangle frameRightPosition
        {
            get
            {
                return new Rectangle(this.MapDisplayOffset.X + this.framePosition.X + this.framePosition.Width-1, this.MapDisplayOffset.Y + this.framePosition.Y, 2, this.framePosition.Height+1);
            }
        }

        internal bool IsMapShowing
        {
            get
            {
                return this.isMapShowing;
            }
            set
            {
                this.isMapShowing = value;
                if (value)
                {
                    Session.MainGame.mainGameScreen.OnMouseRightUp += new Screen.MouseRightUp(this.screen_OnMouseRightUp);
                    //Session.MainGame.mainGameScreen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);

                    this.SetDisplayOffset(Session.MainGame.mainGameScreen, this.MapShowPosition);
                    this.AddDisableRects();
                }
                else
                {
                    Session.MainGame.mainGameScreen.OnMouseRightUp -= new Screen.MouseRightUp(this.screen_OnMouseRightUp);
                    //Session.MainGame.mainGameScreen.OnMouseMove -= new Screen.MouseMove(this.screen_OnMouseMove);
                    this.RemoveDisableRects();
                }
            }
        }

        internal Rectangle MapPosition
        {
            get
            {
                return new Rectangle(this.MapDisplayOffset.X, this.MapDisplayOffset.Y, this.mapSize.X, this.mapSize.Y);
            }
        }

        private Rectangle ToolDisplayPosition
        {
            get
            {
                return new Rectangle(this.ToolPosition.X + this.DisplayOffset.X, this.ToolPosition.Y + this.DisplayOffset.Y, this.ToolPosition.Width, this.ToolPosition.Height);
            }
        }

        private Rectangle TroopToolDisplayPosition
        {
            get
            {
                return new Rectangle(this.TroopToolPosition.X + this.DisplayOffset.X, this.TroopToolPosition.Y + this.DisplayOffset.Y, this.TroopToolPosition.Width, this.TroopToolPosition.Height);
            }
        }
    }

 

}
