using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using GameObjects;
using GameManager;

namespace youcelanPlugin
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
        public int ItemID = -1;  // 🔥 添加：支持带参数的方法调用（如 TitleName(ItemID)）
        private TabListInFrame tabList;
        internal FreeText Text;
        public int Width { get; set; } // Missing Width property

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

        private static int _columnDrawCount = 0;
        
        public void Draw()
        {

            if (this.DisplayPosition.Right > this.tabList.VisibleLowerClient.Right)
            {

                if (this.DisplayPosition.Left < this.tabList.VisibleLowerClient.Right)
                {
                    CacheManager.Draw(this.tabList.rightArrowTexture, StaticMethods.LeftRectangle(this.DisplayPosition, new Rectangle(0, 0, this.tabList.rightArrowTexture.Width, this.tabList.rightArrowTexture.Height)), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.35f);
                }
            }
            else if (this.DisplayPosition.Left < this.tabList.VisibleLowerClient.Left)
            {

                if (this.DisplayPosition.Right > this.tabList.VisibleLowerClient.Left)
                {
                    CacheManager.Draw(this.tabList.leftArrowTexture, StaticMethods.RightRectangle(this.DisplayPosition, new Rectangle(0, 0, this.tabList.leftArrowTexture.Width, this.tabList.leftArrowTexture.Height)), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.35f);
                }
            }
            else
            {

                Rectangle? sourceRectangle = null;
                CacheManager.Draw(this.tabList.columnheaderTexture, this.DisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.35f);
                sourceRectangle = null;
                CacheManager.Draw(this.tabList.columnspliterTexture, this.SpliterPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.35f);
                this.Text.Draw(0.3499f);
                
                // 🔥 HOT PATH：Draw() 每帧调用，禁止任何日志输出
                // 诊断信息已移至 ReCalculate()（Cold Path）
                
                for (int i = 0; i < this.ColumnTextList.Count; i++)
                {
                    bool inBounds = (this.ColumnTextList.DisplayPosition(i).Bottom <= this.tabList.VisibleLowerClient.Bottom) && 
                                   (this.ColumnTextList.DisplayPosition(i).Top >= this.DisplayPosition.Bottom);
                    bool hasText = !String.IsNullOrEmpty(this.ColumnTextList[i].Text);
                    
                    if (inBounds && hasText)
                    {
                        if (this.Editable)
                        {
                            sourceRectangle = null;
                            var text = this.ColumnTextList[i].Text;
                            var rec = StaticMethods.CenterRectangle(this.ColumnTextList.DisplayPosition(i), new Rectangle(0, 0, this.tabList.checkboxWidth, this.tabList.checkboxWidth));
                            var pos = new Vector2(rec.X, rec.Y);
                            CacheManager.DrawString(Session.Current.Font, text, pos, Color.White, 0f, Vector2.Zero, Text.Builder.Scale, SpriteEffects.None, 0.3499f);
                        }
                        else
                        {
                            this.ColumnTextList.Draw(i, 0.3499f);
                        }
                    }
                }

            }
        }

        public object GetPropertyValue(object ClassInstance)
        {
            return StaticMethods.GetPropertyValue(ClassInstance, this.Name);
        }

        public void MoveHorizontal(int offset)
        {
            this.Text.DisplayOffset = new Point(this.Text.DisplayOffset.X + offset, this.Text.DisplayOffset.Y);
            this.ColumnTextList.DisplayOffset = new Point(this.ColumnTextList.DisplayOffset.X + offset, this.ColumnTextList.DisplayOffset.Y);
        }

        public void MoveVertical(int offset)
        {
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

        public void ReCalculate(int top, ref int previousRight)
        {
            this.Text.DisplayOffset = new Point(-4, -4);
            int width = this.Text.Width;
            if (width < this.MinWidth)
            {
                width = this.MinWidth;
            }
            this.Text.Position = new Rectangle(previousRight + 1, top, width, this.Text.Position.Height);
            previousRight = this.Text.Position.Right + this.tabList.columnspliterWidth;
            if (this.tabList.gameObjectList != null)
            {
                // 🔥 修复：每次重新计算前先清空数据，防止重复添加
                // 日期：2026-02-17
                // 说明：由于 SetSelectedTab 和 SetyoucelanContent 都会触发 ReCalculate，
                //       必须在每次重新计算前清空旧数据，确保数据不会重复显示
                

                
                this.ColumnTextList.Clear();
                
                for (int i = 0; i < this.tabList.gameObjectList.Count; i++)
                {
                    // 🔥 2026-03-01 修复：添加特殊方法处理逻辑（兼容 TabListPlugin）
                    string value = GetCellValue(i);
                    this.ColumnTextList.AddText(value);
                }
                

                
                for (int i = 0; i < this.ColumnTextList.Count; i++)
                {
                    this.ColumnTextList[i].MaxWidth = this.Text.Position.Width;
                    this.ColumnTextList[i].Position = new Rectangle(this.Text.Position.X, (this.Text.Position.Bottom + 1) + (i * this.tabList.rowHeight), this.Text.Position.Width, this.tabList.rowHeight);
                }
                this.ColumnTextList.ResetAllAlignedPositions();
                if (this.Editable)
                {
                    this.ResetEditableTextures();
                }
            }
        }

        /// <summary>
        /// 🔥 2026-03-01 新增：获取单元格值（支持带参数的特殊方法）
        /// 从 TabListPlugin/Column.cs 移植的逻辑
        /// </summary>
        private string GetCellValue(int index)
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

            // 🔥 三层忠诚度显示系统：使用 LoyaltyDisplay（根据军师智力显示误差）
            if (this.Name.Equals("Loyalty"))
            {
                // Anti-Band-Aid 合规：直接转换，如果不是 Person 则崩溃
                Person person = (Person)this.tabList.gameObjectList[index];
                
                int displayLoyalty = person.LoyaltyDisplay;
                
                // 🔥 临时诊断：在UI上显示真实值和显示值的对比
                #if DEBUG
                if (index < 3)
                {
                    int realLoyalty = person.Loyalty;
                    Person advisor = person.BelongedFaction?.Advisor;
                    int advisorInt = advisor?.BaseIntelligence ?? 0;
                    
                    // 在UI上显示：显示值(真实值)[军师智力]
                    return $"{displayLoyalty}({realLoyalty})[{advisorInt}]";
                }
                #endif
                
                return displayLoyalty.ToString();
            }

            // 🔥 临时调试：输出前 3 个武将的数据（Anti-Band-Aid 合规）
            #if DEBUG
            if (index < 3 && (this.Name.Equals("NormalStrength") || this.Name.Equals("Loyalty")))
            {
                var debugObj = this.tabList.gameObjectList[index];
                System.Console.WriteLine($"[GetCellValue] index={index}, Name={this.Name}, ObjectType={debugObj.GetType().Name}");
                
                if (debugObj is Person person)
                {
                    System.Console.WriteLine($"[GetCellValue] Person={person.Name}(ID:{person.ID})");
                    
                    if (this.Name.Equals("NormalStrength"))
                    {
                        var directValue = person.NormalStrength;
                        System.Console.WriteLine($"[GetCellValue] 直接访问 NormalStrength={directValue}");
                    }
                    else if (this.Name.Equals("Loyalty"))
                    {
                        var directValue = person.Loyalty;
                        // 业务逻辑判断：野武将的 BelongedFaction 为 null 是正常的
                        var hasFaction = person.BelongedFaction != null;
                        System.Console.WriteLine($"[GetCellValue] 直接访问 Loyalty={directValue}, HasFaction={hasFaction}");
                        if (hasFaction)
                        {
                            System.Console.WriteLine($"[GetCellValue] Faction={person.BelongedFaction.Name}");
                        }
                    }
                }
            }
            #endif

            // 🔥 默认处理：使用源生成器的属性/方法访问
            object obj = StaticMethods.GetPropertyValue(this.tabList.gameObjectList[index], this.Name);
            
            #if DEBUG
            if (index < 3 && (this.Name.Equals("NormalStrength") || this.Name.Equals("Loyalty")))
            {
                System.Console.WriteLine($"[GetCellValue] GetPropertyValue 返回: obj={obj}, Type={obj.GetType().Name}");
                System.Console.WriteLine("---");
            }
            #endif
            
            // 🔥 Anti-Band-Aid 合规：直接调用 ToString()，不掩盖 null
            return obj.ToString();
        }

        public void ResetAllTextures()
        {
            if (this.Editable)
            {
                var count = this.tabList.gameObjectList.Count;
                for (var i = 0; i < count; i++)
                {
                    var isSelected = (bool)StaticMethods.GetPropertyValue(this.tabList.gameObjectList[i], this.Name);
                    this.ColumnTextList[i].TextTexture = isSelected
                        ? (this.tabList.MultiSelecting ? this.tabList.checkboxSelectedTexture : this.tabList.roundcheckboxSelectedTexture)
                        : (this.tabList.MultiSelecting ? this.tabList.checkboxTexture : this.tabList.roundcheckboxTexture);
                }
                this.ColumnTextList.ResetAllAlignedPositions();
            }
            else
            {
                // 🔥 2026-02-17 修复信息重复显示问题（根本修复 v2）
                // 根本原因：ReCalculate() 已经负责添加文本（当 Count==0 时）
                // 解决方案：ResetAllTextures() 只负责更新现有文本内容，不添加/删除
                // 调用链路：Tab.ReCalculate() → Column.ReCalculate() (添加) → Tab.ResetAllTextures() → Column.ResetAllTextures() (更新)
                
                // 只更新现有项的文本内容
                var updateCount = Math.Min(this.ColumnTextList.Count, this.tabList.gameObjectList.Count);
                for (var i = 0; i < updateCount; i++)
                {
                    // 🔥 2026-03-01 修复：使用统一的 GetCellValue 方法
                    this.ColumnTextList[i].Text = GetCellValue(i);
                }
                
                this.ColumnTextList.ResetAllAlignedPositions();
            }
        }

        public void ResetEditableTextures()
        {
            if (this.Editable)
            {
                for (int i = 0; i < this.tabList.gameObjectList.Count; i++)
                {
                    if ((bool) StaticMethods.GetPropertyValue(this.tabList.gameObjectList[i], this.Name))
                    {
                        this.ColumnTextList[i].TextTexture = this.tabList.MultiSelecting ? this.tabList.checkboxSelectedTexture : this.tabList.roundcheckboxSelectedTexture;
                    }
                    else
                    {
                        this.ColumnTextList[i].TextTexture = this.tabList.MultiSelecting ? this.tabList.checkboxTexture : this.tabList.roundcheckboxTexture;
                    }
                }
                this.ColumnTextList.ResetAllAlignedPositions();
            }
        }

        internal Rectangle DisplayPosition
        {
            get
            {
                return this.Text.DisplayPosition;
            }
        }

        internal Rectangle SpliterPosition
        {
            get
            {
                return new Rectangle(this.DisplayPosition.Right + 1, this.DisplayPosition.Y, this.tabList.columnspliterWidth, this.tabList.columnspliterHeight);
            }
        }
    }
}

