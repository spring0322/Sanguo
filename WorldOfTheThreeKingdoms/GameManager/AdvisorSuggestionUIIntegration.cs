using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 军师建议UI集成示例
    /// 展示如何在MainGameScreen中集成建议缓存系统
    /// </summary>
    public static class AdvisorSuggestionUIIntegration
    {
        /// <summary>
        /// 更新军师按钮显示状态
        /// 这个方法应该在MainGameScreen的Update或Draw方法中调用
        /// </summary>
        /// <param name="currentFaction">当前势力</param>
        /// <param name="advisorButton">军师按钮的矩形区域</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="buttonTexture">按钮纹理</param>
        /// <param name="font">字体</param>
        public static void UpdateAdvisorButton(Faction currentFaction, Rectangle advisorButton, 
            SpriteBatch spriteBatch, Texture2D buttonTexture, SpriteFont font)
        {
            if (currentFaction == null || currentFaction.Advisor == null) return;

            // 检查是否有建议 (这个调用非常高效，因为使用了缓存)
            var suggestionKind = currentFaction.CheckAdvisorHasSuggestion();
            
            // 判定是否高亮按钮
            bool showGoldBorder = (suggestionKind != AdvisorSuggestionKind.None);
            
            // 绘制按钮基础纹理
            spriteBatch.Draw(buttonTexture, advisorButton, Color.White);
            
            // 如果有建议，绘制金色边框
            if (showGoldBorder)
            {
                DrawGoldBorder(spriteBatch, advisorButton, currentFaction.GetSuggestionUrgencyColor());
                
                // 绘制建议类型图标
                DrawSuggestionIcon(spriteBatch, advisorButton, suggestionKind, font);
                
                // 可选：绘制闪烁效果
                if (IsHighUrgency(currentFaction.CurrentSuggestionDetails))
                {
                    DrawBlinkingEffect(spriteBatch, advisorButton);
                }
            }
            
            // 绘制军师头像和名称
            DrawAdvisorInfo(spriteBatch, advisorButton, currentFaction.Advisor, font);
        }

        /// <summary>
        /// 显示军师建议详细信息
        /// 当玩家点击军师按钮时调用
        /// </summary>
        /// <param name="currentFaction">当前势力</param>
        /// <returns>是否显示了建议</returns>
        public static bool ShowAdvisorSuggestionDialog(Faction currentFaction)
        {
            if (currentFaction?.Advisor == null) return false;

            var suggestionKind = currentFaction.CheckAdvisorHasSuggestion();
            
            if (suggestionKind == AdvisorSuggestionKind.None)
            {
                // 显示无建议的对话
                ShowNoSuggestionDialog(currentFaction);
                return false;
            }

            // 显示建议详情对话
            ShowSuggestionDetailDialog(currentFaction);
            return true;
        }

        /// <summary>
        /// 处理玩家行动后的建议更新
        /// 在玩家执行招募、建设等行动后调用
        /// </summary>
        /// <param name="currentFaction">当前势力</param>
        /// <param name="actionType">行动类型</param>
        public static void OnPlayerActionCompleted(Faction currentFaction, string actionType)
        {
            if (currentFaction == null) return;

            // 检查建议是否已解决
            currentFaction.CheckAdviceResolved();

            // 根据行动类型进行特定检查
            switch (actionType.ToLower())
            {
                case "recruit":
                case "招募":
                    // 招募行动完成，检查招募建议是否解决
                    if (currentFaction.CurrentRoundSuggestion == AdvisorSuggestionKind.Personnel)
                    {
                        currentFaction.CheckAdviceResolved();
                    }
                    break;

                case "build":
                case "建设":
                    // 建设行动完成，检查内政建议是否解决
                    if (currentFaction.CurrentRoundSuggestion == AdvisorSuggestionKind.Internal)
                    {
                        currentFaction.CheckAdviceResolved();
                    }
                    break;

                case "battle":
                case "战斗":
                    // 战斗结束，检查军事相关建议是否解决
                    if (currentFaction.CurrentRoundSuggestion == AdvisorSuggestionKind.Emergency ||
                        currentFaction.CurrentRoundSuggestion == AdvisorSuggestionKind.Military)
                    {
                        currentFaction.CheckAdviceResolved();
                    }
                    break;
            }
        }

        /// <summary>
        /// 回合开始时刷新建议
        /// 在新回合开始时调用
        /// </summary>
        /// <param name="currentFaction">当前势力</param>
        public static void OnTurnStart(Faction currentFaction)
        {
            if (currentFaction?.Advisor == null) return;

            // 强制刷新建议 (会自动检查回合数)
            currentFaction.RefreshAdvisorSuggestion();

            // 如果有新建议，可以显示通知
            if (currentFaction.CurrentRoundSuggestion != AdvisorSuggestionKind.None)
            {
                ShowNewSuggestionNotification(currentFaction);
            }
        }

        #region 私有绘制方法

        /// <summary>
        /// 绘制金色边框
        /// </summary>
        private static void DrawGoldBorder(SpriteBatch spriteBatch, Rectangle rect, Color urgencyColor)
        {
            int borderWidth = 3;
            
            // 上边框
            spriteBatch.Draw(GetPixelTexture(), new Rectangle(rect.X, rect.Y, rect.Width, borderWidth), urgencyColor);
            // 下边框
            spriteBatch.Draw(GetPixelTexture(), new Rectangle(rect.X, rect.Bottom - borderWidth, rect.Width, borderWidth), urgencyColor);
            // 左边框
            spriteBatch.Draw(GetPixelTexture(), new Rectangle(rect.X, rect.Y, borderWidth, rect.Height), urgencyColor);
            // 右边框
            spriteBatch.Draw(GetPixelTexture(), new Rectangle(rect.Right - borderWidth, rect.Y, borderWidth, rect.Height), urgencyColor);
        }

        /// <summary>
        /// 绘制建议类型图标
        /// </summary>
        private static void DrawSuggestionIcon(SpriteBatch spriteBatch, Rectangle rect, AdvisorSuggestionKind kind, SpriteFont font)
        {
            string iconText = GetSuggestionIcon(kind);
            Vector2 iconSize = font.MeasureString(iconText);
            Vector2 iconPosition = new Vector2(
                rect.Right - iconSize.X - 5,
                rect.Y + 5
            );
            
            // 绘制图标背景
            Rectangle iconBg = new Rectangle(
                (int)iconPosition.X - 2,
                (int)iconPosition.Y - 2,
                (int)iconSize.X + 4,
                (int)iconSize.Y + 4
            );
            spriteBatch.Draw(GetPixelTexture(), iconBg, Color.Black * 0.7f);
            
            // 绘制图标文字
            spriteBatch.DrawString(font, iconText, iconPosition, Color.White);
        }

        /// <summary>
        /// 绘制闪烁效果
        /// </summary>
        private static void DrawBlinkingEffect(SpriteBatch spriteBatch, Rectangle rect)
        {
            // 简单的闪烁效果：根据时间计算透明度
            float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            float alpha = (float)(Math.Sin(time * 4) * 0.3 + 0.7); // 0.4 到 1.0 之间闪烁
            
            spriteBatch.Draw(GetPixelTexture(), rect, Color.Yellow * alpha * 0.3f);
        }

        /// <summary>
        /// 绘制军师信息
        /// </summary>
        private static void DrawAdvisorInfo(SpriteBatch spriteBatch, Rectangle rect, Person advisor, SpriteFont font)
        {
            if (advisor == null) return;

            // 绘制军师名称
            string advisorName = advisor.Name;
            Vector2 nameSize = font.MeasureString(advisorName);
            Vector2 namePosition = new Vector2(
                rect.X + (rect.Width - nameSize.X) / 2,
                rect.Bottom - nameSize.Y - 5
            );
            
            // 名称背景
            Rectangle nameBg = new Rectangle(
                (int)namePosition.X - 2,
                (int)namePosition.Y - 2,
                (int)nameSize.X + 4,
                (int)nameSize.Y + 4
            );
            spriteBatch.Draw(GetPixelTexture(), nameBg, Color.Black * 0.8f);
            
            // 名称文字
            spriteBatch.DrawString(font, advisorName, namePosition, Color.White);
        }

        #endregion

        #region 对话框方法

        /// <summary>
        /// 显示无建议对话
        /// </summary>
        private static void ShowNoSuggestionDialog(Faction faction)
        {
            string message = $"军师 {faction.Advisor.Name}：\n" +
                           "主公，当前形势良好，暂无特别建议。\n" +
                           "可继续按既定方针治理国政。";
            
            // 这里应该调用游戏的对话框系统
            ShowGameDialog("军师建议", message);
        }

        /// <summary>
        /// 显示建议详情对话
        /// </summary>
        private static void ShowSuggestionDetailDialog(Faction faction)
        {
            if (faction.CurrentSuggestionDetails == null) return;

            var suggestion = faction.CurrentSuggestionDetails;
            string title = $"军师建议 - {suggestion.Title}";
            string message = faction.GetCurrentSuggestionText();
            
            // 添加紧急程度提示
            if (suggestion.Urgency >= 8)
            {
                message = "【紧急】" + message;
            }
            else if (suggestion.Urgency >= 6)
            {
                message = "【重要】" + message;
            }
            
            // 这里应该调用游戏的对话框系统
            ShowGameDialog(title, message);
        }

        /// <summary>
        /// 显示新建议通知
        /// </summary>
        private static void ShowNewSuggestionNotification(Faction faction)
        {
            if (faction.CurrentSuggestionDetails == null) return;

            string notification = $"军师 {faction.Advisor.Name} 有新建议：{faction.CurrentSuggestionDetails.Title}";
            
            // 这里应该调用游戏的通知系统
            ShowGameNotification(notification, faction.GetSuggestionUrgencyColor());
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取建议类型图标
        /// </summary>
        private static string GetSuggestionIcon(AdvisorSuggestionKind kind)
        {
            switch (kind)
            {
                case AdvisorSuggestionKind.Emergency: return "⚔";
                case AdvisorSuggestionKind.Personnel: return "👤";
                case AdvisorSuggestionKind.Internal: return "🏛";
                case AdvisorSuggestionKind.Diplomatic: return "🤝";
                case AdvisorSuggestionKind.Military: return "⚡";
                case AdvisorSuggestionKind.Technology: return "📚";
                case AdvisorSuggestionKind.Intelligence: return "🔍";
                case AdvisorSuggestionKind.Strategic: return "🎯";
                default: return "💭";
            }
        }

        /// <summary>
        /// 检查是否为高紧急度建议
        /// </summary>
        private static bool IsHighUrgency(AdvisorSuggestion suggestion)
        {
            return suggestion?.Urgency >= 8;
        }

        /// <summary>
        /// 获取像素纹理 (用于绘制边框)
        /// </summary>
        private static Texture2D GetPixelTexture()
        {
            // 这里应该返回游戏中的1x1像素纹理
            // 实际实现中需要从游戏的纹理管理器获取
            return null; // 占位符
        }

        /// <summary>
        /// 显示游戏对话框 (占位符方法)
        /// </summary>
        private static void ShowGameDialog(string title, string message)
        {
            // 实际实现中应该调用游戏的对话框系统
            Console.WriteLine($"[对话框] {title}: {message}");
        }

        /// <summary>
        /// 显示游戏通知 (占位符方法)
        /// </summary>
        private static void ShowGameNotification(string message, Color color)
        {
            // 实际实现中应该调用游戏的通知系统
            Console.WriteLine($"[通知] {message}");
        }

        #endregion

        #region 使用示例

        /// <summary>
        /// 使用示例：在MainGameScreen中的集成代码
        /// </summary>
        public static void ExampleIntegration()
        {
            /*
            // 在MainGameScreen的Update方法中：
            public void Update(GameTime gameTime)
            {
                var currentFaction = Session.Current.Scenario.CurrentFaction;
                
                // 检查是否需要刷新建议 (每回合自动检查)
                if (IsNewTurn())
                {
                    AdvisorSuggestionUIIntegration.OnTurnStart(currentFaction);
                }
                
                // 其他更新逻辑...
            }

            // 在MainGameScreen的Draw方法中：
            public void Draw(SpriteBatch spriteBatch)
            {
                var currentFaction = Session.Current.Scenario.CurrentFaction;
                
                // 绘制军师按钮 (会自动显示建议状态)
                AdvisorSuggestionUIIntegration.UpdateAdvisorButton(
                    currentFaction, advisorButtonRect, spriteBatch, buttonTexture, font);
                
                // 其他绘制逻辑...
            }

            // 在处理按钮点击时：
            private void OnAdvisorButtonClicked()
            {
                var currentFaction = Session.Current.Scenario.CurrentFaction;
                
                // 显示军师建议对话
                bool hasAdvice = AdvisorSuggestionUIIntegration.ShowAdvisorSuggestionDialog(currentFaction);
                
                if (!hasAdvice)
                {
                    // 如果没有建议，可以显示军师的其他功能
                    ShowAdvisorMenu();
                }
            }

            // 在玩家完成行动后：
            private void OnPlayerActionCompleted(string actionType)
            {
                var currentFaction = Session.Current.Scenario.CurrentFaction;
                
                // 检查建议是否已解决
                AdvisorSuggestionUIIntegration.OnPlayerActionCompleted(currentFaction, actionType);
            }
            */
        }

        #endregion
    }
}