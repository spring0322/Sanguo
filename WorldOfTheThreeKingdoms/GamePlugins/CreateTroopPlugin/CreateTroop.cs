using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using GameObjects.Influences;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PluginInterface;
using System;
using System.Collections.Generic;

namespace CreateTroopPlugin
{

    public class CreateTroop
    {
        internal Point BackgroundSize;
        internal PlatformTexture BackgroundTexture;
        internal Rectangle CombatMethodClient;
        internal FreeRichText CombatMethodText = new FreeRichText();
        internal PlatformTexture CreateButtonDisabledTexture;
        internal PlatformTexture CreateButtonDisplayTexture;
        internal bool CreateButtonEnabled;
        internal Rectangle CreateButtonPosition;
        internal PlatformTexture CreateButtonSelectedTexture;
        internal PlatformTexture CreateButtonTexture;
        internal GameDelegates.VoidFunction CreateFunction;
        internal Architecture CreatingArchitecture;
        internal Person CreatingLeader;
        internal Military CreatingMilitary;
        internal GameObjectList CreatingPersons;
#pragma warning disable CS0649 // Field 'CreateTroop.CreatingShelledMilitary' is never assigned to, and will always have its default value null
        internal Military CreatingShelledMilitary;
#pragma warning restore CS0649 // Field 'CreateTroop.CreatingShelledMilitary' is never assigned to, and will always have its default value null
        internal Troop CreatingTroop;
        private Point DisplayOffset;
        internal IGameFrame GameFramePlugin;
        internal Rectangle InfluenceClient;
        internal FreeRichText InfluenceText = new FreeRichText();
        private bool isShowing;
        internal List<LabelText> LabelTexts = new List<LabelText>();
        internal PlatformTexture LeaderButtonDisabledTexture;
        internal PlatformTexture LeaderButtonDisplayTexture;
        internal bool LeaderButtonEnabled;
        internal Rectangle LeaderButtonPosition;
        internal PlatformTexture LeaderButtonSelectedTexture;
        internal PlatformTexture LeaderButtonTexture;
        internal PlatformTexture MilitaryButtonDisabledTexture;
        internal PlatformTexture MilitaryButtonDisplayTexture;
        internal bool MilitaryButtonEnabled;
        internal Rectangle MilitaryButtonPosition;
        internal PlatformTexture MilitaryButtonSelectedTexture;
        internal PlatformTexture MilitaryButtonTexture;
        internal INumberInputer NumberInputerPlugin;
        internal Rectangle OtherPersonClient;
        internal FreeRichText OtherPersonText = new FreeRichText();
        internal PlatformTexture PersonButtonDisabledTexture;
        internal PlatformTexture PersonButtonDisplayTexture;
        internal bool PersonButtonEnabled;
        internal Rectangle PersonButtonPosition;
        internal PlatformTexture PersonButtonSelectedTexture;
        internal PlatformTexture PersonButtonTexture;
        internal Rectangle PortraitClient;
        internal PlatformTexture RationButtonDisabledTexture;
        internal PlatformTexture RationButtonDisplayTexture;
        internal bool RationButtonEnabled;
        internal Rectangle RationButtonPosition;
        internal PlatformTexture RationButtonSelectedTexture;
        internal PlatformTexture RationButtonTexture;
        internal int RationDays;

        private bool setttingRation = false;
        internal MilitaryKind ShellMilitaryKind;
        internal Rectangle StuntClient;
        internal FreeRichText StuntText = new FreeRichText();
        internal ITabList TabListPlugin;
        internal FreeText TroopNameText;

        internal PlatformTexture zijinButtonDisabledTexture;
        internal PlatformTexture zijinButtonDisplayTexture;
        internal bool zijinButtonEnabled;
        internal Rectangle zijinButtonPosition;
        internal PlatformTexture zijinButtonSelectedTexture;
        internal PlatformTexture zijinButtonTexture;
        internal int zijin=0;

        private bool shezhizijin = false;


        private void AddLeaderPreferredPersons()
        {
            if (this.CreatingLeader == null) return;
            if (this.CreatingLeader.preferredTroopPersons.Count > 0)
            {
                foreach (Person p in this.CreatingLeader.preferredTroopPersons)
                {
                    if (this.CreatingArchitecture.PersonsExcludeNvGuan.GameObjects.Contains(p) && !this.CreatingPersons.GameObjects.Contains(p))
                    {
                        this.CreatingPersons.Add(p);
                    }
                }
            }
        }


        private void AfterSelectMilitary()
        {
            // ===== STEP 1: Initialize CreatingPersons =====
            System.Diagnostics.Debug.WriteLine($"[AfterSelectMilitary] START - Architecture: {this.CreatingArchitecture?.Name ?? "null"}");
            
            // Always create a new list to prevent stale data
            this.CreatingPersons = new GameObjectList();
            this.CreatingLeader = null;
            
            // 🔥 改进：智能选择主将
            // 日期：2026-02-11
            // 玩家手动创建：使用 AI 逻辑选择最佳主将作为默认值，玩家可以通过 UI 更换
            // AI 创建：直接使用 AI 选择的主将
            
            if (this.CreatingArchitecture != null && this.CreatingMilitary != null)
            {
                // 先尝试使用编队绑定的主将（如果有）
                Person preferredLeader = null;
                if (this.CreatingArchitecture.PersonsExcludeNvGuan.HasGameObject(this.CreatingMilitary.FollowedLeader))
                {
                    preferredLeader = this.CreatingMilitary.FollowedLeader;
                }
                else if (this.CreatingArchitecture.PersonsExcludeNvGuan.HasGameObject(this.CreatingMilitary.Leader))
                {
                    preferredLeader = this.CreatingMilitary.Leader;
                }
                
                // 设置主将
                if (preferredLeader != null)
                {
                    // 优先使用编队绑定的主将
                    this.CreatingLeader = preferredLeader;
                }
                else
                {
                    // 否则使用 AI 逻辑选择最佳主将
                    this.CreatingLeader = SelectBestLeaderForMilitary(this.CreatingArchitecture.PersonsExcludeNvGuan, this.CreatingMilitary);
                }
                
                // 只添加主将到列表
                if (this.CreatingLeader != null)
                {
                    this.CreatingPersons.Add(this.CreatingLeader);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[AfterSelectMilitary] CreatingPersons.Count: {this.CreatingPersons?.Count ?? 0}, CreatingLeader: {this.CreatingLeader?.Name ?? "null"}");
            
            // ===== STEP 2: Handle CreatingTroop (only if it exists) =====
            if (this.CreatingTroop != null && this.CreatingMilitary != null)
            {
                this.CreatingTroop.SetArmy(this.CreatingMilitary);
                
                if (!this.CreatingTroop.IsTransport)
                {
                    if (this.CreatingArchitecture.Food >= this.CreatingTroop.FoodMax)
                    {
                        this.RationDays = this.CreatingTroop.RationDays;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodMax;
                    }
                    else
                    {
                        this.RationDays = this.CreatingArchitecture.Food / this.CreatingTroop.FoodCostPerDay;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodCostPerDay * this.RationDays;
                    }
                }
                else
                {
                    if (this.CreatingArchitecture.Food >= this.CreatingTroop.FoodCostPerDay * 20)
                    {
                        this.RationDays = 20;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodCostPerDay * 20;
                    }
                    else
                    {
                        this.RationDays = this.CreatingArchitecture.Food / this.CreatingTroop.FoodCostPerDay;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodCostPerDay * this.RationDays;
                    }
                }
                
                this.zijin = 0;
                this.CreatingTroop.zijin = 0;
            }
            
            // 自动添加主将偏好的武将
            this.AddLeaderPreferredPersons();
        }
        
        /// <summary>
        /// 🔥 新增：为军队选择最佳主将
        /// 日期：2026-02-11
        /// 根据军队类型和武将能力选择最合适的主将
        /// </summary>
        private Person SelectBestLeaderForMilitary(GameObjectList persons, Military military)
        {
            if (persons == null || persons.Count == 0)
                return null;
            
            Person bestLeader = null;
            int bestScore = -1;
            
            // C# 12: 使用模式匹配简化类型检查
            foreach (GameObject obj in persons.GetList())
            {
                if (obj is not Person person || person.Tiredness >= 100)
                    continue;
                
                // 计算综合评分
                int score = CalculateLeaderScore(person, military);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestLeader = person;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[SelectBestLeaderForMilitary] 为军队 {military?.Name} 选择主将: {bestLeader?.Name ?? "null"} (评分: {bestScore})");
            return bestLeader;
        }
        
        /// <summary>
        /// 🔥 新增：计算武将作为主将的评分
        /// 考虑因素：战斗力、统率、适应性
        /// </summary>
        private int CalculateLeaderScore(Person person, Military military)
        {
            if (person == null || military == null)
                return 0;
            
            // 基础评分：战斗力（主要因素）
            int score = person.FightingForce;
            
            // 统率加成（影响部队士气和战斗力）
            score += person.Command * 2;
            
            // 武力加成（影响近战能力）
            score += person.Strength;
            
            // 智力加成（影响策略和特技）
            score += person.Intelligence / 2;
            
            // 兵种适应性加成
            if (military.Kind != null)
            {
                switch (military.Kind.Type)
                {
                    case MilitaryType.步兵:
                        // 步兵重视武力和统率
                        score += person.Strength / 2;
                        break;
                    case MilitaryType.弩兵:
                        // 弩兵重视智力
                        score += person.Intelligence / 2;
                        break;
                    case MilitaryType.骑兵:
                        // 骑兵重视武力和统率
                        score += (person.Strength + person.Command) / 2;
                        break;
                    case MilitaryType.水军:
                        // 水军重视智力和统率
                        score += (person.Intelligence + person.Command) / 2;
                        break;
                    case MilitaryType.器械:
                        // 器械重视智力
                        score += person.Intelligence;
                        break;
                }
            }
            
            // 疲劳惩罚
            score -= person.Tiredness / 2;
            
            return score;
        }

        internal void Draw()
        {
            Rectangle? sourceRectangle = null;
            CacheManager.Draw(this.BackgroundTexture, this.BackgroundDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);
            if (this.MilitaryButtonEnabled)
            {
                sourceRectangle = null;
                CacheManager.Draw(this.MilitaryButtonDisplayTexture, this.MilitaryButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            else
            {
                sourceRectangle = null;
                CacheManager.Draw(this.MilitaryButtonDisabledTexture, this.MilitaryButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            if (this.PersonButtonEnabled)
            {
                sourceRectangle = null;
                CacheManager.Draw(this.PersonButtonDisplayTexture, this.PersonButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            else
            {
                sourceRectangle = null;
                CacheManager.Draw(this.PersonButtonDisabledTexture, this.PersonButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            if (this.LeaderButtonEnabled)
            {
                sourceRectangle = null;
                CacheManager.Draw(this.LeaderButtonDisplayTexture, this.LeaderButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            else
            {
                sourceRectangle = null;
                CacheManager.Draw(this.LeaderButtonDisabledTexture, this.LeaderButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            if (this.RationButtonEnabled)
            {
                sourceRectangle = null;
                CacheManager.Draw(this.RationButtonDisplayTexture, this.RationButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            else
            {
                sourceRectangle = null;
                CacheManager.Draw(this.RationButtonDisabledTexture, this.RationButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            if (this.zijinButtonEnabled)
            {
                sourceRectangle = null;
                CacheManager.Draw(this.zijinButtonDisplayTexture, this.zijinButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            else
            {
                sourceRectangle = null;
                CacheManager.Draw(this.zijinButtonDisabledTexture, this.zijinButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            if (this.CreateButtonEnabled)
            {
                sourceRectangle = null;
                CacheManager.Draw(this.CreateButtonDisplayTexture, this.CreateButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            else
            {
                sourceRectangle = null;
                CacheManager.Draw(this.CreateButtonDisabledTexture, this.CreateButtonDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
            }
            if (this.CreatingTroop != null && this.CreatingTroop.Leader != null)
            {
                CacheManager.DrawZhsanAvatar(this.CreatingTroop.Leader, this.PortraitDisplayPosition, 0.199f, PortraitSize.Small);
            }
            this.TroopNameText.Draw(0.1999f);
            foreach (LabelText text in this.LabelTexts)
            {
                text.Label.Draw(0.1999f);
                text.Text.Draw(0.1999f);
            }
            this.OtherPersonText.Draw(0.1999f);
            this.CombatMethodText.Draw(0.1999f);
            this.StuntText.Draw(0.1999f);
            this.InfluenceText.Draw(0.1999f);
        }

        private void InitialCreateingTroop()
        {
            System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] START - Architecture: {this.CreatingArchitecture?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] CampaignMilitaryList.Count: {this.CreatingArchitecture?.GetCampaignMilitaryList()?.Count ?? -1}");
            System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] ShellMilitaryKind: {this.ShellMilitaryKind?.Name ?? "null"}");
            
            // 🔥 修复：强制清理旧的 CreatingTroop，防止残留数据
            // 每次打开对话框时都应该从头开始创建新的部队
            if (this.CreatingTroop != null)
            {
                System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] 清理旧的 CreatingTroop: {this.CreatingTroop.DisplayName}");
                this.CreatingTroop.Destroy(true, false, true);
                this.CreatingTroop = null;
            }
            
            if (this.ShellMilitaryKind == null)
            {
                int militaryCount = this.CreatingArchitecture.GetCampaignMilitaryList().Count;
                System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] 非运兵船模式，军队数量: {militaryCount}");
                
                if (militaryCount == 1)
                {
                    // FIX: Initialize CreatingMilitary and call AfterSelectMilitary FIRST
                    // so that CreatingPersons is properly initialized before CreateSimulateTroop uses it
                    this.CreatingMilitary = this.CreatingArchitecture.CampaignMilitaryList[0] as Military;
                    System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] CreatingMilitary 设置为: {this.CreatingMilitary?.Name ?? "null"}");
                    
                    this.AfterSelectMilitary();  // This sets CreatingPersons
                    
                    // Now CreateSimulateTroop can use the properly initialized CreatingPersons
                    // 注意：即使 CreatingLeader 为 null，也允许创建部队（用户可以后续选择主将）
                    System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] 调用 CreateSimulateTroop - Military: {this.CreatingMilitary?.Name ?? "null"}, Leader: {this.CreatingLeader?.Name ?? "null"}");
                    this.CreatingTroop = Troop.CreateSimulateTroop(this.CreatingArchitecture, this.CreatingPersons, this.CreatingLeader, this.CreatingMilitary, this.RationDays, this.CreatingArchitecture.Position);
                    this.MoveCandidatesToPersons();
                }
                else if (militaryCount > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] 有多个军队，需要用户选择");
                    // 列出所有军队
                    for (int i = 0; i < militaryCount; i++)
                    {
                        var mil = this.CreatingArchitecture.CampaignMilitaryList[i] as Military;
                        System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop]   军队 {i}: {mil?.Name ?? "null"}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] ⚠️ 警告：建筑没有军队！");
                }
            }
            else
            {
                this.CreatingMilitary = Military.SimCreate(this.CreatingArchitecture, this.ShellMilitaryKind);
                System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] CreatingMilitary (Shell) 创建: {this.CreatingMilitary?.Name ?? "null"}");
                
                if (this.CreatingArchitecture.GetCampaignMilitaryList().Count == 1)
                {
                    // FIX: Same fix for shelled military case
                    this.CreatingMilitary.SetShelledMilitary(this.CreatingArchitecture.CampaignMilitaryList[0] as Military);
                    this.AfterSelectMilitary();  // This sets CreatingPersons
                    
                    // 注意：即使 CreatingLeader 为 null，也允许创建部队（用户可以后续选择主将）
                    System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] 调用 CreateSimulateTroop (Shell) - Military: {this.CreatingMilitary?.Name ?? "null"}, Leader: {this.CreatingLeader?.Name ?? "null"}");
                    this.CreatingTroop = Troop.CreateSimulateTroop(this.CreatingArchitecture, this.CreatingPersons, this.CreatingLeader, this.CreatingMilitary, this.RationDays, this.CreatingArchitecture.Position);
                    this.MoveCandidatesToPersons();
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[InitialCreateingTroop] 调用 RefreshDetailDisplay - CreatingMilitary: {this.CreatingMilitary?.Name ?? "null"}");
            this.RefreshDetailDisplay();
        }

        internal void Initialize()
        {
            
        }

        private void MoveCandidatesToPersons()
        {
            // 🔥 根本修复：CreatingTroop 可能为 null（没有主将时）
            if (this.CreatingTroop == null || this.CreatingTroop.Candidates == null)
            {
                return;
            }
            
            foreach (GameObject obj in this.CreatingTroop.Candidates)
            {
                Person p = (obj is Person ? (Person)obj : null);
                if (p != null)
                {
                    p.LocationTroop = this.CreatingTroop;
                }
            }
        }

        private void RefreshDetailDisplay()
        {
            // 🔥 修复：即使没有选择军队，也要设置按钮状态
            // 让用户可以选择军队
            this.MilitaryButtonEnabled = this.CreatingArchitecture != null && this.CreatingArchitecture.CampaignMilitaryList.Count > 1;
            
            // 如果还没有选择军队，只设置按钮状态，不创建部队
            if (this.CreatingArchitecture == null || this.CreatingMilitary == null)
            {
                System.Diagnostics.Debug.WriteLine("[RefreshDetailDisplay] 等待用户选择军队...");
                // 禁用其他按钮
                this.PersonButtonEnabled = false;
                this.LeaderButtonEnabled = false;
                this.CreateButtonEnabled = false;
                this.RationButtonEnabled = false;
                this.zijinButtonEnabled = false;
                return;
            }
            
            if (this.CreatingPersons != null)
            {
                foreach (GameObject obj in this.CreatingPersons)
                {
                    Person person = (obj is Person ? (Person)obj : null);
                    if (person != null)
                    {
                        foreach (Skill s in person.Skills.GetSkillList())
                        {
                            s.Influences.PurifyInfluence(person, Applier.Skill, s.ID, false);
                        }
                        foreach (Title t in person.Titles)
                        {
                            t.Influences.PurifyInfluence(person, Applier.Title, t.ID, false);
                        }
                        foreach (Stunt s in person.Stunts.GetStuntList())
                        {
                            s.Influences.PurifyInfluence(person, Applier.Stunt, 0, false);
                        }
                        person.PurifyAllTreasures(false);
                    }
                }
            }
            this.CreatingTroop = Troop.CreateSimulateTroop(this.CreatingArchitecture, this.CreatingPersons, this.CreatingLeader, this.CreatingMilitary, this.RationDays, this.CreatingArchitecture.Position);
            this.MoveCandidatesToPersons();
            
            // 🔥 根本修复：验证模拟部队创建成功
            // 如果 CreateSimulateTroop 返回 null（例如没有主将），则不继续处理
            if (this.CreatingTroop == null)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshDetailDisplay] ⚠️ 警告：CreateSimulateTroop 返回 null，无法继续");
                this.CreateButtonEnabled = false;
                this.RationButtonEnabled = false;
                this.zijinButtonEnabled = false;
                return;
            }
            
            if ((!this.shezhizijin && !this.setttingRation && (this.CreatingMilitary != null)) && (this.CreatingPersons != null))
            {
                /*
                if (this.CreatingArchitecture.Food >= this.CreatingTroop.FoodMax)
                {
                    this.RationDays = this.CreatingTroop.RationDays;
                    this.CreatingTroop.Food = this.CreatingTroop.FoodMax;
                }
                else
                {
                    this.RationDays = this.CreatingArchitecture.Food / this.CreatingTroop.FoodCostPerDay;
                    this.CreatingTroop.Food = this.CreatingTroop.FoodCostPerDay * this.RationDays;
                }
                */

                if (!this.CreatingTroop.IsTransport)
                {
                    if (this.CreatingArchitecture.Food >= this.CreatingTroop.FoodMax)
                    {
                        this.RationDays = this.CreatingTroop.RationDays;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodMax;
                    }
                    else
                    {
                        this.RationDays = this.CreatingArchitecture.Food / this.CreatingTroop.FoodCostPerDay;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodCostPerDay * this.RationDays;

                    }
                }
                else
                {
                    if (this.CreatingArchitecture.Food >= this.CreatingTroop.FoodCostPerDay * 20)
                    {
                        this.RationDays = 20;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodCostPerDay * 20;
                    }
                    else
                    {
                        this.RationDays = this.CreatingArchitecture.Food / this.CreatingTroop.FoodCostPerDay;
                        this.CreatingTroop.Food = this.CreatingTroop.FoodCostPerDay * this.RationDays;

                    }
                }

            }
            if ( (this.CreatingMilitary != null) && (this.CreatingPersons != null))
            {
                /*
                if (this.CreatingArchitecture.Fund  >= this.CreatingTroop.Army.zijinzuidazhi)
                {
                    this.zijin = this.CreatingTroop.Army.zijinzuidazhi;
                    this.CreatingTroop.zijin = this.CreatingTroop.Army.zijinzuidazhi;
                }
                else
                {
                    this.zijin = this.CreatingArchitecture.Fund;
                    this.CreatingTroop.zijin = this.CreatingArchitecture.Fund;

                }*/
                this.CreatingTroop.zijin = this.zijin;

            }
            /*
            if (this.shezhizijin)
            {
                this.CreatingTroop.zijin = this.zijin ;

            }
            */
            this.MilitaryButtonEnabled = this.CreatingArchitecture.CampaignMilitaryList.Count > 1;
            this.PersonButtonEnabled = (this.CreatingMilitary != null) && (this.CreatingArchitecture.PersonCount > 1);
            this.LeaderButtonEnabled = (this.CreatingPersons != null) && (this.CreatingPersons.Count > 1);
            this.CreateButtonEnabled = ((this.CreatingTroop.PersonCount > 0) && (this.CreatingTroop.Leader != null)) && (this.CreatingTroop.Army != null);
            this.RationButtonEnabled = this.CreateButtonEnabled;
            this.zijinButtonEnabled = this.CreateButtonEnabled && this.CreatingTroop.IsTransport;
            this.TroopNameText.Text = this.CreatingTroop.DisplayName;
            foreach (LabelText text in this.LabelTexts)
            {
                text.Text.Text = StaticMethods.GetPropertyValue(this.CreatingTroop, text.PropertyName).ToString();
            }
            this.OtherPersonText.Clear();
            this.OtherPersonText.AddText("其他人物", this.OtherPersonText.TitleColor);
            this.OtherPersonText.AddNewLine();
            if (this.CreatingTroop.PersonCount > 0)
            {
                int num = this.CreatingTroop.PersonCount - 1;
                this.OtherPersonText.AddText(num.ToString() + "人", this.OtherPersonText.SubTitleColor);
                this.OtherPersonText.AddNewLine();
                foreach (GameObject obj in this.CreatingTroop.Persons)
                {
                    Person person = (obj is Person ? (Person)obj : null);
                    if (person != null && person != this.CreatingTroop.Leader)
                    {
                        this.OtherPersonText.AddText(person.Name);
                        this.OtherPersonText.AddNewLine();
                    }
                }
            }
            this.OtherPersonText.ResortTexts();
            this.CombatMethodText.Clear();
            this.CombatMethodText.AddText("部队战法", this.CombatMethodText.TitleColor);
            this.CombatMethodText.AddNewLine();
            if (this.CreatingTroop.PersonCount > 0)
            {
                this.CombatMethodText.AddText(this.CreatingTroop.CombatMethods.Count.ToString() + "种", this.CombatMethodText.SubTitleColor);
                this.CombatMethodText.AddNewLine();
                foreach (CombatMethod method in this.CreatingTroop.CombatMethods.CombatMethods.Values)
                {
                    this.CombatMethodText.AddText(method.Name, this.CombatMethodText.SubTitleColor2);
                    this.CombatMethodText.AddText(" 战意消耗" + ((method.Combativity - this.CreatingTroop.DecrementOfCombatMethodCombativityConsuming)).ToString(), Color.LightGreen);
                    this.CombatMethodText.AddNewLine();
                }
            }
            this.CombatMethodText.ResortTexts();
            this.StuntText.Clear();
            this.StuntText.AddText("战斗特技", this.StuntText.TitleColor);
            this.StuntText.AddNewLine();
            if (this.CreatingTroop.PersonCount > 0)
            {
                this.StuntText.AddText(this.CreatingTroop.Stunts.Count.ToString() + "种", this.StuntText.SubTitleColor);
                this.StuntText.AddNewLine();
                foreach (Stunt stunt in this.CreatingTroop.Stunts.Stunts.Values)
                {
                    this.StuntText.AddText(stunt.Name, this.StuntText.SubTitleColor2);
                    this.StuntText.AddText(" 战意消耗" + stunt.Combativity, this.StuntText.SubTitleColor3);
                    this.StuntText.AddNewLine();
                }
            }
            this.StuntText.ResortTexts();
            this.InfluenceText.Clear();
            this.InfluenceText.AddText("部队特性", this.InfluenceText.TitleColor);
            this.InfluenceText.AddNewLine();
            if (this.CreatingMilitary != null)
            {
                this.InfluenceText.AddText(this.CreatingMilitary.Kind.Name, this.InfluenceText.SubTitleColor);
                this.InfluenceText.AddNewLine();
                foreach (Influence influence in this.CreatingMilitary.Kind.Influences.Influences.Values)
                {
                    this.InfluenceText.AddText(influence.Name, this.InfluenceText.SubTitleColor2);
                    this.InfluenceText.AddText(influence.Description, this.InfluenceText.SubTitleColor3);
                    this.InfluenceText.AddNewLine();
                }
            }
            this.InfluenceText.ResortTexts();
        }

        public bool IsDialog
        {
            get
            {
                return Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Dialog;
            }
        }

        private void screen_OnMouseLeftUp(Point position)
        {
            if (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Dialog)
            {
                if (StaticMethods.PointInRectangle(position, this.OtherPersonDisplayPosition))
                {
                    if (this.OtherPersonText.CurrentPageIndex < (this.OtherPersonText.PageCount - 1))
                    {
                        this.OtherPersonText.NextPage();
                    }
                    else if (this.OtherPersonText.CurrentPageIndex == (this.OtherPersonText.PageCount - 1))
                    {
                        this.OtherPersonText.FirstPage();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.CombatMethodDisplayPosition))
                {
                    if (this.CombatMethodText.CurrentPageIndex < (this.CombatMethodText.PageCount - 1))
                    {
                        this.CombatMethodText.NextPage();
                    }
                    else if (this.CombatMethodText.CurrentPageIndex == (this.CombatMethodText.PageCount - 1))
                    {
                        this.CombatMethodText.FirstPage();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.InfluenceDisplayPosition))
                {
                    if (this.InfluenceText.CurrentPageIndex < (this.InfluenceText.PageCount - 1))
                    {
                        this.InfluenceText.NextPage();
                    }
                    else if (this.InfluenceText.CurrentPageIndex == (this.InfluenceText.PageCount - 1))
                    {
                        this.InfluenceText.FirstPage();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.MilitaryButtonDisplayPosition))
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.SelectMilitary();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.PersonButtonDisplayPosition))
                {
                    if (this.PersonButtonEnabled)
                    {
                        this.SelectPersons();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.LeaderButtonDisplayPosition))
                {
                    if (this.LeaderButtonEnabled)
                    {
                        this.SelectLeader();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.RationButtonDisplayPosition))
                {
                    if (this.RationButtonEnabled)
                    {
                        this.SetRation();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.zijinButtonDisplayPosition))
                {
                    if (this.zijinButtonEnabled)
                    {
                        this.Setzijin();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.CreateButtonDisplayPosition) && this.CreateButtonEnabled)
                {
                    this.SelectTroopStartPosition();
                }
            }
        }

        private void screen_OnMouseMove(Point position, bool leftDown)
        {
            if (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Dialog)
            {
                if (StaticMethods.PointInRectangle(position, this.MilitaryButtonDisplayPosition))
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.MilitaryButtonDisplayTexture = this.MilitaryButtonSelectedTexture;
                    }
                    if (this.PersonButtonEnabled)
                    {
                        this.PersonButtonDisplayTexture = this.PersonButtonTexture;
                    }
                    if (this.LeaderButtonEnabled)
                    {
                        this.LeaderButtonDisplayTexture = this.LeaderButtonTexture;
                    }
                    if (this.RationButtonEnabled)
                    {
                        this.RationButtonDisplayTexture = this.RationButtonTexture;
                    }
                    if (this.zijinButtonEnabled)
                    {
                        this.zijinButtonDisplayTexture = this.zijinButtonTexture;
                    }
                    if (this.CreateButtonEnabled)
                    {
                        this.CreateButtonDisplayTexture = this.CreateButtonTexture;
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.PersonButtonDisplayPosition))
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.MilitaryButtonDisplayTexture = this.MilitaryButtonTexture;
                    }
                    if (this.PersonButtonEnabled)
                    {
                        this.PersonButtonDisplayTexture = this.PersonButtonSelectedTexture;
                    }
                    if (this.LeaderButtonEnabled)
                    {
                        this.LeaderButtonDisplayTexture = this.LeaderButtonTexture;
                    }
                    if (this.RationButtonEnabled)
                    {
                        this.RationButtonDisplayTexture = this.RationButtonTexture;
                    }
                    if (this.zijinButtonEnabled)
                    {
                        this.zijinButtonDisplayTexture = this.zijinButtonTexture;
                    }
                    if (this.CreateButtonEnabled)
                    {
                        this.CreateButtonDisplayTexture = this.CreateButtonTexture;
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.LeaderButtonDisplayPosition))
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.MilitaryButtonDisplayTexture = this.MilitaryButtonTexture;
                    }
                    if (this.PersonButtonEnabled)
                    {
                        this.PersonButtonDisplayTexture = this.PersonButtonTexture;
                    }
                    if (this.LeaderButtonEnabled)
                    {
                        this.LeaderButtonDisplayTexture = this.LeaderButtonSelectedTexture;
                    }
                    if (this.RationButtonEnabled)
                    {
                        this.RationButtonDisplayTexture = this.RationButtonTexture;
                    }
                    if (this.zijinButtonEnabled)
                    {
                        this.zijinButtonDisplayTexture = this.zijinButtonTexture;
                    }
                    if (this.CreateButtonEnabled)
                    {
                        this.CreateButtonDisplayTexture = this.CreateButtonTexture;
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.RationButtonDisplayPosition))
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.MilitaryButtonDisplayTexture = this.MilitaryButtonTexture;
                    }
                    if (this.PersonButtonEnabled)
                    {
                        this.PersonButtonDisplayTexture = this.PersonButtonTexture;
                    }
                    if (this.LeaderButtonEnabled)
                    {
                        this.LeaderButtonDisplayTexture = this.LeaderButtonTexture;
                    }
                    if (this.RationButtonEnabled)
                    {
                        this.RationButtonDisplayTexture = this.RationButtonSelectedTexture;
                    }
                    if (this.zijinButtonEnabled)
                    {
                        this.zijinButtonDisplayTexture = this.zijinButtonTexture;
                    }
                    if (this.CreateButtonEnabled)
                    {
                        this.CreateButtonDisplayTexture = this.CreateButtonTexture;
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.CreateButtonDisplayPosition))
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.MilitaryButtonDisplayTexture = this.MilitaryButtonTexture;
                    }
                    if (this.PersonButtonEnabled)
                    {
                        this.PersonButtonDisplayTexture = this.PersonButtonTexture;
                    }
                    if (this.LeaderButtonEnabled)
                    {
                        this.LeaderButtonDisplayTexture = this.LeaderButtonTexture;
                    }
                    if (this.RationButtonEnabled)
                    {
                        this.RationButtonDisplayTexture = this.RationButtonTexture;
                    }
                    if (this.zijinButtonEnabled)
                    {
                        this.zijinButtonDisplayTexture = this.zijinButtonTexture;
                    }
                    if (this.CreateButtonEnabled)
                    {
                        this.CreateButtonDisplayTexture = this.CreateButtonSelectedTexture;
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.zijinButtonDisplayPosition))
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.MilitaryButtonDisplayTexture = this.MilitaryButtonTexture;
                    }
                    if (this.PersonButtonEnabled)
                    {
                        this.PersonButtonDisplayTexture = this.PersonButtonTexture;
                    }
                    if (this.LeaderButtonEnabled)
                    {
                        this.LeaderButtonDisplayTexture = this.LeaderButtonTexture;
                    }
                    if (this.RationButtonEnabled)
                    {
                        this.RationButtonDisplayTexture = this.RationButtonTexture;
                    }
                    if (this.zijinButtonEnabled)
                    {
                        this.zijinButtonDisplayTexture = this.zijinButtonSelectedTexture;
                    }
                    if (this.CreateButtonEnabled)
                    {
                        this.CreateButtonDisplayTexture = this.CreateButtonTexture;
                    }
                }
                else
                {
                    if (this.MilitaryButtonEnabled)
                    {
                        this.MilitaryButtonDisplayTexture = this.MilitaryButtonTexture;
                    }
                    if (this.PersonButtonEnabled)
                    {
                        this.PersonButtonDisplayTexture = this.PersonButtonTexture;
                    }
                    if (this.LeaderButtonEnabled)
                    {
                        this.LeaderButtonDisplayTexture = this.LeaderButtonTexture;
                    }
                    if (this.RationButtonEnabled)
                    {
                        this.RationButtonDisplayTexture = this.RationButtonTexture;
                    }
                    if (this.zijinButtonEnabled)
                    {
                        this.zijinButtonDisplayTexture = this.zijinButtonTexture;
                    }
                    if (this.CreateButtonEnabled)
                    {
                        this.CreateButtonDisplayTexture = this.CreateButtonTexture;
                    }
                }
            }
        }

        private void screen_OnMouseRightDown(Point position)
        {
            if (Session.MainGame.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Dialog)
            {
                // 🔥 根本修复：CreatingTroop 可能为 null（没有主将时）
                if (this.CreatingTroop != null && this.CreatingTroop.Army != null)
                {
                    this.CreatingTroop.Destroy(true, false, true);
                }
                this.IsShowing = false;
            }
        }

        private void SelectLeader()
        {
            this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.Architecture_PersonToTroop, false, true, true, false, this.CreatingPersons, this.CreatingLeader.GetGameObjectList(), "出征人物", "");
            this.GameFramePlugin.SetOKFunction(delegate {
                this.CreatingLeader = this.TabListPlugin.SelectedItem is Person ? (Person)this.TabListPlugin.SelectedItem : null;
                this.RefreshDetailDisplay();
            });
        }

        private void SelectMilitary()
        {
            if (this.CreatingPersons != null)
            {
                foreach (GameObject obj in this.CreatingPersons)
                {
                    Person p = (obj is Person ? (Person)obj : null);
                    if (p != null)
                    {
                        p.LocationTroop = null;
                    }
                }
            }
            
            if (this.ShellMilitaryKind == null)
            {
                this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Military, FrameFunction.GetCampaignMilitary, false, true, true, false, this.CreatingArchitecture.GetCampaignMilitaryList(), (this.CreatingMilitary == null) ? null : this.CreatingMilitary.GetGameObjectList(), "选择编队", "");
            }
            else
            {
                this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Military, FrameFunction.GetCampaignMilitary, false, true, true, false, this.CreatingArchitecture.GetShelledMilitaryList(this.ShellMilitaryKind.Type), (this.CreatingMilitary.ShelledMilitary == null) ? null : this.CreatingMilitary.ShelledMilitary.GetGameObjectList(), "选择编队", "");
            }
            this.GameFramePlugin.SetOKFunction(delegate {
                // 添加类型安全检查
                var selectedItem = this.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    if (selectedItem is Military)
                    {
                        if (this.ShellMilitaryKind == null)
                        {
                            this.CreatingMilitary = (selectedItem is Military ? (Military)selectedItem : null);
                        }
                        else
                        {
                            this.CreatingMilitary.SetShelledMilitary((selectedItem is Military ? (Military)selectedItem : null));
                        }
                        this.AfterSelectMilitary();
                        this.RefreshDetailDisplay();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateTroop] 错误：选中的对象不是Military类型: {selectedItem.GetType().Name}");
                        // 可以在这里显示错误消息给用户
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CreateTroop] 错误：没有选中任何对象");
                }
            });
        }

        private void SelectPersons()
        {
            this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.Architecture_PersonToTroop, false, true, true, true, this.CreatingArchitecture.PersonsExcludeNvGuan, this.CreatingPersons, "出征人物", "");
            this.GameFramePlugin.SetOKFunction(delegate {
                if (this.CreatingPersons != null)
                {
                    foreach (GameObject obj in this.CreatingPersons)
                    {
                        Person p = (obj is Person ? (Person)obj : null);
                        if (p != null)
                        {
                            p.LocationTroop = null;
                        }
                    }
                }

                // 添加类型安全检查
                var selectedList = this.TabListPlugin.SelectedItemList as GameObjectList;
                if (selectedList != null)
                {
                    // 验证所有选中的对象都是Person类型
                    bool allPersons = true;
                    foreach (GameObject obj in selectedList)
                    {
                        if (!(obj is Person))
                        {
                            allPersons = false;
                            System.Diagnostics.Debug.WriteLine($"[CreateTroop] 错误：选中的对象不是Person类型: {obj?.GetType().Name}");
                            break;
                        }
                    }
                    
                    if (allPersons)
                    {
                        // Clone the list to avoid reference sharing with TabListPlugin which causes list pollution
                        this.CreatingPersons = new GameObjectList();
                        foreach (GameObject obj in selectedList)
                        {
                            if (obj is Person)
                            {
                                this.CreatingPersons.Add(obj);
                            }
                        }
                        
                        if (this.CreatingPersons.Count > 0)
                        {
                            Person selectedLeader = this.CreatingPersons[0] as Person;
                            if (this.CreatingPersons.Count > 1)
                            {
                                int maxFightingAbility = 0;
                                foreach (GameObject obj in this.CreatingPersons)
                                {
                                    Person p = (obj is Person ? (Person)obj : null);
                                    if (p != null)
                                    {
                                        this.CreatingTroop = Troop.CreateSimulateTroop(this.CreatingArchitecture, this.CreatingPersons, p, this.CreatingMilitary, this.RationDays, this.CreatingArchitecture.Position);
                                        this.MoveCandidatesToPersons();
                                        if (this.CreatingTroop.FightingForce > maxFightingAbility)
                                        {
                                            maxFightingAbility = this.CreatingTroop.FightingForce;
                                            selectedLeader = p;
                                        }
                                    }
                                }
                            }
                            this.CreatingLeader = selectedLeader;
                            this.RefreshDetailDisplay();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[CreateTroop] 错误：选中的对象包含非Person类型，操作取消");
                        // 可以在这里显示错误消息给用户
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CreateTroop] 错误：SelectedItemList不是GameObjectList类型");
                }
            });
        }

        private void SelectTroopStartPosition()
        {
            // Debug logging to trace CreatingPersons content
            System.Diagnostics.Debug.WriteLine($"[SelectTroopStartPosition] CreatingPersons is null: {this.CreatingPersons == null}");
            System.Diagnostics.Debug.WriteLine($"[SelectTroopStartPosition] CreatingPersons count: {this.CreatingPersons?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[SelectTroopStartPosition] CreatingLeader: {this.CreatingLeader?.Name ?? "null"}");
            
            if (this.CreatingPersons != null)
            {
                foreach (GameObject obj in this.CreatingPersons)
                {
                    System.Diagnostics.Debug.WriteLine($"[SelectTroopStartPosition] CreatingPersons item: Type={obj?.GetType().Name}, IsPerson={obj is Person}");
                }
            }
            
            if (this.CreatingTroop?.Leader == null)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectTroopStartPosition] ERROR: CreatingTroop.Leader is null!");
                return;
            }
            
            this.CreatingTroop.Leader.preferredTroopPersons.Clear();
            foreach (GameObject obj in this.CreatingPersons)
            {
                Person p = (obj is Person ? (Person)obj : null);
                if (p != null && p != this.CreatingTroop.Leader)
                {
                    this.CreatingTroop.Leader.preferredTroopPersons.Add(p);
                }
            }
            if (this.CreateFunction != null)
            {
                this.CreateFunction();
            }
            this.IsShowing = false;
            Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.ArchitectureAvailableContactArea));
        }

        internal void SetArchitecture(Architecture architecture)
        {
            this.CreatingArchitecture = architecture;
        }

        internal void SetPosition(ShowPosition showPosition)
        {
            Rectangle rectDes = new Rectangle(0, 0, Session.MainGame.mainGameScreen.viewportSize.X, Session.MainGame.mainGameScreen.viewportSize.Y);
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
            this.TroopNameText.DisplayOffset = this.DisplayOffset;
            foreach (LabelText text in this.LabelTexts)
            {
                text.Label.DisplayOffset = this.DisplayOffset;
                text.Text.DisplayOffset = this.DisplayOffset;
            }
            this.OtherPersonText.DisplayOffset = new Point(this.DisplayOffset.X + this.OtherPersonClient.X, this.DisplayOffset.Y + this.OtherPersonClient.Y);
            this.CombatMethodText.DisplayOffset = new Point(this.DisplayOffset.X + this.CombatMethodClient.X, this.DisplayOffset.Y + this.CombatMethodClient.Y);
            this.StuntText.DisplayOffset = new Point(this.DisplayOffset.X + this.StuntClient.X, this.DisplayOffset.Y + this.StuntClient.Y);
            this.InfluenceText.DisplayOffset = new Point(this.DisplayOffset.X + this.InfluenceClient.X, this.DisplayOffset.Y + this.InfluenceClient.Y);
        }

        private void SetRation()
        {
            this.NumberInputerPlugin.SetDepthOffset(-0.01f);
            if (this.CreatingTroop.Army.KindID == 29 && this.CreatingTroop.Army.Quantity > 0)
            {
                this.NumberInputerPlugin.SetScale((int)(10000.0 / this.CreatingTroop.Army.Quantity));
                this.NumberInputerPlugin.SetUnit("万");
            }
            else
            {
                this.NumberInputerPlugin.SetUnit("天");
            }
            if (this.CreatingTroop.FoodCostPerDay == 0)
            {
                this.NumberInputerPlugin.SetMax((this.CreatingArchitecture.Food < this.CreatingTroop.RationDays) ? (this.CreatingArchitecture.Food) : this.CreatingTroop.RationDays);
            }
            else
            {
                this.NumberInputerPlugin.SetMax(((this.CreatingArchitecture.Food / this.CreatingTroop.FoodCostPerDay) < this.CreatingTroop.RationDays) ? (this.CreatingArchitecture.Food / this.CreatingTroop.FoodCostPerDay) : this.CreatingTroop.RationDays);
            }
            this.NumberInputerPlugin.SetMapPosition(ShowPosition.Center);
            
            this.NumberInputerPlugin.SetEnterFunction(delegate 
            {
                this.RationDays = this.NumberInputerPlugin.Number;
                this.setttingRation = true;
                this.RefreshDetailDisplay();
                this.setttingRation = false;
            });
            this.NumberInputerPlugin.IsShowing = true;
        }

        private void Setzijin()
        {
            this.NumberInputerPlugin.SetMax((this.CreatingArchitecture.Fund  < this.CreatingTroop.Army.zijinzuidazhi) ?  this.CreatingArchitecture.Fund: this.CreatingTroop.Army.zijinzuidazhi);
            this.NumberInputerPlugin.SetDepthOffset(-0.01f);
            this.NumberInputerPlugin.SetMapPosition(ShowPosition.Center);
            this.NumberInputerPlugin.SetEnterFunction(delegate
            {
                this.zijin = this.NumberInputerPlugin.Number;
                this.shezhizijin = true;
                this.RefreshDetailDisplay();
                this.shezhizijin = false;
            });
            this.NumberInputerPlugin.IsShowing = true;
        }

        private void ShowTabListInFrame(UndoneWorkKind undoneWork, FrameKind kind, FrameFunction function, bool OKEnabled, bool CancelEnabled, bool showCheckBox, bool multiselecting, GameObjectList gameObjectList, GameObjectList selectedObjectList, string title, string tabName)
        {
            this.GameFramePlugin.Kind = kind;
            this.GameFramePlugin.Function = function;
            this.TabListPlugin.InitialValues(gameObjectList, selectedObjectList, InputManager.NowMouse.ScrollWheelValue, title);
            this.TabListPlugin.SetListKindByName(kind.ToString(), showCheckBox, multiselecting);
            this.TabListPlugin.SetSelectedTab(tabName);
            this.GameFramePlugin.SetFrameContent(this.TabListPlugin.TabList, Session.MainGame.mainGameScreen.viewportSizeFull);
            this.GameFramePlugin.OKButtonEnabled = OKEnabled;
            this.GameFramePlugin.CancelButtonEnabled = CancelEnabled;
            this.GameFramePlugin.IsShowing = true;
        }

        private Rectangle BackgroundDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X, this.DisplayOffset.Y, this.BackgroundSize.X, this.BackgroundSize.Y);
            }
        }

        private Rectangle CombatMethodDisplayPosition
        {
            get
            {
                return new Rectangle(this.CombatMethodText.DisplayOffset.X, this.CombatMethodText.DisplayOffset.Y, this.CombatMethodText.ClientWidth, this.CombatMethodText.ClientHeight);
            }
        }

        private Rectangle CreateButtonDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X + this.CreateButtonPosition.X, this.DisplayOffset.Y + this.CreateButtonPosition.Y, this.CreateButtonPosition.Width, this.CreateButtonPosition.Height);
            }
        }

        private Rectangle InfluenceDisplayPosition
        {
            get
            {
                return new Rectangle(this.InfluenceText.DisplayOffset.X, this.InfluenceText.DisplayOffset.Y, this.InfluenceText.ClientWidth, this.InfluenceText.ClientHeight);
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
                this.isShowing = value;
                if (value)
                {
                    this.InitialCreateingTroop();
                    Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Dialog, DialogKind.TreasureDetail));
                    Session.MainGame.mainGameScreen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
                    Session.MainGame.mainGameScreen.OnMouseLeftUp += new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                    Session.MainGame.mainGameScreen.OnMouseRightDown += new Screen.MouseRightDown(this.screen_OnMouseRightDown);
                }
                else
                {
                    if (Session.MainGame.mainGameScreen.PopUndoneWork().Kind != UndoneWorkKind.Dialog)
                    {
                        throw new Exception("The UndoneWork is not a Dialog.");
                    }
                    Session.MainGame.mainGameScreen.OnMouseMove -= new Screen.MouseMove(this.screen_OnMouseMove);
                    Session.MainGame.mainGameScreen.OnMouseLeftUp -= new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                    Session.MainGame.mainGameScreen.OnMouseRightDown -= new Screen.MouseRightDown(this.screen_OnMouseRightDown);
                    this.OtherPersonText.Clear();
                    this.CombatMethodText.Clear();
                    this.StuntText.Clear();
                    this.InfluenceText.Clear();
                    
                    // 🔥 根本修复：CreatingTroop 可能为 null（例如没有选择主将时）
                    if (this.CreatingTroop != null)
                    {
                        foreach (GameObject obj in this.CreatingTroop.Persons)
                        {
                            Person p = (obj is Person ? (Person)obj : null);
                            if (p != null)
                            {
                                p.LocationTroop = null;
                            }
                        }
                        this.CreatingTroop.Persons.PurifyInfluences();
                    }
                    
                    this.CreatingTroop = null;
                    this.CreatingPersons = null;
                    this.CreatingMilitary = null;
                    this.CreatingLeader = null;
                    this.CreatingArchitecture = null;
                }
            }
        }

        private Rectangle LeaderButtonDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X + this.LeaderButtonPosition.X, this.DisplayOffset.Y + this.LeaderButtonPosition.Y, this.LeaderButtonPosition.Width, this.LeaderButtonPosition.Height);
            }
        }

        private Rectangle MilitaryButtonDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X + this.MilitaryButtonPosition.X, this.DisplayOffset.Y + this.MilitaryButtonPosition.Y, this.MilitaryButtonPosition.Width, this.MilitaryButtonPosition.Height);
            }
        }

        private Rectangle OtherPersonDisplayPosition
        {
            get
            {
                return new Rectangle(this.OtherPersonText.DisplayOffset.X, this.OtherPersonText.DisplayOffset.Y, this.OtherPersonText.ClientWidth, this.OtherPersonText.ClientHeight);
            }
        }

        private Rectangle PersonButtonDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X + this.PersonButtonPosition.X, this.DisplayOffset.Y + this.PersonButtonPosition.Y, this.PersonButtonPosition.Width, this.PersonButtonPosition.Height);
            }
        }

        private Rectangle PortraitDisplayPosition
        {
            get
            {
                return new Rectangle(this.PortraitClient.X + this.DisplayOffset.X, this.PortraitClient.Y + this.DisplayOffset.Y, this.PortraitClient.Width, this.PortraitClient.Height);
            }
        }

        private Rectangle RationButtonDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X + this.RationButtonPosition.X, this.DisplayOffset.Y + this.RationButtonPosition.Y, this.RationButtonPosition.Width, this.RationButtonPosition.Height);
            }
        }

        private Rectangle zijinButtonDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X + this.zijinButtonPosition.X, this.DisplayOffset.Y + this.zijinButtonPosition.Y, this.zijinButtonPosition.Width, this.zijinButtonPosition.Height);
            }
        }

        private Rectangle StuntDisplayPosition
        {
            get
            {
                return new Rectangle(this.StuntText.DisplayOffset.X, this.StuntText.DisplayOffset.Y, this.StuntText.ClientWidth, this.StuntText.ClientHeight);
            }
        }
    }
}


