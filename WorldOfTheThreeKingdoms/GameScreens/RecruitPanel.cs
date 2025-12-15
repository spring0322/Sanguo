using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameObjects;
using GameManager;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 招募面板 - 集成军师预判系统
    /// </summary>
    public class RecruitPanel
    {
        #region UI组件和布局
        private SpriteFont font;
        private Texture2D backgroundTexture;
        private Texture2D buttonTexture;
        private Texture2D dropdownTexture;
        
        // 面板布局
        private Rectangle panelRect;
        private Rectangle recruiterDropdownRect;
        private Rectangle strategistTextRect;
        private Rectangle strategistFaceRect;
        private Rectangle confirmButtonRect;
        private Rectangle cancelButtonRect;
        private Rectangle detailsButtonRect;
        
        // UI状态
        private bool isVisible = false;
        private bool dropdownOpen = false;
        private int selectedRecruiterIndex = -1;
        private List<Person> availableRecruiters = new List<Person>();
        
        // 鼠标状态
        private MouseState previousMouseState;
        #endregion

        #region 数据
        private Faction myFaction;
        private Person targetOfficer;
        private StrategistManager.PredictionResult currentPrediction;
        private Person recommendedRecruiter;
        private bool showDetails = false;
        #endregion

        public RecruitPanel()
        {
            InitializeLayout();
        }

        /// <summary>
        /// 初始化UI布局
        /// </summary>
        private void InitializeLayout()
        {
            // 面板居中显示
            int panelWidth = 600;
            int panelHeight = 400;
            int screenWidth = 1024; // 假设屏幕宽度
            int screenHeight = 768;  // 假设屏幕高度
            
            panelRect = new Rectangle(
                (screenWidth - panelWidth) / 2,
                (screenHeight - panelHeight) / 2,
                panelWidth,
                panelHeight
            );

            // 各组件布局
            recruiterDropdownRect = new Rectangle(panelRect.X + 20, panelRect.Y + 60, 200, 30);
            strategistFaceRect = new Rectangle(panelRect.X + 20, panelRect.Y + 120, 80, 80);
            strategistTextRect = new Rectangle(panelRect.X + 110, panelRect.Y + 120, 460, 120);
            
            confirmButtonRect = new Rectangle(panelRect.X + 20, panelRect.Y + 320, 100, 40);
            cancelButtonRect = new Rectangle(panelRect.X + 140, panelRect.Y + 320, 100, 40);
            detailsButtonRect = new Rectangle(panelRect.X + 260, panelRect.Y + 320, 120, 40);
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public void LoadContent(GraphicsDevice graphicsDevice, SpriteFont gameFont)
        {
            font = gameFont;
            
            // 创建简单的纹理
            backgroundTexture = CreateColorTexture(graphicsDevice, Color.DarkBlue);
            buttonTexture = CreateColorTexture(graphicsDevice, Color.Gray);
            dropdownTexture = CreateColorTexture(graphicsDevice, Color.LightGray);
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
        /// 当选中招募目标时调用
        /// </summary>
        public void OnTargetSelected(Person target, Faction faction)
        {
            myFaction = faction;
            targetOfficer = target;
            isVisible = true;
            showDetails = false;

            // 获取可用的招募者列表
            availableRecruiters.Clear();
            foreach (var person in faction.Persons)
            {
                if (person != null && person.BelongedFaction == faction && person != target)
                {
                    availableRecruiters.Add(person);
                }
            }

            // 1. 获取推荐招募者
            recommendedRecruiter = RecruitmentSystem.GetRecommendedRecruiter(faction, target);

            // 2. 使用新的招募系统获取军师预测
            if (recommendedRecruiter != null)
            {
                var (predictedRate, comment) = RecruitmentSystem.GetStrategistPrediction(faction.Advisor, recommendedRecruiter, target);
                
                // 创建预测结果结构
                currentPrediction = new StrategistManager.PredictionResult
                {
                    SuccessRate = predictedRate == -1 ? 50 : predictedRate, // -1表示无军师，显示50%
                    Comment = comment,
                    IsImpossible = false // 永远不阻止玩家尝试，给"头铁"的机会
                };
            }
            else
            {
                // 没有可用招募者
                currentPrediction = new StrategistManager.PredictionResult
                {
                    SuccessRate = 0,
                    Comment = "无可用招募者",
                    IsImpossible = true
                };
            }

            // 3. 自动选中推荐人选
            if (recommendedRecruiter != null)
            {
                selectedRecruiterIndex = availableRecruiters.IndexOf(recommendedRecruiter);
            }
            else
            {
                selectedRecruiterIndex = 0; // 默认选择第一个
            }

            System.Diagnostics.Debug.WriteLine($"[RecruitPanel] 目标: {target.Name}, 推荐: {recommendedRecruiter?.Name}, 军师预测: {currentPrediction.SuccessRate}%");
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

                // 确定按钮 - 永远可以点击，给玩家"头铁"的机会
                if (confirmButtonRect.Contains(mousePos))
                {
                    ExecuteRecruitment();
                }
                // 取消按钮
                else if (cancelButtonRect.Contains(mousePos))
                {
                    Hide();
                }
                // 详情按钮
                else if (detailsButtonRect.Contains(mousePos))
                {
                    showDetails = !showDetails;
                }
                // 下拉框
                else if (recruiterDropdownRect.Contains(mousePos))
                {
                    dropdownOpen = !dropdownOpen;
                }
                // 下拉框选项
                else if (dropdownOpen)
                {
                    HandleDropdownSelection(mousePos);
                }
            }

            previousMouseState = mouseState;
        }

        /// <summary>
        /// 处理下拉框选择
        /// </summary>
        private void HandleDropdownSelection(Point mousePos)
        {
            for (int i = 0; i < availableRecruiters.Count; i++)
            {
                Rectangle optionRect = new Rectangle(
                    recruiterDropdownRect.X,
                    recruiterDropdownRect.Y + (i + 1) * 25,
                    recruiterDropdownRect.Width,
                    25
                );

                if (optionRect.Contains(mousePos))
                {
                    selectedRecruiterIndex = i;
                    dropdownOpen = false;
                    
                    // 重新计算预测（如果选择了不同的招募者）
                    if (i < availableRecruiters.Count)
                    {
                        UpdatePredictionForRecruiter(availableRecruiters[i]);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 为特定招募者更新预测
        /// </summary>
        private void UpdatePredictionForRecruiter(Person recruiter)
        {
            if (recruiter == null || targetOfficer == null) return;

            // 使用新的招募系统重新计算预测
            var (predictedRate, comment) = RecruitmentSystem.GetStrategistPrediction(myFaction.Advisor, recruiter, targetOfficer);
            
            currentPrediction = new StrategistManager.PredictionResult
            {
                SuccessRate = predictedRate == -1 ? 50 : predictedRate, // -1表示无军师，显示50%
                Comment = comment,
                IsImpossible = false // 永远不阻止玩家尝试
            };

            System.Diagnostics.Debug.WriteLine($"[RecruitPanel] 更新预测: {recruiter.Name} -> {targetOfficer.Name}, 成功率: {currentPrediction.SuccessRate}%");
        }

        /// <summary>
        /// 执行招募
        /// </summary>
        private void ExecuteRecruitment()
        {
            if (selectedRecruiterIndex >= 0 && selectedRecruiterIndex < availableRecruiters.Count)
            {
                Person selectedRecruiter = availableRecruiters[selectedRecruiterIndex];
                
                System.Diagnostics.Debug.WriteLine($"[RecruitPanel] 执行招募: {selectedRecruiter.Name} 去招募 {targetOfficer.Name}");
                
                // 使用新的招募系统执行招募
                bool success = RecruitmentSystem.ExecuteRecruitment(selectedRecruiter, targetOfficer);
                
                // 处理招募结果
                RecruitmentSystem.HandleRecruitmentResult(selectedRecruiter, targetOfficer, success);
                
                Hide();
            }
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            dropdownOpen = false;
            showDetails = false;
        }

        /// <summary>
        /// 绘制面板
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isVisible) return;

            try
            {
                // 绘制背景
                spriteBatch.Draw(backgroundTexture, panelRect, Color.White * 0.9f);
                DrawBorder(spriteBatch, panelRect, 2, Color.Gold);

                // 绘制标题
                string title = $"招募 {targetOfficer?.Name ?? "未知"}";
                Vector2 titlePos = new Vector2(panelRect.X + 20, panelRect.Y + 10);
                spriteBatch.DrawString(font, title, titlePos, Color.White);

                // 绘制招募者选择
                DrawRecruiterDropdown(spriteBatch);

                // 绘制军师建议
                DrawStrategistAdvice(spriteBatch);

                // 绘制按钮
                DrawButtons(spriteBatch);

                // 绘制详细信息
                if (showDetails)
                {
                    DrawDetailedAnalysis(spriteBatch);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecruitPanel] 绘制异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制招募者下拉框
        /// </summary>
        private void DrawRecruiterDropdown(SpriteBatch spriteBatch)
        {
            // 下拉框背景
            spriteBatch.Draw(dropdownTexture, recruiterDropdownRect, Color.White);
            DrawBorder(spriteBatch, recruiterDropdownRect, 1, Color.Black);

            // 当前选中的招募者
            if (selectedRecruiterIndex >= 0 && selectedRecruiterIndex < availableRecruiters.Count)
            {
                string selectedName = availableRecruiters[selectedRecruiterIndex].Name;
                Vector2 textPos = new Vector2(recruiterDropdownRect.X + 5, recruiterDropdownRect.Y + 5);
                spriteBatch.DrawString(font, selectedName, textPos, Color.Black);
            }

            // 推荐标识
            if (selectedRecruiterIndex >= 0 && selectedRecruiterIndex < availableRecruiters.Count &&
                availableRecruiters[selectedRecruiterIndex] == recommendedRecruiter)
            {
                Vector2 recommendPos = new Vector2(recruiterDropdownRect.Right - 60, recruiterDropdownRect.Y + 5);
                spriteBatch.DrawString(font, "★推荐", recommendPos, Color.Gold);
            }

            // 下拉箭头
            Vector2 arrowPos = new Vector2(recruiterDropdownRect.Right - 20, recruiterDropdownRect.Y + 10);
            spriteBatch.DrawString(font, dropdownOpen ? "▲" : "▼", arrowPos, Color.Black);

            // 下拉选项
            if (dropdownOpen)
            {
                for (int i = 0; i < availableRecruiters.Count; i++)
                {
                    Rectangle optionRect = new Rectangle(
                        recruiterDropdownRect.X,
                        recruiterDropdownRect.Y + (i + 1) * 25,
                        recruiterDropdownRect.Width,
                        25
                    );

                    Color bgColor = (i == selectedRecruiterIndex) ? Color.LightBlue : Color.White;
                    spriteBatch.Draw(dropdownTexture, optionRect, bgColor);
                    DrawBorder(spriteBatch, optionRect, 1, Color.Black);

                    Vector2 optionTextPos = new Vector2(optionRect.X + 5, optionRect.Y + 2);
                    spriteBatch.DrawString(font, availableRecruiters[i].Name, optionTextPos, Color.Black);

                    // 推荐标识
                    if (availableRecruiters[i] == recommendedRecruiter)
                    {
                        Vector2 starPos = new Vector2(optionRect.Right - 60, optionRect.Y + 2);
                        spriteBatch.DrawString(font, "★推荐", starPos, Color.Gold);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制军师建议
        /// </summary>
        private void DrawStrategistAdvice(SpriteBatch spriteBatch)
        {
            if (myFaction?.Advisor != null)
            {
                // 军师头像背景 - 根据预测结果改变背景色
                Color faceBackgroundColor = GetStrategistFaceColor(currentPrediction.SuccessRate);
                spriteBatch.Draw(buttonTexture, strategistFaceRect, faceBackgroundColor);
                DrawBorder(spriteBatch, strategistFaceRect, 2, Color.Black);

                // 军师表情文字显示
                string expression = GetStrategistExpressionText(currentPrediction.SuccessRate);
                Vector2 faceTextPos = new Vector2(strategistFaceRect.X + 5, strategistFaceRect.Y + 20);
                spriteBatch.DrawString(font, "军师", faceTextPos, Color.Black);
                Vector2 expressionPos = new Vector2(strategistFaceRect.X + 5, strategistFaceRect.Y + 45);
                spriteBatch.DrawString(font, expression, expressionPos, Color.Black);

                // 军师评语背景
                spriteBatch.Draw(dropdownTexture, strategistTextRect, Color.White * 0.8f);
                DrawBorder(spriteBatch, strategistTextRect, 1, Color.Black);

                // 军师评语
                string adviceText = $"军师 {myFaction.Advisor.Name}：\n{currentPrediction.Comment}";
                Vector2 advicePos = new Vector2(strategistTextRect.X + 10, strategistTextRect.Y + 10);
                
                // 使用新的颜色系统
                Color textColor = StrategistManager.GetSuccessRateColor(currentPrediction.SuccessRate);
                DrawWrappedText(spriteBatch, adviceText, advicePos, strategistTextRect.Width - 20, textColor);

                // 成功率显示 - 更醒目的显示
                string rateText = $"预测成功率: {currentPrediction.SuccessRate}%";
                Vector2 ratePos = new Vector2(strategistTextRect.X + 10, strategistTextRect.Bottom - 25);
                
                // 成功率文字加粗效果（通过多次绘制实现）
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0) continue;
                        Vector2 shadowPos = new Vector2(ratePos.X + i, ratePos.Y + j);
                        spriteBatch.DrawString(font, rateText, shadowPos, Color.Black * 0.3f);
                    }
                }
                spriteBatch.DrawString(font, rateText, ratePos, textColor);

                // 添加军师智力提示
                if (myFaction.Advisor.Intelligence < 70)
                {
                    string intelligenceWarning = $"(军师智力: {myFaction.Advisor.Intelligence}, 分析可能有误差)";
                    Vector2 warningPos = new Vector2(strategistTextRect.X + 10, strategistTextRect.Bottom - 45);
                    spriteBatch.DrawString(font, intelligenceWarning, warningPos, Color.Orange);
                }
            }
            else
            {
                // 无军师提示
                spriteBatch.Draw(dropdownTexture, strategistTextRect, Color.Gray * 0.5f);
                DrawBorder(spriteBatch, strategistTextRect, 1, Color.Black);
                
                Vector2 noAdvisorPos = new Vector2(strategistTextRect.X + 10, strategistTextRect.Y + 40);
                spriteBatch.DrawString(font, "提示：未设军师，无法预判成功率。", noAdvisorPos, Color.Gray);
                Vector2 blindBoxPos = new Vector2(strategistTextRect.X + 10, strategistTextRect.Y + 60);
                spriteBatch.DrawString(font, "招募结果完全随机！", blindBoxPos, Color.Red);
            }
        }

        /// <summary>
        /// 根据成功率获取军师头像背景色
        /// </summary>
        private Color GetStrategistFaceColor(int successRate)
        {
            if (successRate >= 70) return Color.LightGreen;      // 绿色：军师微笑
            else if (successRate >= 50) return Color.LightYellow; // 黄色：军师中性
            else if (successRate >= 30) return Color.Orange;      // 橙色：军师担忧
            else return Color.LightPink;                          // 红色：军师摇头/流汗
        }

        /// <summary>
        /// 根据成功率获取军师表情文字
        /// </summary>
        private string GetStrategistExpressionText(int successRate)
        {
            if (successRate >= 80) return "😊微笑";
            else if (successRate >= 60) return "😐中性";
            else if (successRate >= 40) return "😟担忧";
            else if (successRate >= 20) return "😰流汗";
            else return "😵摇头";
        }

        /// <summary>
        /// 绘制按钮
        /// </summary>
        private void DrawButtons(SpriteBatch spriteBatch)
        {
            // 确定按钮 - 永远可点击，给玩家"头铁"的机会
            // 但根据成功率改变按钮颜色和文字，给予视觉反馈
            Color confirmColor;
            string confirmText;
            
            if (currentPrediction.SuccessRate >= 70)
            {
                confirmColor = Color.Green;
                confirmText = "执行招募";
            }
            else if (currentPrediction.SuccessRate >= 40)
            {
                confirmColor = Color.Orange;
                confirmText = "尝试招募";
            }
            else if (currentPrediction.SuccessRate >= 20)
            {
                confirmColor = Color.Yellow;
                confirmText = "冒险招募";
            }
            else
            {
                confirmColor = Color.Red;
                confirmText = "强行招募"; // 即使军师说不行，玩家也可以"头铁"尝试
            }
            
            spriteBatch.Draw(buttonTexture, confirmButtonRect, confirmColor);
            DrawBorder(spriteBatch, confirmButtonRect, 2, Color.Black);
            DrawCenteredText(spriteBatch, confirmText, confirmButtonRect, Color.White);

            // 如果成功率极低，添加警告边框闪烁效果
            if (currentPrediction.SuccessRate < 20)
            {
                // 简单的闪烁效果（可以用时间来控制）
                DrawBorder(spriteBatch, confirmButtonRect, 3, Color.Red);
            }

            // 取消按钮
            spriteBatch.Draw(buttonTexture, cancelButtonRect, Color.Gray);
            DrawBorder(spriteBatch, cancelButtonRect, 1, Color.Black);
            DrawCenteredText(spriteBatch, "取消", cancelButtonRect, Color.White);

            // 详情按钮
            spriteBatch.Draw(buttonTexture, detailsButtonRect, Color.Blue);
            DrawBorder(spriteBatch, detailsButtonRect, 1, Color.Black);
            DrawCenteredText(spriteBatch, showDetails ? "隐藏详情" : "详细分析", detailsButtonRect, Color.White);
        }

        /// <summary>
        /// 绘制详细分析
        /// </summary>
        private void DrawDetailedAnalysis(SpriteBatch spriteBatch)
        {
            if (myFaction?.Advisor == null) return;

            Rectangle detailRect = new Rectangle(panelRect.Right + 10, panelRect.Y, 300, panelRect.Height);
            spriteBatch.Draw(backgroundTexture, detailRect, Color.White * 0.9f);
            DrawBorder(spriteBatch, detailRect, 2, Color.Gold);

            Vector2 detailPos = new Vector2(detailRect.X + 10, detailRect.Y + 10);
            
            // 使用新的招募系统获取详细分析
            Person selectedRecruiter = (selectedRecruiterIndex >= 0 && selectedRecruiterIndex < availableRecruiters.Count) 
                ? availableRecruiters[selectedRecruiterIndex] : null;
            
            string analysis = selectedRecruiter != null 
                ? RecruitmentSystem.GetDetailedAnalysis(myFaction.Advisor, selectedRecruiter, targetOfficer)
                : "请先选择招募者";
                
            DrawWrappedText(spriteBatch, analysis, detailPos, detailRect.Width - 20, Color.Black);
        }

        #region 辅助绘制方法
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
        /// 绘制居中文本
        /// </summary>
        private void DrawCenteredText(SpriteBatch spriteBatch, string text, Rectangle rect, Color color)
        {
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2,
                rect.Y + (rect.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, text, textPos, color);
        }

        /// <summary>
        /// 绘制换行文本
        /// </summary>
        private void DrawWrappedText(SpriteBatch spriteBatch, string text, Vector2 position, float maxWidth, Color color)
        {
            string[] lines = text.Split('\n');
            float lineHeight = font.LineSpacing;
            
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 linePos = new Vector2(position.X, position.Y + i * lineHeight);
                spriteBatch.DrawString(font, lines[i], linePos, color);
            }
        }
        #endregion

        /// <summary>
        /// 面板是否可见
        /// </summary>
        public bool IsVisible => isVisible;
    }
}