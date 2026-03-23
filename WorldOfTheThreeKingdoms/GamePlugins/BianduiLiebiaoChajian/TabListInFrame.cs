using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Xml;

namespace BianduiLiebiaoChajian
{
    public class TabListInFrame : FrameContent
    {
        private Rectangle TopLeftRectangle;
        internal PlatformTexture TopLeftTexture;
        private Rectangle TopRightRectangle;
        internal PlatformTexture TopRightTexture;
        private Rectangle BottomLeftRectangle;
        internal PlatformTexture BottomLeftTexture;
        private Rectangle BottomRightRectangle;
        internal PlatformTexture BottomRightTexture;

        internal Point TopLeftPosition = new Point();
        internal string checkboxDisplayName;
        internal string checkboxName;
        internal PlatformTexture checkboxSelectedTexture;
        internal PlatformTexture checkboxTexture;
        internal int checkboxWidth;
        internal int columnheaderHeight;
        internal PlatformTexture columnheaderTexture;
        internal int columnspliterHeight;
        internal PlatformTexture columnspliterTexture;
        internal int columnspliterWidth;
        internal TextAlign ColumnTextAlign;

        internal Font ColumnTextBuilder = new Font();

        internal Color ColumnTextColor;
        internal bool DrawFocused = false;
        private bool firstTimeMapViewSelector = true;
        internal int Focused;
        internal GameObject FocusedObject;
        internal PlatformTexture focusTrackTexture;
        internal Rectangle FullLowerClient;
        internal GameObjectList gameObjectList;
        private bool HeightCanShrink = true;
        internal IArchitectureDetail iArchitectureDetail;
        internal IFactionTechniques iFactionTechniques;
        internal IGameFrame iGameFrame;
        internal IMapViewSelector iMapViewSelector;
        internal IPersonDetail iPersonDetail;
        internal ITreasureDetail iTreasureDetail;
        internal ITroopDetail iTroopDetail;
        internal PlatformTexture leftArrowTexture;
        internal List<ListKind> ListKinds;
        private ListKind listKindToDisplay;
        internal bool MovingHorizontalScrollBar = false;
        internal bool MovingVerticalScrollBar = false;
        internal bool MultiSelecting = false;
        private Point oldMousePosition;
        internal int oldScrollValue;
        internal int PortraitHeight;
        internal int PortraitWidth;
        internal PlatformTexture rightArrowTexture;
        private bool RightClickClose = true;
        internal SubKind RootListKind;
        internal PlatformTexture roundcheckboxSelectedTexture;
        internal PlatformTexture roundcheckboxTexture;
        internal int rowHeight;
        internal List<Rectangle> RowRectangles;
        
        internal PlatformTexture scrollbuttonTexture;
        internal int scrollbuttonWidth;
        internal PlatformTexture scrolltrackTexture;
        internal int scrolltrackWidth;
        internal GameObject SelectedItem;
        internal GameObjectList SelectedItemList;
        internal int SelectedItemMaxCount;
        internal bool SelectingBool = false;
        internal bool SelectingRows = false;
        internal string SelectSoundFile;
        internal bool ShowCheckBox = false;
        internal bool ShowHorizontalScrollBar = false;
        internal bool ShowVerticalScrollBar = false;
        private Stack<SubKind> SubKinds = new Stack<SubKind>();
        internal int tabbuttonHeight;
        internal PlatformTexture tabbuttonselectedTexture;
        internal PlatformTexture tabbuttonTexture;
        internal int tabbuttonWidth;
        internal TextAlign TabTextAlign;

        internal Font TabTextBuilder = new Font();
        internal Color TabTextColor;
        internal TextAlign TextAlign;
        internal Font TextBuilder = new Font();
        internal Color TextColor;
        internal PlatformTexture upArrowTexture;
        internal Rectangle VisibleLowerClient;
        private bool WidthCanShrink = true;

        // Missing properties that are referenced in the codebase
        internal int Bingyi = 0;
        internal Rectangle backgroundRectangle;
        internal int leftedgeWidth;
        internal Rectangle bottomedgeRectangle;
        internal PlatformTexture bottomedgeTexture;
        internal int bottomedgeWidth;
        internal Rectangle leftedgeRectangle;
        internal PlatformTexture leftedgeTexture;
        internal Rectangle rightedgeRectangle;
        internal PlatformTexture rightedgeTexture;
        internal int rightedgeWidth;
        internal Rectangle topedgeRectangle;
        internal PlatformTexture topedgeTexture;
        internal int topedgeWidth;
        internal PlatformTexture backgroundTexture;

        // Tool-related properties
        internal PlatformTexture ToolTexture;
        internal PlatformTexture ToolSelectedTexture;
        internal PlatformTexture ToolDisplayTexture;
        internal Rectangle ToolPosition;

        public TabListInFrame()
        {
            this.ListKinds = new List<ListKind>();
            this.SelectedItemList = new GameObjectList();
            this.RowRectangles = new List<Rectangle>();
        }

        internal void AddListKind(ListKind listKind)
        {
            this.ListKinds.Add(listKind);
        }

        internal void SetGameObjectList(GameObjectList list)
        {
            this.gameObjectList = list;
        }

        public void InitialValues(GameObjectList gameObjectList, GameObjectList selectedObjectList, int scrollValue, string title)
        {
            this.SetObjectList(gameObjectList);
            this.SetSelectedObjectList(selectedObjectList);
            // Additional initialization logic can be added here
        }

        private void SetObjectList(GameObjectList gameObjectList)
        {
            this.gameObjectList = gameObjectList;
            foreach (GameObject obj in gameObjectList)
            {
                obj.Selected = false;
            }
        }

        private void SetSelectedObjectList(GameObjectList selectedObjectList)
        {
            if (selectedObjectList != null)
            {
                foreach (GameObject obj in selectedObjectList)
                {
                    obj.Selected = true;
                }
            }
        }

        public void RefreshEditable()
        {
            // 刷新可编辑状态的基本实现
        }

        public void SetListKindByName(string name, bool showCheckBox, bool multiSelecting)
        {
            // 根据名称设置列表类型的基本实现
            foreach (var listKind in this.ListKinds)
            {
                if (listKind.Name == name)
                {
                    this.listKindToDisplay = listKind;
                    break;
                }
            }
        }

        public void Initialize()
        {
            // 初始化的基本实现
            this.ListKinds = new List<ListKind>();
            this.RowRectangles = new List<Rectangle>();
        }

        public void SetSelectedTab(string tabName)
        {
            // 设置选中标签的基本实现
            if (this.listKindToDisplay != null)
            {
                foreach (var tab in this.listKindToDisplay.Tabs)
                {
                    if (tab.Name == tabName)
                    {
                        tab.Selected = true;
                        this.listKindToDisplay.SelectedTab = tab;
                    }
                    else
                    {
                        tab.Selected = false;
                    }
                }
            }
        }

        public void SetyoucelanContent(Point viewportSize)
        {
            // 设置右侧栏内容的基本实现
        }

        public Rectangle GetRealLowerVisibleClient()
        {
            return this.VisibleLowerClient;
        }

        public void ShrinkRectanglesHeight()
        {
            // 缩小矩形高度的基本实现
        }

        public void EnlargeRectanglesHeight()
        {
            // 扩大矩形高度的基本实现
        }

        public void ShrinkRectanglesWidth()
        {
            // 缩小矩形宽度的基本实现
        }

        public void EnlargeRectanglesWidth()
        {
            // 扩大矩形宽度的基本实现
        }

        public void AddRows()
        {
            // 添加行的基本实现
            if (this.gameObjectList != null)
            {
                this.RowRectangles.Clear();
                for (int i = 0; i < this.gameObjectList.Count; i++)
                {
                    this.RowRectangles.Add(new Rectangle(this.VisibleLowerClient.X, 
                        (this.VisibleLowerClient.Y + this.columnheaderHeight) + (this.rowHeight * i), 
                        this.VisibleLowerClient.Width - 1, this.rowHeight));
                }
            }
        }

        public void LoadFromXMLNode(XmlNode rootNode)
        {
            // Basic XML loading implementation - can be expanded as needed
            if (rootNode.Attributes["RowHeight"] != null)
            {
                this.rowHeight = int.Parse(rootNode.Attributes["RowHeight"].Value);
            }
            
            // Load ListKinds from XML
            foreach (XmlNode node in rootNode.ChildNodes)
            {
                if (node.Name == "ListKind")
                {
                    ListKind item = new ListKind(this);
                    item.LoadFromXMLNode(node);
                    this.ListKinds.Add(item);
                }
            }
        }

        // SubKind 类定义
        internal class SubKind
        {
            public string Name { get; set; }
            public int ID { get; set; }
            public GameObjectList GameObjects { get; set; }

            public SubKind()
            {
                this.GameObjects = new GameObjectList();
            }
        }
    }
}