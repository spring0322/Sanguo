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
        
        // 🔥 Phase 3：预分配缓冲区（2026-03-30）
        // 原因：避免每次结算后重新分配数组，减少GC压力
        private int[] _ownerFactionBuffer;
        private int[] _energyLevelBuffer;
        private int _bufferSize;

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
            // 🔥 Phase 2：移除 TerritoryManager 依赖（2026-03-30）
            // 原因：统一使用 GlobalInfluenceMap 作为唯一数据源
            
            // 🔥 检查 GlobalInfluenceMap 是否就绪
            bool hasInfluenceMap = scenario.Factions.GetList()
                .OfType<Faction>()
                .Any(f => f.GlobalInfluenceMap != null && f.GlobalInfluenceMap.Length > 0);
            
            if (!hasInfluenceMap)
            {
                // 🔥 势力范围系统未就绪时，显示纯地形底图（2026-03-30）
                System.Diagnostics.Debug.WriteLine("[AirView] ⚠️ GlobalInfluenceMap 未就绪，创建空白小地图");
                CreateBlankMinimap(scenario);
                return;
            }
            
            // 🔥 调试：确认 GlobalInfluenceMap 已初始化（2026-03-30）
            System.Diagnostics.Debug.WriteLine("[AirView] ✅ GlobalInfluenceMap 已就绪，开始生成完整小地图");
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

                // 🔥 Phase 2：移除 TerritoryManager.RecalculateTerritory 调用（2026-03-30）
                // 原因：不再使用 TerritoryManager 作为数据源
                System.Diagnostics.Debug.WriteLine("[AirView] ✅ 使用 GlobalInfluenceMap 数据源");


                Color[] mapColors;
                
                if (useTerrainCache)
                {
                    // 🎯 优化：使用缓存的地形层，只更新势力版图
                    mapColors = new Color[totalPixels];
                    _cachedTerrainLayer.GetData(mapColors);
                    
                    System.Diagnostics.Debug.WriteLine("[AirView] 🎨 更新势力范围颜色...");
                    // 只更新势力相关的颜色
                    UpdateTerritoryColors(mapColors, scenario, width, height);
                    System.Diagnostics.Debug.WriteLine("[AirView] ✅ 势力范围颜色更新完成");
                }
                else
                {
                    // 🎯 完整生成：地形层 + 势力层
                    System.Diagnostics.Debug.WriteLine("[AirView] 🎨 生成完整地图（地形+势力）...");
                    mapColors = GenerateCompleteMap(scenario, width, height, totalPixels);
                    System.Diagnostics.Debug.WriteLine("[AirView] ✅ 完整地图生成完成");
                    
                    // 缓存地形层
                    CacheTerrainLayer(scenario, width, height, totalPixels);
                }
                // 🎨 应用极速边缘平滑，消除菱形锯齿，让势力边缘与底图自然融合
                System.Diagnostics.Debug.WriteLine("[AirView] 🎨 应用边缘平滑模糊...");
                ApplyFastBoxBlur(mapColors, width, height);
                
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
        
        // 🎯 Phase 1+2+3：只更新势力版图颜色（移除TerritoryManager依赖 + 使用战略归属 + 预分配缓冲区）
        // 日期：2026-03-30
        private void UpdateTerritoryColors(Color[] mapColors, GameObjects.GameScenario scenario, int width, int height)
        {
            // 🎨 修改日期：2026-03-30
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
            
            // 🔥 调试：输出 GlobalInfluenceMap 状态（2026-03-30）
            System.Diagnostics.Debug.WriteLine($"[UpdateTerritoryColors] GlobalInfluenceMap 状态: {(hasInfluenceMap ? "已初始化" : "未初始化")}");
            
            // 🔥 Phase 3：预分配缓冲区（首次调用时初始化）
            if (_ownerFactionBuffer == null || _bufferSize != totalPixels)
            {
                _ownerFactionBuffer = new int[totalPixels];
                _energyLevelBuffer = new int[totalPixels];
                _bufferSize = totalPixels;
                System.Diagnostics.Debug.WriteLine($"[UpdateTerritoryColors] 预分配缓冲区: {totalPixels} 像素");
            }
            
            // 🔥 Phase 1：使用战略归属（TerritoryOwner）而非综合能量（EffectiveTotalEnergy）
            // 原因：小地图主层应显示"领地归属"而非"战术控制/压制"
            Parallel.For(0, totalPixels, i =>
            {
                int x = i % width;
                int y = i / width;
                int mapIndex = WorldOfTheThreeKingdoms.GameManager.TerrainCostCache.GetIndex(x, y);
                
                // 🔥 Phase 1：直接读取结算后的战略归属（不再自行推导）
                int ownerFactionID = -1;
                int bestCityEnergy = 0;
                
                for (int fIdx = 0; fIdx < validFactionCount; fIdx++)
                {
                    var faction = factionsArray[fIdx];
                    
                    // 🔥 边界检查：防止数组越界（合理的防御）
                    if (faction.GlobalInfluenceMap == null || mapIndex >= faction.GlobalInfluenceMap.Length)
                        continue;
                    
                    ref readonly var tile = ref faction.GlobalInfluenceMap[mapIndex];
                    
                    // 🔥 关键修复：使用 TerritoryOwner（战略归属）而非 EffectiveTotalEnergy（综合控制）
                    // TerritoryOwner 只看 CityEnergy（城市扩散），不含部队临时压制
                    if (tile.TerritoryOwner == faction.ID && tile.CityEnergy > bestCityEnergy)
                    {
                        bestCityEnergy = tile.CityEnergy;
                        ownerFactionID = faction.ID;
                    }
                }
                
                _ownerFactionBuffer[i] = ownerFactionID;
                _energyLevelBuffer[i] = bestCityEnergy;
            });
            
            // 🔥 更新颜色（混合地形色和势力色）
            int updatedPixelCount = 0;
            Parallel.For(0, totalPixels, i =>
            {
                int ownerFactionID = _ownerFactionBuffer[i];
                int energy = _energyLevelBuffer[i];
                
                int x = i % width;
                int y = i / width;
                bool isKnown = true;
                if (!Session.GlobalVariables.SkyEye && !scenario.NoCurrentPlayer && scenario.CurrentPlayer != null)
                {
                    isKnown = scenario.CurrentPlayer.IsPositionKnown(new Point(x, y));
                }
                
                if (isKnown && ownerFactionID >= 0 && energy > 0)
                {
                    var faction = scenario.Factions.GetGameObject(ownerFactionID) as Faction;
                    if (faction != null)
                    {
                        Color terrainColor = mapColors[i];
                        Color factionColor = GetFactionColor(faction);
                        
                        float normalizedEnergy = MathF.Min(energy / 10000f, 1f);
                        
                        // 1. 非线性增强能量比例
                        float boostedEnergy = MathF.Pow(normalizedEnergy, 0.4f);
                        
                        // 2. 保证高浓度，防止底色过多透出
                        float energyAlpha = Math.Clamp(0.5f + (boostedEnergy * 0.5f), 0.5f, 1.0f);
                        
                        float rF = factionColor.R / 255f;
                        float gF = factionColor.G / 255f;
                        float bF = factionColor.B / 255f;
                        
                        float rT = terrainColor.R / 255f;
                        float gT = terrainColor.G / 255f;
                        float bT = terrainColor.B / 255f;
                        
                        // --- 核心修复：地形明暗映射染色法 (Topographic Color Mapping) ---
                        // 提取底图（羊皮纸）的明暗纹理
                        float terrainLuma = rT * 0.299f + gT * 0.587f + bT * 0.114f;
                        
                        // 绝招1：让势力色直接与地形的明暗纹理相乘
                        float colorR = rF * terrainLuma * 1.2f;
                        float colorG = gF * terrainLuma * 1.2f;
                        float colorB = bF * terrainLuma * 1.2f;
                        
                        // 绝招2：强制亮度拉开差距 (Luminance Contrast Guarantee)
                        float colorLuma = colorR * 0.299f + colorG * 0.587f + colorB * 0.114f;
                        if (colorLuma > 0.6f)
                        {
                            float suppress = 0.6f / colorLuma;
                            colorR *= suppress;
                            colorG *= suppress;
                            colorB *= suppress;
                        }
                        
                        // 3. 混合：在原地形和【强化反差后的势力墨迹】之间插值
                        float finalR = rT * (1f - energyAlpha) + colorR * energyAlpha;
                        float finalG = gT * (1f - energyAlpha) + colorG * energyAlpha;
                        float finalB = bT * (1f - energyAlpha) + colorB * energyAlpha;
                        
                        mapColors[i] = new Color(
                            (byte)Math.Clamp(finalR * 255f, 0f, 255f),
                            (byte)Math.Clamp(finalG * 255f, 0f, 255f),
                            (byte)Math.Clamp(finalB * 255f, 0f, 255f),
                            (byte)255);
                        System.Threading.Interlocked.Increment(ref updatedPixelCount);
                    }
                }
                else
                {
                    Color tColor = mapColors[i];
                    if (!isKnown)
                    {
                        mapColors[i] = new Color(
                            (byte)(tColor.R * 0.7f),
                            (byte)(tColor.G * 0.7f),
                            (byte)(tColor.B * 0.7f),
                            (byte)255);
                    }
                }
            });
            
            // 🔥 调试：输出更新统计（2026-03-30）
            System.Diagnostics.Debug.WriteLine($"[UpdateTerritoryColors] 更新了 {updatedPixelCount}/{totalPixels} 个像素的势力颜色");
        }
        
        // 🎯 Phase 1+2：生成完整地图（地形+势力，移除TerritoryManager依赖 + 使用战略归属）
        // 日期：2026-03-30
        private Color[] GenerateCompleteMap(GameObjects.GameScenario scenario, int width, int height, int totalPixels)
                {
                    // 🎨 修改日期：2026-03-30
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

                    // 🔥 调试：输出 GlobalInfluenceMap 状态（2026-03-30）
                    System.Diagnostics.Debug.WriteLine($"[GenerateCompleteMap] GlobalInfluenceMap 状态: {(hasInfluenceMap ? "已初始化" : "未初始化")}");
                    System.Diagnostics.Debug.WriteLine($"[GenerateCompleteMap] 势力数量: {validFactionCount}");

                    // 🔥 Phase 1：使用战略归属（TerritoryOwner）而非综合能量（EffectiveTotalEnergy）
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

                            // 🔥 Phase 1：直接读取结算后的战略归属（不再自行推导）
                            int ownerFactionID = -1;
                            int bestCityEnergy = 0;

                            for (int fIdx = 0; fIdx < validFactionCount; fIdx++)
                            {
                                var faction = factionsArray[fIdx];

                                // 🔥 边界检查：防止数组越界（合理的防御）
                                if (faction.GlobalInfluenceMap == null || mapIndex >= faction.GlobalInfluenceMap.Length)
                                    continue;

                                ref readonly var tile = ref faction.GlobalInfluenceMap[mapIndex];

                                // 🔥 关键修复：使用 TerritoryOwner（战略归属）而非 EffectiveTotalEnergy（综合控制）
                                // TerritoryOwner 只看 CityEnergy（城市扩散），不含部队临时压制
                                if (tile.TerritoryOwner == faction.ID && tile.CityEnergy > bestCityEnergy)
                                {
                                    bestCityEnergy = tile.CityEnergy;
                                    ownerFactionID = faction.ID;
                                }
                            }

                            ownerFactions[i] = ownerFactionID;
                            energyLevels[i] = bestCityEnergy;
                        }
                    });

                    // 4. 并行生成颜色数组
                    Color[] mapColors = new Color[totalPixels];

                    // 🔥 调试：统计势力归属（2026-03-30）
                    int territoryPixelCount = 0;
                    for (int i = 0; i < totalPixels; i++)
                    {
                        // 🔥 关键：ID=0（洛阳）是有效的，必须使用 >= 0
                        if (ownerFactions[i] >= 0 && energyLevels[i] > 0)
                        {
                            territoryPixelCount++;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[GenerateCompleteMap] 势力领土像素数: {territoryPixelCount}/{totalPixels}");

                    int updatedPixelCount = 0;
                    Parallel.For(0, totalPixels, i =>
                    {
                        int terrainId = terrainIds[i];
                        int ownerFactionID = ownerFactions[i];
                        int energy = energyLevels[i];

                        Color terrainColor = GetTerrainColor(terrainId);

                        // 🔥 提前计算坐标（避免重复计算）
                        int px = i % width;
                        int py = i / width;

                        // 🔥 战争迷雾判断（2026-03-31）
                        bool isKnown = true;
                        if (!Session.GlobalVariables.SkyEye && !scenario.NoCurrentPlayer && scenario.CurrentPlayer != null)
                        {
                            isKnown = scenario.CurrentPlayer.IsPositionKnown(new Point(px, py));
                        }

                        if (isKnown && ownerFactionID >= 0 && energy > 0)
                        {
                            var faction = scenario.Factions.GetGameObject(ownerFactionID) as Faction;
                            if (faction != null)
                            {
                                Color factionColor = GetFactionColor(faction);

                                float normalizedEnergy = MathF.Min(energy / 10000f, 1f);
                                
                                // 1. 非线性增强能量比例
                                float boostedEnergy = MathF.Pow(normalizedEnergy, 0.4f);
                                
                                // 2. 保证高浓度，防止底色过多透出
                                float energyAlpha = Math.Clamp(0.5f + (boostedEnergy * 0.5f), 0.5f, 1.0f);
                                
                                float rF = factionColor.R / 255f;
                                float gF = factionColor.G / 255f;
                                float bF = factionColor.B / 255f;
                                
                                float rT = terrainColor.R / 255f;
                                float gT = terrainColor.G / 255f;
                                float bT = terrainColor.B / 255f;
                                
                                // --- 核心修复：地形明暗映射染色法 (Topographic Color Mapping) ---
                                // 提取底图（羊皮纸）的明暗纹理
                                float terrainLuma = rT * 0.299f + gT * 0.587f + bT * 0.114f;
                                
                                // 绝招1：让势力色直接与地形的明暗纹理相乘
                                float colorR = rF * terrainLuma * 1.2f;
                                float colorG = gF * terrainLuma * 1.2f;
                                float colorB = bF * terrainLuma * 1.2f;
                                
                                // 绝招2：强制亮度拉开差距 (Luminance Contrast Guarantee)
                                float colorLuma = colorR * 0.299f + colorG * 0.587f + colorB * 0.114f;
                                if (colorLuma > 0.6f)
                                {
                                    float suppress = 0.6f / colorLuma;
                                    colorR *= suppress;
                                    colorG *= suppress;
                                    colorB *= suppress;
                                }
                                
                                // 3. 混合：在原地形和【强化反差后的势力墨迹】之间插值
                                float finalR = rT * (1f - energyAlpha) + colorR * energyAlpha;
                                float finalG = gT * (1f - energyAlpha) + colorG * energyAlpha;
                                float finalB = bT * (1f - energyAlpha) + colorB * energyAlpha;
                                
                                Color blendedColor = new Color(
                                    (byte)Math.Clamp(finalR * 255f, 0f, 255f),
                                    (byte)Math.Clamp(finalG * 255f, 0f, 255f),
                                    (byte)Math.Clamp(finalB * 255f, 0f, 255f),
                                    (byte)255);

                                bool isBoundary = false;
                                if (px > 0 && ownerFactions[i - 1] != ownerFactionID) isBoundary = true;
                                else if (px < width - 1 && ownerFactions[i + 1] != ownerFactionID) isBoundary = true;
                                else if (py > 0 && ownerFactions[i - width] != ownerFactionID) isBoundary = true;
                                else if (py < height - 1 && ownerFactions[i + width] != ownerFactionID) isBoundary = true;

                                if (isBoundary)
                                {
                                    blendedColor.A = (byte)(blendedColor.A * 0.9f);
                                }
                                
                                mapColors[i] = blendedColor;
                                System.Threading.Interlocked.Increment(ref updatedPixelCount);
                            }
                            else
                            {
                                mapColors[i] = terrainColor;
                            }
                        }
                        else
                        {
                            Color tColor = terrainColor;
                            if (!isKnown)
                            {
                                mapColors[i] = new Color(
                                    (byte)(tColor.R * 0.7f),
                                    (byte)(tColor.G * 0.7f),
                                    (byte)(tColor.B * 0.7f),
                                    (byte)255);
                            }
                            else
                            {
                                mapColors[i] = tColor;
                            }
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
        // 🎯 极速边缘平滑 (Box Blur) - 解决菱形锯齿感
        // 使用 ArrayPool 做到零 GC 分配，符合 AOT 规范
        private void ApplyFastBoxBlur(Color[] colors, int width, int height)
        {
            int totalPixels = width * height;
            Color[] tempColors = System.Buffers.ArrayPool<Color>.Shared.Rent(totalPixels);

            try
            {
                // 迭代 3 次，利用小核产生接近大半径高斯模糊的圆润效果 (消除菱形)
                for (int pass = 0; pass < 1; pass++)
                {
                    Parallel.For(0, height, y =>
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = y * width + x;
                            Color currentC = colors[index];

                            // UI 保护机制：如果是纯白（城池/据点等UI标识），直接跳过模糊，保持锐利
                            if (currentC.R > 240 && currentC.G > 240 && currentC.B > 240)
                            {
                                tempColors[index] = currentC;
                                continue;
                            }

                            int r = 0, g = 0, b = 0;
                            int count = 0;

                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int ny = y + dy;
                                if (ny < 0 || ny >= height) continue;

                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    int nx = x + dx;
                                    if (nx < 0 || nx >= width) continue;

                                    Color c = colors[ny * width + nx];
                                    // 采样时不把周围的白色UI点混合进来，防止UI周围出现白色光晕
                                    if (c.R > 240 && c.G > 240 && c.B > 240) continue;

                                    r += c.R;
                                    g += c.G;
                                    b += c.B;
                                    count++;
                                }
                            }

                            if (count > 0)
                            {
                                tempColors[index] = new((byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)255);
                            }
                            else
                            {
                                tempColors[index] = currentC;
                            }
                        }
                    });

                    Array.Copy(tempColors, colors, totalPixels);
                }
            }
            finally
            {
                System.Buffers.ArrayPool<Color>.Shared.Return(tempColors);
            }
        }
        private Color GetTerrainColor(int terrainId)
        {
            // 统一使用低饱和度、偏暖灰/宣纸色的色板
            switch (terrainId)
            {
                case 0: return new Color(20, 20, 20);      // 无 - 极暗灰
                case 1: return new Color(225, 218, 201);   // 平原 - 宣纸底色
                case 2: return new Color(215, 208, 191);   // 草原 - 略深的宣纸色
                case 3: return new Color(195, 188, 171);   // 森林 - 偏暗的卡其灰
                case 4: return new Color(175, 175, 165);   // 湿地 - 偏冷灰
                case 5: return new Color(185, 170, 155);   // 山地 - 浅赭灰（凸显地形）
                case 6: return new Color(135, 150, 165);   // 水域 - 水墨黛青（低饱和暗蓝）
                case 7: return new Color(165, 150, 140);   // 峻岭 - 暗赭灰
                case 8: return new Color(210, 200, 180);   // 荒地 - 枯草灰
                case 9: return new Color(230, 220, 190);   // 沙漠 - 浅沙黄
                case 10: return new Color(170, 155, 140);  // 栈道 - 灰褐
                default: return new Color(225, 218, 201);  // 默认使用平原底色
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
