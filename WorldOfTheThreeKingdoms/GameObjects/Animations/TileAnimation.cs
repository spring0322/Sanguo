using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using WorldOfTheThreeKingdoms.GameGlobal;



namespace GameObjects.Animations
{
    public class TileAnimation
    {
        public int currentFrameIndex;
        public int currentStayIndex;
        public bool Drawing;
        public TileAnimationKind Kind;
        public Animation LinkedAnimation;
        public bool Looping;
        public Point Position;

        public void Draw(Rectangle destination)
        {
            if (this.Drawing)
            {
                // 🔥 Anti-Band-Aid：不添加空检查，让它崩溃
                // 日期：2026-03-09
                // 原因：如果 LinkedAnimation 或 Texture 为 null，说明 TileAnimationGenerator.AddTileAnimation 初始化错误
                // 解决：让它崩溃，追溯到数据源（AllTileAnimations 加载或序列化逻辑）
                // 诊断：如果崩溃，检查 TileAnimationGenerator.AddTileAnimation 的日志输出
                
                bool endLoop = false;
                // 🔥 修复：战法动画必须显示在最前面，使用极小的深度值
                // 参考：CombatNumberItem 使用 0.01f，自动存档使用 0.0001f
                // 战法动画应该比战斗数字更靠前，使用 0.005f
                float layerDepth = this.LinkedAnimation.Back ? 0.008f : 0.005f;
                
                // 🔥 临时诊断：只在前3帧和最后1帧输出（避免 Hot Path 高频字符串分配）
                #if DEBUG
                /*
                bool shouldLog = this.currentFrameIndex < 3 || endLoop;
                if (shouldLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] 绘制前 - Kind={this.Kind}, Pos={this.Position}, Frame={this.currentFrameIndex}/{this.LinkedAnimation.FrameCount}, Stay={this.currentStayIndex}/{this.LinkedAnimation.StayCount}");
                }
                */
                #endif
                
                // 🔥 修复：恢复正确的动画帧切片逻辑
                // 日期：2026-03-04
                // 问题：火焰等地块动画没有正确显示，显示为整张纹理
                // 原因：之前为了修复"不可见精灵"问题，强制使用null作为sourceRectangle，导致无法正确切片
                // 解决：使用正确的帧切片逻辑，如果矩形无效说明动画配置数据错误，应该修复数据源
                Rectangle sourceRect = this.LinkedAnimation.GetCurrentDisplayRectangle(
                    ref this.currentFrameIndex, 
                    ref this.currentStayIndex, 
                    this.LinkedAnimation.Texture.Width / this.LinkedAnimation.FrameCount, 
                    0, 
                    out endLoop, 
                    false);
                
                #if DEBUG
                /*
                if (shouldLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] 绘制后 - Frame={this.currentFrameIndex}, Stay={this.currentStayIndex}, EndLoop={endLoop}, SrcRect={sourceRect}");
                    System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] 准备调用 CacheManager.Draw - LinkedAnimation.Texture={this.LinkedAnimation.Texture != null}, Texture2D={this.LinkedAnimation.Texture.Texture != null}, Dest={destination}, Source={sourceRect}");
                    System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] 当前 CacheManager.Scale={CacheManager.Scale}, LayerDepth={layerDepth}");
                    
                    // 🔥 新增：详细的纹理信息调试
                    if (this.LinkedAnimation.Texture != null && this.LinkedAnimation.Texture.Texture != null)
                    {
                        var tex = this.LinkedAnimation.Texture.Texture;
                        System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] 纹理详情 - Name={tex.Name}, Size={tex.Width}x{tex.Height}, Format={tex.Format}, IsDisposed={tex.IsDisposed}");
                        System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] 源矩形验证 - 源矩形={sourceRect}, 纹理尺寸={tex.Width}x{tex.Height}, 超出边界={(sourceRect.Right > tex.Width || sourceRect.Bottom > tex.Height)}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] ❌ 纹理为空 - LinkedAnimation.Texture={this.LinkedAnimation.Texture}, Texture2D={(this.LinkedAnimation.Texture?.Texture)}");
                    }
                }
                */
                #endif
                
                // 🔥 修复：战法动画绘制时强制使用 Vector2.One 缩放
                // 原因：主游戏循环可能设置了错误的缩放值，导致动画缩放过小或过大而不可见
                // 解决：在绘制战法动画时临时重置缩放，绘制完成后恢复
                Vector2 originalScale = CacheManager.Scale;
                CacheManager.Scale = Vector2.One;
                
                try
                {
                    // 🔥 Anti-Band-Aid：直接访问，让 null 崩溃
                    // 如果 LinkedAnimation.Texture 为 null，说明 TileAnimationGenerator.AddTileAnimation 初始化错误
                    // 如果 LinkedAnimation.Texture.Texture 为 null，说明 Animation.Texture 属性的延迟加载失败
                    // 解决：追溯到数据源（AllTileAnimations 加载或 CacheManager.GetTempTexture）
                    CacheManager.Draw(this.LinkedAnimation.Texture.Texture, 
                        destination, 
                        sourceRect, 
                        Color.White, 
                        0f, 
                        Vector2.Zero, 
                        SpriteEffects.None, 
                        layerDepth);
                }
                finally
                {
                    // 🔥 恢复原始缩放值
                    CacheManager.Scale = originalScale;
                }
                
                if (endLoop)
                {
                    if (this.Looping)
                    {
                        this.currentFrameIndex = 0;
                        this.currentStayIndex = 0;
                    }
                    else
                    {
                        this.Drawing = false;
                        
                        #if DEBUG
                        /*
                        System.Diagnostics.Debug.WriteLine($"[TileAnimation.Draw] 动画结束 - Kind={this.Kind}, Position={this.Position}");
                        */
                        #endif
                    }
                }
            }
        }


        public override int GetHashCode()
        {
            return ((this.Position.ToString().GetHashCode() ^ this.Kind.ToString().GetHashCode()) ^ this.Looping.GetHashCode());
        }
    }
}

