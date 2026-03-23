using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WorldOfTheThreeKingdoms;

// 使用别名避免命名冲突
using Session = GameManager.Session;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 军师策略建议界面
    /// </summary>
    public class StrategyAdviceUI
    {
        private bool isVisible = false;
        private Rectangle windowRect;
        private Faction currentFaction;
        private Person currentAdvisor;
        private StrategyKind selectedStrategy = StrategyKind.Gossip;
        private StrategyAdviceResult currentAdvice;
        private object currentTarget;

        // UI 组件位置
        private Rectangle strategyListRect;
        private Rectangle adviceTextRect;
        private Rectangle candidateInfoRect;
        private Rectangle closeButtonRect;

        public bool IsVisible => isVisible;

        /// <summary>
        /// 显示策略建议界面
        /// </summary>
        public void Show(Faction faction, Person advisor, object target = null)
        {
            currentFaction = faction;
            currentAdvisor = advisor;
            currentTarget = target;
            isVisible = true;

            // 设置界面位置和大小
            int screenWidth = Session.MainGame.GraphicsDevice.Viewport.Width;
            int screenHeight = Session.MainGame.GraphicsDevice.Viewport.Height;
            
            windowRect = new Rectangle(
                screenWidth / 2 - 400,
                screenHeight / 2 - 300,
                800,
                600
            );

            // 设置子组件位置
            strategyListRect = new Rectangle(windowRect.X + 20, windowRect.Y + 60, 200, 400);
            adviceTextRect = new Rectangle(windowRect.X + 240, windowRect.Y + 60, 540, 200);
            candidateInfoRect = new Rectangle(windowRect.X + 240, windowRect.Y + 280, 540, 180);
            closeButtonRect = new Rectangle(windowRect.X + windowRect.Width - 80, windowRect.Y + 10, 60, 30);

            // 获取初始建议
            RefreshAdvice();

            System.Diagnostics.Debug.WriteLine($"[策略建议界面] 已显示，当前军师: {advisor?.Name ?? "无"}");
        }

        /// <summary>
        /// 隐藏界面
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            System.Diagnostics.Debug.WriteLine("[策略建议界面] 已隐藏");
        }

        /// <summary>
        /// 刷新建议内容
        /// </summary>
        private void RefreshAdvice()
        {
            if (currentAdvisor == null || currentFaction == null) return;

            currentAdvice = AdvisorStrategySystem.GetAdvice(
                currentAdvisor, 
                currentFaction, 
                currentTarget, 
                selectedStrategy
            );

            System.Diagnostics.Debug.WriteLine($"[策略建议] 已刷新 {selectedStrategy} 的建议");
        }

        /// <summary>
        /// 处理鼠标点击
        /// </summary>
        public bool HandleMouseClick(int x, int y)
        {
            if (!isVisible) return false;

            // 检查是否点击了关闭按钮
            if (closeButtonRect.Contains(x, y))
            {
                Hide();
                return true;
            }

            // 检查是否点击了策略列表
            if (strategyListRect.Contains(x, y))
            {
                int itemHeight = 50;
                int clickedIndex = (y - strategyListRect.Y) / itemHeight;
                
                var strategies = Enum.GetValues(typeof(StrategyKind)).Cast<StrategyKind>().ToArray();
                if (clickedIndex >= 0 && clickedIndex < strategies.Length)
                {
                    selectedStrategy = strategies[clickedIndex];
                    RefreshAdvice();
                    System.Diagnostics.Debug.WriteLine($"[策略建议] 选择了策略: {selectedStrategy}");
                }
                return true;
            }

            // 检查是否点击在窗口内（阻止点击穿透）
            return windowRect.Contains(x, y);
        }

        /// <summary>
        /// 绘制界面
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D backgroundTexture, Texture2D buttonTexture)
        {
            if (!isVisible) return;

            // 绘制背景窗口
            spriteBatch.Draw(backgroundTexture, windowRect, Color.White);

            // 绘制标题
            string title = $"军师策略建议 - {currentAdvisor?.Name ?? "无军师"}";
            spriteBatch.DrawString(font, title, new Vector2(windowRect.X + 20, windowRect.Y + 20), Color.Black);

            // 绘制关闭按钮
            spriteBatch.Draw(buttonTexture, closeButtonRect, Color.LightGray);
            spriteBatch.DrawString(font, "关闭", new Vector2(closeButtonRect.X + 10, closeButtonRect.Y + 5), Color.Black);

            // 绘制策略列表
            DrawStrategyList(spriteBatch, font, buttonTexture);

            // 绘制建议内容
            DrawAdviceContent(spriteBatch, font);

            // 绘制候选人信息
            DrawCandidateInfo(spriteBatch, font);
        }

        /// <summary>
        /// 绘制策略列表
        /// </summary>
        private void DrawStrategyList(SpriteBatch spriteBatch, SpriteFont font, Texture2D buttonTexture)
        {
            var strategies = Enum.GetValues(typeof(StrategyKind)).Cast<StrategyKind>().ToArray();
            int itemHeight = 50;

            for (int i = 0; i < strategies.Length; i++)
            {
                var strategy = strategies[i];
                var itemRect = new Rectangle(
                    strategyListRect.X,
                    strategyListRect.Y + i * itemHeight,
                    strategyListRect.Width,
                    itemHeight - 5
                );

                // 高亮选中的策略
                Color bgColor = (strategy == selectedStrategy) ? Color.LightBlue : Color.LightGray;
                spriteBatch.Draw(buttonTexture, itemRect, bgColor);

                // 绘制策略名称
                string strategyName = AdvisorStrategySystem.GetStrategyName(strategy);
                spriteBatch.DrawString(font, strategyName, 
                    new Vector2(itemRect.X + 10, itemRect.Y + 10), Color.Black);

                // 绘制策略说明（小字）
                string description = AdvisorStrategySystem.GetStrategyDescription(strategy);
                if (description.Length > 20) description = description.Substring(0, 20) + "...";
                spriteBatch.DrawString(font, description, 
                    new Vector2(itemRect.X + 10, itemRect.Y + 25), Color.DarkGray);
            }
        }

        /// <summary>
        /// 绘制建议内容
        /// </summary>
        private void DrawAdviceContent(SpriteBatch spriteBatch, SpriteFont font)
        {
            // 绘制建议文本框背景 - 使用简单的颜色填充
            var backgroundTexture = new Texture2D(Session.MainGame.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { Color.LightYellow });
            spriteBatch.Draw(backgroundTexture, adviceTextRect, Color.White);

            if (currentAdvice.AdvisorComment != null)
            {
                // 分行显示建议文本
                string[] lines = currentAdvice.AdvisorComment.Split('\n');
                for (int i = 0; i < lines.Length && i < 8; i++) // 最多显示8行
                {
                    spriteBatch.DrawString(font, lines[i], 
                        new Vector2(adviceTextRect.X + 10, adviceTextRect.Y + 10 + i * 20), Color.Black);
                }
            }
        }

        /// <summary>
        /// 绘制候选人信息
        /// </summary>
        private void DrawCandidateInfo(SpriteBatch spriteBatch, SpriteFont font)
        {
            // 绘制候选人信息框背景 - 使用简单的颜色填充
            var backgroundTexture = new Texture2D(Session.MainGame.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { Color.LightCyan });
            spriteBatch.Draw(backgroundTexture, candidateInfoRect, Color.White);

            if (currentAdvice.BestCandidate != null)
            {
                var candidate = currentAdvice.BestCandidate;
                int yOffset = 10;

                // 候选人姓名
                spriteBatch.DrawString(font, $"推荐人选: {candidate.Name}", 
                    new Vector2(candidateInfoRect.X + 10, candidateInfoRect.Y + yOffset), Color.Black);
                yOffset += 25;

                // 属性信息
                spriteBatch.DrawString(font, $"智力: {candidate.Intelligence}  魅力: {candidate.Glamour}", 
                    new Vector2(candidateInfoRect.X + 10, candidateInfoRect.Y + yOffset), Color.Black);
                yOffset += 20;

                spriteBatch.DrawString(font, $"统率: {candidate.Command}  政治: {candidate.Politics}", 
                    new Vector2(candidateInfoRect.X + 10, candidateInfoRect.Y + yOffset), Color.Black);
                yOffset += 20;

                // 预测成功率
                Color chanceColor = currentAdvice.PredictedChance >= 70 ? Color.Green : 
                                   currentAdvice.PredictedChance >= 40 ? Color.Orange : Color.Red;
                spriteBatch.DrawString(font, $"预测成功率: {currentAdvice.PredictedChance}%", 
                    new Vector2(candidateInfoRect.X + 10, candidateInfoRect.Y + yOffset), chanceColor);
                yOffset += 25;

                // 策略说明
                string description = AdvisorStrategySystem.GetStrategyDescription(selectedStrategy);
                string[] descLines = WrapText(description, 60); // 60字符换行
                for (int i = 0; i < descLines.Length && i < 3; i++) // 最多3行
                {
                    spriteBatch.DrawString(font, descLines[i], 
                        new Vector2(candidateInfoRect.X + 10, candidateInfoRect.Y + yOffset + i * 18), Color.DarkBlue);
                }
            }
            else
            {
                spriteBatch.DrawString(font, "当前无可用人员执行此策略", 
                    new Vector2(candidateInfoRect.X + 10, candidateInfoRect.Y + 10), Color.Red);
            }
        }

        /// <summary>
        /// 文本换行处理
        /// </summary>
        private string[] WrapText(string text, int maxLength)
        {
            if (text.Length <= maxLength) return new[] { text };

            var lines = new List<string>();
            int start = 0;
            while (start < text.Length)
            {
                int length = Math.Min(maxLength, text.Length - start);
                lines.Add(text.Substring(start, length));
                start += length;
            }
            return lines.ToArray();
        }

        /// <summary>
        /// 更新逻辑
        /// </summary>
        public void Update()
        {
            // 这里可以添加动画或其他更新逻辑
        }
    }
}