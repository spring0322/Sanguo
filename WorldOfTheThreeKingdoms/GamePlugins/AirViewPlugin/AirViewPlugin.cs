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
using GameGlobal;
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
        
        // 新增：战略小地图相关字段
        private Texture2D AirViewImage;
        private Rectangle DisplayRect;

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
            for (int i = 0; i < Enum.GetValues(typeof(TerrainKind)).Length; i++)
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
            this.airView.Align = (ToolAlign)Enum.Parse(typeof(ToolAlign), node.Attributes.GetNamedItem("Align").Value);
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

        // 高性能版本：使用数据快照和并行优化
        public void CreateStrategicMinimap(GameObjects.GameScenario scenario)
        {
            // 使用本地引用，防止循环中多次访问 Session 造成的开销
            var territoryManager = Session.Current.TerritoryManager;
            if (territoryManager == null) return;

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

                System.Diagnostics.Debug.WriteLine($"[AirView] 开始生成战略小地图: {width}x{height}");

                // 2. 重新计算势力版图 (确保这一步不在并行循环中，因为它可能修改数据)
                // 注意：如果此操作非常耗时，应考虑将其移出渲染帧逻辑
                System.Diagnostics.Debug.WriteLine("[AirView] 重新计算势力版图...");
                territoryManager.RecalculateTerritory(scenario.Architectures, scenario.ScenarioMap.MapData);
                System.Diagnostics.Debug.WriteLine("[AirView] 势力版图计算完成");

                // 3. 数据快照 (Snapshot Data) - 关键优化
                // 为了线程安全和速度，将需要的数据预先提取到一维数组中
                // 这样在 Parallel.For 中就不需要访问全局 Session 或复杂的对象图
                int[] terrainIds = new int[totalPixels];
                int[] ownerFactions = new int[totalPixels];
                float[] strengths = new float[totalPixels];
                bool[] isBorders = new bool[totalPixels];

                // 预取数据 (这一步如果是单线程瓶颈，也可以尝试并行，但需确保 Get 方法线程安全)
                // 这里假设 GetTerrainDetailByPositionNoCheck 和 GetTerritoryOwner 访问开销较小
                // 如果可以直接访问底层数组会更快
                Parallel.For(0, totalPixels, i =>
                {
                    int x = i % width;
                    int y = i / width;

                    var terrainDetail = scenario.GetTerrainDetailByPositionNoCheck(new Point(x, y));
                    terrainIds[i] = terrainDetail?.ID ?? 0;
                    ownerFactions[i] = territoryManager.GetTerritoryOwner(x, y);
                    isBorders[i] = territoryManager.IsBorderPixel(x, y);
                    strengths[i] = territoryManager.GetTerritoryStrength(x, y);
                });

                // 4. 并行生成颜色数组
                Color[] mapColors = new Color[totalPixels];

                // 预先获取所有势力的引用，避免循环中查找
                // 假设 FactionID 是连续的或者用字典缓存颜色会更好
                // 这里为了保持逻辑简单，保留原逻辑但在循环外处理颜色获取的优化思路
                Parallel.For(0, totalPixels, i =>
                {
                    int terrainId = terrainIds[i];
                    int ownerFaction = ownerFactions[i];
                    bool isBorder = isBorders[i];

                    // 获取地形基础色 (建议 GetTerrainColor 内部仅仅是查表，不要有逻辑)
                    Color terrainColor = GetTerrainColor(terrainId);

                    if (isBorder)
                    {
                        mapColors[i] = Color.Gold;
                    }
                    else if (ownerFaction >= 0)
                    {
                        // 获取势力颜色
                        // 优化：实际项目中建议传入一个 Dictionary<int, Color> factionColors 避免重复查找 GameObject
                        var faction = scenario.Factions.GetGameObject(ownerFaction) as Faction;
                        if (faction != null)
                        {
                            Color factionColor = GetFactionColor(faction);
                            float strength = strengths[i];

                            // 调整混合比例，使得核心区域势力色更重，边缘地形色更重
                            float blendRatio = 0.4f + (strength * 0.4f);
                            mapColors[i] = Color.Lerp(terrainColor, factionColor, blendRatio);
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

                // 5. 资源清理与纹理生成 (主线程)
                // [重要] 释放旧的纹理资源，防止显存泄漏
                if (this.AirViewImage != null && !this.AirViewImage.IsDisposed)
                {
                    this.AirViewImage.Dispose();
                }

                // 清理缓存中的旧引用 (根据你的 CacheManager 实现调整)
                string textureName = "StrategicMinimap";
                if (CacheManager.TextureTempDics.ContainsKey(textureName))
                {
                    // 如果缓存里存的是同一个引用，也要确保释放逻辑正确
                    // CacheManager.TextureTempDics[textureName].Dispose(); // 视具体实现而定
                }

                // 创建新纹理
                Texture2D strategyMap = new Texture2D(Platform.GraphicsDevice, width, height);
                strategyMap.SetData(mapColors);

                // 6. 更新引用
                this.AirViewImage = strategyMap;
                this.airView.MapTexture = new PlatformTexture()
                {
                    Name = textureName,
                    Width = this.AirViewImage.Width,
                    Height = this.AirViewImage.Height
                };

                // 更新缓存
                CacheManager.TextureTempDics[textureName] = this.AirViewImage;
                System.Diagnostics.Debug.WriteLine($"[AirView] 战略小地图生成完成: {width}x{height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AirView] 生成战略小地图时发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AirView] 异常堆栈: {ex.StackTrace}");
            }
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
