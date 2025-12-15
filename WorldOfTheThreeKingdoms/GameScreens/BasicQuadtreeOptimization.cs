using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects.TroopDetail;
using GameGlobal;
using GameObjects;
using Platforms;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 基础四叉树优化实现，直接集成到 MainGameScreen 中
    /// </summary>
    public class BasicQuadtreeOptimization
    {
        private const int MAX_OBJECTS = 10;
        private const int MAX_LEVELS = 5;
        
        private int _level;
        private List<Troop> _objects;
        private Rectangle _bounds;
        private BasicQuadtreeOptimization[] _nodes;

        public BasicQuadtreeOptimization(int level, Rectangle bounds)
        {
            _level = level;
            _bounds = bounds;
            _objects = new List<Troop>();
            _nodes = new BasicQuadtreeOptimization[4];
        }

        public void Clear()
        {
            _objects.Clear();
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].Clear();
                    _nodes[i] = null;
                }
            }
        }

        private void Split()
        {
            int subWidth = _bounds.Width / 2;
            int subHeight = _bounds.Height / 2;
            int x = _bounds.X;
            int y = _bounds.Y;

            _nodes[0] = new BasicQuadtreeOptimization(_level + 1, new Rectangle(x + subWidth, y, subWidth, subHeight));
            _nodes[1] = new BasicQuadtreeOptimization(_level + 1, new Rectangle(x, y, subWidth, subHeight));
            _nodes[2] = new BasicQuadtreeOptimization(_level + 1, new Rectangle(x, y + subHeight, subWidth, subHeight));
            _nodes[3] = new BasicQuadtreeOptimization(_level + 1, new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight));
        }

        private int GetIndex(Rectangle pRect)
        {
            int index = -1;
            double verticalMidpoint = _bounds.X + (_bounds.Width / 2.0);
            double horizontalMidpoint = _bounds.Y + (_bounds.Height / 2.0);

            bool topQuadrant = (pRect.Y < horizontalMidpoint && pRect.Y + pRect.Height < horizontalMidpoint);
            bool bottomQuadrant = (pRect.Y > horizontalMidpoint);

            if (pRect.X < verticalMidpoint && pRect.X + pRect.Width < verticalMidpoint)
            {
                if (topQuadrant) index = 1;
                else if (bottomQuadrant) index = 2;
            }
            else if (pRect.X > verticalMidpoint)
            {
                if (topQuadrant) index = 0;
                else if (bottomQuadrant) index = 3;
            }

            return index;
        }

        public void Insert(Troop troop)
        {
            Rectangle troopBounds = GetTroopBounds(troop);

            if (_nodes[0] != null)
            {
                int index = GetIndex(troopBounds);
                if (index != -1)
                {
                    _nodes[index].Insert(troop);
                    return;
                }
            }

            _objects.Add(troop);

            if (_objects.Count > MAX_OBJECTS && _level < MAX_LEVELS)
            {
                if (_nodes[0] == null) Split();

                int i = 0;
                while (i < _objects.Count)
                {
                    Rectangle objBounds = GetTroopBounds(_objects[i]);
                    int index = GetIndex(objBounds);
                    if (index != -1)
                    {
                        Troop t = _objects[i];
                        _objects.RemoveAt(i);
                        _nodes[index].Insert(t);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        public void Retrieve(List<Troop> returnObjects, Rectangle rect)
        {
            int index = GetIndex(rect);
            
            if (index != -1 && _nodes[0] != null)
            {
                _nodes[index].Retrieve(returnObjects, rect);
            }
            else if (_nodes[0] != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    _nodes[i].Retrieve(returnObjects, rect);
                }
            }

            returnObjects.AddRange(_objects);
        }

        private Rectangle GetTroopBounds(Troop troop)
        {
            const int tileWidth = 60;
            const int tileHeight = 40;
            
            int worldX = troop.Position.X * tileWidth;
            int worldY = troop.Position.Y * tileHeight;
            
            return new Rectangle(worldX, worldY, tileWidth, tileHeight);
        }
    }

    /// <summary>
    /// 基础优化渲染器，直接在 MainGameScreen 中使用
    /// </summary>
    public static class BasicTroopRenderer
    {
        private static List<Troop> _visibleTroops = new List<Troop>();

        public static void DrawOptimized(Point viewportSize, GameTime gameTime, BasicQuadtreeOptimization quadtree)
        {
            if (!Setting.Current.GlobalVariables.DrawTroopAnimation)
            {
                return;
            }

            // 获取可见区域
            Rectangle visibleArea = GetVisibleArea();
            
            // 使用四叉树查询
            _visibleTroops.Clear();
            int totalTroops = Session.Current.Scenario.Troops.Count;
            
            if (quadtree != null)
            {
                quadtree.Retrieve(_visibleTroops, visibleArea);
            }
            else
            {
                _visibleTroops.AddRange(Session.Current.Scenario.Troops.GetList());
            }
            
            int culledTroops = totalTroops - _visibleTroops.Count;

            // 绘制部队
            foreach (Troop troop in _visibleTroops)
            {
                if (troop.Destroyed || !troop.DrawAnimation)
                {
                    continue;
                }

                if (!Session.MainGame.mainGameScreen.mainMapLayer.TileInScreen(troop.Position))
                {
                    continue;
                }

                if (!IsVisible(troop))
                {
                    troop.SetNotShowing();
                    continue;
                }

                DrawSingleTroop(troop, gameTime);
            }

            // 调试输出
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine($"[BasicTroopRenderer] 总计: {totalTroops}, 查询: {_visibleTroops.Count}, 剔除: {culledTroops}");
            }
        }

        private static Rectangle GetVisibleArea()
        {
            var mainGameScreen = Session.MainGame.mainGameScreen;
            if (mainGameScreen != null)
            {
                try
                {
                    return mainGameScreen.GetVisibleArea(Session.MainGame.SpriteScale2);
                }
                catch
                {
                    // 回退到手动计算
                }
            }

            // 手动计算可见区域
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
            
            visibleArea.Inflate(100, 100);
            return visibleArea;
        }

        private static bool IsVisible(Troop troop)
        {
            return (Session.GlobalVariables.SkyEye || 
                    Session.Current.Scenario.NoCurrentPlayer || 
                    Session.Current.Scenario.CurrentPlayer.IsFriendly(troop.BelongedFaction) || 
                    Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) &&
                   (Session.GlobalVariables.SkyEye || 
                    Session.Current.Scenario.CurrentPlayer == null || 
                    troop.Status != TroopStatus.埋伏 || 
                    troop.IsFriendly(Session.Current.Scenario.CurrentPlayer));
        }

        private static void DrawSingleTroop(Troop troop, GameTime gameTime)
        {
            Color troopColor = Color.White;
            if (troop.CurrentOutburstKind == OutburstKind.愤怒)
            {
                troopColor = Color.Red;
            }
            else if (troop.CurrentOutburstKind == OutburstKind.沉静)
            {
                troopColor = Color.Green;
            }

            if (troop.TileAnimation.FrameCount == 0)
            {
                troop.TileAnimation.FrameCount = 1;
            }

            try
            {
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BasicTroopRenderer] 绘制部队出错: {ex.Message}");
            }
        }
    }
}