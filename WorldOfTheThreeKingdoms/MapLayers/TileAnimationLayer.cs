using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms;
using Microsoft.Xna.Framework.Graphics;
using GameObjects.Animations;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameScreens.ScreenLayers

{
    public class TileAnimationLayer
    {
        public void Draw(Point viewportSize)
        {
            // 🔥 Anti-Band-Aid：这些空检查是遗留代码，暂时保留
            // TODO：未来应该在初始化时确保这些对象非 null，然后移除这些检查
            if (Session.Current?.Scenario?.GeneratorOfTileAnimation?.TileAnimations == null)
            {
                return;
            }

            if (Session.MainGame?.mainGameScreen?.mainMapLayer == null)
            {
                return;
            }

            foreach (TileAnimation animation in Session.Current.Scenario.GeneratorOfTileAnimation.TileAnimations.Values)
            {
                if (animation == null) continue;

                bool inScreen = Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(animation.Position);
                bool isVisible = (Session.GlobalVariables.SkyEye || Session.Current.Scenario.NoCurrentPlayer) || 
                                 ((Session.Current.Scenario.CurrentPlayer != null) && Session.Current.Scenario.CurrentPlayer.IsPositionKnown(animation.Position));

                #if DEBUG
                /*
                // 🔥 诊断：输出动画状态（仅前3帧，避免 Hot Path 字符串分配）
                if (animation.Drawing && animation.currentFrameIndex < 3)
                {
                    System.Diagnostics.Debug.WriteLine($"[TileAnimationLayer] Kind={animation.Kind}, Pos={animation.Position}, InScreen={inScreen}, IsVisible={isVisible}, DrawTroopAnim={Setting.Current.GlobalVariables.DrawTroopAnimation}");
                }
                */
                #endif

                if ((Setting.Current.GlobalVariables.DrawTroopAnimation && inScreen) && isVisible)
                {
                    var tiles = Session.MainGame.mainGameScreen.mainMapLayer.Tiles;
                    if (tiles != null && 
                        animation.Position.X >= 0 && animation.Position.X < tiles.GetLength(0) &&
                        animation.Position.Y >= 0 && animation.Position.Y < tiles.GetLength(1) &&
                        tiles[animation.Position.X, animation.Position.Y] != null)
                    {
                        animation.Draw(tiles[animation.Position.X, animation.Position.Y].Destination);
                    }
                }
                else
                {
                    if (!animation.Looping)
                    {
                        animation.Drawing = false;
                    }
                }
            }
            
            if (Session.Current?.Scenario?.GeneratorOfTileAnimation != null)
            {
                Session.Current.Scenario.GeneratorOfTileAnimation.ClearFinishedAnimation();
            }
        }

        public void Initialize()
        {
        }
    }

 

}
