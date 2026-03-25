using GameFreeText;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using GameManager;

namespace TabListPlugin
{

    public class Tab
    {
        public List<Column> Columns;
        public string DisplayName;
        public int ID;
        private ListKind listKind;
        public string ListKind;
        public string ListMethod;
        public string Name;
        public float Scale = 1f;
        private bool selected;
        public bool SmallToBig;
        public int SortColumnID;
        private TabListInFrame tabList;
        internal FreeText Text;

        internal Tab(TabListInFrame tabList, ListKind listKind)
        {
            this.Text = new FreeText(tabList.TabTextBuilder);
            this.Text.TextColor = tabList.TabTextColor;
            this.Text.Align = tabList.TabTextAlign;
            this.Text.DisplayOffset = new Point(-10, -6); // Shift Left-Up (Center). Adjusted from -2.
            this.tabList = tabList;
            this.listKind = listKind;
            this.Columns = new List<Column>();
        }

        private int LeastDetailLevelCache = -1;
        public int LeastDetailLevel
        {
            get
            {
                int r = 99;
                if (LeastDetailLevelCache < 0){
                    foreach (Column c in this.Columns)
                    {
                        if (c.DetailLevel < r && c.CountToDisplay)
                        {
                            r = c.DetailLevel;
                        }
                    }
                    LeastDetailLevelCache = r;
                }
                return LeastDetailLevelCache;
            }
        }

        public bool Visible
        {
            get
            {
                return this.LeastDetailLevel <= Session.GlobalVariables.TabListDetailLevel;
            }
        }

        public void Draw()
        {
            if (this.Visible)
            {
                if (this.selected)
                {
                    CacheManager.Draw(this.tabList.tabbuttonselectedTexture, this.Position, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.035f);
                    this.Text.Draw(Color.White, 0.03499f);
                    foreach (Column column in this.Columns)
                    {
                        if (column.Visible)
                        {
                            column.Draw();
                        }
                    }
                }
                else
                {
                    CacheManager.Draw(this.tabList.tabbuttonTexture, this.Position, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.035f);
                    this.Text.Draw(0.03499f);
                }
            }
        }

        public Column GetColumnByID(int ID)
        {
            foreach (Column column in this.Columns)
            {
                if (column.ID == ID)
                {
                    return column;
                }
            }
            return null;
        }

        public void LoadColumnsFromString(string columnString)
        {
            char[] separator = new char[] { ' ' };
            string[] strArray = columnString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < strArray.Length; i++)
            {
                Column columnByID = this.listKind.GetColumnByID(int.Parse(strArray[i]));
                if (columnByID != null)
                {
                    this.Columns.Add(columnByID);
                }
            }
        }

        public void MoveHorizontal(int offset)
        {
            // 🔥 推荐版模式禁用横向移动
            // 日期：2026-03-23
            // 原因：推荐版模式下固定列+弹性列布局，不需要横向滚动
            if (this.tabList.IsRecommendedMode())
            {
                return;
            }

            if ((this.Columns[0].DisplayPosition.Left + offset) > this.tabList.VisibleLowerClient.Left)
            {
                offset = this.tabList.VisibleLowerClient.Left - this.Columns[0].DisplayPosition.Left;
            }
            else if ((this.Columns[this.Columns.Count - 1].DisplayPosition.Right + offset) < this.tabList.VisibleLowerClient.Right)
            {
                offset = this.tabList.VisibleLowerClient.Right - this.Columns[this.Columns.Count - 1].DisplayPosition.Right;
            }
            if (offset != 0)
            {
                foreach (Column column in this.Columns)
                {
                    column.MoveHorizontal(offset);
                }
            }
        }

        public void MoveVertical(int offset)
        {
            foreach (Column column in this.Columns)
            {
                column.MoveVertical(offset);
            }
            if (this.Columns.Count > 0)
            {
                this.Columns[0].AdjustRowRectangles(this.tabList.RowRectangles);
            }
        }

        internal void ReCalculate(int yOffset)
        {
            if (!this.selected) return;

            int x = this.tabList.RealClient.X;
            this.tabList.FullLowerClient.X = x;
            this.tabList.FullLowerClient.Y = this.tabList.VisibleLowerClient.Y;

            // 🔥 推荐版模式：固定列 + 弹性列布局
            // 日期：2026-03-23
            if (this.tabList.IsRecommendedMode())
            {
                // 1. 计算固定列总宽（跳过描述列）
                int fixedColumnsWidth = 0;
                foreach (Column column in this.Columns)
                {
                    if (column.Name != "Description")
                    {
                        int columnWidth = CalculateFixedColumnWidth(column);
                        fixedColumnsWidth += columnWidth + this.tabList.columnspliterWidth;
                    }
                }

                // 2. 布局所有列（跳过描述列，因为下方有详情说明区）
                foreach (Column column in this.Columns)
                {
                    if (column.Name == "Description")
                    {
                        // 🔥 推荐版模式：隐藏描述列（下方有详情说明区）
                        continue;
                    }

                    // 固定列：使用受控宽度
                    int fixedWidth = CalculateFixedColumnWidth(column);
                    column.Text.Position = new Rectangle(x + 1, this.listKind.ColumnsTop, fixedWidth, column.Text.Position.Height);
                    x = column.Text.Position.Right + this.tabList.columnspliterWidth;

                    // 更新单元格位置
                    if (this.tabList.gameObjectList != null)
                    {
                        for (int i = 0; i < column.ColumnTextList.Count; i++)
                        {
                            column.ColumnTextList[i].MaxWidth = fixedWidth;
                            column.ColumnTextList[i].Position = new Rectangle(
                                column.Text.Position.X,
                                (column.Text.Position.Bottom + 1) + (i * this.tabList.rowHeight),
                                fixedWidth,
                                this.tabList.rowHeight);
                        }
                        column.ColumnTextList.ResetAllAlignedPositions();
                    }

                    column.ColumnTextList.DisplayOffset = new Point(0, yOffset);
                }
            }
            else
            {
                // 原始逻辑：按内容测宽
                foreach (Column column in this.Columns)
                {
                    column.ReCalculate(this.listKind.ColumnsTop, ref x);
                    column.ColumnTextList.DisplayOffset = new Point(0, yOffset);
                }
            }

            this.SortTheKeyColumn();
            this.tabList.FullLowerClient.Width = x - this.tabList.RealClient.X + this.tabList.iGameFrame.LeftEdge + this.tabList.iGameFrame.RightEdge;
            this.listKind.ResetScrollTracks();
            
            if (this.Columns.Count > 0)
            {
                this.Columns[0].AdjustRowRectangles(this.tabList.RowRectangles);
            }
            
            this.ResetAllTextures();
        }

        internal void ResetAllTextures()
        {
            foreach (Column column in this.Columns)
            {
                column.ResetAllTextures();
            }
        }

        internal void ResetEditableTextures()
        {
            foreach (Column column in this.Columns)
            {
                column.ResetEditableTextures();
            }
        }

        public void ResetSelected()
        {
            this.selected = false;
        }

        public void SetPosition(Rectangle position)
        {
            this.Position = position;
        }

        private void SortTheKeyColumn()
        {
            if (this.SortColumnID > 0)
            {
                Column columnByID = this.GetColumnByID(this.SortColumnID);
                if (columnByID != null)
                {
                    PropertyComparer comparer = new PropertyComparer(columnByID.Name, columnByID.IsNumber, this.SmallToBig);
                    this.tabList.gameObjectList.GameObjects.Sort(comparer);
                }
            }
        }

        /// <summary>
        /// 计算固定列的宽度（推荐版模式）
        /// 基于 XML 的 MinWidth + 内容测宽，应用上限（最多扩大到 MinWidth 的 2 倍）
        /// 日期：2026-03-23
        /// </summary>
        private int CalculateFixedColumnWidth(Column column)
        {
            if (column == null) return 120; // 后备值

            int baseWidth = column.MinWidth;
            if (baseWidth <= 0) baseWidth = 80; // 最小后备值

            // 🔥 上限：最多扩大到 MinWidth 的 2 倍
            int maxAllowedWidth = baseWidth * 2;

            // 测量内容宽度（复用 Column.ReCalculate 的逻辑）
            var font = Session.Current.Font;
            if (font == null) return baseWidth;

            float finalCalculatedWidth = baseWidth;
            float headerScale = column.Text.Builder.Scale;

            // 1. 测量列头宽度
            if (!string.IsNullOrEmpty(column.DisplayName))
            {
                float headerWidth = 0;
                try { headerWidth = font.MeasureString(column.DisplayName).X * headerScale; } 
                catch { headerWidth = column.DisplayName.Length * 24 * headerScale; }

                float safeHeaderWidth = headerWidth + 50;
                if (safeHeaderWidth > finalCalculatedWidth) 
                    finalCalculatedWidth = safeHeaderWidth;
            }

            // 2. 测量内容宽度（仅对非可编辑列）
            if (!column.Editable && this.tabList.gameObjectList != null && column.ColumnTextList.Count > 0)
            {
                float contentScale = (column.ColumnTextList.Font != null) 
                    ? column.ColumnTextList.Font.Scale 
                    : headerScale;
                float maxContentPixelWidth = 0;

                // 🔥 性能优化：只采样前 20 行（避免遍历大列表）
                int sampleCount = Math.Min(20, column.ColumnTextList.Count);
                for (int i = 0; i < sampleCount; i++)
                {
                    string text = column.ColumnTextList[i].Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        float w = 0;
                        try { w = font.MeasureString(text).X * contentScale; } 
                        catch { w = text.Length * 14 * contentScale; }
                        
                        if (w > maxContentPixelWidth) 
                            maxContentPixelWidth = w;
                    }
                }

                float safeContentWidth = maxContentPixelWidth + 30;
                if (safeContentWidth > finalCalculatedWidth)
                {
                    finalCalculatedWidth = safeContentWidth;
                }
            }

            // 3. 应用上限
            if (finalCalculatedWidth > maxAllowedWidth)
            {
                finalCalculatedWidth = maxAllowedWidth;
            }

            return (int)Math.Ceiling(finalCalculatedWidth);
        }

        internal int CurrentYOffset
        {
            get
            {
                return ((this.Columns.Count > 0) ? this.Columns[0].ColumnTextList.DisplayOffset.Y : 0);
            }
        }

        internal Rectangle Position
        {
            get
            {
                return this.Text.Position;
            }
            set
            {
                this.Text.Position = value;
            }
        }

        public bool Selected
        {
            get
            {
                return this.selected;
            }
            set
            {
                if (this.selected != value)
                {
                    this.selected = value;
                    this.ReCalculate(this.listKind.ResetAllOtherTabs(this));
                    this.listKind.SelectedTab = this;
                }
            }
        }
    }
}

