// ===================================================================
// 军师右键菜单处理器 - 处理AdvisorContextMenu.xml配置
// ===================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using GameObjects;
using GameManager;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 军师右键菜单处理器
    /// </summary>
    public class AdvisorContextMenuHandler
    {
        private Dictionary<string, ContextMenuAction> _actionHandlers;
        private Dictionary<string, ConditionChecker> _conditionCheckers;

        public AdvisorContextMenuHandler()
        {
            InitializeActionHandlers();
            InitializeConditionCheckers();
        }

        #region 动作处理器初始化

        private void InitializeActionHandlers()
        {
            _actionHandlers = new Dictionary<string, ContextMenuAction>
            {
                // 基础军师操作
                ["ShowAdvisorCandidateList"] = ShowAdvisorCandidateList,
                ["ShowAdvisorReappointmentList"] = ShowAdvisorReappointmentList,
                ["RecallCurrentAdvisor"] = RecallCurrentAdvisor,
                ["AppointPersonAsAdvisor"] = AppointPersonAsAdvisor,
                ["RecallPersonFromAdvisor"] = RecallPersonFromAdvisor,
                ["AppointSpecificAdvisor"] = AppointSpecificAdvisor,

                // 军师功能
                ["ShowAdvisorAdvice"] = ShowAdvisorAdvice,
                ["ShowAdvisorInfo"] = ShowAdvisorInfo,
                ["ShowAdvisorAbilityAssessment"] = ShowAdvisorAbilityAssessment,

                // 军师调度
                ["SendAdvisorToArchitecture"] = SendAdvisorToArchitecture,
                ["RecallAdvisorFromArchitecture"] = RecallAdvisorFromArchitecture,

                // 高级功能
                ["ShowStrategicPlanning"] = ShowStrategicPlanning,
                ["ShowIntelligenceAnalysis"] = ShowIntelligenceAnalysis,
                ["ShowPersonnelAdvice"] = ShowPersonnelAdvice,
                ["ShowDiplomaticAdvice"] = ShowDiplomaticAdvice,
                ["ShowAdvisorSettings"] = ShowAdvisorSettings,
                ["ExecuteAdvice"] = ExecuteAdvice
            };
        }

        #endregion

        #region 条件检查器初始化

        private void InitializeConditionCheckers()
        {
            _conditionCheckers = new Dictionary<string, ConditionChecker>
            {
                // 基础条件
                ["HasLeader"] = (context) => context.Faction?.Leader != null,
                ["LeaderCaptured"] = (context) => context.Faction?.Leader?.BelongedCaptive != null,
                ["HasCurrentAdvisor"] = (context) => context.Faction?.Advisor != null,
                ["AdvisorCandidateCount"] = (context) => context.Faction?.AdvisorCandicate?.Count ?? 0,
                ["HasBetterCandidates"] = (context) => HasBetterAdvisorCandidates(context.Faction),

                // 人物条件
                ["PersonIntelligence"] = (context) => context.Person?.Intelligence ?? 0,
                ["PersonAvailable"] = (context) => context.Person?.Available ?? false,
                ["PersonAlive"] = (context) => context.Person?.Alive ?? false,
                ["PersonNotCaptured"] = (context) => context.Person?.BelongedCaptive == null,
                ["PersonNotInTroop"] = (context) => context.Person?.LocationTroop == null,
                ["PersonNotLeader"] = (context) => context.Person != context.Faction?.Leader,
                ["PersonNotCurrentAdvisor"] = (context) => context.Person != context.Faction?.Advisor,
                ["PersonSameFaction"] = (context) => context.Person?.BelongedFaction == context.Faction,
                ["IsCurrentAdvisor"] = (context) => context.Person == context.Faction?.Advisor,
                ["CanBeAdvisor"] = (context) => CanPersonBeAdvisor(context.Person),
                ["IsAdvisorCandidate"] = (context) => IsPersonAdvisorCandidate(context.Person, context.Faction),

                // 建筑条件
                ["ArchitectureSameFaction"] = (context) => context.Architecture?.BelongedFaction == context.Faction,
                ["AdvisorNotInArchitecture"] = (context) => context.Faction?.Advisor?.LocationArchitecture != context.Architecture,
                ["AdvisorInThisArchitecture"] = (context) => context.Faction?.Advisor?.LocationArchitecture == context.Architecture
            };
        }

        #endregion

        #region 基础军师操作

        /// <summary>
        /// 显示军师候选人列表
        /// </summary>
        private void ShowAdvisorCandidateList(MenuActionContext context)
        {
            if (context.Faction == null) return;

            var candidates = context.Faction.AdvisorCandicate;
            if (candidates.Count == 0)
            {
                ShowMessage("没有合适的军师候选人");
                return;
            }

            // 显示候选人选择界面
            var screenManager = Session.MainGame.mainGameScreen as ScreenManager;
            screenManager?.ShowTabListInFrame(
                UndoneWorkKind.Frame,
                FrameKind.Person,
                FrameFunction.AppointAdvisor,
                false, true, true, false,
                candidates,
                null,
                "任命军师",
                ""
            );
        }

        /// <summary>
        /// 显示重新任命军师列表
        /// </summary>
        private void ShowAdvisorReappointmentList(MenuActionContext context)
        {
            if (context.Faction?.Advisor == null) return;

            var candidates = context.Faction.GetAdvisorCandidates(true); // 排除当前军师
            if (candidates.Count == 0)
            {
                ShowMessage("没有更好的军师候选人");
                return;
            }

            // 显示重新任命界面
            var screenManager = Session.MainGame.mainGameScreen as ScreenManager;
            screenManager?.ShowTabListInFrame(
                UndoneWorkKind.Frame,
                FrameKind.Person,
                FrameFunction.AppointAdvisor,
                false, true, true, false,
                candidates,
                null,
                $"重新任命军师 (当前: {context.Faction.AdvisorName})",
                ""
            );
        }

        /// <summary>
        /// 罢免当前军师
        /// </summary>
        private void RecallCurrentAdvisor(MenuActionContext context)
        {
            if (context.Faction?.Advisor == null)
            {
                ShowMessage("当前没有军师可以罢免");
                return;
            }

            string advisorName = context.Faction.AdvisorName;
            
            // 执行罢免
            context.Faction.AdvisorID = -1;
            context.Faction.Advisor = null;
            
            ShowMessage($"已罢免军师 {advisorName}");
        }

        /// <summary>
        /// 任命指定人物为军师
        /// </summary>
        private void AppointPersonAsAdvisor(MenuActionContext context)
        {
            if (context.Person == null || context.Faction == null) return;

            if (!IsPersonAdvisorCandidate(context.Person, context.Faction))
            {
                ShowMessage($"{context.Person.Name} 不符合军师任命条件");
                return;
            }

            string message;
            if (context.Faction.Advisor != null)
            {
                message = $"将军师从 {context.Faction.AdvisorName} 更换为 {context.Person.Name}";
            }
            else
            {
                message = $"任命 {context.Person.Name} 为军师";
            }

            // 执行任命
            context.Faction.AdvisorID = context.Person.ID;
            context.Faction.AppointAdvisor(context.Person);
            
            ShowMessage(message);
        }

        /// <summary>
        /// 罢免指定人物的军师职务
        /// </summary>
        private void RecallPersonFromAdvisor(MenuActionContext context)
        {
            if (context.Person == null || context.Faction == null) return;

            if (context.Person != context.Faction.Advisor)
            {
                ShowMessage($"{context.Person.Name} 不是当前军师");
                return;
            }

            RecallCurrentAdvisor(context);
        }

        /// <summary>
        /// 任命特定ID的人物为军师
        /// </summary>
        private void AppointSpecificAdvisor(MenuActionContext context)
        {
            if (context.ActionParameter == null) return;

            if (int.TryParse(context.ActionParameter, out int personId))
            {
                var person = Session.Current.Scenario.Persons.GetGameObject(personId) as Person;
                if (person != null)
                {
                    var newContext = new MenuActionContext
                    {
                        Person = person,
                        Faction = context.Faction,
                        Architecture = context.Architecture
                    };
                    AppointPersonAsAdvisor(newContext);
                }
            }
        }

        #endregion

        #region 军师功能

        /// <summary>
        /// 显示军师建议
        /// </summary>
        private void ShowAdvisorAdvice(MenuActionContext context)
        {
            if (context.Faction?.Advisor == null)
            {
                ShowMessage("当前没有军师提供建议");
                return;
            }

            // 这里可以集成军师建议系统
            ShowMessage($"军师 {context.Faction.AdvisorName} 的建议：\n\n" +
                       "1. 加强内政建设\n" +
                       "2. 招募更多人才\n" +
                       "3. 与邻国保持友好关系");
        }

        /// <summary>
        /// 显示军师信息
        /// </summary>
        private void ShowAdvisorInfo(MenuActionContext context)
        {
            if (context.Faction?.Advisor == null)
            {
                ShowMessage("当前没有军师");
                return;
            }

            var advisor = context.Faction.Advisor;
            string info = $"军师信息：\n\n" +
                         $"姓名: {advisor.Name}\n" +
                         $"智力: {advisor.Intelligence}\n" +
                         $"政治: {advisor.Politics}\n" +
                         $"魅力: {advisor.Charm}\n" +
                         $"所在地: {advisor.LocationArchitecture?.Name ?? "未知"}";

            ShowMessage(info);
        }

        /// <summary>
        /// 显示军师能力评估
        /// </summary>
        private void ShowAdvisorAbilityAssessment(MenuActionContext context)
        {
            if (context.Person == null) return;

            var person = context.Person;
            string assessment = $"{person.Name} 的军师能力评估：\n\n" +
                               $"智力: {person.Intelligence} {GetRating(person.Intelligence)}\n" +
                               $"政治: {person.Politics} {GetRating(person.Politics)}\n" +
                               $"魅力: {person.Charm} {GetRating(person.Charm)}\n\n" +
                               $"综合评价: {GetOverallRating(person)}";

            ShowMessage(assessment);
        }

        #endregion

        #region 军师调度

        /// <summary>
        /// 派遣军师到建筑
        /// </summary>
        private void SendAdvisorToArchitecture(MenuActionContext context)
        {
            if (context.Faction?.Advisor == null || context.Architecture == null) return;

            // 这里实现军师调度逻辑
            ShowMessage($"已派遣军师 {context.Faction.AdvisorName} 到 {context.Architecture.Name}");
        }

        /// <summary>
        /// 从建筑召回军师
        /// </summary>
        private void RecallAdvisorFromArchitecture(MenuActionContext context)
        {
            if (context.Faction?.Advisor == null || context.Architecture == null) return;

            // 这里实现军师召回逻辑
            ShowMessage($"已从 {context.Architecture.Name} 召回军师 {context.Faction.AdvisorName}");
        }

        #endregion

        #region 高级功能

        /// <summary>
        /// 显示战略规划
        /// </summary>
        private void ShowStrategicPlanning(MenuActionContext context)
        {
            ShowMessage("战略规划功能正在开发中...");
        }

        /// <summary>
        /// 显示情报分析
        /// </summary>
        private void ShowIntelligenceAnalysis(MenuActionContext context)
        {
            ShowMessage("情报分析功能正在开发中...");
        }

        /// <summary>
        /// 显示人事建议
        /// </summary>
        private void ShowPersonnelAdvice(MenuActionContext context)
        {
            ShowMessage("人事建议功能正在开发中...");
        }

        /// <summary>
        /// 显示外交建议
        /// </summary>
        private void ShowDiplomaticAdvice(MenuActionContext context)
        {
            ShowMessage("外交建议功能正在开发中...");
        }

        /// <summary>
        /// 显示军师设置
        /// </summary>
        private void ShowAdvisorSettings(MenuActionContext context)
        {
            ShowMessage("军师设置功能正在开发中...");
        }

        /// <summary>
        /// 执行军师建议
        /// </summary>
        private void ExecuteAdvice(MenuActionContext context)
        {
            ShowMessage("执行建议功能正在开发中...");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查是否有更好的军师候选人
        /// </summary>
        private bool HasBetterAdvisorCandidates(Faction faction)
        {
            if (faction?.Advisor == null) return faction?.AdvisorCandicate?.Count > 0;

            var candidates = faction.GetAdvisorCandidates(true); // 排除当前军师
            return candidates.Any(p => p.Intelligence > faction.Advisor.Intelligence);
        }

        /// <summary>
        /// 检查人物是否可以成为军师
        /// </summary>
        private bool CanPersonBeAdvisor(Person person)
        {
            return person != null &&
                   person.Alive &&
                   person.Available &&
                   person.Intelligence >= 60; // 降低要求用于显示评估
        }

        /// <summary>
        /// 检查人物是否是军师候选人
        /// </summary>
        private bool IsPersonAdvisorCandidate(Person person, Faction faction)
        {
            if (person == null || faction == null) return false;

            return person.Alive &&
                   person.Available &&
                   person.BelongedCaptive == null &&
                   person.LocationTroop == null &&
                   person.BelongedFaction == faction &&
                   person != faction.Leader &&
                   person != faction.Advisor &&
                   person.Intelligence >= 70;
        }

        /// <summary>
        /// 获取能力评级
        /// </summary>
        private string GetRating(int value)
        {
            return value switch
            {
                >= 90 => "(S级)",
                >= 80 => "(A级)",
                >= 70 => "(B级)",
                >= 60 => "(C级)",
                _ => "(D级)"
            };
        }

        /// <summary>
        /// 获取综合评价
        /// </summary>
        private string GetOverallRating(Person person)
        {
            int avg = (person.Intelligence + person.Politics + person.Charm) / 3;
            return avg switch
            {
                >= 85 => "卓越的军师人选",
                >= 75 => "优秀的军师人选",
                >= 65 => "合格的军师人选",
                >= 55 => "勉强合格的军师人选",
                _ => "不适合担任军师"
            };
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        private void ShowMessage(string message)
        {
            Console.WriteLine($"[军师系统] {message}");
            // 这里可以集成实际的消息显示系统
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 执行菜单动作
        /// </summary>
        public void ExecuteAction(string actionName, MenuActionContext context)
        {
            if (_actionHandlers.TryGetValue(actionName, out var handler))
            {
                try
                {
                    handler(context);
                }
                catch (Exception ex)
                {
                    ShowMessage($"执行动作 {actionName} 失败: {ex.Message}");
                }
            }
            else
            {
                ShowMessage($"未知的动作: {actionName}");
            }
        }

        /// <summary>
        /// 检查条件
        /// </summary>
        public bool CheckCondition(string conditionName, MenuActionContext context, string operatorType = "Equal", object expectedValue = null)
        {
            if (!_conditionCheckers.TryGetValue(conditionName, out var checker))
            {
                return false;
            }

            try
            {
                var actualValue = checker(context);
                
                if (expectedValue == null)
                {
                    return Convert.ToBoolean(actualValue);
                }

                return CompareValues(actualValue, expectedValue, operatorType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查条件 {conditionName} 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 比较值
        /// </summary>
        private bool CompareValues(object actual, object expected, string operatorType)
        {
            if (actual == null || expected == null) return false;

            return operatorType switch
            {
                "Equal" => actual.Equals(expected),
                "GreaterThan" => Convert.ToDouble(actual) > Convert.ToDouble(expected),
                "LessThan" => Convert.ToDouble(actual) < Convert.ToDouble(expected),
                "GreaterOrEqual" => Convert.ToDouble(actual) >= Convert.ToDouble(expected),
                "LessOrEqual" => Convert.ToDouble(actual) <= Convert.ToDouble(expected),
                "NotEqual" => !actual.Equals(expected),
                _ => false
            };
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 菜单动作上下文
    /// </summary>
    public class MenuActionContext
    {
        public Faction Faction { get; set; }
        public Person Person { get; set; }
        public Architecture Architecture { get; set; }
        public string ActionParameter { get; set; }
    }

    /// <summary>
    /// 菜单动作委托
    /// </summary>
    public delegate void ContextMenuAction(MenuActionContext context);

    /// <summary>
    /// 条件检查委托
    /// </summary>
    public delegate object ConditionChecker(MenuActionContext context);

    #endregion
}

// ===================================================================
// 使用示例
// ===================================================================

public class AdvisorContextMenuExample
{
    public static void TestContextMenu()
    {
        // 创建处理器
        var handler = new AdvisorContextMenuHandler();
        
        // 创建上下文
        var context = new MenuActionContext
        {
            Faction = Session.Current.Scenario.CurrentFaction,
            Person = null, // 根据实际情况设置
            Architecture = null
        };
        
        // 执行动作
        handler.ExecuteAction("ShowAdvisorCandidateList", context);
        
        // 检查条件
        bool hasAdvisor = handler.CheckCondition("HasCurrentAdvisor", context);
        Console.WriteLine($"有军师: {hasAdvisor}");
    }
}