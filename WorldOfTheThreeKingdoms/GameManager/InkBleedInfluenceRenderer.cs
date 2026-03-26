// ============================================================
// 文件: WorldOfTheThreeKingdoms/GameManager/InkBleedInfluenceRenderer.cs
// 创建日期: 2026-03-13
// 功能: 新工笔重彩势力范围渲染器（水墨晕染风格）
// ============================================================

#nullable disable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameObjects.FactionDetail;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager;

/// <summary>
/// 🎨 新工笔重彩势力范围渲染器
/// 🔥 Hot Path：Draw() 方法每帧调用，严格 Zero-Allocation
/// </summary>
public sealed class InkBleedInfluenceRenderer : IDisposable
{
    // 🔥 双轨数据流
    private readonly int[] _targetInfluenceMap;    // 逻辑目标层（势力ID）
    private readonly float[] _visualInfluenceMap;  // 视觉过渡层（透明度 0-1）
    
    // 🔥 势力颜色缓存（Cold Path 更新，Hot Path 直接按 ID 索引，避免每帧字典查找）
    // 索引 = 势力 ID，值 = 势力颜色（ID=0 是有效势力，必须支持）
    private Color[] _factionColorCache = [];
    
    private readonly int _mapWidth;
    private readonly int _mapHeight;
    
    // 🔥 低分屏画布（1/2 分辨率）
    private RenderTarget2D _lowResTarget;
    private readonly GraphicsDevice _graphicsDevice;
    
    // 🔥 低分屏缩放比例缓存（分辨率变化时在 RebuildRenderTarget 中更新，避免每帧重算）
    private float _scaleX;
    private float _scaleY;
    private int _lowResTileW;   // 预计算的低分屏格子宽（依赖 tileSize，UpdateRenderTarget 传入时更新）
    private int _lowResTileH;
    private int _cachedTileSize = -1;  // 上次计算 _lowResTileW/H 时的 tileSize
    
    // 🔥 水墨侵蚀着色器
    private readonly Effect _inkBleedEffect;
    private readonly Texture2D _noiseTexture;
    
    // 🔥 像素纹理（用于绘制纯色方块）
    private readonly Texture2D _pixelTexture;
    
    // 🔥 地形遮罩纹理（用于区分水域/陆地的水墨效果）
    // 日期：2026-03-17
    private Texture2D? _terrainMask;
    
    // 🔥 平滑插值速度
    private const float LERP_SPEED = 0.15f;
    
    /// <summary>
    /// 是否启用水墨渲染（默认开启，F12 切换）
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    public InkBleedInfluenceRenderer(
        GraphicsDevice graphicsDevice,
        int mapWidth,
        int mapHeight,
        Effect inkBleedEffect,
        Texture2D noiseTexture)
    {
        // 🔥 ANTI-BAND-AID：明确的数据源检查，不是防御性空检查
        if (graphicsDevice == null)
        {
            throw new ArgumentNullException(nameof(graphicsDevice),
                "InkBleedInfluenceRenderer: GraphicsDevice 未初始化，请检查 MainGameScreen.LoadContent()");
        }
        
        if (inkBleedEffect == null)
        {
            throw new ArgumentNullException(nameof(inkBleedEffect),
                "InkBleedInfluenceRenderer: InkBleed.fx 着色器未加载，请检查 Content.Load<Effect>(\"InkBleed\")");
        }
        
        if (noiseTexture == null)
        {
            throw new ArgumentNullException(nameof(noiseTexture),
                "InkBleedInfluenceRenderer: XuanPaperNoise.png 纹理未加载，请检查 Content.Load<Texture2D>(\"XuanPaperNoise\")");
        }
        
        _graphicsDevice = graphicsDevice;
        _inkBleedEffect = inkBleedEffect;
        _noiseTexture = noiseTexture;
        
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        
        int totalTiles = mapWidth * mapHeight;
        _targetInfluenceMap = new int[totalTiles];
        _visualInfluenceMap = new float[totalTiles];
        
        // 🔥 初始化为 -1（无主）
        Array.Fill(_targetInfluenceMap, -1);
        // 🔥 视觉层也初始化为 0（无势力时透明）
        Array.Fill(_visualInfluenceMap, 0f);
        
        // 🔥 创建低分屏画布（1/4 分辨率）
        int screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        _lowResTarget = new RenderTarget2D(
            graphicsDevice,
            screenWidth / 2,
            screenHeight / 2,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
        
        // 🔥 初始化低分屏缩放比例缓存
        _scaleX = (float)_lowResTarget.Width / screenWidth;
        _scaleY = (float)_lowResTarget.Height / screenHeight;
        
        // 🔥 创建 1x1 像素纹理
        _pixelTexture = new(graphicsDevice, 1, 1);  // ✅ C# 12 目标类型推断
        _pixelTexture.SetData([Color.White]);  // ✅ C# 12 集合表达式
        
        // 🧊 Cold Path：设置着色器常量参数（只在初始化时设置一次）
        // 🔥 性能优化：避免在 Hot Path（DrawOverlay）中每帧重复设置相同的常量
        _inkBleedEffect.Parameters["NoiseTexture"]?.SetValue(_noiseTexture);
        _inkBleedEffect.Parameters["NoiseScale"]?.SetValue(5.0f);
        _inkBleedEffect.Parameters["EdgeThreshold"]?.SetValue(0.2f);
        _inkBleedEffect.Parameters["InkSpread"]?.SetValue(0.15f);
        
        System.Diagnostics.Debug.WriteLine(
            $"[InkBleedInfluenceRenderer] 初始化完成：地图 {mapWidth}×{mapHeight}，低分屏 {_lowResTarget.Width}×{_lowResTarget.Height}");
    }
    
    /// <summary>
    /// 🧊 Cold Path：根据 zhsan 当前地图数据动态生成水墨晕染遮罩
    /// 日期：2026-03-17
    /// 调用时机：在 UpdateInfluenceMap() 之后调用一次
    /// </summary>
    public void GenerateTerrainMask(GameScenario scenario)
    {
        // 🔥 参数验证（ANTI-BAND-AID 协议：分别检查，不使用 ?.）
        if (scenario == null)
        {
            throw new ArgumentNullException(nameof(scenario), "剧本数据为 null");
        }
        
        if (scenario.ScenarioMap == null)
        {
            throw new InvalidOperationException($"剧本 {scenario.Title} 的地图数据未初始化");
        }

        int mapWidth = scenario.ScenarioMap.MapDimensions.X;
        int mapHeight = scenario.ScenarioMap.MapDimensions.Y;

        // 🔥 验证地图尺寸与渲染器一致
        if (mapWidth != _mapWidth || mapHeight != _mapHeight)
        {
            throw new InvalidOperationException(
                $"地图尺寸不匹配：剧本地图 {mapWidth}×{mapHeight}，渲染器地图 {_mapWidth}×{_mapHeight}");
        }

        // 1. 创建极小尺寸的纹理 (1个像素代表1个地图格子)
        Texture2D maskTexture = new(_graphicsDevice, mapWidth, mapHeight);

        // 2. 准备颜色数组 (一维数组，AOT 友好，连续内存)
        Color[] maskData = new Color[mapWidth * mapHeight];

        // 3. 遍历大地图数据进行像素填充
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int index = y * mapWidth + x;
                Point position = new(x, y);

                // 🔥 使用 zhsan 实际的地形查询 API
                TerrainKind terrain = scenario.GetTerrainKindByPositionNoCheck(position);

                // 🔥 地形分类：水域 vs 陆地
                // 水域填纯白 (晕染最大化)，陆地填纯黑 (边缘硬朗)
                // 湿地作为过渡态填灰色
                maskData[index] = terrain switch
                {
                    TerrainKind.水域 => Color.White,              // 纯水域：最大晕染
                    TerrainKind.湿地 => new Color(128, 128, 128), // 沼泽/浅滩：过渡态
                    _ => Color.Black                              // 陆地：硬边缘
                };
            }
        }

        // 4. 将数据一次性写入 GPU
        maskTexture.SetData(maskData);

        // 5. 释放旧纹理并更新引用
        _terrainMask?.Dispose();
        _terrainMask = maskTexture;
        
        // 6. 设置着色器参数
        _inkBleedEffect.Parameters["MaskTexture"]?.SetValue(_terrainMask);
        
        System.Diagnostics.Debug.WriteLine(
            $"[InkBleedInfluenceRenderer] ✅ 地形遮罩生成完成：{mapWidth}×{mapHeight}");
    }
    
    /// <summary>
    /// 🔥 Hot Path：更新逻辑目标层
    /// 每回合调用一次，重新计算势力归属
    /// </summary>
    public void UpdateInfluenceMap()
    {
        // 🔥 清空旧数据
        Array.Fill(_targetInfluenceMap, -1);
        
        // 🔥 ANTI-BAND-AID：明确检查数据源
        var scenario = global::GameManager.Session.Current.Scenario;
        if (scenario == null)
        {
            throw new InvalidOperationException(
                "InkBleedInfluenceRenderer.UpdateInfluenceMap: Session.Current.Scenario 为 null，请检查游戏初始化流程");
        }
        
        if (scenario.Factions == null)
        {
            throw new InvalidOperationException(
                "InkBleedInfluenceRenderer.UpdateInfluenceMap: Scenario.Factions 为 null，请检查剧本加载");
        }
        
        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;
        
        // 🔥 初始化顺序容错：GlobalInfluenceMap 在 InfluenceUpdateManager.Initialize() 中初始化
        // 日期：2026-03-18
        // 场景：UpdateInfluenceMap() 可能在 GlobalInfluenceMap 初始化前被调用
        // 解决：检查所有势力的 GlobalInfluenceMap，如果任何一个未初始化则跳过更新
        int expectedLength = _mapWidth * _mapHeight;
        for (int fIdx = 0; fIdx < factionCount; fIdx++)
        {
            if (factions[fIdx] is not Faction f) continue;
            
            if (f.GlobalInfluenceMap == null || f.GlobalInfluenceMap.Length == 0)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[InkBleedInfluenceRenderer] ⏸️ 势力 {f.Name} 的 GlobalInfluenceMap 未初始化，" +
                    $"跳过更新（等待 InfluenceUpdateManager.Initialize() 完成）");
                #endif
                return;  // 🔥 容错：跳过更新，不抛出异常
            }
            
            if (f.GlobalInfluenceMap.Length != expectedLength)
            {
                // 🔥 ANTI-BAND-AID：地图尺寸不一致是数据损坏，必须 Fail Fast
                throw new InvalidOperationException(
                    $"InkBleedInfluenceRenderer: 势力 {f.Name} 的 GlobalInfluenceMap 长度={f.GlobalInfluenceMap.Length}，" +
                    $"与渲染器地图尺寸 {_mapWidth}×{_mapHeight}={expectedLength} 不一致，" +
                    $"请检查 TerrainCostCache.Initialize 是否在渲染器构造前调用");
            }
        }
        
        // 🔥 遍历全图，查询每个地块的势力归属
        for (int i = 0; i < _targetInfluenceMap.Length; i++)
        {
            int x = i % _mapWidth;
            int y = i / _mapWidth;
            
            // 🔥 使用与 GlobalInfluenceMap 一致的索引方式
            int mapIndex = WorldOfTheThreeKingdoms.GameManager.TerrainCostCache.GetIndex(x, y);
            
            // 🔥 查询能量最高的势力
            int maxEnergy = 0;
            int ownerFactionID = -1;
            
            for (int fIdx = 0; fIdx < factionCount; fIdx++)
            {
                if (factions[fIdx] is not Faction faction) continue;
                
                // 🔥 日期：2026-03-16
                // 🔥 重构：使用 EffectiveTotalEnergy
                // 🆕 日期：2026-03-21
                // 🆕 包含残留能量（视觉效果：残留能量显示为半透明）
                int energy = faction.GlobalInfluenceMap[mapIndex].EffectiveTotalEnergy;
                
                // 🆕 残留能量单独计算（用于半透明显示）
                int residualEnergy = faction.GlobalInfluenceMap[mapIndex].ResidualEnergy;
                if (residualEnergy > 0 && energy == 0)
                {
                    // 只有残留能量，没有活跃能量：显示为半透明
                    energy = residualEnergy / 2;  // 残留能量效果减半
                }
                
                if (energy > maxEnergy)
                {
                    maxEnergy = energy;
                    ownerFactionID = faction.ID;
                }
            }
            
            _targetInfluenceMap[i] = ownerFactionID;
        }
        // 🧊 Cold Path：重建颜色缓存（每回合一次）
        // 🔥 关键：ID=0 是有效势力，数组大小必须覆盖所有 ID
        int maxID = -1;
        for (int fIdx = 0; fIdx < factionCount; fIdx++)
        {
            if (factions[fIdx] is Faction f && f.ID > maxID)
                maxID = f.ID;
        }
        
        if (maxID >= 0)
        {
            _factionColorCache = new Color[maxID + 1];
            for (int fIdx = 0; fIdx < factionCount; fIdx++)
            {
                if (factions[fIdx] is Faction f)
                    _factionColorCache[f.ID] = f.FactionColor;  // 🔥 ID=0 直接写入索引 0
            }
        }
        
        // 🔥 修复延迟显示：同步初始化 _visualInfluenceMap 为目标值
        // 日期：2026-03-20
        // 原因：_visualInfluenceMap 初始化为 0，通过 LERP 慢慢爬升到 1.0f，需要约 30 帧才能达到可见阈值
        // 解决：在 UpdateInfluenceMap() 中同步初始化 _visualInfluenceMap，立即显示水墨效果
        // 发现：存档操作后水墨渲染立即显示，因为 freeTilesMemory() → InitializeInkBleedRenderer() → UpdateInfluenceMap()
        // 🧊 Cold Path：使用 Span 保持与 Update() 方法的一致性
        Span<float> visual = _visualInfluenceMap.AsSpan();
        ReadOnlySpan<int> target = _targetInfluenceMap.AsSpan();
        for (int i = 0; i < visual.Length; i++)
        {
            visual[i] = target[i] >= 0 ? 1.0f : 0.0f;
        }
    }
    
    /// <summary>
    /// 🔥 Hot Path：平滑插值（每帧调用）
    /// </summary>
    public void Update()
    {
        // 🔥 使用 Span 遍历，Zero-Allocation
        Span<float> visual = _visualInfluenceMap.AsSpan();
        ReadOnlySpan<int> target = _targetInfluenceMap.AsSpan();
        
        for (int i = 0; i < visual.Length; i++)
        {
            float targetAlpha = target[i] >= 0 ? 1.0f : 0.0f;
            visual[i] += (targetAlpha - visual[i]) * LERP_SPEED;
            
            // 🔥 阈值裁剪（避免无限逼近）
            if (Math.Abs(visual[i] - targetAlpha) < 0.01f)
            {
                visual[i] = targetAlpha;
            }
        }
    }
    
    /// <summary>
    /// 🔥 Hot Path：Pre-pass，在主画面渲染之前调用
    /// 将势力范围绘制到低分屏 RenderTarget
    /// 注意：此方法会暂时中断外层 SpriteBatch，完成后会恢复
    /// </summary>
    public void UpdateRenderTarget(
        SpriteBatch spriteBatch,
        Rectangle viewport,
        int tileSize,
        int leftEdge,
        int topEdge)
    {
        // 🔥 F12 关闭时直接返回
        if (!IsEnabled) return;
        
        var scenario = global::GameManager.Session.Current.Scenario;
        if (scenario == null)
            throw new InvalidOperationException("InkBleedInfluenceRenderer.UpdateRenderTarget: Session.Current.Scenario 为 null");
        
        var currentPlayer = scenario.CurrentPlayer;
        bool isSkyEyeMode = global::GameManager.Session.GlobalVariables.SkyEye || currentPlayer == null;
        
        // ============================================================
        // 🔥 关键：先结束外层的 SpriteBatch
        // ============================================================
        spriteBatch.End();
        
        // 保存当前 RenderTarget
        var previousRenderTarget = _graphicsDevice.GetRenderTargets();
        
        // 切换到低分屏 RenderTarget
        _graphicsDevice.SetRenderTarget(_lowResTarget);
        _graphicsDevice.Clear(Color.Transparent);
        
        // 使用独立的 SpriteBatch 批次
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null, null, null, null);
        
        int startX = Math.Max(0, viewport.Left);
        int startY = Math.Max(0, viewport.Top);
        int endX = Math.Min(_mapWidth, viewport.Right + 1);
        int endY = Math.Min(_mapHeight, viewport.Bottom + 1);
        
        // 🔥 低分屏缩放比例从缓存字段读取（在构造函数和 RebuildRenderTarget 中更新）
        // tileSize 变化时（缩放地图）才重算格子尺寸
        if (tileSize != _cachedTileSize)
        {
            _lowResTileW = Math.Max(1, (int)(tileSize * _scaleX));
            _lowResTileH = Math.Max(1, (int)(tileSize * _scaleY));
            _cachedTileSize = tileSize;
        }
        
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int index = y * _mapWidth + x;
                
                if (!isSkyEyeMode)
                {
                    var infoLevel = currentPlayer.GetInformationLevel(new Point(x, y));
                    if (infoLevel == InformationLevel.无) continue;
                }
                
                int factionID = _targetInfluenceMap[index];
                if (factionID < 0) continue;  // 🔥 ID < 0 无势力，ID=0 是有效势力
                
                // 🔥 Hot Path：直接按索引取缓存颜色，Zero-Allocation，无字典查找
                if (factionID >= _factionColorCache.Length)
                    throw new InvalidOperationException($"InkBleedInfluenceRenderer: 势力 ID={factionID} 超出颜色缓存范围，请检查 UpdateInfluenceMap 是否已调用");
                
                float alpha = _visualInfluenceMap[index];
                if (alpha < 0.01f) continue;
                
                // 🔥 先算全分辨率屏幕坐标，再整体乘以缩放比例映射到低分屏
                int screenX = x * tileSize + leftEdge;
                int screenY = y * tileSize + topEdge;
                int lowResX = (int)(screenX * _scaleX);
                int lowResY = (int)(screenY * _scaleY);
                
                Color color = _factionColorCache[factionID] * (alpha * 0.4f);
                spriteBatch.Draw(
                    _pixelTexture,
                    new Rectangle(lowResX, lowResY, _lowResTileW, _lowResTileH),
                    color);
            }
        }
        
        spriteBatch.End();
        
        // 恢复之前的 RenderTarget
        if (previousRenderTarget.Length > 0)
        {
            _graphicsDevice.SetRenderTargets(previousRenderTarget);
        }
        else
        {
            _graphicsDevice.SetRenderTarget(null);
        }
        
        // ============================================================
        // 🔥 关键：恢复外层的 SpriteBatch 状态
        // ============================================================
        spriteBatch.Begin(
            SpriteSortMode.BackToFront,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            null, null, null, null);
    }
    
    /// <summary>
    /// 🔥 Hot Path：Post-Overlay，在主画面渲染之后调用
    /// 将低分屏内容叠加到已完成的主画面上
    /// 注意：此方法会结束外层 SpriteBatch，调用后需要重新 Begin
    /// </summary>
    /// <summary>
    /// 🔥 绘制水墨叠加效果到屏幕
    /// 注意：此方法会结束外层 SpriteBatch，调用后需要重新 Begin
    /// </summary>
    public void DrawOverlay(SpriteBatch spriteBatch, GameTime gameTime)
    {
        // 🔥 F12 关闭时直接返回
        if (!IsEnabled) return;

        // 🔥 每帧更新动态着色器参数
        // 日期：2026-03-16
        // 原因：支持窗口大小动态改变和时间动画效果

        // 1. 视口大小（支持窗口缩放）
        Viewport viewport = _graphicsDevice.Viewport;
        Vector2 screenSize = new(viewport.Width, viewport.Height);
        _inkBleedEffect.Parameters["ViewportSize"]?.SetValue(screenSize);

        // 2. 游戏运行时间（用于动态水墨流变效果）
        float currentTime = (float)gameTime.TotalGameTime.TotalSeconds;
        _inkBleedEffect.Parameters["Time"]?.SetValue(currentTime);

        // 3. 地形遮罩纹理（已在 GenerateTerrainMask 中设置，此处无需重复设置）
        // 日期：2026-03-17
        // 注意：MaskTexture 在 GenerateTerrainMask() 中设置，只需调用一次

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,  // 🔥 关键：正确的半透明叠加
            SamplerState.LinearClamp,
            null, null, _inkBleedEffect, null);

        // 🔥 使用项目中实际可用的 3 参数 Draw 重载
        // Draw(Texture2D, Rectangle, Color)
        spriteBatch.Draw(
            _lowResTarget,
            _graphicsDevice.Viewport.Bounds,
            new Color(255, 255, 255, 102));  // 40% 透明度

        spriteBatch.End();
    }

    
    /// <summary>
    /// 🔥 重建低分屏画布（分辨率变化时调用）
    /// 🧊 Cold Path：分辨率变化时调用
    /// </summary>
    public void RebuildRenderTarget()
    {
        _lowResTarget?.Dispose();
        
        int screenWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;
        
        _lowResTarget = new RenderTarget2D(
            _graphicsDevice,
            screenWidth / 2,
            screenHeight / 2,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
        
        // 🔥 分辨率变化时更新缩放比例缓存，同时使 tileSize 缓存失效
        _scaleX = (float)_lowResTarget.Width / screenWidth;
        _scaleY = (float)_lowResTarget.Height / screenHeight;
        _cachedTileSize = -1;  // 强制下一帧重算 _lowResTileW/H
        
        System.Diagnostics.Debug.WriteLine(
            $"[InkBleedInfluenceRenderer] 重建低分屏画布：{_lowResTarget.Width}×{_lowResTarget.Height}");
    }
    
    /// <summary>
    /// 🔥 释放资源
    /// 🧊 Cold Path：游戏退出时调用
    /// </summary>
    public void Dispose()
    {
        // 🔥 延迟初始化场景：资源可能为 null
        _lowResTarget?.Dispose();
        _pixelTexture?.Dispose();
        _terrainMask?.Dispose();
    }
    
    /// <summary>
    /// 🧊 Cold Path：诊断方法 - 输出渲染器状态
    /// 手动调用（例如按 F12 键）来检查渲染器是否正常工作
    /// </summary>
    public string GetDiagnosticInfo()
    {
        var scenario = global::GameManager.Session.Current?.Scenario;
        if (scenario == null)
        {
            return "[诊断] Scenario 为 null";
        }
        
        // 统计势力范围数据
        int totalInfluenceTiles = 0;
        int maxFactionID = -1;
        
        for (int i = 0; i < _targetInfluenceMap.Length; i++)
        {
            if (_targetInfluenceMap[i] >= 0)
            {
                totalInfluenceTiles++;
                if (_targetInfluenceMap[i] > maxFactionID)
                {
                    maxFactionID = _targetInfluenceMap[i];
                }
            }
        }
        
        // 统计可见透明度
        int visibleTiles = 0;
        for (int i = 0; i < _visualInfluenceMap.Length; i++)
        {
            if (_visualInfluenceMap[i] > 0.01f)
            {
                visibleTiles++;
            }
        }
        
        return $"""
            [水墨渲染器诊断]
            地图尺寸: {_mapWidth}×{_mapHeight}
            低分屏尺寸: {_lowResTarget.Width}×{_lowResTarget.Height}
            势力范围格子数: {totalInfluenceTiles}
            可见格子数: {visibleTiles}
            最大势力ID: {maxFactionID}
            势力总数: {scenario.Factions?.Count ?? 0}
            当前玩家: {scenario.CurrentPlayer?.Name ?? "null"}
            """;
    }
}
