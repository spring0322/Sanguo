using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 招募面板集成示例 - 展示如何在MainGameScreen中使用
    /// </summary>
    public static class RecruitPanelIntegration
    {
        /// <summary>
        /// 在MainGameScreen中集成招募面板的示例代码
        /// </summary>
        public static void IntegrateIntoMainGameScreen()
        {
            /*
            // 在MainGameScreen类中添加以下代码：

            public class MainGameScreen : GameScreen
            {
                // 添加招募面板实例
                private RecruitPanel recruitPanel;

                // 在LoadContent方法中初始化
                public override void LoadContent()
                {
                    // ... 其他初始化代码 ...
                    
                    recruitPanel = new RecruitPanel();
                    recruitPanel.LoadContent(GraphicsDevice, font);
                }

                // 在Update方法中更新
                public override void Update(GameTime gameTime)
                {
                    // ... 其他更新逻辑 ...
                    
                    recruitPanel.Update(gameTime);
                    
                    // 处理招募相关的输入
                    HandleRecruitmentInput();
                }

                // 在Draw方法中绘制
                public override void Draw(SpriteBatch spriteBatch)
                {
                    // ... 其他绘制逻辑 ...
                    
                    // 招募面板在最上层绘制
                    recruitPanel.Draw(spriteBatch);
                }

                // 处理招募输入的示例方法
                private void HandleRecruitmentInput()
                {
                    // 当玩家右键点击敌方武将时，打开招募面板
                    if (rightClickedPerson != null && rightClickedPerson.BelongedFaction != currentFaction)
                    {
                        // 检查是否可以招募
                        if (CanRecruit(rightClickedPerson))
                        {
                            recruitPanel.OnTargetSelected(rightClickedPerson, currentFaction);
                        }
                        rightClickedPerson = null;
                    }
                }

                // 检查是否可以招募的逻辑
                private bool CanRecruit(Person target)
                {
                    // 基本条件检查
                    if (target == null || target.BelongedFaction == null) return false;
                    if (target.BelongedFaction == currentFaction) return false; // 不能招募自己人
                    if (target.BelongedCaptive != null) return false; // 俘虏无法招募
                    
                    // 其他游戏规则检查...
                    return true;
                }
            }
            */
        }

        /// <summary>
        /// 创建完整的招募系统工作流程示例
        /// </summary>
        public static void CreateRecruitmentWorkflow(Faction playerFaction, Person targetOfficer)
        {
            Console.WriteLine("=== 招募系统工作流程演示 ===");
            
            // 1. 检查基本条件
            if (!CanInitiateRecruitment(playerFaction, targetOfficer))
            {
                Console.WriteLine("❌ 无法发起招募：不满足基本条件");
                return;
            }

            // 2. 获取军师预判
            var (bestRecruiter, prediction) = StrategistManager.PredictRecruitment(playerFaction, targetOfficer);
            
            Console.WriteLine($"🎯 招募目标: {targetOfficer.Name}");
            Console.WriteLine($"👑 推荐招募者: {bestRecruiter?.Name ?? "无"}");
            Console.WriteLine($"📊 预测成功率: {prediction.SuccessRate}%");
            Console.WriteLine($"💬 军师评语: {prediction.Comment}");

            // 3. 根据预测结果给出建议
            if (prediction.IsImpossible)
            {
                Console.WriteLine("🚫 军师建议：此事不可能成功，建议放弃");
                return;
            }
            else if (prediction.SuccessRate >= 80)
            {
                Console.WriteLine("✅ 军师建议：成功率极高，建议立即执行");
            }
            else if (prediction.SuccessRate >= 60)
            {
                Console.WriteLine("⚠️ 军师建议：有一定成功率，可以尝试");
            }
            else if (prediction.SuccessRate >= 30)
            {
                Console.WriteLine("⚠️ 军师建议：成功率较低，需谨慎考虑");
            }
            else
            {
                Console.WriteLine("❌ 军师建议：成功率极低，不建议执行");
            }

            // 4. 显示详细分析
            Console.WriteLine("\n📋 详细分析:");
            string analysis = StrategistManager.GetRecruitmentAnalysis(playerFaction, targetOfficer);
            Console.WriteLine(analysis);

            // 5. 模拟玩家决策
            Console.WriteLine("\n🤔 玩家可以选择:");
            Console.WriteLine("1. 按军师建议执行招募");
            Console.WriteLine("2. 选择其他招募者");
            Console.WriteLine("3. 放弃招募");
            Console.WriteLine("4. 查看更多战略建议");
        }

        /// <summary>
        /// 检查是否可以发起招募
        /// </summary>
        private static bool CanInitiateRecruitment(Faction playerFaction, Person targetOfficer)
        {
            // 基本条件检查
            if (playerFaction == null || targetOfficer == null) return false;
            if (targetOfficer.BelongedFaction == playerFaction) return false;
            if (targetOfficer.BelongedCaptive != null) return false;
            
            // 检查是否有可用的招募者
            bool hasAvailableRecruiter = false;
            foreach (var person in playerFaction.Persons)
            {
                if (person != null && person.BelongedFaction == playerFaction && person != targetOfficer)
                {
                    hasAvailableRecruiter = true;
                    break;
                }
            }
            
            return hasAvailableRecruiter;
        }

        /// <summary>
        /// 创建招募面板的快捷方法
        /// </summary>
        public static RecruitPanel CreateRecruitPanel(GraphicsDevice graphicsDevice, SpriteFont font)
        {
            var panel = new RecruitPanel();
            panel.LoadContent(graphicsDevice, font);
            return panel;
        }

        /// <summary>
        /// 处理招募结果的示例
        /// </summary>
        public static void HandleRecruitmentResult(Person recruiter, Person target, bool success)
        {
            if (success)
            {
                Console.WriteLine($"🎉 招募成功！{recruiter.Name} 成功说服了 {target.Name}");
                
                // 执行招募逻辑
                // target.BelongedFaction = recruiter.BelongedFaction;
                // target.Loyalty = 80; // 新招募的武将忠诚度
                
                // 触发招募成功事件
                // OnRecruitmentSuccess?.Invoke(recruiter, target);
            }
            else
            {
                Console.WriteLine($"😞 招募失败！{target.Name} 拒绝了 {recruiter.Name} 的招募");
                
                // 可能的后果
                // target.Loyalty += 10; // 拒绝招募后忠诚度可能提升
                
                // 触发招募失败事件
                // OnRecruitmentFailure?.Invoke(recruiter, target);
            }
        }

        /// <summary>
        /// 创建招募系统的完整UI流程
        /// </summary>
        public static void CreateCompleteRecruitmentUI()
        {
            Console.WriteLine("=== 完整招募UI流程设计 ===");
            Console.WriteLine();
            
            Console.WriteLine("1. 触发招募面板:");
            Console.WriteLine("   - 右键点击敌方武将");
            Console.WriteLine("   - 选择'招募'选项");
            Console.WriteLine("   - 检查基本条件");
            Console.WriteLine();
            
            Console.WriteLine("2. 显示招募面板:");
            Console.WriteLine("   - 目标武将信息");
            Console.WriteLine("   - 招募者下拉选择框（自动选中推荐者）");
            Console.WriteLine("   - 军师头像和建议文本");
            Console.WriteLine("   - 成功率显示（颜色编码）");
            Console.WriteLine("   - 确定/取消/详情按钮");
            Console.WriteLine();
            
            Console.WriteLine("3. 交互功能:");
            Console.WriteLine("   - 切换招募者时实时更新预测");
            Console.WriteLine("   - 详情按钮显示完整分析");
            Console.WriteLine("   - 成功率过低时禁用确定按钮");
            Console.WriteLine("   - 军师评语根据性格变化");
            Console.WriteLine();
            
            Console.WriteLine("4. 执行招募:");
            Console.WriteLine("   - 消耗行动点数");
            Console.WriteLine("   - 根据成功率进行判定");
            Console.WriteLine("   - 显示结果动画");
            Console.WriteLine("   - 更新游戏状态");
        }
    }
}