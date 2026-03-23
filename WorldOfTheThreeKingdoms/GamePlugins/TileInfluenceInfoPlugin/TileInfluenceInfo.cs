using GameFreeText;
using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;

namespace TileInfluenceInfoPlugin;

/// <summary>
/// 地块势力范围信息显示核心类
/// 日期：2026-03-13
/// </summary>
public sealed class TileInfluenceInfo
{
    // 背景
    public PlatformTexture BackgroundTexture { get; set; }
    public Rectangle BackgroundClient { get; set; }
    
    // 五行文本
    public FreeText FactionArchitectureText { get; set; }  // 势力-城池
    public FreeText TerrainText { get; set; }              // 地形
    public FreeText ControlText { get; set; }              // 控制力
    public FreeText SupplyText { get; set; }               // 补给
    public FreeText BuffText { get; set; }                 // 攻防加成
    
    // 显示控制
    public bool IsShowing { get; set; }
    
    /// <summary>
    /// 绘制插件（🔥 Hot Path：每帧调用）
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsShowing) return;
        
        // 🔥 ANTI-BAND-AID：BackgroundTexture 在 LoadDataFromXMLDocument 中初始化
        // 如果为 null，说明初始化失败，应该在加载时 Fail Fast
        CacheManager.Draw(BackgroundTexture, BackgroundClient, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.43f);
        
        // 🔥 ANTI-BAND-AID：这些对象在 LoadDataFromXMLDocument 中初始化，不应为 null
        // 如果为 null，说明初始化失败，应该在加载时 Fail Fast
        const float textDepth = 0.4299f;
        FactionArchitectureText.Draw(textDepth);
        TerrainText.Draw(textDepth);
        ControlText.Draw(textDepth);
        SupplyText.Draw(textDepth);
        BuffText.Draw(textDepth);
    }
}
