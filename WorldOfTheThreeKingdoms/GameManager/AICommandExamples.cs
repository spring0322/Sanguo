using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.FactionDetail;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI命令使用示例 - 展示如何安全地使用AI命令系统
    /// </summary>
    public static class AICommandExamples
    {
        /// <summary>
        /// 示例1: 安全的AI部队创建
        /// </summary>
        public static void ExampleCreateTroop()
        {
            // 获取示例数据
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            var faction = scenario.CurrentPlayer;
            if (faction?.Architectures.Count == 0) return;

            var city = faction.Architectures[0] as Architecture;
            if (city?.Persons.Count == 0) return;

            var leader = city.Persons[0] as Person;
            var militaryKind = scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds[0];

            // 使用AI系统安全创建部队
            AISystemIntegrator.RequestCreateTroop(
                city, 
                leader, 
                militaryKind, 
                1000, 
                "AI示例-自动编队"
            );

            System.Diagnostics.Debug.WriteLine("[AICommandExamples] 已请求创建部队");
        }

        /// <summary>
        /// 示例2: 智能的人员管理
        /// </summary>
        public static void ExamplePersonManagement()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            var faction = scenario.CurrentPlayer;
            if (faction?.Architectures.Count == 0) return;

            foreach (Architecture city in faction.Architectures)
            {
                // 找出空闲的武将
                var idlePersons = new List<Person>();
                foreach (Person person in city.Persons)
                {
                    if (person.OutsideTask == OutsideTaskKind.无)
                    {
                        idlePersons.Add(person);
                    }
                }

                // 如果有太多空闲武将，召回一些去其他城市
                if (idlePersons.Count > 5)
                {
                    for (int i = 5; i < idlePersons.Count; i++)
                    {
                        AISystemIntegrator.RequestRecallPerson(
                            idlePersons[i], 
                            "AI示例-人员调配"
                        );
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("[AICommandExamples] 已请求人员管理");
        }

        /// <summary>
        /// 示例3: 与军师系统结合
        /// </summary>
        public static void ExampleAdvisorIntegration()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            var faction = scenario.CurrentPlayer;
            if (faction?.Architectures.Count == 0) return;

            var city = faction.Architectures[0] as Architecture;
            if (city?.Persons.Count == 0) return;

            // 模拟军师建议：组建部队
            var leader = city.Persons[0] as Person;
            var militaryKind = scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds[0];
            
            var advisorRecommendation = new Tuple<Architecture, Person, MilitaryKind, int>(
                city, leader, militaryKind, 1500
            );

            // 通过AI系统执行军师建议
            AISystemIntegrator.ExecuteAdvisorRecommendation(
                faction,
                "CreateTroop",
                advisorRecommendation,
                "军师建议-加强防务"
            );

            System.Diagnostics.Debug.WriteLine("[AICommandExamples] 已执行军师建议");
        }

        /// <summary>
        /// 示例4: 与外交系统结合
        /// </summary>
        public static void ExampleDiplomacyIntegration()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            var currentFaction = scenario.CurrentPlayer;
            if (currentFaction?.Architectures.Count == 0) return;

            var sourceCity = currentFaction.Architectures[0] as Architecture;
            if (sourceCity?.Persons.Count == 0) return;

            // 找到一个外交官
            Person diplomat = null;
            foreach (Person person in sourceCity.Persons)
            {
                if (person.Politics > 80) // 政治能力高的作为外交官
                {
                    diplomat = person;
                    break;
                }
            }

            if (diplomat == null) return;

            // 找到目标势力
            Faction targetFaction = null;
            foreach (Faction faction in scenario.Factions)
            {
                if (faction != currentFaction && !faction.Destroyed)
                {
                    targetFaction = faction;
                    break;
                }
            }

            if (targetFaction == null) return;

            // 执行外交行动
            AISystemIntegrator.ExecuteDiplomaticAction(
                sourceCity,
                targetFaction,
                "停战",
                diplomat
            );

            System.Diagnostics.Debug.WriteLine("[AICommandExamples] 已执行外交行动");
        }

        /// <summary>
        /// 示例5: 批量操作
        /// </summary>
        public static void ExampleBatchOperations()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            var faction = scenario.CurrentPlayer;
            if (faction?.Architectures.Count == 0) return;

            var commands = new List<AICommand>();

            // 为每个城市创建一支部队
            foreach (Architecture city in faction.Architectures)
            {
                if (city.Persons.Count > 0 && city.Fund > 10000)
                {
                    var leader = city.Persons[0] as Person;
                    var militaryKind = scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds[0];

                    var command = new AICommand(
                        AICommandType.CreateTroop,
                        city,
                        new Tuple<Architecture, Person, MilitaryKind, int>(city, leader, militaryKind, 800),
                        $"批量编队-{city.Name}"
                    );

                    commands.Add(command);
                }
            }

            // 批量执行
            if (commands.Count > 0)
            {
                AISystemIntegrator.ExecuteBatchOperations(
                    commands.ToArray(),
                    "AI示例-全面动员"
                );

                System.Diagnostics.Debug.WriteLine($"[AICommandExamples] 已提交批量操作: {commands.Count} 个命令");
            }
        }

        /// <summary>
        /// 示例6: 系统监控和调试
        /// </summary>
        public static void ExampleSystemMonitoring()
        {
            // 记录系统状态
            AISystemIntegrator.LogSystemStatus();

            // 获取队列统计
            string stats = AISystemIntegrator.GetQueueStats();
            System.Diagnostics.Debug.WriteLine($"[AICommandExamples] 系统统计: {stats}");

            // 检查系统是否正常运行
            bool isRunning = AISystemIntegrator.IsRunning();
            System.Diagnostics.Debug.WriteLine($"[AICommandExamples] 系统运行状态: {(isRunning ? "正常" : "停止")}");

            // 如果队列积压过多，可以紧急停止
            int queueCount = AILegacyAdapter.GetQueueCount();
            if (queueCount > 50)
            {
                System.Diagnostics.Debug.WriteLine("[AICommandExamples] 队列积压过多，执行紧急停止");
                AISystemIntegrator.EmergencyStop();
            }
        }

        /// <summary>
        /// 运行所有示例 - 用于测试
        /// </summary>
        public static void RunAllExamples()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AICommandExamples] 开始运行AI命令示例");

                ExampleSystemMonitoring();
                ExampleCreateTroop();
                ExamplePersonManagement();
                ExampleAdvisorIntegration();
                ExampleDiplomacyIntegration();
                ExampleBatchOperations();

                System.Diagnostics.Debug.WriteLine("[AICommandExamples] 所有示例运行完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AICommandExamples] 运行示例时发生错误: {ex.Message}");
            }
        }
    }
}