using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms;
using Microsoft.Xna.Framework.Graphics;
using GameManager;

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
            try
            {
                if ((Setting.Current.GlobalVariables.DrawMapVeil && !Session.GlobalVariables.SkyEye) && !Session.Current.Scenario.NoCurrentPlayer)
                {
                    // 验证veilTexture是否有效
                    if (this.veilTexture == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[MapVeilLayer] veilTexture为null，跳过绘制");
                        return;
                    }
                    
                    if (this.CurrentPlayer == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[MapVeilLayer] CurrentPlayer为null，跳过绘制");
                        return;
                    }
                    
                    foreach (Tile tile in Session.MainGame.mainGameScreen.mainMapLayer.DisplayingTiles)
                    {
                        try
                        {
                            Rectangle? nullable = null;
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
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MapVeilLayer] 绘制单个瓦片时发生异常: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapVeilLayer] Draw方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MapVeilLayer] 异常堆栈: {ex.StackTrace}");
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
