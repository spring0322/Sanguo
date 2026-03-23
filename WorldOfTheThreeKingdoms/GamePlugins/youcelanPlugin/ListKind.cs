using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace youcelanPlugin
{
    public class ListKind
    {
        public List<Column> AllColumns;
        private int columnsTop;
        internal string DisplayName;
        internal Microsoft.Xna.Framework.Rectangle HorizontalScrollBar;
        internal int HorizontalScrollTrackLength;
        internal int ID;
        internal Microsoft.Xna.Framework.Rectangle LeftScrollTrack;
        internal Microsoft.Xna.Framework.Rectangle LowerScrollTrack;
        internal string Name;
        internal Microsoft.Xna.Framework.Rectangle RightScrollTrack;
        internal Tab SelectedTab;
        internal bool ShowPortrait;
        private TabListInFrame tabList;
        private int tabMargin;
        public List<Tab> Tabs;
        internal Microsoft.Xna.Framework.Rectangle UpperScrollTrack;
        internal Microsoft.Xna.Framework.Rectangle VerticalScrollBar;
        internal int VerticalScrollTrackLength;

        internal ListKind(TabListInFrame tabList)
        {
            this.tabList = tabList;
            this.AllColumns = new List<Column>();
            this.Tabs = new List<Tab>();
        }

        internal void AddCheckBoxColumn()
        {
            Column item = new Column(this.tabList) {
                ID = 0,
                Name = this.tabList.checkboxName,
                IsNumber = false,
                SmallToBig = false,
                DisplayName = this.tabList.checkboxDisplayName,
                Width = this.tabList.checkboxWidth
            };
            this.AllColumns.Add(item);
        }

        internal void AddColumn(string name, string displayName, int width, bool isNumber, bool smallToBig)
        {
            Column item = new Column(this.tabList) {
                ID = this.AllColumns.Count,
                Name = name,
                IsNumber = isNumber,
                SmallToBig = smallToBig,
                DisplayName = displayName,
                Width = width
            };
            this.AllColumns.Add(item);
        }

        internal void AddTab(string name, string displayName, string listMethod)
        {
            Tab item = new Tab(this.tabList, this) {
                ID = this.Tabs.Count,
                Name = name,
                DisplayName = displayName,
                ListMethod = listMethod
            };
            this.Tabs.Add(item);
        }

        internal void Draw()
        {
            // 渲染所有 Tab (包含列表内容)
            for (int i = 0; i < this.Tabs.Count; i++)
            {
                this.Tabs[i].Draw();
            }
            
            // 渲染焦点框
            if ((((this.tabList.DrawFocused && !this.tabList.MovingHorizontalScrollBar) && !this.tabList.MovingVerticalScrollBar) && (this.SelectedTab != null)) && ((this.tabList.Focused >= (this.tabList.VisibleLowerClient.Top + this.tabList.columnheaderHeight)) && ((this.tabList.Focused + this.tabList.rowHeight) <= this.tabList.VisibleLowerClient.Bottom)))
            {
                CacheManager.Draw(this.tabList.focusTrackTexture, new Rectangle(this.tabList.VisibleLowerClient.Left, this.tabList.Focused, this.tabList.VisibleLowerClient.Width - 1, 1), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                CacheManager.Draw(this.tabList.focusTrackTexture, new Rectangle(this.tabList.VisibleLowerClient.Left, this.tabList.Focused, 1, this.tabList.rowHeight), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                CacheManager.Draw(this.tabList.focusTrackTexture, new Rectangle(this.tabList.VisibleLowerClient.Left, (this.tabList.Focused + this.tabList.rowHeight) - 1, this.tabList.VisibleLowerClient.Width - 1, 1), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                CacheManager.Draw(this.tabList.focusTrackTexture, new Rectangle(this.tabList.VisibleLowerClient.Right - 1, this.tabList.Focused, 1, this.tabList.rowHeight), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
            }
            
            // 渲染垂直滚动条
            if (this.tabList.ShowVerticalScrollBar)
            {
                CacheManager.Draw(this.tabList.scrolltrackTexture, this.LeftScrollTrack, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                CacheManager.Draw(this.tabList.scrolltrackTexture, this.RightScrollTrack, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                CacheManager.Draw(this.tabList.scrollbuttonTexture, this.VerticalScrollBar, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.899f);
            }
            if (this.tabList.ShowHorizontalScrollBar)
            {
                CacheManager.Draw(this.tabList.scrolltrackTexture, this.UpperScrollTrack, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                CacheManager.Draw(this.tabList.scrolltrackTexture, this.LowerScrollTrack, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                CacheManager.Draw(this.tabList.scrollbuttonTexture, this.HorizontalScrollBar, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.899f);
            }
        }

        internal void LoadFromXMLNode(XmlNode rootNode)
        {
            // 🔥 修复：完整实现 XML 加载逻辑
            // 日期：2026-02-17
            // 问题：原实现为空，导致 Columns 和 Tabs 没有加载
            // 参考：BianduiLiebiaoChajian/ListKind.cs 的正确实现
            
            // 1. 加载 Columns
            XmlNode columnsNode = rootNode.ChildNodes.Item(0);
            foreach (XmlNode columnNode in columnsNode.ChildNodes)
            {
                Font font;
                Color color;
                
                Column column = new(this.tabList)
                {
                    ID = int.Parse(columnNode.Attributes.GetNamedItem("ID").Value),
                    Name = columnNode.Attributes.GetNamedItem("Name").Value,
                    IsNumber = bool.Parse(columnNode.Attributes.GetNamedItem("IsNumber").Value),
                    DisplayName = columnNode.Attributes.GetNamedItem("DisplayName").Value,
                    MinWidth = int.Parse(columnNode.Attributes.GetNamedItem("MinWidth").Value),
                    SmallToBig = true
                };
                
                // 🔥 2026-03-01 添加：加载 ItemID（用于带参数的方法调用）
                if (columnNode.Attributes.GetNamedItem("ItemID") != null)
                {
                    column.ItemID = int.Parse(columnNode.Attributes.GetNamedItem("ItemID").Value);
                }
                
                StaticMethods.LoadFontAndColorFromXMLNode(columnNode, out font, out color);
                column.ColumnTextList = new(font);
                column.ColumnTextList.TextColor = color;
                column.ColumnTextList.Align = Enum.Parse<TextAlign>(columnNode.Attributes.GetNamedItem("Align").Value);
                column.Text.Text = column.DisplayName;
                
                this.AllColumns.Add(column);
            }
            
            // 2. 加载 Tabs
            XmlNode tabsNode = rootNode.ChildNodes.Item(1);
            this.tabMargin = int.Parse(tabsNode.Attributes.GetNamedItem("Margin").Value);
            
            foreach (XmlNode tabNode in tabsNode.ChildNodes)
            {
                Tab tab = new(this.tabList, this)
                {
                    ID = int.Parse(tabNode.Attributes.GetNamedItem("ID").Value),
                    Name = tabNode.Attributes.GetNamedItem("Name").Value,
                    DisplayName = tabNode.Attributes.GetNamedItem("DisplayName").Value
                };
                
                if (tabNode.Attributes.GetNamedItem("ListKind") != null)
                {
                    tab.ListKind = tabNode.Attributes.GetNamedItem("ListKind").Value;
                }
                
                if (tabNode.Attributes.GetNamedItem("ListMethod") != null)
                {
                    tab.ListMethod = tabNode.Attributes.GetNamedItem("ListMethod").Value;
                }
                
                tab.LoadColumnsFromString(tabNode.Attributes.GetNamedItem("Columns").Value);
                
                if (tabNode.Attributes.GetNamedItem("SortColumnID") != null)
                {
                    tab.SortColumnID = int.Parse(tabNode.Attributes.GetNamedItem("SortColumnID").Value);
                }
                
                if (tabNode.Attributes.GetNamedItem("SmallToBig") != null)
                {
                    tab.SmallToBig = bool.Parse(tabNode.Attributes.GetNamedItem("SmallToBig").Value);
                }
                
                tab.Text.Text = tab.DisplayName;
                this.Tabs.Add(tab);
            }
        }

        internal void Update()
        {
            // 更新逻辑 - 可以根据需要实现
        }

        public void ClearData()
        {
            foreach (Column column in this.AllColumns)
            {
                column.ClearData();
            }
        }

        public int ColumnsTop
        {
            get { return this.columnsTop; }
            set
            {
                this.columnsTop = value;
                this.tabList.VisibleLowerClient = this.tabList.RealClient;
                this.tabList.VisibleLowerClient.Y = value;
                this.tabList.VisibleLowerClient.Height -= value - this.tabList.RealClient.Y;
                this.tabList.AddRows();
            }
        }

        public void ReCalculate()
        {
            Rectangle position = new(this.tabList.RealClient.X + this.tabMargin, this.tabList.RealClient.Y + this.tabMargin, this.tabList.tabbuttonWidth, this.tabList.tabbuttonHeight);
            
            Tab selectedTab = null;
            for (int i = 0; i < this.Tabs.Count; i++)
            {
                if (position.Right > (this.tabList.RealClient.Right - this.tabMargin))
                {
                    position.X = this.tabList.RealClient.X + this.tabMargin;
                    position.Y += position.Height + this.tabMargin;
                }
                this.Tabs[i].SetPosition(position);
                position.X += position.Width + this.tabMargin;
                
                if (this.Tabs[i].Selected)
                {
                    selectedTab = this.Tabs[i];
                }
            }
            
            this.ColumnsTop = (position.Bottom + this.tabMargin) + 1;
            this.SelectedTab = selectedTab;
            
            if (selectedTab != null)
            {
                selectedTab.ReCalculate(selectedTab.CurrentYOffset);
            }
        }

        public void ResetEditableTextures()
        {
            if (this.SelectedTab != null)
            {
                this.SelectedTab.ResetEditableTextures();
            }
        }

        public void ResetAllTextures()
        {
            if (this.SelectedTab != null)
            {
                this.SelectedTab.ResetAllTextures();
            }
        }

        public bool IsInEditableColumn(Point position)
        {
            // 检查是否在可编辑列中的基本实现
            return false;
        }

        public void MoveHorizontal(int offset)
        {
            this.HorizontalScrollBar.X += (int)((double)offset / (this.tabList.RowRectangles[0].Width) * (this.tabList.VisibleLowerClient.Width - this.VerticalScrollBar.Width));
            if (this.HorizontalScrollBar.Left < this.tabList.VisibleLowerClient.Left)
            {
                this.HorizontalScrollBar.X = this.tabList.VisibleLowerClient.Left;
            }
            if (this.HorizontalScrollBar.Right > (this.tabList.VisibleLowerClient.Left + this.HorizontalScrollTrackLength))
            {
                this.HorizontalScrollBar.X = (this.tabList.VisibleLowerClient.Left + this.HorizontalScrollTrackLength) - this.HorizontalScrollBar.Width;
            }
            if (this.SelectedTab != null)
            {
                offset = (offset * this.tabList.VisibleLowerClient.Width) / this.HorizontalScrollBar.Width;
                this.SelectedTab.MoveHorizontal(-offset);
            }
        }

        public void MoveVertical(int offset)
        {
            this.VerticalScrollBar.Y += offset;
            if (this.VerticalScrollBar.Top < (this.tabList.VisibleLowerClient.Top + this.tabList.columnheaderHeight))
            {
                this.VerticalScrollBar.Y = this.tabList.VisibleLowerClient.Top + this.tabList.columnheaderHeight;
            }
            if (this.VerticalScrollBar.Bottom > ((this.tabList.VisibleLowerClient.Top + this.tabList.columnheaderHeight) + this.VerticalScrollTrackLength))
            {
                this.VerticalScrollBar.Y = ((this.tabList.VisibleLowerClient.Top + this.tabList.columnheaderHeight) + this.VerticalScrollTrackLength) - this.VerticalScrollBar.Height;
            }
            if (this.SelectedTab != null)
            {
                offset = (offset * (this.tabList.VisibleLowerClient.Height - this.tabList.columnheaderHeight)) / this.VerticalScrollBar.Height;
                this.SelectedTab.MoveVertical(-offset);
            }
        }

        public bool HasSelectedTab
        {
            get
            {
                return this.SelectedTab != null;
            }
        }

        public void SetSelectedTab(string tabName)
        {
            // 设置选中标签的基本实现
            foreach (var tab in this.Tabs)
            {
                if (tab.Name == tabName)
                {
                    tab.Selected = true;
                    this.SelectedTab = tab;
                }
                else
                {
                    tab.Selected = false;
                }
            }
        }

        public void RemoveCheckBoxColumn()
        {
            // 移除复选框列的基本实现
            if (this.AllColumns.Count > 0 && this.AllColumns[0].ID == 0)
            {
                this.AllColumns.RemoveAt(0);
            }
        }

        public Column GetColumnByID(int id)
        {
            // 根据ID获取列的基本实现
            foreach (var column in this.AllColumns)
            {
                if (column.ID == id)
                {
                    return column;
                }
            }
            return null;
        }

        public void ResetScrollTracks()
        {
            // 🔥 修复：实现滚动条计算逻辑
            // 日期：2026-02-17
            // 问题：ResetScrollTracks 方法为空，导致滚动条不显示，无法查看所有城池
            // 解决：从 TabListPlugin 移植完整的滚动条计算逻辑
            
            Rectangle realLowerVisibleClient = this.tabList.GetRealLowerVisibleClient();
            
            // 水平滚动条计算
            if (this.tabList.FullLowerClient.Width > realLowerVisibleClient.Width)
            {
                this.tabList.ShowHorizontalScrollBar = true;
                if (this.tabList.FullLowerClient.Height > realLowerVisibleClient.Height)
                {
                    this.HorizontalScrollTrackLength = (realLowerVisibleClient.Width - (2 * this.tabList.scrolltrackWidth)) - this.tabList.scrollbuttonWidth;
                }
                else
                {
                    this.HorizontalScrollTrackLength = realLowerVisibleClient.Width;
                }
                this.UpperScrollTrack = new Rectangle(realLowerVisibleClient.Left, (realLowerVisibleClient.Bottom - (2 * this.tabList.scrolltrackWidth)) - this.tabList.scrollbuttonWidth, this.HorizontalScrollTrackLength, this.tabList.scrolltrackWidth);
                this.LowerScrollTrack = new Rectangle(realLowerVisibleClient.Left, realLowerVisibleClient.Bottom - this.tabList.scrolltrackWidth, this.HorizontalScrollTrackLength, this.tabList.scrolltrackWidth);
                this.HorizontalScrollBar = new Rectangle(realLowerVisibleClient.Left, (realLowerVisibleClient.Bottom - this.tabList.scrolltrackWidth) - this.tabList.scrollbuttonWidth, (this.HorizontalScrollTrackLength * realLowerVisibleClient.Width) / this.tabList.FullLowerClient.Width, this.tabList.scrollbuttonWidth);
                this.tabList.ShrinkRectanglesHeight();
            }
            else
            {
                this.tabList.ShowHorizontalScrollBar = false;
                this.tabList.EnlargeRectanglesHeight();
            }
            
            // 垂直滚动条计算
            if (this.tabList.FullLowerClient.Height > realLowerVisibleClient.Height)
            {
                this.tabList.ShowVerticalScrollBar = true;
                if (this.tabList.FullLowerClient.Width > realLowerVisibleClient.Width)
                {
                    this.VerticalScrollTrackLength = ((realLowerVisibleClient.Height - (2 * this.tabList.scrolltrackWidth)) - this.tabList.scrollbuttonWidth) - this.tabList.columnheaderHeight;
                }
                else
                {
                    this.VerticalScrollTrackLength = realLowerVisibleClient.Height - this.tabList.columnheaderHeight;
                }
                this.LeftScrollTrack = new Rectangle((realLowerVisibleClient.Right - (2 * this.tabList.scrolltrackWidth)) - this.tabList.scrollbuttonWidth, realLowerVisibleClient.Top + this.tabList.columnheaderHeight, this.tabList.scrolltrackWidth, this.VerticalScrollTrackLength);
                this.RightScrollTrack = new Rectangle(realLowerVisibleClient.Right - this.tabList.scrolltrackWidth, realLowerVisibleClient.Top + this.tabList.columnheaderHeight, this.tabList.scrolltrackWidth, this.VerticalScrollTrackLength);
                
                int calculatedHeight = (this.VerticalScrollTrackLength * realLowerVisibleClient.Height) / this.tabList.FullLowerClient.Height;
                this.VerticalScrollBar = new Rectangle((realLowerVisibleClient.Right - this.tabList.scrolltrackWidth) - this.tabList.scrollbuttonWidth, realLowerVisibleClient.Top + this.tabList.columnheaderHeight, this.tabList.scrollbuttonWidth, Math.Max(20, calculatedHeight));
                
                this.tabList.ShrinkRectanglesWidth();
            }
            else
            {
                this.tabList.ShowVerticalScrollBar = false;
                this.tabList.EnlargeRectanglesWidth();
            }
            
            // 调整滚动条位置（如果有选中的标签）
            if (this.SelectedTab != null)
            {
                Rectangle visibleLowerClient = this.tabList.VisibleLowerClient;
                if (this.VerticalScrollBar.Bottom < visibleLowerClient.Bottom)
                {
                    int num = (int)((Math.Abs(this.SelectedTab.CurrentYOffset) * visibleLowerClient.Height) / ((double)this.tabList.FullLowerClient.Height));
                    if ((num + this.VerticalScrollBar.Bottom) > visibleLowerClient.Bottom)
                    {
                        num = visibleLowerClient.Bottom - this.VerticalScrollBar.Bottom;
                    }
                    this.VerticalScrollBar = new Rectangle(this.VerticalScrollBar.X, this.VerticalScrollBar.Y + num, this.VerticalScrollBar.Width, this.VerticalScrollBar.Height);
                }
                else if (this.tabList.ShowHorizontalScrollBar)
                {
                    this.VerticalScrollBar = new Rectangle(this.VerticalScrollBar.X, ((visibleLowerClient.Bottom - this.VerticalScrollBar.Height) - (2 * this.tabList.scrolltrackWidth)) - this.tabList.scrollbuttonWidth, this.VerticalScrollBar.Width, this.VerticalScrollBar.Height);
                }
                else
                {
                    this.VerticalScrollBar = new Rectangle(this.VerticalScrollBar.X, visibleLowerClient.Bottom - this.VerticalScrollBar.Height, this.VerticalScrollBar.Width, this.VerticalScrollBar.Height);
                }
            }
        }

        public void ResetAllOtherTabs(Tab currentTab)
        {
            // 重置其他标签的基本实现
            foreach (var tab in this.Tabs)
            {
                if (tab != currentTab)
                {
                    tab.Selected = false;
                }
            }
        }
    }
}