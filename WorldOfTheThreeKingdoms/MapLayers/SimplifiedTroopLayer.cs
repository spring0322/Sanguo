using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects.TroopDetail;
using GameManager;
using GameGlobal;
using GameObjects;
using Platforms;

namespace WorldOfTheThreeKingdoms.MapLayers
{
    /// <summary>
    /// 简化的部队渲染层，使用四叉树优化
    /// </summary>
    public class SimplifiedTroopLayer
    {
        private List<Troop> _visibleTroops = new List<Troop>();

        public void Draw(Point viewportSize, GameTime gameTime, Quadtree troopQuadtree)
        {
            if (!Setting.Current.GlobalVariables.DrawTroopAnimation)
            {
                return;
            }

            // 获取可见区域
            Rectangle visibleArea = GetVisibleArea();
            
            // 使用四叉树获取可能可见的部队
            _visibleTroops.Clear();
            int totalTroops = Session.Current.Scenario.Troops.Count;
            
            if (troopQuadtree != null)
            {
                troopQuadtree.Retrieve(_visibleTroops, visibleArea);
            }
            else
            {
                // 回退到所有部队
                _visibleTroops.AddRange(Session.Current.Scenario.Troops.GetList());
            }
            
            int culledTroops = totalTroops - _visibleTroops.Count;

            // 只绘制四叉树查询返回的部队
            foreach (Troop troop in _visibleTroops)
            {
                if (troop.Destroyed || !troop.DrawAnimation)
                {
                    continue;
                }

                // 额外的精确剔除检查
                if (!Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(troop.Position))
                {
                    continue;
                }

                // 可见性检查（战争迷雾等）
                if (!IsVisible(troop))
                {
                    troop.SetNotShowing();
                    continue;
                }

                // 使用原始的部队绘制逻辑
                DrawSingleTroop(troop, viewportSize, gameTime);
            }

            // 调试信息
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine($"[SimplifiedTroopLayer] 总计: {totalTroops}, 查询: {_visibleTroops.Count}, 剔除: {culledTroops}");
            }
        }

        private Rectangle GetVisibleArea()
        {
            // 使用 Screen 基类的 GetVisibleArea 方法
            var mainGameScreen = Session.MainGame.mainGameScreen;
            if (mainGameScreen != null)
            {
                return mainGameScreen.GetVisibleArea(Session.MainGame.SpriteScale2);
            }

            // 回退：手动计算可见区域
            int tileWidth = Session.MainGame.mainGameScreen.mainMapLayer.TileWidth;
            int tileHeight = Session.MainGame.mainGameScreen.mainMapLayer.TileHeight;
            
            Point topLeft = Session.MainGame.mainGameScreen.TopLeftPosition;
            Point bottomRight = Session.MainGame.mainGameScreen.BottomRightPosition;
            
            Rectangle visibleArea = new Rectangle(
                topLeft.X * tileWidth,
                topLeft.Y * tileHeight,
                (bottomRight.X - topLeft.X + 1) * tileWidth,
                (bottomRight.Y - topLeft.Y + 1) * tileHeight
            );
            
            // 添加填充以防止边缘对象消失
            visibleArea.Inflate(100, 100);
            return visibleArea;
        }

        private bool IsVisible(Troop troop)
        {
            // 检查可见性条件（战争迷雾、派系关系等）
            return (Session.GlobalVariables.SkyEye || 
                    Session.Current.Scenario.NoCurrentPlayer || 
                    Session.Current.Scenario.CurrentPlayer.IsFriendly(troop.BelongedFaction) || 
                    Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) &&
                   (Session.GlobalVariables.SkyEye || 
                    Session.Current.Scenario.CurrentPlayer == null || 
                    troop.Status != TroopStatus.埋伏 || 
                    troop.IsFriendly(Session.Current.Scenario.CurrentPlayer));
        }

        private void DrawSingleTroop(Troop troop, Point viewportSize, GameTime gameTime)
        {
            // 简化的部队绘制 - 使用基本的精灵绘制
            Color troopColor = Color.White;
            if (troop.CurrentOutburstKind == OutburstKind.愤怒)
            {
                troopColor = Color.Red;
            }
            else if (troop.CurrentOutburstKind == OutburstKind.沉静)
            {
                troopColor = Color.Green;
            }

            // 确保部队有动画帧
            if (troop.TileAnimation.FrameCount == 0)
            {
                troop.TileAnimation.FrameCount = 1;
            }

            // 绘制部队精灵
            if (troop.Action == TroopAction.Stop)
            {
                CacheManager.Draw(
                    troop.TileAnimation.Texture,
                    Session.MainGame.mainGameScreen.mainMapLayer.Tiles[troop.Position.X, troop.Position.Y].Destination,
                    new Rectangle?(troop.GetCurrentTroopActionRectangle(troop.TileAnimation.Texture.Width / troop.TileAnimation.FrameCount)),
                    troopColor,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.7f);
            }
            else
            {
                // 移动中的部队
                CacheManager.Draw(
                    troop.TileAnimation.Texture,
                    troop.RealDestination,
                    new Rectangle?(troop.GetCurrentTroopActionRectangle(troop.TileAnimation.Texture.Width / troop.TileAnimation.FrameCount)),
                    troopColor,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.7f);
            }
        }

        public void Initialize()
        {
            // 初始化简化部队层需要的资源
        }
    }
}