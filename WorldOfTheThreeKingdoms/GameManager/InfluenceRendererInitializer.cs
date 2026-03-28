// ============================================================
// 文件: WorldOfTheThreeKingdoms/GameManager/InfluenceRendererInitializer.cs
// 创建日期: 2026-03-12
// 功能: 势力范围渲染器初始化器（资源加载与管线整合）
// ============================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 🎨 势力范围渲染器初始化器
    /// 🧊 Cold Path：只在游戏启动时调用一次
    /// </summary>
    public static class InfluenceRendererInitializer
    {
        /// <summary>
        /// 创建传统风格渲染器
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <returns>传统风格的 InfluenceRenderer</returns>
        public static InfluenceRenderer CreateTraditionalRenderer(GraphicsDevice graphicsDevice)
        {
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException(nameof(graphicsDevice));
            }
            
            // 创建 1x1 白色纹理（填充用）
            Texture2D pixelTexture = new(graphicsDevice, 1, 1);
            pixelTexture.SetData([Color.White]);
            
            // 加载 white_pixel 纹理（边界线用）
            // 注意：此处无 ContentManager，直接创建 1x1 白色纹理代替
            Texture2D whitePixel = new(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);
            
            return new InfluenceRenderer(pixelTexture, whitePixel);
        }
        
        /// <summary>
        /// 创建新工笔重彩风格渲染器
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <param name="content">ContentManager 实例</param>
        /// <param name="screenWidth">屏幕宽度</param>
        /// <param name="screenHeight">屏幕高度</param>
        /// <returns>新工笔重彩风格的 InfluenceRenderer</returns>
        public static InfluenceRenderer CreateInkBleedRenderer(
            GraphicsDevice graphicsDevice,
            ContentManager content,
            int screenWidth,
            int screenHeight)
        {
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException(nameof(graphicsDevice));
            }
            
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            
            // 🔥 步骤 1：创建 1x1 白色纹理（填充用）
            Texture2D pixelTexture = new(graphicsDevice, 1, 1);
            pixelTexture.SetData([Color.White]);
            
            // 🔥 步骤 1b：加载 white_pixel 纹理（边界线用）
            Texture2D whitePixel = content.Load<Texture2D>("Effects/white_pixel");
            
            // 🔥 步骤 2：加载 InkBleed Shader
            Effect inkBleedEffect;
            try
            {
                // 🎨 假设 Shader 文件位于 Content/Effects/InkBleed.fx
                // 编译后的文件为 InkBleed.mgfx（MonoGame 格式）
                inkBleedEffect = content.Load<Effect>("Effects/InkBleed");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "[InfluenceRendererInitializer] 无法加载 InkBleed Shader，" +
                    "请确保 Content/Effects/InkBleed.fx 已编译并添加到 Content.mgcb", ex);
            }
            
            // 🔥 步骤 3：加载宣纸噪点纹理
            Texture2D noiseTexture;
            try
            {
                // 🎨 假设噪点图位于 Content/Textures/XuanPaperNoise.png
                noiseTexture = content.Load<Texture2D>("Effects/XuanPaperNoise");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "[InfluenceRendererInitializer] 无法加载宣纸噪点纹理，" +
                    "请确保 Content/Effects/XuanPaperNoise.png 已添加到 Content.mgcb", ex);
            }
            
            // 🔥 步骤 4：创建 InkBleedRenderer
            InkBleedRenderer inkBleedRenderer = new(
                graphicsDevice,
                inkBleedEffect,
                noiseTexture,
                screenWidth,
                screenHeight);
            
            // 🔥 步骤 5：创建 InfluenceRenderer（新工笔重彩模式）
            return new InfluenceRenderer(pixelTexture, whitePixel, inkBleedRenderer);
        }
        
        /// <summary>
        /// 自动选择渲染器（根据配置或硬件能力）
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <param name="content">ContentManager 实例</param>
        /// <param name="screenWidth">屏幕宽度</param>
        /// <param name="screenHeight">屏幕高度</param>
        /// <param name="preferInkBleed">是否优先使用新工笔重彩风格</param>
        /// <returns>InfluenceRenderer 实例</returns>
        public static InfluenceRenderer CreateRenderer(
            GraphicsDevice graphicsDevice,
            ContentManager content,
            int screenWidth,
            int screenHeight,
            bool preferInkBleed = true)
        {
            if (!preferInkBleed)
            {
                return CreateTraditionalRenderer(graphicsDevice);
            }
            
            try
            {
                // 尝试创建新工笔重彩渲染器
                return CreateInkBleedRenderer(graphicsDevice, content, screenWidth, screenHeight);
            }
            catch (Exception ex)
            {
                // 如果失败，回退到传统渲染器
                System.Diagnostics.Debug.WriteLine(
                    $"[InfluenceRendererInitializer] 无法创建新工笔重彩渲染器，回退到传统模式：{ex.Message}");
                
                return CreateTraditionalRenderer(graphicsDevice);
            }
        }
    }
}
