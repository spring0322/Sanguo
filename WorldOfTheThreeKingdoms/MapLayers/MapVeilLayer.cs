using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms;
using Microsoft.Xna.Framework.Graphics;
using GameManager;
using WorldOfTheThreeKingdoms.GameManager;

namespace WorldOfTheThreeKingdoms.GameScreens.ScreenLayers

{
    public class MapVeilLayer
    {
        private Color BlendColorFull = new Color(new Vector4(0.8f, 0.8f, 0.8f, 0f));
        private Color BlendColorHigh = new Color(new Vector4(0.8f, 0.8f, 0.8f, 0.09f));
        private Color BlendColorLow = new Color(new Vector4(0.8f, 0.8f, 0.8f, 0.27f));
        private Color BlendColorMiddle = new Color(new Vector4(0.8f, 0.8f, 0.8f, 0.18f));
        private Color BlendColorNone = new Color(new Vector4(0.8f, 0.8f, 0.8f, 0.6f));

        private PlatformTexture veilTexture;

        public void Draw(Point viewportSize)
        {
            // 🔥 ANTI-BAND-AID：明确检查数据源
            if (Session.Current == null)
            {
                throw new InvalidOperationException(
                    "[MapVeilLayer] Session.Current 为 null，游戏未正确初始化");
            }
            
            if (Session.Current.Scenario == null)
            {
                throw new InvalidOperationException(
                    "[MapVeilLayer] Session.Current.Scenario 为 null，场景未加载");
            }

            if ((Setting.Current.GlobalVariables.DrawMapVeil && !Session.GlobalVariables.SkyEye) && !Session.Current.Scenario.NoCurrentPlayer)
            {
                // 🔥 ANTI-BAND-AID：明确检查 veilTexture
                if (this.veilTexture == null)
                {
                    throw new InvalidOperationException(
                        "[MapVeilLayer] veilTexture 为 null，未正确初始化，请检查 Initialize 方法");
                }
                
                // 🔥 ANTI-BAND-AID：明确检查 CurrentPlayer
                if (this.CurrentPlayer == null)
                {
                    throw new InvalidOperationException(
                        "[MapVeilLayer] CurrentPlayer 为 null，场景数据错误");
                }
                
                // 🔥 ANTI-BAND-AID：明确检查 GlobalInfluenceMap
                if (this.CurrentPlayer.GlobalInfluenceMap == null)
                {
                    throw new InvalidOperationException(
                        $"[MapVeilLayer] 当前玩家 {this.CurrentPlayer.Name} 的 GlobalInfluenceMap 为 null，" +
                        $"数据未正确初始化，请检查 InfluenceUpdateManager");
                }
                
                int mapWidth = TerrainCostCache.MapWidth;
                
                // 🔥 Hot Path 优化：提前声明，避免循环内分配
                Rectangle? nullable = null;
                
                // 🔥 ANTI-BAND-AID：明确检查 DisplayingTiles
                var displayingTiles = Session.MainGame.mainGameScreen.mainMapLayer.DisplayingTiles;
                if (displayingTiles == null)
                {
                    throw new InvalidOperationException(
                        "[MapVeilLayer] DisplayingTiles 为 null，地图层未正确初始化");
                }
                
                // 🔥 Hot Path 优化：使用 for 循环，避免枚举器分配
                int tileCount = displayingTiles.Count;
                for (int i = 0; i < tileCount; i++)
                {
                    Tile tile = displayingTiles[i];
                    
                    // 🔥 检查情报等级（决定迷雾深浅）
                    switch (this.CurrentPlayer.GetKnownAreaDataNoCheck(tile.Position))
                    {
                        case InformationLevel.无:
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorNone, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;

                        case InformationLevel.低:
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorLow, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;

                        case InformationLevel.中:
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorMiddle, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;

                        case InformationLevel.高:
                            CacheManager.Draw(this.veilTexture, tile.Destination, nullable, this.BlendColorHigh, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
                            break;
                            
                        // InformationLevel.全 不绘制迷雾
                    }
                }
            }
        }

        public void Initialize(MainGameScreen screen)
        {
            this.veilTexture = screen.Textures.MapVeilTextures[0];
        }

        private Faction CurrentPlayer
        {
            get
            {
                return Session.Current.Scenario.CurrentPlayer;
            }
        }
    }


}
