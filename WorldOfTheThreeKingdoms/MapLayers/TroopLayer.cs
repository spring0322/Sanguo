using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms;
using PluginInterface;
using Microsoft.Xna.Framework.Graphics;
using GameObjects.Animations;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameScreens.ScreenLayers
{
    public class TroopLayer
    {
        // 🔥 缓存光环纹理和源矩形，避免每帧查找和分配
        // 保持强引用，防止被 CacheManager 或 GC 清理
        private Texture2D _cachedAuraTexture;
        private Rectangle _cachedAuraSourceRect;
        private Vector2 _cachedAuraTextureCenter; // 🔥 缓存纹理中心，避免每帧分配
        private bool _auraResourcesInitialized = false;
        private readonly string _auraTexturePath = "Content/Textures/GameComponents/TroopAuraRing.png";

        public void Draw(Point viewportSize, GameTime gameTime)
        {
            if (Session.Current?.Scenario == null) return;
            if (Session.MainGame?.mainGameScreen?.mainMapLayer?.Tiles == null) return;
            
            bool playerControlling = Session.Current.Scenario.IsPlayerControlling();
            
            // 🔥 性能优化：提升到循环外，避免每个部队都重复访问
            // 日期：2026-03-18
            // 注意：leftEdge/topEdge 不能缓存，因为它们可能在 Draw 期间被修改（鼠标拖动地图）
            var mapLayer = Session.MainGame.mainGameScreen.mainMapLayer;
            int tileWidth = mapLayer.TileWidth;
            int tileHeight = mapLayer.TileHeight;
            
            if (Setting.Current.GlobalVariables.DrawTroopAnimation)
            {
                bool hold = false;
                
            //Label_097B:
                // 🔥 性能修复：直接遍历 Troops，避免 GetList() 的分配和复制
                // 日期：2026-03-18
                // 问题：GetList() 每次调用都创建新的 GameObjectList 并复制所有元素
                // 解决：GameObjectList 实现了 IEnumerable<GameObject>，可以直接遍历
                foreach (Troop troop in Session.Current.Scenario.Troops)
                {

                    
                    if (troop == null) continue;
                    
                    if (troop.Destroyed)
                    {
                        continue;
                    }
                    // 🔧 修复左上角幽灵兵模：跳过无势力的孤儿部队（simulate troop 泄漏）
                    if (troop.BelongedFaction == null)
                    {
                        continue;
                    }
                    if (!troop.DrawAnimation)
                    {

                        
                        troop.SetNotShowing();
                        continue;
                    }
                    

                    
                    // 🔥 修复：检测并销毁越界的僵尸部队
                    // 根因：军团解散/寻路错误/序列化损坏导致部队坐标越界
                    // 解决：主动销毁越界部队，而非仅跳过渲染（避免幽灵兵模）
                    if (troop.Position.X < 0 || troop.Position.Y < 0 || 
                        troop.Position.X >= Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(0) ||
                        troop.Position.Y >= Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(1))
                    {

                        
                        // 标记为已销毁，下一帧将被清理
                        troop.Destroyed = true;
                        continue;
                    }
                    
                    // 🔥 修复：直接从视觉位置计算屏幕坐标，避免使用缓存的 Tile.Destination
                    // 日期：2026-03-18
                    // 问题：Tile.Destination 包含视口偏移（leftEdge/topEdge），当视口移动时会变化，
                    //       导致兵模跟随视角移动（幽灵兵模）
                    // 解决：直接从逻辑坐标计算屏幕坐标（visualPos × TileWidth + leftEdge）
                    // 关键：leftEdge/topEdge 必须每次读取最新值，不能缓存
                    Vector2 visualPos = troop.VisualPosition;
                    
                    #if DEBUG
                    // 🔥 临时调试：输出前 3 个部队的坐标（限制输出量）
                    // 警告：这会严重影响性能，仅用于临时诊断，修复后必须移除
                    // 注意：debugCount 不能是 static，改为实例字段或移除
                    #endif
                    
                    // 🔥 关键修复：直接计算屏幕坐标，不依赖 Tile.Destination 缓存
                    // 性能优化：tileWidth/tileHeight 在循环外缓存，leftEdge/topEdge 每次读取最新值
                    Rectangle tileDestination = new Rectangle(
                        (int)(visualPos.X * tileWidth + mapLayer.LeftEdge + 0.5f),
                        (int)(visualPos.Y * tileHeight + mapLayer.TopEdge + 0.5f),
                        tileWidth,
                        tileHeight
                    );
                    
                    /*
                    #if DEBUG
                    // 🔥 终极诊断：追踪完整渲染管线（逻辑坐标 → 视觉坐标 → 像素坐标）
                    if (troop.ManualControl)
                    {
                        // 移除 Action 限制，追踪所有状态
                        System.Diagnostics.Debug.WriteLine($"[TroopLayer.Draw] {troop.DisplayName} 渲染管线: Action={troop.Action} Logic={troop.Position} → Visual=({visualPos.X:F4},{visualPos.Y:F4}) → SubPixel=({subPixelX:F2},{subPixelY:F2}) → FinalDest=({tileDestination.X},{tileDestination.Y})");
                    }
                    #endif
                    */
                    
                    if (Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(troop.Position) && (((Session.GlobalVariables.SkyEye || Session.Current.Scenario.NoCurrentPlayer) || Session.Current.Scenario.CurrentPlayer.IsFriendly(troop.BelongedFaction)) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)))
                    {
                        // 🎨 绘制战术威压光环（所有部队）
                        // 修复日期：2026-03-19
                        // 功能：根据部队 Action 状态动态显示不同颜色和旋转速度的光环
                        // - Stop: 势力颜色，慢速旋转
                        // - Move: 深紫色，快速旋转
                        // - Attack: 鲜红色，极速旋转
                        // - Cast: 橙色，中速旋转
                        DrawTacticalAura(
                            troop,
                            tileDestination.X + tileDestination.Width * 0.5f,
                            tileDestination.Y + tileDestination.Height * 0.5f,
                            gameTime,
                            tileWidth
                        );

                        
                        Color white = Color.White;
                        // 🔧 确保颜色的 Alpha 值为 255（完全不透明）
                        white = new Color((byte)white.R, (byte)white.G, (byte)white.B, (byte)255);
                        
                        if (troop.CurrentOutburstKind == OutburstKind.愤怒)
                        {
                            white = new Color((byte)255, (byte)0, (byte)0, (byte)255); // 确保红色也是不透明的
                        }
                        else if (troop.CurrentOutburstKind == OutburstKind.沉静)
                        {
                            white = new Color((byte)0, (byte)255, (byte)0, (byte)255); // 确保绿色也是不透明的
                        }
                        if (!(((Session.GlobalVariables.SkyEye || (Session.Current.Scenario.CurrentPlayer == null)) || (troop.Status != TroopStatus.埋伏)) || troop.IsFriendly(Session.Current.Scenario.CurrentPlayer)))
                        {
                            troop.SetNotShowing();
                            continue;
                        }
                        if (troop.TileAnimation == null) 
                        {
                            troop.SetNotShowing();
                            continue;
                        }
                        if (troop.TileAnimation.FrameCount == 0)
                        {
                            troop.TileAnimation.FrameCount = 1;
                        }

                        if ((troop.Action == TroopAction.Stop) && (troop.PreAction != TroopPreAction.无))
                        {
                            // 🔥 技术性修复：添加空引用检查
                            if (troop.TileAnimation?.Texture != null)
                            {
                                int frameWidth = troop.TileAnimation.FrameCount > 0 ? 
                                    Math.Max(1, troop.TileAnimation.Texture.Width / troop.TileAnimation.FrameCount) : 1;
                                CacheManager.Draw(troop.TileAnimation.Texture, tileDestination, new Rectangle?(troop.GetCurrentPreTroopActionRectangle(frameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                            }
                            this.DrawStoppedTroop( viewportSize, troop, tileDestination);
                        }
                        else if (troop.Action == TroopAction.Stop)
                        {
                            this.DrawStoppedTroop(viewportSize, troop, tileDestination);
                        }
                        else if (troop.Action == TroopAction.Move)
                        {
                            // ---------------------------------------------------------
                            // FIXED LOGIC: Moving troop - Draw even if Animation is missing
                            // ---------------------------------------------------------
                            // 🔧 使用 Move.png 的硬编码规格
                            int moveFrameWidth = 128;  // 硬编码帧宽度
                            int moveFrameHeight = 128; // 硬编码帧高度
                            int moveTotalColumns = 10; // 总列数
                            
                            // 2. Calculate current animation frame for moving troop
                            int moveCurrentFrame = (int)(gameTime.TotalGameTime.TotalMilliseconds / 150) % moveTotalColumns;
                            int moveCurrentRow = (int)troop.Direction; // 使用对应方向
                            
                            Rectangle moveSourceRect = new Rectangle(moveCurrentFrame * moveFrameWidth, moveCurrentRow * moveFrameHeight, moveFrameWidth, moveFrameHeight);

                            // 3. Calculate destination rectangle for moving troop
                            // 🔥 修复：使用视觉插值位置而不是逻辑位置
                            Rectangle destination = tileDestination;
                            
                            // 4. Draw with proper source rectangle
                            CacheManager.Draw(troop.TroopTexture,
                                destination,
                                moveSourceRect, // Use calculated source rectangle instead of null
                                white,
                                0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                            // ---------------------------------------------------------
                            // 🔥 技术性修复：避免除零异常和空引用异常
                            if (troop.CurrentStunt != null && troop.StuntTileAnimation != null && troop.StuntTileAnimation.Texture != null)
                            {
                                int stuntFrameWidth = troop.StuntTileAnimation.FrameCount > 0 ? 
                                    Math.Max(1, troop.StuntTileAnimation.Texture.Width / troop.StuntTileAnimation.FrameCount) : 1;
                                CacheManager.Draw(troop.StuntTileAnimation.Texture, tileDestination, new Rectangle?(troop.GetStuntTroopTileAnimationRectangle(stuntFrameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                            }
                        }
                        else
                        {
                            Rectangle? nullable;
                            if ((troop.Action == TroopAction.Attack) || (troop.Action == TroopAction.Cast))
                            {
                                if (troop.PreAction != TroopPreAction.无)
                                {
                                    // 🔥 技术性修复：添加空引用检查
                                    if (troop.TileAnimation?.Texture != null)
                                    {
                                        int frameWidth = troop.TileAnimation.FrameCount > 0 ? 
                                            Math.Max(1, troop.TileAnimation.Texture.Width / troop.TileAnimation.FrameCount) : 1;
                                        // 🔥 修复：使用视觉插值位置
                                        CacheManager.Draw(troop.TileAnimation.Texture, tileDestination, new Rectangle?(troop.GetCurrentPreTroopActionRectangle(frameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                                    }
                                    hold = true;
                                }
                                troop.PlayCriticalAttackSound();
                                
                                // ---------------------------------------------------------
                                // FIXED LOGIC: Attack/Cast action - Draw even if Animation is missing
                                // ---------------------------------------------------------
                                // 🔧 使用 Move.png 的硬编码规格
                                int attackFrameWidth = 128;  // 硬编码帧宽度
                                int attackFrameHeight = 128; // 硬编码帧高度
                                int attackTotalColumns = 10; // 总列数
                                
                                // 2. Calculate current animation frame for attack
                                int currentFrame = (int)(gameTime.TotalGameTime.TotalMilliseconds / 100) % attackTotalColumns;
                                int currentRow = (int)troop.Direction; // 使用对应方向
                                
                                Rectangle sourceRect = new Rectangle(currentFrame * attackFrameWidth, currentRow * attackFrameHeight, attackFrameWidth, attackFrameHeight);

                                // 3. Draw with proper source rectangle
                                // 🔥 修复：使用视觉插值位置
                                CacheManager.Draw(troop.TroopTexture,
                                    tileDestination,
                                    sourceRect, // Use calculated source rectangle instead of null
                                    white,
                                    0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                                // ---------------------------------------------------------
                                nullable = null;
                                if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 4)
                                {
                                    // 🔥 修复：使用视觉插值位置
                                    CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[4], tileDestination, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                                }
                                // 🔥 技术性修复：避免除零异常和空引用异常
                                if (troop.CurrentStunt != null && troop.StuntTileAnimation != null && troop.StuntTileAnimation.Texture != null)
                                {
                                    int stuntFrameWidth = troop.StuntTileAnimation.FrameCount > 0 ? 
                                        Math.Max(1, troop.StuntTileAnimation.Texture.Width / troop.StuntTileAnimation.FrameCount) : 1;
                                    CacheManager.Draw(troop.StuntTileAnimation.Texture, tileDestination, new Rectangle?(troop.GetStuntTroopTileAnimationRectangle(stuntFrameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                                }
                              
                                troop._attackAnimationFrameCounter++;

                                const int ATTACK_ANIMATION_DURATION_FRAMES = 60;
                                
                                if (troop._attackAnimationFrameCounter >= ATTACK_ANIMATION_DURATION_FRAMES)
                                {
                                    troop.Action = TroopAction.Stop;
                                    troop.ApplyDamageList();
                                    troop.ApplyStratagemEffect();
                                    troop._attackAnimationFrameCounter = 0; 
                                }
                            }
                            else if ((troop.Action == TroopAction.BeAttacked) || (troop.Action == TroopAction.BeCasted))
                            {
                                if (troop.OrientationTroop != null)
                                {
                                    hold = troop.OrientationTroop.PreAction != TroopPreAction.无;
                                }
                                // 🔥 技术性修复：避免除零异常和空引用异常
                                if (troop.Effect != TroopEffect.无 && troop.EffectTileAnimation != null && troop.EffectTileAnimation.Texture != null)
                                {
                                    int effectFrameWidth = troop.EffectTileAnimation.FrameCount > 0 ? 
                                        Math.Max(1, troop.EffectTileAnimation.Texture.Width / troop.EffectTileAnimation.FrameCount) : 1;
                                    CacheManager.Draw(troop.EffectTileAnimation.Texture, tileDestination, new Rectangle?(troop.GetEffectTroopTileAnimationRectangle(effectFrameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                                }
                                
                                // ---------------------------------------------------------
                                // FIXED LOGIC: BeAttacked/BeCasted action - Draw even if Animation is missing
                                // ---------------------------------------------------------
                                // 🔧 使用 Move.png 的硬编码规格
                                int defenseFrameWidth = 128;  // 硬编码帧宽度
                                int defenseFrameHeight = 128; // 硬编码帧高度
                                int defenseTotalColumns = 10; // 总列数
                                
                                // 2. Calculate current animation frame for defense
                                int defenseCurrentFrame = (int)(gameTime.TotalGameTime.TotalMilliseconds / 120) % defenseTotalColumns;
                                int defenseCurrentRow = (int)troop.Direction; // 使用对应方向
                                
                                Rectangle defenseSourceRect = new Rectangle(defenseCurrentFrame * defenseFrameWidth, defenseCurrentRow * defenseFrameHeight, defenseFrameWidth, defenseFrameHeight);

                                // 3. Draw with proper source rectangle
                                // 🔥 修复：使用视觉插值位置
                                CacheManager.Draw(troop.TroopTexture,
                                    tileDestination,
                                    defenseSourceRect, // Use calculated source rectangle instead of null
                                    white,
                                    0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                                // ---------------------------------------------------------
                                nullable = null;
                                if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 2)
                                {
                                    // 🔥 修复：使用视觉插值位置
                                    CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[2], tileDestination, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                                }
                                // 🔥 技术性修复：避免除零异常和空引用异常
                                if (troop.CurrentStunt != null && troop.StuntTileAnimation != null && troop.StuntTileAnimation.Texture != null)
                                {
                                    int stuntFrameWidth = troop.StuntTileAnimation.FrameCount > 0 ? 
                                        Math.Max(1, troop.StuntTileAnimation.Texture.Width / troop.StuntTileAnimation.FrameCount) : 1;
                                    CacheManager.Draw(troop.StuntTileAnimation.Texture, tileDestination, new Rectangle?(troop.GetStuntTroopTileAnimationRectangle(stuntFrameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                                }
                            }
                            else
                            {
                                if (troop.WaitForDeepChaosFrameCount > 0)
                                {
                                    if ((troop.OrientationTroop == null) || troop.OrientationTroop.Destroyed)
                                    {
                                        troop.WaitForDeepChaos = false;
                                        troop.WaitForDeepChaosFrameCount = 0;
                                    }
                                    else
                                    {
                                        troop.WaitForDeepChaosFrameCount--;
                                    }
                                }
                                this.DrawStoppedTroop(viewportSize, troop, tileDestination);
                            }
                        }
                        
                        bool delayCombatNumber =
                            troop.Action == TroopAction.Attack ||
                            troop.Action == TroopAction.Cast ||
                            troop.Action == TroopAction.BeAttacked ||
                            troop.Action == TroopAction.BeCasted ||
                            troop.PreAction != TroopPreAction.无;
                        if (!delayCombatNumber)
                        {
                            if (!troop.IncrementNumberList.IsEmpty)
                            {
                                troop.IncrementNumberList.Draw(Session.Current.Scenario.GameCommonData.NumberGenerator, new GetDisplayRectangle(Session.MainGame.mainGameScreen.mainMapLayer.GetDestination), Session.MainGame.mainGameScreen.mainMapLayer.TileWidth, gameTime);
                            }
                            if (!troop.DecrementNumberList.IsEmpty)
                            {
                                troop.DecrementNumberList.Draw(Session.Current.Scenario.GameCommonData.NumberGenerator, new GetDisplayRectangle(Session.MainGame.mainGameScreen.mainMapLayer.GetDestination), Session.MainGame.mainGameScreen.mainMapLayer.TileWidth, gameTime);
                            }
                        }
                        if (troop.ShowNumber && troop.IncrementNumberList.IsEmpty && troop.DecrementNumberList.IsEmpty)
                        {
                            troop.ShowNumber = false;
                        }
                        Session.MainGame.mainGameScreen.Plugins.TroopTitlePlugin.DrawTroop( troop, playerControlling);
                        this.DrawTroopTarget( troop, viewportSize, gameTime);
                    }
                    else
                    {
                        troop.SetNotShowing();
                    }
                }
            }
            else
            {
                // 🔥 性能修复：直接遍历 Troops，避免 GetList() 的分配和复制
                // 日期：2026-03-18
                foreach (Troop troop in Session.Current.Scenario.Troops)
                {
                    if ((troop != null) && !troop.Destroyed)
                    {
                        troop.SetNotShowing();
                        if (Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(troop.Position) && (((Session.GlobalVariables.SkyEye || Session.Current.Scenario.NoCurrentPlayer) || Session.Current.Scenario.CurrentPlayer.IsFriendly(troop.BelongedFaction)) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)))
                        {
                            // 🔥 修复：直接从视觉位置计算屏幕坐标（与上面动画分支保持一致）
                            // 日期：2026-03-18
                            Vector2 visualPos = troop.VisualPosition;
                            
                            // 🔥 关键修复：直接计算屏幕坐标，不依赖 Tile.Destination 缓存
                            // 性能优化：tileWidth/tileHeight 在循环外缓存，leftEdge/topEdge 每次读取最新值
                            Rectangle renderDest = new Rectangle(
                                (int)(visualPos.X * tileWidth + mapLayer.LeftEdge + 0.5f),
                                (int)(visualPos.Y * tileHeight + mapLayer.TopEdge + 0.5f),
                                tileWidth,
                                tileHeight
                            );
                            
                            // 🎨 绘制战术威压光环（所有部队）
                            // 修复日期：2026-03-19
                            // 功能：根据部队 Action 状态动态显示不同颜色和旋转速度的光环
                            DrawTacticalAura(
                                troop,
                                renderDest.X + renderDest.Width * 0.5f,
                                renderDest.Y + renderDest.Height * 0.5f,
                                gameTime,
                                tileWidth
                            );
                            
                            this.DrawStoppedTroop( viewportSize, troop, renderDest);
                            Session.MainGame.mainGameScreen.Plugins.TroopTitlePlugin.DrawTroop( troop, playerControlling);
                            this.DrawTroopTarget( troop, viewportSize, gameTime);
                        }
                    }
                }
            }
        }

        private void DrawStoppedTroop(Point viewportSize, Troop troop, Rectangle renderDestination)
        {
            if (troop == null) return;
            if (Session.MainGame?.mainGameScreen?.mainMapLayer?.Tiles == null) return;
            
            // 🔥 技术性修复：添加边界检查避免数组越界
            if (troop.Position.X < 0 || troop.Position.Y < 0 || 
                troop.Position.X >= Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(0) ||
                troop.Position.Y >= Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(1))
            {
                return;
            }
            
            Rectangle? nullable;
            Color white = new Color((byte)255, (byte)255, (byte)255, (byte)255); // 🔧 确保颜色完全不透明
            if ((Session.MainGame.mainGameScreen.DrawingSelector && (troop.Status == TroopStatus.一般)) && 
                Session.Current.Scenario.IsCurrentPlayer(troop.BelongedFaction) && !troop.Operated)
            {
                Point positionByPoint = Session.MainGame.mainGameScreen.GetPositionByPoint(Session.MainGame.mainGameScreen.SelectorStartPosition);
                Point point2 = Session.MainGame.mainGameScreen.GetPositionByPoint(Session.MainGame.mainGameScreen.MousePosition);
                Rectangle r = new Rectangle(
                    Math.Min(point2.X, positionByPoint.X), Math.Min(point2.Y, positionByPoint.Y), 
                    Math.Abs(point2.X - positionByPoint.X), Math.Abs(point2.Y - positionByPoint.Y));
                if (r.Contains(troop.Position))
                {
                    white = new Color((byte)0, (byte)0, (byte)255, (byte)255); // 蓝色，完全不透明
                }
            }
            if (troop.DrawSelected && !troop.Operated && Session.MainGame.mainGameScreen.CurrentTroop == null && !Session.MainGame.mainGameScreen.Plugins.ContextMenuPlugin.IsShowing)
            {
                if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 4)
                {
                    CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[4], renderDestination, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                }
            }
            if (troop.CurrentOutburstKind == OutburstKind.愤怒)
            {
                white = new Color((byte)255, (byte)0, (byte)0, (byte)255); // 红色，完全不透明
            }
            else if (troop.CurrentOutburstKind == OutburstKind.沉静)
            {
                white = new Color((byte)0, (byte)255, (byte)0, (byte)255); // 绿色，完全不透明
            }
            
            // ---------------------------------------------------------
            // 修复：使用动画状态机驱动静止部队的帧循环，确保所有电脑表现一致
            // ---------------------------------------------------------
            Rectangle sourceRect;
            if (troop.CurrentAnimation != null && troop.CurrentAnimation.FrameCount > 0)
            {
                int frameWidthForAnim = troop.TroopTexture.Width / troop.CurrentAnimation.FrameCount;
                sourceRect = troop.GetCurrentStopDisplayRectangle(Math.Max(1, frameWidthForAnim));
            }
            else
            {
                // 兜底：动画数据缺失时显示第一帧
                int currentRow = (int)troop.Direction;
                sourceRect = new Rectangle(0, currentRow * 128, 128, 128);
            }

            CacheManager.Draw(troop.TroopTexture,
                renderDestination,
                sourceRect,
                white,
                0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            // ---------------------------------------------------------
            
            // COMMENTED OUT: Complex source rectangle calculation that was causing invisible sprites
            // CacheManager.Draw(troop.TroopTexture, renderDestination, new Rectangle?(troop.GetCurrentStopDisplayRectangle(troop.TroopTexture.Width / troop.CurrentAnimation.FrameCount)), white, 0f, Vector2.Zero, SpriteEffects.None, 0.7f);
            if (Session.MainGame.mainGameScreen.SelectorTroops.HasGameObject(troop.ID))
            {
                nullable = null;
                if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 4)
                {
                    CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[4], renderDestination, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                }
            }
            if (troop.Surrounding)
            {
                nullable = null;
                if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 4)
                {
                    CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[4], renderDestination, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                }
            }
            // 🔥 技术性修复：避免除零异常和空引用异常
            if (troop.Status != TroopStatus.一般 && troop.StatusTileAnimation != null && troop.StatusTileAnimation.Texture != null)
            {
                int statusFrameWidth = troop.StatusTileAnimation.FrameCount > 0 ? 
                    Math.Max(1, troop.StatusTileAnimation.Texture.Width / troop.StatusTileAnimation.FrameCount) : 1;
                CacheManager.Draw(troop.StatusTileAnimation.Texture, renderDestination, new Rectangle?(troop.GetStatusTroopTileAnimationRectangle(statusFrameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
            }
            // 🔥 技术性修复：避免除零异常和空引用异常
            if (troop.CurrentStunt != null && troop.StuntTileAnimation != null && troop.StuntTileAnimation.Texture != null)
            {
                int stuntFrameWidth = troop.StuntTileAnimation.FrameCount > 0 ? 
                    Math.Max(1, troop.StuntTileAnimation.Texture.Width / troop.StuntTileAnimation.FrameCount) : 1;
                CacheManager.Draw(troop.StuntTileAnimation.Texture, renderDestination, new Rectangle?(troop.GetStuntTroopTileAnimationRectangle(stuntFrameWidth)), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }
            if ((Session.MainGame.mainGameScreen.CurrentTroop == troop) && (Session.MainGame.mainGameScreen.UndoneWorks.Peek().Kind == UndoneWorkKind.ContextMenu))
            {
                foreach (Point point3 in troop.OffenceArea.Area)
                {
                    if (Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(point3))
                    {
                        nullable = null;
                        if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                            Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 2)
                        {
                            CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[2], Session.MainGame.mainGameScreen.mainMapLayer.Tiles[point3.X, point3.Y].Destination, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                        }
                    }
                }
            }
        }

        private void DrawTroopPath(Troop troop, Point viewportSize, GameTime gameTime)
        {
            if (troop == null) return;
            if (Session.MainGame?.mainGameScreen?.mainMapLayer?.Tiles == null) return;
            
            if (((Session.MainGame.mainGameScreen.CurrentTroop == troop) && (Session.MainGame.mainGameScreen.UndoneWorks.Peek().Kind == UndoneWorkKind.Selecting)) && (((SelectingUndoneWorkKind)Session.MainGame.mainGameScreen.UndoneWorks.Peek().SubKind) == SelectingUndoneWorkKind.TroopDestination))
            {
                Rectangle rectangle;
                Rectangle? nullable;
                if (troop.FirstTierPath.Count > 0)
                {
                    foreach (Point point in troop.UnfinishedFirstTierPath)
                    {
                        if (Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(point))
                        {
                            nullable = null;
                            if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                                Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 0)
                            {
                                CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[0], Session.MainGame.mainGameScreen.mainMapLayer.Tiles[point.X, point.Y].Destination, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                            }
                        }
                    }
                }
                if (troop.SecondTierPath != null)
                {
                    rectangle.Width = Session.MainGame.mainGameScreen.mainMapLayer.TileWidth * GameObjectConsts.SecondTierSquareSize;
                    rectangle.Height = Session.MainGame.mainGameScreen.mainMapLayer.TileHeight * GameObjectConsts.SecondTierSquareSize;
                    foreach (Point point in troop.UnfinishedSecondTierPath)
                    {
                        rectangle.X = Session.MainGame.mainGameScreen.mainMapLayer.LeftEdge + (point.X * rectangle.Width);
                        rectangle.Y = Session.MainGame.mainGameScreen.mainMapLayer.TopEdge + (point.Y * rectangle.Height);
                        if (StaticMethods.RectangleInViewport(rectangle, viewportSize))
                        {
                            nullable = null;
                            if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                                Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 5)
                            {
                                CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[5], rectangle, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                            }
                        }
                    }
                }
                if (troop.ThirdTierPath != null)
                {
                    rectangle.Width = Session.MainGame.mainGameScreen.mainMapLayer.TileWidth * GameObjectConsts.ThirdTierSquareSize;
                    rectangle.Height = Session.MainGame.mainGameScreen.mainMapLayer.TileHeight * GameObjectConsts.ThirdTierSquareSize;
                    foreach (Point point in troop.UnfinishedThirdTierPath)
                    {
                        rectangle.X = Session.MainGame.mainGameScreen.mainMapLayer.LeftEdge + (point.X * rectangle.Width);
                        rectangle.Y = Session.MainGame.mainGameScreen.mainMapLayer.TopEdge + (point.Y * rectangle.Height);
                        if (StaticMethods.RectangleInViewport(rectangle, viewportSize))
                        {
                            nullable = null;
                            if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                                Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 5)
                            {
                                CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[5], rectangle, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                            }
                        }
                    }
                }
                if (Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(troop.RealDestination))
                {
                    if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                        Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 6)
                    {
                        CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[6], Session.MainGame.mainGameScreen.mainMapLayer.Tiles[troop.RealDestination.X, troop.RealDestination.Y].Destination, null, new Color(255f, (float)(gameTime.TotalGameTime.Milliseconds / 4), 255f), 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                    }
                }
            }
        }

        private void DrawTroopTarget(Troop troop, Point viewportSize, GameTime gameTime)
        {
            if (troop == null) return;
            if (Session.MainGame?.mainGameScreen?.mainMapLayer?.Tiles == null) return;
            
            if ((((Session.MainGame.mainGameScreen.CurrentTroop == troop) && (Session.MainGame.mainGameScreen.UndoneWorks.Peek().Kind == UndoneWorkKind.Selecting)) && (((SelectingUndoneWorkKind)Session.MainGame.mainGameScreen.UndoneWorks.Peek().SubKind) == SelectingUndoneWorkKind.TroopTarget)) && ((troop.TargetTroop != null) && Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(troop.TargetTroop.Position)))
            {
                if (Session.MainGame.mainGameScreen.Textures?.TileFrameTextures != null && 
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 6)
                {
                    CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.TileFrameTextures[6], Session.MainGame.mainGameScreen.mainMapLayer.Tiles[troop.TargetTroop.Position.X, troop.TargetTroop.Position.Y].Destination, null, new Color(255f, (float)(gameTime.TotalGameTime.Milliseconds / 4), 255f), 0f, Vector2.Zero, SpriteEffects.None, 0.3f);
                }
            }
        }

        public void Initialize()
        {
            InitializeAuraResources();
        }

        /// <summary>
        /// 初始化光环渲染资源（在 LoadContent 中调用）
        /// 🧊 COLD PATH：允许使用可读性优先的代码
        /// </summary>
        private void InitializeAuraResources()
        {
            try
            {
                // 🔥 修复：使用 LoadTexture 加载到持久化缓存（而非临时缓存）
                // isTemp=false：加载到 TextureDics（持久化），不会被自动清理
                // 路径格式：Content/Textures/... （不需要 Content.mgcb 处理）
                _cachedAuraTexture = CacheManager.LoadTexture(_auraTexturePath);
                
                // 🔥 ANTI-BAND-AID：明确检查加载结果，Fail Fast
                if (_cachedAuraTexture == null)
                {
                    throw new InvalidOperationException(
                        $"[TroopLayer] 光环纹理加载失败：CacheManager.LoadTexture 返回 null，请确认文件存在：{_auraTexturePath}");
                }
                
                // 🔥 检查纹理是否已被释放（设备丢失等）
                if (_cachedAuraTexture.IsDisposed)
                {
                    throw new InvalidOperationException(
                        "[TroopLayer] 光环纹理加载失败：Texture2D.IsDisposed=true（可能是设备丢失或纹理损坏）");
                }
                
                // 缓存源矩形（整个纹理），避免每帧分配
                _cachedAuraSourceRect = new Rectangle(0, 0, _cachedAuraTexture.Width, _cachedAuraTexture.Height);
                
                // 🔥 HOT PATH 优化：预计算纹理中心，避免每帧分配 Vector2
                // 日期：2026-03-19
                _cachedAuraTextureCenter = new Vector2(
                    _cachedAuraTexture.Width * 0.5f,
                    _cachedAuraTexture.Height * 0.5f
                );
                
                _auraResourcesInitialized = true;
                
                System.Diagnostics.Debug.WriteLine($"[TroopLayer] 光环系统初始化成功：纹理尺寸 {_cachedAuraTexture.Width}x{_cachedAuraTexture.Height}");
            }
            catch (Exception ex)
            {
                // 🔥 记录错误但不崩溃（光环是视觉增强，不影响游戏逻辑）
                System.Diagnostics.Debug.WriteLine($"[TroopLayer] 光环系统初始化失败: {ex.Message}");
                _auraResourcesInitialized = false;
                _cachedAuraTexture = null;
            }
        }

        /// <summary>
        /// 检查并重新加载纹理（如果被释放）
        /// 🔥 HOT PATH：每帧调用，必须高效
        /// </summary>
        private bool EnsureAuraTextureValid()
        {
            // 快速返回：已初始化且纹理有效
            if (_auraResourcesInitialized && _cachedAuraTexture != null && !_cachedAuraTexture.IsDisposed)
            {
                return true;
            }
            
            // 纹理被释放或未初始化，尝试重新加载
            if (_cachedAuraTexture == null || _cachedAuraTexture.IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[TroopLayer] 检测到光环纹理被释放，尝试重新加载...");
                InitializeAuraResources();
            }
            
            // 返回最终状态
            return _auraResourcesInitialized && _cachedAuraTexture != null && !_cachedAuraTexture.IsDisposed;
        }

        /// <summary>
        /// 绘制部队战术威压光环（动态墨圈特效）
        /// 🔥 HOT PATH：每帧调用，零分配，无 LINQ
        /// 日期：2026-03-19
        /// 功能：根据部队 Action 状态动态调整颜色、旋转速度、呼吸效果
        /// </summary>
        private void DrawTacticalAura(
            Troop troop,
            float screenX,
            float screenY,
            GameTime gameTime,
            float tileWidth)
        {
            // 🔥 关键修复：每次绘制前检查纹理是否有效
            if (!EnsureAuraTextureValid())
            {
                return;
            }

            // 1. 获取威压能量（仅用于判断是否渲染）
            int zocEnergy = troop.CalculateZocEnergy();
            if (zocEnergy <= 0) return;
            
            #if DEBUG
            // 🔥 临时调试：输出部队状态，诊断为什么状态变化不生效
            // ⚠️ 警告：这会严重影响性能（每帧字符串分配），诊断完成后必须移除
            // 限制输出频率：每 60 帧输出一次
            /*
            if ((int)(gameTime.TotalGameTime.TotalMilliseconds / 16.67) % 60 == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawTacticalAura] 部队:{troop.DisplayName} Action:{troop.Action} 势力:{troop.BelongedFaction.Name}");
            }
            */
            #endif
            
            // 🔥 固定领域大小为1个格子（只覆盖部队所在格子）
            float radiusTiles = 0.5f;
            
            // 2. 状态映射系统：根据 TroopAction 动态调整参数
            Color baseColor;
            float baseRotationSpeed;
            float baseAlpha;
            float breathingAmount;
            float breathingSpeed;
            
            switch (troop.Action)
            {
                case TroopAction.Move:
                    // 行军：深紫色，快速旋转，中速呼吸
                    baseColor = new Color(75, 0, 130); // DeepPurple
                    baseRotationSpeed = 2.5f;
                    baseAlpha = 0.6f;
                    breathingAmount = 0.2f;
                    breathingSpeed = 1.5f;
                    break;
                    
                case TroopAction.Attack:
                case TroopAction.BeAttacked:
                    // 交战：鲜红色，极速旋转，快速闪烁
                    baseColor = new Color(255, 0, 0); // Red
                    baseRotationSpeed = 4.0f;
                    baseAlpha = 0.8f;
                    breathingAmount = 0.6f;
                    breathingSpeed = 5.0f;
                    break;
                    
                case TroopAction.Cast:
                case TroopAction.BeCasted:
                    // 施法：橙色，中速旋转，强呼吸
                    baseColor = new Color(255, 140, 0); // DarkOrange
                    baseRotationSpeed = 3.0f;
                    baseAlpha = 0.7f;
                    breathingAmount = 0.4f;
                    breathingSpeed = 3.0f;
                    break;
                    
                case TroopAction.Stop:
                default:
                    // 静止/默认：势力颜色，中速旋转，标准呼吸
                    if (troop.BelongedFaction == null)
                    {
                        throw new InvalidOperationException(
                            $"数据损坏：部队 {troop.ID} 的 BelongedFaction 为 null");
                    }
                    baseColor = troop.BelongedFaction.FactionColor;
                    baseRotationSpeed = 1.0f;
                    baseAlpha = 0.5f;
                    breathingAmount = 0.2f;
                    breathingSpeed = 1.0f;
                    break;
            }
            
            // 3. 动态参数实时计算
            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            
            // 计算旋转角度（基于时间与状态速度）
            float currentRotation = time * baseRotationSpeed * MathHelper.TwoPi;
            
            // 计算呼吸透明度（基于正弦波与状态参数）
            float breathingFactor = baseAlpha + MathF.Sin(time * breathingSpeed * MathHelper.TwoPi) * breathingAmount;
            
            // 🔥 HOT PATH：手动 Clamp，避免 Math.Clamp 的潜在装箱
            if (breathingFactor < 0f) breathingFactor = 0f;
            if (breathingFactor > 1f) breathingFactor = 1f;
            
            // 4. 预乘 Alpha 颜色混合
            // 🔥 修复：只调整 Alpha 通道，保持 RGB 原色
            // 日期：2026-03-19
            // 问题：之前将 breathingFactor 应用到 RGB，导致所有颜色都变暗，看不出差异
            // 解决：RGB 保持原色，只用 breathingFactor 调整透明度
            Color renderColor = new Color(
                baseColor.R,
                baseColor.G,
                baseColor.B,
                (int)(255 * breathingFactor)
            );
            
            #if DEBUG
            // 🔥 临时调试：输出颜色和参数
            // ⚠️ 警告：这会严重影响性能（每帧字符串分配），诊断完成后必须移除
            // 限制输出频率：每 60 帧输出一次
            /*
            if ((int)(gameTime.TotalGameTime.TotalMilliseconds / 16.67) % 60 == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawTacticalAura] 颜色:R={baseColor.R} G={baseColor.G} B={baseColor.B} Alpha={(int)(255 * breathingFactor)} 旋转速度:{baseRotationSpeed}");
            }
            */
            #endif
            
            // 5. 计算目标矩形和旋转原点
            float targetDiameter = radiusTiles * 2f * tileWidth;
            
            // 🔥 关键修复：旋转时必须正确设置 origin 和 destination
            // 日期：2026-03-19
            // 原因：origin = Vector2.Zero 导致旋转围绕左上角，光环跑到其他格子
            // 解决：
            //   1. origin = 纹理中心（在纹理坐标系中，已预计算缓存）
            //   2. destination 的位置 = 屏幕中心点（screenX, screenY 已经是中心）
            //   这样旋转就会围绕格子中心进行，不会跑到其他格子
            
            // destination 的位置应该是屏幕中心点，不需要减去半径
            // 因为 origin 会处理偏移
            int destX = (int)screenX;
            int destY = (int)screenY;
            
            // 🔥 HOT PATH：值类型栈上分配，零 GC
            Rectangle destination = new Rectangle(destX, destY, (int)targetDiameter, (int)targetDiameter);
            
            // 6. 绘制旋转光环（使用 8 参数重载）
            // 🔥 关键：使用预缓存的纹理中心作为 origin，让旋转围绕中心进行
            // 🔥 修复日期：2026-03-20
            // 🔥 问题：depth=0.1f 导致光环渲染在部队模型上方（部队depth=0.3f~0.5f）
            // 🔥 解决：depth=0.6f 让光环渲染在部队模型下方，视觉层次正确
            CacheManager.Draw(
                texture: _cachedAuraTexture,
                destination: destination,
                source: _cachedAuraSourceRect,
                color: renderColor,
                rotation: currentRotation,
                origin: _cachedAuraTextureCenter,
                effect: SpriteEffects.None,
                depth: 0.6f  // 🔥 修复：从 0.1f 改为 0.6f，渲染在部队模型下方
            );
        }
    }

 

}
