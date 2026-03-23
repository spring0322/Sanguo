using System;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.FactionDetail;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI系统集成器 - 提供统一的AI操作接口
    /// 与军师系统、外交系统等集成
    /// </summary>
    public static class AISystemIntegrator
    {
        private static bool isEmergencyStopped = false;

        /// <summary>
        /// 处理AI命令队列 - 在主游戏循环中调用
        /// </summary>
        public static void ProcessAICommands()
        {
            if (isEmergencyStopped)
            {
                return;
            }

            try
            {
                AILegacyAdapter.ProcessCommands();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AISystemIntegrator] 处理AI命令时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 请求创建部队 - 安全的AI部队创建接口
        /// </summary>
        public static void RequestCreateTroop(Architecture city, Person leader, MilitaryKind kind, int quantity, string source = "AI")
        {
            if (isEmergencyStopped) return;

            var command = new AICommand(
                AICommandType.CreateTroop,
                city,
                new Tuple<Architecture, Person, MilitaryKind, int>(city, leader, kind, quantity),
                $"{source}-组建部队-{leader.Name}"
            );

            AILegacyAdapter.Enqueue(command);
        }

        /// <summary>
        /// 请求召回人员 - 安全的AI人员管理接口
        /// </summary>
        public static void RequestRecallPerson(Person person, string source = "AI")
        {
            if (isEmergencyStopped) return;

            var command = new AICommand(
                AICommandType.RecallPerson,
                person,
                person,
                $"{source}-召回人员-{person.Name}"
            );

            AILegacyAdapter.Enqueue(command);
        }

        /// <summary>
        /// 请求部队行动 - 安全的AI部队操作接口
        /// </summary>
        public static void RequestTroopAction(Troop troop, string action, object target, string source = "AI")
        {
            if (isEmergencyStopped) return;

            var command = new AICommand(
                AICommandType.TroopAction,
                troop,
                new Tuple<Troop, string, object>(troop, action, target),
                $"{source}-部队行动-{troop.Name}-{action}"
            );

            AILegacyAdapter.Enqueue(command);
        }

        /// <summary>
        /// 请求资源消费 - 安全的AI资源管理接口
        /// </summary>
        public static void RequestSpendResource(Architecture city, string resourceType, int amount, string source = "AI")
        {
            if (isEmergencyStopped) return;

            var command = new AICommand(
                AICommandType.SpendResource,
                city,
                new Tuple<Architecture, string, int>(city, resourceType, amount),
                $"{source}-消费资源-{city.Name}-{resourceType}-{amount}"
            );

            AILegacyAdapter.Enqueue(command);
        }

        /// <summary>
        /// 请求外交行动 - 安全的AI外交接口
        /// </summary>
        public static void RequestDiplomaticAction(Architecture sourceCity, Faction targetFaction, string actionType, Person diplomat, string source = "AI")
        {
            if (isEmergencyStopped) return;

            var command = new AICommand(
                AICommandType.DiplomaticAction,
                sourceCity,
                new Tuple<Architecture, Faction, string, Person>(sourceCity, targetFaction, actionType, diplomat),
                $"{source}-外交行动-{actionType}-{targetFaction.Name}"
            );

            AILegacyAdapter.Enqueue(command);
        }

        /// <summary>
        /// 执行军师建议 - 与军师系统集成
        /// </summary>
        public static void ExecuteAdvisorRecommendation(Faction faction, string recommendationType, object parameters, string source = "军师建议")
        {
            if (isEmergencyStopped) return;

            try
            {
                switch (recommendationType)
                {
                    case "CreateTroop":
                        if (parameters is Tuple<Architecture, Person, MilitaryKind, int> troopParam)
                        {
                            var (city, leader, kind, quantity) = troopParam;
                            RequestCreateTroop(city, leader, kind, quantity, source);
                        }
                        break;

                    case "RecallPerson":
                        if (parameters is Person person)
                        {
                            RequestRecallPerson(person, source);
                        }
                        break;

                    case "DiplomaticAction":
                        if (parameters is Tuple<Architecture, Faction, string, Person> diplomacyParam)
                        {
                            var (sourceCity, targetFaction, actionType, diplomat) = diplomacyParam;
                            RequestDiplomaticAction(sourceCity, targetFaction, actionType, diplomat, source);
                        }
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine($"[AISystemIntegrator] 未知的军师建议类型: {recommendationType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AISystemIntegrator] 执行军师建议失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行外交行动 - 与外交系统集成
        /// </summary>
        public static void ExecuteDiplomaticAction(Architecture sourceCity, Faction targetFaction, string actionType, Person diplomat)
        {
            RequestDiplomaticAction(sourceCity, targetFaction, actionType, diplomat, "外交系统");
        }

        /// <summary>
        /// 批量执行AI操作 - 用于复杂的AI决策
        /// </summary>
        public static void ExecuteBatchOperations(AICommand[] commands, string source = "批量AI")
        {
            if (isEmergencyStopped) return;

            foreach (var command in commands)
            {
                if (command != null)
                {
                    command.DebugInfo = $"{source}-{command.DebugInfo}";
                    AILegacyAdapter.Enqueue(command);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[AISystemIntegrator] 批量操作入队: {commands.Length} 个命令");
        }

        /// <summary>
        /// 记录系统状态 - 用于调试和监控
        /// </summary>
        public static void LogSystemStatus()
        {
            int queueCount = AILegacyAdapter.GetQueueCount();
            string status = isEmergencyStopped ? "紧急停止" : "正常运行";
            
            System.Diagnostics.Debug.WriteLine($"[AISystemIntegrator] 系统状态: {status}, 队列长度: {queueCount}");
        }

        /// <summary>
        /// 紧急停止所有AI操作
        /// </summary>
        public static void EmergencyStop()
        {
            isEmergencyStopped = true;
            AILegacyAdapter.ClearQueue();
            System.Diagnostics.Debug.WriteLine("[AISystemIntegrator] 紧急停止已激活，所有AI操作已停止");
        }

        /// <summary>
        /// 恢复AI操作
        /// </summary>
        public static void Resume()
        {
            isEmergencyStopped = false;
            System.Diagnostics.Debug.WriteLine("[AISystemIntegrator] AI操作已恢复");
        }

        /// <summary>
        /// 检查系统是否正常运行
        /// </summary>
        public static bool IsRunning()
        {
            return !isEmergencyStopped;
        }

        /// <summary>
        /// 获取队列统计信息
        /// </summary>
        public static string GetQueueStats()
        {
            int queueCount = AILegacyAdapter.GetQueueCount();
            string status = isEmergencyStopped ? "停止" : "运行";
            return $"AI系统: {status}, 队列: {queueCount}";
        }
    }
}