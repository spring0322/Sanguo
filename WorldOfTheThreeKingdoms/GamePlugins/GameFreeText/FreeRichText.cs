using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameFreeText
{
    public class FreeRichText
    {
        public Font Builder = new Font();
        public int ClientHeight;
        public int ClientWidth;

        // 核心排版变量
        private int currentPageIndex;
        private int currentRow;

        // 颜色配置
        public Color DefaultColor;
        public Color TitleColor;
        public Color SubTitleColor;
        public Color SubTitleColor2;
        public Color SubTitleColor3;
        public Color PositiveColor;
        public Color NegativeColor;

        public Point DisplayOffset;
        public bool MultiPage;
        public bool OnePage;
        private List<int> PageIndexs;
        public int RowMargin;
        public List<SimpleText> Texts;

        // 【新增】全局修正系数
        // 如果所有字都挤，把这个改大，比如 1.1f (加宽10%)
        // 如果所有字都太散，把这个改小，比如 0.9f
        public float CharSpacing = 1.1f;

        // 行距系数
        public float LineSpacing = 1.0f;

        public FreeRichText()
        {
            this.Texts = new List<SimpleText>();
            this.PageIndexs = new List<int>();
            this.ClientWidth = 200;
            this.ClientHeight = 600;
            this.RowMargin = 10;
        }

        public void Clear()
        {
            this.Texts.Clear();
            this.PageIndexs.Clear();
            this.currentRow = 0;
            this.currentPageIndex = 0;
            this.MultiPage = false;
        }

        // ==========================================
        // 核心修复区域 1：计算行高
        // ==========================================
        public int RowHeight
        {
            get
            {
                float scale = this.Builder?.Scale ?? 1.0f;
                float baseHeight = 24f; // 兜底默认值

                if (Session.Current?.Font != null)
                {
                    try
                    {
                        // 获取标准汉字高度 ("测"字通常能代表平均高度)
                        baseHeight = Session.Current.Font.MeasureString("测").Y;
                    }
                    catch { }
                }

                // 计算最终高度：基准高度 * 缩放 * 行距系数 + 额外边距
                return (int)(baseHeight * scale * this.LineSpacing) + this.RowMargin;
            }
        }

        // ==========================================
        // 核心修复区域 2：计算字宽 (暴力纠错版)
        // ==========================================
        public int GetTextWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            float scale = this.Builder?.Scale ?? 1.0f;
            float baseHeight = 24f;
            float measuredWidth = 0f;

            // 1. 获取字体的基础测量数据
            if (Session.Current?.Font != null)
            {
                try
                {
                    Vector2 size = Session.Current.Font.MeasureString(text);
                    measuredWidth = size.X;
                    baseHeight = Session.Current.Font.MeasureString("测").Y; // 获取单字标准高度
                }
                catch
                {
                    measuredWidth = 20f * text.Length;
                }
            }
            else
            {
                measuredWidth = 20f * text.Length;
            }

            // 2. 【核心修复逻辑】方块字强制对齐
            // 汉字是方块字，宽度通常等于高度。但对于英文字母、数字和符号，宽度远小于高度
            int blockCharCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] > 255) blockCharCount++;
            }

            float finalBaseWidth = measuredWidth;
            if (blockCharCount > 0)
            {
                float theoreticalSquareWidth = blockCharCount * baseHeight + (text.Length - blockCharCount) * (baseHeight * 0.5f);
                finalBaseWidth = Math.Max(measuredWidth, theoreticalSquareWidth);
            }

            // 3. 应用缩放和间距系数
            // 使用 Math.Ceiling 向上取整，宁可宽一点也不要叠起来
            return (int)Math.Ceiling(finalBaseWidth * scale * this.CharSpacing);
        }

        // ==========================================
        // 核心排版逻辑 (BuildRow)
        // ==========================================
        private void BuildRow(int index)
        {
            if (this.ClientWidth < 10) this.ClientWidth = 200;

            int currentX = 0;
            this.currentRow = 0;
            int rh = this.RowHeight;

            for (int i = 0; i < this.Texts.Count; i++)
            {
                SimpleText item = this.Texts[i];

                if (item.NewLine)
                {
                    this.currentRow++;
                    currentX = 0;
                    item.Row = this.currentRow;
                    item.TextPosition = new Rectangle(0, item.Row * rh, 0, 0);
                    continue;
                }

                int w = GetTextWidth(item.Text);

                // --- 核心修复：自动折行与分割 ---
                if (currentX + w > this.ClientWidth)
                {
                    int spaceLeft = this.ClientWidth - currentX;
                    
                    // 如果剩余空间太小（小于20像素，约一个字宽），先换行
                    if (currentX > 0 && spaceLeft < 20)
                    {
                        this.currentRow++;
                        currentX = 0;
                        spaceLeft = this.ClientWidth;
                    }

                    // 换行后如果还放不下，或者决定就在当前行拆分
                    if (currentX + w > this.ClientWidth) // 重新检查
                    {
                        // 计算能放下多少个字符
                        // 暴力寻找拆分点
                        int fitCount = 0;
                        int currentW = 0;
                        for (int k = 1; k <= item.Text.Length; k++)
                        {
                            string sub = item.Text.Substring(0, k);
                            int subW = GetTextWidth(sub);
                            if (currentX + subW > this.ClientWidth)
                            {
                                break;
                            }
                            fitCount = k;
                            currentW = subW;
                        }

                        if (fitCount > 0 && fitCount < item.Text.Length)
                        {
                            // 执行拆分
                            string part1 = item.Text.Substring(0, fitCount);
                            string part2 = item.Text.Substring(fitCount);

                            // 修改当前项
                            item.Text = part1;
                            w = currentW; // 更新宽度

                            // 插入剩余部分作为新项
                            SimpleText newItem = new SimpleText();
                            newItem.Text = part2;
                            newItem.TextColor = item.TextColor;
                            newItem.Builder = item.Builder;
                            this.Texts.Insert(i + 1, newItem);
                        }
                        else if (fitCount == 0 && currentX > 0)
                        {
                            // 连一个字都放不下，强制换行（不应该发生，因为前面已经判断过 spaceLeft < 20）
                            this.currentRow++;
                            currentX = 0;
                            i--; // 重新处理该项
                            continue;
                        }
                    }
                }
                // -----------------------------

                // 写入坐标
                item.Row = this.currentRow;
                item.TextPosition = new Rectangle(currentX, item.Row * rh, w, rh);

                currentX += w;

                // 分页判断
                if ((this.currentRow + 1) * rh > this.ClientHeight)
                {
                    this.MultiPage = true;
                    if (!this.PageIndexs.Contains(i + 1))
                    {
                        this.PageIndexs.Add(i + 1);
                    }
                    this.currentRow = 0;
                    currentX = 0;
                }
            }
        }

        public void ResortTexts()
        {
            if (this.Texts.Count == 0) return;

            this.MultiPage = false;
            this.currentPageIndex = 0;
            this.PageIndexs.Clear();
            this.PageIndexs.Add(0);

            foreach (var text in this.Texts)
            {
                if (text.Text == @"\n") text.NewLine = true;
            }

            this.BuildRow(0);

            if (this.OnePage && (this.PageIndexs.Count > 1))
            {
                this.Texts.RemoveRange(this.PageIndexs[1], this.Texts.Count - this.PageIndexs[1]);
            }
        }

        // ==========================================
        // Draw 方法
        // ==========================================
        public void Draw(float Depth)
        {
            if (this.Texts.Count == 0) return;

            int start = 0;
            int end = this.Texts.Count;

            if (this.PageIndexs.Count > 0 && this.currentPageIndex < this.PageIndexs.Count)
            {
                start = this.PageIndexs[this.currentPageIndex];
                if (this.currentPageIndex + 1 < this.PageIndexs.Count)
                {
                    end = this.PageIndexs[this.currentPageIndex + 1];
                }
            }

            for (int i = start; i < end; i++)
            {
                if (!this.Texts[i].NewLine && !string.IsNullOrEmpty(this.Texts[i].Text))
                {
                    // 使用 TextPosition
                    Vector2 pos = new Vector2(
                        this.Texts[i].TextPosition.X + this.DisplayOffset.X,
                        this.Texts[i].TextPosition.Y + this.DisplayOffset.Y
                    );

                    CacheManager.DrawString(
                        Session.Current.Font,
                        this.Texts[i].Text,
                        pos,
                        this.TextDisplayColor(i),
                        0f,
                        Vector2.Zero,
                        this.Builder.Scale,
                        SpriteEffects.None,
                        Depth
                    );
                }
            }
        }

        // ==========================================
        // 辅助方法 (保持不变)
        // ==========================================
        public void AddGameObjectTextBranch(GameObject gameObject, GameObjectTextBranch branch)
        {
            if ((branch != null) && (branch.Leaves.Count != 0))
            {
                foreach (GameObjectTextLeaf leaf in branch.Leaves)
                {
                    if ((gameObject != null) && (leaf.Property != ""))
                    {
                        this.AddText(StaticMethods.GetPropertyValue(gameObject, leaf.Property).ToString(), leaf.TextColor);
                    }
                    else
                    {
                        this.AddText(leaf.Text, leaf.TextColor);
                    }
                }
                this.ResortTexts();
            }
        }

        public void SetGameObjectTextBranch(GameObject gameObject, GameObjectTextBranch branch)
        {
            if ((branch != null) && (branch.Leaves.Count != 0))
            {
                this.Clear();
                foreach (GameObjectTextLeaf leaf in branch.Leaves)
                {
                    if ((gameObject != null) && (leaf.Property != ""))
                    {
                        this.AddText(StaticMethods.GetPropertyValue(gameObject, leaf.Property).ToString(), leaf.TextColor);
                    }
                    else
                    {
                        this.AddText(leaf.Text, leaf.TextColor);
                    }
                }
                this.ResortTexts();
            }
        }

        public int TopAddGameObjectTextBranch(GameObject gameObject, GameObjectTextBranch branch)
        {
            if (branch != null)
            {
                if (branch.Leaves.Count == 0) return 0;

                this.AddNewLine(0);
                for (int i = branch.Leaves.Count - 1; i >= 0; i--)
                {
                    GameObjectTextLeaf leaf = branch.Leaves[i];
                    if ((gameObject != null) && (leaf.Property != ""))
                    {
                        this.AddText(0, StaticMethods.GetPropertyValue(gameObject, leaf.Property).ToString(), leaf.TextColor);
                    }
                    else
                    {
                        this.AddText(0, leaf.Text, leaf.TextColor);
                    }
                }
                this.ResortTexts();
                return this.RowHeight;
            }
            return 0;
        }

        public void AddNewLine()
        {
            SimpleText item = new SimpleText
            {
                Text = @"\n",
                Builder = Builder,
                NewLine = true
            };
            this.Texts.Add(item);
        }

        public void AddNewLine(int pos)
        {
            SimpleText item = new SimpleText
            {
                Text = @"\n",
                Builder = Builder,
                NewLine = true
            };
            this.Texts.Insert(pos, item);
        }

        public void AddText(string text)
        {
            string[] parts = text.Split(new string[] { "\\n" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    this.Texts.Add(new SimpleText { Text = @"\n", Builder = Builder, NewLine = true });
                }
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    this.Texts.Add(new SimpleText { Text = parts[i], Builder = Builder });
                }
            }
        }

        public void AddText(string text, Color color)
        {
            string[] parts = text.Split(new string[] { "\\n" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    this.Texts.Add(new SimpleText { Text = @"\n", Builder = Builder, NewLine = true });
                }
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    this.Texts.Add(new SimpleText { Text = parts[i], TextColor = color, Builder = Builder });
                }
            }
        }

        public void AddText(int pos, string text, Color color)
        {
            string[] parts = text.Split(new string[] { "\\n" }, StringSplitOptions.None);
            int currentPos = pos;
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    this.Texts.Insert(currentPos++, new SimpleText { Text = @"\n", Builder = Builder, NewLine = true });
                }
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    this.Texts.Insert(currentPos++, new SimpleText { Text = parts[i], TextColor = color, Builder = Builder });
                }
            }
        }

        public void FirstPage()
        {
            this.currentPageIndex = 0;
        }

        public void NextPage()
        {
            if (this.currentPageIndex < (this.PageIndexs.Count - 1))
            {
                this.currentPageIndex++;
            }
        }

        private Color TextDisplayColor(int index)
        {
            return ((this.Texts[index].TextColor == new Color()) ? this.DefaultColor : this.Texts[index].TextColor);
        }

        private Rectangle TextDisplayPosition(int index)
        {
            return new Rectangle(this.Texts[index].TextPosition.X + this.DisplayOffset.X, this.Texts[index].TextPosition.Y + this.DisplayOffset.Y, this.Texts[index].TextPosition.Width, this.Texts[index].TextPosition.Height);
        }

        private int CurrentPageEndIndex
        {
            get
            {
                return ((this.PageIndexs.Count > (this.currentPageIndex + 1)) ? this.PageIndexs[this.currentPageIndex + 1] : this.Texts.Count);
            }
        }

        public int CurrentPageIndex
        {
            get { return this.currentPageIndex; }
        }

        public int PageCount
        {
            get { return this.PageIndexs.Count; }
        }

        public int RealHeight
        {
            get
            {
                if (this.Texts.Count > 0)
                {
                    int lastRow = 0;
                    for (int i = 0; i < Texts.Count; i++)
                    {
                        if (Texts[i].Row > lastRow) lastRow = Texts[i].Row;
                    }
                    return (lastRow + 1) * this.RowHeight;
                }
                return this.ClientHeight;
            }
        }
    }
}