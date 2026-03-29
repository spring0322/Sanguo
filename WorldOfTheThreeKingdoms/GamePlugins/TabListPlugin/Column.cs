using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using System;
using System.Collections.Generic;
using System.Text;

namespace TabListPlugin
{
    public class Column
    {
        internal FreeTextList ColumnTextList;
        public string DisplayName;
        internal bool Editable = false;
        public int ID;
        public bool IsNumber;
        public int MinWidth;
        public string Name;
        public float Scale = 1f;
        public bool SmallToBig;
        public int ItemID = -1;
        public int DetailLevel;
        public bool CountToDisplay = true;
        private TabListInFrame tabList;
        internal FreeText Text;
        
        // 🎯 性能优化：缓存 RasterizerState，避免每帧分配
        private static readonly RasterizerState ScissorEnabledState = new RasterizerState { ScissorTestEnable = true };
        
        // 🎯 性能优化：缓存换行结果列表和 StringBuilder，避免每帧分配
        private readonly List<string> cachedWrappedLines = [];
        private readonly StringBuilder lineBuilder = new StringBuilder(256);
        private readonly StringBuilder clipBuilder = new StringBuilder(256);
        
        // 🎯 褒赏箭头：静态常量，避免每帧分配
        private const string REWARD_ARROW = "↑ ";

        internal Column(TabListInFrame tabList)
        {
            this.tabList = tabList;
            this.Text = new FreeText(tabList.ColumnTextBuilder);
            this.Text.TextColor = tabList.ColumnTextColor;
            this.Text.Align = tabList.ColumnTextAlign;
            this.Text.Position = new Rectangle(0, 0, 0, tabList.columnheaderHeight);
        }

        public void AdjustRowRectangles(List<Rectangle> rowRectangles)
        {
            for (int i = 0; i < rowRectangles.Count; i++)
            {
                rowRectangles[i] = new Rectangle(rowRectangles[i].X, this.ColumnTextList.DisplayPosition(i).Y, rowRectangles[i].Width, this.tabList.rowHeight);
            }
        }

        public void ClearData()
        {
            this.ColumnTextList.Clear();
        }

        public void Draw()
        {
            // 🔥 推荐版模式：跳过描述列绘制（改为右侧竖向详情区）
            if (this.tabList.IsRecommendedMode() && this.Name == "Description")
            {
                return;
            }

            // 1. 基础视口剔除
            if (this.DisplayPosition.Right <= this.tabList.VisibleLowerClient.Left || 
                this.DisplayPosition.Left >= this.tabList.VisibleLowerClient.Right)
            {
                return; 
            }

            // 🎯 使用 ScissorRectangle 进行硬件级裁剪（最可靠的方法）
            var graphicsDevice = Platform.GraphicsDevice;
            var oldScissorRect = graphicsDevice.ScissorRectangle;
            var oldRasterizerState = graphicsDevice.RasterizerState;
            
            try
            {
                // 设置裁剪区域为可见区域
                var scissorRect = new Rectangle(
                    this.tabList.VisibleLowerClient.X,
                    this.tabList.VisibleLowerClient.Y,
                    this.tabList.VisibleLowerClient.Width,
                    this.tabList.VisibleLowerClient.Height
                );
                
                // 确保裁剪区域在屏幕范围内
                if (scissorRect.Right > graphicsDevice.Viewport.Width)
                {
                    scissorRect.Width = graphicsDevice.Viewport.Width - scissorRect.X;
                }
                if (scissorRect.Bottom > graphicsDevice.Viewport.Height)
                {
                    scissorRect.Height = graphicsDevice.Viewport.Height - scissorRect.Y;
                }
                
                // 🎯 性能优化：使用缓存的 RasterizerState，避免每帧分配
                graphicsDevice.RasterizerState = ScissorEnabledState;
                graphicsDevice.ScissorRectangle = scissorRect;

                // 🎯 计算绘制的安全区域
                float allowedRight = this.tabList.VisibleLowerClient.Right;
                
                // 2. 绘制表头背景和分割线
                CacheManager.Draw(this.tabList.columnheaderTexture, this.DisplayPosition, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.035f);
                CacheManager.Draw(this.tabList.columnspliterTexture, this.SpliterPosition, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.035f);

                // 🎯 文本绘制的硬性右边界
                float hardLimitX = allowedRight - 10; // 留10px缓冲

                // 3. 绘制表头文字
                var headRec = this.Text.AlignedPosition;
                var headPos = new Vector2(headRec.X - 12, headRec.Y - 6);
                string clippedHeadText = GetStrictClippedText(this.Text.Text, headPos.X, hardLimitX, this.Text.Builder.Scale);
                
                if (!string.IsNullOrEmpty(clippedHeadText))
                {
                    CacheManager.DrawString(Session.Current.Font, clippedHeadText, headPos, Color.Gold, 0f, Vector2.Zero, this.Text.Builder.Scale, SpriteEffects.None, 0.03499f);
                }

                // 4. 绘制列表内容
                for (int i = 0; i < this.ColumnTextList.Count; i++)
                {
                    var cellRect = this.ColumnTextList.DisplayPosition(i);
                    
                    // 垂直剔除
                    if (cellRect.Bottom <= this.tabList.VisibleLowerClient.Top || cellRect.Top >= this.tabList.VisibleLowerClient.Bottom)
                        continue;

                    if (this.Editable)
                    {
                        if (this.ColumnTextList[i].TextTexture != null)
                        {
                            var rec = StaticMethods.CenterRectangle(cellRect, new Rectangle(0, 0, this.tabList.checkboxWidth, this.tabList.checkboxWidth));
                            CacheManager.Draw(this.ColumnTextList[i].TextTexture, rec, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.03499f);
                        }
                    }
                    else
                    {
                        // 🔥 2026-03-29：设施列表的效果和条件列使用多行文本显示
                        bool isMultiLineColumn = (this.Name == "Description" || this.Name == "ConditionString") &&
                                                 this.tabList.listKindToDisplay?.Name == "Facility";
                        
                        if (isMultiLineColumn)
                        {
                            DrawMultiLineCellText(i, cellRect, hardLimitX);
                        }
                        else
                        {
                            DrawCellText(i, cellRect, hardLimitX);
                        }
                    }
                }
            }
            finally
            {
                // 恢复原始状态
                graphicsDevice.ScissorRectangle = oldScissorRect;
                graphicsDevice.RasterizerState = oldRasterizerState;
            }
        }

        private void DrawCellText(int index, Rectangle cellRect, float rightLimitX)
        {
            string fullText = this.ColumnTextList[index].Text;
            
            if (string.IsNullOrEmpty(fullText)) return;

            var itemRec = this.ColumnTextList[index].AlignedPosition;
            
            // 如果起始位置就已经超过了硬性边界，直接放弃绘制
            if (itemRec.X >= rightLimitX) return;

            // 🎯 修复：对于右对齐的文本，itemRec 已经是正确的绘制位置
            // 不需要重新计算 cellWidth，直接使用列的宽度
            float cellWidth = this.DisplayPosition.Width;

            float fontScale = this.ColumnTextList.Font.Scale;
            float lineHeight = 18 * fontScale;

            // 🎯 智能字体缩放：如果文本过长，自动缩小字体
            float textPixelWidth = 0;
            try { textPixelWidth = Session.Current.Font.MeasureString(fullText).X * fontScale; }
            catch { textPixelWidth = fullText.Length * 14 * fontScale; }

            if (textPixelWidth > cellWidth && !this.IsNumber)
            {
                float ratio = cellWidth / textPixelWidth;
                // 允许缩小到 50%-100%，确保长文本能显示
                if (ratio >= 0.5f && ratio < 1.0f) 
                {
                    fontScale *= ratio; 
                    lineHeight = 18 * fontScale;
                }
                else if (ratio < 0.5f)
                {
                    // 如果需要缩小到50%以下，限制在50%（保证可读性）
                    fontScale *= 0.5f;
                    lineHeight = 18 * fontScale;
                }
            }

            // 🎯 褒赏状态检测：检查当前行对应的GameObject是否是已褒赏的Person
            bool isRewarded = false;
            bool isHoneymoon = false;
            if (index < this.tabList.gameObjectList.Count)
            {
                GameObject obj = this.tabList.gameObjectList[index];
                if (obj is Person person)
                {
                    // 🔥 修复：只有当TempLoyaltyChange > 0时才显示箭头（表示有褒赏带来的忠诚度加成）
                    // 而不是根据RewardFinished（本月是否已褒赏）来判断
                    isRewarded = person.TempLoyaltyChange > 0;
                    isHoneymoon = person.HoneymoonMonths > 0;
                }
            }

            // 获取换行 (基于实际可用宽度换行)
            List<string> wrappedLines = GetWrappedLines(fullText, cellWidth, this.ColumnTextList.Font, fontScale);

            // 垂直裁剪
            int maxLinesVisible = (int)(this.tabList.rowHeight / lineHeight);
            if (maxLinesVisible < 1) maxLinesVisible = 1;

            Color rowColor = this.ColumnTextList.TextColor;
            
            // 🎯 姓名列特殊颜色处理
            if (this.DisplayName == "姓名" || this.Name == "Name")
            {
                rowColor = Color.Orange;
            }
            else if (this.Name == "TitleName" || this.DisplayName == "称号")
            {
                rowColor = GetTitleColor(fullText);
            }
            else if (this.Name == "ConvinceExecutingMark")
            {
                rowColor = fullText == "○" ? Color.LightGreen : Color.IndianRed;
            }
            else
            {
                // 🔥 修复：强制统一为白色（移除三色轮换）
                rowColor = Color.White;
            }

            float yOffset = -4; 
            int linesToDraw = Math.Min(wrappedLines.Count, maxLinesVisible);
            
            // 🎯 褒赏箭头绘制：在忠诚度列绘制向上箭头
            bool shouldDrawArrow = isRewarded && (this.DisplayName == "忠诚度" || this.Name == "Loyalty");
            
            // 🎯 蜜月期标记：在姓名列绘制蜜月标记 ♥
            bool shouldDrawHoneymoon = isHoneymoon && (this.DisplayName == "姓名" || this.Name == "Name");
            
            // 🎯 忠诚度居中对齐
            bool shouldCenter = (this.DisplayName == "忠诚度" || this.Name == "Loyalty" || this.Name == "ConvinceExecutingMark");
            
            for (int lineIdx = 0; lineIdx < linesToDraw; lineIdx++)
            {
                string line = wrappedLines[lineIdx];
                if (lineIdx == linesToDraw - 1 && wrappedLines.Count > linesToDraw)
                {
                    if (line.Length > 2) line = line.Substring(0, line.Length - 1) + "..";
                }

                var itemPos = new Vector2(itemRec.X, itemRec.Y + yOffset);

                // 🎯 忠诚度居中对齐：计算居中位置
                if (shouldCenter && lineIdx == 0)
                {
                    float textWidth = 0;
                    try { textWidth = Session.Current.Font.MeasureString(line).X * fontScale; }
                    catch { textWidth = line.Length * 14 * fontScale; }
                    
                    // 如果有箭头，需要加上箭头宽度
                    if (shouldDrawArrow)
                    {
                        float arrowWidth = Session.Current.Font.MeasureString(REWARD_ARROW).X * fontScale;
                        textWidth += arrowWidth;
                    }
                    
                    // 居中：(列宽 - 文本宽) / 2
                    float centerOffset = (cellWidth - textWidth) / 2;
                    if (centerOffset > 0)
                    {
                        itemPos.X = cellRect.X + centerOffset;
                    }
                }

                // 🎯 褒赏箭头：在文本前绘制向上箭头 ↑
                if (shouldDrawArrow && lineIdx == 0)
                {
                    CacheManager.DrawString(Session.Current.Font, REWARD_ARROW, itemPos, Color.LightGreen, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0.03499f);
                    
                    // 调整文本位置，为箭头留出空间
                    float arrowWidth = Session.Current.Font.MeasureString(REWARD_ARROW).X * fontScale;
                    itemPos.X += arrowWidth;
                }
                
                // 🎯 蜜月期标记：在文本前绘制爱心 ♥
                if (shouldDrawHoneymoon && lineIdx == 0)
                {
                    const string HONEYMOON_MARK = "♥";
                    CacheManager.DrawString(Session.Current.Font, HONEYMOON_MARK, itemPos, Color.Pink, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0.03499f);
                    
                    // 调整文本位置，为标记留出空间
                    float markWidth = Session.Current.Font.MeasureString(HONEYMOON_MARK).X * fontScale;
                    itemPos.X += markWidth;
                }

                // [绝对防御]: 使用最严格的裁剪方法（ScissorRectangle 已经在 Draw 中启用）
                string clippedLine = GetStrictClippedText(line, itemPos.X, rightLimitX, fontScale);

                if (!string.IsNullOrEmpty(clippedLine))
                {
                    CacheManager.DrawString(Session.Current.Font, clippedLine, itemPos, rowColor, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0.03499f);
                }
                
                yOffset += lineHeight;
            }
        }

        /// <summary>
        /// 绘制多行文本单元格（用于设施列表的效果和条件列）
        /// 
        /// 特性：
        /// - 支持 • 分隔的多行文本
        /// - 自动换行，确保内容不溢出列宽
        /// - 超出行高时显示省略号
        /// 
        /// 日期：2026-03-29
        /// </summary>
        private void DrawMultiLineCellText(int index, Rectangle cellRect, float rightLimitX)
        {
            string fullText = this.ColumnTextList[index].Text;
            
            if (string.IsNullOrEmpty(fullText)) return;

            // 如果起始位置就已经超过了硬性边界，直接放弃绘制
            if (cellRect.X >= rightLimitX) return;

            float cellWidth = this.DisplayPosition.Width - 20; // 留20px左右边距
            float fontScale = 0.85f; // 稍微缩小字体，让长文本更精致
            float lineHeight = 18 * fontScale;

            // 🔥 关键：将 • 分隔的文本拆分为多行
            string[] bulletPoints = fullText.Split('•', StringSplitOptions.RemoveEmptyEntries);
            
            Color rowColor = Color.White;
            float yOffset = 2; // 顶部留2px边距
            
            // 计算可显示的最大行数
            int maxLinesVisible = (int)((this.tabList.rowHeight - 4) / lineHeight);
            if (maxLinesVisible < 1) maxLinesVisible = 1;
            
            int currentLine = 0;
            
            for (int bulletIdx = 0; bulletIdx < bulletPoints.Length; bulletIdx++)
            {
                string bullet = bulletPoints[bulletIdx].Trim();
                if (string.IsNullOrEmpty(bullet)) continue;
                
                // 为每个要点添加 • 前缀
                string bulletText = "•" + bullet;
                
                // 对每个要点进行换行处理
                List<string> wrappedLines = GetWrappedLines(bulletText, cellWidth, this.ColumnTextList.Font, fontScale);
                
                for (int lineIdx = 0; lineIdx < wrappedLines.Count; lineIdx++)
                {
                    if (currentLine >= maxLinesVisible)
                    {
                        // 超出可显示行数，绘制省略号后退出
                        var ellipsisPos = new Vector2(cellRect.X + 10, cellRect.Y + yOffset);
                        CacheManager.DrawString(Session.Current.Font, "...", ellipsisPos, rowColor, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0.03499f);
                        return;
                    }
                    
                    string line = wrappedLines[lineIdx];
                    var itemPos = new Vector2(cellRect.X + 10, cellRect.Y + yOffset);
                    
                    // 裁剪文本，确保不超出右边界
                    string clippedLine = GetStrictClippedText(line, itemPos.X, rightLimitX, fontScale);
                    
                    if (!string.IsNullOrEmpty(clippedLine))
                    {
                        CacheManager.DrawString(Session.Current.Font, clippedLine, itemPos, rowColor, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0.03499f);
                    }
                    
                    yOffset += lineHeight;
                    currentLine++;
                }
            }
        }

        // [严厉的裁剪方法 - 强制防止超出UI]
        private string GetStrictClippedText(string text, float startX, float limitX, float scale)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            // 1. 如果起点在界外，全砍
            if (startX >= limitX) return "";
            
            float availableWidth = limitX - startX;
            if (availableWidth <= 10) return ""; // 空间太小，不画

            // 2. 测量完整文本宽度
            float fullWidth = 0;
            try 
            { 
                fullWidth = Session.Current.Font.MeasureString(text).X * scale; 
            }
            catch 
            { 
                fullWidth = text.Length * 14 * scale; // 估算
            }

            // 3. 如果能完整放下，直接返回
            if (fullWidth <= availableWidth) return text;

            // 4. 放不下，逐字裁剪并加省略号
            // 🎯 性能优化：复用缓存的 StringBuilder
            clipBuilder.Clear();
            
            // 预留省略号的空间（约20px）
            float ellipsisWidth = 20 * scale;
            float targetWidth = availableWidth - ellipsisWidth;
            if (targetWidth < 10) return "..."; // 空间太小，只显示省略号

            float currentWidth = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                float charW = 14 * scale;
                try { charW = Session.Current.Font.MeasureString(c.ToString()).X * scale; } catch { }

                if (currentWidth + charW > targetWidth)
                {
                    break; // 超出目标宽度，停止
                }
                
                clipBuilder.Append(c);
                currentWidth += charW;
            }

            // 如果裁剪了内容，加上省略号
            if (clipBuilder.Length < text.Length && clipBuilder.Length > 0)
            {
                clipBuilder.Append("...");
            }

            return clipBuilder.ToString();
        }

        private Color GetTitleColor(string text)
        {
            int idx = text.IndexOf("级");
            if (idx > 0 && int.TryParse(text.AsSpan(0, idx), out int level))
            {
                if (level >= 9) return Color.Gold;
                if (level == 8) return Color.Magenta;
                if (level == 7) return Color.Cyan;
                if (level == 6) return Color.Lime;
            }
            return Color.White;
        }

        private List<string> GetWrappedLines(string text, float maxWidth, WorldOfTheThreeKingdoms.GameGlobal.Font font, float currentScale)
        {
            // 🎯 性能优化：复用缓存的列表，避免每次分配
            cachedWrappedLines.Clear();
            
            if (string.IsNullOrEmpty(text)) return cachedWrappedLines;

            float actualWidth = 0;
            try { actualWidth = Session.Current.Font.MeasureString(text).X * currentScale; }
            catch { actualWidth = text.Length * 14 * currentScale; }

            if (actualWidth <= maxWidth || this.IsNumber)
            {
                cachedWrappedLines.Add(text);
                return cachedWrappedLines;
            }

            // 🎯 性能优化：使用 StringBuilder 和 for 循环，避免字符串拼接分配
            lineBuilder.Clear();
            float currentWidth = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                float charWidth = 14 * currentScale;
                try { charWidth = Session.Current.Font.MeasureString(c.ToString()).X * currentScale; } catch { }

                if (currentWidth + charWidth > maxWidth)
                {
                    if (lineBuilder.Length > 0)
                    {
                        cachedWrappedLines.Add(lineBuilder.ToString());
                        lineBuilder.Clear();
                    }
                    lineBuilder.Append(c);
                    currentWidth = charWidth;
                }
                else
                {
                    lineBuilder.Append(c);
                    currentWidth += charWidth;
                }
            }
            
            if (lineBuilder.Length > 0)
            {
                cachedWrappedLines.Add(lineBuilder.ToString());
            }

            return cachedWrappedLines;
        }

        public void MoveHorizontal(int offset)
        {
            this.Text.DisplayOffset = new Point(this.Text.DisplayOffset.X + offset, this.Text.DisplayOffset.Y);
            this.ColumnTextList.DisplayOffset = new Point(this.ColumnTextList.DisplayOffset.X + offset, this.ColumnTextList.DisplayOffset.Y);
        }

        public void MoveVertical(int offset)
        {
            if (this.ColumnTextList.Count == 0) return;
            Rectangle rectangle = this.ColumnTextList.DisplayPosition(0);
            Rectangle rectangle2 = this.ColumnTextList.DisplayPosition(this.ColumnTextList.Count - 1);
            if ((rectangle.Top + offset) > (this.DisplayPosition.Bottom + 1))
            {
                offset = (this.DisplayPosition.Bottom + 1) - rectangle.Top;
            }
            else if ((rectangle2.Bottom + offset) < this.tabList.VisibleLowerClient.Bottom)
            {
                offset = this.tabList.VisibleLowerClient.Bottom - rectangle2.Bottom;
            }
            if (offset != 0)
            {
                this.ColumnTextList.DisplayOffset = new Point(this.ColumnTextList.DisplayOffset.X, this.ColumnTextList.DisplayOffset.Y + offset);
            }
        }

        public string GetDataString(int index)
        {
            // 🔥 特殊处理：带 ItemID 参数的方法调用
            if (this.Name.Equals("HasSkill")) 
                return (this.tabList.gameObjectList[index] is Person p && p.HasSkill(this.ItemID)) ? "○" : "×";
            
            if (this.Name.Equals("HasStunt")) 
                return (this.tabList.gameObjectList[index] is Person p && p.HasStunt(this.ItemID)) ? "○" : "×";
            
            if (this.Name.Equals("TitleName")) 
                return (this.tabList.gameObjectList[index] is Person p) ? p.TitleName(this.ItemID) : "";
            
            if (this.Name.Equals("HasInfluenceKind")) 
                return (this.tabList.gameObjectList[index] is Person p && p.HasInfluenceKind(this.ItemID)) ? "○" : "×";
            
            if (this.Name.Equals("InfluenceKindValueByTreasure")) 
                return (this.tabList.gameObjectList[index] is Person p) ? p.InfluenceKindValueByTreasure(this.ItemID).ToString() : "";
            
            // 🔥 2026-03-03 修复：添加 HasTreasureforGroup 支持
            if (this.Name.Equals("HasTreasureforGroup"))
                return (this.tabList.gameObjectList[index] is Person p && p.HasTreasureforGroup(this.ItemID)) ? "○" : "×";

            // 🔥 三层忠诚度显示系统：使用 LoyaltyDisplay（根据军师智力显示误差）
            if (this.Name.Equals("Loyalty"))
                return (this.tabList.gameObjectList[index] is Person person) ? person.LoyaltyDisplay.ToString() : "";

            // 🔥 2026-03-13 修复：IdealTendencyString 属性（源生成器未生成）
            if (this.Name.Equals("IdealTendencyString"))
            {
                if (this.tabList.gameObjectList[index] is not Person p)
                {
                    return "";
                }
                
                return p.IdealTendencyString;
            }

            // 🔥 2026-03-13 修复：CharacterString 属性（调试）
            if (this.Name.Equals("CharacterString"))
            {
                if (this.tabList.gameObjectList[index] is not Person p)
                {
                    return "";
                }
                
                return p.CharacterString;
            }

            // 🔥 2026-03-13 修复：Braveness 属性（源生成器未生成）
            if (this.Name.Equals("Braveness"))
                return (this.tabList.gameObjectList[index] is Person p) ? p.Braveness.ToString() : "";

            // 🔥 2026-03-13 修复：Calmness 属性（源生成器未生成）
            if (this.Name.Equals("Calmness"))
                return (this.tabList.gameObjectList[index] is Person p) ? p.Calmness.ToString() : "";

            if (this.Name.Equals("ConvinceExecutingMark"))
                return this.GetConvinceExecutingMark(index);

            // 🔥 默认处理：使用源生成器的属性/方法访问
            object obj = StaticMethods.GetPropertyValue(this.tabList.gameObjectList[index], this.Name);
            

            
            if (obj is bool b) 
                return b ? "○" : "×";
            
            return obj.ToString();
        }

        private string GetConvinceExecutingMark(int index)
        {
            if (index < 0 || index >= this.tabList.gameObjectList.Count)
            {
                return "×";
            }

            if (this.tabList.gameObjectList[index] is not Person targetPerson)
            {
                return "×";
            }

            Faction currentPlayer = Session.Current?.Scenario?.CurrentPlayer;
            if (currentPlayer?.Persons == null)
            {
                return "×";
            }

            for (int i = 0; i < currentPlayer.Persons.Count; i++)
            {
                if (currentPlayer.Persons[i] is not Person executor)
                {
                    continue;
                }

                if (executor.Status != GameObjects.PersonDetail.PersonStatus.Moving)
                {
                    continue;
                }

                if (executor.OutsideTask != OutsideTaskKind.说服)
                {
                    continue;
                }

                if (executor.ConvincingPerson == targetPerson)
                {
                    return "○";
                }
            }

            return "×";
        }

        public void ReCalculate(int top, ref int previousRight)
        {
            if (!this.Visible) return;

            this.Text.DisplayOffset = Point.Zero;

            if (this.tabList.gameObjectList != null)
            {
                if (this.ColumnTextList.Count != this.tabList.gameObjectList.Count)
                {
                    this.ColumnTextList.Clear();
                    for (int num2 = 0; num2 < this.tabList.gameObjectList.Count; num2++)
                    {
                        string s = GetDataString(num2);
                        this.ColumnTextList.AddText(s);
                    }
                }
            }

            Microsoft.Xna.Framework.Graphics.SpriteFont font = Session.Current.Font;
            float finalCalculatedWidth = this.MinWidth;

            if (font != null)
            {
                float headerScale = this.Text.Builder.Scale;
                
                if (!string.IsNullOrEmpty(this.DisplayName))
                {
                    float headerWidth = 0;
                    try { headerWidth = font.MeasureString(this.DisplayName).X * headerScale; } catch { }
                    float estimated = this.DisplayName.Length * 24 * headerScale;
                    if (headerWidth < estimated) headerWidth = estimated;

                    float safeHeaderWidth = headerWidth + 50; 
                    if (safeHeaderWidth > finalCalculatedWidth) finalCalculatedWidth = safeHeaderWidth;
                }

                if (!this.Editable && this.tabList.gameObjectList != null && this.ColumnTextList.Count > 0)
                {
                    // 🔥 推荐版模式：禁用描述列的内容测宽扩列
                    // 日期：2026-03-23
                    // 原因：描述列宽度由 Tab.ReCalculate 决定（弹性列），不应根据内容扩列
                    bool isDescriptionInRecommendedMode = this.tabList.IsRecommendedMode() && this.Name == "Description";
                    
                    if (!isDescriptionInRecommendedMode)
                    {
                        float contentScale = (this.ColumnTextList.Font != null) ? this.ColumnTextList.Font.Scale : headerScale;
                        float maxContentPixelWidth = 0;

                        for (int i = 0; i < this.ColumnTextList.Count; i++)
                        {
                            string text = this.ColumnTextList[i].Text;
                            if (!string.IsNullOrEmpty(text))
                            {
                                float w = 0;
                                try { w = font.MeasureString(text).X * contentScale; } catch { }
                                if (w > maxContentPixelWidth) maxContentPixelWidth = w;
                            }
                        }

                        float safeContentWidth = maxContentPixelWidth + 30;
                        if (safeContentWidth > finalCalculatedWidth)
                        {
                            finalCalculatedWidth = safeContentWidth;
                        }
                    }
                }
            }

            // 🎯 限制列宽不超过可见区域
            // 如果列宽超过可见区域，会导致后续列位置计算错误
            float maxAllowedWidth = this.tabList.VisibleLowerClient.Width;
            if (maxAllowedWidth <= 0)
            {
                maxAllowedWidth = 800; // fallback：使用保守的默认值
            }
            else
            {
                // 单列最多占可见区域的80%，确保有空间显示其他列
                maxAllowedWidth = maxAllowedWidth * 0.8f - 60; // 减去滚动条和边距
            }
            
            // 设置绝对上下限
            if (maxAllowedWidth < 150) maxAllowedWidth = 150;
            if (maxAllowedWidth > 800) maxAllowedWidth = 800; // 硬性上限800px
            
            if (finalCalculatedWidth > maxAllowedWidth)
            {
                finalCalculatedWidth = maxAllowedWidth;
            }

            int finalWidthInt = (int)Math.Ceiling(finalCalculatedWidth);

            this.Text.Position = new Rectangle(previousRight + 1, top, finalWidthInt, this.Text.Position.Height);
            previousRight = this.Text.Position.Right + this.tabList.columnspliterWidth;

            if (this.tabList.gameObjectList != null)
            {
                for (int num2 = 0; num2 < this.ColumnTextList.Count; num2++)
                {
                    this.ColumnTextList[num2].MaxWidth = this.Text.Position.Width;
                    this.ColumnTextList[num2].Position = new Rectangle(
                        this.Text.Position.X, 
                        (this.Text.Position.Bottom + 1) + (num2 * this.tabList.rowHeight), 
                        this.Text.Position.Width, 
                        this.tabList.rowHeight);
                }
                this.ColumnTextList.ResetAllAlignedPositions();

                if (this.Editable) ResetEditableTextures();
            }
        }

        public void ResetAllTextures()
        {
            if (!this.Visible) return;
            try
            {
                if (this.Editable) ResetEditableTextures();
                else
                {
                    for (int num = 0; num < this.tabList.gameObjectList.Count; num++)
                    {
                        this.ColumnTextList[num].Text = this.GetDataString(num);
                    }
                    this.ColumnTextList.ResetAllAlignedPositions();
                }
            }
            catch { }
        }

        public void ResetEditableTextures()
        {
            if (this.Editable && this.tabList.gameObjectList != null)
            {
                try
                {
                    for (int i = 0; i < this.tabList.gameObjectList.Count; i++)
                    {
                        object val = StaticMethods.GetPropertyValue(this.tabList.gameObjectList[i], this.Name);
                        bool isSelected = (val is bool b && b);
                        this.ColumnTextList[i].TextTexture = isSelected 
                            ? (this.tabList.MultiSelecting ? this.tabList.checkboxSelectedTexture : this.tabList.roundcheckboxSelectedTexture)
                            : (this.tabList.MultiSelecting ? this.tabList.checkboxTexture : this.tabList.roundcheckboxTexture);
                    }
                    this.ColumnTextList.ResetAllAlignedPositions();
                }
                catch { }
            }
        }

        internal Rectangle DisplayPosition => this.Text.DisplayPosition;
        internal Rectangle SpliterPosition => new Rectangle(this.DisplayPosition.Right + 1, this.DisplayPosition.Y, this.tabList.columnspliterWidth, this.tabList.columnspliterHeight);
        public bool Visible => this.DetailLevel <= Session.GlobalVariables.TabListDetailLevel;
    }
}
