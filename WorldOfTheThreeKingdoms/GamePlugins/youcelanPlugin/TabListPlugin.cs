using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using PluginInterface;
using PluginInterface.BaseInterface;
using System;
//using System.Drawing;
using System.Xml;
using WorldOfTheThreeKingdoms;

namespace youcelanPlugin
{

    public class TabListPlugin : GameObject, Iyoucelan, IBasePlugin, IPluginXML, IPluginGraphics
    {
        private string author = "clip_on";
        private const string DataPath = @"Content\Textures\GameComponents\youcelan\Data\";
        private string description = "可选择类别的详细列表";
        private const string Path = @"Content\Textures\GameComponents\youcelan\";
        private string pluginName = "youcelanPlugin";
        public TabListInFrame tabList = new TabListInFrame();
        private string version = "1.0.1";
        private const string XMLFilename = "youcelanData.xml";
        
        // 🔥 新增：异步纹理加载支持
        private bool _texturesLoaded = false;
        private readonly object _loadLock = new();
        private XmlDocument _configDocument;  // 缓存配置文档


        public FrameFunction Function
        {
            get
            {
                return this.tabList.Function;
            }
            set
            {
                this.tabList.Function = value;
            }
        }

        public FrameKind Kind
        {
            get;
            set;
            /*
            get
            {
                return this.tabList.Kind;
            }
            set
            {
                this.tabList.Kind = value;
            }*/
        }

        public void Dispose()
        {
        }

        private static int _drawCallCount = 0;
        
        public void Draw()
        {
            // 🔥 临时诊断：仅输出前 5 次调用,避免性能影响
            // TODO: 诊断完成后移除此代码块
            if (_drawCallCount < 5)
            {
                System.Console.WriteLine($">>> TabListPlugin.Draw 被调用 (第 {++_drawCallCount} 次)");
            }
            
            DrawDiagnostics.LogPluginDraw(this.IsShowing);
            this.tabList.Draw();
        }

        public void Initialize(Screen screen)
        {
        }

        public void InitialValues(object gameObjectList, object selectedObjectList, int scrollValue, string title)
        {
            this.tabList.InitialValues(gameObjectList as GameObjectList, selectedObjectList as GameObjectList, scrollValue, title);
        }

        public void LoadDataFromXMLDocument(string filename)
        {
            // 🔥 修复：youcelanPlugin 在首帧可能被渲染，必须同步加载纹理
            // 日期：2026-02-14
            LoadConfigOnly(filename);
            
            // 立即同步加载纹理（不能异步，否则首帧渲染会失败）
            LoadTexturesSync();
        }
        
        /// <summary>
        /// 同步加载纹理（用于首帧渲染需要的插件）
        /// </summary>
        private void LoadTexturesSync()
        {
            lock (_loadLock)
            {
                if (_texturesLoaded || _configDocument == null) return;
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                System.Diagnostics.Debug.WriteLine("[youcelanPlugin] 开始同步加载纹理...");
                
                try
                {
                    XmlNode nextSibling = _configDocument.FirstChild.NextSibling;
                    
                    // 加载所有纹理
                    XmlNode node = nextSibling.ChildNodes.Item(0);
                    this.tabList.leftedgeTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(1);
                    this.tabList.rightedgeTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(2);
                    this.tabList.topedgeTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(3);
                    this.tabList.bottomedgeTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(4);
                    this.tabList.backgroundTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);

                    node = nextSibling.ChildNodes.Item(5);
                    this.tabList.ToolTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    this.tabList.ToolSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("Selected").Value);
                    this.tabList.ToolDisplayTexture = this.tabList.ToolSelectedTexture;

                    node = nextSibling.ChildNodes.Item(6);
                    this.tabList.tabbuttonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    this.tabList.tabbuttonselectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("SelectedFileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(7);
                    this.tabList.columnheaderTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(8);
                    this.tabList.columnspliterTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(9);
                    this.tabList.scrollbuttonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(10);
                    this.tabList.scrolltrackTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(11);
                    this.tabList.leftArrowTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("LeftFileName").Value);
                    this.tabList.rightArrowTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("RightFileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(12);
                    this.tabList.focusTrackTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(13);
                    this.tabList.checkboxTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    this.tabList.checkboxSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("SelectedFileName").Value);
                    this.tabList.roundcheckboxTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("RoundFileName").Value);
                    this.tabList.roundcheckboxSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("RoundSelectedFileName").Value);

                    node = nextSibling.ChildNodes.Item(19);
                    this.tabList.TopLeftTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(20);
                    this.tabList.TopRightTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(21);
                    this.tabList.BottomLeftTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    node = nextSibling.ChildNodes.Item(22);
                    this.tabList.BottomRightTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\youcelan\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                    
                    _texturesLoaded = true;
                    sw.Stop();
                    System.Diagnostics.Debug.WriteLine($"[youcelanPlugin] 纹理加载完成: {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[youcelanPlugin] 纹理加载失败: {ex.Message}");
                }
            }
        }
        
        private void LoadConfigOnly(string filename)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            Font font;
            Microsoft.Xna.Framework.Color color;
            _configDocument = new();
            string xml = Platform.Current.LoadText(filename);
            _configDocument.LoadXml(xml);
            XmlNode nextSibling = _configDocument.FirstChild.NextSibling;

            // 只读取尺寸和配置，不加载纹理
            XmlNode node = nextSibling.ChildNodes.Item(0);
            this.tabList.leftedgeWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            
            node = nextSibling.ChildNodes.Item(1);
            this.tabList.rightedgeWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            
            node = nextSibling.ChildNodes.Item(2);
            this.tabList.topedgeWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            
            node = nextSibling.ChildNodes.Item(3);
            this.tabList.bottomedgeWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);

            node = nextSibling.ChildNodes.Item(5);
            this.tabList.ToolPosition = StaticMethods.LoadRectangleFromXMLNode(node);

            node = nextSibling.ChildNodes.Item(6);
            this.tabList.tabbuttonWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            this.tabList.tabbuttonHeight = int.Parse(node.Attributes.GetNamedItem("Height").Value);
            
            node = nextSibling.ChildNodes.Item(7);
            this.tabList.columnheaderHeight = int.Parse(node.Attributes.GetNamedItem("Height").Value);
            
            node = nextSibling.ChildNodes.Item(8);
            this.tabList.columnspliterWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            this.tabList.columnspliterHeight = int.Parse(node.Attributes.GetNamedItem("Height").Value);
            
            node = nextSibling.ChildNodes.Item(9);
            this.tabList.scrollbuttonWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            
            node = nextSibling.ChildNodes.Item(10);
            this.tabList.scrolltrackWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            
            node = nextSibling.ChildNodes.Item(13);
            this.tabList.checkboxName = node.Attributes.GetNamedItem("Name").Value;
            this.tabList.checkboxDisplayName = node.Attributes.GetNamedItem("DisplayName").Value;
            this.tabList.checkboxWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            
            node = nextSibling.ChildNodes.Item(14);
            this.tabList.PortraitWidth = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            this.tabList.PortraitHeight = int.Parse(node.Attributes.GetNamedItem("Height").Value);
            
            node = nextSibling.ChildNodes.Item(15);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.tabList.TabTextBuilder.SetFreeTextBuilder(font);
            this.tabList.TabTextColor = color;
            this.tabList.TabTextAlign = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            
            node = nextSibling.ChildNodes.Item(16);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.tabList.ColumnTextBuilder.SetFreeTextBuilder(font);
            this.tabList.ColumnTextColor = color;
            this.tabList.ColumnTextAlign = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            
            node = nextSibling.ChildNodes.Item(17);
            this.tabList.SelectSoundFile = @"Content\Sound\" + node.Attributes.GetNamedItem("Select").Value;
            
            node = nextSibling.ChildNodes.Item(18);
            this.tabList.TopLeftPosition.X  = int.Parse(node.Attributes.GetNamedItem("X").Value);
            this.tabList.TopLeftPosition.Y = int.Parse(node.Attributes.GetNamedItem("Y").Value);

            this.tabList.LoadFromXMLNode(nextSibling.ChildNodes.Item(23));
            
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[youcelanPlugin] 配置加载耗时: {sw.ElapsedMilliseconds} ms");
        }

        public void RefreshEditable()
        {
            this.tabList.RefreshEditable();
        }

        public void SetArchitectureDetailDialog(IArchitectureDetail iArchitectureDetail)
        {
            this.tabList.iArchitectureDetail = iArchitectureDetail;
        }

        public void SetFactionTechniquesDialog(IFactionTechniques iFactionTechniques)
        {
            this.tabList.iFactionTechniques = iFactionTechniques;
        }

        public void SetGameFrame(IGameFrame iGameFrame)
        {
            this.tabList.iGameFrame = iGameFrame;
        }

        public void SetGraphicsDevice()
        {
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\youcelanData.xml");
        }

        public void SetListKindByName(string Name, bool ShowCheckBox, bool MultiSelecting)
        {
            this.tabList.SetListKindByName(Name, ShowCheckBox, MultiSelecting);
        }

        public void SetMapViewSelector(IMapViewSelector iMapViewSelector)
        {
            this.tabList.iMapViewSelector = iMapViewSelector;
            //iMapViewSelector.SetTabList(this);
        }

        public void SetPersonDetailDialog(IPersonDetail iPersonDetail)
        {
            this.tabList.iPersonDetail = iPersonDetail;
        }

        public void SetScreen(Screen screen)
        {
            this.tabList.Initialize();
        }

        public void SetSelectedItemMaxCount(int max)
        {
            this.tabList.SelectedItemMaxCount = max;
        }

        public void SetSelectedTab(string tabName)
        {
            this.tabList.SetSelectedTab(tabName);
        }

        public void SetTreasureDetailDialog(ITreasureDetail iTreasureDetail)
        {
            this.tabList.iTreasureDetail = iTreasureDetail;
        }

        public void SetTroopDetailDialog(ITroopDetail iTroopDetail)
        {
            this.tabList.iTroopDetail = iTroopDetail;
        }

        public void Update(GameTime gameTime)
        {
            // ✅ 修复：隐藏时不强制收起右侧栏，避免干扰拖动逻辑
            // 原逻辑：每帧检查 IsShowing，如果为 false 就强制收起
            // 问题：拖动过程中如果 IsShowing 变为 false，会中断拖动
            // 解决：移除强制收起逻辑，让 TabListInFrame 自己管理展开/收起状态
            if (!this.IsShowing)
            {
                return;
            }
            
            // 🔥 性能修复：移除每帧调用 ReCalculate()
            // 日期：2026-02-17
            // 问题：原代码每帧调用 ReCalculate()，导致每帧分配大量 Rectangle 对象
            // 解决：ReCalculate() 应该只在布局改变时调用（例如选中标签、窗口大小改变）
            // 参考：TabListPlugin.Update() 是空的，不需要每帧重新计算
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

        public bool IsShowing
        {
            get
            {
                return this.tabList.IsShowing;
            }
            set
            {
                this.tabList.IsShowing = value;
            }
        }

        public string PluginName
        {
            get
            {
                return this.pluginName;
            }
        }

        public object SelectedItem
        {
            get
            {
                return this.tabList.SelectedItem;
            }
        }

        public object SelectedItemList
        {
            get
            {
                return this.tabList.SelectedItemList;
            }
        }

        public object TabList
        {
            get
            {
                return this.tabList;
            }
        }

        public string Version
        {
            get
            {
                return this.version;
            }
        }

        public Microsoft.Xna.Framework.Rectangle FrameRectangle
        {
            get
            {
                if (this.tabList.xianshiyoucelan)
                {
                    return new Microsoft.Xna.Framework.Rectangle(this.tabList.FramePosition.X - 18, this.tabList.FramePosition.Y - 28, this.tabList.FramePosition.Width + 32, this.tabList.FramePosition.Height + 54);
                }
                else
                {
                    return this.tabList.ToolDisplayPosition;
                }
            }
        }

        public void SetyoucelanContent(Microsoft.Xna.Framework.Point viewportSize)
        {
            //if (content is FrameContent)
            //{
            this.tabList.SetyoucelanContent(viewportSize);
            //}
        }
    }
}

