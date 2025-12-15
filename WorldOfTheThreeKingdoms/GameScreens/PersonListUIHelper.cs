using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameGlobal;

namespace GameScreens
{
    /// <summary>
    /// 人员列表UI辅助类
    /// 提供基于军师感知的人员信息显示功能
    /// </summary>
    public static class PersonListUIHelper
    {
        /// <summary>
        /// 绘制人员行（基于军师感知）
        /// </summary>
        /// <param name="target">目标人物</param>
        /// <param name="position">绘制位置</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="font">字体</param>
        public static void DrawPersonRow(Person target, Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                if (target == null) return;

                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;
                
                float columnWidth = 80f;
                Vector2 currentPos = position;

                // 绘制姓名（始终显示真实姓名）
                DrawText(spriteBatch, font, target.Name, currentPos, Color.White);
                currentPos.X += columnWidth;

                // 绘制忠诚度
                DrawLoyaltyColumn(target, advisor, faction, currentPos, spriteBatch, font);
                currentPos.X += columnWidth;

                // 绘制能力值（如果有军师）
                if (faction?.IsPlayer == true && advisor != null)
                {
                    DrawAbilityColumns(target, advisor, currentPos, spriteBatch, font);
                }
                else
                {
                    DrawTrueAbilityColumns(target, currentPos, spriteBatch, font);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI] DrawPersonRow 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制忠诚度列
        /// </summary>
        private static void DrawLoyaltyColumn(Person target, Person advisor, Faction faction, Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            string loyaltyText;
            Color textColor = Color.White;
            
            if (faction?.IsPlayer == true && advisor != null)
            {
                // 玩家且有军师 -> 使用军师的眼光
                loyaltyText = AdvisorDataHelper.GetLoyaltyString(advisor, target);
                
                // 根据军师智力设置颜色
                textColor = GetLoyaltyTextColor(advisor);
                
                // 添加可信度提示
                if (advisor.Intelligence < 80)
                {
                    loyaltyText += GetReliabilityIndicator(advisor);
                }
            }
            else if (faction?.IsPlayer == true)
            {
                // 玩家无军师 -> 显示问号
                loyaltyText = "???";
                textColor = Color.Gray;
            }
            else
            {
                // AI势力或上帝模式 -> 显示真实值
                loyaltyText = target.Loyalty.ToString();
                textColor = GetLoyaltyValueColor(target.Loyalty);
            }

            DrawText(spriteBatch, font, loyaltyText, position, textColor);
        }

        /// <summary>
        /// 绘制能力值列（军师观测版本）
        /// </summary>
        private static void DrawAbilityColumns(Person target, Person advisor, Vector2 startPosition, SpriteBatch spriteBatch, SpriteFont font)
        {
            float columnWidth = 60f;
            Vector2 currentPos = startPosition;

            var abilities = new[]
            {
                ("Intelligence", target.Intelligence),
                ("Command", target.Command),
                ("Strength", target.Strength),
                ("Politics", target.Politics)
            };

            foreach (var (abilityName, realValue) in abilities)
            {
                int observedValue = AdvisorDataHelper.GetObservedAbility(advisor, target, abilityName);
                string displayText = FormatAbilityValue(observedValue, advisor.Intelligence);
                Color textColor = GetAbilityTextColor(advisor, realValue, observedValue);
                
                DrawText(spriteBatch, font, displayText, currentPos, textColor);
                currentPos.X += columnWidth;
            }
        }

        /// <summary>
        /// 绘制能力值列（真实值版本）
        /// </summary>
        private static void DrawTrueAbilityColumns(Person target, Vector2 startPosition, SpriteBatch spriteBatch, SpriteFont font)
        {
            float columnWidth = 60f;
            Vector2 currentPos = startPosition;

            var abilities = new[] { target.Intelligence, target.Command, target.Strength, target.Politics };

            foreach (int value in abilities)
            {
                Color textColor = GetAbilityValueColor(value);
                DrawText(spriteBatch, font, value.ToString(), currentPos, textColor);
                currentPos.X += columnWidth;
            }
        }

        /// <summary>
        /// 获取忠诚度文本颜色
        /// </summary>
        private static Color GetLoyaltyTextColor(Person advisor)
        {
            return advisor.Intelligence switch
            {
                >= 100 => Color.Gold,        // 神算 - 金色
                >= 90 => Color.LightBlue,    // 高智力 - 浅蓝
                >= 80 => Color.White,        // 中等智力 - 白色
                >= 60 => Color.LightGray,    // 较低智力 - 浅灰
                _ => Color.Gray               // 低智力 - 灰色
            };
        }

        /// <summary>
        /// 获取忠诚度数值颜色
        /// </summary>
        private static Color GetLoyaltyValueColor(int loyalty)
        {
            return loyalty switch
            {
                >= 90 => Color.Green,
                >= 70 => Color.Yellow,
                >= 50 => Color.Orange,
                _ => Color.Red
            };
        }

        /// <summary>
        /// 获取能力值文本颜色
        /// </summary>
        private static Color GetAbilityTextColor(Person advisor, int realValue, int observedValue)
        {
            // 如果是神算军师，使用特殊颜色
            if (advisor.Intelligence >= 100)
            {
                return Color.Gold;
            }

            // 根据观测值设置颜色
            return GetAbilityValueColor(observedValue);
        }

        /// <summary>
        /// 获取能力值颜色
        /// </summary>
        private static Color GetAbilityValueColor(int value)
        {
            return value switch
            {
                >= 90 => Color.Purple,    // 超高
                >= 80 => Color.Blue,      // 很高
                >= 70 => Color.Green,     // 较高
                >= 60 => Color.Yellow,    // 中等
                >= 50 => Color.Orange,    // 较低
                _ => Color.Red            // 低
            };
        }

        /// <summary>
        /// 格式化能力值显示
        /// </summary>
        private static string FormatAbilityValue(int value, int advisorIntelligence)
        {
            // 根据军师智力决定显示精度
            return advisorIntelligence switch
            {
                >= 95 => value.ToString(),                    // 精确值
                >= 80 => value.ToString(),                    // 数值（有误差但不显示）
                >= 60 => $"{Math.Max(0, value - 5)}~{Math.Min(100, value + 5)}", // 区间
                _ => GetAbilityDescription(value)             // 文字描述
            };
        }

        /// <summary>
        /// 获取能力描述
        /// </summary>
        private static string GetAbilityDescription(int value)
        {
            return value switch
            {
                >= 90 => "极高",
                >= 80 => "很高",
                >= 70 => "较高",
                >= 60 => "中等",
                >= 50 => "较低",
                _ => "很低"
            };
        }

        /// <summary>
        /// 获取可信度指示器
        /// </summary>
        private static string GetReliabilityIndicator(Person advisor)
        {
            return advisor.Intelligence switch
            {
                >= 80 => "",           // 高智力不显示
                >= 70 => " (?)",       // 中等智力显示问号
                >= 60 => " (??)",      // 较低智力显示双问号
                _ => " (???)"          // 低智力显示三问号
            };
        }

        /// <summary>
        /// 绘制文本的辅助方法
        /// </summary>
        private static void DrawText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color)
        {
            if (spriteBatch != null && font != null && !string.IsNullOrEmpty(text))
            {
                spriteBatch.DrawString(font, text, position, color);
            }
        }

        /// <summary>
        /// 绘制带背景的人员行
        /// </summary>
        public static void DrawPersonRowWithBackground(Person target, Rectangle bounds, SpriteBatch spriteBatch, SpriteFont font, Texture2D backgroundTexture)
        {
            try
            {
                // 绘制背景
                if (backgroundTexture != null)
                {
                    Color bgColor = GetPersonRowBackgroundColor(target);
                    spriteBatch.Draw(backgroundTexture, bounds, bgColor);
                }

                // 绘制内容
                Vector2 contentPosition = new Vector2(bounds.X + 5, bounds.Y + 5);
                DrawPersonRow(target, contentPosition, spriteBatch, font);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI] DrawPersonRowWithBackground 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取人员行背景颜色
        /// </summary>
        private static Color GetPersonRowBackgroundColor(Person target)
        {
            Faction faction = Session.Current.Scenario.CurrentFaction;
            Person advisor = faction?.Advisor;

            if (faction?.IsPlayer == true && advisor != null)
            {
                // 基于军师观测的忠诚度设置背景色
                int observedLoyalty = AdvisorDataHelper.GetObservedValue(advisor, target, target.Loyalty, "Loyalty");
                
                return observedLoyalty switch
                {
                    >= 90 => Color.DarkGreen * 0.3f,    // 深绿背景
                    >= 70 => Color.DarkBlue * 0.3f,     // 深蓝背景
                    >= 50 => Color.DarkOrange * 0.3f,   // 深橙背景
                    _ => Color.DarkRed * 0.3f           // 深红背景
                };
            }

            return Color.Transparent;
        }

        /// <summary>
        /// 绘制军师信息提示
        /// </summary>
        public static void DrawAdvisorInfo(Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;

                if (faction?.IsPlayer == true && advisor != null)
                {
                    string infoText = $"军师: {advisor.Name} (智力 {advisor.Intelligence})";
                    string accuracyText = AdvisorDataHelper.GetAccuracyAssessment(advisor);
                    
                    DrawText(spriteBatch, font, infoText, position, Color.LightBlue);
                    DrawText(spriteBatch, font, accuracyText, new Vector2(position.X, position.Y + 20), Color.Gray);
                }
                else if (faction?.IsPlayer == true)
                {
                    DrawText(spriteBatch, font, "无军师 - 信息不明", position, Color.Red);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI] DrawAdvisorInfo 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制列表标题
        /// </summary>
        public static void DrawListHeader(Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                float columnWidth = 80f;
                Vector2 currentPos = position;
                Color headerColor = Color.Yellow;

                var headers = new[] { "姓名", "忠诚", "智力", "统率", "武力", "政治" };

                foreach (string header in headers)
                {
                    DrawText(spriteBatch, font, header, currentPos, headerColor);
                    currentPos.X += (header == "姓名" || header == "忠诚") ? columnWidth : 60f;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI] DrawListHeader 失败: {ex.Message}");
            }
        }
    }
}