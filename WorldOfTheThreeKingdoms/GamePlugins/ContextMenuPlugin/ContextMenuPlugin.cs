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


namespace ContextMenuPlugin
{

    public class ContextMenuPlugin : GameObject, IGameContextMenu, IBasePlugin, IPluginXML, IPluginGraphics
    {
        private string author = "clip_on";
        private ContextMenu contextMenu = new ContextMenu();
        public ContextMenu ContextMenu { get { return this.contextMenu; } }
        private const string DataPath = @"Content\Textures\GameComponents\ContextMenu\Data\";
        private string description = "上下文菜单";
        private const string Path = @"Content\Textures\GameComponents\ContextMenu\";
        private string pluginName = "ContextMenuPlugin";
        private string version = "1.0.0";
        private const string XMLFilename = "ContextMenuData.xml";

        // 作弊模式开关
        public static bool CheatMode = false;
        
        // 🔥 新增：延迟初始化支持
        private bool _scenarioInitialized = false;
        private readonly object _initLock = new();

        public void Dispose()
        {
        }

        public void Draw()
        {
            this.contextMenu.Draw();
        }

        public void Initialize(Screen screen)
        {
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
            this.contextMenu.RightClickItemTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.contextMenu.RightClickItemSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("SelectedFileName").Value);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);

            this.contextMenu.RightClickFreeTextBuilder = font;
            //this.contextMenu.RightClickFreeTextBuilder.SetFreeTextBuilder(font);

            this.contextMenu.RightClickTextColor = color;
            node = nextSibling.ChildNodes.Item(1);
            this.contextMenu.LeftClickItemTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.contextMenu.LeftClickItemSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("SelectedFileName").Value);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);

            this.contextMenu.LeftClickFreeTextBuilder = font;
            //this.contextMenu.LeftClickFreeTextBuilder.SetFreeTextBuilder(font);

            this.contextMenu.LeftClickTextColor = color;
            node = nextSibling.ChildNodes.Item(2);
            this.contextMenu.DisabledItemTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.contextMenu.DisabledItemSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("SelectedFileName").Value);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);

            this.contextMenu.DisabledFreeTextBuilder = font;
            //this.contextMenu.DisabledFreeTextBuilder.SetFreeTextBuilder(font);

            this.contextMenu.DisabledTextColor = color;
            node = nextSibling.ChildNodes.Item(3);
            this.contextMenu.RightDisabledItemTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.contextMenu.RightDisabledItemSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("SelectedFileName").Value);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);

            this.contextMenu.RightDisabledFreeTextBuilder = font;
            //this.contextMenu.RightDisabledFreeTextBuilder.SetFreeTextBuilder(font);

            this.contextMenu.RightDisabledTextColor = color;
            node = nextSibling.ChildNodes.Item(4);
            this.contextMenu.HasChildTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ContextMenu\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            node = nextSibling.ChildNodes.Item(5);
            this.contextMenu.ClickSoundFile = @"Content\Sound\" + node.Attributes.GetNamedItem("Click").Value;
            this.contextMenu.OpenSoundFile = @"Content\Sound\" + node.Attributes.GetNamedItem("Open").Value;
            this.contextMenu.FoldSoundFile = @"Content\Sound\" + node.Attributes.GetNamedItem("Fold").Value;
            node = nextSibling.ChildNodes.Item(6);
            this.contextMenu.LoadFromXmlNode(node);
        }

        public void Prepare(int X, int Y, Microsoft.Xna.Framework.Point viewportSize)
        {
            // 🔥 在显示菜单前确保场景数据已初始化
            if (!_scenarioInitialized)
            {
                SetScenarioLazy();
            }
            
            if (Session.LargeContextMenu)
            {
                this.contextMenu.Prepare(365, Y, viewportSize);                
            }
            else
            {
                this.contextMenu.Prepare(X, Y, viewportSize);
            }
        }

        public void SetCurrentGameObject(object gameObject)
        {
            this.contextMenu.CurrentGameObject = gameObject;
        }

        public void SetGraphicsDevice()
        {
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\ContextMenuData.xml");
        }

        // 🔥 实现延迟初始化接口
        public bool SupportLazyInit => true;
        
        public void SetScenario()
        {
            // 空实现，延迟到真正使用时
        }
        
        public void SetScenarioLazy()
        {
            lock (_initLock)
            {
                if (_scenarioInitialized) return;
                
                System.Diagnostics.Debug.WriteLine("[ContextMenuPlugin] 开始延迟初始化场景数据...");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                
                // 原有的初始化逻辑
                InitializeScenarioData();
                
                _scenarioInitialized = true;
                sw.Stop();
                System.Diagnostics.Debug.WriteLine($"[ContextMenuPlugin] 延迟初始化完成: {sw.ElapsedMilliseconds} ms");
            }
        }
        
        private void InitializeScenarioData()
        {
            foreach (MenuKind kind in this.contextMenu.MenuKinds)
            {
                string kindName = kind.Name;
                if (kindName.Equals("TroopLeftClick"))
                {
                    foreach (MenuItem i in kind.MenuItems)
                    {
                        if (i.Name.Equals("TroopCombatMethod")|| i.Name.Equals("TroopAutoCombatMethod"))
                        {
                            i.MenuItems.Clear();
                            foreach (GameObjects.TroopDetail.CombatMethod m in Session.Current.Scenario.GameCommonData.AllCombatMethods.CombatMethods.Values)
                            {
                                MenuItem item = new MenuItem(i, kind, kind.contextMenu);
                                item.ID = m.ID;
                                item.Name = m.ID.ToString();
                                item.DisplayName = m.Name;
                                item.ChangeDisplayName = "GetCombatMethodDisplayName";
                                item.DisplayIfTrue = "HasCombatMethod";
                                item.IsParamIDItem = true;
                                item.Param = m.ID.ToString();
                                i.MenuItems.Add(item);
                            }
                        }
                        else if (i.Name.Equals("TroopStratagem"))
                        {
                            i.MenuItems.Clear();
                            foreach (GameObjects.TroopDetail.Stratagem m in Session.Current.Scenario.GameCommonData.AllStratagems.Stratagems.Values)
                            {
                                MenuItem item = new MenuItem(i, kind, kind.contextMenu);
                                item.ID = m.ID;
                                item.Name = m.ID.ToString();
                                item.DisplayName = m.Name;
                                item.ChangeDisplayName = "GetStratagemDisplayName";
                                item.DisplayIfTrue = "HasStratagem";
                                item.Param = m.ID.ToString();
                                i.MenuItems.Add(item);
                            }
                        }
                        else if (i.Name.Equals("TroopStunt"))
                        {
                            i.MenuItems.Clear();
                            foreach (GameObjects.PersonDetail.Stunt m in Session.Current.Scenario.GameCommonData.AllStunts.Stunts.Values)
                            {
                                MenuItem item = new MenuItem(i, kind, kind.contextMenu);
                                item.ID = m.ID;
                                item.Name = m.ID.ToString();
                                item.DisplayName = m.Name;
                                item.ChangeDisplayName = "GetStuntDisplayName";
                                item.DisplayIfTrue = "HasStunt";
                                item.IsParamIDItem = true;
                                item.Param = m.ID.ToString();
                                i.MenuItems.Add(item);
                            }
                        }
                    }
                }
                
            }
        }

        public void SetIHelp(IHelp iHelp)
        {
            this.contextMenu.HelpPlugin = iHelp;
        }

        public void SetMenuKindByID(int ID)
        {
            this.contextMenu.SetMenuKindByID(ID);
        }

        public void SetMenuKindByName(string Name)
        {
            this.contextMenu.SetMenuKindByName(Name);
        }

        public void SetScreen(Screen screen)
        {
            this.contextMenu.Initialize();
        }

        public void Update(GameTime gameTime)
        {
            // 检测作弊模式切换: Ctrl + Z
            if (InputManager.KeyBoardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Z) && 
                InputManager.KeyBoardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) && 
                InputManager.KeyBoardStatePre.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.Z))
            {
                CheatMode = !CheatMode;
                System.Diagnostics.Debug.WriteLine($"[ContextMenu] CheatMode toggled to: {CheatMode}");

                // 强制刷新当前菜单可见性
                this.contextMenu.RefreshAllItemsVisible();
            }
            
            // 监听全局作弊模式变化（通过Ctrl+Shift+Z切换）
            if (Session.GlobalVariables.EnableCheat != lastGlobalCheatState)
            {
                lastGlobalCheatState = Session.GlobalVariables.EnableCheat;
                System.Diagnostics.Debug.WriteLine($"[ContextMenu] Global EnableCheat changed to: {lastGlobalCheatState}");
                this.contextMenu.RefreshAllItemsVisible();
            }
        }

        private static bool lastGlobalCheatState = false;


        public void ShezhiBianduiLiebiaoXinxi(bool Xianshi, Microsoft.Xna.Framework.Rectangle Weizhi)
        {
            this.contextMenu.BianduiLiebiaoXianshi = Xianshi;
            this.contextMenu.BianduiLiebiaoWeizhi = Weizhi;
        }

        public string Author
        {
            get
            {
                return this.author;
            }
        }

        public int CurrentParamID
        {
            get
            {
                return this.contextMenu.CurrentParamID;
            }
        }

        public object CurrentGameObject
        {
            get
            {
                return this.contextMenu.CurrentGameObject;
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

        public void ClearFunctions()
        {
            this.contextMenu.ClearFunctions();
        }

        public void AddMenu(string DisplayName, Action SelectedAction)
        {
            this.contextMenu.AddMenu(DisplayName, SelectedAction);
        }

        public void Show()
        {
            this.contextMenu.Show();
        }

        public bool IsShowing
        {
            get
            {
                return this.contextMenu.IsShowing;
            }
            set
            {
                this.contextMenu.IsShowing = value;
            }
        }

        public ContextMenuKind Kind
        {
            get
            {
                return this.contextMenu.Kind;
            }
        }

        public string PluginName
        {
            get
            {
                return this.pluginName;
            }
        }

        public ContextMenuResult Result
        {
            get
            {
                return this.contextMenu.Result;
            }
        }

        public string Version
        {
            get
            {
                return this.version;
            }
        }
    }
}

