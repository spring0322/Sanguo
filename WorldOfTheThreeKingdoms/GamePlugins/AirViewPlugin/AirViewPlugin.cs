using System;
using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks; // 用于异步
using AirViewPlugin;
using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PluginInterface;
using PluginInterface.BaseInterface;
using WorldOfTheThreeKingdoms;
using Platforms;
using PersonPortraitPlugin;
using GameManager;

//using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;

//using Microsoft.Xna.Framework.Input;
//using Microsoft.Xna.Framework.Media;
//using Microsoft.Xna.Framework.Net;
//using Microsoft.Xna.Framework.Storage;

namespace AirViewPlugin
{
    public class AirViewPlugin : GameObject, IAirView, IBasePlugin, IPluginXML, IPluginGraphics, IScreenDisableRects
    {
        private AirView airView = new AirView();
#pragma warning disable CS0169 // The field 'AirViewPlugin.architectureImage' is never used
        private Image architectureImage;
#pragma warning restore CS0169 // The field 'AirViewPlugin.architectureImage' is never used
        private string author = "clip_on";
        private const string DataPath = @"Content\Textures\GameComponents\AirView\Data\";
        private string description = "微缩地图";
        private const string Path = @"Content\Textures\GameComponents\AirView\";
        private string pluginName = "AirViewPlugin";
        private List<Image> TerrainImages = new List<Image>();
        private Image troopFriendlyImage;
        private Image troopHostileImage;
        private Image troopImage;
        private string version = "1.0.0";
        private const string XMLFilename = "AirViewData.xml";
        
        // 🔥 新增：跟踪是否使用了降级方案（2026-03-17）
        private bool _usingFallbackMinimap = false;
        
        // 🔥 新增：公开方法检查是否使用了降级方案（2026-03-17）
        public bool IsUsingFallbackMinimap() => _usingFallbackMinimap;
        
        // 新增：战略小地图相关字段
        private Texture2D AirViewImage;
        private Rectangle DisplayRect;
        
        // 🎯 新增：地形缓存优化
        private static Texture2D _cachedTerrainLayer;
        private static string _cachedMapName;
        private static bool _terrainCacheDirty = true;

        public void AddDisableRects()
        {
            this.airView.AddDisableRects();
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
        }

        public void Draw(GameTime gameTime)
        {
            this.airView.Draw(gameTime);
        }

        public void Initialize(Screen screen)
        {
            for (int i = 0; i < Enum.GetValues<TerrainKind>().Length; i++)
            {
                string filename = "Content/Textures/Resources/Terrain/" + i.ToString() + "/Basic01.png";
                this.TerrainImages.Add(Image.FromFile(filename));
            }
        }

        public void LoadDataFromXMLDocument(string filename)
        {
            Font font;
            Microsoft.Xna.Framework.Color color;
            XmlDocument document = new XmlDocument();

            string xml = Platform.Current.LoadText(filename);
            document.LoadXml(xml);

            XmlNode nextSibling = document.FirstChild.NextSibling;
            XmlNode node = nextSibling.ChildNodes.Item(0);
            this.airView.Align = Enum.Parse<ToolAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.airView.Width = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            node = nextSibling.ChildNodes.Item(1);
            this.airView.ToolTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.airView.ToolSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.airView.ToolDisplayTexture = this.airView.ToolTexture;
            this.airView.ToolPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            node = nextSibling.ChildNodes.Item(2);
            this.airView.Transparent = float.Parse(node.Attributes.GetNamedItem("Transparent").Value);
            this.airView.MapShowPosition = (ShowPosition)Enum.Parse(typeof(ShowPosition), node.Attributes.GetNamedItem("Position").Value);
            this.airView.MapMaxWidth = int.Parse(node.Attributes.GetNamedItem("MaxWidth").Value);
            this.airView.MapMaxHeight = int.Parse(node.Attributes.GetNamedItem("MaxHeight").Value);
            this.airView.DefaultTileLength = int.Parse(node.Attributes.GetNamedItem("TileLength").Value);
            this.airView.TileLength = this.airView.DefaultTileLength;
            this.airView.TileLengthMax = int.Parse(node.Attributes.GetNamedItem("TileLengthMax").Value);
            node = nextSibling.ChildNodes.Item(3);
            this.airView.FrameTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            node = nextSibling.ChildNodes.Item(4);
            this.airView.ArchitectureUnitTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            node = nextSibling.ChildNodes.Item(5);
            this.troopImage = Image.FromFile(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.troopFriendlyImage = Image.FromFile(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("Friendly").Value);
            this.troopHostileImage = Image.FromFile(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("Hostile").Value);
            this.airView.TroopFactionColorTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            node = nextSibling.ChildNodes.Item(6);
            this.airView.ConmentBackgroundTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.airView.Conment = new FreeText(font, color);
            this.airView.Conment.Position = StaticMethods.LoadRectangleFromXMLNode(node);
            this.airView.Conment.Align = (TextAlign)Enum.Parse(typeof(TextAlign), node.Attributes.GetNamedItem("Align").Value);

            node = nextSibling.ChildNodes.Item(7);
            this.airView.TroopToolTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.airView.TroopToolSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.airView.TroopToolDisplayTexture = this.airView.TroopToolSelectedTexture;
            this.airView.TroopToolPosition = StaticMethods.LoadRectangleFromXMLNode(node);
        }
        
        public void ReloadAirView()
        {
            //if (this.airView.MapTexture != null)
            //{
            //    this.airView.MapTexture.Dispose();
            //    this.airView.MapTexture = null;
            //}

            //待處理
            //Bitmap image = new Bitmap(this.airView.scenario.ScenarioMap.MapDimensions.X * this.airView.TileLength, this.airView.scenario.ScenarioMap.MapDimensions.Y * this.airView.TileLength, PixelFormat.Format32bppArgb);
            //Graphics graphics = Graphics.FromImage(image);
            //graphics.Clear(System.Drawing.Color.White);
            //for (int i = 0; i < this.airView.scenario.ScenarioMap.MapDimensions.X; i++)
            //{
            //    for (int j = 0; j < this.airView.scenario.ScenarioMap.MapDimensions.Y; j++)
            //    {
            //        graphics.DrawImage(this.TerrainImages[this.airView.scenario.ScenarioMap.MapData[i, j]], new System.Drawing.Rectangle(i * this.airView.TileLength, j * this.airView.TileLength, this.airView.TileLength, this.airView.TileLength));
            //    }
            //}

            //try
            //{
            //    image.Save(@"Content\Textures\GameComponents\AirView\~tmp.image");
            //    this.airView.MapTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\~tmp.image");
            //    File.Delete(@"Content\Textures\GameComponents\AirView\~tmp.image");
            //}
            //catch
            //{
            //    this.airView.MapTexture = null;
            //}
            //this.ReloadTroopView();
        }
        
        public void ReloadAirView(string dituwenjian)
        {
            if (Session.Current?.Scenario != null)
            {
                CreateStrategicMinimap(Session.Current.Scenario);
            }
        }

        //public void ReloadArchitectureView()  //以前的代码，现在已不用
        //{
        //    if (this.airView.ArchitectureTexture != null)
        //    {
        //        this.airView.ArchitectureTexture.Dispose();
        //        this.airView.ArchitectureTexture = null;
        //    }
        //    Bitmap image = new Bitmap(this.airView.scenario.ScenarioMap.MapDimensions.X * this.airView.TileLength, this.airView.scenario.ScenarioMap.MapDimensions.Y * this.airView.TileLength, PixelFormat.Format32bppArgb);
        //    Graphics graphics = Graphics.FromImage(image);
        //    graphics.Clear(System.Drawing.Color.Transparent);
        //    for (int i = 0; i < this.airView.scenario.ScenarioMap.MapDimensions.X; i++)
        //    {
        //        for (int j = 0; j < this.airView.scenario.ScenarioMap.MapDimensions.Y; j++)
        //        {
        //            if (this.airView.scenario.PositionIsArchitecture(new Microsoft.Xna.Framework.Point(i, j)))
        //            {
        //                graphics.DrawImage(this.architectureImage, new System.Drawing.Rectangle(i * this.airView.TileLength, j * this.airView.TileLength, this.airView.TileLength * 4, this.airView.TileLength * 4));
        //            }
        //        }
        //    }
        //    try
        //    {
        //        image.Save(@"Content\Textures\GameComponents\AirView\~tmp.image");
        //        this.airView.ArchitectureTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\~tmp.image");
        //        File.Delete(@"Content\Textures\GameComponents\AirView\~tmp.image");
        //    }
        //    catch
        //    {
        //        this.airView.ArchitectureTexture = null;
        //    }
        //}

        //public void ReloadTroopView() //以前的代码，现在已不用
        //{
        //    if (!this.airView.scenario.NoCurrentPlayer)
        //    {
        //        if (this.airView.TroopTexture != null)
        //        {
        //            this.airView.TroopTexture.Dispose();
        //            this.airView.TroopTexture = null;
        //        }
        //        Bitmap image = new Bitmap(this.airView.scenario.ScenarioMap.MapDimensions.X * this.airView.TileLength, this.airView.scenario.ScenarioMap.MapDimensions.Y * this.airView.TileLength, PixelFormat.Format32bppArgb);
        //        Graphics graphics = Graphics.FromImage(image);
        //        graphics.Clear(System.Drawing.Color.Transparent);
        //        for (int i = 0; i < this.airView.scenario.ScenarioMap.MapDimensions.X; i++)
        //        {
        //            for (int j = 0; j < this.airView.scenario.ScenarioMap.MapDimensions.Y; j++)
        //            {
        //                if (this.airView.scenario.CurrentPlayer.IsPositionKnown(new Microsoft.Xna.Framework.Point(i, j)))
        //                {
        //                    Troop troopByPositionNoCheck = this.airView.scenario.GetTroopByPositionNoCheck(new Microsoft.Xna.Framework.Point(i, j));
        //                    if ((troopByPositionNoCheck != null) && !troopByPositionNoCheck.Destroyed)
        //                    {
        //                        if (troopByPositionNoCheck.BelongedFaction == this.airView.scenario.CurrentPlayer)
        //                        {
        //                            graphics.DrawImage(this.troopImage, new System.Drawing.Rectangle(i * this.airView.TileLength, j * this.airView.TileLength, this.airView.TileLength, this.airView.TileLength));
        //                        }
        //                        else if (troopByPositionNoCheck.IsFriendly(this.airView.scenario.CurrentPlayer))
        //                        {
        //                            graphics.DrawImage(this.troopFriendlyImage, new System.Drawing.Rectangle(i * this.airView.TileLength, j * this.airView.TileLength, this.airView.TileLength, this.airView.TileLength));
        //                        }
        //                        else
        //                        {
        //                            graphics.DrawImage(this.troopHostileImage, new System.Drawing.Rectangle(i * this.airView.TileLength, j * this.airView.TileLength, this.airView.TileLength, this.airView.TileLength));
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        try
        //        {
        //            image.Save(@"Content\Textures\GameComponents\AirView\~tmp.image");
        //            this.airView.TroopTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\AirView\~tmp.image");
        //            File.Delete(@"Content\Textures\GameComponents\AirView\~tmp.image");
        //        }
        //        catch
        //        {
        //            this.airView.TroopTexture = null;
        //        }
        //    }
        //}

        public void RemoveDisableRects()
        {
            this.airView.RemoveDisableRects();
        }

        public void ResetFramePosition(Microsoft.Xna.Framework.Point viewportSize, int leftEdge, int topEdge, Microsoft.Xna.Framework.Point totalMapSize)
        {
            this.airView.ResetFramePosition(viewportSize, leftEdge, topEdge, totalMapSize);
        }

        public void ResetFrameSize(Microsoft.Xna.Framework.Point viewportSize, Microsoft.Xna.Framework.Point totalMapSize)
        {
            this.airView.ResetFrameSize(viewportSize, totalMapSize);
        }

        public void ResetMapPosition(Screen screen)
        {
            this.airView.SetDisplayOffset(screen, this.airView.MapShowPosition);
        }

        public void SetGraphicsDevice()
        {
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\AirViewData.xml");
        }

        public void SetMapPosition(ShowPosition showPosition)
        {
            this.airView.SetDisplayOffset(Session.MainGame.mainGameScreen, showPosition);
        }

        public void SetScreen(Screen screen)
        {
            this.airView.Name = this.pluginName;
            this.airView.Initialize(screen);
        }

        public void Update(GameTime gameTime)
        { 
            this.airView.Update();
        }

        public Microsoft.Xna.Framework.Rectangle MapPosition
        {
            get
            {
                return this.airView.MapPosition;
            }
        }

        public string Author
        {
            get
            {
                return this.author;
            }
        }

        public string Description
        {
            get
            {
                return this.description;
            }
        }

        public object Instance
        {
            get
            {
                return this;
            }
        }

        public bool IsMapShowing
        {
            get
            {
                return this.airView.IsMapShowing;
            }
        }

        public string PluginName
        {
            get
            {
                return this.pluginName;
            }
        }

        public object ToolInstance
        {
            get
            {
                return this.airView;
            }
        }

        public string Version
        {
            get
            {
                return this.version;
            }
        }

        // 🔥 新增：创建空白小地图（2026-03-17）
        // 用途：当 TerritoryManager 未初始化时，创建一个基础的地形小地图
        private void CreateBlankMinimap(GameObjects.GameScenario scenario)
        {
            try
            {
                int width = scenario.ScenarioMap.MapDimensions.X;
                int height = scenario.ScenarioMap.MapDimensions.Y;

                if (width <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AirView] 地图尺寸异常，无法创建空白小地图: {width}x{height}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[AirView] 🎨 创建空白小地图: {width}x{height}");

                int totalPixels = width * height;
                Color[] mapColors = new Color[totalPixels];

                // 只绘制基础地形，不绘制势力范围
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        var terrainKind = scenario.GetTerrainKindByPositionNoCheck(new Point(x, y));
                        
                        // 使用简化的地形颜色
                        mapColors[index] = terrainKind switch
                        {
                            TerrainKind.平原 => new Color(144, 238, 144),  // 浅绿
                            TerrainKind.草原 => new Color(173, 255, 47),   // 黄绿
                            TerrainKind.森林 => new Color(34, 139, 34),    // 深绿
                            TerrainKind.水域 => new Color(70, 130, 180),   // 钢蓝
                            TerrainKind.山地 => new Color(139, 69, 19),    // 棕色
                            TerrainKind.峻岭 => new Color(105, 105, 105),  // 暗灰
                            TerrainKind.荒地 => new Color(189, 183, 107),  // 暗卡其
                            TerrainKind.湿地 => new Color(107, 142, 35),   // 橄榄绿
                            TerrainKind.沙漠 => new Color(255, 222, 173),  // 纳瓦霍白
                            TerrainKind.栈道 => new Color(160, 82, 45),    // 赭色
                            _ => new Color(200, 200, 200)  // 默认灰色
                        };
                    }
                }

                // 🔥 修复：先从缓存中移除，再释放纹理（2026-03-18）
                // 原因：小地图纹理存储在永久缓存中，需要从 TextureDics 移除
                string textureName = "StrategicMinimap";
                
                // 先从永久缓存中移除旧引用
                if (CacheManager.TextureDics.ContainsKey(textureName))
                {
                    CacheManager.TextureDics.Remove(textureName);
                }
                
                // 再释放旧纹理资源
                if (this.AirViewImage != null && !this.AirViewImage.IsDisposed)
                {
                    this.AirViewImage.Dispose();
                }

                // 创建新纹理
                Texture2D blankMap = new(Platform.GraphicsDevice, width, height);
                blankMap.SetData(mapColors);
                this.AirViewImage = blankMap;
                this.airView.MapTexture = new()
                {
                    Name = textureName,
                    Width = this.AirViewImage.Width,
                    Height = this.AirViewImage.Height,
                    Texture = this.AirViewImage
                };

                // 🔥 修复：将小地图纹理添加到永久缓存，避免被 Clear(CacheType.Page) 清理（2026-03-18）
                CacheManager.TextureDics[textureName] = this.AirViewImage;
                
                // 🔥 调试：确认纹理已添加到缓存（2026-03-18）
                System.Diagnostics.Debug.WriteLine($"[AirView] ✅ 空白纹理已添加到永久缓存: {textureName}, IsDisposed={this.AirViewImage.IsDisposed}");
                System.Diagnostics.Debug.WriteLine($"[AirView] 缓存验证: ContainsKey={CacheManager.TextureDics.ContainsKey(textureName)}");
                System.Diagnostics.Debug.WriteLine($"[AirView] MapTexture.Name={this.airView.MapTexture?.Name}");
                System.Diagnostics.Debug.WriteLine($"[AirView] MapTexture.Texture={this.airView.MapTexture?.Texture}");
                
                // 🔥 标记使用了降级方案（2026-03-17）
                _usingFallbackMinimap = true;
                
                System.Diagnostics.Debug.WriteLine($"[AirView] ✅ 空白小地图创建完成: {width}x{height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AirView] ❌ 创建空白小地图时发生异常: {ex.Message}");
            }
        }

        // 高性能版本：使用数据快照和并行优化 + 地形缓存
        public void CreateStrategicMinimap(GameObjects.GameScenario scenario)
        {
            // 🧊 Cold Path：回合结束时调用
            // 使用本地引用，防止循环中多次访问 Session 造成的开销
            var territoryManager = Session.Current.TerritoryManager;
            if (territoryManager == null)
            {
                // 🔥 修复：TerritoryManager 未初始化时，记录日志并创建空白小地图（2026-03-17）
                // 原因：游戏加载时 ReloadAirView 可能在 TerritoryManager 初始化之前被调用
                System.Diagnostics.Debug.WriteLine("[AirView] ⚠️ TerritoryManager 未初始化，创建空白小地图");
                CreateBlankMinimap(scenario);
                return;
            }
            
            // 🔥 调试：确认 TerritoryManager 已初始化（2026-03-17）
            System.Diagnostics.Debug.WriteLine("[AirView] ✅ TerritoryManager 已初始化，开始生成完整小地图");
            System.Diagnostics.Debug.WriteLine($"[AirView] 势力数量: {scenario.Factions.Count}");

            try
            {
                // 1. 获取地图尺寸
                int width = scenario.ScenarioMap.MapDimensions.X;
                int height = scenario.ScenarioMap.MapDimensions.Y;

                // 防御式检查：如果地图尺寸异常，则直接跳过生成，避免创建非法纹理导致显卡错误
                if (width <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AirView] 地图尺寸异常，跳过战略小地图生成: {width}x{height}");
                    return;
                }
                int totalPixels = width * height;

                System.Diagnostics.Debug.WriteLine($"[AirView] 🎨 开始生成战略小地图: {width}x{height}");

                // 🎯 新增：检查地形缓存
                string currentMapName = scenario.ScenarioMap.MapName ?? "unknown";
                bool useTerrainCache = false;
                
                if (!_terrainCacheDirty && _cachedMapName == currentMapName && _cachedTerrainLayer != null && !_cachedTerrainLayer.IsDisposed)
                {
                    useTerrainCache = true;
                    System.Diagnostics.Debug.WriteLine("[AirView] ✅ 使用地形缓存，跳过地形层生成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AirView] 🔄 地形缓存失效，重新生成地形层");
                    _terrainCacheDirty = false;
                    _cachedMapName = currentMapName;
                }

                // 2. 重新计算势力版图 (确保这一步不在并行循环中，因为它可能修改数据)
                // 注意：如果此操作非常耗时，应考虑将其移出渲染帧逻辑
                System.Diagnostics.Debug.WriteLine("[AirView] 🔄 重新计算势力版图...");
                territoryManager.RecalculateTerritory(scenario.Architectures, scenario.ScenarioMap.MapData);
                System.Diagnostics.Debug.WriteLine("[AirView] ✅ 势力版图计算完成");
                
                // 🔥 调试：检查势力版图是否有数据（2026-03-17）
                int territoryCount = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int owner = territoryManager.GetTerritoryOwner(x, y);
                        if (owner >= 0)
                        {
                            territoryCount++;
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[AirView] 🔍 势力版图统计: {territoryCount}/{totalPixels} 个格子有势力归属");


                Color[] mapColors;
                
                if (useTerrainCache)
                {
                    // 🎯 优化：使用缓存的地形层，只更新势力版图
                    mapColors = new Color[totalPixels];
                    _cachedTerrainLayer.GetData(mapColors);
                    
                    System.Diagnostics.Debug.WriteLine("[AirView] 🎨 更新势力范围颜色...");
                    // 只更新势力相关的颜色
                    UpdateTerritoryColors(mapColors, territoryManager, scenario, width, height);
                    System.Diagnostics.Debug.WriteLine("[AirView] ✅ 势力范围颜色更新完成");
                }
                else
                {
                    // 🎯 完整生成：地形层 + 势力层
                    System.Diagnostics.Debug.WriteLine("[AirView] 🎨 生成完整地图（地形+势力）...");
                    mapColors = GenerateCompleteMap(territoryManager, scenario, width, height, totalPixels);
                    System.Diagnostics.Debug.WriteLine("[AirView] ✅ 完整地图生成完成");
                    
                    // 缓存地形层
                    CacheTerrainLayer(scenario, width, height, totalPixels);
                }

                // 🔥 调试：检查颜色数据
                int nonZeroPixels = 0;
                for (int i = 0; i < Math.Min(100, mapColors.Length); i++)
                {
                    if (mapColors[i].R != 0 || mapColors[i].G != 0 || mapColors[i].B != 0)
                    {
                        nonZeroPixels++;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[AirView] 🔍 颜色数据检查: 前100像素中有 {nonZeroPixels} 个非黑色像素");

                // 5. 资源清理与纹理生成 (主线程)
                // 🔥 修复：先从缓存中移除，再释放纹理（2026-03-18）
                // 原因：小地图纹理存储在永久缓存中，需要从 TextureDics 移除
                string textureName = "StrategicMinimap";
                
                // 先从永久缓存中移除旧引用
                if (CacheManager.TextureDics.ContainsKey(textureName))
                {
                    CacheManager.TextureDics.Remove(textureName);
                }
                
                // 再释放旧纹理资源
                if (this.AirViewImage != null && !this.AirViewImage.IsDisposed)
                {
                    this.AirViewImage.Dispose();
                }

                // 创建新纹理
                System.Diagnostics.Debug.WriteLine($"[AirView] 🎨 创建纹理: {width}x{height}");
                Texture2D strategyMap = new(Platform.GraphicsDevice, width, height);
                strategyMap.SetData(mapColors);
                System.Diagnostics.Debug.WriteLine($"[AirView] ✅ 纹理创建完成");

                // 6. 更新引用
                this.AirViewImage = strategyMap;
                this.airView.MapTexture = new()
                {
                    Name = textureName,
                    Width = this.AirViewImage.Width,
                    Height = this.AirViewImage.Height,
                    Texture = this.AirViewImage  // 🔥 关键修复：设置 Texture 属性
                };

                // 🔥 修复：将小地图纹理添加到永久缓存，避免被 Clear(CacheType.Page) 清理（2026-03-18）
                // 原因：MainMapLayer.Clear() 会调用 CacheManager.Clear(CacheType.Page)，清空所有临时纹理
                // 解决：使用 TextureDics（永久缓存）而不是 TextureTempDics（临时缓存）
                CacheManager.TextureDics[textureName] = this.AirViewImage;
                
                // 🔥 调试：确认纹理已添加到缓存（2026-03-18）
                System.Diagnostics.Debug.WriteLine($"[AirView] ✅ 纹理已添加到永久缓存: {textureName}, IsDisposed={this.AirViewImage.IsDisposed}");
                System.Diagnostics.Debug.WriteLine($"[AirView] 缓存验证: ContainsKey={CacheManager.TextureDics.ContainsKey(textureName)}");
                System.Diagnostics.Debug.WriteLine($"[AirView] MapTexture.Name={this.airView.MapTexture?.Name}");
                System.Diagnostics.Debug.WriteLine($"[AirView] MapTexture.Texture={this.airView.MapTexture?.Texture}");
                
                // 🔥 清除降级标记（2026-03-17）
                _usingFallbackMinimap = false;
                
                System.Diagnostics.Debug.WriteLine($"[AirView] ✅ 战略小地图生成完成: {width}x{height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AirView] ❌ 生成战略小地图时发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AirView] 堆栈: {ex.StackTrace}");
            }
        }

        
        // 🎯 新增：缓存地形层
        private void CacheTerrainLayer(GameObjects.GameScenario scenario, int width, int height, int totalPixels)
        {
            try
            {
                Color[] terrainColors = new Color[totalPixels];
                
                // 并行生成纯地形颜色
                Parallel.For(0, totalPixels, i =>
                {
                    int x = i % width;
                    int y = i / width;
                    
                    var terrainDetail = scenario.GetTerrainDetailByPositionNoCheck(new Point(x, y));
                    int terrainId = terrainDetail?.ID ?? 0;
                    terrainColors[i] = GetTerrainColor(terrainId);
                });
                
                // 释放旧的地形缓存
                if (_cachedTerrainLayer != null && !_cachedTerrainLayer.IsDisposed)
                {
                    _cachedTerrainLayer.Dispose();
                }
                
                // 创建新的地形缓存
                _cachedTerrainLayer = new Texture2D(Platform.GraphicsDevice, width, height);
                _cachedTerrainLayer.SetData(terrainColors);
                
                // System.Diagnostics.Debug.WriteLine("[AirView] 地形层已缓存");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AirView] 缓存地形层时发生异常: {ex.Message}");
            }
        }
        
        // 🎯 新增：只更新势力版图颜色
        private void UpdateTerritoryColors(Color[] mapColors, WorldOfTheThreeKingdoms.GameManager.TerritoryManager territoryManager, GameObjects.GameScenario scenario, int width, int height)
        {
            // 🎨 修改日期：2026-03-17
            // 🎨 功能：使用 GlobalInfluenceMap 渲染势力范围，与大地图保持一致
            // 🧊 Cold Path：回合结束时调用
            
            int totalPixels = width * height;
            
            // 🔥 ANTI-BAND-AID：检查 GlobalInfluenceMap 是否已初始化
            // 🧊 Cold Path：容错处理，因为小地图可能在 InfluenceUpdateManager 初始化前调用
            var factionsGameObjectList = scenario.Factions.GetList();
            int factionCount = factionsGameObjectList.Count;
            if (factionCount == 0) return;
            
            // 🔥 性能优化：预先转换为数组，避免 Parallel.For 内部枚举器分配
            Faction[] factionsArray = new Faction[factionCount];
            int validFactionCount = 0;
            for (int idx = 0; idx < factionCount; idx++)
            {
                if (factionsGameObjectList[idx] is Faction f)
                {
                    factionsArray[validFactionCount++] = f;
                }
            }
            
            bool hasInfluenceMap = false;
            for (int idx = 0; idx < validFactionCount; idx++)
            {
                if (factionsArray[idx].GlobalInfluenceMap != null && factionsArray[idx].GlobalInfluenceMap.Length > 0)
                {
                    hasInfluenceMap = true;
                    break;
                }
            }
            
            // 🔥 调试：输出 GlobalInfluenceMap 状态（2026-03-17）
            System.Diagnostics.Debug.WriteLine($"[UpdateTerritoryColors] GlobalInfluenceMap 状态: {(hasInfluenceMap ? "已初始化" : "未初始化")}");
            
            // 🔥 预取势力归属数据（优先使用 GlobalInfluenceMap，回退到 TerritoryManager）
            int[] ownerFactions = new int[totalPixels];
            int[] energyLevels = new int[totalPixels];
            
            Parallel.For(0, totalPixels, i =>
            {
                int x = i % width;
                int y = i / width;
                
                if (hasInfluenceMap)
                {
                    // 🔥 使用与 InkBleedInfluenceRenderer 相同的逻辑
                    int mapIndex = WorldOfTheThreeKingdoms.GameManager.TerrainCostCache.GetIndex(x, y);
                    
                    // 🔥 查询能量最高的势力（使用 for 循环避免分配）
                    int maxEnergy = 0;
                    int ownerFactionID = -1;
                    
                    for (int fIdx = 0; fIdx < validFactionCount; fIdx++)
                    {
                        var faction = factionsArray[fIdx];
                        
                        // 🔥 边界检查：防止数组越界（合理的防御）
                        if (faction.GlobalInfluenceMap == null || mapIndex >= faction.GlobalInfluenceMap.Length)
                            continue;
                        
                        int energy = faction.GlobalInfluenceMap[mapIndex].EffectiveTotalEnergy;
                        if (energy > maxEnergy)
                        {
                            maxEnergy = energy;
                            ownerFactionID = faction.ID;
                        }
                    }
                    
                    ownerFactions[i] = ownerFactionID;
                    energyLevels[i] = maxEnergy;
                }
                else
                {
                    // 🔥 回退：使用 TerritoryManager 数据（2026-03-17）
                    ownerFactions[i] = territoryManager.GetTerritoryOwner(x, y);
                    energyLevels[i] = (int)(territoryManager.GetTerritoryStrength(x, y) * 10000);
                }
            });
            
            // 🔥 更新颜色（混合地形色和势力色）
            int updatedPixelCount = 0;
            Parallel.For(0, totalPixels, i =>
            {
                int ownerFactionID = ownerFactions[i];
                int energy = energyLevels[i];
                
                if (ownerFactionID >= 0 && energy > 0)
                {
                    var faction = scenario.Factions.GetGameObject(ownerFactionID) as Faction;
                    if (faction != null)
                    {
                        Color terrainColor = mapColors[i]; // 使用缓存的地形色
                        Color factionColor = GetFactionColor(faction);
                        
                        // 🎨 根据能量值计算混合比例（能量越高，势力色越明显）
                        // 能量范围通常是 0-10000，归一化到 0-1
                        float normalizedEnergy = Math.Min(energy / 10000f, 1f);
                        float blendRatio = 0.3f + (normalizedEnergy * 0.5f); // 0.3 ~ 0.8
                        
                        mapColors[i] = Color.Lerp(terrainColor, factionColor, blendRatio);
                        System.Threading.Interlocked.Increment(ref updatedPixelCount);
                    }
                }
                // 如果不是势力领土，保持原地形色不变
            });
            
            // 🔥 调试：输出更新统计（2026-03-17）
            System.Diagnostics.Debug.WriteLine($"[UpdateTerritoryColors] 更新了 {updatedPixelCount}/{totalPixels} 个像素的势力颜色");
        }
        
        // 🎯 新增：生成完整地图（地形+势力）
        private Color[] GenerateCompleteMap(WorldOfTheThreeKingdoms.GameManager.TerritoryManager territoryManager, GameObjects.GameScenario scenario, int width, int height, int totalPixels)
        {
            // 🎨 修改日期：2026-03-17
            // 🎨 功能：使用 GlobalInfluenceMap 渲染势力范围，与大地图保持一致
            // 🧊 Cold Path：回合结束时调用
            
            // 3. 数据快照 (Snapshot Data) - 关键优化
            int[] terrainIds = new int[totalPixels];
            int[] ownerFactions = new int[totalPixels];
            int[] energyLevels = new int[totalPixels];

            // 🔥 ANTI-BAND-AID：检查 GlobalInfluenceMap 是否已初始化
            // 🧊 Cold Path：容错处理，因为小地图可能在 InfluenceUpdateManager 初始化前调用
            var factionsGameObjectList = scenario.Factions.GetList();
            int factionCount = factionsGameObjectList.Count;
            
            // 🔥 性能优化：预先转换为数组，避免 Parallel.For 内部枚举器分配
            Faction[] factionsArray = new Faction[factionCount];
            int validFactionCount = 0;
            for (int idx = 0; idx < factionCount; idx++)
            {
                if (factionsGameObjectList[idx] is Faction f)
                {
                    factionsArray[validFactionCount++] = f;
                }
            }
            
            bool hasInfluenceMap = false;
            for (int idx = 0; idx < validFactionCount; idx++)
            {
                if (factionsArray[idx].GlobalInfluenceMap != null && factionsArray[idx].GlobalInfluenceMap.Length > 0)
                {
                    hasInfluenceMap = true;
                    break;
                }
            }
            
            // 🔥 调试：输出 GlobalInfluenceMap 状态（2026-03-17）
            System.Diagnostics.Debug.WriteLine($"[GenerateCompleteMap] GlobalInfluenceMap 状态: {(hasInfluenceMap ? "已初始化" : "未初始化")}");
            System.Diagnostics.Debug.WriteLine($"[GenerateCompleteMap] 势力数量: {validFactionCount}");

            Parallel.For(0, totalPixels, i =>
            {
                int x = i % width;
                int y = i / width;

                var terrainDetail = scenario.GetTerrainDetailByPositionNoCheck(new Point(x, y));
                terrainIds[i] = terrainDetail?.ID ?? 0;
                
                // 🎨 使用 GlobalInfluenceMap 查询势力归属
                if (hasInfluenceMap)
                {
                    int mapIndex = WorldOfTheThreeKingdoms.GameManager.TerrainCostCache.GetIndex(x, y);
                    
                    // 🔥 查询能量最高的势力（使用 for 循环避免分配）
                    int maxEnergy = 0;
                    int ownerFactionID = -1;
                    
                    for (int fIdx = 0; fIdx < validFactionCount; fIdx++)
                    {
                        var faction = factionsArray[fIdx];
                        
                        // 🔥 边界检查：防止数组越界（合理的防御）
                        if (faction.GlobalInfluenceMap == null || mapIndex >= faction.GlobalInfluenceMap.Length)
                            continue;
                        
                        int energy = faction.GlobalInfluenceMap[mapIndex].EffectiveTotalEnergy;
                        if (energy > maxEnergy)
                        {
                            maxEnergy = energy;
                            ownerFactionID = faction.ID;
                        }
                    }
                    
                    ownerFactions[i] = ownerFactionID;
                    energyLevels[i] = maxEnergy;
                }
                else
                {
                    // 🧊 Cold Path：回退到旧的 TerritoryManager 逻辑
                    ownerFactions[i] = territoryManager.GetTerritoryOwner(x, y);
                    energyLevels[i] = (int)(territoryManager.GetTerritoryStrength(x, y) * 10000);
                }
            });

            // 4. 并行生成颜色数组
            Color[] mapColors = new Color[totalPixels];
            
            // 🔥 调试：统计势力归属（2026-03-17）
            int territoryPixelCount = 0;
            for (int i = 0; i < totalPixels; i++)
            {
                if (ownerFactions[i] >= 0 && energyLevels[i] > 0)
                {
                    territoryPixelCount++;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[GenerateCompleteMap] 势力归属统计: {territoryPixelCount}/{totalPixels} 个格子有势力");

            int updatedPixelCount = 0;
            Parallel.For(0, totalPixels, i =>
            {
                int terrainId = terrainIds[i];
                int ownerFactionID = ownerFactions[i];
                int energy = energyLevels[i];

                Color terrainColor = GetTerrainColor(terrainId);

                if (ownerFactionID >= 0 && energy > 0)
                {
                    var faction = scenario.Factions.GetGameObject(ownerFactionID) as Faction;
                    if (faction != null)
                    {
                        Color factionColor = GetFactionColor(faction);
                        
                        // 🎨 根据能量值计算混合比例（能量越高，势力色越明显）
                        float normalizedEnergy = Math.Min(energy / 10000f, 1f);
                        float blendRatio = 0.3f + (normalizedEnergy * 0.5f); // 0.3 ~ 0.8
                        
                        mapColors[i] = Color.Lerp(terrainColor, factionColor, blendRatio);
                        System.Threading.Interlocked.Increment(ref updatedPixelCount);
                    }
                    else
                    {
                        mapColors[i] = terrainColor;
                    }
                }
                else
                {
                    mapColors[i] = terrainColor;
                }
            });
            
            // 🔥 调试：输出更新统计（2026-03-17）
            System.Diagnostics.Debug.WriteLine($"[GenerateCompleteMap] 更新了 {updatedPixelCount}/{totalPixels} 个像素的势力颜色");
            
            return mapColors;
        }
        
        // 🎯 新增：标记地形缓存需要更新
        public static void MarkTerrainCacheDirty()
        {
            _terrainCacheDirty = true;
        }

        // 辅助：地形配色表 (调整这里来解决"黑乎乎"的问题)
        private Color GetTerrainColor(int terrainId)
        {
            switch (terrainId)
            {
                case 0: return Color.Black;                                // 无
                case 1: return new Color((byte)144, (byte)238, (byte)144); // 平原 - 浅绿
                case 2: return new Color((byte)50, (byte)205, (byte)50);   // 草原 - 酸橙绿
                case 3: return new Color((byte)34, (byte)139, (byte)34);   // 森林 - 森林绿
                case 4: return new Color((byte)47, (byte)79, (byte)79);    // 湿地 - 深青色
                case 5: return new Color((byte)160, (byte)82, (byte)45);   // 山地 - 赭色
                case 6: return new Color((byte)30, (byte)144, (byte)255);  // 水域 - 道奇蓝 (亮蓝色)
                case 7: return new Color((byte)105, (byte)105, (byte)105); // 峻岭 - 暗灰
                case 8: return new Color((byte)189, (byte)183, (byte)107); // 荒地 - 卡其色
                case 9: return new Color((byte)210, (byte)180, (byte)140); // 沙漠 - 棕褐色
                case 10: return new Color((byte)139, (byte)69, (byte)19);  // 栈道 - 鞍褐
                default: return new Color((byte)50, (byte)100, (byte)50);  // 默认
            }
        }

        // 辅助：势力配色
        private Color GetFactionColor(GameObjects.Faction faction)
        {
            if (faction != null)
            {
                // 使用势力的颜色，但调整透明度以便与地形色混合
                var factionColor = faction.FactionColor;
                return new Color(factionColor.R, factionColor.G, factionColor.B, (byte)180); // 半透明
            }
            return Color.Gray;
        }
    }

 

}
