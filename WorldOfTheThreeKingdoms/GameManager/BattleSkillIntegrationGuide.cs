using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens;

namespace GameManager
{
    /// <summary>
    /// 战斗技能预测系统集成指南
    /// </summary>
    public static class BattleSkillIntegrationGuide
    {
        /// <summary>
        /// 在MainGameScreen中集成战斗技能系统的完整示例
        /// </summary>
        public static void IntegrateIntoMainGameScreen()
        {
            /*
            // 在MainGameScreen类中添加以下代码：

            public class MainGameScreen : GameScreen
            {
                // 添加战斗相关组件
                private BattleInputController battleInputController;
                private BattleSkillPanel battleSkillPanel;
                private bool isBattleMode = false;

                // 在LoadContent方法中初始化
                public override void LoadContent()
                {
                    // ... 其他初始化代码 ...
                    
                    // 初始化战斗组件
                    battleInputController = new BattleInputController(currentFaction);
                    battleInputController.LoadContent(font, GraphicsDevice);
                    
                    battleSkillPanel = new BattleSkillPanel(currentFaction, battleInputController);
                    battleSkillPanel.LoadContent(font, GraphicsDevice);
                }

                // 在Update方法中更新
                public override void Update(GameTime gameTime)
                {
                    // ... 其他更新逻辑 ...
                    
                    if (isBattleMode)
                    {
                        // 更新战斗输入控制器
                        battleInputController.Update(gameTime, GetAllTroopsInBattle());
                        
                        // 更新技能面板
                        battleSkillPanel.Update(gameTime);
                        
                        // 处理战斗模式切换
                        HandleBattleModeInput();
                    }
                }

                // 在Draw方法中绘制
                public override void Draw(SpriteBatch spriteBatch)
                {
                    // ... 其他绘制逻辑 ...
                    
                    if (isBattleMode)
                    {
                        // 绘制战斗输入控制器（浮动文本等）
                        battleInputController.Draw(spriteBatch);
                        
                        // 绘制技能面板
                        battleSkillPanel.Draw(spriteBatch);
                    }
                }

                // 处理战斗模式输入
                private void HandleBattleModeInput()
                {
                    // ESC键退出战斗模式
                    if (InputManager.IsKeyPressed(Keys.Escape))
                    {
                        ExitBattleMode();
                    }
                    
                    // Tab键切换技能面板显示
                    if (InputManager.IsKeyPressed(Keys.Tab))
                    {
                        battleSkillPanel.SetVisible(!battleSkillPanel.IsVisible);
                    }
                }

                // 进入战斗模式
                private void EnterBattleMode()
                {
                    isBattleMode = true;
                    battleSkillPanel.SetVisible(true);
                    
                    // 重新初始化可用技能
                    battleInputController = new BattleInputController(currentFaction);
                    battleInputController.LoadContent(font, GraphicsDevice);
                }

                // 退出战斗模式
                private void ExitBattleMode()
                {
                    isBattleMode = false;
                    battleInputController.CancelTargetSelection();
                    battleSkillPanel.SetVisible(false);
                }

                // 获取战斗中的所有部队
                private List<Troop> GetAllTroopsInBattle()
                {
                    var troops = new List<Troop>();
                    
                    // 添加我方部队
                    foreach (var troop in currentFaction.Troops)
                    {
                        if (troop != null && troop.InBattle)
                        {
                            troops.Add(troop);
                        }
                    }
                    
                    // 添加敌方部队
                    foreach (var faction in GameManager.AllFactions)
                    {
                        if (faction != currentFaction)
                        {
                            foreach (var troop in faction.Troops)
                            {
                                if (troop != null && troop.InBattle)
                                {
                                    troops.Add(troop);
                                }
                            }
                        }
                    }
                    
                    return troops;
                }
            }
            */
        }

        /// <summary>
        /// 创建完整的战斗技能使用工作流程
        /// </summary>
        public static void CreateBattleSkillWorkflow()
        {
            Console.WriteLine("=== 战斗技能系统工作流程 ===");
            Console.WriteLine();
            
            Console.WriteLine("1. 战斗开始阶段:");
            Console.WriteLine("   - 检测到战斗时自动进入战斗模式");
            Console.WriteLine("   - 显示技能面板，展示可用技能");
            Console.WriteLine("   - 根据君主能力初始化技能列表");
            Console.WriteLine();
            
            Console.WriteLine("2. 技能选择阶段:");
            Console.WriteLine("   - 玩家点击技能按钮开始选择目标");
            Console.WriteLine("   - 鼠标悬停在敌军上显示成功率预测");
            Console.WriteLine("   - 军师智力影响预测准确度");
            Console.WriteLine("   - 无军师时显示 ??%");
            Console.WriteLine();
            
            Console.WriteLine("3. 目标确认阶段:");
            Console.WriteLine("   - 左键点击确认目标");
            Console.WriteLine("   - 右键取消技能选择");
            Console.WriteLine("   - 显示技能执行动画");
            Console.WriteLine();
            
            Console.WriteLine("4. 结果反馈阶段:");
            Console.WriteLine("   - 根据真实成功率判定结果");
            Console.WriteLine("   - 显示成功/失败浮动文字");
            Console.WriteLine("   - 应用技能效果到目标部队");
            Console.WriteLine("   - 更新战场状态");
        }

        /// <summary>
        /// 技能系统的键盘快捷键设计
        /// </summary>
        public static void DesignKeyboardShortcuts()
        {
            Console.WriteLine("=== 推荐键盘快捷键设计 ===");
            Console.WriteLine();
            
            Console.WriteLine("战斗模式快捷键:");
            Console.WriteLine("  F1 - 火计");
            Console.WriteLine("  F2 - 水计");
            Console.WriteLine("  F3 - 伏兵");
            Console.WriteLine("  F4 - 挑衅");
            Console.WriteLine("  F5 - 混乱");
            Console.WriteLine("  F6 - 撤退");
            Console.WriteLine("  F7 - 鼓舞");
            Console.WriteLine();
            
            Console.WriteLine("界面控制:");
            Console.WriteLine("  Tab - 切换技能面板显示/隐藏");
            Console.WriteLine("  ESC - 取消当前技能选择/退出战斗模式");
            Console.WriteLine("  Space - 暂停/继续战斗");
            Console.WriteLine("  Ctrl+A - 显示军师详细分析");
            Console.WriteLine();
            
            Console.WriteLine("实现示例:");
            Console.WriteLine(@"
            // 在Update方法中添加快捷键处理
            private void HandleBattleShortcuts()
            {
                if (InputManager.IsKeyPressed(Keys.F1))
                    battleInputController.StartTargetSelection(SkillType.FirePlot);
                if (InputManager.IsKeyPressed(Keys.F2))
                    battleInputController.StartTargetSelection(SkillType.WaterPlot);
                // ... 其他技能快捷键
                
                if (InputManager.IsKeyPressed(Keys.Tab))
                    battleSkillPanel.SetVisible(!battleSkillPanel.IsVisible);
                    
                if (InputManager.IsKeyPressed(Keys.Escape))
                    battleInputController.CancelTargetSelection();
            }
            ");
        }

        /// <summary>
        /// UI/UX 设计建议
        /// </summary>
        public static void DesignUIUXGuidelines()
        {
            Console.WriteLine("=== UI/UX 设计建议 ===");
            Console.WriteLine();
            
            Console.WriteLine("1. 视觉反馈设计:");
            Console.WriteLine("   - 成功率 >= 70%: 绿色显示，军师微笑");
            Console.WriteLine("   - 成功率 50-69%: 黄色显示，军师中性");
            Console.WriteLine("   - 成功率 30-49%: 橙色显示，军师担忧");
            Console.WriteLine("   - 成功率 < 30%: 红色显示，军师摇头");
            Console.WriteLine("   - 无军师: 灰色显示 ??%");
            Console.WriteLine();
            
            Console.WriteLine("2. 浮动文本设计:");
            Console.WriteLine("   - 位置: 敌军头顶上方40像素");
            Console.WriteLine("   - 字体: 加粗，带阴影");
            Console.WriteLine("   - 动画: 淡入淡出，轻微上浮");
            Console.WriteLine("   - 持续时间: 2-3秒");
            Console.WriteLine();
            
            Console.WriteLine("3. 技能面板设计:");
            Console.WriteLine("   - 位置: 屏幕右下角");
            Console.WriteLine("   - 大小: 300x200像素");
            Console.WriteLine("   - 布局: 军师信息 + 技能按钮网格");
            Console.WriteLine("   - 透明度: 90%，不完全遮挡战场");
            Console.WriteLine();
            
            Console.WriteLine("4. 交互反馈:");
            Console.WriteLine("   - 鼠标悬停: 按钮高亮，显示技能描述");
            Console.WriteLine("   - 技能选择: 改变鼠标光标样式");
            Console.WriteLine("   - 目标锁定: 敌军单位边框高亮");
            Console.WriteLine("   - 执行成功: 绿色爆炸效果");
            Console.WriteLine("   - 执行失败: 红色X标记");
        }

        /// <summary>
        /// 性能优化建议
        /// </summary>
        public static void PerformanceOptimizationTips()
        {
            Console.WriteLine("=== 性能优化建议 ===");
            Console.WriteLine();
            
            Console.WriteLine("1. 预测计算优化:");
            Console.WriteLine("   - 缓存军师预测结果，避免重复计算");
            Console.WriteLine("   - 只在鼠标移动到新目标时重新计算");
            Console.WriteLine("   - 使用对象池管理浮动文本");
            Console.WriteLine();
            
            Console.WriteLine("2. 渲染优化:");
            Console.WriteLine("   - 技能面板使用纹理缓存");
            Console.WriteLine("   - 浮动文本批量绘制");
            Console.WriteLine("   - 只绘制屏幕内的UI元素");
            Console.WriteLine();
            
            Console.WriteLine("3. 内存管理:");
            Console.WriteLine("   - 及时清理不用的浮动文本");
            Console.WriteLine("   - 重用预测结果结构体");
            Console.WriteLine("   - 避免频繁的字符串拼接");
            Console.WriteLine();
            
            Console.WriteLine("实现示例:");
            Console.WriteLine(@"
            // 预测结果缓存
            private Dictionary<(Troop, SkillType), (int rate, DateTime time)> predictionCache 
                = new Dictionary<(Troop, SkillType), (int, DateTime)>();
            
            private int GetCachedPrediction(Troop target, SkillType skill)
            {
                var key = (target, skill);
                if (predictionCache.TryGetValue(key, out var cached))
                {
                    // 缓存5秒内有效
                    if (DateTime.Now - cached.time < TimeSpan.FromSeconds(5))
                        return cached.rate;
                }
                
                // 重新计算并缓存
                int newRate = StrategistManager.GetBattlePrediction(faction, target, skill);
                predictionCache[key] = (newRate, DateTime.Now);
                return newRate;
            }
            ");
        }

        /// <summary>
        /// 测试和调试建议
        /// </summary>
        public static void TestingAndDebuggingTips()
        {
            Console.WriteLine("=== 测试和调试建议 ===");
            Console.WriteLine();
            
            Console.WriteLine("1. 单元测试:");
            Console.WriteLine("   - 测试不同智力军师的预测准确度");
            Console.WriteLine("   - 测试无军师情况的处理");
            Console.WriteLine("   - 测试极端数值的边界情况");
            Console.WriteLine();
            
            Console.WriteLine("2. 集成测试:");
            Console.WriteLine("   - 测试UI组件之间的交互");
            Console.WriteLine("   - 测试键盘快捷键功能");
            Console.WriteLine("   - 测试战斗模式切换");
            Console.WriteLine();
            
            Console.WriteLine("3. 调试工具:");
            Console.WriteLine("   - 添加调试信息显示开关");
            Console.WriteLine("   - 记录预测准确度统计");
            Console.WriteLine("   - 提供GM命令快速测试");
            Console.WriteLine();
            
            Console.WriteLine("调试代码示例:");
            Console.WriteLine(@"
            #if DEBUG
            // 调试信息显示
            private bool showDebugInfo = false;
            
            private void DrawDebugInfo(SpriteBatch spriteBatch)
            {
                if (!showDebugInfo) return;
                
                var debugText = new StringBuilder();
                debugText.AppendLine($'当前军师: {faction.Advisor?.Name ?? ""无""}');
                debugText.AppendLine($'预测缓存数量: {predictionCache.Count}');
                debugText.AppendLine($'浮动文本数量: {floatingTextManager.ActiveCount}');
                
                spriteBatch.DrawString(font, debugText.ToString(), 
                    new Vector2(10, 100), Color.Yellow);
            }
            
            // GM命令处理
            private void HandleGMCommands()
            {
                if (InputManager.IsKeyPressed(Keys.F12))
                    showDebugInfo = !showDebugInfo;
                    
                if (InputManager.IsKeyPressed(Keys.F11))
                    BattleSkillPredictionTest.RunCompleteTest();
            }
            #endif
            ");
        }

        /// <summary>
        /// 扩展功能建议
        /// </summary>
        public static void ExtensionFeatureSuggestions()
        {
            Console.WriteLine("=== 扩展功能建议 ===");
            Console.WriteLine();
            
            Console.WriteLine("1. 高级预测功能:");
            Console.WriteLine("   - 考虑地形因素的预测修正");
            Console.WriteLine("   - 季节和天气对技能的影响");
            Console.WriteLine("   - 部队状态（疲劳、士气）的影响");
            Console.WriteLine("   - 历史成功率统计和学习");
            Console.WriteLine();
            
            Console.WriteLine("2. 军师AI增强:");
            Console.WriteLine("   - 军师主动建议最佳技能");
            Console.WriteLine("   - 根据战况动态调整建议");
            Console.WriteLine("   - 军师性格影响建议风格");
            Console.WriteLine("   - 军师经验值系统");
            Console.WriteLine();
            
            Console.WriteLine("3. 用户体验改进:");
            Console.WriteLine("   - 技能连击系统");
            Console.WriteLine("   - 预设技能组合");
            Console.WriteLine("   - 自动技能执行选项");
            Console.WriteLine("   - 回放和分析功能");
            Console.WriteLine();
            
            Console.WriteLine("4. 多人游戏支持:");
            Console.WriteLine("   - 隐藏敌方军师预测");
            Console.WriteLine("   - 军师间谍和反间谍");
            Console.WriteLine("   - 实时预测同步");
            Console.WriteLine("   - 观战模式的预测显示");
        }

        /// <summary>
        /// 运行完整的集成指南
        /// </summary>
        public static void RunCompleteGuide()
        {
            Console.WriteLine("🎮 战斗技能预测系统集成指南");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            CreateBattleSkillWorkflow();
            Console.WriteLine();
            
            DesignKeyboardShortcuts();
            Console.WriteLine();
            
            DesignUIUXGuidelines();
            Console.WriteLine();
            
            PerformanceOptimizationTips();
            Console.WriteLine();
            
            TestingAndDebuggingTips();
            Console.WriteLine();
            
            ExtensionFeatureSuggestions();
            
            Console.WriteLine("=====================================");
            Console.WriteLine("集成指南完成！");
            Console.WriteLine("战斗技能预测系统已准备就绪，可以开始集成到游戏中。");
        }
    }
}