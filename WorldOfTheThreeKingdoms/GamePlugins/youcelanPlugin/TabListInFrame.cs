using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PluginInterface;
using System;
using System.Collections.Generic;
//using System.Runtime.InteropServices;
using System.Xml;

namespace youcelanPlugin
{

    public class TabListInFrame : FrameContent
    {
        private Rectangle TopLeftRectangle;
        internal PlatformTexture TopLeftTexture;
        //internal int TopLeftWidth;
        private Rectangle TopRightRectangle;
        internal PlatformTexture TopRightTexture;
        //internal int TopRightWidth;
        private Rectangle BottomLeftRectangle;
        internal PlatformTexture BottomLeftTexture;
        //internal int BottomLeftWidth;
        private Rectangle BottomRightRectangle;
        internal PlatformTexture BottomRightTexture;
        //internal int BottomRightWidth;


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
        internal ListKind listKindToDisplay;
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
        internal string Title = "";
        internal Rectangle VisibleLowerClient;

        internal PlatformTexture ToolDisplayTexture;
        internal Rectangle ToolPosition;
        internal PlatformTexture ToolSelectedTexture;
        internal PlatformTexture ToolTexture;

        private bool WidthCanShrink = true;


        public  bool xianshiyoucelan=true;


        private Rectangle backgroundRectangle;
        internal PlatformTexture backgroundTexture;

        private Rectangle bottomedgeRectangle;
        internal PlatformTexture bottomedgeTexture;
        internal int bottomedgeWidth;


    
        private Rectangle leftedgeRectangle;
        internal PlatformTexture leftedgeTexture;
        internal int leftedgeWidth;
    
   
        internal Rectangle Position;
     
        private Rectangle rightedgeRectangle;
        internal PlatformTexture rightedgeTexture;
        internal int rightedgeWidth;
    
      
        private Rectangle topedgeRectangle;
        internal PlatformTexture topedgeTexture;
        internal int topedgeWidth;
        
        internal Rectangle ToolDisplayPosition;
        //private FrameContent frameContent = null;
        internal bool jiancexianshi=false ;

        // 🎯 右侧栏切换按钮
        internal PlatformTexture ArchitectureListButtonTexture;
        internal PlatformTexture ArchitectureListButtonSelectedTexture;
        internal PlatformTexture TroopListButtonTexture;
        internal PlatformTexture TroopListButtonSelectedTexture;
        internal Rectangle ArchitectureListButtonPosition;
        internal Rectangle TroopListButtonPosition;
        public bool isShowingTroopList { get; private set; } = false;  // 🔥 2026-03-05 改为公共属性：支持外部读取当前列表类型
        
        // 使用文字按钮（临时方案，直到有图片资源）
        internal FreeText ArchitectureListButtonText;
        internal FreeText TroopListButtonText;

        // 🎯 拖动功能
        private bool isDragging = false;
        private bool hasDragged = false;  // 标记是否发生过拖动
        private bool wasExpandedOnMouseDown = false;  // 记录按下鼠标时的展开状态
        private Point dragStartMousePosition;
        private Point dragStartFramePosition;
        private Rectangle titleDragArea;
        private const int DragThreshold = 10;  // 拖动阈值（像素）
        private DateTime mouseDownTime;  // 记录鼠标按下时间
        private const int ClickTimeThresholdMs = 200;  // 提高到 200ms，给真正的点击更宽裕的判定时间

        // 🔥 性能优化：更新按钮颜色（只在状态改变时调用，不在Draw中每帧调用）
        private void UpdateButtonColors()
        {
            this.ArchitectureListButtonText.TextColor = this.isShowingTroopList ? Color.Gray : Color.Yellow;
            this.TroopListButtonText.TextColor = this.isShowingTroopList ? Color.Yellow : Color.Gray;
        }

        public void SetyoucelanContent(Point viewportSize)
        {
            // ✅ 修复：只在第一次初始化时设置位置，之后保持用户拖动的位置
            if (this.Position.Width == 0 || this.Position.Height == 0)
            {
                // 第一次初始化：设置默认位置
                this.FramePosition = GetRectangleFityoucelan(this.DefaultFrameWidth, this.DefaultFrameHeight, viewportSize);
                this.SetPosition(this.FramePosition);
            }
            // 否则保持当前位置，不重置
            
            this.ReCalculate();
        }
        /*
        private void frameContent_OnItemClick()
        {
            if (this.Function == FrameFunction.Jump)
            {
                //this.IsShowing = false;
                if (Session.MainGame.mainGameScreen.PopUndoneWork().Kind != UndoneWorkKind.None)
                {
                    //throw new Exception("The UndoneWork is not a Frame.");  //错误检查
                }

            }
        }*/



        private Rectangle GetRectangleFityoucelan(int width, int height, Point viewportSize)
        {
            int x = width;
            int y = height;
            if (viewportSize.X < width)
            {
                x = viewportSize.X;
            }
            if (viewportSize.Y < height)
            {
                y = viewportSize.Y;
            }
            
            // ✅ 修改初始位置：放在屏幕左侧，距离顶部100px
            int initialX = 20;  // 距离左边缘20px
            int initialY = 100;  // 距离顶部100px，避免挡住其他UI
            
            return new(initialX, initialY, x, y);
        }
        
        private void SetPosition(Rectangle position)
        {
            this.Position = position;
            this.FramePosition = position;  // ✅ 修复：更新 FramePosition，这会自动更新 RealClient
            this.ResetRectangles();
            this.ReCalculate();
        }

        private void ResetRectangles()
        {
            this.leftedgeRectangle = new Rectangle(this.Position.X - this.leftedgeWidth, this.Position.Y, this.leftedgeWidth, this.Position.Height);
            this.rightedgeRectangle = new Rectangle(this.Position.X + this.Position.Width, this.Position.Y, this.rightedgeWidth, this.Position.Height);
            this.topedgeRectangle = new Rectangle(this.Position.X, this.Position.Y - this.topedgeWidth, this.Position.Width, this.topedgeWidth);
            this.bottomedgeRectangle = new Rectangle(this.Position.X, this.Position.Y + this.Position.Height, this.Position.Width, this.bottomedgeWidth);
            this.backgroundRectangle = new Rectangle(this.Position.X, this.Position.Y, this.Position.Width, this.Position.Height);

            // 🎯 根据展开/收起状态动态计算 ToolDisplayPosition
            if (this.xianshiyoucelan)
            {
                // 展开状态：标题在右侧栏内部
                this.ToolDisplayPosition = new Rectangle(
                    this.Position.X + this.ToolPosition.X,
                    this.Position.Y + this.ToolPosition.Y,
                    this.ToolPosition.Width,
                    this.ToolPosition.Height
                );
            }
            else
            {
                // 收起状态：标题贴在屏幕左侧边缘
                this.ToolDisplayPosition = new Rectangle(
                    0,  // 贴左边缘
                    this.Position.Y + this.ToolPosition.Y,
                    this.ToolPosition.Width,
                    this.ToolPosition.Height
                );
            }

            // 🎯 初始化切换按钮位置（在右侧栏顶部）
            int buttonWidth = 80;
            int buttonHeight = 30;
            int buttonY = this.Position.Y + 45;
            int buttonSpacing = 5;
            
            this.ArchitectureListButtonPosition = new Rectangle(
                this.Position.X + 5,
                buttonY,
                buttonWidth,
                buttonHeight
            );
            
            this.TroopListButtonPosition = new Rectangle(
                this.Position.X + 5 + buttonWidth + buttonSpacing,
                buttonY,
                buttonWidth,
                buttonHeight
            );
            
            // 初始化文字按钮
            if (this.ArchitectureListButtonText == null)
            {
                this.ArchitectureListButtonText = new FreeText(
                    new Font("方正隶变_GBK", 14.0f, "Bold"),
                    Color.Yellow
                );
                this.ArchitectureListButtonText.Text = "建筑";
            }
            this.ArchitectureListButtonText.Position = this.ArchitectureListButtonPosition;
            this.ArchitectureListButtonText.Align = TextAlign.Middle;
            
            if (this.TroopListButtonText == null)
            {
                this.TroopListButtonText = new FreeText(
                    new Font("方正隶变_GBK", 14.0f, "Bold"),
                    Color.Gray
                );
                this.TroopListButtonText.Text = "部队";
            }
            this.TroopListButtonText.Position = this.TroopListButtonPosition;
            this.TroopListButtonText.Align = TextAlign.Middle;
            
            // 🔥 初始化时设置按钮颜色
            this.UpdateButtonColors();
            
            // 🎯 计算拖动区域（扩大到包含整个标题和按钮区域）
            // 标题区域从顶部边缘到按钮下方，约100px高度
            this.titleDragArea = new Rectangle(
                this.Position.X,
                this.Position.Y - this.topedgeWidth,
                this.Position.Width,
                100  // ✅ 扩大到100px，包含边缘(10) + 标题(40) + 按钮(30) + 余量(20)
            );

            this.TopLeftRectangle = new Rectangle(this.Position.X - this.leftedgeWidth, this.Position.Y - this.topedgeWidth, this.leftedgeWidth, this.topedgeWidth);
            this.TopRightRectangle = new Rectangle(this.Position.X + this.Position.Width, this.Position.Y - this.topedgeWidth, this.rightedgeWidth, this.topedgeWidth);
            this.BottomLeftRectangle = new Rectangle(this.Position.X - this.leftedgeWidth, this.Position.Y + this.Position.Height, this.leftedgeWidth, this.bottomedgeWidth);
            this.BottomRightRectangle = new Rectangle(this.Position.X + this.Position.Width, this.Position.Y + this.Position.Height, this.rightedgeWidth, this.bottomedgeWidth);


            //this.okbuttonRectangle = new Rectangle(this.Position.X + this.okbuttonPosition.X, this.Position.Y + this.okbuttonPosition.Y, this.okbuttonSize.X, this.okbuttonSize.Y);
            //this.cancelbuttonRectangle = new Rectangle(this.Position.X + this.cancelbuttonPosition.X, this.Position.Y + this.cancelbuttonPosition.Y, this.cancelbuttonSize.X, this.cancelbuttonSize.Y);
            //this.titleRectangle = new Rectangle(this.Position.X, this.Position.Y - this.titleHeight, this.titleWidth, this.titleHeight);
            //this.TitleText.Position = this.titleRectangle;
            //this.mapviewselectorButtonRectangle = new Rectangle(this.Position.X + this.mapviewselectorbuttonPosition.X, this.Position.Y + this.mapviewselectorbuttonPosition.Y, this.mapviewselectorbuttonSize.X, this.mapviewselectorbuttonSize.Y);
        }



        public void AddRows()
        {
            if (this.gameObjectList != null)
            {
                this.RowRectangles.Clear();
                for (int i = 0; i < this.gameObjectList.Count; i++)
                {
                    this.RowRectangles.Add(new Rectangle(this.VisibleLowerClient.X, (this.VisibleLowerClient.Y + this.columnheaderHeight) + (this.rowHeight * i), this.VisibleLowerClient.Width - 1, this.rowHeight));
                }
            }
        }

        public void ClearData()
        {
            this.Title = "";
            this.RowRectangles.Clear();
            this.Focused = 0;
            this.WidthCanShrink = true;
            this.HeightCanShrink = true;
            if (this.listKindToDisplay != null)
            {
                this.listKindToDisplay.ClearData();
            }
        }

        public override void Draw()
        {
            DrawDiagnostics.LogFrameDraw(this.jiancexianshi, this.xianshiyoucelan);
            
            if (this.jiancexianshi)
            {
                DrawDiagnostics.LogEnterJiancexianshi();
                CacheManager.Draw(this.ToolDisplayTexture, this.ToolDisplayPosition, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.099f);
                if (this.xianshiyoucelan)
                {
                    DrawDiagnostics.LogEnterXianshiyoucelan();

                    CacheManager.Draw(this.leftedgeTexture, this.leftedgeRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);

                    CacheManager.Draw(this.rightedgeTexture, this.rightedgeRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);

                    CacheManager.Draw(this.topedgeTexture, this.topedgeRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);

                    CacheManager.Draw(this.bottomedgeTexture, this.bottomedgeRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);

                    CacheManager.Draw(this.backgroundTexture, this.backgroundRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);

                    CacheManager.Draw(this.TopLeftTexture, this.TopLeftRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);
                    CacheManager.Draw(this.TopRightTexture, this.TopRightRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);
                    CacheManager.Draw(this.BottomLeftTexture, this.BottomLeftRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);
                    CacheManager.Draw(this.BottomRightTexture, this.BottomRightRectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);

                    // 🎯 绘制切换按钮
                    // ✅ Anti-Band-Aid: 直接访问，如果为null说明初始化有问题，应该暴露
                    this.ArchitectureListButtonText.Draw(0.39f);
                    this.TroopListButtonText.Draw(0.39f);

                    //base.Draw();
                    //if (this.frameContent != null)
                    //{
                    //    this.frameContent.Draw();
                    //}

                    if (this.listKindToDisplay != null)
                    {
                        this.listKindToDisplay.Draw();
                    }
                }
            }
        }


        /*private Rectangle ToolDisplayPosition
        {
            get
            {
                return new Rectangle(this.ToolPosition.X + 60, this.ToolPosition.Y + 50, this.ToolPosition.Width, this.ToolPosition.Height);
            }
        }*/


        public void EnlargeRectanglesHeight()
        {
            if (!this.HeightCanShrink)
            {
                this.VisibleLowerClient = new Rectangle(this.VisibleLowerClient.X, this.VisibleLowerClient.Y, this.VisibleLowerClient.Width, (this.VisibleLowerClient.Height + (2 * this.scrolltrackWidth)) + this.scrollbuttonWidth);
                this.HeightCanShrink = true;
            }
        }

        public void EnlargeRectanglesWidth()
        {
            if (!this.WidthCanShrink)
            {
                this.VisibleLowerClient = new Rectangle(this.VisibleLowerClient.X, this.VisibleLowerClient.Y, (this.VisibleLowerClient.Width + (2 * this.scrolltrackWidth)) + this.scrollbuttonWidth, this.VisibleLowerClient.Height);
                for (int i = 0; i < this.RowRectangles.Count; i++)
                {
                    this.RowRectangles[i] = new Rectangle(this.RowRectangles[i].X, this.RowRectangles[i].Y, (this.RowRectangles[i].Width + (2 * this.scrolltrackWidth)) + this.scrollbuttonWidth, this.RowRectangles[i].Height);
                }
                this.WidthCanShrink = true;
            }
        }

        public Tab FindTabByPosition(Point position)
        {
            foreach (Tab tab in this.listKindToDisplay.Tabs)
            {
                if (StaticMethods.PointInRectangle(position, tab.Position))
                {
                    return tab;
                }
            }
            return null;
        }

        private Column GetColumnByPosition(Point position)
        {
            if (this.listKindToDisplay.SelectedTab != null)
            {
                foreach (Column column in this.listKindToDisplay.SelectedTab.Columns)
                {
                    if (((column.DisplayPosition.Left >= this.VisibleLowerClient.Left) && (column.DisplayPosition.Right <= this.VisibleLowerClient.Right)) && StaticMethods.PointInRectangle(position, column.DisplayPosition))
                    {
                        return column;
                    }
                }
            }
            return null;
        }

        public override string GetCurrentTitle()
        {
            // 🔍 调试：输出标题获取（直接访问，如果null会抛异常，暴露问题）
            // System.Diagnostics.Debug.WriteLine($"[GetCurrentTitle] Title='{this.Title}', DisplayName='{this.listKindToDisplay.DisplayName}'");
            
            // ✅ Anti-Band-Aid: 直接返回Title
            return this.Title;
        }

        private GameObject GetGameObjectByPosition(Point position)
        {
            if (this.RowRectangles.Count != this.gameObjectList.Count) return null;
            for (int i = 0; i < this.RowRectangles.Count; i++)
            {
                Rectangle rectangle = this.RowRectangles[i];
                if (((rectangle.Bottom <= this.VisibleLowerClient.Bottom) && ((rectangle = this.RowRectangles[i]).Top >= (this.VisibleLowerClient.Top + this.columnheaderHeight))) && StaticMethods.PointInRectangle(position, this.RowRectangles[i]))
                {
                    return this.gameObjectList[i];
                }
            }
            return null;
        }

        public ListKind GetListKindByID(int ID)
        {
            foreach (ListKind kind in this.ListKinds)
            {
                if (kind.ID == ID)
                {
                    return kind;
                }
            }
            return null;
        }

        public ListKind GetListKindByName(string Name)
        {
            foreach (ListKind kind in this.ListKinds)
            {
                if (kind.Name == Name)
                {
                    return kind;
                }
            }
            return null;
        }

        public Rectangle GetRealLowerVisibleClient()
        {
            return new Rectangle(this.RealClient.X, this.listKindToDisplay.ColumnsTop, this.RealClient.Width, this.RealClient.Bottom - this.listKindToDisplay.ColumnsTop);
        }

        public int GetRowTopByPosition(Point position)
        {
            foreach (Rectangle rectangle in this.RowRectangles)
            {
                if (((rectangle.Bottom <= this.VisibleLowerClient.Bottom) && (rectangle.Top >= (this.VisibleLowerClient.Top + this.columnheaderHeight))) && StaticMethods.PointInRectangle(position, rectangle))
                {
                    return rectangle.Top;
                }
            }
            return -1;
        }

        public override string GetTitleString()
        {
            if (this.listKindToDisplay != null)
            {
                return this.listKindToDisplay.DisplayName;
            }
            return "未知";
        }

        internal void Initialize()
        {
            this.ListKinds = new List<ListKind>();
            this.RowRectangles = new List<Rectangle>();
        }

        public override void InitializeMapViewSelectorButton()
        {
            GameDelegates.VoidFunction function = null;
            if (this.MapViewSelectorButtonEnabled)
            {
                if (function == null)
                {
                    function = delegate {
                        this.iMapViewSelector.SetMultiSelecting(this.MultiSelecting);
                        this.iMapViewSelector.SetGameObjectList(this.gameObjectList);
                        if (this.firstTimeMapViewSelector)
                        {
                            this.firstTimeMapViewSelector = false;
                            this.iMapViewSelector.SetMapPosition(ShowPosition.Center);
                        }
                        this.iMapViewSelector.IsShowing = true;
                    };
                }
                this.MapViewSelectorFunction = function;
            }
        }

        public void InitialValues(GameObjectList gameObjectList, GameObjectList selectedObjectList, int scrollValue, string title)
        {
            this.SubKinds.Clear();
            this.SetObjectList(gameObjectList);
            this.oldScrollValue = scrollValue;
            this.Title = title;
            
            // 🔍 调试：输出标题设置
            // System.Diagnostics.Debug.WriteLine($"[InitialValues] 设置标题: '{title}'");
            
            // ✅ Anti-Band-Aid: 直接访问，如果为null说明SetListKindByName失败，应该暴露错误
            this.listKindToDisplay.DisplayName = title;
            // System.Diagnostics.Debug.WriteLine($"[InitialValues] 强制覆盖 DisplayName: '{title}'");
        }

        public void LoadFromXMLNode(XmlNode rootNode)  //读取xml里的列表文件样式，包括tablist 和 listkind
        {
            this.rowHeight = int.Parse(rootNode.Attributes.GetNamedItem("RowHeight").Value);
            this.defaultFrameWidth = int.Parse(rootNode.Attributes.GetNamedItem("FrameWidth").Value);
            this.defaultFrameHeight = int.Parse(rootNode.Attributes.GetNamedItem("FrameHeight").Value);
            this.client.X = int.Parse(rootNode.Attributes.GetNamedItem("ClientX").Value);
            this.client.Y = int.Parse(rootNode.Attributes.GetNamedItem("ClientY").Value);
            this.client.Width = int.Parse(rootNode.Attributes.GetNamedItem("ClientWidth").Value);
            this.client.Height = int.Parse(rootNode.Attributes.GetNamedItem("ClientHeight").Value);
            this.defaultOKButtonPosition.X = int.Parse(rootNode.Attributes.GetNamedItem("OKButtonX").Value);
            this.defaultOKButtonPosition.Y = int.Parse(rootNode.Attributes.GetNamedItem("OKButtonY").Value);
            this.defaultCancelButtonPosition.X = int.Parse(rootNode.Attributes.GetNamedItem("CancelButtonX").Value);
            this.defaultCancelButtonPosition.Y = int.Parse(rootNode.Attributes.GetNamedItem("CancelButtonY").Value);
            this.defaultMapViewSelectorButtonPosition.X = int.Parse(rootNode.Attributes.GetNamedItem("MapViewSelectorButtonX").Value);
            this.defaultMapViewSelectorButtonPosition.Y = int.Parse(rootNode.Attributes.GetNamedItem("MapViewSelectorButtonY").Value);
            foreach (XmlNode node in rootNode.ChildNodes)
            {
                ListKind item = new ListKind(this) {
                    ID = int.Parse(node.Attributes.GetNamedItem("ID").Value),
                    Name = node.Attributes.GetNamedItem("Name").Value,
                    DisplayName = node.Attributes.GetNamedItem("DisplayName").Value,
                    ShowPortrait = bool.Parse(node.Attributes.GetNamedItem("ShowPortrait").Value)
                };
                item.LoadFromXMLNode(node);
                this.ListKinds.Add(item);
            }
        }

        internal void PopSubKind()
        {
            if (this.SubKinds.Count > 0)
            {
                SubKind rootListKind;
                this.SubKinds.Pop();
                if (this.SubKinds.Count > 0)
                {
                    rootListKind = this.SubKinds.Peek();
                }
                else
                {
                    rootListKind = this.RootListKind;
                }
                this.SetObjectList(rootListKind.List);
                this.listKindToDisplay = rootListKind.Kind;
                this.ReCalculate();
            }
            else
            {
                this.RightClickClose = true;
            }
        }

        internal void PushSubKindByName(string name, GameObjectList list)
        {
            if ((list != null) && (list.Count != 0))
            {
                if (this.SubKinds.Count == 0)
                {
                    this.RightClickClose = false;
                    this.RootListKind.Kind = this.listKindToDisplay;
                    this.RootListKind.List = this.gameObjectList;
                }
                this.SetObjectList(list);
                this.listKindToDisplay = this.GetListKindByName(name);
                if (this.listKindToDisplay != null)
                {
                    this.ReCalculate();
                    SubKind item = new SubKind {
                        Kind = this.listKindToDisplay,
                        List = list
                    };
                    this.SubKinds.Push(item);
                }
            }
        }

        public override void ReCalculate()
        {
            //base.ReCalculate();
            if (this.listKindToDisplay != null)
            {
                this.listKindToDisplay.ReCalculate();
            }
            this.SelectDefaultTab();
        }

        internal void RefreshEditable()
        {
            this.ResetEditableTextures();
            this.OKButtonEnabled = this.gameObjectList.HasSelectedItem();
            if (this.MultiSelecting)
            {
                this.SelectedItemList = this.gameObjectList.GetSelectedList();
            }
            else if (this.OKButtonEnabled)
            {
                this.SelectedItem = this.gameObjectList.GetSelectedList()[0];
            }
        }

        internal void ResetEditableTextures()
        {
            if (this.listKindToDisplay != null)
            {
                this.listKindToDisplay.ResetEditableTextures();
            }
        }

        private void screen_OnMouseLeftDown(Point position)
        {
            // 🔍 调试：输出鼠标点击位置和所有可点击区域
            // System.Diagnostics.Debug.WriteLine($"[右侧栏] ========== OnMouseLeftDown 开始 ==========");
            // System.Diagnostics.Debug.WriteLine($"[右侧栏] 鼠标点击位置: ({position.X}, {position.Y})");
            // System.Diagnostics.Debug.WriteLine($"[右侧栏] xianshiyoucelan（进入时）: {this.xianshiyoucelan}");
            // System.Diagnostics.Debug.WriteLine($"[右侧栏] Position: ({this.Position.X}, {this.Position.Y}, {this.Position.Width}, {this.Position.Height})");
            // System.Diagnostics.Debug.WriteLine($"[右侧栏] ToolDisplayPosition: ({this.ToolDisplayPosition.X}, {this.ToolDisplayPosition.Y}, {this.ToolDisplayPosition.Width}, {this.ToolDisplayPosition.Height})");
            
            // 🔍 明确判断点击位置
            bool inToolDisplay = StaticMethods.PointInRectangle(position, this.ToolDisplayPosition);
            bool inArchButton = StaticMethods.PointInRectangle(position, this.ArchitectureListButtonPosition);
            bool inTroopButton = StaticMethods.PointInRectangle(position, this.TroopListButtonPosition);
            
            // System.Diagnostics.Debug.WriteLine($"[右侧栏] 点击判断: ToolDisplay={inToolDisplay}, ArchButton={inArchButton}, TroopButton={inTroopButton}");
            
            // 🎯 绿色标题区域：记录点击（展开时准备拖动，收起时准备打开）
            if (inToolDisplay)
            {
                // System.Diagnostics.Debug.WriteLine($"[右侧栏] 进入 ToolDisplay 分支，xianshiyoucelan={this.xianshiyoucelan}");
                
                // 记录按下时的展开状态和时间
                this.wasExpandedOnMouseDown = this.xianshiyoucelan;
                this.mouseDownTime = DateTime.Now;
                
                if (this.xianshiyoucelan)
                {
                    // 展开状态：准备拖动或收起
                    this.isDragging = true;
                    this.hasDragged = false;
                    this.dragStartMousePosition = position;
                    this.dragStartFramePosition = new(this.Position.X, this.Position.Y);
                    // System.Diagnostics.Debug.WriteLine($"[右侧栏] ✅ 点击绿色标题区域（展开状态），准备拖动或收起");
                }
                else
                {
                    // 收起状态：立即展开
                    this.xianshiyoucelan = true;
                    this.isDragging = true;
                    this.hasDragged = false;
                    this.ResetRectangles();  // 🔥 更新 ToolDisplayPosition
                    // System.Diagnostics.Debug.WriteLine($"[右侧栏] ✅ 点击绿色标题区域（收起状态），展开");
                }
                return;
            }
            
            // 🎯 处理切换按钮点击
            if (inArchButton)
            {
                // System.Diagnostics.Debug.WriteLine("[右侧栏] ✅ 点击建筑列表按钮");
                if (this.isShowingTroopList)
                {
                    this.isShowingTroopList = false;
                    this.UpdateButtonColors();
                    Session.MainGame.mainGameScreen.ShowArchitectureListInYoucelan();
                }
                return;
            }
            
            if (inTroopButton)
            {
                // System.Diagnostics.Debug.WriteLine("[右侧栏] ✅ 点击部队列表按钮");
                if (!this.isShowingTroopList)
                {
                    this.isShowingTroopList = true;
                    this.UpdateButtonColors();
                    Session.MainGame.mainGameScreen.ShowTroopListInYoucelan();
                }
                return;
            }

            if (this.xianshiyoucelan && (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.None) && StaticMethods.PointInRectangle(position, this.RealClient))
            {
                if (position.Y < this.listKindToDisplay.ColumnsTop)
                {
                    Tab tab = this.FindTabByPosition(position);
                    if (tab != null)
                    {
                        tab.Selected = true;
                    }
                }
                else if (position.Y < (this.listKindToDisplay.ColumnsTop + this.columnheaderHeight))
                {
                    Column columnByPosition = this.GetColumnByPosition(position);
                    if (columnByPosition != null)
                    {
                        PropertyComparer comparer = new PropertyComparer(columnByPosition.Name, columnByPosition.IsNumber, columnByPosition.SmallToBig);
                        this.gameObjectList.Sort(comparer);
                        this.listKindToDisplay.ResetAllTextures();
                        columnByPosition.SmallToBig = !columnByPosition.SmallToBig;
                    }
                }
                else
                {
                    GameObject gameObjectByPosition;
                    if (this.ShowCheckBox)
                    {
                        gameObjectByPosition = this.GetGameObjectByPosition(position);
                        if (gameObjectByPosition != null)
                        {
                            if (this.listKindToDisplay.IsInEditableColumn(position))
                            {
                                if ((gameObjectByPosition.Selected || (this.SelectedItemMaxCount <= 0)) || (this.gameObjectList.GetSelectedList().Count < this.SelectedItemMaxCount))
                                {
                                    gameObjectByPosition.Selected = !gameObjectByPosition.Selected;
                                    if (this.MultiSelecting)
                                    {
                                        this.SelectingRows = true;
                                        this.SelectingBool = gameObjectByPosition.Selected;
                                        this.SelectedItemList = this.gameObjectList.GetSelectedList();
                                    }
                                    else
                                    {
                                        this.gameObjectList.SetOtherUnSelected(gameObjectByPosition);
                                    }
                                    this.OKButtonEnabled = this.gameObjectList.HasSelectedItem() || (gameObjectByPosition is Faction);
                                    this.ResetEditableTextures();
                                    if (gameObjectByPosition.Selected)
                                    {
                                        this.SelectedItem = gameObjectByPosition;
                                        if (!(this.MultiSelecting || !Setting.Current.GlobalVariables.SingleSelectionOneClick))
                                        {
                                            this.iGameFrame.OK();
                                        }
                                        else
                                        {
                                            Session.MainGame.mainGameScreen.PlayNormalSound(this.SelectSoundFile);
                                        }
                                    }
                                    else
                                    {
                                        this.SelectedItem = null;
                                    }
                                }
                            }
                            else
                            {
                                if (gameObjectByPosition is Troop)
                                {
                                    if ((this.iTroopDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iTroopDetail.SetPosition(ShowPosition.Center);
                                        this.iTroopDetail.SetTroop(gameObjectByPosition);
                                        this.iTroopDetail.IsShowing = true;
                                    }
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Troop ? (Troop)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Person)
                                {
                                    if ((this.iPersonDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iPersonDetail.SetPosition(ShowPosition.Center);
                                        this.iPersonDetail.SetPerson(gameObjectByPosition);
                                        this.iPersonDetail.IsShowing = true;
                                    }
                                    if (!((gameObjectByPosition is Person ? (Person)gameObjectByPosition : null)).IsCaptive)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Person ? (Person)gameObjectByPosition : null)).Position);
                                    }
                                }
                                else if (gameObjectByPosition is Architecture)
                                {
                                    if ((this.iArchitectureDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iArchitectureDetail.SetPosition(ShowPosition.Center);
                                        this.iArchitectureDetail.SetArchitecture(gameObjectByPosition);
                                        this.iArchitectureDetail.IsShowing = true;
                                    }
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Architecture ? (Architecture)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Military)
                                {
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Military ? (Military)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Faction)
                                {
                                    if ((this.iFactionTechniques != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iFactionTechniques.SetArchitecture(null);
                                        this.iFactionTechniques.SetFaction(gameObjectByPosition, false);
                                        this.iFactionTechniques.SetPosition(ShowPosition.Center);
                                        this.iFactionTechniques.IsShowing = true;
                                    }
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Faction ? (Faction)gameObjectByPosition : null)).Leader.Position);
                                }
                                else if (gameObjectByPosition is Captive)
                                {
                                    if ((this.iPersonDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iPersonDetail.SetPosition(ShowPosition.Center);
                                        this.iPersonDetail.SetPerson((gameObjectByPosition as Captive).CaptivePerson);
                                        this.iPersonDetail.IsShowing = true;
                                    }
                                }
                                else if (gameObjectByPosition is Treasure)
                                {
                                    if ((this.iTreasureDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iTreasureDetail.SetPosition(ShowPosition.Center);
                                        this.iTreasureDetail.SetTreasure(gameObjectByPosition);
                                        this.iTreasureDetail.IsShowing = true;
                                    }
                                    if (((gameObjectByPosition is Treasure ? (Treasure)gameObjectByPosition : null)).BelongedPerson != null)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Treasure ? (Treasure)gameObjectByPosition : null)).BelongedPerson.Position);
                                    }
                                }
                                if (gameObjectByPosition != null)
                                {
                                    this.TriggerItemClick();
                                }
                            }
                        }
                    }
                    else
                    {
                        gameObjectByPosition = this.GetGameObjectByPosition(position);
                        if (gameObjectByPosition != null)
                        {
                            if (this.listKindToDisplay.SelectedTab.ListMethod != null)
                            {
                                this.PushSubKindByName(this.listKindToDisplay.SelectedTab.ListKind, StaticMethods.GetListMethodValue(gameObjectByPosition, this.listKindToDisplay.SelectedTab.ListMethod) as GameObjectList);
                            }
                            else
                            {
                                if (gameObjectByPosition is Troop)
                                {
                                    if ((this.iTroopDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iTroopDetail.SetPosition(ShowPosition.Center);
                                        this.iTroopDetail.SetTroop(gameObjectByPosition);
                                        this.iTroopDetail.IsShowing = true;
                                    }
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Troop ? (Troop)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Person)
                                {
                                    if ((this.iPersonDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iPersonDetail.SetPosition(ShowPosition.Center);
                                        this.iPersonDetail.SetPerson(gameObjectByPosition);
                                        this.iPersonDetail.IsShowing = true;
                                    }
                                    if (!((gameObjectByPosition is Person ? (Person)gameObjectByPosition : null)).IsCaptive)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Person ? (Person)gameObjectByPosition : null)).Position);
                                    }
                                }
                                else if (gameObjectByPosition is Architecture)
                                {
                                    if ((this.iArchitectureDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iArchitectureDetail.SetPosition(ShowPosition.Center);
                                        this.iArchitectureDetail.SetArchitecture(gameObjectByPosition);
                                        this.iArchitectureDetail.IsShowing = true;
                                    }
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Architecture ? (Architecture)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Military)
                                {
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Military ? (Military)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Faction)
                                {
                                    if ((this.iFactionTechniques != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iFactionTechniques.SetArchitecture(null);
                                        this.iFactionTechniques.SetFaction(gameObjectByPosition, false);
                                        this.iFactionTechniques.SetPosition(ShowPosition.Center);
                                        this.iFactionTechniques.IsShowing = true;
                                    }
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Faction ? (Faction)gameObjectByPosition : null)).Leader.Position);
                                }
                                else if (gameObjectByPosition is Captive)
                                {
                                    if ((this.iPersonDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iPersonDetail.SetPosition(ShowPosition.Center);
                                        this.iPersonDetail.SetPerson((gameObjectByPosition as Captive).CaptivePerson);
                                        this.iPersonDetail.IsShowing = true;
                                    }
                                }
                                else if (gameObjectByPosition is Treasure)
                                {
                                    if ((this.iTreasureDetail != null) && (this.Function != FrameFunction.Jump))
                                    {
                                        this.iTreasureDetail.SetPosition(ShowPosition.Center);
                                        this.iTreasureDetail.SetTreasure(gameObjectByPosition);
                                        this.iTreasureDetail.IsShowing = true;
                                    }
                                    if (((gameObjectByPosition is Treasure ? (Treasure)gameObjectByPosition : null)).BelongedPerson != null)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Treasure ? (Treasure)gameObjectByPosition : null)).BelongedPerson.Position);
                                    }
                                }
                                if (gameObjectByPosition != null)
                                {
                                    this.TriggerItemClick();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void screen_OnMouseLeftUp(Point position)
        {
            var elapsedMs = (DateTime.Now - this.mouseDownTime).TotalMilliseconds;
            
            if (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.None)
            {
                if (this.isDragging)
                {
                    // ✅ 终极防误触判定逻辑：
                    // 1. 过滤幽灵按键：< 15ms 绝非人类操作，直接忽略。
                    // 2. 正常单击：没有发生位移 (!hasDragged) 且 处于正常时间范围 (< 200ms)。
                    // 3. 原地长按：超过 200ms 未移动而松开，视为"放弃拖动"，不触发单击收起。
                    bool isPhantomClick = elapsedMs < 15;
                    bool isNormalClick = !this.hasDragged && elapsedMs < ClickTimeThresholdMs;
                    
                    if (!isPhantomClick && isNormalClick)
                    {
                        // 单击：切换展开/收起
                        if (this.wasExpandedOnMouseDown)
                        {
                            this.xianshiyoucelan = false;
                            this.ResetRectangles();
                            // System.Diagnostics.Debug.WriteLine($"[右侧栏] 单击收起");
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine($"[右侧栏] 单击展开（保持）");
                        }
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"[右侧栏] 非单击操作（拖动或长按或幽灵点击）");
                    }
                    
                    this.isDragging = false;
                    this.hasDragged = false;
                }
                
                this.MovingHorizontalScrollBar = false;
                this.MovingVerticalScrollBar = false;
                this.SelectingRows = false;
            }
        }

        private void screen_OnMouseMove(Point position, bool leftDown)
        {
            // 🎯 收起状态：检测鼠标是否靠近屏幕右侧边缘，自动展开
            if (!this.xianshiyoucelan)
            {
                var viewportSize = Session.MainGame.mainGameScreen.viewportSize;
                const int EdgeThreshold = 50; // 靠近边缘的阈值（像素）
                
                // 鼠标在屏幕右侧边缘附近
                if (position.X >= viewportSize.X - EdgeThreshold)
                {
                    this.xianshiyoucelan = true;
                    this.ResetRectangles();
                }
                else
                {
                    return;
                }
            }
            
            // 🎯 检测是否开始拖动（只看物理距离）
            if (leftDown && this.isDragging && !this.hasDragged)
            {
                int deltaX = Math.Abs(position.X - this.dragStartMousePosition.X);
                int deltaY = Math.Abs(position.Y - this.dragStartMousePosition.Y);
                
                // ✅ 移除 elapsedMs 判定，只依赖实际移动距离
                if (deltaX > DragThreshold || deltaY > DragThreshold)
                {
                    this.hasDragged = true;
                }
            }
            
            // 🎯 执行拖动
            // ✅ 修复：只在 leftDown=true 时执行拖动，但检测不需要 leftDown
            if (leftDown && this.isDragging && this.hasDragged)
            {
                int deltaX = position.X - this.dragStartMousePosition.X;
                int deltaY = position.Y - this.dragStartMousePosition.Y;
                
                int newX = this.dragStartFramePosition.X + deltaX;
                int newY = this.dragStartFramePosition.Y + deltaY;
                
                // 限制拖动范围（不超出屏幕）
                var viewportSize = Session.MainGame.mainGameScreen.viewportSize;
                newX = Math.Max(0, Math.Min(newX, viewportSize.X - this.Position.Width));
                newY = Math.Max(0, Math.Min(newY, viewportSize.Y - this.Position.Height));
                
                this.SetPosition(new(newX, newY, this.Position.Width, this.Position.Height));
                this.oldMousePosition = position;
                return;
            }
            
            if ((Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.None) && (this.oldMousePosition != position))
            {
                if (leftDown)
                {
                    
                    if (this.ShowCheckBox && ((this.MultiSelecting && !this.MovingHorizontalScrollBar) && !this.MovingVerticalScrollBar))
                    {
                        GameObject gameObjectByPosition = this.GetGameObjectByPosition(position);
                        if (gameObjectByPosition != null)
                        {
                            if (!this.SelectingRows)
                            {
                                this.SelectingRows = true;
                                this.SelectingBool = gameObjectByPosition.Selected;
                            }
                            if (this.SelectingRows)
                            {
                                if (this.SelectingBool)
                                {
                                    if ((this.SelectedItemMaxCount <= 0) || (this.gameObjectList.GetSelectedList().Count < this.SelectedItemMaxCount))
                                    {
                                        gameObjectByPosition.Selected = this.SelectingBool;
                                        this.ResetEditableTextures();
                                    }
                                }
                                else
                                {
                                    gameObjectByPosition.Selected = this.SelectingBool;
                                    this.ResetEditableTextures();
                                }
                            }
                            this.OKButtonEnabled = this.gameObjectList.HasSelectedItem();
                            this.SelectedItemList = this.gameObjectList.GetSelectedList();
                        }
                    }
                    if (this.ShowHorizontalScrollBar && (this.MovingHorizontalScrollBar || StaticMethods.PointInRectangle(position, this.listKindToDisplay.HorizontalScrollBar)))
                    {
                        this.listKindToDisplay.MoveHorizontal(position.X - this.oldMousePosition.X);
                        this.MovingHorizontalScrollBar = true;
                    }
                    if (this.ShowVerticalScrollBar && (this.MovingVerticalScrollBar || StaticMethods.PointInRectangle(position, this.listKindToDisplay.VerticalScrollBar)))
                    {
                        this.listKindToDisplay.MoveVertical(position.Y - this.oldMousePosition.Y);
                        this.MovingVerticalScrollBar = true;
                    }
                }
                else
                {
                    int rowTopByPosition = this.GetRowTopByPosition(position);
                    if (rowTopByPosition >= 0)
                    {
                        this.Focused = rowTopByPosition;
                        this.FocusedObject = this.GetGameObjectByPosition(position);
                        this.DrawFocused = true;
                    }
                    else
                    {
                        this.DrawFocused = false;
                    }
                }
                this.oldMousePosition = position;
            }
        }

        private void screen_OnMouseRightUp(Point position)
        {
            if (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.None)
            {
                this.PopSubKind();
            }
        }

        private void screen_OnMouseScroll(Point position, int scrollValue)
        {
            if (((Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.None) && (!this.MovingHorizontalScrollBar && !this.MovingVerticalScrollBar)) && (this.listKindToDisplay != null))
            {
                if (this.ShowVerticalScrollBar)
                {
                    this.listKindToDisplay.MoveVertical((this.oldScrollValue - scrollValue) / 6);
                }
                else if (this.ShowHorizontalScrollBar)
                {
                    this.listKindToDisplay.MoveHorizontal((scrollValue - this.oldScrollValue) / 6);
                }
                this.oldScrollValue = scrollValue;
            }
        }

        private void SelectDefaultTab()
        {
            if (((this.listKindToDisplay != null) && (this.listKindToDisplay.Tabs.Count > 0)) && !this.listKindToDisplay.HasSelectedTab)
            {
                // 🔥 2026-02-18 修复信息重复显示问题（根本修复 v6）
                // 根本原因：直接设置 Tabs[0].Selected = true，但没有取消其他 Tab 的选中状态
                // 导致多个 Tab 同时被选中，ListKind.Draw() 会对每个选中的 Tab 绘制内容
                // 解决方案：先取消所有 Tab 的选中状态，再选中第一个
                // 性能：使用 for 循环代替 foreach（Cold Path，但遵循规范）
                for (int i = 0; i < this.listKindToDisplay.Tabs.Count; i++)
                {
                    this.listKindToDisplay.Tabs[i].Selected = false;
                }
                this.listKindToDisplay.Tabs[0].Selected = true;
            }
        }

        public void SetListKindByID(int ID, bool showCheckBox, bool multiSelecting)
        {
            this.listKindToDisplay = this.GetListKindByID(ID);
            this.MultiSelecting = multiSelecting;
            if (this.listKindToDisplay != null)
            {
                this.SetShowCheckBox(showCheckBox);
            }
        }

        public void SetListKindByName(string Name, bool showCheckBox, bool multiSelecting)
        {
            // System.Diagnostics.Debug.WriteLine($"[SetListKindByName] 开始查找: {Name}");
            // System.Diagnostics.Debug.WriteLine($"[SetListKindByName] ListKinds数量: {this.ListKinds?.Count ?? 0}");

            if (this.ListKinds != null)
            {
                foreach (var kind in this.ListKinds)
                {
                    // System.Diagnostics.Debug.WriteLine($"[SetListKindByName] 可用配置: ID={kind.ID}, Name={kind.Name}");
                }
            }
            
            this.listKindToDisplay = this.GetListKindByName(Name);
            
            if (this.listKindToDisplay != null)
            {
                // System.Diagnostics.Debug.WriteLine($"[SetListKindByName] ✅ 找到配置: {this.listKindToDisplay.Name}");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine($"[SetListKindByName] ❌ 未找到配置: {Name}");
            }
            
            this.MultiSelecting = multiSelecting;
            if (this.listKindToDisplay != null)
            {
                this.SetShowCheckBox(showCheckBox);
            }
        }

        public void SetObjectList(GameObjectList gameObjectList)
        {


            
            this.ClearData();
            this.gameObjectList = gameObjectList;
            
            if (gameObjectList != null)
            {

                
                foreach (GameObject obj2 in gameObjectList)
                {
                    //obj2.Selected = false;  不能影响建筑连接的改动，建筑列表插件和建筑是否被选中无关
                }
                
                this.FullLowerClient.Height = (gameObjectList.Count * this.rowHeight) + this.columnheaderHeight;


            }
            

        }

        private void SetSelectedObjectList(GameObjectList selectedObjectList)
        {
            if (selectedObjectList != null)
            {
                foreach (GameObject obj2 in selectedObjectList)
                {
                    obj2.Selected = true;
                }
            }
        }

        public void SetSelectedTab(string tabName)
        {
            if ((this.listKindToDisplay != null) && (this.listKindToDisplay.Tabs.Count > 0))
            {
                this.listKindToDisplay.SetSelectedTab(tabName);
            }
        }

        private void SetShowCheckBox(bool show)
        {
            this.ShowCheckBox = show;
            if (this.listKindToDisplay != null)
            {
                if (show)
                {
                    this.listKindToDisplay.AddCheckBoxColumn();
                }
                else
                {
                    this.listKindToDisplay.RemoveCheckBoxColumn();
                }
            }
        }

        public void ShrinkRectanglesHeight()
        {
            if (this.HeightCanShrink)
            {
                this.VisibleLowerClient = new Rectangle(this.VisibleLowerClient.X, this.VisibleLowerClient.Y, this.VisibleLowerClient.Width, (this.VisibleLowerClient.Height - (2 * this.scrolltrackWidth)) - this.scrollbuttonWidth);
                this.HeightCanShrink = false;
            }
        }

        public void ShrinkRectanglesWidth()
        {
            if (this.WidthCanShrink)
            {
                this.VisibleLowerClient = new Rectangle(this.VisibleLowerClient.X, this.VisibleLowerClient.Y, (this.VisibleLowerClient.Width - (2 * this.scrolltrackWidth)) - this.scrollbuttonWidth, this.VisibleLowerClient.Height);
                for (int i = 0; i < this.RowRectangles.Count; i++)
                {
                    this.RowRectangles[i] = new Rectangle(this.RowRectangles[i].X, this.RowRectangles[i].Y, (this.RowRectangles[i].Width - (2 * this.scrolltrackWidth)) - this.scrollbuttonWidth, this.RowRectangles[i].Height);
                }
                this.WidthCanShrink = false;
            }
        }

        public override bool CanClose
        {
            get
            {
                return this.RightClickClose;
            }
        }

        public override bool IsShowing
        {
            get
            {/*
                bool isShowing = base.isShowing;
                if (isShowing && (this.iPersonDetail != null))
                {
                    isShowing = !this.iPersonDetail.IsShowing;
                }
                if (isShowing && (this.iTroopDetail != null))
                {
                    isShowing = !this.iTroopDetail.IsShowing;
                }
                if (isShowing && (this.iArchitectureDetail != null))
                {
                    isShowing = !this.iArchitectureDetail.IsShowing;
                }
                if (isShowing && (this.iFactionTechniques != null))
                {
                    isShowing = !this.iFactionTechniques.IsShowing;
                }
                return isShowing;
              */

                return this.jiancexianshi ;
            }
            set
            {
                // 🔥 修复：移除自动调用 SetMouseEvent
                // 日期：2026-02-16
                // 问题：setter 中调用 SetMouseEvent(Session.MainGame.mainGameScreen, value)
                //       但 Session.MainGame.mainGameScreen 可能为 null
                // 解决：IsShowing 只负责设置显示状态，事件订阅由显式调用 SetMouseEvent 完成
                this.jiancexianshi = value;
            }
        }        public void SetMouseEvent(Screen screen, bool value)
        {
            if (this.jiancexianshi != value)
            {
                this.jiancexianshi = value;
                if (value)
                {
                    // 展开时的逻辑
                }
                else
                {
                    this.SelectedItemMaxCount = 0;
                    this.OKFunction = null;
                }
            }

            screen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
            
            // ✅ 修复：正确绑定 OnMouseLeftDown 事件
            screen.OnMouseLeftDown += new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
            
            screen.OnMouseLeftUp += new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
            
            screen.OnMouseRightUp += new Screen.MouseRightUp(this.screen_OnMouseRightUp);
        }

        public override bool MapViewSelectorButtonEnabled
        {
            get
            {
                return ((((this.iMapViewSelector != null) && (this.gameObjectList.Count > 0)) && (this.gameObjectList[0] is Architecture)) && this.ShowCheckBox);
            }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct SubKind
        {
            internal ListKind Kind;
            internal GameObjectList List;
        }
    }
}


