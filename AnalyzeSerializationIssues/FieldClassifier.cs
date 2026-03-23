using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalyzeSerializationIssues
{
    /// <summary>
    /// 字段分类器：区分哪些字段必须序列化，哪些是运行时计算的
    /// </summary>
    public class FieldClassifier
    {
        // 🔥 运行时状态字段（不应该序列化）
        private static readonly HashSet<string> RuntimeStateKeywords =
        [
            "Current", "Target", "Destination", "Path", "Animation", "Drawing",
            "Simulating", "Queue", "Cache", "Temp", "Recent", "Today",
            "Counter", "Index", "Frame", "Done", "Finished", "Moved",
            "Waiting", "Show", "Has", "Is", "Can", "Will", "Allow",
            "Destroyed", "Status", "Action", "Task", "Effect"
        ];

        // 🔥 计算属性（不应该序列化）
        private static readonly HashSet<string> ComputedPropertyKeywords =
        [
            "Count", "Total", "Available", "Possible", "Rate", "Multiple",
            "Increment", "Decrement", "Bonus", "Penalty"
        ];

        // 🔥 遗留字段（已废弃，不需要序列化）
        private static readonly HashSet<string> LegacyKeywords =
        [
            "Legacy", "Old", "String", "IDString"
        ];

        // 🔥 必须序列化的核心字段（基于业务逻辑）
        private static readonly Dictionary<string, HashSet<string>> CriticalFields = new()
        {
            ["Person"] = new HashSet<string>
            {
                // 基础属性
                "YearAvailable", "YearBorn", "YearDead",
                "Reputation", "Fund", "Karma",
                
                // 经验值
                "BubingExperience", "NubingExperience", "QibingExperience",
                "ShuijunExperience", "QixieExperience", "TacticsExperience",
                "StratagemExperience", "InternalExperience",
                
                // 能力潜力
                "CommandPotential", "StrengthPotential", "IntelligencePotential",
                "PoliticsPotential", "GlamourPotential",
                
                // 状态
                "Tiredness", "InjureRate", "OfficerMerit",
                "WorkKind", "OutsideTask", "TaskDays",
                
                // 统计数据
                "YearJoin", "TroopDamageDealt", "TroopBeDamageDealt",
                "ArchitectureDamageDealt", "RebelCount", "ExecuteCount",
                "OfficerKillCount", "FleeCount", "HeldCaptiveCount", "CaptiveCount",
                "StratagemSuccessCount", "StratagemFailCount",
                "StratagemBeSuccessCount", "StratagemBeFailCount",
                "RoutCount", "RoutedCount",
                
                // 特殊状态
                "Immortal", "NvGuan", "IsGeneratedChildren",
                "DaySinceAvailable", "ReturnedDaySince", "LastOutsideTask",
                
                // 关系
                "Strain", "BornRegion", "Qualification",
                "LeaderPossibility", "StrategyTendency", "ValuationOnGovernment",
                
                // 其他
                "Tags", "RewardFinished", "WaitForFeiZiPeriod",
                "ArrivingDays", "AvailableLocation", "DeadReason"
            },
            
            ["Architecture"] = new HashSet<string>
            {
                "BuildingDaysLeft", "BuildingFacility",
                "MayorOnDutyDays", "RecentlyAttacked", "RecentlyBreaked", "RecentlyHit",
                "AutoRecruiting", "AutoZhaoXian", "HasManualHire", "HireFinished",
                "IsStrategicCenter", "TroopershipAvailable",
                "PlanArchitectureID", "PlanFacilityKindID",
                "TransferFoodArchitectureID", "TransferFundArchitectureID",
                "DefensiveLegionID", "RobberTroopID",
                "SuspendTroopTransfer", "OldFactionName"
            },
            
            ["Faction"] = new HashSet<string>
            {
                "AdvisorID", "Prince",
                "TechniquePointForFacility", "TechniquePointForTechnique",
                "UpgradingDaysLeft", "UpgradingTechnique",
                "AutoRefuse", "NotPlayerSelectable",
                "IsAdvisorRecommendationEnabled",
                "LastTalentRecommendYear", "LastSuggestionCheckTurn",
                "PreferredTechniqueKinds", "PlanTechniqueString",
                "SecondTierXResidue", "SecondTierYResidue",
                "ThirdTierXResidue", "ThirdTierYResidue"
            },
            
            ["Troop"] = new HashSet<string>
            {
                "ChaosDayLeft", "CutRoutewayDays",
                "Gold", "TechnologyIncrement",
                "OutburstDefenceMultiple", "OutburstOffenceMultiple",
                "OutburstNeverBeIntoChaos", "OutburstPreventCriticalStrike",
                "AttackDefaultKind", "AttackTargetKind",
                "CastDefaultKind", "CastTargetKind",
                "AutoCombatMethodID", "ForceTroopTargetId"
            },
            
            ["Treasure"] = new HashSet<string>()
        };

        public static void ClassifyMissingFields(string typeName, List<string> missingFields)
        {
            Console.WriteLine($"\n{'='} {typeName} 字段分类 {'='}\n");

            List<string> critical = [];
            List<string> runtime = [];
            List<string> computed = [];
            List<string> legacy = [];
            List<string> uncertain = [];

            foreach (var field in missingFields)
            {
                if (CriticalFields.ContainsKey(typeName) && CriticalFields[typeName].Contains(field))
                {
                    critical.Add(field);
                }
                else if (LegacyKeywords.Any(k => field.Contains(k)))
                {
                    legacy.Add(field);
                }
                else if (RuntimeStateKeywords.Any(k => field.Contains(k)))
                {
                    runtime.Add(field);
                }
                else if (ComputedPropertyKeywords.Any(k => field.Contains(k)))
                {
                    computed.Add(field);
                }
                else
                {
                    uncertain.Add(field);
                }
            }

            // 输出分类结果
            if (critical.Count > 0)
            {
                Console.WriteLine($"🔴 必须修复 ({critical.Count}):");
                foreach (var f in critical.OrderBy(x => x))
                    Console.WriteLine($"   - {f}");
            }

            if (uncertain.Count > 0)
            {
                Console.WriteLine($"\n🟡 需要确认 ({uncertain.Count}):");
                foreach (var f in uncertain.OrderBy(x => x))
                    Console.WriteLine($"   - {f}");
            }

            if (runtime.Count > 0)
            {
                Console.WriteLine($"\n🟢 运行时状态 - 不需要序列化 ({runtime.Count}):");
                foreach (var f in runtime.OrderBy(x => x))
                    Console.WriteLine($"   - {f}");
            }

            if (computed.Count > 0)
            {
                Console.WriteLine($"\n🟢 计算属性 - 不需要序列化 ({computed.Count}):");
                foreach (var f in computed.OrderBy(x => x))
                    Console.WriteLine($"   - {f}");
            }

            if (legacy.Count > 0)
            {
                Console.WriteLine($"\n⚪ 遗留字段 - 不需要序列化 ({legacy.Count}):");
                foreach (var f in legacy.OrderBy(x => x))
                    Console.WriteLine($"   - {f}");
            }

            // 统计
            Console.WriteLine($"\n📊 统计:");
            Console.WriteLine($"   必须修复: {critical.Count}");
            Console.WriteLine($"   需要确认: {uncertain.Count}");
            Console.WriteLine($"   可以忽略: {runtime.Count + computed.Count + legacy.Count}");
        }
    }
}
