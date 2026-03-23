namespace PersonDetailPlugin
{
    using GameFreeText;
    using WorldOfTheThreeKingdoms.GameGlobal;
    using GameManager;
    using GameObjects;
    using GameObjects.Conditions;
    using GameObjects.Influences;
    using GameObjects.PersonDetail;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;

    internal class PersonDetail
    {
        // 🛡️ 递归保护标志：防止在人员调配期间访问属性导致栈溢出
        [ThreadStatic]
        private static bool _isAccessingPersonProperties = false;
        internal FreeTextList AllSkillTexts;
        internal Point BackgroundSize;
        internal PlatformTexture BackgroundTexture;
        internal Rectangle BiographyClient;
        internal FreeRichText BiographyText = new FreeRichText();
        internal FreeText CalledNameText;
        internal Rectangle ConditionClient;
        internal FreeRichText ConditionText = new FreeRichText();
        private object current;
        private Point DisplayOffset;
        internal FreeText GivenNameText;
        internal Rectangle InfluenceClient;
        internal FreeRichText InfluenceText = new FreeRichText();
        private bool isShowing;
        internal List<LabelText> LabelTexts = new List<LabelText>();
        internal FreeTextList LearnableSkillTexts;
        internal List<Skill> LinkedSkills = new List<Skill>();
        internal Rectangle TitleClient;
        // internal Rectangle GuanzhiClient; //官职
        //internal FreeRichText GuanzhiText = new FreeRichText();
        internal FreeRichText TitleText = new FreeRichText();
        internal FreeTextList PersonSkillTexts;
        internal Rectangle PortraitClient;
        internal Screen screen;
        internal Person ShowingPerson;
        internal Point SkillBlockSize;
        internal Point SkillDisplayOffset;
        internal Rectangle StuntClient;
        internal FreeRichText StuntText = new FreeRichText();
        internal FreeText SurNameText;
        
        // 🔥 宝物显示区域（12个宝物分组）
        internal Dictionary<int, Rectangle> TreasureClients = new Dictionary<int, Rectangle>();
        
        // 🔥 诊断标志：避免每帧重复输出日志
        #if DEBUG
        private static bool _treasureDiagnosticLogged = false;
        #endif


        internal void Draw()
        {
            try
            {
                if (this.ShowingPerson != null)
                {
                    Rectangle? sourceRectangle = null;
                    if (this.BackgroundTexture != null)
                    {
                        CacheManager.Draw(this.BackgroundTexture, this.BackgroundDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);
                    }
                    try
                    {
                        if (this.ShowingPerson != null)
                        {
                            CacheManager.DrawZhsanAvatar(this.ShowingPerson, this.PortraitDisplayPosition, 0.199f, PortraitSize.Medium);
                        }
                    }
                    catch
                    {
                    }
                    if (this.SurNameText != null) this.SurNameText.Draw(0.1999f);
                    if (this.GivenNameText != null) this.GivenNameText.Draw(0.1999f);
                    if (this.CalledNameText != null) this.CalledNameText.Draw(0.1999f);
                    if (this.LabelTexts != null)
                    {
                        foreach (LabelText text in this.LabelTexts)
                        {
                            if (text != null && text.Label != null) text.Label.Draw(0.1999f);
                            if (text != null && text.Text != null) text.Text.Draw(0.1999f);
                        }
                    }
                    if (this.TitleText != null) this.TitleText.Draw(0.1999f);
                    //this.GuanzhiText.Draw(spriteBatch, 0.1999f);
                    if (this.AllSkillTexts != null) this.AllSkillTexts.Draw((float)0.1999f);
                    if (this.PersonSkillTexts != null) this.PersonSkillTexts.Draw((float)0.1998f);
                    if (this.LearnableSkillTexts != null) this.LearnableSkillTexts.Draw((float)0.1998f);
                    if (this.StuntText != null) this.StuntText.Draw(0.1999f);
                    if (this.InfluenceText != null) this.InfluenceText.Draw(0.1999f);
                    if (this.ConditionText != null) this.ConditionText.Draw(0.1999f);
                    if (this.BiographyText != null) this.BiographyText.Draw(0.1999f);
                    
                    // 🔥 绘制宝物图标
                    DrawTreasures();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonDetail] Draw Error: {ex.ToString()}");
            }
        }
        
        /// <summary>
        /// 绘制宝物图标
        /// 🔥 2026-03-18 新增：显示人物拥有的宝物
        /// 性能：Draw() 是 Hot Path，但宝物数量有限（最多12个），循环开销可接受
        /// </summary>
        private void DrawTreasures()
        {
            #if DEBUG
            // 🔥 诊断：只在首次调用时输出日志，避免每帧重复输出
            if (!_treasureDiagnosticLogged)
            {
                _treasureDiagnosticLogged = true;
                
                System.Diagnostics.Debug.WriteLine($"[DrawTreasures] 首次调用诊断");
                System.Diagnostics.Debug.WriteLine($"  - ShowingPerson: {(this.ShowingPerson != null ? $"{this.ShowingPerson.ID} ({this.ShowingPerson.Name})" : "null")}");
                System.Diagnostics.Debug.WriteLine($"  - Treasures: {(this.ShowingPerson?.Treasures != null ? "存在" : "null")}");
                System.Diagnostics.Debug.WriteLine($"  - Treasures.Count: {this.ShowingPerson?.Treasures?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"  - TreasureIDs: {(this.ShowingPerson?.TreasureIDs != null ? $"Count={this.ShowingPerson.TreasureIDs.Count}, [{string.Join(", ", this.ShowingPerson.TreasureIDs)}]" : "null")}");
                System.Diagnostics.Debug.WriteLine($"  - TreasureClients.Count: {TreasureClients.Count}");
                
                if (this.ShowingPerson?.Treasures != null && this.ShowingPerson.Treasures.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  - 宝物列表:");
                    
                    foreach (Treasure t in this.ShowingPerson.Treasures)
                    {
                        System.Diagnostics.Debug.WriteLine($"    - Treasure {t.ID} ({t.Name}), Group={t.TreasureGroup}, Picture={(t.Picture != null ? "存在" : "null")}");
                    }
                }
                else if (this.ShowingPerson?.Treasures != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - ⚠️ Treasures.Count = 0");
                    
                    // 检查是否是数据链接问题
                    if (this.ShowingPerson.TreasureIDs != null && this.ShowingPerson.TreasureIDs.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - ⚠️ 数据链接失败：TreasureIDs 有 {this.ShowingPerson.TreasureIDs.Count} 个值，但 Treasures 为空");
                        System.Diagnostics.Debug.WriteLine($"  - ⚠️ 这说明 LinkTreasures() 没有正确执行或查找表为空");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  - ⚠️ 数据源问题：TreasureIDs 本身就是空的");
                        System.Diagnostics.Debug.WriteLine($"  - ⚠️ 这说明序列化时没有保存 TreasureIDs，或反序列化时没有加载");
                    }
                }
            }
            #endif
            
            if (this.ShowingPerson == null || this.ShowingPerson.Treasures == null)
                return;
                
            // 宝物分组列表（对应XML中的12个区域）
            int[] treasureGroups = [10, 15, 20, 25, 30, 40, 50, 55, 60, 70, 90, 100];
            
            // 🔥 Hot Path：使用 for 循环而不是 foreach（虽然数组很小）
            for (int i = 0; i < treasureGroups.Length; i++)
            {
                int groupId = treasureGroups[i];
                
                if (!TreasureClients.ContainsKey(groupId))
                    continue;
                    
                // 检查该分组是否有宝物（正确的方法名是 HasTreasureforGroup）
                if (!this.ShowingPerson.HasTreasureforGroup(groupId))
                    continue;
                    
                // 获取该分组中价值最高的宝物纹理
                var treasureTexture = this.ShowingPerson.TreasurePictureforGroup(groupId);
                if (treasureTexture == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  ⚠️ 警告：Group {groupId} 的宝物纹理为 null");
                    #endif
                    continue;
                }
                    
                // 绘制宝物图标
                Rectangle destRect = TreasureClients[groupId];
                destRect.X += this.DisplayOffset.X;
                destRect.Y += this.DisplayOffset.Y;
                
                CacheManager.Draw(treasureTexture, destRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.1998f);
            }
        }

        internal void Initialize(Screen screen)
        {
            this.screen = screen;
        }

        private void screen_OnMouseLeftUp(Point position)
        {
            if (StaticMethods.PointInRectangle(position, new Rectangle(this.BiographyText.DisplayOffset.X, this.BiographyText.DisplayOffset.Y, this.BiographyClient.Width, this.BiographyClient.Height)))
            {
                if (this.BiographyText.CurrentPageIndex < (this.BiographyText.PageCount - 1))
                {
                    this.BiographyText.NextPage();
                }
                else if (this.BiographyText.CurrentPageIndex == (this.BiographyText.PageCount - 1))
                {
                    this.BiographyText.FirstPage();
                }
            }
        }

        private void screen_OnMouseMove(Point position, bool leftDown)
        {
            // 🛡️ 递归保护：防止在人员调配期间访问属性导致栈溢出
            if (_isAccessingPersonProperties)
            {
                return;
            }

            try
            {
                _isAccessingPersonProperties = true;

                bool flag = false;
            if (!flag && StaticMethods.PointInRectangle(position, this.TitleDisplayPosition) && this.TitleText.RowHeight > 0)
            {
                int num2 = (position.Y - this.TitleText.DisplayOffset.Y) / this.TitleText.RowHeight;
                if (num2 >= 0)
                {
                    int num3 = num2;
                    if (this.ShowingPerson.Titles.Count > num3)
                    {
                        Title title = this.ShowingPerson.Titles[num3] as Title;
                        if (title != null)
                        {
                            if (this.current != title)
                            {
                                this.BiographyText.Clear();
                                this.InfluenceText.Clear();
                                //阿柒:增加根据称号等级设定不同字体颜色
                                Color titleColor = Color.White;
                                if (title.Level < 4)
                                {
                                    titleColor = Color.AliceBlue;
                                }
                                else if (title.Level >= 4 && title.Level < 7)
                                {
                                    titleColor = Color.YellowGreen;
                                }
                                else if (title.Level >= 7 && title.Level < 10)
                                {
                                    titleColor = Color.LightSkyBlue;
                                }
                                else if (title.Level >= 10 && title.Level < 13)
                                {
                                    titleColor = Color.Violet;
                                }
                                else
                                {
                                    titleColor = Color.Orange;
                                }
                                this.InfluenceText.AddText(title.DetailedName, titleColor);
                                this.InfluenceText.AddNewLine();
                                foreach (Influence influence in title.Influences.Influences.Values)
                                {
                                    //阿柒:根据影响种类设定不同颜色
                                    if (influence.Kind.ID == 280 || influence.Kind.ID == 281 || influence.Kind.ID == 285 || influence.Kind.ID == 290 || influence.Kind.ID == 300)
                                    {
                                        this.InfluenceText.AddText(influence.Description, Color.Moccasin);
                                    }
                                    else
                                    {
                                        this.InfluenceText.AddText(influence.Description);
                                    }

                                    this.InfluenceText.AddNewLine();
                                }
                                this.InfluenceText.ResortTexts();
                                this.ConditionText.Clear();
                                if (title.FundForHolder > 0)
                                {
                                    this.ConditionText.AddText("薪金：" + title.FundForHolder + "/月\n");
                                }
                                this.ConditionText.AddText("修习条件", this.ConditionText.TitleColor);
                                this.ConditionText.AddNewLine();
                                foreach (Condition condition in title.Conditions.Conditions.Values)
                                {
                                    if (condition.CheckCondition(this.ShowingPerson))
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                    }
                                    else
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                    }
                                    this.ConditionText.AddNewLine();
                                }
                                foreach (Condition condition in title.ArchitectureConditions.Conditions.Values)
                                {
                                    if (this.ShowingPerson.LocationArchitecture != null && condition.CheckCondition(this.ShowingPerson.LocationArchitecture))
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                    }
                                    else
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                    }
                                    this.ConditionText.AddNewLine();
                                }
                                foreach (Condition condition in title.FactionConditions.Conditions.Values)
                                {
                                    if (this.ShowingPerson.BelongedFaction != null && condition.CheckCondition(this.ShowingPerson.BelongedFaction))
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                    }
                                    else
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                    }
                                    this.ConditionText.AddNewLine();
                                }

                                this.ConditionText.ResortTexts();
                                this.current = title;
                            }
                            flag = true;
                        }
                    }
                }
            }
            /* if (!flag && StaticMethods.PointInRectangle(position, this.GuanzhiDisplayPosition))
             {
                 int num2 = (position.Y - this.GuanzhiText.DisplayOffset.Y / this.GuanzhiText.RowHeight);
                 if (num2 > 1)
                 {
                     int num3 = num2 - 2;
                     if (this.ShowingPerson.Guanzhis.Count > num3)
                     {
                         Guanzhi guanzhi = this.ShowingPerson.Guanzhis[num3] as Guanzhi;
                         if (guanzhi != null)
                         {
                             if (this.current != guanzhi)
                             {
                                 this.BiographyText.Clear();
                                 this.InfluenceText.Clear();
                                 this.InfluenceText.AddText(guanzhi.DetailedName, this.InfluenceText.TitleColor);
                                 this.InfluenceText.AddNewLine();
                                 foreach (Influence influence in guanzhi.Influences.Influences.Values)
                                 {
                                     this.InfluenceText.AddText(influence.Description);
                                     this.InfluenceText.AddNewLine();
                                 }
                                 this.InfluenceText.ResortTexts();
                                 this.ConditionText.Clear();
                                 this.ConditionText.AddText("授予条件", this.ConditionText.TitleColor);
                                 this.ConditionText.AddNewLine();
                                 foreach (Condition condition in guanzhi.Conditions.Conditions.Values)
                                 {
                                     if (condition.CheckCondition(this.ShowingPerson))
                                     {
                                         this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                     }
                                     else
                                     {
                                         this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                     }
                                     this.ConditionText.AddNewLine();
                                 }
                                 foreach (Condition condition in guanzhi.LoseConditions.Conditions.Values)
                                 {
                                     if (condition.CheckCondition(this.ShowingPerson))
                                     {
                                         this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                     }
                                     else
                                     {
                                         this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                     }
                                     this.ConditionText.AddNewLine();
                                 }
                                 foreach (Condition condition in guanzhi.FactionConditions.Conditions.Values)
                                 {
                                     if (this.ShowingPerson.BelongedFaction != null && condition.CheckCondition(this.ShowingPerson.BelongedFaction))
                                     {
                                         this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                     }
                                     else
                                     {
                                         this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                     }
                                     this.ConditionText.AddNewLine();
                                 }

                                 this.ConditionText.ResortTexts();
                                 this.current = guanzhi;
                             }
                             flag = true;
                         }
                     }
                 }
             }*/

            if (!flag && StaticMethods.PointInRectangle(position, this.StuntDisplayPosition) && this.StuntText.RowHeight > 0)
            {
                int num2 = (position.Y - this.StuntText.DisplayOffset.Y) / this.StuntText.RowHeight;
                if (num2 > -1)
                {
                    int num3 = num2;
                    if (this.ShowingPerson.Stunts.Count > num3)
                    {
                        Stunt stunt = this.ShowingPerson.Stunts.GetStuntList()[num3] as Stunt;
                        if (stunt != null)
                        {
                            if (this.current != stunt)
                            {
                                this.BiographyText.Clear();
                                this.InfluenceText.Clear();
                                this.InfluenceText.AddText("战斗特技", this.InfluenceText.TitleColor);
                                this.InfluenceText.AddText(stunt.Name, this.InfluenceText.SubTitleColor);
                                this.InfluenceText.AddNewLine();
                                this.InfluenceText.AddText("持续天数", this.InfluenceText.SubTitleColor2);
                                this.InfluenceText.AddText((stunt.Period * Session.Parameters.DayInTurn).ToString(), this.InfluenceText.SubTitleColor3);
                                this.InfluenceText.AddText("天", this.InfluenceText.SubTitleColor2);
                                this.InfluenceText.AddNewLine();
                                foreach (Influence influence in stunt.Influences.Influences.Values)
                                {
                                    this.InfluenceText.AddText(influence.Description);
                                    this.InfluenceText.AddNewLine();
                                }
                                this.InfluenceText.ResortTexts();
                                this.ConditionText.Clear();
                                this.ConditionText.AddText("使用条件", this.ConditionText.TitleColor);
                                this.ConditionText.AddNewLine();
                                if ((this.ShowingPerson.LocationTroop != null) && (this.ShowingPerson == this.ShowingPerson.LocationTroop.Leader))
                                {
                                    foreach (Condition condition in stunt.CastConditions.Conditions.Values)
                                    {
                                        if (condition.CheckCondition(this.ShowingPerson.LocationTroop))
                                        {
                                            this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                        }
                                        else
                                        {
                                            this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                        }
                                        this.ConditionText.AddNewLine();
                                    }
                                }
                                else
                                {
                                    foreach (Condition condition in stunt.CastConditions.Conditions.Values)
                                    {
                                        this.ConditionText.AddText(condition.Name);
                                        this.ConditionText.AddNewLine();
                                    }
                                }
                                this.ConditionText.AddNewLine();
                                this.ConditionText.AddText("修习条件", this.ConditionText.SubTitleColor);
                                this.ConditionText.AddNewLine();
                                foreach (Condition condition in stunt.LearnConditions.Conditions.Values)
                                {
                                    if (condition.CheckCondition(this.ShowingPerson))
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                    }
                                    else
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                    }
                                    this.ConditionText.AddNewLine();
                                }
                                this.ConditionText.ResortTexts();
                                this.current = stunt;
                            }
                            flag = true;
                        }
                    }
                }
            }
            if (!flag)
            {
                for (int i = 0; i < this.AllSkillTexts.Count; i++)
                {
                    if (StaticMethods.PointInRectangle(position, this.AllSkillTexts[i].AlignedPosition))
                    {
                        if (this.current != this.LinkedSkills[i])
                        {
                            this.BiographyText.Clear();
                            this.InfluenceText.Clear();
                            if (this.LinkedSkills[i].InfluenceCount > 0)
                            {
                                this.InfluenceText.AddText("技能", this.InfluenceText.TitleColor);
                                this.InfluenceText.AddText(this.LinkedSkills[i].Name, this.InfluenceText.SubTitleColor);
                                this.InfluenceText.AddNewLine();
                                foreach (Influence influence in this.LinkedSkills[i].Influences.Influences.Values)
                                {
                                    //阿柒:根据影响种类设定不同颜色
                                    if (influence.Kind.ID == 280 || influence.Kind.ID == 281 || influence.Kind.ID == 285 || influence.Kind.ID == 290 || influence.Kind.ID == 300)
                                    {
                                        this.InfluenceText.AddText(influence.Description, Color.Moccasin);
                                    }
                                    else
                                    {
                                        this.InfluenceText.AddText(influence.Description);
                                    }
                                    this.InfluenceText.AddNewLine();
                                }
                                this.InfluenceText.ResortTexts();
                                this.ConditionText.Clear();
                                this.ConditionText.AddText("修习条件", this.ConditionText.TitleColor);
                                this.ConditionText.AddNewLine();
                                foreach (Condition condition in this.LinkedSkills[i].Conditions.Conditions.Values)
                                {
                                    if (condition.CheckCondition(this.ShowingPerson))
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.PositiveColor);
                                    }
                                    else
                                    {
                                        this.ConditionText.AddText(condition.Name, this.ConditionText.NegativeColor);
                                    }
                                    this.ConditionText.AddNewLine();
                                }
                                this.ConditionText.ResortTexts();
                            }
                            this.current = this.LinkedSkills[i];
                        }
                        flag = true;
                        break;
                    }
                }
            }
            if (!flag)
            {
                if (this.current != null)
                {
                    this.current = null;
                    this.InfluenceText.Clear();
                    this.ConditionText.Clear();
                    if (this.ShowingPerson.PersonBiography != null)
                    {
                        this.BiographyText.Clear();
                        this.BiographyText.AddText("列传", this.BiographyText.TitleColor);
                        this.BiographyText.AddNewLine();
                        this.BiographyText.AddText(this.ShowingPerson.PersonBiography.Brief);
                        this.BiographyText.AddNewLine();
                        this.BiographyText.AddText("演义", this.BiographyText.SubTitleColor);
                        this.BiographyText.AddNewLine();
                        this.BiographyText.AddText(this.ShowingPerson.PersonBiography.Romance);
                        this.BiographyText.AddNewLine();
                        this.BiographyText.AddText("历史", this.BiographyText.SubTitleColor2);
                        this.BiographyText.AddNewLine();
                        this.BiographyText.AddText(this.ShowingPerson.PersonBiography.History);
                        this.BiographyText.AddNewLine();
                        this.BiographyText.AddText("剧本", this.BiographyText.SubTitleColor2);
                        this.BiographyText.AddText("：");
                        String[] lineBrokenText = ShowingPerson.PersonBiography.InGame.Split('\n');
                        foreach (String s in lineBrokenText)
                        {
                            this.BiographyText.AddText(s);
                            this.BiographyText.AddNewLine();
                        }
                        this.BiographyText.ResortTexts();
                    }
                }
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonDetail] screen_OnMouseMove Error: {ex.ToString()}");
            }
            finally
            {
                _isAccessingPersonProperties = false;
            }
        }

        private void screen_OnMouseRightUp(Point position)
        {
            this.IsShowing = false;
        }

        internal void SetPerson(Person person)
        {
            #if DEBUG
            // 🔥 关键诊断：追踪 SetPerson 调用时机
            System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] ========== 开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] Person: {person?.Name}(ID:{person?.ID})");
            System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] person.Skills: {(person?.Skills == null ? "null" : "已初始化")}");
            if (person?.Skills != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] person.Skills.Skills: {(person.Skills.Skills == null ? "null" : $"Count={person.Skills.Skills.Count}")}");
                if (person.Skills.Skills != null && person.Skills.Skills.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] person.Skills.Skills.Keys: [{string.Join(", ", person.Skills.Skills.Keys)}]");
                }
            }
            #endif
            
            // 🛡️ 递归保护：防止在人员调配期间访问属性导致栈溢出
            if (_isAccessingPersonProperties)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PersonDetail] 递归保护：跳过SetPerson（正在访问人物属性）");
#endif
                return;
            }

            try
            {
                _isAccessingPersonProperties = true;

                foreach (Skill skill in Session.Current.Scenario.GameCommonData.AllSkills.Skills.Values)
                {
                    Rectangle position = new Rectangle(this.SkillDisplayOffset.X + (skill.DisplayCol * this.SkillBlockSize.X), this.SkillDisplayOffset.Y + (skill.DisplayRow * this.SkillBlockSize.Y), this.SkillBlockSize.X, this.SkillBlockSize.Y);
                    this.AllSkillTexts.AddText(skill.Name, position);
                    this.LinkedSkills.Add(skill);
                }
                this.AllSkillTexts.ResetAllAlignedPositions();

                this.ShowingPerson = person;
            this.SurNameText.Text = person.SurName;
            this.GivenNameText.Text = person.GivenName;
            this.CalledNameText.Text = person.CalledName;
            foreach (LabelText text in this.LabelTexts)
            {
                text.Text.Text = StaticMethods.GetPropertyValue(person, text.PropertyName).ToString();
            }
            this.TitleText.Clear();
            foreach (Title title in person.Titles)
            {
                if (title != null)
                {
                    //阿柒:根据称号等级设定不同颜色
                    if (title.Level < 4)
                    {
                        this.TitleText.AddText("  " + title.DetailedName, Color.AliceBlue);
                    }
                    else if (title.Level >= 4 && title.Level < 7)
                    {
                        this.TitleText.AddText("  " + title.DetailedName, Color.YellowGreen);
                    }
                    else if (title.Level >= 7 && title.Level < 10)
                    {
                        this.TitleText.AddText("  " + title.DetailedName, Color.LightSkyBlue);
                    }
                    else if (title.Level >= 10 && title.Level < 13)
                    {
                        this.TitleText.AddText(title.DetailedName, Color.Violet);
                    }
                    else
                    {
                        this.TitleText.AddText(title.DetailedName, Color.Orange);
                    }

                }
                //this.TitleText.AddText(title.DetailedName, Color.DarkSlateBlue);
                this.TitleText.AddNewLine();
            }
            this.TitleText.ResortTexts();

            // this.GuanzhiText.Clear();
            /* foreach (Guanzhi guanzhi in person.Guanzhis)
             {
                 this.GuanzhiText.AddText(guanzhi.DetailedName, Color.Lime);
                 this.GuanzhiText.AddNewLine();
             }
             this.GuanzhiText.ResortTexts();
             */
            this.PersonSkillTexts.SimpleClear();
            this.LearnableSkillTexts.SimpleClear();
            
            #if DEBUG
            // 🔥 调试日志：追踪 UI 显示技能
            System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] {person.Name}(ID:{person.ID}): 开始更新技能显示");
            System.Diagnostics.Debug.WriteLine($"  - person.Skills.Skills.Count = {person.Skills.Skills.Count}");
            if (person.Skills.Skills.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"  - person.Skills.Skills.Keys = [{string.Join(", ", person.Skills.Skills.Keys)}]");
            }
            #endif
            
            foreach (Skill skill in Session.Current.Scenario.GameCommonData.AllSkills.Skills.Values)
            {
                Rectangle position = new Rectangle(this.SkillDisplayOffset.X + (skill.DisplayCol * this.SkillBlockSize.X), this.SkillDisplayOffset.Y + (skill.DisplayRow * this.SkillBlockSize.Y), this.SkillBlockSize.X, this.SkillBlockSize.Y);
                if (person.Skills.GetSkill(skill.ID) != null)
                {
                    this.PersonSkillTexts.AddText(skill.Name, position);
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  ✅ 添加到 PersonSkillTexts: {skill.Name}(ID:{skill.ID})");
                    #endif
                }
                else if (skill.CanLearn(person))
                {
                    this.LearnableSkillTexts.AddText(skill.Name, position);
                }
            }
            this.PersonSkillTexts.ResetAllAlignedPositions();
            this.LearnableSkillTexts.ResetAllAlignedPositions();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] {person.Name}(ID:{person.ID}): 完成，PersonSkillTexts.Count = {this.PersonSkillTexts.Count}");
            System.Diagnostics.Debug.WriteLine($"[PersonDetail.SetPerson] ========== 结束 ==========");
            #endif
            this.StuntText.Clear();
            //阿柒:特技显示效果修改,去掉多余的字
            //this.StuntText.AddText("战斗特技", Color.Yellow);
            //this.StuntText.AddNewLine();
            //this.StuntText.AddText(person.Stunts.Count.ToString() + "种", Color.Lime);
            //this.StuntText.AddNewLine();
            foreach (Stunt stunt in person.Stunts.Stunts.Values)
            {
                this.StuntText.AddText(stunt.Name, Color.Khaki);
                this.StuntText.AddText(" 战意消耗" + stunt.Combativity.ToString(), Color.SkyBlue);
                this.StuntText.AddNewLine();
            }
            this.StuntText.ResortTexts();
            this.BiographyText.Clear();
            
            // 🔥 调试：列传显示诊断
            System.Diagnostics.Debug.WriteLine($"[列传诊断] 武将: {person.Name} (ID: {person.ID})");
            System.Diagnostics.Debug.WriteLine($"[列传诊断] PersonBiographyID: {person.PersonBiographyID}");
            
            if (Session.Current?.Scenario?.AllBiographies == null)
            {
                System.Diagnostics.Debug.WriteLine($"[列传诊断] ❌ AllBiographies 为 null");
            }
            else
            {
                var bio = Session.Current.Scenario.AllBiographies.GetBiography(person.PersonBiographyID);
                if (bio == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[列传诊断] ❌ GetBiography({person.PersonBiographyID}) 返回 null");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[列传诊断] ✓ 找到列传 (Brief长度: {bio.Brief?.Length ?? 0})");
                }
            }
            
            if (person.PersonBiography == null)
            {
                System.Diagnostics.Debug.WriteLine($"[列传诊断] ❌ person.PersonBiography 为 null");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[列传诊断] ✓ person.PersonBiography 不为 null");
            }
            
            if (person.PersonBiography != null)
            {
                this.BiographyText.Clear();
                this.BiographyText.AddText("列传", this.BiographyText.TitleColor);
                this.BiographyText.AddNewLine();
                this.BiographyText.AddText(this.ShowingPerson.PersonBiography.Brief);
                this.BiographyText.AddNewLine();
                this.BiographyText.AddText("演义", this.BiographyText.SubTitleColor);
                this.BiographyText.AddNewLine();
                this.BiographyText.AddText(this.ShowingPerson.PersonBiography.Romance);
                this.BiographyText.AddNewLine();
                this.BiographyText.AddText("历史", this.BiographyText.SubTitleColor2);
                this.BiographyText.AddNewLine();
                this.BiographyText.AddText(this.ShowingPerson.PersonBiography.History);
                this.BiographyText.AddNewLine();
                this.BiographyText.AddText("剧本", this.BiographyText.SubTitleColor2);
                this.BiographyText.AddText("：");
                String[] lineBrokenText = ShowingPerson.PersonBiography.InGame.Split('\n');
                foreach (String s in lineBrokenText)
                {
                    this.BiographyText.AddText(s);
                    this.BiographyText.AddNewLine();
                }
                this.BiographyText.ResortTexts();
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonDetail] SetPerson Error: {ex.ToString()}");
            }
            finally
            {
                _isAccessingPersonProperties = false;
            }
        }

        internal void SetPosition(ShowPosition showPosition)
        {
            Rectangle rectDes = new Rectangle(0, 0, this.screen.viewportSize.X, this.screen.viewportSize.Y);
            Rectangle rect = new Rectangle(0, 0, this.BackgroundSize.X, this.BackgroundSize.Y);
            switch (showPosition)
            {
                case ShowPosition.Center:
                    rect = StaticMethods.GetCenterRectangle(rectDes, rect);
                    break;

                case ShowPosition.Top:
                    rect = StaticMethods.GetTopRectangle(rectDes, rect);
                    break;

                case ShowPosition.Left:
                    rect = StaticMethods.GetLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.Right:
                    rect = StaticMethods.GetRightRectangle(rectDes, rect);
                    break;

                case ShowPosition.Bottom:
                    rect = StaticMethods.GetBottomRectangle(rectDes, rect);
                    break;

                case ShowPosition.TopLeft:
                    rect = StaticMethods.GetTopLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.TopRight:
                    rect = StaticMethods.GetTopRightRectangle(rectDes, rect);
                    break;

                case ShowPosition.BottomLeft:
                    rect = StaticMethods.GetBottomLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.BottomRight:
                    rect = StaticMethods.GetBottomRightRectangle(rectDes, rect);
                    break;
            }
            this.DisplayOffset = new Point(rect.X, rect.Y);
            this.SurNameText.DisplayOffset = this.DisplayOffset;
            this.GivenNameText.DisplayOffset = this.DisplayOffset;
            this.CalledNameText.DisplayOffset = this.DisplayOffset;
            foreach (LabelText text in this.LabelTexts)
            {
                text.Label.DisplayOffset = this.DisplayOffset;
                text.Text.DisplayOffset = this.DisplayOffset;
            }
            this.TitleText.DisplayOffset = new Point(this.DisplayOffset.X + this.TitleClient.X, this.DisplayOffset.Y + this.TitleClient.Y);
            // this.GuanzhiText.DisplayOffset = new Point(this.DisplayOffset.X + this.GuanzhiClient.X, this.DisplayOffset.Y + this.GuanzhiClient.Y);
            this.AllSkillTexts.DisplayOffset = this.DisplayOffset;
            this.PersonSkillTexts.DisplayOffset = this.DisplayOffset;
            this.LearnableSkillTexts.DisplayOffset = this.DisplayOffset;
            this.StuntText.DisplayOffset = new Point(this.DisplayOffset.X + this.StuntClient.X, this.DisplayOffset.Y + this.StuntClient.Y);
            this.InfluenceText.DisplayOffset = new Point(this.DisplayOffset.X + this.InfluenceClient.X, this.DisplayOffset.Y + this.InfluenceClient.Y);
            this.ConditionText.DisplayOffset = new Point(this.DisplayOffset.X + this.ConditionClient.X, this.DisplayOffset.Y + this.ConditionClient.Y);
            this.BiographyText.DisplayOffset = new Point(this.DisplayOffset.X + this.BiographyClient.X, this.DisplayOffset.Y + this.BiographyClient.Y);
        }

        private Rectangle BackgroundDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X, this.DisplayOffset.Y, this.BackgroundSize.X, this.BackgroundSize.Y);
            }
        }

        public bool IsShowing
        {
            get
            {
                return this.isShowing;
            }
            set
            {
                if (this.isShowing == value) return;
                this.isShowing = value;
                if (value)
                {
                    this.screen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.SubDialog, DialogKind.PersonDetail));
                    this.screen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
                    this.screen.OnMouseLeftUp += new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                    this.screen.OnMouseRightUp += new Screen.MouseRightUp(this.screen_OnMouseRightUp);
                }
                else
                {
                    if (this.screen.PopUndoneWork().Kind != UndoneWorkKind.SubDialog)
                    {
                        throw new Exception("The UndoneWork is not a SubDialog.");
                    }
                    this.screen.OnMouseMove -= new Screen.MouseMove(this.screen_OnMouseMove);
                    this.screen.OnMouseLeftUp -= new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                    this.screen.OnMouseRightUp -= new Screen.MouseRightUp(this.screen_OnMouseRightUp);
                    this.current = null;
                    this.InfluenceText.Clear();
                    this.ConditionText.Clear();
                }
            }
        }

        private Rectangle PortraitDisplayPosition
        {
            get
            {
                return new Rectangle(this.PortraitClient.X + this.DisplayOffset.X, this.PortraitClient.Y + this.DisplayOffset.Y, this.PortraitClient.Width, this.PortraitClient.Height);
            }
        }

        private Rectangle TitleDisplayPosition
        {
            get
            {
                return new Rectangle(this.TitleText.DisplayOffset.X, this.TitleText.DisplayOffset.Y, this.TitleText.ClientWidth, this.TitleText.ClientHeight);
            }
        }
        /*
        private Rectangle GuanzhiDisplayPosition
        {
            get
            {
                return new Rectangle(this.GuanzhiText.DisplayOffset.X, this.GuanzhiText.DisplayOffset.Y, this.GuanzhiText.ClientWidth, this.GuanzhiText.ClientHeight);
            }
        }
        */
        private Rectangle StuntDisplayPosition
        {
            get
            {
                return new Rectangle(this.StuntText.DisplayOffset.X, this.StuntText.DisplayOffset.Y, this.StuntText.ClientWidth, this.StuntText.ClientHeight);
            }
        }
    }
}

