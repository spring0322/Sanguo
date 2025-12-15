using System;
using GameObjects;
using GameManager;

namespace GameManager
{
    /// <summary>
    /// 招募系统集成示例 - 展示如何在游戏中使用新的招募系统
    /// </summary>
    public static class RecruitmentSystemIntegration
    {
        /// <summary>
        /// 在MainGameScreen中集成招募系统的完整示例
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
                    
                    // 检查是否有可用的招募者
                    Person recommender = RecruitmentSystem.GetRecommendedRecruiter(currentFaction, target);
                    return recommender != null;
                }
            }
            */
        }

        /// <summary>
        /// 创建完整的招募工作流程示例
        /// </summary>
        public static void CreateRecruitmentWorkflow(Faction playerFaction, Person targetOfficer)
        {
            Console.WriteLine("=== 新招募系统工作流程演示 ===");
            Console.WriteLine("基于 Truth -> Lens -> Outcome 模式");
            Console.WriteLine();
            
            // 1. 检查基本条件
            if (!CanInitiateRecruitment(playerFaction, targetOfficer))
            {
                Console.WriteLine("❌ 无法发起招募：不满足基本条件");
                return;
            }

            // 2. 获取推荐招募者
            Person recommendedRecruiter = RecruitmentSystem.GetRecommendedRecruiter(playerFaction, targetOfficer);
            
            Console.WriteLine($"🎯 招募目标: {targetOfficer.Name} (忠诚{targetOfficer.Loyalty})");
            Console.WriteLine($"👑 推荐招募者: {recommendedRecruiter?.Name ?? "无"}");
            
            if (recommendedRecruiter == null)
            {
                Console.WriteLine("❌ 没有合适的招募者");
                return;
            }

            // 3. 获取军师预测（带误差的观测）
            var (perceivedRate, prediction) = RecruitmentSystem.GetStrategistPrediction(
                playerFaction.Advisor, 
                recommendedRecruiter, 
                targetOfficer
            );
            
            Console.WriteLine($"🔮 军师预测:");
            if (perceivedRate == -1)
            {
                Console.WriteLine($"   成功率: ??% (无军师)");
                Console.WriteLine($"   评语: {prediction}");
            }
            else
            {
                Console.WriteLine($"   成功率: {perceivedRate}%");
                Console.WriteLine($"   评语: {prediction}");
            }

            // 4. 根据预测结果给出建议
            if (perceivedRate == -1)
            {
                Console.WriteLine("⚠️ 系统提示：无军师情况下，招募结果完全随机");
            }
            else if (perceivedRate >= 80)
            {
                Console.WriteLine("✅ 系统建议：军师认为成功率很高，建议立即执行");
            }
            else if (perceivedRate >= 60)
            {
                Console.WriteLine("👍 系统建议：军师认为有较好成功率，值得尝试");
            }
            else if (perceivedRate >= 40)
            {
                Console.WriteLine("⚠️ 系统建议：军师认为成功率一般，需谨慎考虑");
            }
            else if (perceivedRate >= 20)
            {
                Console.WriteLine("⚠️ 系统建议：军师认为成功率较低，风险较高");
            }
            else
            {
                Console.WriteLine("❌ 系统建议：军师强烈反对，但玩家仍可\"头铁\"尝试");
            }

            // 5. 显示详细分析
            Console.WriteLine("\n📋 详细分析:");
            string analysis = RecruitmentSystem.GetDetailedAnalysis(playerFaction.Advisor, recommendedRecruiter, targetOfficer);
            Console.WriteLine(analysis);

            // 6. 模拟玩家决策和执行
            Console.WriteLine("\n🎮 玩家选择执行招募...");
            
            // 执行招募（基于真实成功率，不是军师预测的成功率）
            bool actualSuccess = RecruitmentSystem.ExecuteRecruitment(recommendedRecruiter, targetOfficer);
            
            if (actualSuccess)
            {
                Console.WriteLine("🎉 招募成功！");
                Console.WriteLine($"   {targetOfficer.Name} 被 {recommendedRecruiter.Name} 成功说服");
                
                // 处理成功结果
                RecruitmentSystem.HandleRecruitmentResult(recommendedRecruiter, targetOfficer, true);
                
                // 分析军师预测准确性
                if (perceivedRate != -1)
                {
                    if (perceivedRate >= 60)
                        Console.WriteLine("   军师预测较为准确，成功率确实较高");
                    else
                        Console.WriteLine("   军师可能低估了成功率，或者运气不错！");
                }
            }
            else
            {
                Console.WriteLine("😞 招募失败！");
                Console.WriteLine($"   {targetOfficer.Name} 拒绝了 {recommendedRecruiter.Name} 的招募");
                
                // 处理失败结果
                RecruitmentSystem.HandleRecruitmentResult(recommendedRecruiter, targetOfficer, false);
                
                // 分析军师预测准确性
                if (perceivedRate != -1)
                {
                    if (perceivedRate <= 40)
                        Console.WriteLine("   军师预测较为准确，确实成功率不高");
                    else
                        Console.WriteLine("   军师可能高估了成功率，或者运气不好");
                }
            }

            Console.WriteLine("\n💡 系统说明:");
            Console.WriteLine("   - 军师看到的成功率可能与真实成功率不同（智力影响观测准确度）");
            Console.WriteLine("   - 最终结果基于真实成功率，不是军师预测的成功率");
            Console.WriteLine("   - 这就是 Truth -> Lens -> Outcome 模式的核心");
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
            Person recommender = RecruitmentSystem.GetRecommendedRecruiter(playerFaction, targetOfficer);
            return recommender != null;
        }

        /// <summary>
        /// 演示不同军师智力对预测的影响
        /// </summary>
        public static void DemonstrateStrategistIntelligenceEffect()
        {
            Console.WriteLine("=== 军师智力对预测影响演示 ===");
            Console.WriteLine();

            // 创建测试数据
            var recruiter = CreateTestPerson("测试招募者", 80, 85, 90, 85);
            var target = CreateTestPerson("测试目标", 70, 75, 65, 70, 60);

            var strategists = new[]
            {
                CreateTestPerson("诸葛亮", 100, 95, 90, 95), // 智力100，误差0%
                CreateTestPerson("庞统", 90, 90, 85, 90),    // 智力90，误差5%
                CreateTestPerson("荀彧", 80, 80, 80, 85),    // 智力80，误差10%
                CreateTestPerson("田丰", 70, 70, 75, 80),    // 智力70，误差15%
                CreateTestPerson("郭图", 60, 65, 70, 60)     // 智力60，误差20%
            };

            Console.WriteLine("同一招募场景，不同军师的预测结果:");
            Console.WriteLine($"招募者: {recruiter.Name}, 目标: {target.Name}");
            Console.WriteLine();

            foreach (var strategist in strategists)
            {
                Console.WriteLine($"=== 军师: {strategist.Name} (智力{strategist.Intelligence}) ===");
                
                // 显示理论误差范围
                int errorRange = (100 - strategist.Intelligence) / 2;
                Console.WriteLine($"理论误差范围: ±{errorRange}%");
                
                // 进行5次预测，展示误差变化
                Console.WriteLine("5次预测结果:");
                for (int i = 1; i <= 5; i++)
                {
                    var (rate, comment) = RecruitmentSystem.GetStrategistPrediction(strategist, recruiter, target);
                    Console.WriteLine($"  第{i}次: {rate}% - {comment}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("💡 观察要点:");
            Console.WriteLine("- 智力越高的军师，预测结果越稳定");
            Console.WriteLine("- 智力低的军师，预测结果波动较大");
            Console.WriteLine("- 但所有预测都基于同一个真实成功率");
        }

        /// <summary>
        /// 演示"头铁"机制
        /// </summary>
        public static void DemonstrateHeadstrongMechanism()
        {
            Console.WriteLine("=== \"头铁\"机制演示 ===");
            Console.WriteLine();

            var recruiter = CreateTestPerson("玩家武将", 70, 75, 80, 75);
            var target = CreateTestPerson("高忠诚目标", 80, 85, 75, 80, 95); // 极高忠诚度
            var strategist = CreateTestPerson("智谋军师", 90, 85, 80, 85);

            // 获取军师预测
            var (perceivedRate, prediction) = RecruitmentSystem.GetStrategistPrediction(strategist, recruiter, target);
            
            Console.WriteLine($"场景: 招募高忠诚度目标 (忠诚{target.Loyalty})");
            Console.WriteLine($"军师预测: {perceivedRate}%");
            Console.WriteLine($"军师评语: {prediction}");
            Console.WriteLine();

            if (perceivedRate < 20)
            {
                Console.WriteLine("🚫 传统设计: 按钮变灰，禁止玩家操作");
                Console.WriteLine("✅ 新设计: 按钮仍可点击，但显示\"强行招募\"");
                Console.WriteLine();
                
                Console.WriteLine("玩家心理: \"军师说不行，但我就是要试试！\"");
                Console.WriteLine("系统响应: \"好的，您可以尝试，但风险自负\"");
                Console.WriteLine();
                
                // 模拟玩家"头铁"执行
                Console.WriteLine("🎮 玩家选择\"头铁\"执行...");
                bool success = RecruitmentSystem.ExecuteRecruitment(recruiter, target);
                
                if (success)
                {
                    Console.WriteLine("🎉 意外成功！玩家的坚持得到了回报！");
                    Console.WriteLine("💡 这就是\"头铁\"机制的魅力 - 给玩家惊喜的可能");
                }
                else
                {
                    Console.WriteLine("😞 如军师所料，招募失败了");
                    Console.WriteLine("💡 但玩家至少有了尝试的机会，不会感到被系统限制");
                }
            }
            else
            {
                Console.WriteLine("✅ 军师认为有一定成功率，正常执行流程");
            }
        }

        /// <summary>
        /// 创建测试人物
        /// </summary>
        private static Person CreateTestPerson(string name, int intelligence, int command, int charm, int politics, int loyalty = 50)
        {
            var person = new Person();
            person.Name = name;
            person.Intelligence = intelligence;
            person.Command = command;
            person.Charm = charm;
            person.Politics = politics;
            person.Loyalty = loyalty;
            person.Strength = 80;
            person.Calmness = 70;
            person.Ideal = 50;
            person.Character = new Character { ID = 0 }; // 默认仁德型
            return person;
        }

        /// <summary>
        /// 运行完整的集成演示
        /// </summary>
        public static void RunCompleteIntegrationDemo()
        {
            Console.WriteLine("🎮 招募系统集成演示");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            // 创建测试势力和人物
            var playerFaction = new Faction();
            playerFaction.Leader = CreateTestPerson("刘备", 75, 85, 95, 80);
            playerFaction.Advisor = CreateTestPerson("诸葛亮", 100, 95, 90, 95);
            
            var targetOfficer = CreateTestPerson("关羽", 80, 95, 85, 80, 90);
            
            // 添加一些招募者到势力中
            playerFaction.Persons.Add(playerFaction.Leader);
            playerFaction.Persons.Add(playerFaction.Advisor);
            playerFaction.Persons.Add(CreateTestPerson("张飞", 65, 98, 75, 40));
            
            // 设置人物所属势力
            foreach (var person in playerFaction.Persons)
            {
                person.BelongedFaction = playerFaction;
            }

            CreateRecruitmentWorkflow(playerFaction, targetOfficer);
            Console.WriteLine();
            
            DemonstrateStrategistIntelligenceEffect();
            Console.WriteLine();
            
            DemonstrateHeadstrongMechanism();
            
            Console.WriteLine("=====================================");
            Console.WriteLine("集成演示完成！新招募系统已准备就绪。");
            Console.WriteLine();
            Console.WriteLine("核心特性:");
            Console.WriteLine("✅ Truth->Lens->Outcome 模式");
            Console.WriteLine("✅ 军师智力影响观测准确度");
            Console.WriteLine("✅ \"头铁\"机制保持玩家自主权");
            Console.WriteLine("✅ 丰富的个性化评语系统");
            Console.WriteLine("✅ 完整的分析和反馈机制");
        }
    }
}