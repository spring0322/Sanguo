using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TroopSurveyPlugin
{

    internal class TroopSurvey
    {
        public FreeText ArmyText;
        public Point BackgroundSize;
        public PlatformTexture BackgroundTexture;
        public FreeText CombativityText;
        public FreeText CombatTitleText;
        private Point displayOffset;
        public Color FactionColor;
        public Rectangle FactionPosition;
        public FreeText FactionText;
        public PlatformTexture FactionTexture;
        public FreeText KindText;
        public InformationLevel Level;
        public FreeText MoraleText;
        public FreeText NameText;
        public FreeText StatusText;
        public Troop TroopToSurvey;
        public Faction ViewingFaction;
        
        // 主将头像相关属性
        public Rectangle LeaderPortraitPosition;

        public void Draw()
        {
            Rectangle? sourceRectangle = null;
            CacheManager.Draw(this.BackgroundTexture, new Rectangle(this.displayOffset.X, this.displayOffset.Y, this.BackgroundSize.X, this.BackgroundSize.Y), sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.05f);
            CacheManager.Draw(this.FactionTexture, new Rectangle(this.displayOffset.X + this.FactionPosition.X, this.displayOffset.Y + this.FactionPosition.Y, this.FactionPosition.Width, this.FactionPosition.Height), null, this.FactionColor, 0f, Vector2.Zero, SpriteEffects.None, 0.049f);
            
            // 🔧 绘制兵种图标（带势力底色和兵种文字）
            DrawTroopKindIcon(0.048f);
            
            // 绘制主将头像在红框区域
            Rectangle leaderPortraitRect = Rectangle.Empty;
            if (this.TroopToSurvey != null && this.TroopToSurvey.Leader != null)
            {
                leaderPortraitRect = new Rectangle(
                    this.displayOffset.X + this.LeaderPortraitPosition.X,
                    this.displayOffset.Y + this.LeaderPortraitPosition.Y,
                    this.LeaderPortraitPosition.Width,
                    this.LeaderPortraitPosition.Height
                );
                CacheManager.DrawZhsanAvatar(this.TroopToSurvey.Leader, leaderPortraitRect, 0.048f, WorldOfTheThreeKingdoms.GameGlobal.PortraitSize.Small);
            }
            
            // 绘制主将名字 - 竖向排列
            if (this.TroopToSurvey?.Leader != null && !string.IsNullOrEmpty(this.TroopToSurvey.Leader.Name))
            {
                DrawVerticalText(this.TroopToSurvey.Leader.Name, this.NameText, 0.048f);
            }
            
            this.KindText.Draw(0.05f);
            this.FactionText.Draw(0.05f);
            
            // 绘制称号 - 横向渲染
            this.CombatTitleText.Draw(0.05f);
            
            this.ArmyText.Draw(0.05f);
            this.MoraleText.Draw(0.05f);
            this.CombativityText.Draw(0.05f);
            this.StatusText.Draw(0.05f);
        }

        /// <summary>
        /// 绘制竖向文字（从上到下，每个字符一行）
        /// </summary>
        /// <summary>
        /// 绘制竖向文字（从上到下，每个字符一行）
        /// </summary>
        private void DrawVerticalText(string text, FreeText textTemplate, float depth)
        {
            if (string.IsNullOrEmpty(text) || textTemplate == null) return;
            
            // 安全检查：确保Session和Font可用
            if (Session.Current == null || Session.Current.Font == null) return;
            
            try
            {
                int x = this.displayOffset.X + textTemplate.Position.X;
                int y = this.displayOffset.Y + textTemplate.Position.Y;
                int charHeight = 15; // 每个字符的高度间距（调小以适应更多字符）
                float scale = 0.8f; // 缩放比例（调小字号）
                
                foreach (char c in text)
                {
                    // 使用CacheManager.DrawString绘制单个字符
                    Vector2 position = new Vector2(x, y);
                    
                    try 
                    {
                        CacheManager.DrawString(
                            Session.Current.Font,
                            c.ToString(),
                            position,
                            textTemplate.TextColor,
                            0f,
                            Vector2.Zero,
                            scale,
                            SpriteEffects.None,
                            depth
                        );
                    }
                    catch (Exception drawEx)
                    {
                        // 捕获单个字符绘制的异常，避免整个UI崩溃
                         // System.Diagnostics.Debug.WriteLine($"[TroopSurvey] DrawChar error: {drawEx.Message}");
                    }
                    
                    y += charHeight;
                }
            }
            catch (Exception ex)
            {
                 // System.Diagnostics.Debug.WriteLine($"[TroopSurvey] DrawVerticalText error: {ex.Message}");
            }
        }

        private void ResetTextsPosition()
        {
            this.NameText.DisplayOffset = this.displayOffset;
            this.KindText.DisplayOffset = this.displayOffset;
            this.FactionText.DisplayOffset = this.displayOffset;
            this.CombatTitleText.DisplayOffset = this.displayOffset;
            this.ArmyText.DisplayOffset = this.displayOffset;
            this.MoraleText.DisplayOffset = this.displayOffset;
            this.CombativityText.DisplayOffset = this.displayOffset;
            this.StatusText.DisplayOffset = this.displayOffset;
        }

        public void Update()
        {
            this.FactionColor = Color.White;
            if (this.TroopToSurvey.BelongedFaction != null)
            {
                this.FactionColor = this.TroopToSurvey.BelongedFaction.FactionColor;
            }
            if (!((this.ViewingFaction == null) || Session.GlobalVariables.SkyEye))
            {
                this.NameText.Text = this.TroopToSurvey.DisplayName;
                this.KindText.Text = this.TroopToSurvey.KindString;
                this.FactionText.Text = this.TroopToSurvey.FactionString;
                this.CombatTitleText.Text = this.GetPriorityTitleString(); // 使用优先级称号
                this.ArmyText.Text = this.TroopToSurvey.QuantityInInformationLevel(this.Level);
                this.MoraleText.Text = this.TroopToSurvey.MoraleInInformationLevel(this.Level);
                this.CombativityText.Text = this.TroopToSurvey.CombativityInInformationLevel(this.Level);
                this.StatusText.Text = this.TroopToSurvey.DisplayStatus;
            }
            else
            {
                this.NameText.Text = this.TroopToSurvey.DisplayName;
                this.KindText.Text = this.TroopToSurvey.KindString;
                this.FactionText.Text = this.TroopToSurvey.FactionString;
                this.CombatTitleText.Text = this.GetPriorityTitleString(); // 使用优先级称号
                this.ArmyText.Text = this.TroopToSurvey.Quantity.ToString();
                this.MoraleText.Text = this.TroopToSurvey.Morale.ToString();
                this.CombativityText.Text = this.TroopToSurvey.Combativity.ToString();
                this.StatusText.Text = this.TroopToSurvey.DisplayStatus;
            }
        }

        /// <summary>
        /// 获取优先级称号字符串
        /// 优先级：个人称号(ID=1) > 君主(ID=3) > 战斗称号(ID=20) > 合称(ID=2/102) > 其他
        /// </summary>
        private string GetPriorityTitleString()
        {
            if (this.TroopToSurvey?.Leader == null) return "----";

            var leader = this.TroopToSurvey.Leader;
            
            // 优先级1: 个人称号 (TitleKind.ID = 1, Name = "称号")
            foreach (var title in leader.Titles)
            {
                if (title.Kind != null && title.Kind.ID == 1)
                {
                    return title.Name;
                }
            }

            // 优先级2: 君主称号 (TitleKind.ID = 3, Name = "君主")
            foreach (var title in leader.Titles)
            {
                if (title.Kind != null && title.Kind.ID == 3)
                {
                    return title.Name;
                }
            }

            // 优先级3: 战斗称号 (TitleKind.ID = 20, Name = "武将")
            foreach (var title in leader.Titles)
            {
                if (title.Kind != null && title.Kind.ID == 20)
                {
                    return title.Name;
                }
            }

            // 优先级4: 合称 (TitleKind.ID = 2 或 102, Name = "合称")
            foreach (var title in leader.Titles)
            {
                if (title.Kind != null && (title.Kind.ID == 2 || title.Kind.ID == 102))
                {
                    return title.Name;
                }
            }

            // 优先级5: 其他 (任何剩余的称号)
            foreach (var title in leader.Titles)
            {
                if (title.Kind != null)
                {
                    return title.Name;
                }
            }

            // 如果都没有，返回默认值
            return "----";
        }

        public Point DisplayOffset
        {
            get
            {
                return this.displayOffset;
            }
            set
            {
                this.displayOffset = value;
                this.ResetTextsPosition();
            }
        }
        
        // 兵种图标位置
        public Rectangle TroopKindIconPosition;
        
        /// <summary>
        /// 绘制兵种图标（带势力底色和兵种文字）
        /// </summary>
        private void DrawTroopKindIcon(float depth)
        {
            try
            {
                if (this.TroopToSurvey?.Army?.Kind?.Name == null) return;
                if (this.TroopKindIconPosition.Width <= 0 || this.TroopKindIconPosition.Height <= 0) return;

                // 获取势力颜色
                Color factionColor = Color.Gray;
                if (this.TroopToSurvey.BelongedFaction != null)
                {
                    factionColor = this.TroopToSurvey.BelongedFaction.FactionColor;
                }

                // 获取兵种首字
                string kindChar = this.TroopToSurvey.Army.Kind.Name.Substring(0, 1);

                // 计算绘制位置
                Rectangle iconRect = new Rectangle(
                    this.displayOffset.X + this.TroopKindIconPosition.X,
                    this.displayOffset.Y + this.TroopKindIconPosition.Y,
                    this.TroopKindIconPosition.Width,
                    this.TroopKindIconPosition.Height
                );

                // 绘制势力底色背景
                CacheManager.Draw(this.FactionTexture, iconRect, null, factionColor, 0f, Vector2.Zero, SpriteEffects.None, depth);

                // 确定文字颜色（根据背景亮度自动选择黑白）
                bool isBright = (0.299 * factionColor.R + 0.587 * factionColor.G + 0.114 * factionColor.B) > 150;
                Color textColor = isBright ? Color.Black : Color.White;

                // 计算文字居中位置
                if (Session.Current?.Font != null)
                {
                    Vector2 textSize = Session.Current.Font.MeasureString(kindChar);
                    float scale = Math.Min(
                        (iconRect.Width * 0.8f) / textSize.X,
                        (iconRect.Height * 0.8f) / textSize.Y
                    );
                    
                    Vector2 scaledSize = textSize * scale;
                    Vector2 textPos = new Vector2(
                        iconRect.X + (iconRect.Width - scaledSize.X) / 2,
                        iconRect.Y + (iconRect.Height - scaledSize.Y) / 2
                    );

                    // 绘制黑色描边
                    Vector2[] outlineOffsets = {
                        new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
                        new Vector2(-1, 0),                      new Vector2(1, 0),
                        new Vector2(-1, 1),  new Vector2(0, 1),  new Vector2(1, 1)
                    };
                    
                    Color outlineColor = isBright ? Color.White : Color.Black;
                    foreach (var offset in outlineOffsets)
                    {
                        CacheManager.DrawString(Session.Current.Font, kindChar, textPos + offset, outlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, depth - 0.001f);
                    }

                    // 绘制主文字
                    CacheManager.DrawString(Session.Current.Font, kindChar, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, depth - 0.002f);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopSurvey] DrawTroopKindIcon error: {ex.Message}");
            }
        }
    }
}

