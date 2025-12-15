using System;
using GameObjects;
using GameManager;

namespace GameManager
{
    /// <summary>
    /// AI策略系统集成示例
    /// </summary>
    public static class AIStrategyIntegration
    {
        /// <summary>
        /// 在游戏主循环中集成AI策略系统
        /// </summary>
        public static void IntegrateIntoGameLoop()
        {
            /*
            // 在游戏主循环或战斗系统中添加以下代码：

            public class BattleSystem
            {
                // 在每个AI势力的回合中调用
                public void ProcessAIFactionTurn(Faction aiFaction)
                {
                    // ... 其他AI逻辑 ...
                    
                    // 处理AI战斗决策
                    foreach (var aiTroop in aiFaction.Troops)
                    {
                        if (aiTroop.InBattle)
                        {
                            var nearbyEnemies = GetNearbyEnemyTroops(aiTroop);
                            if (nearbyEnemies.Any())
                            {
                                var targetEnemy = SelectBestTarget(nearbyEnemies);
                                
                                // 使用AI策略系统进行决策
                                AIStrategySystem.AI_ExecuteStratagem(aiFaction, targetEnemy);
                            }
                        }
                    }
                }
                
                // 获取附近的敌军
                private List<Troop> GetNearbyEnemyTroops(Troop aiTroop)
                {
                    // 实现获取附近敌军的逻辑
                    return new List<Troop>();
                }
                
                // 选择最佳攻击目标
                private Troop SelectBestTarget(List<Troop> enemies)
                {
                    // 实现目标选择逻辑
                    return enemies.FirstOrDefault();
                }
            }
            */
        }

        /// <summary>
        /// 创建完整的AI决策演示
        /// </summary>
        public static void DemonstrateAIDecisionMaking()
        {
            Console.WriteLine("=== AI决策系统演示 ===");
            Console.WriteLine();

            // 场景1：智谋型君主 + 高智力军师
            Console.WriteLine("场景1：刘备 + 诸葛亮 vs 曹军");
            var liuBeiFaction = CreateDemoFaction("刘备", "诸葛亮", 75, 85, 95, 80, 100, 95, 90, 95, 0);
            var caoTroop = CreateDemoTroop("曹操", 95, 90, 85, 75);
            
            DemonstrateDecisionProcess(liuBeiFaction, caoTroop);
            Console.WriteLine();

            // 场景2：刚愎自用君主 + 军师
            Console.WriteLine("场景2：袁绍 + 田丰 vs 公孙瓒军");
            var yuanShaoFaction = CreateDemoFaction("袁绍", "田丰", 70, 75, 80, 60, 85, 70, 75, 85, 3);
            var gongsunTroop = CreateDemoTroop("公孙瓒", 65, 85, 70, 65);
            
            DemonstrateDecisionProcess(yuanShaoFaction, gongsunTroop);
            Console.WriteLine();

            // 场景3：无军师的莽撞君主
            Console.WriteLine("场景3：张飞独自指挥 vs 敌军");
            var zhangFeiFaction = CreateDemoFaction("张飞", null, 65, 98, 75, 40, 0, 0, 0, 0, 3);
            var enemyTroop = CreateDemoTroop("敌将", 70, 80, 65, 70);
            
            DemonstrateDecisionProcess(zhangFeiFaction, enemyTroop);
        }

        /// <summary>
        /// 演示决策过程
        /// </summary>
        private static void DemonstrateDecisionProcess(Faction aiFaction, Troop enemyTroop)
        {
            Console.WriteLine($"AI势力: {aiFaction.Name}");
            Console.WriteLine($"君主: {aiFaction.Leader.Name} (智{aiFaction.Leader.Intelligence} 统{aiFaction.Leader.Command} 魅{aiFaction.Leader.Charm})");
            
            if (aiFaction.Advisor != null)
            {
                Console.WriteLine($"军师: {aiFaction.Advisor.Name} (智{aiFaction.Advisor.Intelligence})");
            }
            else
            {
                Console.WriteLine("军师: 无");
            }
            
            Console.WriteLine($"敌军: {enemyTroop.Leader.Name}");
            Console.WriteLine();

            // 获取决策分析
            string analysis = AIStrategySystem.GetAIDecisionAnalysis(aiFaction, enemyTroop);
            Console.WriteLine(analysis);

            // 模拟决策过程
            Console.WriteLine("决策过程模拟:");
            
            if (aiFaction.Advisor != null)
            {
                // 检查是否听从军师
                bool willListen = aiFaction.AICheckListenToAdvisor();
                Console.WriteLine($"1. 军师提出建议");
                Console.WriteLine($"2. 君主考虑是否采纳: {(willListen ? "采纳" : "拒绝")}");
                
                if (willListen)
                {
                    Console.WriteLine($"3. 结果: {aiFaction.Leader.Name} 听从了 {aiFaction.Advisor.Name} 的建议");
                    Console.WriteLine($"4. 执行军师推荐的最佳策略");
                }
                else
                {
                    Console.WriteLine($"3. 结果: {aiFaction.Leader.Name} 拒绝了 {aiFaction.Advisor.Name} 的建议");
                    
                    string alternativeAction = GetPersonalityBasedAlternative(aiFaction.Leader.Character?.ID ?? 0);
                    Console.WriteLine($"4. 君主选择: {alternativeAction}");
                    
                    // 模拟玩家情报
                    Console.WriteLine($"5. 情报网络: 「{aiFaction.Leader.Name} 拒绝了军师建议，选择了不同的策略！」");
                }
            }
            else
            {
                Console.WriteLine($"1. {aiFaction.Leader.Name} 无军师，独自制定策略");
                
                string soloStrategy = GetPersonalityBasedSoloStrategy(aiFaction.Leader.Character?.ID ?? 0);
                Console.WriteLine($"2. 基于性格选择: {soloStrategy}");
                Console.WriteLine($"3. 执行决策");
            }
        }

        /// <summary>
        /// 根据性格获取替代行动
        /// </summary>
        private static string GetPersonalityBasedAlternative(int personalityId)
        {
            switch (personalityId)
            {
                case 0: // 仁德型
                    return "选择更仁慈的策略，如鼓舞士气而非强攻";
                case 1: // 霸道型
                    return "展现威势，选择更具攻击性的策略";
                case 2: // 冷静型
                    return "重新分析，选择风险更低的备选方案";
                case 3: // 莽撞型
                    return "不管三七二十一，直接发起猛攻";
                case 4: // 狡诈型
                    return "使用出人意料的计谋，如声东击西";
                default:
                    return "选择直接进攻";
            }
        }

        /// <summary>
        /// 根据性格获取独自策略
        /// </summary>
        private static string GetPersonalityBasedSoloStrategy(int personalityId)
        {
            switch (personalityId)
            {
                case 0: // 仁德型
                    return "谨慎行事，选择风险较低的策略";
                case 1: // 霸道型
                    return "凭借经验和直觉，选择攻击性策略";
                case 2: // 冷静型
                    return "理性分析战况，选择最优策略";
                case 3: // 莽撞型
                    return "凭一腔热血，直接冲锋陷阵";
                case 4: // 狡诈型
                    return "过度自信，可能选择过于复杂的计谋";
                default:
                    return "随机应变";
            }
        }

        /// <summary>
        /// 演示历史著名的军师君主冲突
        /// </summary>
        public static void DemonstrateHistoricalConflicts()
        {
            Console.WriteLine("=== 历史著名军师君主冲突演示 ===");
            Console.WriteLine();

            var historicalCases = new[]
            {
                new {
                    Lord = "袁绍",
                    Advisor = "田丰",
                    Conflict = "官渡之战前，田丰建议持久战，袁绍急于求成",
                    Outcome = "袁绍拒绝建议，最终败于曹操"
                },
                new {
                    Lord = "刘备",
                    Advisor = "诸葛亮",
                    Conflict = "夷陵之战前，诸葛亮劝阻东征，刘备执意报仇",
                    Outcome = "刘备不听劝阻，大败于陆逊"
                },
                new {
                    Lord = "曹操",
                    Advisor = "荀彧",
                    Conflict = "关于称王问题，荀彧反对，曹操坚持",
                    Outcome = "君臣关系破裂，荀彧忧愤而死"
                }
            };

            foreach (var case_ in historicalCases)
            {
                Console.WriteLine($"=== {case_.Lord} vs {case_.Advisor} ===");
                Console.WriteLine($"冲突: {case_.Conflict}");
                Console.WriteLine($"结果: {case_.Outcome}");
                Console.WriteLine();
                
                Console.WriteLine("系统模拟:");
                Console.WriteLine($"- {case_.Lord} 的性格特征导致拒绝军师建议");
                Console.WriteLine($"- AI系统会根据君主性格计算拒绝概率");
                Console.WriteLine($"- 玩家通过情报网络得知君主与军师的分歧");
                Console.WriteLine($"- 这种分歧可能影响该势力的战略效果");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 演示情报系统的作用
        /// </summary>
        public static void DemonstrateIntelligenceSystem()
        {
            Console.WriteLine("=== 情报系统演示 ===");
            Console.WriteLine();

            Console.WriteLine("情报系统的作用:");
            Console.WriteLine("1. 监控敌方势力的内部决策");
            Console.WriteLine("2. 发现君主与军师的分歧");
            Console.WriteLine("3. 预测敌方可能的战略选择");
            Console.WriteLine("4. 为玩家提供战略情报优势");
            Console.WriteLine();

            Console.WriteLine("情报消息示例:");
            Console.WriteLine("📰 「袁绍拒绝了军师田丰的持久战建议，决定立即进攻！」");
            Console.WriteLine("📰 「曹操采纳了荀彧的建议，准备使用火攻战术！」");
            Console.WriteLine("📰 「张飞无军师在侧，独自决定发起强攻！」");
            Console.WriteLine("📰 「孙权与周瑜意见一致，准备实施水战计划！」");
            Console.WriteLine();

            Console.WriteLine("玩家可以利用这些情报:");
            Console.WriteLine("- 预判敌方战术，制定针对性策略");
            Console.WriteLine("- 识别敌方内部矛盾，寻找外交机会");
            Console.WriteLine("- 评估敌方决策质量，判断威胁程度");
            Console.WriteLine("- 在关键时刻进行反制或利用");
        }

        /// <summary>
        /// 创建演示势力
        /// </summary>
        private static Faction CreateDemoFaction(string leaderName, string advisorName,
            int leaderInt, int leaderCmd, int leaderCha, int leaderPol,
            int advisorInt, int advisorCmd, int advisorCha, int advisorPol, int personalityId)
        {
            var faction = new Faction();
            faction.Name = $"{leaderName}军";
            
            // 创建君主
            faction.Leader = new Person();
            faction.Leader.Name = leaderName;
            faction.Leader.Intelligence = leaderInt;
            faction.Leader.Command = leaderCmd;
            faction.Leader.Charm = leaderCha;
            faction.Leader.Politics = leaderPol;
            faction.Leader.Character = new Character { ID = personalityId };
            faction.Leader.BelongedFaction = faction;
            
            // 创建军师（如果有）
            if (!string.IsNullOrEmpty(advisorName))
            {
                faction.Advisor = new Person();
                faction.Advisor.Name = advisorName;
                faction.Advisor.Intelligence = advisorInt;
                faction.Advisor.Command = advisorCmd;
                faction.Advisor.Charm = advisorCha;
                faction.Advisor.Politics = advisorPol;
                faction.Advisor.Character = new Character { ID = 2 }; // 默认冷静型
                faction.Advisor.BelongedFaction = faction;
            }
            
            return faction;
        }

        /// <summary>
        /// 创建演示部队
        /// </summary>
        private static Troop CreateDemoTroop(string leaderName, int intelligence, int command, int charm, int calmness)
        {
            var troop = new Troop();
            troop.Leader = new Person();
            troop.Leader.Name = leaderName;
            troop.Leader.Intelligence = intelligence;
            troop.Leader.Command = command;
            troop.Leader.Charm = charm;
            troop.Leader.Calmness = calmness;
            troop.Quantity = 5000;
            troop.Morale = 80;
            return troop;
        }

        /// <summary>
        /// 演示AI策略系统的游戏价值
        /// </summary>
        public static void DemonstrateGameplayValue()
        {
            Console.WriteLine("=== AI策略系统的游戏价值 ===");
            Console.WriteLine();

            Console.WriteLine("1. 增强AI真实性:");
            Console.WriteLine("   - AI君主不再是完美的决策者");
            Console.WriteLine("   - 性格缺陷会影响战略选择");
            Console.WriteLine("   - 军师的价值得到体现");
            Console.WriteLine();

            Console.WriteLine("2. 创造战略机会:");
            Console.WriteLine("   - 玩家可以利用敌方君主的性格弱点");
            Console.WriteLine("   - 情报系统提供决策优势");
            Console.WriteLine("   - 外交可以利用内部矛盾");
            Console.WriteLine();

            Console.WriteLine("3. 增加游戏深度:");
            Console.WriteLine("   - 每个AI势力都有独特的决策模式");
            Console.WriteLine("   - 历史人物的性格得到还原");
            Console.WriteLine("   - 军师系统更加重要和有趣");
            Console.WriteLine();

            Console.WriteLine("4. 提升重玩价值:");
            Console.WriteLine("   - AI行为具有一定随机性");
            Console.WriteLine("   - 不同的军师配置产生不同结果");
            Console.WriteLine("   - 玩家需要适应不同的AI对手");
        }

        /// <summary>
        /// 运行完整演示
        /// </summary>
        public static void RunCompleteDemo()
        {
            Console.WriteLine("🎮 AI策略系统完整演示");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            DemonstrateAIDecisionMaking();
            Console.WriteLine();
            
            DemonstrateHistoricalConflicts();
            Console.WriteLine();
            
            DemonstrateIntelligenceSystem();
            Console.WriteLine();
            
            DemonstrateGameplayValue();
            
            Console.WriteLine("=====================================");
            Console.WriteLine("演示完成！AI策略系统为游戏带来了:");
            Console.WriteLine("✅ 更真实的AI行为");
            Console.WriteLine("✅ 丰富的战略互动");
            Console.WriteLine("✅ 有价值的情报系统");
            Console.WriteLine("✅ 深度的角色扮演体验");
        }
    }
}