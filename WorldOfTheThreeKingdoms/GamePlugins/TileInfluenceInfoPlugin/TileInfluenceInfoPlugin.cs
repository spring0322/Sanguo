#nullable disable

using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using PluginInterface;
using PluginInterface.BaseInterface;
using System;
using System.Collections.Generic;
using System.Xml;
using WorldOfTheThreeKingdoms.GameScreens;
using WTKGameManager = WorldOfTheThreeKingdoms.GameManager;  // 🔥 引用 TerrainCostCache

namespace TileInfluenceInfoPlugin;

/// <summary>
/// 地块势力范围信息显示插件
/// 位置：右上角，占窗口右侧约 16.7% 宽度（1/6），留出上边距和右边距
/// 功能：显示鼠标悬停地块的势力范围、地形、控制力、补给、攻防加成
/// 日期：2026-03-13
/// </summary>
public sealed class TileInfluenceInfoPlugin : GameObject, ITileInfluenceInfo, IBasePlugin, IPluginXML, IPluginGraphics
{
    // 背景纹理路径（InfluenceRenderer 专用）
    private const string DataPath = @"Content\Textures\GameComponents\InfluenceRenderer\Data\";
    private const string XMLFilename = "TileInfluenceInfoData.xml";
    
    private readonly TileInfluenceInfo _tileInfo = new();
    
    // 插件元数据
    public string Author => "zhsan-dev";
    public string Description => "地块势力范围信息显示";
    public string PluginName => "TileInfluenceInfoPlugin";
    public string Version => "1.0.0";
    
    // IBasePlugin.Instance 实现
    public object Instance => this;
    
    // 当前悬停的地块坐标
    private Point _currentTilePosition = new(-1, -1);
    
    public bool IsShowing
    {
        get => _tileInfo.IsShowing;
        set => _tileInfo.IsShowing = value;
    }
    
    public void Dispose() { }
    
    public void Initialize(Screen screen) { }
    
    public void SetGraphicsDevice()
    {
        LoadDataFromXMLDocument(@"Content\Data\Plugins\TileInfluenceInfoData.xml");
    }
    
    /// <summary>
    /// 🧊 Cold Path：从 XML 加载配置
    /// </summary>
    public void LoadDataFromXMLDocument(string filename)
    {
        XmlDocument document = new();
        string xml = Platform.Current.LoadText(filename);
        
        // 🔥 ANTI-BAND-AID：Fail Fast
        if (string.IsNullOrEmpty(xml))
        {
            throw new InvalidOperationException(
                $"❌ TileInfluenceInfoPlugin 数据文件加载失败：{filename}\n   文件不存在或内容为空。");
        }
        
        document.LoadXml(xml);
        XmlNode root = document.FirstChild.NextSibling;
        
        // 加载背景
        XmlNode node = root.ChildNodes.Item(0);
        _tileInfo.BackgroundTexture = CacheManager.GetTempTexture(
            DataPath + node.Attributes.GetNamedItem("FileName").Value);
        _tileInfo.BackgroundClient = StaticMethods.LoadRectangleFromXMLNode(node);
        
        // 加载五行文本配置
        _tileInfo.FactionArchitectureText = LoadTextNode(root.ChildNodes.Item(1));
        _tileInfo.TerrainText = LoadTextNode(root.ChildNodes.Item(2));
        _tileInfo.ControlText = LoadTextNode(root.ChildNodes.Item(3));
        _tileInfo.SupplyText = LoadTextNode(root.ChildNodes.Item(4));
        _tileInfo.BuffText = LoadTextNode(root.ChildNodes.Item(5));
    }
    
    /// <summary>
    /// 🧊 Cold Path：加载单个文本节点
    /// </summary>
    private static FreeText LoadTextNode(XmlNode node)
    {
        StaticMethods.LoadFontAndColorFromXMLNode(node, out Font font, out Color color);
        return new FreeText(font, color)
        {
            Position = StaticMethods.LoadRectangleFromXMLNode(node),
            Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value),
            DisplayOffset = new(0, 0),
            Text = ""  // 初始化为空字符串
        };
    }
    
    /// <summary>
    /// 🧊 Cold Path：窗口大小调整时重新布局
    /// </summary>
    public void SetRealViewportSize(Point realViewportSize)
    {
        // 重新加载原始尺寸
        LoadDataFromXMLDocument(@"Content\Data\Plugins\TileInfluenceInfoData.xml");
        
        // 🎯 设计：占据窗口右侧约 13.3% 宽度（240/1800），留出右边距和上边距
        // 原始设计宽度：240px（从 300px 缩小）
        const float designWidthRatio = 0.133f;  // 占窗口 13.3%
        const float baseDesignWidth = 240f;
        const int rightMargin = 10;  // 右边距 10px
        const int topMargin = 10;    // 上边距 10px
        
        float targetWidth = realViewportSize.X * designWidthRatio;
        float scaleX = targetWidth / baseDesignWidth;
        
        // 🎯 关键：将插件定位到右上角，留出右边距和上边距
        int rightOffset = realViewportSize.X - (int)targetWidth - rightMargin;
        
        // 局部方法：缩放并平移 Rectangle 到右侧，添加上边距
        Rectangle ScaleAndOffsetRect(Rectangle rect)
        {
            return new(
                (int)(rect.X * scaleX) + rightOffset,
                rect.Y + topMargin,
                (int)(rect.Width * scaleX),
                rect.Height
            );
        }
        
        // 局部方法：缩放并平移 FreeText
        void ScaleAndOffsetText(FreeText text)
        {
            text.Position = ScaleAndOffsetRect(text.Position);
        }
        
        // 调整背景位置和宽度（添加上边距）
        _tileInfo.BackgroundClient = new(
            rightOffset,
            topMargin,
            (int)targetWidth,
            _tileInfo.BackgroundClient.Height
        );
        
        // 缩放并平移所有文本到右侧
        ScaleAndOffsetText(_tileInfo.FactionArchitectureText);
        ScaleAndOffsetText(_tileInfo.TerrainText);
        ScaleAndOffsetText(_tileInfo.ControlText);
        ScaleAndOffsetText(_tileInfo.SupplyText);
        ScaleAndOffsetText(_tileInfo.BuffText);
    }
    
    /// <summary>
    /// 🔥 Hot Path：更新鼠标悬停的地块信息
    /// </summary>
    public void UpdateTileInfo(Point mousePosition, int leftEdge, int topEdge, int tileWidth, int tileHeight)
    {
        // 计算地图坐标
        int mapX = (mousePosition.X - leftEdge) / tileWidth;
        int mapY = (mousePosition.Y - topEdge) / tileHeight;
        
        // 检查坐标有效性
        if (mapX < 0 || mapY < 0 || 
            mapX >= Session.Current.Scenario.ScenarioMap.MapDimensions.X ||
            mapY >= Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
        {
            IsShowing = false;
            return;
        }
        
        Point tilePos = new(mapX, mapY);
        
        // 🔥 性能优化：只在地块变化时更新
        if (tilePos == _currentTilePosition)
        {
            return;
        }
        
        _currentTilePosition = tilePos;
        UpdateTileInfoInternal(mapX, mapY);
    }
    
    /// <summary>
    /// 🧊 Cold Path：更新地块信息文本（地块变化时调用）
    /// </summary>
    private void UpdateTileInfoInternal(int mapX, int mapY)
    {
        GameScenario scenario = Session.Current.Scenario;

        // 🔥 性能优化：一次计算所有需要的数据（避免重复计算）
        int index = WTKGameManager.TerrainCostCache.GetIndex(mapX, mapY);
        (Faction? dominantFaction, int maxEnergy) = GetDominantFactionAndEnergy(mapX, mapY);
        
        // 🔥 预计算玩家能量和净能量（供后续方法使用）
        int playerEnergy = 0;
        int netEnergy = 0;
        if (scenario.CurrentPlayer != null && scenario.CurrentPlayer.GlobalInfluenceMap is { Length: > 0 })
        {
            playerEnergy = scenario.CurrentPlayer.GlobalInfluenceMap[index].EffectiveTotalEnergy;
            
            // 🔥 关键修复：净能量计算逻辑
            // 如果最强势力是己方或盟友，使用己方能量
            // 如果最强势力是敌方，使用 playerEnergy - maxEnergy（负数）
            if (dominantFaction == scenario.CurrentPlayer || 
                (dominantFaction != null && dominantFaction.IsFriendly(scenario.CurrentPlayer)))
            {
                netEnergy = playerEnergy;  // 己方区域：直接使用己方能量
            }
            else
            {
                netEnergy = playerEnergy - maxEnergy;  // 敌方区域：负数
            }
        }

        // 🔥 战争迷雾判定：检查玩家是否有视野
        bool hasVision = CheckPlayerVision(mapX, mapY, dominantFaction, maxEnergy);

        // 第一行：势力名称 + 城池名称
        UpdateFactionAndArchitectureText(mapX, mapY, dominantFaction, maxEnergy, hasVision);

        // 第二行：剩余能量值（传递 netEnergy）
        UpdateEnergyText(netEnergy, hasVision);

        // 第三行：控制力
        UpdateControlText(dominantFaction, hasVision);

        // 第四行：补给（传递预计算的 netEnergy）
        UpdateSupplyText(netEnergy, hasVision);

        // 第五行：增益（传递预计算的 netEnergy）
        UpdateBuffText(netEnergy, hasVision);

        IsShowing = true;
    }

    
    /// <summary>
    /// 🧊 Cold Path：获取地块的最强势力和能量值（一次遍历，避免重复计算）
    /// </summary>
    /// <returns>元组：(最强势力, 最大能量值)</returns>
    private (Faction? dominantFaction, int maxEnergy) GetDominantFactionAndEnergy(int mapX, int mapY)
    {
        GameScenario scenario = Session.Current.Scenario;
        
        // 🔥 ANTI-BAND-AID：Factions 应该在场景加载时初始化
        if (scenario.Factions == null)
        {
            throw new InvalidOperationException(
                "[TileInfluenceInfoPlugin] Scenario.Factions 为 null，数据未正确初始化");
        }
        
        // 🔥 关键：使用 TerrainCostCache.GetIndex() 计算索引（与 InfluenceRenderer 一致）
        int index = WTKGameManager.TerrainCostCache.GetIndex(mapX, mapY);
        
        // 🔥 一次遍历找出能量最高的势力
        Faction? dominantFaction = null;
        int maxEnergy = 0;
        
        var factions = scenario.Factions.GetList();
        int factionCount = factions.Count;
        
        for (int f = 0; f < factionCount; f++)
        {
            if (factions[f] is not Faction factionObj)
            {
                throw new InvalidOperationException(
                    $"[TileInfluenceInfoPlugin] Scenario.Factions 包含无效项（索引 {f}）");
            }
            
            // 🔥 ANTI-BAND-AID：GlobalInfluenceMap 应该在势力初始化时创建
            if (factionObj.GlobalInfluenceMap == null)
            {
                throw new InvalidOperationException(
                    $"[TileInfluenceInfoPlugin] 势力 {factionObj.Name} 的 GlobalInfluenceMap 为 null");
            }
            if ((uint)index >= (uint)factionObj.GlobalInfluenceMap.Length)
            {
                throw new InvalidOperationException(
                    $"[TileInfluenceInfoPlugin] 势力 {factionObj.Name} 的 GlobalInfluenceMap 长度异常，index={index}，length={factionObj.GlobalInfluenceMap.Length}");
            }
            
            // 🔥 关键：使用标准索引读取能量值（与 InfluenceRenderer 一致）
            // 🔥 日期：2026-03-16
            // 🔥 重构：使用 EffectiveTotalEnergy
            int energy = factionObj.GlobalInfluenceMap[index].EffectiveTotalEnergy;
            
            if (energy > maxEnergy)
            {
                maxEnergy = energy;
                dominantFaction = factionObj;
            }
        }
        
        return (dominantFaction, maxEnergy);
    }
    
    /// <summary>
    /// 🧊 Cold Path：战争迷雾判定（检查玩家是否有视野）
    /// </summary>
    /// <returns>true = 有视野，false = 战争迷雾</returns>
    private bool CheckPlayerVision(int mapX, int mapY, Faction? dominantFaction, int maxEnergy)
    {
        GameScenario scenario = Session.Current.Scenario;
        
        // 🔥 天眼模式：全图可见
        if (Session.GlobalVariables.SkyEye)
        {
            return true;
        }
        
        // 🔥 无玩家势力：全图可见
        if (scenario.CurrentPlayer == null)
        {
            return true;
        }
        
        // 🔥 关键：玩家能量 > 0 → 有实时视野
        // 🔥 日期：2026-03-16
        // 🔥 重构：使用 EffectiveTotalEnergy
        int index = WTKGameManager.TerrainCostCache.GetIndex(mapX, mapY);
        if (scenario.CurrentPlayer.GlobalInfluenceMap == null)
        {
            throw new InvalidOperationException(
                $"[TileInfluenceInfoPlugin] 当前玩家 {scenario.CurrentPlayer.Name} 的 GlobalInfluenceMap 为 null");
        }
        if ((uint)index >= (uint)scenario.CurrentPlayer.GlobalInfluenceMap.Length)
        {
            throw new InvalidOperationException(
                $"[TileInfluenceInfoPlugin] 当前玩家 {scenario.CurrentPlayer.Name} 的 GlobalInfluenceMap 长度异常，index={index}，length={scenario.CurrentPlayer.GlobalInfluenceMap.Length}");
        }
        int playerEnergy = scenario.CurrentPlayer.GlobalInfluenceMap[index].EffectiveTotalEnergy;
        
        if (playerEnergy > 0)
        {
            return true;
        }
        
        // 🔥 无玩家能量：检查是否有情报覆盖
        Point position = new(mapX, mapY);
        return scenario.CurrentPlayer.IsPositionKnown(position);
    }
    
    /// <summary>
    /// 第一行：势力名称 + 城池名称
    /// </summary>
    private void UpdateFactionAndArchitectureText(int mapX, int mapY, Faction? dominantFaction, int maxEnergy, bool hasVision)
    {
        // 🔥 战争迷雾：显示"未知"
        if (!hasVision)
        {
            _tileInfo.FactionArchitectureText.Text = "未知";
            return;
        }
        
        // 🔥 无势力：显示"无主之地"
        if (dominantFaction == null || maxEnergy <= 0)
        {
            _tileInfo.FactionArchitectureText.Text = "无主之地";
            return;
        }
        
        // 🔥 获取城池名称（从势力的 SourceArchitectureMap）
        int index = WTKGameManager.TerrainCostCache.GetIndex(mapX, mapY);
        int archId = WTKGameManager.MultiSourceInfluenceCalculator.GetSourceArchitectureId(dominantFaction.ID, index);
        

        
        // 🔥 关键：ID=0（洛阳）是有效的，必须使用 >= 0
        if (archId >= 0)
        {
            Architecture arch = Session.Current.Scenario.Architectures
                .GetGameObject(archId) as Architecture;
            
            // 🔥 ANTI-BAND-AID：如果建筑不存在，说明数据损坏
            if (arch == null)
            {
                throw new InvalidOperationException(
                    $"[TileInfluenceInfoPlugin] 数据损坏：势力 {dominantFaction.Name} 的 SourceArchitectureMap[{index}] " +
                    $"引用了不存在的建筑 ID={archId}");
            }
            

            
            _tileInfo.FactionArchitectureText.Text = $"{dominantFaction.Name}     【{arch.Name}】";
        }
        else
        {
            // 无城池归属（野外能量扩散区域）

            _tileInfo.FactionArchitectureText.Text = dominantFaction.Name;
        }
    }
    
    /// <summary>
    /// 第二行：剩余能量值
    /// 🔥 日期：2026-03-17
    /// 🔥 修复：显示剩余能量（归属势力的优势能量），而不是绝对能量
    /// 🔥 逻辑：
    ///   - 己方区域：己方能量 - 其他势力最大能量（正数）
    ///   - 敌方区域：己方能量 - 敌方能量（负数）
    /// </summary>
    private void UpdateEnergyText(int netEnergy, bool hasVision)
    {
        // 🔥 战争迷雾：显示"未知"
        if (!hasVision)
        {
            _tileInfo.TerrainText.Text = "能量：未知";
            return;
        }
        
        // 🔥 显示剩余能量（可能为负数）
        if (netEnergy >= 0)
        {
            _tileInfo.TerrainText.Text = $"能量：+{netEnergy}";
        }
        else
        {
            _tileInfo.TerrainText.Text = $"能量：{netEnergy}";
        }
    }
    
    /// <summary>
    /// 第三行：控制力（我方/敌方/中立）
    /// </summary>
    private void UpdateControlText(Faction? dominantFaction, bool hasVision)
    {
        // 🔥 战争迷雾：显示"未知"
        if (!hasVision)
        {
            _tileInfo.ControlText.Text = "控制力：未知";
            return;
        }
        
        GameScenario scenario = Session.Current.Scenario;
        
        // 🔥 无玩家势力：显示"中立"
        if (scenario.CurrentPlayer == null)
        {
            _tileInfo.ControlText.Text = "控制力：中立";
            return;
        }
        
        // 🔥 无势力：显示"中立"
        if (dominantFaction == null)
        {
            _tileInfo.ControlText.Text = "控制力：中立";
            return;
        }
        
        // 🔥 判断我方/盟友/敌方
        // 使用排除法：不是我方、不是盟友 → 就是敌方
        if (dominantFaction == scenario.CurrentPlayer)
        {
            _tileInfo.ControlText.Text = "控制力：我方";
        }
        else if (dominantFaction.IsFriendly(scenario.CurrentPlayer))
        {
            _tileInfo.ControlText.Text = "控制力：盟友";
        }
        else
        {
            // 🔥 排除法：有视野且不是我方/盟友 → 必定是敌方
            _tileInfo.ControlText.Text = "控制力：敌方";
        }
    }
    
    /// <summary>
    /// 第四行：补给（根据是否我方地块显示正/负数值）
    /// 🔥 日期：2026-03-17
    /// 🔥 修复：使用城池视角计算粮食消耗倍率（最高±15%，阈值500）
    /// 🔥 修复：始终显示实际数值，不再显示"正常"
    /// 🔥 性能优化：netEnergy 由调用方预计算并传入
    /// </summary>
    private void UpdateSupplyText(int netEnergy, bool hasVision)
    {
        // 🔥 战争迷雾：显示"未知"
        if (!hasVision)
        {
            _tileInfo.SupplyText.Text = "耗粮：未知";
            return;
        }
        
        GameScenario scenario = Session.Current.Scenario;
        
        // 🔥 无玩家势力：显示"正常"
        if (scenario.CurrentPlayer == null)
        {
            _tileInfo.SupplyText.Text = "耗粮：正常";
            return;
        }
        
        // 🔥 使用城池视角计算粮食消耗倍率（阈值500）
        float multiplier = WTKGameManager.InfluenceBuffCalculator.CalculateFoodConsumptionMultiplier(netEnergy, isArchitecture: true);
        float percentage = (multiplier - 1.0f) * 100;
        
        // 🔥 修复：始终显示实际数值（包括 0.0%）
        if (percentage < 0)
        {
            // 我方优势：降低粮食消耗
            _tileInfo.SupplyText.Text = $"耗粮：{percentage:F1}%";
        }
        else if (percentage > 0)
        {
            // 敌方优势：增加粮食消耗
            _tileInfo.SupplyText.Text = $"耗粮：+{percentage:F1}%";
        }
        else
        {
            // 中立：显示 0.0%
            _tileInfo.SupplyText.Text = "耗粮：0.0%";
        }
    }
    
    /// <summary>
    /// 第五行：增益（根据是否我方地块显示正/负百分比）
    /// 🔥 日期：2026-03-17
    /// 🔥 修复：使用城池视角计算攻防加成（城池最高10%，阈值500）
    /// 🔥 修复：始终显示实际数值，不再显示"无"或"无加成"
    /// 🔥 修复：缩短显示格式，避免超出 UI 宽度
    /// 🔥 性能优化：netEnergy 由调用方预计算并传入
    /// </summary>
    private void UpdateBuffText(int netEnergy, bool hasVision)
    {
        // 🔥 战争迷雾：显示"未知"
        if (!hasVision)
        {
            _tileInfo.BuffText.Text = "攻防：未知";
            return;
        }
        
        GameScenario scenario = Session.Current.Scenario;
        
        // 🔥 无玩家势力：显示"无"
        if (scenario.CurrentPlayer == null)
        {
            _tileInfo.BuffText.Text = "攻防：无";
            return;
        }
        
        // 🔥 使用城池视角计算攻防加成（城池最高10%，阈值500）
        float attackMultiplier = WTKGameManager.InfluenceBuffCalculator.CalculateAttackBonus(netEnergy, isArchitecture: true);
        float defenseMultiplier = WTKGameManager.InfluenceBuffCalculator.CalculateDefenseBonus(netEnergy, isArchitecture: true);
        
        float attackPercentage = (attackMultiplier - 1.0f) * 100;
        float defensePercentage = (defenseMultiplier - 1.0f) * 100;
        
        // 🔥 修复：缩短显示格式（攻防数值相同，只显示一次）
        if (attackPercentage > 0 || defensePercentage > 0)
        {
            _tileInfo.BuffText.Text = $"攻防：+{attackPercentage:F1}%";
        }
        else
        {
            _tileInfo.BuffText.Text = "攻防：+0.0%";
        }
    }
    
    /// <summary>
    /// 🔥 Hot Path：绘制插件
    /// </summary>
    public void Draw()
    {
        if (IsShowing)
        {
            _tileInfo.Draw(Session.MainGame.SpriteBatch);
        }
    }
    
    public void Update(GameTime gameTime) { }
    
    public void SetScreen(Screen screen) { }
}
