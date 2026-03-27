using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms;
using Microsoft.Xna.Framework.Graphics;
using GameObjects.MapDetail;
using GameFreeText;
using System.Threading;
using System.IO;
using Platforms;
using GameManager;
using WorldOfTheThreeKingdoms.Helpers;
using System.Runtime.InteropServices; // 用于高性能 Span 操作

namespace WorldOfTheThreeKingdoms.GameScreens.ScreenLayers
{
    public class MainMapLayer
    {
        //private Texture2D BackgroundMap;
        //private beijingtupian beijingtupian = new beijingtupian();
        //private Texture2D BackgroundMap1;
        //private Texture2D BackgroundMap2;
        //private Texture2D BackgroundMap3;
        //private Texture2D BackgroundMap4;

        bool drawBlackWhenNoneTexture = true;

        public List<Tile> DisplayingTiles = new List<Tile>();

        public List<MapTile> DisplayingMapTiles = new List<MapTile>();
        
        private int leftEdge = 0;

        private List<int> TerrainList = new List<int>();
        public Tile[,] Tiles;
        public MapTile[,] MapTiles;
        public int tileWidthMax = 100;
        public int tileWidthMin = 30;
        private int topEdge = 0;
        private Rectangle jianzhujuxing = new Rectangle();
        private Rectangle qizijuxing = new Rectangle();
        internal bool xianshidituxiaokuai = true;
        
        // 缓存邻居索引偏移量，避免重复创建数组
        private readonly int[] _neighborOffsets = { -1, +1, -30, +30, -31, -29, +29, +31 };

        private void CheckMapTileTexture(MapTile maptile)
        {
            // 🔥 GPU设备丢失检查
            if (CacheManager.IsDeviceLost) return;

            if (maptile.TileTexture == null)
            {
                try
                {
                    if (Session.Current?.Scenario?.ScenarioMap?.MapName == null)
                    {
                        // System.Diagnostics.Debug.WriteLine("[MainMapLayer] CheckMapTileTexture: ScenarioMap.MapName为null");
                        return;
                    }

                    string mapName = Session.Current.Scenario.ScenarioMap.MapName;
                    
                    // 路径优化：使用 Path.Combine 兼容不同操作系统
                    string mapDir = Path.Combine("Content", "Textures", "Resources", "ditu", mapName);
                    
                    // 修复：如果带下划线的目录不存在，尝试不带下划线的目录
                    if (!Directory.Exists(mapDir) && mapName.StartsWith('_'))
                    {
                        string alternativeMapName = mapName.Substring(1); 
                        string alternativeMapDir = Path.Combine("Content", "Textures", "Resources", "ditu", alternativeMapName);
                        if (Directory.Exists(alternativeMapDir))
                        {
                            mapName = alternativeMapName;
                        }
                    }
                    
                    string basePath = Path.Combine("Content", "Textures", "Resources", "ditu", mapName, maptile.number);

                    // 1. 优先尝试 DDS
                    string ddsPath = basePath + ".dds";
                    if (File.Exists(ddsPath))
                    {
                        maptile.TileTexture = WorldOfTheThreeKingdoms.Helpers.DDSLoader.Load(Platform.GraphicsDevice, ddsPath);
                    }
                    
                    // 2. 尝试 PNG
                    if (maptile.TileTexture == null)
                    {
                        string pngPath = basePath + ".png";
                        if (File.Exists(pngPath))
                        {
                            using (FileStream fs = new FileStream(pngPath, FileMode.Open))
                            {
                                maptile.TileTexture = Texture2D.FromStream(Platform.GraphicsDevice, fs);
                            }
                        }
                    }
                    
                    // 3. 回退到 JPG
                    if (maptile.TileTexture == null)
                    {
                        string jpgPath = basePath + ".jpg";
                        if (File.Exists(jpgPath))
                        {
                            using (FileStream fs = new FileStream(jpgPath, FileMode.Open))
                            {
                                maptile.TileTexture = Texture2D.FromStream(Platform.GraphicsDevice, fs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainMapLayer] 加载异常: {ex.Message}");
                    
                    // 🔥 检测GPU设备移除异常
                    if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
                        ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
                    {
                        CacheManager.MarkDeviceLost(ex);
                        return; 
                    }
                    
                    try
                    {
                        if (Platform.GraphicsDevice != null && !Platform.GraphicsDevice.IsDisposed)
                        {
                             maptile.TileTexture = new Texture2D(Platform.GraphicsDevice, 1, 1);
                             maptile.TileTexture.SetData(new Color[] { Color.White });
                        }
                    }
                    catch { }
                }
            }
        }

        private void CheckTileTexture(Tile tile, out List<PlatformTexture> decorativeTextures)
        {
            decorativeTextures = null;
            
            // 如果 Scenario 未初始化，直接返回，避免崩溃
            if (Session.Current?.Scenario == null) return;

            TerrainDetail terrainDetailByPositionNoCheck = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(tile.Position);
            
            if (terrainDetailByPositionNoCheck.Textures != null && terrainDetailByPositionNoCheck.Textures.BasicTextures.Count != 0)
            {
                int i;
                List<int> list;
                int num12;
                
                // 重置地形列表
                for (i = 0; i < this.TerrainList.Count; i++)
                {
                    this.TerrainList[i] = 0;
                }
                
                TerrainDirection direction = TerrainDirection.None;
                int num2 = 0;

                Point position = new Point(tile.Position.X - 1, tile.Position.Y);
                int leftId = 0;
                TerrainDetail leftDetail = null;
                if (!Session.Current.Scenario.PositionOutOfRange(position))
                {
                    leftDetail = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(position);
                    leftId = leftDetail.ID;
                }
                Point point2 = new Point(tile.Position.X - 1, tile.Position.Y - 1);
                int topLeftId = 0;
                if (!Session.Current.Scenario.PositionOutOfRange(point2))
                {
                    topLeftId = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID;
                }
                Point point3 = new Point(tile.Position.X, tile.Position.Y - 1);
                int topId = 0;
                TerrainDetail top = null;
                if (!Session.Current.Scenario.PositionOutOfRange(point3))
                {
                    top = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point3);
                    topId = top.ID;
                }
                Point point4 = new Point(tile.Position.X + 1, tile.Position.Y - 1);
                int topRightId = 0;
                if (!Session.Current.Scenario.PositionOutOfRange(point4))
                {
                    topRightId = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point4).ID;
                }
                Point point5 = new Point(tile.Position.X + 1, tile.Position.Y);
                int rightId = 0;
                TerrainDetail right = null;
                if (!Session.Current.Scenario.PositionOutOfRange(point5))
                {
                    right = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point5);
                    rightId = right.ID;
                }
                Point point6 = new Point(tile.Position.X + 1, tile.Position.Y + 1);
                int bottomRightId = 0;
                if (!Session.Current.Scenario.PositionOutOfRange(point6))
                {
                    bottomRightId = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point6).ID;
                }
                Point point7 = new Point(tile.Position.X, tile.Position.Y + 1);
                int bottomId = 0;
                TerrainDetail bottom = null;
                if (!Session.Current.Scenario.PositionOutOfRange(point7))
                {
                    bottom = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point7);
                    bottomId = bottom.ID;
                }
                Point point8 = new Point(tile.Position.X - 1, tile.Position.Y + 1);
                int bottomLeftId = 0;
                if (!Session.Current.Scenario.PositionOutOfRange(point8))
                {
                    bottomLeftId = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point8).ID;
                }

                int equalTerrainCnt = 0;
                if ((leftId > 0) && (leftId == terrainDetailByPositionNoCheck.ID)) equalTerrainCnt++;
                if ((topId > 0) && (topId == terrainDetailByPositionNoCheck.ID)) equalTerrainCnt++;
                if ((rightId > 0) && (rightId == terrainDetailByPositionNoCheck.ID)) equalTerrainCnt++;
                if ((bottomId > 0) && (bottomId == terrainDetailByPositionNoCheck.ID)) equalTerrainCnt++;

                if (equalTerrainCnt < 4)
                {
                    if (top != null)
                    {
                        (list = this.TerrainList)[num12 = Session.Current.Scenario.ScenarioMap.MapData[point3.X, point3.Y]] = list[num12] + 1;
                        for (i = 0; i < this.TerrainList.Count; i++)
                        {
                            if ((i != terrainDetailByPositionNoCheck.ID) && ((((this.TerrainList[i] >= 2) && (leftId == i)) && (topLeftId != terrainDetailByPositionNoCheck.ID)) && (topId == i)))
                            {
                                direction = TerrainDirection.TopLeft;
                                num2 = i;
                                break;
                            }
                        }
                    }
                    if (topRightId > 0)
                    {
                        (list = this.TerrainList)[num12 = Session.Current.Scenario.ScenarioMap.MapData[point4.X, point4.Y]] = list[num12] + 1;
                    }
                    if (rightId > 0)
                    {
                        (list = this.TerrainList)[num12 = Session.Current.Scenario.ScenarioMap.MapData[point5.X, point5.Y]] = list[num12] + 1;
                        for (i = 0; i < this.TerrainList.Count; i++)
                        {
                            if (i != terrainDetailByPositionNoCheck.ID)
                            {
                                if (((direction == TerrainDirection.None) && (this.TerrainList[i] >= 2)) && (((topId == i) && (topRightId != terrainDetailByPositionNoCheck.ID)) && (rightId == i)))
                                {
                                    direction = TerrainDirection.TopRight;
                                    num2 = i;
                                }
                                if (this.TerrainList[i] >= 3)
                                {
                                    if ((((leftId == i) && (topId == i)) && ((rightId == i) && (topRightId != terrainDetailByPositionNoCheck.ID))) && (topLeftId != terrainDetailByPositionNoCheck.ID))
                                    {
                                        direction = TerrainDirection.Top;
                                        num2 = i;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    if (bottomRightId > 0)
                    {
                        (list = this.TerrainList)[num12 = Session.Current.Scenario.ScenarioMap.MapData[point6.X, point6.Y]] = list[num12] + 1;
                    }
                    if (bottomId > 0)
                    {
                        (list = this.TerrainList)[num12 = Session.Current.Scenario.ScenarioMap.MapData[point7.X, point7.Y]] = list[num12] + 1;
                        for (i = 0; i < this.TerrainList.Count; i++)
                        {
                            if (i != terrainDetailByPositionNoCheck.ID)
                            {
                                if (((direction == TerrainDirection.None) && (this.TerrainList[i] >= 2)) && (((rightId == i) && (bottomRightId != terrainDetailByPositionNoCheck.ID)) && (bottomId == i)))
                                {
                                    direction = TerrainDirection.BottomRight;
                                    num2 = i;
                                }
                                if ((this.TerrainList[i] >= 3) && ((((topId == i) && (rightId == i)) && ((bottomId == i) && (topRightId != terrainDetailByPositionNoCheck.ID))) && (bottomRightId != terrainDetailByPositionNoCheck.ID)))
                                {
                                    direction = TerrainDirection.Right;
                                    num2 = i;
                                }
                            }
                        }
                    }
                    if (bottomLeftId > 0)
                    {
                        (list = this.TerrainList)[num12 = Session.Current.Scenario.ScenarioMap.MapData[point8.X, point8.Y]] = list[num12] + 1;
                    }
                    if (leftId > 0)
                    {
                        for (i = 0; i < this.TerrainList.Count; i++)
                        {
                            if (i != terrainDetailByPositionNoCheck.ID)
                            {
                                if (((direction == TerrainDirection.None) && (this.TerrainList[i] >= 2)) && (((bottomId == i) && (bottomLeftId != terrainDetailByPositionNoCheck.ID)) && (leftId == i)))
                                {
                                    direction = TerrainDirection.BottomLeft;
                                    num2 = i;
                                }
                                if ((this.TerrainList[i] >= 3) && ((((rightId == i) && (bottomId == i)) && ((leftId == i) && (bottomRightId != terrainDetailByPositionNoCheck.ID))) && (bottomLeftId != terrainDetailByPositionNoCheck.ID)))
                                {
                                    direction = TerrainDirection.Bottom;
                                    num2 = i;
                                }
                                if (((((this.TerrainList[i] >= 4) && (leftId == i)) && ((topId == i) && (rightId == i))) && (bottomId == i)) && ((((topLeftId != terrainDetailByPositionNoCheck.ID) && (topRightId != terrainDetailByPositionNoCheck.ID)) && ((bottomRightId != terrainDetailByPositionNoCheck.ID) && (bottomLeftId != terrainDetailByPositionNoCheck.ID))) || (this.TerrainList[i] >= 7)))
                                {
                                    direction = TerrainDirection.Centre;
                                    num2 = i;
                                    break;
                                }
                            }
                        }
                    }
                    if ((((direction != TerrainDirection.Centre) && (direction != TerrainDirection.Top)) && ((direction != TerrainDirection.Right) && (direction != TerrainDirection.Bottom))) && (topId > 0))
                    {
                        for (i = 0; i < this.TerrainList.Count; i++)
                        {
                            if (((i != terrainDetailByPositionNoCheck.ID) && (this.TerrainList[i] >= 3)) && ((((bottomId == i) && (leftId == i)) && ((topId == i) && (bottomLeftId != terrainDetailByPositionNoCheck.ID))) && (topLeftId != terrainDetailByPositionNoCheck.ID)))
                            {
                                direction = TerrainDirection.Left;
                                num2 = i;
                            }
                        }
                    }
                    decorativeTextures = new List<PlatformTexture>();
                    switch (direction)
                    {
                        case TerrainDirection.Top:
                            decorativeTextures.Add(top.Textures.TopTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % top.Textures.TopTextures.Count]);
                            if ((((bottom != null) && (bottom.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomRightId != terrainDetailByPositionNoCheck.ID) && (bottomLeftId != terrainDetailByPositionNoCheck.ID))) && (bottom.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(bottom.Textures.BottomEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % bottom.Textures.BottomEdgeTextures.Count]);
                            }
                            return;

                        case TerrainDirection.Left:
                            decorativeTextures.Add(leftDetail.Textures.LeftTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.LeftTextures.Count]);
                            if ((((right != null) && (right.ID != terrainDetailByPositionNoCheck.ID)) && ((topRightId != terrainDetailByPositionNoCheck.ID) && (bottomRightId != terrainDetailByPositionNoCheck.ID))) && (right.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(right.Textures.RightEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % right.Textures.RightEdgeTextures.Count]);
                            }
                            return;

                        case TerrainDirection.Right:
                            decorativeTextures.Add(right.Textures.RightTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % right.Textures.RightTextures.Count]);
                            if ((((leftDetail != null) && (leftDetail.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomLeftId != terrainDetailByPositionNoCheck.ID) && (topLeftId != terrainDetailByPositionNoCheck.ID))) && (leftDetail.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(leftDetail.Textures.LeftEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.LeftEdgeTextures.Count]);
                            }
                            return;

                        case TerrainDirection.Bottom:
                            decorativeTextures.Add(bottom.Textures.BottomTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % bottom.Textures.BottomTextures.Count]);
                            if ((((top != null) && (top.ID != terrainDetailByPositionNoCheck.ID)) && ((topLeftId != terrainDetailByPositionNoCheck.ID) && (topRightId != terrainDetailByPositionNoCheck.ID))) && (top.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(top.Textures.TopEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % top.Textures.TopEdgeTextures.Count]);
                            }
                            return;

                        case TerrainDirection.TopLeft:
                            decorativeTextures.Add(leftDetail.Textures.TopLeftCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.TopLeftCornerTextures.Count]);
                            if ((((right == null) || (terrainDetailByPositionNoCheck.ID == bottomRightId)) || (rightId != bottomId)) || (terrainDetailByPositionNoCheck.ID == rightId))
                            {
                                if ((((right != null) && (right.ID != terrainDetailByPositionNoCheck.ID)) && ((topRightId != terrainDetailByPositionNoCheck.ID) && (bottomRightId != terrainDetailByPositionNoCheck.ID))) && (right.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(right.Textures.RightEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % right.Textures.RightEdgeTextures.Count]);
                                }
                                if ((((bottom != null) && (bottom.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomRightId != terrainDetailByPositionNoCheck.ID) && (bottomLeftId != terrainDetailByPositionNoCheck.ID))) && (bottom.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(bottom.Textures.BottomEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % bottom.Textures.BottomEdgeTextures.Count]);
                                }
                                return;
                            }
                            decorativeTextures.Add(right.Textures.BottomRightCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % right.Textures.BottomRightCornerTextures.Count]);
                            return;

                        case TerrainDirection.TopRight:
                            decorativeTextures.Add(top.Textures.TopRightCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % top.Textures.TopRightCornerTextures.Count]);
                            if ((((bottom == null) || (terrainDetailByPositionNoCheck.ID == bottomLeftId)) || (bottomId != leftId)) || (terrainDetailByPositionNoCheck.ID == bottomId))
                            {
                                if ((((leftDetail != null) && (leftDetail.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomLeftId != terrainDetailByPositionNoCheck.ID) && (topLeftId != terrainDetailByPositionNoCheck.ID))) && (leftDetail.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(leftDetail.Textures.LeftEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.LeftEdgeTextures.Count]);
                                }
                                if ((((bottom != null) && (bottom.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomRightId != terrainDetailByPositionNoCheck.ID) && (bottomLeftId != terrainDetailByPositionNoCheck.ID))) && (bottom.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(bottom.Textures.BottomEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % bottom.Textures.BottomEdgeTextures.Count]);
                                }
                                return;
                            }
                            decorativeTextures.Add(bottom.Textures.BottomLeftCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % bottom.Textures.BottomLeftCornerTextures.Count]);
                            return;

                        case TerrainDirection.BottomLeft:
                            decorativeTextures.Add(bottom.Textures.BottomLeftCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % bottom.Textures.BottomLeftCornerTextures.Count]);
                            if ((((top == null) || (terrainDetailByPositionNoCheck.ID == topRightId)) || (topId != rightId)) || (terrainDetailByPositionNoCheck.ID == topId))
                            {
                                if ((((top != null) && (top.ID != terrainDetailByPositionNoCheck.ID)) && ((topLeftId != terrainDetailByPositionNoCheck.ID) && (topRightId != terrainDetailByPositionNoCheck.ID))) && (top.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(top.Textures.TopEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % top.Textures.TopEdgeTextures.Count]);
                                }
                                if ((((right != null) && (right.ID != terrainDetailByPositionNoCheck.ID)) && ((topRightId != terrainDetailByPositionNoCheck.ID) && (bottomRightId != terrainDetailByPositionNoCheck.ID))) && (right.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(right.Textures.RightEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % right.Textures.RightEdgeTextures.Count]);
                                }
                                return;
                            }
                            decorativeTextures.Add(top.Textures.TopRightCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % top.Textures.TopRightCornerTextures.Count]);
                            return;

                        case TerrainDirection.BottomRight:
                            decorativeTextures.Add(right.Textures.BottomRightCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % right.Textures.BottomRightCornerTextures.Count]);
                            if ((((leftDetail == null) || (terrainDetailByPositionNoCheck.ID == topLeftId)) || (leftId != topId)) || (terrainDetailByPositionNoCheck.ID == leftId))
                            {
                                if ((((leftDetail != null) && (leftDetail.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomLeftId != terrainDetailByPositionNoCheck.ID) && (topLeftId != terrainDetailByPositionNoCheck.ID))) && (leftDetail.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(leftDetail.Textures.LeftEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.LeftEdgeTextures.Count]);
                                }
                                if ((((top != null) && (top.ID != terrainDetailByPositionNoCheck.ID)) && ((topLeftId != terrainDetailByPositionNoCheck.ID) && (topRightId != terrainDetailByPositionNoCheck.ID))) && (top.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                                {
                                    decorativeTextures.Add(top.Textures.TopEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % top.Textures.TopEdgeTextures.Count]);
                                }
                                return;
                            }
                            decorativeTextures.Add(leftDetail.Textures.TopLeftCornerTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.TopLeftCornerTextures.Count]);
                            return;

                        case TerrainDirection.Centre:
                            decorativeTextures.Add(leftDetail.Textures.CentreTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.CentreTextures.Count]);
                            return;

                        case TerrainDirection.None:
                            if ((((leftDetail != null) && (leftDetail.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomLeftId != terrainDetailByPositionNoCheck.ID) && (topLeftId != terrainDetailByPositionNoCheck.ID))) && (leftDetail.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(leftDetail.Textures.LeftEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % leftDetail.Textures.LeftEdgeTextures.Count]);
                            }
                            if ((((top != null) && (top.ID != terrainDetailByPositionNoCheck.ID)) && ((topLeftId != terrainDetailByPositionNoCheck.ID) && (topRightId != terrainDetailByPositionNoCheck.ID))) && (top.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(top.Textures.TopEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % top.Textures.TopEdgeTextures.Count]);
                            }
                            if ((((right != null) && (right.ID != terrainDetailByPositionNoCheck.ID)) && ((topRightId != terrainDetailByPositionNoCheck.ID) && (bottomRightId != terrainDetailByPositionNoCheck.ID))) && (right.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(right.Textures.RightEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % right.Textures.RightEdgeTextures.Count]);
                            }
                            if ((((bottom != null) && (bottom.ID != terrainDetailByPositionNoCheck.ID)) && ((bottomRightId != terrainDetailByPositionNoCheck.ID) && (bottomLeftId != terrainDetailByPositionNoCheck.ID))) && (bottom.GraphicLayer < terrainDetailByPositionNoCheck.GraphicLayer))
                            {
                                decorativeTextures.Add(bottom.Textures.BottomEdgeTextures[((tile.Position.X * 7) + (tile.Position.Y * 11)) % bottom.Textures.BottomEdgeTextures.Count]);
                            }
                            return;
                    }
                }
            }
        }

        public void freeTilesMemory(bool gc = true, bool clearAll = false)
        {
            if (this.MapTiles != null)
            {
                var nums = GetCurrentViewMapTileNumsAll_HashSet();

                foreach (MapTile maptile in this.MapTiles)
                {
                    if (nums.Contains(int.Parse(maptile.number)) && !clearAll)
                    {
                        continue;
                    }
                    if (maptile.TileTexture != null)
                    {
                        maptile.TileTexture.Dispose();
                        maptile.TileTexture = null;
                    }                  
                }
            }

            CacheManager.Clear(CacheType.Page);
            
            // 🔥 Content.Unload() 已释放 InkBleed effect 和 XuanPaperNoise 纹理
            // 必须重建 _inkRenderer，否则下一帧 DrawOverlay 会 ObjectDisposedException
            Session.MainGame.mainGameScreen.InitializeInkBleedRenderer();

            if (gc)
            {
                GC.Collect();
            }
        }

        private int[] GetCurrentViewMapTileNums1()
        {
            lock (this.DisplayingMapTiles)
            {
                int count = this.DisplayingMapTiles.Count;
                int[] result = new int[count];
                var span = CollectionsMarshal.AsSpan(this.DisplayingMapTiles);
                for (int i = 0; i < count; i++)
                {
                    result[i] = int.Parse(span[i].number);
                }
                return result;
            }
        }

        private int[] GetCurrentViewMapTileNums2()
        {
            var disNums = GetCurrentViewMapTileNums1();
            
            HashSet<int> resultSet = new HashSet<int>(disNums.Length * 9);

            foreach (var num in disNums)
            {
                resultSet.Add(num);
            }

            HashSet<int> expandSet = new HashSet<int>();
            HashSet<int> currentSet = new HashSet<int>(disNums);

            foreach (var nu in disNums)
            {
                foreach (var offset in _neighborOffsets)
                {
                    int num = nu + offset;
                    // 假设总瓦片数限制在0到899之间
                    if (num >= 0 && num <= 899 && !currentSet.Contains(num))
                    {
                        expandSet.Add(num);
                    }
                }
            }

            return expandSet.ToArray();
        }

        private HashSet<int> GetCurrentViewMapTileNumsAll_HashSet()
        {
            var disNums = GetCurrentViewMapTileNums1();
            HashSet<int> allNums = new HashSet<int>(disNums.Length * 9);

            foreach (var num in disNums)
            {
                allNums.Add(num);
            }

            foreach (var nu in disNums)
            {
                foreach (var offset in _neighborOffsets)
                {
                    int num = nu + offset;
                    if (num >= 0 && num <= 899)
                    {
                        allNums.Add(num);
                    }
                }
            }
            return allNums;
        }

        private int[] GetCurrentViewMapTileNumsAll()
        {
            return GetCurrentViewMapTileNumsAll_HashSet().ToArray();
        }

        public void StopThreads()
        {
            freeTilesMemory();
            if (MapThread1 != null)
            {
                MapThread1.Abort();
                MapThread1 = null;
            }
            if (MapThread2 != null)
            {
                MapThread2.Abort();
                MapThread2 = null;
            }            
        }

        PlatformTask MapThread1;
        PlatformTask MapThread2;

        private void ProcessMapTileTextureSync()
        {
            // 🔥 确保 GraphicsDevice 已初始化后再启动后台线程
            // 这是架构级保护，防止时序错误导致的崩溃
            if (Platform.GraphicsDevice == null)
            {
                return; // GraphicsDevice 未就绪，延迟启动线程
            }

            if (MapThread1 == null)
            {
                MapThread1 = new PlatformTask(() =>
                {
                    while (true)
                    {
                        if (MapThread1 == null || MapThread1.IsStop) break;

                        try
                        {
                            MapTile mapTile = null;

                            lock (this.DisplayingMapTiles)
                            {
                                var span = CollectionsMarshal.AsSpan(this.DisplayingMapTiles);
                                for (int i = 0; i < span.Length; i++)
                                {
                                    if (span[i] != null && span[i].TileTexture == null)
                                    {
                                        mapTile = span[i];
                                        break; 
                                    }
                                }
                            }

                            if (mapTile != null)
                            {
                                CheckMapTileTexture(mapTile);
                            }

                            Platform.Sleep(30);
                        }
                        catch (Exception)
                        {
                            Platform.Sleep(1000);
                        }
                    }
                });
                MapThread1.Start();
            }
            if (MapThread2 == null)
            {
                MapThread2 = new PlatformTask(() =>
                {
                    while (true)
                    {
                        if (MapThread2 == null || MapThread2.IsStop) break;

                        try
                        {
                            MapTile mapTile = null;

                            lock (this.DisplayingMapTiles)
                            {
                                var span = CollectionsMarshal.AsSpan(this.DisplayingMapTiles);
                                bool hasNullTexture = false;
                                for (int i = 0; i < span.Length; i++)
                                {
                                    if (span[i] != null && span[i].TileTexture == null)
                                    {
                                        mapTile = span[i];
                                        hasNullTexture = true;
                                        break;
                                    }
                                }
                                
                                if (hasNullTexture)
                                {
                                    // 仅占位，逻辑保持原样
                                }
                            }

                            if (mapTile != null)
                            {
                                Platform.Sleep(80);
                                continue;
                            }

                            var tileNums2 = GetCurrentViewMapTileNums2();
                            
                            foreach (var num in tileNums2)
                            {
                                var mt = this.MapTiles[num % 30, num / 30];
                                if (mt != null && mt.TileTexture == null)
                                {
                                    mapTile = mt;
                                    break; 
                                }
                            }

                            if (mapTile != null)
                            {
                                CheckMapTileTexture(mapTile);
                            }

                            Platform.Sleep(50);
                        }
                        catch (Exception)
                        {
                            Platform.Sleep(1000);
                        }
                    }
                });
                MapThread2.Start();
            }

        }


        public void Draw(Point viewportSize)
        {
            var spriteBatch = Session.Current?.SpriteBatch;
            if (spriteBatch != null)
            {
                var scenario = Session.Current?.Scenario;
                var scenarioMap = scenario?.ScenarioMap;

                if (scenarioMap?.MapName != null)
                {
                    ProcessMapTileTextureSync();

                    var displaySpan = CollectionsMarshal.AsSpan(this.DisplayingMapTiles);
                    
                    for (int i = 0; i < displaySpan.Length; i++)
                    {
                        MapTile maptile = displaySpan[i];
                        Rectangle? sourceRectangle = null;

                        if (!drawBlackWhenNoneTexture)
                        {
                            this.CheckMapTileTexture(maptile);
                        }

                        if (maptile != null && maptile.TileTexture != null && !maptile.TileTexture.IsDisposed)
                        {
                            spriteBatch.Draw(maptile.TileTexture, maptile.Destination, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                        }
                        
                        // 原代码此处有 CheckTileTexture 的调用，但通常是被注释掉或未使用结果的
                        // 为了性能，默认注释掉，如果您需要开启装饰纹理，请取消下方注释
                        /*
                        List<PlatformTexture> decorativeTextures = null;
                        //this.CheckTileTexture(maptile, out decorativeTextures);
                        if (decorativeTextures != null)
                        {
                            foreach (Texture2D textured in decorativeTextures)
                            {
                                sourceRectangle = null;
                                spriteBatch.Draw(textured, maptile.Destination, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.8998f);
                            }
                        }
                        */
                    }

                    ResetDisplayingTiles(Session.MainGame.mainGameScreen);

                    int mapNum = 0;
                    if (this.MapTiles != null)
                    {
                        int w = MapTiles.GetLength(0);
                        int h = MapTiles.GetLength(1);
                        for (int x = 0; x < w; x++)
                        {
                             for(int y = 0; y < h; y++)
                             {
                                 if (MapTiles[x,y].TileTexture != null) mapNum++;
                             }
                        }
                    }
                    
                    if (mapNum >= 100)
                    {
                        freeTilesMemory(false);
                    }

                    if (Session.MainGame.mainGameScreen.editMode)
                    {
                        var tilesSpan = CollectionsMarshal.AsSpan(this.DisplayingTiles);
                        for (int i = 0; i < tilesSpan.Length; i++)
                        {
                            Tile tile = tilesSpan[i];
                            
                            // 即使在编辑器模式下，CheckTileTexture 的结果在原代码中也未被用于绘制，因此注释以优化性能
                            // List<PlatformTexture> decorativeTextures = null;
                            // this.CheckTileTexture(tile, out decorativeTextures);

                            if (this.xianshidituxiaokuai && scenarioMap.MapData != null && 
                                tile?.Position != null)
                            {
                                int terrainId = scenarioMap.MapData[tile.Position.X, tile.Position.Y];
                                if (terrainId != 0) //未知地形显示为透明
                                {
                                    CacheManager.DrawAvatar(tile.TileTexture.Name, tile.Destination, Color.White, false, true, TextureShape.None, null, 0.8998f);
                                }
                            }
                        }
                    }

                }
                else
                {
                    var tilesSpan = CollectionsMarshal.AsSpan(this.DisplayingTiles);
                    for (int i = 0; i < tilesSpan.Length; i++)
                    {
                        Tile tile = tilesSpan[i];
                        
                        // 如果 TileTexture 为 null，说明初始化失败，这是严重错误
                        if (tile.TileTexture == null)
                        {
                            throw new InvalidOperationException($"Tile at position ({tile.Position.X}, {tile.Position.Y}) has null TileTexture!");
                        }

                        CacheManager.DrawAvatar(tile.TileTexture.Name, tile.Destination, Color.White, false, true, TextureShape.None, null, 0.8998f);
                    }

                }

                if (Session.GlobalVariables.ShowGrid)
                {
                    var tilesSpan = CollectionsMarshal.AsSpan(this.DisplayingTiles);
                    for (int i = 0; i < tilesSpan.Length; i++)
                    {
                        Tile tile = tilesSpan[i];
                        if (Session.MainGame.mainGameScreen.editMode)
                        {
                            CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.EditModeGrid, tile.Destination, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.81f);
                        }
                        else
                        {
                            if (scenarioMap?.MapData != null && tile?.Position != null)
                            {
                                int terrainType = scenarioMap.MapData[tile.Position.X, tile.Position.Y];
                                if (terrainType != 0 && terrainType != 4 && terrainType != 7)
                                {
                                    CacheManager.Draw(Session.MainGame.mainGameScreen.Textures.wanggetupian, tile.Destination, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.81f);
                                }
                            }
                        }
                    }
                }

            }
        }

        public Point GetCurrentScreenCenter(Point viewportSize)
        {
            return new Point(((viewportSize.X / 2) - this.LeftEdge) / this.TileWidth, ((viewportSize.Y / 2) - this.TopEdge) / this.TileHeight);
        }

        public Rectangle GetDestination(Point position)
        {
            return this.Tiles[position.X, position.Y].Destination;
        }

        public Rectangle huoqujianzhujuxing(Point position, global::GameObjects.Architecture jianzhu)
        {
            jianzhujuxing = this.Tiles[position.X, position.Y].Destination;
            int guimo = jianzhu.JianzhuGuimo;
            bool useSimple = Session.Current?.Scenario?.ScenarioMap?.UseSimpleArchImages == true;

            if (jianzhu.Kind.ID != 2 && jianzhu.Kind.ID != 3)
            {
                if (useSimple)
                {
                    jianzhujuxing.X = (position.X) * this.TileWidth + this.LeftEdge;
                    jianzhujuxing.Y = (position.Y) * this.TileHeight + this.TopEdge;
                    jianzhujuxing.Width = this.TileWidth;
                    jianzhujuxing.Height = this.TileHeight;
                }
                else
                {
                    if (jianzhu.Kind.ID != 1 && guimo == 1)
                    {
                        jianzhujuxing.X = position.X * this.TileWidth + this.LeftEdge;
                        jianzhujuxing.Y = position.Y * this.TileHeight + this.TopEdge;
                        jianzhujuxing.Width = this.TileWidth;
                        jianzhujuxing.Height = this.TileHeight;
                    }
                    else if (guimo == 5 || (jianzhu.Kind.ID == 1 && guimo == 1))
                    {
                        jianzhujuxing.X = (position.X - 1) * this.TileWidth + this.LeftEdge;
                        jianzhujuxing.Y = (position.Y - 1) * this.TileHeight + this.TopEdge;
                        jianzhujuxing.Width = this.TileWidth * 3;
                        jianzhujuxing.Height = this.TileHeight * 3;
                    }
                    else if (guimo == 13)
                    {
                        jianzhujuxing.X = (position.X - 2) * this.TileWidth + this.LeftEdge;
                        jianzhujuxing.Y = (position.Y - 2) * this.TileHeight + this.TopEdge;
                        jianzhujuxing.Width = this.TileWidth * 5;
                        jianzhujuxing.Height = this.TileHeight * 5;
                    }
                }
            }
            else if (jianzhu.Kind.ID == 2)
            {
                if (guimo == 1 || useSimple)
                {
                    return jianzhujuxing;
                }
                else if (guimo == 5)
                {
                    if (jianzhu.ArchitectureArea.Area[0].X == jianzhu.ArchitectureArea.Area[1].X)
                    {
                        jianzhujuxing.X = (position.X - 2) * this.TileWidth + this.LeftEdge;
                        jianzhujuxing.Y = (position.Y - 3) * this.TileHeight + this.TopEdge;
                        jianzhujuxing.Width = this.TileWidth * 5;
                        jianzhujuxing.Height = this.TileHeight * 7;

                    }
                    else
                    {
                        jianzhujuxing.X = (position.X - 4) * this.TileWidth + this.LeftEdge;
                        jianzhujuxing.Y = (position.Y - 1) * this.TileHeight + this.TopEdge;
                        jianzhujuxing.Width = this.TileWidth * 9;
                        jianzhujuxing.Height = this.TileHeight * 3;
                    }
                }
                else if (guimo == 3)
                {
                    if (jianzhu.ArchitectureArea.Area[0].X == jianzhu.ArchitectureArea.Area[1].X)
                    {
                        jianzhujuxing.X = (position.X - 2) * this.TileWidth + this.LeftEdge;
                        jianzhujuxing.Y = (position.Y - 2) * this.TileHeight + this.TopEdge;
                        jianzhujuxing.Width = this.TileWidth * 5;
                        jianzhujuxing.Height = this.TileHeight * 5;
                    }
                    else
                    {
                        jianzhujuxing.X = (position.X - 2) * this.TileWidth + this.LeftEdge;
                        jianzhujuxing.Y = (position.Y - 1) * this.TileHeight + this.TopEdge;
                        jianzhujuxing.Width = this.TileWidth * 5;
                        jianzhujuxing.Height = this.TileHeight * 3;
                    }
                }
            }
            return jianzhujuxing;
        }

        public Rectangle huoquqizijuxing(Point position)
        {
            qizijuxing = this.Tiles[position.X, position.Y].Destination;
            qizijuxing.X += (int)(qizijuxing.Width * 0.2);
            qizijuxing.Y += (int)(qizijuxing.Height * 0.54);
            qizijuxing.Width = (int)(qizijuxing.Width * 0.5);
            qizijuxing.Height = (int)(qizijuxing.Height * 0.4);
            return qizijuxing;
        }

        internal Point GetCenterCoordinate(Point point)
        {
            return new Point(
                this.leftEdge + (point.X * this.TileWidth) + this.TileWidth / 2,
                this.topEdge + (point.Y * this.TileHeight) + this.TileHeight / 2
            );
        }

        public Rectangle GetHalfDestination(Point position)
        {
            var dest = this.Tiles[position.X, position.Y].Destination;
            return new Rectangle(
                dest.X + (dest.Width / 4), 
                dest.Y + (dest.Height / 4), 
                dest.Width / 2, 
                dest.Height / 2
            );
        }

        public string GetTerrainNameByPosition(Point position)
        {
            return Session.Current.Scenario.GetTerrainNameByPosition(position);
        }

        public Rectangle GetThreeFourthsDestination(Point position)
        {
            var dest = this.Tiles[position.X, position.Y].Destination;
            return new Rectangle(
                dest.X + (dest.Width / 8), 
                dest.Y + (dest.Height / 8), 
                (dest.Width * 3) / 4, 
                (dest.Height * 3) / 4
            );
        }

        public Point GetTopCenterPoint(Point position)
        {
            return new Point(((position.X * this.TileWidth) + (this.TileWidth / 2)) + this.leftEdge, (position.Y * this.TileHeight) + this.topEdge);
        }

        public void Initialize()
        {
            this.TerrainList.Clear();
            int terrainCount = Enum.GetValues<TerrainKind>().Length;
            if(this.TerrainList.Capacity < terrainCount)
            {
                this.TerrainList.Capacity = terrainCount;
            }
            for (int i = 0; i < terrainCount; i++)
            {
                this.TerrainList.Add(0);
            }
        }

        public void PrepareMap()
        {
            if (Session.Current?.Scenario?.ScenarioMap == null)
            {
                // System.Diagnostics.Debug.WriteLine("[MainMapLayer] PrepareMap: ScenarioMap为null");
                return;
            }

            var mapDim = Session.Current.Scenario.ScenarioMap.MapDimensions;
            this.Tiles = new Tile[mapDim.X, mapDim.Y];
            
            for (int i = 0; i < mapDim.X; i++)
            {
                for (int j = 0; j < mapDim.Y; j++)
                {
                    var tile = new Tile();
                    tile.Position = new Point(i, j);
                    this.Tiles[i, j] = tile;
                    
                    TerrainDetail detail = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(tile.Position);
                    if (detail?.Textures?.BasicTextures?.Count > 0)
                    {
                        tile.TileTexture = detail.Textures.BasicTextures[((i * 7) + (j * 11)) % detail.Textures.BasicTextures.Count];
                    }
                    else
                    {
                        throw new InvalidOperationException($"无法获取位置 ({i},{j}) 的地形纹理！detail={detail}, Textures={detail?.Textures}, BasicTextures.Count={detail?.Textures?.BasicTextures?.Count ?? 0}");
                    }
                }
            }

            // 🔥 地图贴图数量修复：yueluo_1.0 地图有 900 个 DDS 文件（30x30）
            // 不能依赖 ScenarioMap.NumberOfTiles，因为它可能不准确
            int tileCount = 30;  // 固定为 30x30 = 900 个贴图
            this.MapTiles = new MapTile[tileCount, tileCount];
            
            /*
            System.Diagnostics.Debug.WriteLine($"[InitializeMapTiles] 地图贴图数量: {tileCount}x{tileCount} = {tileCount * tileCount}");
            System.Diagnostics.Debug.WriteLine($"[InitializeMapTiles] 贴图编号计算方式: 行优先 (i + j * tileCount)");
            System.Diagnostics.Debug.WriteLine($"  位置(0,0) → 编号: {0 + 0 * tileCount}");
            System.Diagnostics.Debug.WriteLine($"  位置(1,0) → 编号: {1 + 0 * tileCount}");
            System.Diagnostics.Debug.WriteLine($"  位置(0,1) → 编号: {0 + 1 * tileCount}");
            System.Diagnostics.Debug.WriteLine($"  位置(29,0) → 编号: {29 + 0 * tileCount}");
            System.Diagnostics.Debug.WriteLine($"  位置(0,29) → 编号: {0 + 29 * tileCount}");
            */
            
            // 🔥 关键修复：地图贴图编号计算
            // 贴图文件排列方式：行优先（从左到右，从上到下）
            // number = i + j * tileCount
            // 
            // 30x30 地图示例：
            // (0,0)=0   (1,0)=1   (2,0)=2   ... (29,0)=29
            // (0,1)=30  (1,1)=31  (2,1)=32  ... (29,1)=59
            // (0,2)=60  (1,2)=61  (2,2)=62  ... (29,2)=89
            for (int i = 0; i < tileCount; i++)
            {
                for (int j = 0; j < tileCount; j++)
                {
                    this.MapTiles[i, j] = new MapTile
                    {
                        Position = new Point(i, j),
                        number = (i + j * tileCount).ToString()  // 🔥 行优先排列
                    };
                }
            }
            
            // 输出前几个贴图的编号用于验证
            /*
            System.Diagnostics.Debug.WriteLine($"[InitializeMapTiles] 前10个贴图编号:");
            for (int i = 0; i < Math.Min(5, tileCount); i++)
            {
                for (int j = 0; j < Math.Min(2, tileCount); j++)
                {
                    System.Diagnostics.Debug.WriteLine($"  MapTiles[{i},{j}].number = {this.MapTiles[i, j].number}");
                }
            }
            */
        }

        public void ReCalculateTileDestination(MainGameScreen screen)
        {
            this.ResetDisplayingTiles(screen);

            var tilesSpan = CollectionsMarshal.AsSpan(this.DisplayingTiles);
            for (int i = 0; i < tilesSpan.Length; i++)
            {
                Tile tile = tilesSpan[i];
                tile.Destination.X = this.leftEdge + (tile.Position.X * this.TileWidth);
                tile.Destination.Y = this.topEdge + (tile.Position.Y * this.TileHeight);
                tile.Destination.Width = this.TileWidth;
                tile.Destination.Height = this.TileHeight;
            }

            var scenarioMap = Session.Current?.Scenario?.ScenarioMap;
            if (scenarioMap != null)
            {
                int squares = scenarioMap.NumberOfSquaresInEachTile;
                int w = this.TileWidth * squares;
                int h = this.TileHeight * squares;

                var mapTilesSpan = CollectionsMarshal.AsSpan(this.DisplayingMapTiles);
                for (int i = 0; i < mapTilesSpan.Length; i++)
                {
                    MapTile maptile = mapTilesSpan[i];
                    maptile.Destination.X = this.leftEdge + (maptile.Position.X * w);
                    maptile.Destination.Y = this.topEdge + (maptile.Position.Y * h);
                    maptile.Destination.Width = w;
                    maptile.Destination.Height = h;
                }
            }
        }

        public void ResetDisplayingTiles(MainGameScreen screen)
        {
            var scenarioMap = Session.Current?.Scenario?.ScenarioMap;
            if (scenarioMap == null) return;

            if (this.Tiles != null)
            {
                this.DisplayingTiles.Clear();
                int estWidth = (screen.BottomRightPosition.X - screen.TopLeftPosition.X) + 1;
                int estHeight = (screen.BottomRightPosition.Y - screen.TopLeftPosition.Y) + 1;
                if (estWidth > 0 && estHeight > 0)
                {
                    int estimatedCount = estWidth * estHeight;
                    if (this.DisplayingTiles.Capacity < estimatedCount)
                        this.DisplayingTiles.Capacity = estimatedCount;
                }

                int mapDimX = scenarioMap.MapDimensions.X;
                int mapDimY = scenarioMap.MapDimensions.Y;

                for (int i = screen.TopLeftPosition.X; i <= screen.BottomRightPosition.X; i++)
                {
                    for (int j = screen.TopLeftPosition.Y; j <= screen.BottomRightPosition.Y; j++)
                    {
                        if (i >= 0 && i < mapDimX && j >= 0 && j < mapDimY)
                        {
                            this.DisplayingTiles.Add(this.Tiles[i, j]);
                        }
                    }
                }
            }

            if (this.MapTiles != null)
            {
                lock (this.DisplayingMapTiles)
                {
                    this.DisplayingMapTiles.Clear();
                    int squares = scenarioMap.NumberOfSquaresInEachTile;
                    int limitX = screen.BottomRightPosition.X + squares;
                    int limitY = screen.BottomRightPosition.Y + squares;
                    int mapDimX = scenarioMap.MapDimensions.X;
                    int mapDimY = scenarioMap.MapDimensions.Y;

                    for (int i = screen.TopLeftPosition.X; i <= limitX; i += squares)
                    {
                        for (int j = screen.TopLeftPosition.Y; j <= limitY; j += squares)
                        {
                            if (i >= 0 && i < mapDimX && j >= 0 && j < mapDimY)
                            {
                                this.DisplayingMapTiles.Add(this.MapTiles[i / squares, j / squares]);
                            }
                        }
                    }
                }
            }
        }

        public bool TileInScreen(Point tile) => Session.MainGame.mainGameScreen.TileInScreen(tile);

        public Point TranslateCoordinateToTilePosition(int coordinateX, int coordinateY)
        {
            int num = coordinateX - this.leftEdge;
            int num2 = coordinateY - this.topEdge;
            return new Point(num / this.TileWidth, num2 / this.TileHeight);
        }

        public int LeftEdge
        {
            get => this.leftEdge;
            set => this.leftEdge = value;
        }

        public int TileHeight
        {
            get => Session.Current?.Scenario?.ScenarioMap?.TileHeight ?? 40;
            set
            {
                var map = Session.Current?.Scenario?.ScenarioMap;
                if (map == null) return;
                
                map.TileHeight = Math.Clamp(value, map.TileWidthMin, map.TileWidthMax);
            }
        }

        public int TileWidth
        {
            get => Session.Current?.Scenario?.ScenarioMap?.TileWidth ?? 60;
            set
            {
                var map = Session.Current?.Scenario?.ScenarioMap;
                if (map == null) return;

                map.TileWidth = Math.Clamp(value, map.TileWidthMin, map.TileWidthMax);
            }
        }

        public int TopEdge
        {
            get => this.topEdge;
            set => this.topEdge = value;
        }

        public int RightEdge
        {
            get
            {
                // 🔥 ANTI-BAND-AID：不使用 ?. 掩盖初始化错误
                // 如果 GraphicsDevice 为 null，说明初始化顺序有问题，应该让异常抛出
                return this.leftEdge + Platform.GraphicsDevice.Viewport.Width;
            }
        }

        public int BottomEdge
        {
            get
            {
                // 🔥 ANTI-BAND-AID：不使用 ?. 掩盖初始化错误
                return this.topEdge + Platform.GraphicsDevice.Viewport.Height;
            }
        }

        public Point TotalMapSize => new Point(this.TotalTileWidth, this.TotalTileHeight);

        public int TotalTileHeight => Session.Current?.Scenario?.ScenarioMap?.TotalTileHeight ?? 800;

        public int TotalTileWidth => Session.Current?.Scenario?.ScenarioMap?.TotalTileWidth ?? 1200;

        public void chongsheditukuaitupian(int i, int j)
        {
            var tile = new Tile();
            tile.Position = new Point(i, j);
            this.Tiles[i, j] = tile;
            
            TerrainDetail detail = Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(tile.Position);
            if (detail != null)
            {
                if (detail.Textures.BasicTextures.Count > 0)
                {
                    tile.TileTexture = detail.Textures.BasicTextures[((i * 7) + (j * 11)) % detail.Textures.BasicTextures.Count];
                }
                else
                {
                    tile.TileTexture = Session.MainGame.mainGameScreen.Textures.TerrainTextures[Session.Current.Scenario.ScenarioMap.MapData[i, j]];
                }
            }
            this.ReCalculateTileDestination(Session.MainGame.mainGameScreen);
        }

        internal void jiazaibeijingtupian()
        {
            // 旧代码保留
            /*
            if (this.BackgroundMap != null)
            {
                return;
                //this.BackgroundMap.Dispose();
                
            }
            //s=this.beijingtupian.BitmapToMemoryStream(device, bm);
            //bm.Dispose();
            //this.BackgroundMap = this.beijingtupian.huoqupingmutuxing(-this.LeftEdge, -this.TopEdge, Session.MainGame.mainGameScreen.viewportSize.X, Session.MainGame.mainGameScreen.viewportSize.Y,device );
            this.BackgroundMap = Texture2D.FromFile(device, "Content/Textures/Resources/ditu/" + Session.Current.Scenario.ScenarioMap.dituwenjian);
            */
        }
    }
}
