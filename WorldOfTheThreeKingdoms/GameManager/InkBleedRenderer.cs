// ============================================================
// 文件: WorldOfTheThreeKingdoms/GameManager/InkBleedRenderer.cs
// 创建日期: 2026-03-12
// 功能: 新工笔重彩 Shader 渲染管线（双轨渲染）
// ============================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 🎨 新工笔重彩渲染器
    /// 🔥 Hot Path：每帧调用，零分配设计
    /// </summary>
    public sealed class InkBleedRenderer : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Effect _inkBleedEffect;
        private readonly Texture2D _noiseTexture;
        
        // 🔥 低分辨率 RenderTarget（1/4 屏幕尺寸）
        private RenderTarget2D _lowResTarget;
        
        // 🔥 预分配：全屏四边形顶点（避免每帧分配）
        private readonly VertexPositionTexture[] _fullScreenQuad;
        
        // 🔥 Shader 参数缓存（避免字符串查找）
        private readonly EffectParameter _paramSourceTexture;
        private readonly EffectParameter _paramNoiseTexture;
        private readonly EffectParameter _paramBleedIntensity;
        private readonly EffectParameter _paramNoiseScale;
        
        private bool _isDisposed = false;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <param name="inkBleedEffect">InkBleed.fx 着色器</param>
        /// <param name="noiseTexture">宣纸噪点纹理</param>
        /// <param name="screenWidth">屏幕宽度</param>
        /// <param name="screenHeight">屏幕高度</param>
        public InkBleedRenderer(
            GraphicsDevice graphicsDevice,
            Effect inkBleedEffect,
            Texture2D noiseTexture,
            int screenWidth,
            int screenHeight)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _inkBleedEffect = inkBleedEffect ?? throw new ArgumentNullException(nameof(inkBleedEffect));
            _noiseTexture = noiseTexture ?? throw new ArgumentNullException(nameof(noiseTexture));
            
            // 🔥 关键修复：检查 GraphicsDevice 是否已被释放
            // 日期：2026-03-26
            // 原因：RenderTarget2D 构造函数内部会访问 GraphicsDevice，如果设备已释放会抛出 NullReferenceException
            if (graphicsDevice.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(graphicsDevice),
                    "InkBleedRenderer: GraphicsDevice 已被释放，无法创建 RenderTarget2D");
            }
            
            // 🔥 验证屏幕尺寸
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                throw new ArgumentException(
                    $"InkBleedRenderer: 无效的屏幕尺寸 {screenWidth}×{screenHeight}");
            }
            
            // 🔥 极限降采样：1/16 尺寸，强化水墨糊化效果
            int lowResWidth = screenWidth / 8;
            int lowResHeight = screenHeight / 8;
            _lowResTarget = new RenderTarget2D(
                _graphicsDevice,
                lowResWidth,
                lowResHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);
            
            // 🔥 预分配全屏四边形（NDC 坐标）
            _fullScreenQuad = new VertexPositionTexture[4]
            {
                new(new Vector3(-1, 1, 0), new Vector2(0, 0)),   // 左上
                new(new Vector3(1, 1, 0), new Vector2(1, 0)),    // 右上
                new(new Vector3(-1, -1, 0), new Vector2(0, 1)),  // 左下
                new(new Vector3(1, -1, 0), new Vector2(1, 1))    // 右下
            };
            
            // 🔥 缓存 Shader 参数（避免每帧字符串查找）
            _paramSourceTexture = _inkBleedEffect.Parameters["SourceTexture"];
            _paramNoiseTexture = _inkBleedEffect.Parameters["NoiseTexture"];
            _paramBleedIntensity = _inkBleedEffect.Parameters["BleedIntensity"];
            _paramNoiseScale = _inkBleedEffect.Parameters["NoiseScale"];
            
            // 🔥 设置静态参数（只设置一次）
            _paramNoiseTexture?.SetValue(_noiseTexture);
            _paramBleedIntensity?.SetValue(0.6f);  // 晕染强度
            _paramNoiseScale?.SetValue(2.0f);      // 噪点缩放
        }
        
        /// <summary>
        /// 获取低分辨率 RenderTarget（用于 Pass 1 绘制）
        /// </summary>
        public RenderTarget2D LowResTarget => _lowResTarget;
        
        /// <summary>
        /// Pass 2：应用 Shader 并放大到全屏
        /// 🔥 Hot Path：每帧调用，零分配
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch 实例</param>
        public void ApplyShaderAndUpscale(SpriteBatch spriteBatch)
        {
            // 🔥 ANTI-BAND-AID：明确检查
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InkBleedRenderer));
            }
            
            // 🔥 切回主屏幕缓冲区
            _graphicsDevice.SetRenderTarget(null);
            
            // 🔥 设置 Shader 参数
            _paramSourceTexture?.SetValue(_lowResTarget);
            
            // 🔥 使用 LinearClamp 采样器（双线性过滤 + 柔和化）
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,  // 🎨 关键：双线性过滤自动柔和化
                DepthStencilState.None,
                RasterizerState.CullNone,
                _inkBleedEffect);
            
            // 🔥 绘制全屏四边形
            spriteBatch.Draw(
                _lowResTarget,
                new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height),
                Color.White);
            
            spriteBatch.End();
        }
        
        /// <summary>
        /// 屏幕尺寸变化时重建 RenderTarget
        /// </summary>
        public void OnScreenResize(int newWidth, int newHeight)
        {
            _lowResTarget?.Dispose();
            
            int lowResWidth = newWidth / 8;
            int lowResHeight = newHeight / 8;
            _lowResTarget = new RenderTarget2D(
                _graphicsDevice,
                lowResWidth,
                lowResHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _lowResTarget?.Dispose();
            _isDisposed = true;
        }
    }
}
