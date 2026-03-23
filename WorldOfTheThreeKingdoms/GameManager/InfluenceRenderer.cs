// ============================================================
// 文件: WorldOfTheThreeKingdoms/GameManager/InfluenceRenderer.cs
// 创建日期: 2026-03-11
// 修复日期: 2026-03-12
// 功能: 势力范围渲染器（新工笔重彩版本 + 战争迷雾整合）
// ============================================================

#nullable disable

using System;
using GameObjects;
using GameObjects.FactionDetail;
using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 🎨 势力范围渲染器（新工笔重彩风格）
    /// 🔥 Hot Path：在 Draw() 循环中每帧调用
    /// 🆕 整合：战争迷雾 + 能量视野 + 双轨渲染管线
    /// </summary>
    public class InfluenceRenderer : IDisposable
    {
        private readonly Texture2D _pixelTexture;
        private readonly Texture2D _whitePixel;   // 🆕 1x1 纯白纹理，用于绘制边界线
        private readonly InkBleedRenderer? _inkBleedRenderer;
        
        private bool _isEnabled = true;
        private bool _useInkBleedStyle = true;  // 🎨 是否启用新工笔重彩风格
        private bool _isDisposed = false;
        
        // 🆕 边界线配置
        private const int BorderThickness = 3;  // 边界线宽（像素）
        
#if DEBUG
        // 🆕 调试：帧计数器（用于控制日志频率）
        private int _debugFrameCounter = 0;
#endif
        
        /// <summary>
        /// 构造函数（传统模式）
        /// </summary>
        public InfluenceRenderer(Texture2D pixelTexture, Texture2D whitePixel)
        {
            _pixelTexture = pixelTexture 
                ?? throw new ArgumentNullException(nameof(pixelTexture),
                    "[InfluenceRenderer] pixelTexture 不能为 null");
            _whitePixel = whitePixel
                ?? throw new ArgumentNullException(nameof(whitePixel),
                    "[InfluenceRenderer] whitePixel 不能为 null");
            _inkBleedRenderer = null;
            _useInkBleedStyle = false;
        }
        
        /// <summary>
        /// 构造函数（新工笔重彩模式）
        /// </summary>
        public InfluenceRenderer(Texture2D pixelTexture, Texture2D whitePixel, InkBleedRenderer inkBleedRenderer)
        {
            _pixelTexture = pixelTexture 
                ?? throw new ArgumentNullException(nameof(pixelTexture));
            _whitePixel = whitePixel
                ?? throw new ArgumentNullException(nameof(whitePixel));
            _inkBleedRenderer = inkBleedRenderer 
                ?? throw new ArgumentNullException(nameof(inkBleedRenderer));
            _useInkBleedStyle = true;
        }
        
        /// <summary>
        /// 是否启用渲染（可通过 F12 切换）
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
        
        /// <summary>
        /// 是否启用新工笔重彩风格
        /// </summary>
        public bool UseInkBleedStyle
        {
            get => _useInkBleedStyle && _inkBleedRenderer != null;
            set => _useInkBleedStyle = value;
        }
        
        /// <summary>
        /// 绘制势力范围（Hot Path - 每帧调用）
        /// 🔥 Zero Overdraw 优化：每个格子只绘制一次
        /// 🆕 整合：战争迷雾 + 能量视野 + 双轨渲染管线
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch 实例</param>
        /// <param name="viewport">当前视口（Grid坐标）</param>
        /// <param name="tileSize">瓦片大小（像素）</param>
        /// <param name="leftEdge">屏幕左边缘偏移（像素）</param>
        /// <param name="topEdge">屏幕上边缘偏移（像素）</param>
        public void Draw(SpriteBatch spriteBatch, Rectangle viewport, int tileSize, int leftEdge, int topEdge)
        {
            // 🔥 Hot Path：快速返回
            if (!_isEnabled) return;
            
            var config = GameData.InfluenceConfig.Current;
            if (!config.EnableRendering) return;
            
            // 🔥 ANTI-BAND-AID：分开检查，明确记录错误源
            if (Session.Current == null)
            {
                // 游戏未初始化，这是预期行为（启动阶段）
                return;
            }
            
            if (Session.Current.Scenario == null)
            {
                // Session 存在但 Scenario 为 null，这是数据错误
                throw new InvalidOperationException(
                    "[InfluenceRenderer] Session.Current 存在但 Scenario 为 null，数据未正确初始化");
            }
            
            var scenario = Session.Current.Scenario;
            
            // 🔥 ANTI-BAND-AID：明确检查 Factions
            if (scenario.Factions == null)
            {
                throw new InvalidOperationException(
                    "[InfluenceRenderer] Scenario.Factions 为 null，数据未正确初始化");
            }
            
            // 🎨 获取当前玩家（用于战争迷雾判定）
            Faction currentPlayer = scenario.CurrentPlayer;
            bool isSkyEyeMode = Session.GlobalVariables.SkyEye || currentPlayer == null;
            
#if DEBUG
            int totalPixelsDrawn = 0;
            int foggedPixels = 0;
            bool shouldLog = (_debugFrameCounter++ % 60 == 0);
#endif
            
            // 🎨 Pass 1：如果启用新工笔重彩，切换到低分辨率 RenderTarget
            if (_useInkBleedStyle && _inkBleedRenderer != null)
            {
                spriteBatch.End();  // 结束当前批次
                spriteBatch.GraphicsDevice.SetRenderTarget(_inkBleedRenderer.LowResTarget);
                spriteBatch.GraphicsDevice.Clear(Color.Transparent);
                
                // 🔥 使用 NonPremultiplied 混合模式，防止深色发灰
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
            }
            
            // 🔥 Zero Overdraw 优化：按格子遍历，每个格子只绘制一次
            // 计算视口范围（Grid坐标）
            int startX = viewport.X;
            int endX = viewport.X + viewport.Width;
            int startY = viewport.Y;
            int endY = viewport.Y + viewport.Height;
            
            int mapWidth = TerrainCostCache.MapWidth;
            int mapHeight = TerrainCostCache.MapHeight;
            
            // 边界检查
            startX = Math.Max(0, startX);
            endX = Math.Min(mapWidth - 1, endX);
            startY = Math.Max(0, startY);
            endY = Math.Min(mapHeight - 1, endY);
            
            // 🔥 Hot Path 优化：直接访问内部 List，避免 GetList() 创建新对象
            var factions = scenario.Factions.GameObjects;
            int factionCount = factions.Count;
            
            // 🔥 遍历视口内的每个格子
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    // 🆕 战争迷雾判定：有能量的地方都开视野，只是情报等级不同
                    // --- 第一步：战争迷雾判定（玩家视野检查）---
                    // 🎯 核心逻辑：
                    // 1. 玩家能量 > 0 → 有实时视野 → 直接渲染（不检查情报）
                    // 2. 玩家能量 = 0 但有情报 → 有历史视野 → 渲染
                    // 3. 玩家能量 = 0 且无情报 → 无视野 → 不渲染
                    if (!isSkyEyeMode && currentPlayer != null)
                    {
                        int index = y * mapWidth + x;
                        
                        // 🔥 ANTI-BAND-AID：明确检查 GlobalInfluenceMap
                        if (currentPlayer.GlobalInfluenceMap == null)
                        {
                            throw new InvalidOperationException(
                                $"[InfluenceRenderer] 当前玩家 {currentPlayer.Name} 的 GlobalInfluenceMap 为 null，" +
                                $"数据未正确初始化，请检查 InfluenceUpdateManager");
                        }
                        
                        // 🔥 关键：玩家能量 > 0 直接渲染，不检查情报
                        // 🔥 日期：2026-03-16
                        // 🔥 重构：使用 EffectiveTotalEnergy
                        int playerEnergy = currentPlayer.GlobalInfluenceMap[index].EffectiveTotalEnergy;
                        
                        // 🔥 关键：玩家能量 > 0 直接渲染，不检查情报
                        if (playerEnergy > 0)
                        {
                            // 玩家有能量，直接渲染（不执行 continue）
                        }
                        else
                        {
                            // 玩家没有能量，检查是否有历史情报
                            Point tilePos = new(x, y);
                            InformationLevel playerInfoLevel = currentPlayer.GetKnownAreaDataNoCheck(tilePos);
                            
                            // 情报等级：未知(0) 无(1) 低(2) 中(3) 高(4) 全(5)
                            // 只有"低"及以上才显示
                            if (playerInfoLevel < InformationLevel.低)
                            {
                                // 没有情报，不渲染
#if DEBUG
                                foggedPixels++;
#endif
                                continue;
                            }
                        }
                        // 玩家有能量或有情报，继续渲染
                    }
                    
                    // --- 第二步：找出这个格子的最强势力（O(1) 查表）---
                    Faction dominantFaction = null;
                    int maxEnergy = 0;
                    
                    for (int f = 0; f < factionCount; f++)
                    {
                        // 🔥 ANTI-BAND-AID：不使用防御性空检查
                        if (factions[f] is not Faction factionObj)
                        {
                            throw new InvalidOperationException(
                                $"[InfluenceRenderer] Scenario.Factions 包含无效项（索引 {f}），" +
                                $"数据未正确初始化");
                        }
                        
                        // 🔥 ANTI-BAND-AID：GlobalInfluenceMap 应该在势力初始化时创建
                        if (factionObj.GlobalInfluenceMap == null)
                        {
                            throw new InvalidOperationException(
                                $"[InfluenceRenderer] 势力 {factionObj.Name} 的 GlobalInfluenceMap 为 null，" +
                                $"数据未正确初始化，请检查 InfluenceUpdateManager");
                        }
                        
                        if (factionObj.GlobalInfluenceMap.Length == 0)
                        {
                            throw new InvalidOperationException(
                                $"[InfluenceRenderer] 势力 {factionObj.Name} 的 GlobalInfluenceMap 长度为 0，" +
                                $"数据未正确初始化，请检查 MultiSourceInfluenceCalculator");
                        }
                        
                        // 🔥 O(1) 查表：读取这个格子的能量值
                        // 🔥 日期：2026-03-16
                        // 🔥 重构：使用 EffectiveTotalEnergy（城池+部队叠加）
                        int index = y * mapWidth + x;
                        int energy = factionObj.GlobalInfluenceMap[index].EffectiveTotalEnergy;
                        
                        // 找出能量最高的势力
                        if (energy > maxEnergy)
                        {
                            maxEnergy = energy;
                            dominantFaction = factionObj;
                        }
                    }
                    
                    // --- 第三步：绘制最强势力 ---
                    if (dominantFaction != null && maxEnergy > 0)
                    {
                        // 计算屏幕坐标
                        int screenX = leftEdge + x * tileSize;
                        int screenY = topEdge + y * tileSize;
                        
                        // 🎨 强制提亮 + 硬边框策略（解决深色阵营隐形问题）
                        Color baseColor = dominantFaction.FactionColor;
                        Color renderColor;
                        
                        if (_useInkBleedStyle && _inkBleedRenderer != null)
                        {
                            if (maxEnergy >= 400f)
                            {
                                // 核心区：提亮 30%，低透明度（内部填充）
                                int r = baseColor.R + (int)((255 - baseColor.R) * 0.30f);
                                int g = baseColor.G + (int)((255 - baseColor.G) * 0.30f);
                                int b = baseColor.B + (int)((255 - baseColor.B) * 0.30f);
                                renderColor = new Color(r, g, b, (byte)(255 * 0.25f));
                            }
                            else if (maxEnergy >= 150f)
                            {
                                // 控制区：提亮 30%，低透明度
                                int r = baseColor.R + (int)((255 - baseColor.R) * 0.30f);
                                int g = baseColor.G + (int)((255 - baseColor.G) * 0.30f);
                                int b = baseColor.B + (int)((255 - baseColor.B) * 0.30f);
                                renderColor = new Color(r, g, b, (byte)(255 * 0.20f));
                            }
                            else if (maxEnergy > 5f)
                            {
                                // 边缘区：极低透明度
                                renderColor = new Color(
                                    baseColor.R,
                                    baseColor.G,
                                    baseColor.B,
                                    (byte)(255 * 0.12f));
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // 传统风格：简单透明度映射
                            float alpha = maxEnergy >= 500 ? 0.25f : 
                                          maxEnergy >= 200 ? 0.18f : 0.10f;
                            renderColor = new Color(
                                (int)(baseColor.R * alpha),
                                (int)(baseColor.G * alpha),
                                (int)(baseColor.B * alpha),
                                (int)(255 * alpha));
                        }
                        
                        // Pass 1：绘制内部半透明填充
                        spriteBatch.Draw(_pixelTexture, 
                            new Rectangle(screenX, screenY, tileSize, tileSize), 
                            renderColor);
                        
                        // Pass 2：四向边缘检测，绘制硬边界线
                        // 🔥 Hot Path：值类型栈分配，Zero-Allocation
                        // 边框颜色 = 势力纯色，100% 不透明
                        Color borderColor = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)255);
                        
                        // 上方
                        if (GetDominantFaction(x, y - 1, mapWidth, mapHeight, factions, factionCount) != dominantFaction)
                        {
                            spriteBatch.Draw(_whitePixel,
                                new Rectangle(screenX, screenY, tileSize, BorderThickness),
                                borderColor);
                        }
                        // 下方
                        if (GetDominantFaction(x, y + 1, mapWidth, mapHeight, factions, factionCount) != dominantFaction)
                        {
                            spriteBatch.Draw(_whitePixel,
                                new Rectangle(screenX, screenY + tileSize - BorderThickness, tileSize, BorderThickness),
                                borderColor);
                        }
                        // 左侧
                        if (GetDominantFaction(x - 1, y, mapWidth, mapHeight, factions, factionCount) != dominantFaction)
                        {
                            spriteBatch.Draw(_whitePixel,
                                new Rectangle(screenX, screenY, BorderThickness, tileSize),
                                borderColor);
                        }
                        // 右侧
                        if (GetDominantFaction(x + 1, y, mapWidth, mapHeight, factions, factionCount) != dominantFaction)
                        {
                            spriteBatch.Draw(_whitePixel,
                                new Rectangle(screenX + tileSize - BorderThickness, screenY, BorderThickness, tileSize),
                                borderColor);
                        }
                        
#if DEBUG
                        totalPixelsDrawn++;
#endif
                    }
                }
            }
            
            // 🎨 Pass 2：如果启用新工笔重彩，应用 Shader 并放大到全屏
            if (_useInkBleedStyle && _inkBleedRenderer != null)
            {
                spriteBatch.End();
                _inkBleedRenderer.ApplyShaderAndUpscale(spriteBatch);
                
                // 恢复原始渲染状态
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
        }
        
        /// <summary>
        /// 🔥 Hot Path：O(1) 查表，获取指定格子的控制势力
        /// 地图边界外视为无势力（null），触发边界线绘制
        /// </summary>
        private static Faction? GetDominantFaction(
            int x, int y,
            int mapWidth, int mapHeight,
            System.Collections.Generic.List<GameObject> factions, int factionCount)
        {
            // 地图边界外 → 无势力，触发边界线
            if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                return null;
            
            int index = y * mapWidth + x;
            Faction? dominant = null;
            int maxEnergy = 0;
            
            for (int f = 0; f < factionCount; f++)
            {
                var faction = (Faction)factions[f];
                // 🔥 日期：2026-03-16
                // 🔥 重构：使用 EffectiveTotalEnergy
                int energy = faction.GlobalInfluenceMap[index].EffectiveTotalEnergy;
                if (energy > maxEnergy)
                {
                    maxEnergy = energy;
                    dominant = faction;
                }
            }
            
            return maxEnergy > 0 ? dominant : null;
        }
        
        /// <summary>
        /// 切换渲染开关
        /// </summary>
        public void Toggle()
        {
            _isEnabled = !_isEnabled;
            System.Diagnostics.Debug.WriteLine(
                $"[InfluenceRenderer] 势力范围渲染已{(_isEnabled ? "启用" : "禁用")}");
        }
        
        /// <summary>
        /// 切换渲染风格
        /// </summary>
        public void ToggleStyle()
        {
            if (_inkBleedRenderer == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[InfluenceRenderer] 无法切换风格：未初始化 InkBleedRenderer");
                return;
            }
            
            _useInkBleedStyle = !_useInkBleedStyle;
            System.Diagnostics.Debug.WriteLine(
                $"[InfluenceRenderer] 渲染风格已切换为：{(_useInkBleedStyle ? "新工笔重彩" : "传统")}");
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _inkBleedRenderer?.Dispose();
            _isDisposed = true;
        }
    }
}
