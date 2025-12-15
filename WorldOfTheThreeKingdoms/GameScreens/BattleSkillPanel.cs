using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameObjects;
using GameManager;
using static GameManager.StrategistManager;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 战斗技能面板 - 显示可用技能和军师预测
    /// </summary>
    public class BattleSkillPanel
    {
        #region UI组件
        private SpriteFont font;
        private Texture2D backgroundTexture;
        private Texture2D buttonTexture;
        
        // 面板布局
        private Rectangle panelRect;
        private List<Rectangle> skillButtonRects = new List<Rectangle>();
        private Rectangle advisorInfoRect;
        
        // 技能图标位置（简化为文字按钮）
        private Dictionary<SkillType, Rectangle> skillButtons = new Dictionary<SkillType, Rectangle>();
        #endregion

        #region 数据
        private Faction playerFaction;
        private BattleInputController inputController;
        private bool isVisible = true;
        
        // 鼠标状态
        private MouseState previousMouseState;
        
        // 技能信息
        private Dictionary<SkillType, SkillInfo> skillInfos = new Dictionary<SkillType, SkillInfo>();
        #endregion

        /// <summary>
        /// 技能信息结构
        /// </summary>
        private struct SkillInfo
        {
            public string Name;
            public string Description;
            public Color ButtonColor;
            public int RequiredIntelligence;
            public int RequiredCommand;
            public int RequiredCharm;
        }

        public BattleSkillPanel(Faction faction, BattleInputController controller)
        {
            playerFaction = faction;
            inputController = controller;
            
            InitializeSkillInfos();
            InitializeLayout();
        }

        /// <summary>
        /// 初始化技能信息
        /// </summary>
        private void InitializeSkillInfos()
        {
            skillInfos[SkillType.FirePlot] = new SkillInfo
            {
                Name = "火计",
                Description = "对敌军使用火攻，造成大量伤害",
                ButtonColor = Color.Red,
                RequiredIntelligence = 70
            };

            skillInfos[SkillType.WaterPlot] = new SkillInfo
            {
                Name = "水计",
                Description = "引水淹敌，适用于河流附近",
                ButtonColor = Color.Blue,
                RequiredIntelligence = 70
            };

            skillInfos[SkillType.Ambush] = new SkillInfo
            {
                Name = "伏兵",
                Description = "设置伏兵攻击敌军",
                ButtonColor = Color.Brown,
                RequiredCommand = 70
            };

            skillInfos[SkillType.Provoke] = new SkillInfo
            {
                Name = "挑衅",
                Description = "激怒敌将，使其失去理智",
                ButtonColor = Color.Orange,
                RequiredCharm = 70
            };

            skillInfos[SkillType.Confuse] = new SkillInfo
            {
                Name = "混乱",
                Description = "扰乱敌军阵型，降低战斗力",
                ButtonColor = Color.Purple,
                RequiredIntelligence = 70
            };

            skillInfos[SkillType.Retreat] = new SkillInfo
            {
                Name = "撤退",
                Description = "有序撤退，减少损失",
                ButtonColor = Color.Gray,
                RequiredCommand = 70
            };

            skillInfos[SkillType.Rally] = new SkillInfo
            {
                Name = "鼓舞",
                Description = "鼓舞士气，提升战斗力",
                ButtonColor = Color.Gold,
                RequiredCommand = 70
            };
        }

        /// <summary>
        /// 初始化UI布局
        /// </summary>
        private void InitializeLayout()
        {
            // 面板位置（屏幕右下角）
            int panelWidth = 300;
            int panelHeight = 200;
            int screenWidth = 1024; // 假设屏幕宽度
            int screenHeight = 768;  // 假设屏幕高度
            
            panelRect = new Rectangle(
                screenWidth - panelWidth - 10,
                screenHeight - panelHeight - 10,
                panelWidth,
                panelHeight
            );

            // 军师信息区域
            advisorInfoRect = new Rectangle(
                panelRect.X + 10,
                panelRect.Y + 10,
                panelRect.Width - 20,
                40
            );

            // 技能按钮布局（2行4列）
            skillButtonRects.Clear();
            skillButtons.Clear();
            
            int buttonWidth = 60;
            int buttonHeight = 30;
            int buttonSpacing = 10;
            int startX = panelRect.X + 10;
            int startY = advisorInfoRect.Bottom + 10;
            
            var availableSkills = inputController.GetAvailableSkills();
            for (int i = 0; i < availableSkills.Count; i++)
            {
                int row = i / 4;
                int col = i % 4;
                
                Rectangle buttonRect = new Rectangle(
                    startX + col * (buttonWidth + buttonSpacing),
                    startY + row * (buttonHeight + buttonSpacing),
                    buttonWidth,
                    buttonHeight
                );
                
                skillButtonRects.Add(buttonRect);
                skillButtons[availableSkills[i]] = buttonRect;
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public void LoadContent(SpriteFont gameFont, GraphicsDevice graphicsDevice)
        {
            font = gameFont;
            
            // 创建纹理
            backgroundTexture = CreateColorTexture(graphicsDevice, Color.DarkBlue);
            buttonTexture = CreateColorTexture(graphicsDevice, Color.Gray);
        }

        /// <summary>
        /// 创建纯色纹理
        /// </summary>
        private Texture2D CreateColorTexture(GraphicsDevice graphicsDevice, Color color)
        {
            var texture = new Texture2D(graphicsDevice, 1, 1);
            texture.SetData(new[] { color });
            return texture;
        }

        /// <summary>
        /// 更新逻辑
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isVisible) return;

            MouseState mouseState = Mouse.GetState();
            bool clicked = mouseState.LeftButton == ButtonState.Pressed && 
                          previousMouseState.LeftButton == ButtonState.Released;

            if (clicked)
            {
                Point mousePos = new Point(mouseState.X, mouseState.Y);
                HandleSkillButtonClick(mousePos);
            }

            previousMouseState = mouseState;
        }

        /// <summary>
        /// 处理技能按钮点击
        /// </summary>
        private void HandleSkillButtonClick(Point mousePos)
        {
            foreach (var kvp in skillButtons)
            {
                if (kvp.Value.Contains(mousePos))
                {
                    SkillType skill = kvp.Key;
                    
                    if (inputController.IsSkillAvailable(skill))
                    {
                        inputController.StartTargetSelection(skill);
                        System.Diagnostics.Debug.WriteLine($"[BattleSkillPanel] 选择技能: {skillInfos[skill].Name}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 绘制面板
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isVisible) return;

            try
            {
                // 绘制面板背景
                spriteBatch.Draw(backgroundTexture, panelRect, Color.White * 0.9f);
                DrawBorder(spriteBatch, panelRect, 2, Color.Gold);

                // 绘制军师信息
                DrawAdvisorInfo(spriteBatch);

                // 绘制技能按钮
                DrawSkillButtons(spriteBatch);

                // 绘制当前选择状态
                if (inputController.IsSelectingTarget)
                {
                    DrawSelectionStatus(spriteBatch);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BattleSkillPanel] 绘制异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制军师信息
        /// </summary>
        private void DrawAdvisorInfo(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(buttonTexture, advisorInfoRect, Color.White * 0.8f);
            DrawBorder(spriteBatch, advisorInfoRect, 1, Color.Black);

            if (playerFaction?.Advisor != null)
            {
                string advisorText = $"军师: {playerFaction.Advisor.Name} (智力{playerFaction.Advisor.Intelligence})";
                Vector2 textPos = new Vector2(advisorInfoRect.X + 5, advisorInfoRect.Y + 5);
                spriteBatch.DrawString(font, advisorText, textPos, Color.Black);

                // 显示预测准确度
                int accuracy = Math.Min(100, playerFaction.Advisor.Intelligence);
                string accuracyText = $"预测准确度: {accuracy}%";
                Vector2 accuracyPos = new Vector2(advisorInfoRect.X + 5, advisorInfoRect.Y + 20);
                Color accuracyColor = accuracy >= 80 ? Color.Green : accuracy >= 60 ? Color.Orange : Color.Red;
                spriteBatch.DrawString(font, accuracyText, accuracyPos, accuracyColor);
            }
            else
            {
                Vector2 noAdvisorPos = new Vector2(advisorInfoRect.X + 5, advisorInfoRect.Y + 10);
                spriteBatch.DrawString(font, "未设军师 - 技能成功率未知", noAdvisorPos, Color.Red);
            }
        }

        /// <summary>
        /// 绘制技能按钮
        /// </summary>
        private void DrawSkillButtons(SpriteBatch spriteBatch)
        {
            foreach (var kvp in skillButtons)
            {
                SkillType skill = kvp.Key;
                Rectangle buttonRect = kvp.Value;
                SkillInfo info = skillInfos[skill];

                // 检查技能是否可用
                bool isAvailable = inputController.IsSkillAvailable(skill);
                bool isSelected = inputController.IsSelectingTarget && inputController.CurrentSkill == skill;

                // 按钮颜色
                Color buttonColor = isAvailable ? info.ButtonColor : Color.Gray;
                if (isSelected) buttonColor = Color.White; // 选中时高亮

                // 绘制按钮
                spriteBatch.Draw(buttonTexture, buttonRect, buttonColor);
                DrawBorder(spriteBatch, buttonRect, isSelected ? 3 : 1, isSelected ? Color.Yellow : Color.Black);

                // 绘制技能名称
                Vector2 textSize = font.MeasureString(info.Name);
                Vector2 textPos = new Vector2(
                    buttonRect.X + (buttonRect.Width - textSize.X) / 2,
                    buttonRect.Y + (buttonRect.Height - textSize.Y) / 2
                );
                
                Color textColor = isAvailable ? Color.White : Color.DarkGray;
                spriteBatch.DrawString(font, info.Name, textPos, textColor);

                // 如果不可用，显示需求
                if (!isAvailable)
                {
                    string requirement = GetSkillRequirement(skill);
                    if (!string.IsNullOrEmpty(requirement))
                    {
                        Vector2 reqPos = new Vector2(buttonRect.X, buttonRect.Bottom + 2);
                        spriteBatch.DrawString(font, requirement, reqPos, Color.Red);
                    }
                }
            }
        }

        /// <summary>
        /// 获取技能需求文本
        /// </summary>
        private string GetSkillRequirement(SkillType skill)
        {
            if (!skillInfos.ContainsKey(skill)) return "";
            
            var info = skillInfos[skill];
            var leader = playerFaction?.Leader;
            if (leader == null) return "";

            if (info.RequiredIntelligence > 0 && leader.Intelligence < info.RequiredIntelligence)
                return $"需智力{info.RequiredIntelligence}";
            if (info.RequiredCommand > 0 && leader.Command < info.RequiredCommand)
                return $"需统率{info.RequiredCommand}";
            if (info.RequiredCharm > 0 && leader.Charm < info.RequiredCharm)
                return $"需魅力{info.RequiredCharm}";

            return "";
        }

        /// <summary>
        /// 绘制选择状态
        /// </summary>
        private void DrawSelectionStatus(SpriteBatch spriteBatch)
        {
            string statusText = $"正在选择 {skillInfos[inputController.CurrentSkill].Name} 的目标...";
            Vector2 statusPos = new Vector2(panelRect.X + 10, panelRect.Bottom + 5);
            
            // 绘制状态背景
            Vector2 textSize = font.MeasureString(statusText);
            Rectangle statusBg = new Rectangle(
                (int)statusPos.X - 5,
                (int)statusPos.Y - 2,
                (int)textSize.X + 10,
                (int)textSize.Y + 4
            );
            spriteBatch.Draw(backgroundTexture, statusBg, Color.Black * 0.8f);
            
            // 绘制状态文字
            spriteBatch.DrawString(font, statusText, statusPos, Color.Yellow);
        }

        /// <summary>
        /// 绘制边框
        /// </summary>
        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
        {
            // 上边框
            spriteBatch.Draw(backgroundTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // 下边框
            spriteBatch.Draw(backgroundTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // 左边框
            spriteBatch.Draw(backgroundTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // 右边框
            spriteBatch.Draw(backgroundTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        /// <summary>
        /// 显示/隐藏面板
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;
        }

        /// <summary>
        /// 面板是否可见
        /// </summary>
        public bool IsVisible => isVisible;
    }
}