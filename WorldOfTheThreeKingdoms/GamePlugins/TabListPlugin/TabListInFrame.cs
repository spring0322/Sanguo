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

namespace TabListPlugin
{

    internal class TabListInFrame : FrameContent
    {
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
        

        public Font ColumnTextBuilder = new Font();

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
        PlatformTexture SellectAllTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\TabList\Data\CheckBox.png" );
        string selectallstring = " 全 选 ";
        int selectallX, selectallY;
        public Font TabTextBuilder = new Font();

        internal Color TabTextColor;
        internal string Title = "";
        internal Rectangle VisibleLowerClient;
        private bool WidthCanShrink = true;

        // 🎯 褒赏提示相关字段
        private string tooltipText = "";
        private Point tooltipPosition;
        private bool showTooltip = false;
        private int tooltipDelayFrames = 0;
        private const int TOOLTIP_DELAY = 30; // 30帧延迟（约0.5秒）

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
            if (MultiSelecting)
            {
                selectallX = this.listKindToDisplay.AllColumns[0].ColumnTextList[0].Position.X;
                
                // 调整全选按钮位置，避免与地图选择按钮重叠
                // 如果启用了地图选择按钮，将全选按钮放在其后面（右侧）
                if (this.MapViewSelectorButtonEnabled)
                {
                    // 将全选按钮放在地图选择按钮的右侧，避免重叠
                    selectallX = base.RealClient.X + this.MapViewSelectorButtonPosition.X + 230; // 地图按钮宽度 + 间距（增大避免重叠）
                    selectallY = base.RealClient.Y + this.MapViewSelectorButtonPosition.Y;
                }
                else
                {
                    // 如果没有地图选择按钮，使用原来的位置
                    selectallY = base.RealClient.Bottom + (int)(1.2f * rowHeight);
                }
            }
            base.Draw();
            if (this.listKindToDisplay != null)
            {
                this.listKindToDisplay.Draw();
                if (MultiSelecting)
                {
                    CacheManager.Draw(SellectAllTexture, new Rectangle(selectallX - 2 * checkboxWidth, selectallY, (int)(checkboxWidth * 1.3), (int)(checkboxWidth * 1.3)), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.035f);

                    CacheManager.DrawString(Session.Current.Font, selectallstring, new Vector2(selectallX, selectallY), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
            }
            
            // 🎯 绘制褒赏提示
            if (showTooltip && !string.IsNullOrEmpty(tooltipText))
            {
                DrawTooltip();
            }
        }

        private static PlatformTexture dummyTexture;
        private static PlatformTexture GetDummyTexture()
        {
            if (dummyTexture == null)
            {
                var tex2d = new Texture2D(Session.MainGame.GraphicsDevice, 1, 1);
                tex2d.SetData(new[] { Color.White });
                dummyTexture = new PlatformTexture(tex2d);
            }
            return dummyTexture;
        }

        // 🎯 绘制褒赏提示框
        private void DrawTooltip()
        {
            Vector2 textSize = Session.Current.Font.MeasureString(tooltipText);
            int padding = 10;
            int tooltipWidth = (int)textSize.X + padding * 2;
            int tooltipHeight = (int)textSize.Y + padding * 2;
            
            // 调整tooltip位置，避免超出屏幕
            int tooltipX = tooltipPosition.X + 15;
            int tooltipY = tooltipPosition.Y + 15;
            
            if (tooltipX + tooltipWidth > Session.MainGame.GraphicsDevice.Viewport.Width)
            {
                tooltipX = tooltipPosition.X - tooltipWidth - 5;
            }
            if (tooltipY + tooltipHeight > Session.MainGame.GraphicsDevice.Viewport.Height)
            {
                tooltipY = tooltipPosition.Y - tooltipHeight - 5;
            }
            
            PlatformTexture tex = GetDummyTexture();
            Rectangle tooltipRect = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
            
            // 绘制背景（半透明黑色）- 直接传递参数避免分配
            CacheManager.Draw(tex, tooltipRect, null, new Color(0, 0, 0, 200), 0f, Vector2.Zero, SpriteEffects.None, 0.001f);
            
            // 绘制边框（金色）- 简单的4条边
            int borderSize = 2;
            Color borderColor = Color.Gold;
            CacheManager.Draw(tex, new Rectangle(tooltipRect.Left, tooltipRect.Top, tooltipRect.Width, borderSize), null, borderColor, 0f, Vector2.Zero, SpriteEffects.None, 0.0009f); // 顶
            CacheManager.Draw(tex, new Rectangle(tooltipRect.Left, tooltipRect.Bottom - borderSize, tooltipRect.Width, borderSize), null, borderColor, 0f, Vector2.Zero, SpriteEffects.None, 0.0009f); // 底
            CacheManager.Draw(tex, new Rectangle(tooltipRect.Left, tooltipRect.Top, borderSize, tooltipRect.Height), null, borderColor, 0f, Vector2.Zero, SpriteEffects.None, 0.0009f); // 左
            CacheManager.Draw(tex, new Rectangle(tooltipRect.Right - borderSize, tooltipRect.Top, borderSize, tooltipRect.Height), null, borderColor, 0f, Vector2.Zero, SpriteEffects.None, 0.0009f); // 右
            
            // 绘制文本 - 直接传递参数避免分配
            CacheManager.DrawString(
                Session.Current.Font, 
                tooltipText, 
                new Vector2(tooltipX + padding, tooltipY + padding), 
                Color.White, 
                0f, 
                Vector2.Zero, 
                1f, 
                SpriteEffects.None, 
                0.0008f);
        }

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
            if ((this.Title == "") && (this.listKindToDisplay != null))
            {
                return this.listKindToDisplay.DisplayName;
            }
            return this.Title;
        }

        private GameObject GetGameObjectByPosition(Point position)
        {
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
            return new Rectangle(base.RealClient.X, this.listKindToDisplay.ColumnsTop, base.RealClient.Width, base.RealClient.Bottom - this.listKindToDisplay.ColumnsTop);
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
                base.MapViewSelectorFunction = function;
            }
        }

        public void InitialValues(GameObjectList gameObjectList, GameObjectList selectedObjectList, int scrollValue, string title)
        {
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.InitialValues] 开始初始化");
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.InitialValues] gameObjectList.Count = {gameObjectList.Count}");
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.InitialValues] title = {title}");
            
            this.SubKinds.Clear();
            this.SetObjectList(gameObjectList);
            this.SetSelectedObjectList(selectedObjectList);
            this.oldScrollValue = scrollValue;
            this.Title = title;
            
            // 初始化时更新全选按钮状态
            if (this.MultiSelecting)
            {
                this.UpdateSelectAllButtonState();
            }
            
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.InitialValues] 初始化完成");
        }

        public void LoadFromXMLNode(XmlNode rootNode)
        {
            this.rowHeight = int.Parse(rootNode.Attributes.GetNamedItem("RowHeight").Value);
            base.defaultFrameWidth = int.Parse(rootNode.Attributes.GetNamedItem("FrameWidth").Value);
            base.defaultFrameHeight = int.Parse(rootNode.Attributes.GetNamedItem("FrameHeight").Value);
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
                ListKind item = new ListKind(this);
                item.ID = int.Parse(node.Attributes.GetNamedItem("ID").Value);
                item.Name = node.Attributes.GetNamedItem("Name").Value;
                item.DisplayName = node.Attributes.GetNamedItem("DisplayName").Value;
                item.ShowPortrait = bool.Parse(node.Attributes.GetNamedItem("ShowPortrait").Value);
                item.LoadFromXMLNode(node);
                this.ListKinds.Add(item);

            }

        }

        internal void EnsureInfluenceListKind()
        {
            // Manually inject Influence ListKind if not present (ID 18)
            if (this.GetListKindByID(18) == null)
            {
                ListKind influenceKind = new ListKind(this);
                influenceKind.ID = 18;
                influenceKind.Name = "Influence";
                influenceKind.DisplayName = "影响";
                influenceKind.ShowPortrait = false;

                // ID Column
                Column colID = new Column(this);
                colID.ID = 1;
                colID.Name = "ID";
                colID.DisplayName = "ID";
                colID.IsNumber = true;
                colID.MinWidth = 50;
                colID.Text.Text = "ID";
                colID.Editable = false;
                influenceKind.AllColumns.Add(colID);

                // Name Column
                Column colName = new Column(this);
                colName.ID = 2;
                colName.Name = "Description"; // Try Description as Property if Name is generic
                colName.DisplayName = "描述";
                colName.IsNumber = false;
                colName.MinWidth = 400;
                colName.Text.Text = "描述";
                colName.Editable = false;
                influenceKind.AllColumns.Add(colName);
                
                // Tabs
                Tab tab = new Tab(this, influenceKind);
                tab.ID = 0;
                tab.Name = "All";
                tab.DisplayName = "全部";
                tab.Columns.Add(colID);
                tab.Columns.Add(colName);
                tab.Selected = true;
                influenceKind.Tabs.Add(tab);

                this.ListKinds.Add(influenceKind);
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
                    SubKind item = new SubKind();
                    item.Kind = this.listKindToDisplay;
                    item.List = list;
                    this.SubKinds.Push(item);
                }
            }
        }

        public override void ReCalculate()
        {
            base.ReCalculate();
            if (this.listKindToDisplay != null)
            {
                this.listKindToDisplay.ReCalculate();
            }
            this.SelectDefaultTab();
        }

        internal void RefreshEditable()
        {
            this.ResetEditableTextures();
            base.OKButtonEnabled = this.gameObjectList.HasSelectedItem();
            if (this.MultiSelecting)
            {
                this.SelectedItemList = this.gameObjectList.GetSelectedList();
                // 更新全选按钮状态
                this.UpdateSelectAllButtonState();
            }
            else if (base.OKButtonEnabled)
            {
                var selectedList = this.gameObjectList.GetSelectedList();
                if (selectedList.Count > 0)
                {
                    this.SelectedItem = selectedList[0];
                    
                    // 添加类型安全检查和调试信息
                    if (this.SelectedItem != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TabListPlugin] 选中对象类型: {this.SelectedItem.GetType().Name}");
                        
                        // 根据当前显示的列表类型验证选中对象的类型
                        if (this.listKindToDisplay != null)
                        {
                            string expectedType = GetExpectedTypeFromListKind();
                            if (!string.IsNullOrEmpty(expectedType))
                            {
                                string actualType = this.SelectedItem.GetType().Name;
                                if (actualType != expectedType)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[TabListPlugin] 警告：类型不匹配！期望: {expectedType}, 实际: {actualType}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查是否所有项目都被选中
        /// </summary>
        private bool IsAllItemsSelected()
        {
            if (this.gameObjectList == null || this.gameObjectList.Count == 0)
                return false;
            
            foreach (GameObject obj in this.gameObjectList)
            {
                if (!obj.Selected)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 更新全选按钮的状态（文字和图标）
        /// </summary>
        private void UpdateSelectAllButtonState()
        {
            if (this.IsAllItemsSelected())
            {
                // 所有项目都被选中，显示"取消全选"
                SellectAllTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\TabList\Data\CheckBoxSelected.png");
                selectallstring = "取消全选";
            }
            else
            {
                // 不是所有项目都被选中，显示"全选"
                SellectAllTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\TabList\Data\CheckBox.png");
                selectallstring = " 全 选 ";
            }
        }
        
        /// <summary>
        /// 根据当前列表类型获取期望的对象类型名称
        /// </summary>
        private string GetExpectedTypeFromListKind()
        {
            if (this.listKindToDisplay?.Name != null)
            {
                switch (this.listKindToDisplay.Name.ToLower())
                {
                    case "person":
                        return "Person";
                    case "military":
                        return "Military";
                    case "architecture":
                        return "Architecture";
                    case "troop":
                        return "Troop";
                    case "faction":
                        return "Faction";
                    default:
                        return null;
                }
            }
            return null;
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
            //有時滑動滾動條時點到按鈕範圍，縮小點，以後看有沒更完美的修正辦法
            var recSmall = new Rectangle(base.RealClient.X, base.RealClient.Y, base.RealClient.Width - 50, base.RealClient.Height);
            if ((Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame) && StaticMethods.PointInRectangle(position, recSmall))
            {
                if (position.Y < this.listKindToDisplay.ColumnsTop)
                {
                }
                else if (position.Y < (this.listKindToDisplay.ColumnsTop + this.columnheaderHeight))
                {
                }
                else
                {
                    GameObject gameObjectByPosition;
                    if (this.ShowCheckBox)
                    {
                        gameObjectByPosition = this.GetGameObjectByPosition(position);
                        if (gameObjectByPosition != null)
                        {
                            // 🎯 褒赏限制：仅在褒赏功能中，已褒赏的武将不可再次勾选
                            if (base.Function == FrameFunction.GetRewardPerson && 
                                gameObjectByPosition is Person person && 
                                person.RewardFinished)
                            {
                                // 已褒赏的武将，不允许勾选，直接返回
                                return;
                            }
                            
                            if (this.listKindToDisplay.IsInEditableColumn(position))
                            {
                                if ((gameObjectByPosition.Selected || (this.SelectedItemMaxCount <= 0)) || (this.gameObjectList.GetSelectedList().Count < this.SelectedItemMaxCount))
                                {
                                    if (this.MultiSelecting)
                                    {
                                        if (!this.SelectingRows)
                                        {
                                            gameObjectByPosition.Selected = !gameObjectByPosition.Selected;

                                            this.SelectingRows = true;
                                            this.SelectingBool = gameObjectByPosition.Selected;
                                        }

                                        this.SelectedItemList = this.gameObjectList.GetSelectedList();
                                        if (Session.MainGame.mainGameScreen.KeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                                        {
                                            foreach (GameObject g in this.gameObjectList)
                                            {
                                                g.Selected = !g.Selected;
                                            }
                                        }
                                    }
                                    else
                                    {
                                    }
                                    base.OKButtonEnabled = this.gameObjectList.HasSelectedItem() || (gameObjectByPosition is Faction);
                                    this.ResetEditableTextures();
                                    if (gameObjectByPosition.Selected)
                                    {
                                        this.SelectedItem = gameObjectByPosition;
                                        if (!(this.MultiSelecting || !Setting.Current.GlobalVariables.SingleSelectionOneClick))
                                        {
                                           // this.iGameFrame.OK();
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

                            }
                        }
                    }
                    else
                    {
                    }
                }
            }

            else if (MultiSelecting && (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame))
            {
                // 计算全选按钮的点击区域，与Draw方法中的位置保持一致
                int clickAreaX, clickAreaY;
                if (this.MapViewSelectorButtonEnabled)
                {
                    clickAreaX = base.RealClient.X + this.MapViewSelectorButtonPosition.X + 230 - 2 * checkboxWidth;
                    clickAreaY = base.RealClient.Y + this.MapViewSelectorButtonPosition.Y;
                }
                else
                {
                    clickAreaX = this.listKindToDisplay.AllColumns[0].ColumnTextList[0].Position.X - 2 * checkboxWidth;
                    clickAreaY = base.RealClient.Bottom + (int)(1.2f * rowHeight);
                }
                
                if (StaticMethods.PointInRectangle(position, new Rectangle(clickAreaX, clickAreaY, (int)(checkboxWidth * 1.3), (int)(checkboxWidth * 1.3))))
                {
                    if (selectallstring.Equals(" 全 选 "))
                    {
                        // 🎯 全选时，仅在褒赏功能中跳过已褒赏的武将
                        for (int i = 0; i < this.gameObjectList.Count; i++)
                        {
                            GameObject g = this.gameObjectList[i];
                            // 仅在褒赏功能中跳过已褒赏的武将
                            if (base.Function == FrameFunction.GetRewardPerson && 
                                g is Person person && 
                                person.RewardFinished)
                            {
                                continue;
                            }
                            g.Selected = true;
                        }
                    }
                    else
                    {
                        // 取消全选所有项目
                        for (int i = 0; i < this.gameObjectList.Count; i++)
                        {
                            this.gameObjectList[i].Selected = false;
                        }
                    }
                    // 更新按钮状态和界面
                    this.RefreshEditable();
                }
            }

        }

        private void screen_OnMouseLeftUp(Point position)
        {
            //有時滑動滾動條時點到按鈕範圍，縮小點，以後看有沒更完美的修正辦法
            var recSmall = new Rectangle(base.RealClient.X, base.RealClient.Y, base.RealClient.Width - 50, base.RealClient.Height);
            if ((Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame) && StaticMethods.PointInRectangle(position, recSmall))
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
                        PropertyComparer comparer = new PropertyComparer(columnByPosition.Name, columnByPosition.IsNumber, columnByPosition.SmallToBig, columnByPosition.ItemID);
                        this.gameObjectList.StableSort(comparer);
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
                                    if (this.MultiSelecting)
                                    {
                                        /*
                                        this.SelectingRows = true;
                                        this.SelectingBool = gameObjectByPosition.Selected;
                                        this.SelectedItemList = this.gameObjectList.GetSelectedList();
                                         */

                                    }
                                    else
                                    {
                                        gameObjectByPosition.Selected = !gameObjectByPosition.Selected;

                                        this.gameObjectList.SetOtherUnSelected(gameObjectByPosition);
                                    }
                                    base.OKButtonEnabled = this.gameObjectList.HasSelectedItem() || (gameObjectByPosition is Faction);
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
                                            //Session.MainGame.mainGameScreen.PlayNormalSound(this.SelectSoundFile);
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
                                    if ((this.iTroopDetail != null) && (base.Function != FrameFunction.Jump))
                                    {
                                        this.iTroopDetail.SetPosition(ShowPosition.Center);
                                        this.iTroopDetail.SetTroop(gameObjectByPosition);
                                        this.iTroopDetail.IsShowing = true;
                                    }
                                    Point pos = ((gameObjectByPosition is Troop ? (Troop)gameObjectByPosition : null)).Position;
                                    if (pos != Point.Zero)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(pos);
                                    }
                                }
                                else if (gameObjectByPosition is Person)
                                {
                                    if ((this.iPersonDetail != null) && (base.Function != FrameFunction.Jump))
                                    {
                                        this.iPersonDetail.SetPosition(ShowPosition.Center);
                                        this.iPersonDetail.SetPerson(gameObjectByPosition);
                                        this.iPersonDetail.IsShowing = true;
                                    }
                                    if (!((gameObjectByPosition is Person ? (Person)gameObjectByPosition : null)).IsCaptive)
                                    {
                                        Point pos = ((gameObjectByPosition is Person ? (Person)gameObjectByPosition : null)).Position;
                                        if (pos != Point.Zero)
                                        {
                                            Session.MainGame.mainGameScreen.JumpTo(pos);
                                        }
                                    }
                                }
                                else if (gameObjectByPosition is Architecture)
                                {
                                    if ((this.iArchitectureDetail != null) && (base.Function != FrameFunction.Jump))
                                    {
                                        this.iArchitectureDetail.SetPosition(ShowPosition.Center);
                                        this.iArchitectureDetail.SetArchitecture(gameObjectByPosition);
                                        this.iArchitectureDetail.IsShowing = true;
                                    }
                                    Point pos = ((gameObjectByPosition is Architecture ? (Architecture)gameObjectByPosition : null)).Position;
                                    if (pos != Point.Zero)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(pos);
                                    }
                                }
                                else if (gameObjectByPosition is Military)
                                {
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Military ? (Military)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Faction)
                                {
                                    if ((this.iFactionTechniques != null) && (base.Function != FrameFunction.Jump))
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
                                    if ((this.iPersonDetail != null) && (base.Function != FrameFunction.Jump))
                                    {
                                        this.iPersonDetail.SetPosition(ShowPosition.Center);
                                        this.iPersonDetail.SetPerson((gameObjectByPosition as Captive).CaptivePerson);
                                        this.iPersonDetail.IsShowing = true;
                                    }
                                }
                                else if (gameObjectByPosition is Treasure)
                                {
                                    if ((this.iTreasureDetail != null) && (base.Function != FrameFunction.Jump))
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
                                else if (gameObjectByPosition is Information)
                                {
                                    if (base.Function != FrameFunction.Jump)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Information ? (Information)gameObjectByPosition : null)).Position);
                                    }
                                }
                                else if (this.listKindToDisplay.SelectedTab.ListMethod != null)
                                {
                                    this.PushSubKindByName(this.listKindToDisplay.SelectedTab.ListKind, StaticMethods.GetListMethodValue(gameObjectByPosition, this.listKindToDisplay.SelectedTab.ListMethod) as GameObjectList);
                                }
                                if (gameObjectByPosition != null)
                                {
                                    base.TriggerItemClick();
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
                                    if ((this.iTroopDetail != null) && (base.Function != FrameFunction.Jump))
                                    {
                                        this.iTroopDetail.SetPosition(ShowPosition.Center);
                                        this.iTroopDetail.SetTroop(gameObjectByPosition);
                                        this.iTroopDetail.IsShowing = true;
                                    }
                                    Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Troop ? (Troop)gameObjectByPosition : null)).Position);
                                }
                                else if (gameObjectByPosition is Person)
                                {
                                    try
                                    {
                                        if ((this.iPersonDetail != null) && (base.Function != FrameFunction.Jump))
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
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[TabListInFrame] Person Click Error: {ex.ToString()}");
                                    }
                                }
                                else if (gameObjectByPosition is Architecture)
                                {
                                    if ((this.iArchitectureDetail != null) && (base.Function != FrameFunction.Jump))
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
                                    if ((this.iFactionTechniques != null) && (base.Function != FrameFunction.Jump))
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
                                    if ((this.iPersonDetail != null) && (base.Function != FrameFunction.Jump))
                                    {
                                        this.iPersonDetail.SetPosition(ShowPosition.Center);
                                        this.iPersonDetail.SetPerson((gameObjectByPosition as Captive).CaptivePerson);
                                        this.iPersonDetail.IsShowing = true;
                                    }
                                }
                                else if (gameObjectByPosition is Treasure)
                                {
                                    if ((this.iTreasureDetail != null) && (base.Function != FrameFunction.Jump))
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
                                else if (gameObjectByPosition is Information)
                                {
                                    if (base.Function != FrameFunction.Jump)
                                    {
                                        Session.MainGame.mainGameScreen.JumpTo(((gameObjectByPosition is Information ? (Information)gameObjectByPosition : null)).Position);
                                    }
                                }
                                if (gameObjectByPosition != null)
                                {
                                    base.TriggerItemClick();
                                }
                            }
                        }
                    }
                }
            }

            /////////////////////////////////////////////////

            if (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame)
            {
                this.MovingHorizontalScrollBar = false;
                this.MovingVerticalScrollBar = false;
                this.SelectingRows = false;
            }
        }

        private void screen_OnMouseMove(Point position, bool leftDown)
        {
            if ((Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame) && (this.oldMousePosition != position))
            {
                if (leftDown)
                {
                    // 🎯 拖拽时隐藏tooltip
                    showTooltip = false;
                    tooltipDelayFrames = 0;
                    
                    if (this.ShowCheckBox && ((this.MultiSelecting && !this.MovingHorizontalScrollBar) && !this.MovingVerticalScrollBar))
                    {
                        GameObject gameObjectByPosition = this.GetGameObjectByPosition(position);
                        if (gameObjectByPosition != null)
                        {
                            // 🎯 褒赏限制：拖拽时跳过已褒赏的武将
                            if (gameObjectByPosition is Person person && person.RewardFinished)
                            {
                                // 已褒赏的武将，跳过不处理
                            }
                            else
                            {
                                if (!this.SelectingRows)
                                {
                                    this.SelectingRows = true;
                                    this.SelectingBool = !gameObjectByPosition.Selected;
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
                                base.OKButtonEnabled = this.gameObjectList.HasSelectedItem();
                                this.SelectedItemList = this.gameObjectList.GetSelectedList();
                            }
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
                        
                        // 🎯 检查是否悬停在已褒赏的武将上
                        if (this.FocusedObject is Person person && person.RewardFinished)
                        {
                            // 重置tooltip延迟计数器
                            if (tooltipPosition != position)
                            {
                                tooltipDelayFrames = 0;
                                tooltipPosition = position;
                                tooltipText = "本月已褒赏";
                            }
                            
                            // 延迟显示tooltip
                            tooltipDelayFrames++;
                            if (tooltipDelayFrames >= TOOLTIP_DELAY)
                            {
                                showTooltip = true;
                            }
                        }
                        // 🎯 检查是否悬停在蜜月期武将上
                        else if (this.FocusedObject is Person honeymoonPerson && honeymoonPerson.HoneymoonMonths > 0)
                        {
                            // 重置tooltip延迟计数器
                            if (tooltipPosition != position)
                            {
                                tooltipDelayFrames = 0;
                                tooltipPosition = position;
                                tooltipText = $"蜜月期剩余 {honeymoonPerson.HoneymoonMonths} 个月";
                            }
                            
                            // 延迟显示tooltip
                            tooltipDelayFrames++;
                            if (tooltipDelayFrames >= TOOLTIP_DELAY)
                            {
                                showTooltip = true;
                            }
                        }
                        else
                        {
                            // 不是已褒赏或蜜月期的武将，隐藏tooltip
                            showTooltip = false;
                            tooltipDelayFrames = 0;
                        }
                    }
                    else
                    {
                        this.DrawFocused = false;
                        showTooltip = false;
                        tooltipDelayFrames = 0;
                    }
                }
                this.oldMousePosition = position;
            }
        }

        private void screen_OnMouseRightUp(Point position)
        {
            if (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame)
            {
                this.PopSubKind();
            }
        }

        private void screen_OnMouseScroll(Point position, int scrollValue)
        {
            if (((Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame) && (!this.MovingHorizontalScrollBar && !this.MovingVerticalScrollBar)) && (this.listKindToDisplay != null))
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
            this.listKindToDisplay = this.GetListKindByName(Name);
            this.MultiSelecting = multiSelecting;
            if (this.listKindToDisplay != null)
            {
                this.SetShowCheckBox(showCheckBox);
            }
        }

        public void SetObjectList(GameObjectList gameObjectList)
        {
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.SetObjectList] 开始设置对象列表");
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.SetObjectList] gameObjectList.Count = {gameObjectList.Count}");
            
            this.ClearData();
            this.gameObjectList = gameObjectList;
            
            // 🔥 不做null检查，如果gameObjectList为null，让它崩溃暴露调用方问题
            foreach (GameObject obj2 in gameObjectList)
            {
                obj2.Selected = false;
            }
            this.FullLowerClient.Height = (gameObjectList.Count * this.rowHeight) + this.columnheaderHeight;
            
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.SetObjectList] FullLowerClient.Height = {this.FullLowerClient.Height}");
            System.Diagnostics.Debug.WriteLine($"[TabListInFrame.SetObjectList] 设置完成");
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
            {
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
            }
            set
            {
                if (value != base.isShowing)
                {
                    base.isShowing = value;
                    if (value)
                    {
                        Session.MainGame.mainGameScreen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
                        //Session.MainGame.mainGameScreen.OnMouseLeftDown += new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
                        Session.MainGame.mainGameScreen.OnMouseLeftUp += new Screen.MouseLeftUp(this.screen_OnMouseLeftDown);
                        Session.MainGame.mainGameScreen.OnMouseLeftUp += new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                        Session.MainGame.mainGameScreen.OnMouseRightUp += new Screen.MouseRightUp(this.screen_OnMouseRightUp);
                        Session.MainGame.mainGameScreen.OnMouseScroll += new Screen.MouseScroll(this.screen_OnMouseScroll);
                    }
                    else
                    {
                        Session.MainGame.mainGameScreen.OnMouseMove -= new Screen.MouseMove(this.screen_OnMouseMove);
                        //Session.MainGame.mainGameScreen.OnMouseLeftDown -= new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
                        Session.MainGame.mainGameScreen.OnMouseLeftUp -= new Screen.MouseLeftUp(this.screen_OnMouseLeftDown);
                        Session.MainGame.mainGameScreen.OnMouseLeftUp -= new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                        Session.MainGame.mainGameScreen.OnMouseRightUp -= new Screen.MouseRightUp(this.screen_OnMouseRightUp);
                        Session.MainGame.mainGameScreen.OnMouseScroll -= new Screen.MouseScroll(this.screen_OnMouseScroll);
                        this.SelectedItemMaxCount = 0;
                    }
                }
            }
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


