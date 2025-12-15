using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameGlobal;

namespace GameScreens
{
    /// <summary>
    /// 预测系统UI辅助类
    /// 提供军师预测结果的可视化显示
    /// </summary>
    public static class PredictionUIHelper
    {
        /// <summary>
        /// 绘制招募成功率预测
        /// </summary>
        /// <param name="target">招募目标</param>
        /// <param name="position">绘制位置</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="font">字体</param>
        public static void DrawRecruitPrediction(Person target, Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;

                if (faction?.IsPlayer != true)
                {
                    DrawText(spriteBatch, font, "非玩家势力", position, Color.Gray);
                    return;
                }

                if (advisor == null)
                {
                    DrawText(spriteBatch, font, "无军师，无法预测", position, Color.Red);
                    return;
                }

                // 获取预测结果
                int predictedChance = AdvisorPredictionSystem.GetRecruitChanceDisplay(advisor, target);
                string description = AdvisorPredictionSystem.GetRecruitChanceDescription(advisor, target);
                
                // 根据成功率设置颜色
                Color chanceColor = GetChanceColor(predictedChance);
                Color advisorColor = GetAdvisorReliabilityColor(advisor);

                // 绘制预测信息
                DrawText(spriteBatch, font, $"招募成功率: {description}", position, chanceColor);
                
                Vector2 detailPos = new Vector2(position.X, position.Y + 20);
                DrawText(spriteBatch, font, $"军师 {advisor.Name} 预测", detailPos, advisorColor);
                
                // 添加可信度指示
                if (advisor.Intelligence < 80)
                {
                    Vector2 warningPos = new Vector2(position.X, position.Y + 40);
                    string warning = GetReliabilityWarning(advisor);
                    DrawText(spriteBatch, font, warning, warningPos, Color.Yellow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测UI] DrawRecruitPrediction 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制外交成功率预测
        /// </summary>
        /// <param name="targetFaction">目标势力</param>
        /// <param name="diplomacyType">外交类型</param>
        /// <param name="position">绘制位置</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="font">字体</param>
        public static void DrawDiplomacyPrediction(Faction targetFaction, string diplomacyType, Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;

                if (advisor == null)
                {
                    DrawText(spriteBatch, font, "需要军师进行外交预测", position, Color.Red);
                    return;
                }

                int predictedChance = AdvisorPredictionSystem.GetDiplomacyChanceDisplay(advisor, targetFaction, diplomacyType);
                Color chanceColor = GetChanceColor(predictedChance);
                
                string diplomacyName = GetDiplomacyTypeName(diplomacyType);
                DrawText(spriteBatch, font, $"{diplomacyName}: {predictedChance}%", position, chanceColor);
                
                // 绘制建议
                Vector2 advicePos = new Vector2(position.X + 150, position.Y);
                string advice = GetDiplomacyAdvice(predictedChance);
                Color adviceColor = GetAdviceColor(predictedChance);
                DrawText(spriteBatch, font, advice, advicePos, adviceColor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测UI] DrawDiplomacyPrediction 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制战斗胜率预测
        /// </summary>
        /// <param name="ourTroop">我方部队</param>
        /// <param name="enemyTroop">敌方部队</param>
        /// <param name="position">绘制位置</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="font">字体</param>
        public static void DrawBattlePrediction(Troop ourTroop, Troop enemyTroop, Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;

                if (advisor == null)
                {
                    DrawText(spriteBatch, font, "无军师战术分析", position, Color.Gray);
                    return;
                }

                int predictedChance = AdvisorPredictionSystem.GetBattleChanceDisplay(advisor, ourTroop, enemyTroop);
                Color chanceColor = GetChanceColor(predictedChance);
                
                // 绘制胜率
                DrawText(spriteBatch, font, $"预测胜率: {predictedChance}%", position, chanceColor);
                
                // 绘制战术建议
                Vector2 advicePos = new Vector2(position.X, position.Y + 20);
                string battleAdvice = GetBattleAdvice(predictedChance);
                Color adviceColor = GetAdviceColor(predictedChance);
                DrawText(spriteBatch, font, battleAdvice, advicePos, adviceColor);
                
                // 绘制军师信息
                Vector2 advisorPos = new Vector2(position.X, position.Y + 40);
                Color advisorColor = GetAdvisorReliabilityColor(advisor);
                DrawText(spriteBatch, font, $"军师: {advisor.Name}", advisorPos, advisorColor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测UI] DrawBattlePrediction 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制计谋成功率预测
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="stratagemType">计谋类型</param>
        /// <param name="position">绘制位置</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="font">字体</param>
        public static void DrawStratagemPrediction(Person target, string stratagemType, Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;

                if (advisor == null)
                {
                    DrawText(spriteBatch, font, "需要军师制定计谋", position, Color.Red);
                    return;
                }

                int predictedChance = AdvisorPredictionSystem.GetStratagemChanceDisplay(advisor, target, stratagemType);
                Color chanceColor = GetChanceColor(predictedChance);
                
                string stratagemName = GetStratagemTypeName(stratagemType);
                DrawText(spriteBatch, font, $"{stratagemName}: {predictedChance}%", position, chanceColor);
                
                // 显示智力对比
                Vector2 comparisonPos = new Vector2(position.X, position.Y + 20);
                string comparison = $"智力对比: {advisor.Intelligence} vs {target.Intelligence}";
                Color comparisonColor = advisor.Intelligence > target.Intelligence ? Color.Green : Color.Orange;
                DrawText(spriteBatch, font, comparison, comparisonPos, comparisonColor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测UI] DrawStratagemPrediction 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制预测面板
        /// </summary>
        /// <param name="bounds">面板边界</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="target">目标对象</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="font">字体</param>
        /// <param name="backgroundTexture">背景纹理</param>
        public static void DrawPredictionPanel(Rectangle bounds, string actionType, object target, SpriteBatch spriteBatch, SpriteFont font, Texture2D backgroundTexture)
        {
            try
            {
                // 绘制背景
                if (backgroundTexture != null)
                {
                    spriteBatch.Draw(backgroundTexture, bounds, Color.Black * 0.8f);
                }

                Vector2 contentPos = new Vector2(bounds.X + 10, bounds.Y + 10);
                
                // 绘制标题
                DrawText(spriteBatch, font, "军师预测", contentPos, Color.Yellow);
                contentPos.Y += 25;

                // 根据行动类型绘制相应预测
                switch (actionType.ToLower())
                {
                    case "recruit":
                        if (target is Person person)
                        {
                            DrawRecruitPrediction(person, contentPos, spriteBatch, font);
                        }
                        break;

                    case "diplomacy":
                        if (target is (Faction faction, string diplomacyType))
                        {
                            DrawDiplomacyPrediction(faction, diplomacyType, contentPos, spriteBatch, font);
                        }
                        break;

                    case "battle":
                        if (target is (Troop ourTroop, Troop enemyTroop))
                        {
                            DrawBattlePrediction(ourTroop, enemyTroop, contentPos, spriteBatch, font);
                        }
                        break;

                    case "stratagem":
                        if (target is (Person targetPerson, string stratagemType))
                        {
                            DrawStratagemPrediction(targetPerson, stratagemType, contentPos, spriteBatch, font);
                        }
                        break;
                }

                // 绘制军师信息
                DrawAdvisorInfoInPanel(new Vector2(bounds.X + 10, bounds.Bottom - 60), spriteBatch, font);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测UI] DrawPredictionPanel 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制军师信息（面板内）
        /// </summary>
        private static void DrawAdvisorInfoInPanel(Vector2 position, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;

                if (advisor != null)
                {
                    string advisorInfo = $"军师: {advisor.Name} (智力 {advisor.Intelligence})";
                    Color advisorColor = GetAdvisorReliabilityColor(advisor);
                    DrawText(spriteBatch, font, advisorInfo, position, advisorColor);

                    Vector2 accuracyPos = new Vector2(position.X, position.Y + 15);
                    string accuracy = AdvisorDataHelper.GetAccuracyAssessment(advisor);
                    DrawText(spriteBatch, font, accuracy, accuracyPos, Color.LightGray);
                }
                else
                {
                    DrawText(spriteBatch, font, "无军师 - 无法进行预测", position, Color.Red);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测UI] DrawAdvisorInfoInPanel 失败: {ex.Message}");
            }
        }

        #region 颜色和样式辅助方法

        /// <summary>
        /// 根据成功率获取颜色
        /// </summary>
        private static Color GetChanceColor(int chance)
        {
            return chance switch
            {
                >= 80 => Color.Green,      // 高成功率 - 绿色
                >= 60 => Color.LightGreen, // 较高成功率 - 浅绿
                >= 40 => Color.Yellow,     // 中等成功率 - 黄色
                >= 20 => Color.Orange,     // 较低成功率 - 橙色
                _ => Color.Red             // 低成功率 - 红色
            };
        }

        /// <summary>
        /// 根据军师可靠性获取颜色
        /// </summary>
        private static Color GetAdvisorReliabilityColor(Person advisor)
        {
            return advisor.Intelligence switch
            {
                >= 100 => Color.Gold,      // 神算 - 金色
                >= 90 => Color.Cyan,       // 极高 - 青色
                >= 80 => Color.LightBlue,  // 很高 - 浅蓝
                >= 70 => Color.White,      // 较高 - 白色
                >= 60 => Color.LightGray,  // 一般 - 浅灰
                _ => Color.Gray            // 较低 - 灰色
            };
        }

        /// <summary>
        /// 根据成功率获取建议颜色
        /// </summary>
        private static Color GetAdviceColor(int chance)
        {
            return chance switch
            {
                >= 70 => Color.LightGreen,
                >= 50 => Color.Yellow,
                >= 30 => Color.Orange,
                _ => Color.Red
            };
        }

        #endregion

        #region 文本辅助方法

        /// <summary>
        /// 获取外交类型名称
        /// </summary>
        private static string GetDiplomacyTypeName(string diplomacyType)
        {
            return diplomacyType switch
            {
                "Alliance" => "结盟",
                "Trade" => "通商",
                "NonAggression" => "互不侵犯",
                _ => diplomacyType
            };
        }

        /// <summary>
        /// 获取计谋类型名称
        /// </summary>
        private static string GetStratagemTypeName(string stratagemType)
        {
            return stratagemType switch
            {
                "Confusion" => "混乱",
                "Persuade" => "劝降",
                "Sabotage" => "破坏",
                _ => stratagemType
            };
        }

        /// <summary>
        /// 获取外交建议
        /// </summary>
        private static string GetDiplomacyAdvice(int chance)
        {
            return chance switch
            {
                >= 70 => "时机很好",
                >= 50 => "可以尝试",
                >= 30 => "需要准备",
                _ => "暂缓进行"
            };
        }

        /// <summary>
        /// 获取战斗建议
        /// </summary>
        private static string GetBattleAdvice(int chance)
        {
            return chance switch
            {
                >= 80 => "必胜之战",
                >= 60 => "胜算较大",
                >= 40 => "势均力敌",
                >= 20 => "劣势明显",
                _ => "避免交战"
            };
        }

        /// <summary>
        /// 获取可靠性警告
        /// </summary>
        private static string GetReliabilityWarning(Person advisor)
        {
            return advisor.Intelligence switch
            {
                >= 80 => "",
                >= 70 => "预测可能有误差",
                >= 60 => "预测误差较大",
                _ => "预测不太可信"
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

        #endregion

        /// <summary>
        /// 绘制预测对比表
        /// </summary>
        /// <param name="position">起始位置</param>
        /// <param name="targets">目标列表</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="spriteBatch">绘制批次</param>
        /// <param name="font">字体</param>
        public static void DrawPredictionComparison(Vector2 position, object[] targets, string actionType, SpriteBatch spriteBatch, SpriteFont font)
        {
            try
            {
                Faction faction = Session.Current.Scenario.CurrentFaction;
                Person advisor = faction?.Advisor;

                if (advisor == null)
                {
                    DrawText(spriteBatch, font, "无军师，无法进行预测对比", position, Color.Red);
                    return;
                }

                // 绘制表头
                DrawText(spriteBatch, font, "预测对比表", position, Color.Yellow);
                Vector2 currentPos = new Vector2(position.X, position.Y + 25);

                DrawText(spriteBatch, font, "目标\t\t成功率\t建议", currentPos, Color.White);
                currentPos.Y += 20;

                // 绘制每个目标的预测
                foreach (object target in targets)
                {
                    if (target is Person person && actionType == "recruit")
                    {
                        int chance = AdvisorPredictionSystem.GetRecruitChanceDisplay(advisor, person);
                        string desc = AdvisorPredictionSystem.GetRecruitChanceDescription(advisor, person);
                        Color chanceColor = GetChanceColor(chance);

                        DrawText(spriteBatch, font, $"{person.Name}\t\t{chance}%\t{desc}", currentPos, chanceColor);
                        currentPos.Y += 18;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[预测UI] DrawPredictionComparison 失败: {ex.Message}");
            }
        }
    }
}