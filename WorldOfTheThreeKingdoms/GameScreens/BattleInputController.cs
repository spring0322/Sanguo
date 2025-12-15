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
    /// 战斗输入控制器 - 处理战斗中的技能选择和目标指定
    /// </summary>
    public class BattleInputController
    {
        #region 字段和属性
        private Faction playerFaction;
        private SpriteFont font;
        private Texture2D backgroundTexture;
        
        // 技能选择状态
        private bool isSelectingTarget = false;
        private SkillType currentSkill = SkillType.FirePlot;
        private Troop hoveredTroop = null;
        
        // 浮动文本系统
        private FloatingTextManager floatingTextManager;
        
        // 鼠标状态
        private MouseState previousMouseState;
        
        // 可用技能列表
        private List<SkillType> availableSkills = new List<SkillType>();
        #endregion

        public BattleInputController(Faction faction)
        {
            playerFaction = faction;
            floatingTextManager = new FloatingTextManager();
            
            // 初始化可用技能
            InitializeAvailableSkills();
        }

        /// <summary>
        /// 初始化可用技能
        /// </summary>
        private void InitializeAvailableSkills()
        {
            availableSkills.Clear();
            
            // 根据君主能力添加可用技能
            if (playerFaction?.Leader != null)
            {
                Person leader = playerFaction.Leader;
                
                // 智力高的可以使用计策
                if (leader.Intelligence >= 70)
                {
                    availableSkills.Add(SkillType.FirePlot);
                    availableSkills.Add(SkillType.WaterPlot);
                    availableSkills.Add(SkillType.Confuse);
                }
                
                // 统率高的可以使用军事技能
                if (leader.Command >= 70)
                {
                    availableSkills.Add(SkillType.Ambush);
                    availableSkills.Add(SkillType.Rally);
                    availableSkills.Add(SkillType.Retreat);
                }
                
                // 魅力高的可以使用心理战
                if (leader.Charm >= 70)
                {
                    availableSkills.Add(SkillType.Provoke);
                }
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public void LoadContent(SpriteFont gameFont, GraphicsDevice graphicsDevice)
        {
            font = gameFont;
            floatingTextManager.LoadContent(gameFont, graphicsDevice);
            
            // 创建简单背景纹理
            backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { Color.White });
        }

        /// <summary>
        /// 开始选择技能目标
        /// </summary>
        public void StartTargetSelection(SkillType skill)
        {
            currentSkill = skill;
            isSelectingTarget = true;
            
            System.Diagnostics.Debug.WriteLine($"[BattleInput] 开始选择 {GetSkillName(skill)} 的目标");
        }

        /// <summary>
        /// 取消目标选择
        /// </summary>
        public void CancelTargetSelection()
        {
            isSelectingTarget = false;
            hoveredTroop = null;
            floatingTextManager.HideAll();
            
            System.Diagnostics.Debug.WriteLine("[BattleInput] 取消目标选择");
        }

        /// <summary>
        /// 更新逻辑
        /// </summary>
        public void Update(GameTime gameTime, List<Troop> allTroops)
        {
            MouseState mouseState = Mouse.GetState();
            
            // 更新浮动文本
            floatingTextManager.Update(gameTime);
            
            if (isSelectingTarget)
            {
                // 射线检测鼠标指着的单位
                Troop targetTroop = GetTroopUnderMouse(mouseState, allTroops);
                
                if (targetTroop != hoveredTroop)
                {
                    hoveredTroop = targetTroop;
                    UpdateTargetPrediction();
                }
                
                // 检查点击确认目标
                bool clicked = mouseState.LeftButton == ButtonState.Pressed && 
                              previousMouseState.LeftButton == ButtonState.Released;
                
                if (clicked && hoveredTroop != null)
                {
                    ExecuteSkill(hoveredTroop);
                }
                
                // 右键取消
                bool rightClicked = mouseState.RightButton == ButtonState.Pressed && 
                                  previousMouseState.RightButton == ButtonState.Released;
                
                if (rightClicked)
                {
                    CancelTargetSelection();
                }
            }
            
            previousMouseState = mouseState;
        }

        /// <summary>
        /// 获取鼠标下的部队
        /// </summary>
        private Troop GetTroopUnderMouse(MouseState mouseState, List<Troop> allTroops)
        {
            Point mousePos = new Point(mouseState.X, mouseState.Y);
            
            foreach (var troop in allTroops)
            {
                if (troop == null || troop.BelongedFaction == playerFaction) continue;
                
                // 简化的碰撞检测 - 假设每个部队占据一个矩形区域
                Rectangle troopRect = GetTroopBounds(troop);
                if (troopRect.Contains(mousePos))
                {
                    return troop;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 获取部队的边界矩形
        /// </summary>
        private Rectangle GetTroopBounds(Troop troop)
        {
            // 这里应该根据实际的部队渲染位置来计算
            // 暂时返回一个示例矩形
            return new Rectangle(
                (int)troop.RealDestination.X - 25,
                (int)troop.RealDestination.Y - 25,
                50,
                50
            );
        }

        /// <summary>
        /// 更新目标预测显示
        /// </summary>
        private void UpdateTargetPrediction()
        {
            if (hoveredTroop == null)
            {
                floatingTextManager.HideAll();
                return;
            }
            
            // 1. 获取成功率 (带军师误差的!)
            int showRate = StrategistManager.GetBattlePrediction(playerFaction, hoveredTroop, currentSkill);
            
            // 2. 显示在目标头顶
            Vector2 troopPosition = hoveredTroop.RealDestination;
            Vector2 textPosition = new Vector2(troopPosition.X, troopPosition.Y - 40); // 头顶上方
            
            string displayText;
            Color textColor;
            
            if (showRate == -1) // 无军师
            {
                displayText = "??%";
                textColor = Color.Gray;
            }
            else
            {
                displayText = $"{showRate}%";
                textColor = GetSuccessRateColor(showRate);
            }
            
            floatingTextManager.ShowText(textPosition, displayText, textColor, 2.0f);
            
            System.Diagnostics.Debug.WriteLine($"[BattleInput] 显示预测: {hoveredTroop.Leader?.Name} - {displayText}");
        }

        /// <summary>
        /// 执行技能
        /// </summary>
        private void ExecuteSkill(Troop target)
        {
            System.Diagnostics.Debug.WriteLine($"[BattleInput] 对 {target.Leader?.Name} 使用 {GetSkillName(currentSkill)}");
            
            // 这里应该调用实际的技能执行逻辑
            // BattleSystem.ExecuteSkill(playerFaction, target, currentSkill);
            
            // 显示执行结果
            int realSuccessRate = StrategistManager.GetBattlePrediction(playerFaction, target, currentSkill);
            bool success = new Random().Next(100) < realSuccessRate;
            
            Vector2 resultPosition = new Vector2(target.RealDestination.X, target.RealDestination.Y - 60);
            string resultText = success ? "成功!" : "失败!";
            Color resultColor = success ? Color.Green : Color.Red;
            
            floatingTextManager.ShowText(resultPosition, resultText, resultColor, 3.0f);
            
            // 结束目标选择
            CancelTargetSelection();
        }

        /// <summary>
        /// 获取成功率颜色
        /// </summary>
        private Color GetSuccessRateColor(int successRate)
        {
            if (successRate >= 70) return Color.Green;
            else if (successRate >= 50) return Color.Yellow;
            else if (successRate >= 30) return Color.Orange;
            else return Color.Red;
        }

        /// <summary>
        /// 获取技能名称
        /// </summary>
        private string GetSkillName(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.FirePlot: return "火计";
                case SkillType.WaterPlot: return "水计";
                case SkillType.Ambush: return "伏兵";
                case SkillType.Provoke: return "挑衅";
                case SkillType.Confuse: return "混乱";
                case SkillType.Retreat: return "撤退";
                case SkillType.Rally: return "鼓舞";
                default: return "未知";
            }
        }

        /// <summary>
        /// 绘制战斗UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // 绘制浮动文本
            floatingTextManager.Draw(spriteBatch);
            
            // 如果正在选择目标，显示提示信息
            if (isSelectingTarget)
            {
                string hint = $"选择 {GetSkillName(currentSkill)} 的目标 (右键取消)";
                Vector2 hintPos = new Vector2(10, 10);
                
                // 绘制提示背景
                Vector2 textSize = font.MeasureString(hint);
                Rectangle hintBg = new Rectangle(5, 5, (int)textSize.X + 10, (int)textSize.Y + 10);
                spriteBatch.Draw(backgroundTexture, hintBg, Color.Black * 0.7f);
                
                // 绘制提示文字
                spriteBatch.DrawString(font, hint, hintPos, Color.White);
            }
        }

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        public bool IsSkillAvailable(SkillType skill)
        {
            return availableSkills.Contains(skill);
        }

        /// <summary>
        /// 获取可用技能列表
        /// </summary>
        public List<SkillType> GetAvailableSkills()
        {
            return new List<SkillType>(availableSkills);
        }

        /// <summary>
        /// 是否正在选择目标
        /// </summary>
        public bool IsSelectingTarget => isSelectingTarget;

        /// <summary>
        /// 当前选择的技能
        /// </summary>
        public SkillType CurrentSkill => currentSkill;
    }

    /// <summary>
    /// 浮动文本管理器
    /// </summary>
    public class FloatingTextManager
    {
        private struct FloatingText
        {
            public Vector2 Position;
            public string Text;
            public Color Color;
            public float Duration;
            public float TimeLeft;
            public bool IsVisible;
        }

        private List<FloatingText> floatingTexts = new List<FloatingText>();
        private SpriteFont font;
        private Texture2D backgroundTexture;

        public void LoadContent(SpriteFont gameFont, GraphicsDevice graphicsDevice)
        {
            font = gameFont;
            backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { Color.White });
        }

        public void ShowText(Vector2 position, string text, Color color, float duration = 2.0f)
        {
            // 清除旧的文本
            HideAll();
            
            // 添加新的文本
            floatingTexts.Add(new FloatingText
            {
                Position = position,
                Text = text,
                Color = color,
                Duration = duration,
                TimeLeft = duration,
                IsVisible = true
            });
        }

        public void HideAll()
        {
            floatingTexts.Clear();
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                var text = floatingTexts[i];
                text.TimeLeft -= deltaTime;
                
                if (text.TimeLeft <= 0)
                {
                    floatingTexts.RemoveAt(i);
                }
                else
                {
                    // 文字上浮效果
                    text.Position = new Vector2(text.Position.X, text.Position.Y - 20 * deltaTime);
                    floatingTexts[i] = text;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var text in floatingTexts)
            {
                if (!text.IsVisible) continue;
                
                // 计算透明度（随时间淡出）
                float alpha = text.TimeLeft / text.Duration;
                Color drawColor = text.Color * alpha;
                
                // 绘制背景
                Vector2 textSize = font.MeasureString(text.Text);
                Rectangle bgRect = new Rectangle(
                    (int)(text.Position.X - textSize.X / 2 - 5),
                    (int)(text.Position.Y - textSize.Y / 2 - 2),
                    (int)textSize.X + 10,
                    (int)textSize.Y + 4
                );
                spriteBatch.Draw(backgroundTexture, bgRect, Color.Black * (alpha * 0.7f));
                
                // 绘制文字
                Vector2 textPos = new Vector2(
                    text.Position.X - textSize.X / 2,
                    text.Position.Y - textSize.Y / 2
                );
                spriteBatch.DrawString(font, text.Text, textPos, drawColor);
            }
        }
    }
}