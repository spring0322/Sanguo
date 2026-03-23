#nullable enable
using System;
using GameObjects;
using GameObjects.PersonDetail;
using Microsoft.Xna.Framework;

namespace GameObjects.Events;

/// <summary>
/// 武将事件中心 - AOT 兼容的强类型事件系统
/// 
/// 涵盖范围：
/// - 计略执行（劫狱、暗杀、说服、破坏、流言、情报、煽动）
/// - 寻访探索（搜索粮草/资金/武将/技术/宝物）
/// - 学习成长（技能、特技、称号）
/// - 外交任务（同盟、停战、割地、劝降）
/// - 人际关系（怀孕、后宫、物品奖励/没收）
/// 
/// 生命周期：与 ScenarioEvents 共享，由 ScenarioEvents.ClearAllHandlers() 统一清理
/// </summary>
public static class PersonEvents
{
    // ==================== 计略事件 ====================
    
    /// <summary>劫狱成功（替代 ExtensionInterface.call("DoJailBreakSuccess")）</summary>
    public static event Action<GameScenario, Person, Captive>? OnJailBreakSuccess;
    
    /// <summary>暗杀成功（替代 ExtensionInterface.call("Assassinated")）</summary>
    public static event Action<GameScenario, Person, Person>? OnAssassinateSuccess;
    
    /// <summary>说服失败（替代 ExtensionInterface.call("DoConvinceFail")）</summary>
    public static event Action<GameScenario, Person>? OnConvinceFailed;
    
    /// <summary>说服成功（替代 ExtensionInterface.call("DoConvinceSuccess")）</summary>
    public static event Action<GameScenario, Person>? OnConvinceSuccess;
    
    /// <summary>破坏成功（替代 ExtensionInterface.call("DoDestroySuccess")）</summary>
    public static event Action<GameScenario, Person, int>? OnDestroySuccess;
    
    /// <summary>破坏失败（替代 ExtensionInterface.call("DoDestroyFail")）</summary>
    public static event Action<GameScenario, Person>? OnDestroyFailed;
    
    /// <summary>流言成功（替代 ExtensionInterface.call("DoGossipSuccess")）</summary>
    public static event Action<GameScenario, Person>? OnGossipSuccess;
    
    /// <summary>流言失败（替代 ExtensionInterface.call("DoGossipFail")）</summary>
    public static event Action<GameScenario, Person>? OnGossipFailed;
    
    /// <summary>情报成功（替代 ExtensionInterface.call("DoInformationSuccess")）</summary>
    public static event Action<GameScenario, Person, Information>? OnInformationSuccess;
    
    /// <summary>情报失败（替代 ExtensionInterface.call("DoInformationFail")）</summary>
    public static event Action<GameScenario, Person>? OnInformationFailed;
    
    /// <summary>煽动成功（替代 ExtensionInterface.call("DoInstigateSuccess")）</summary>
    public static event Action<GameScenario, Person, int>? OnInstigateSuccess;
    
    /// <summary>煽动失败（替代 ExtensionInterface.call("DoinstigateFail")）</summary>
    public static event Action<GameScenario, Person>? OnInstigateFailed;
    
    /// <summary>间谍成功（替代 ExtensionInterface.call("DoSpySuccess")）</summary>
    public static event Action<GameScenario, Person, int>? OnSpySuccess;
    
    /// <summary>间谍失败（替代 ExtensionInterface.call("DoSpyFail")）</summary>
    public static event Action<GameScenario, Person>? OnSpyFailed;

    // ==================== 寻访探索事件 ====================
    
    /// <summary>寻访完成（替代 ExtensionInterface.call("DoSearch")）</summary>
    public static event Action<GameScenario, Person, SearchResultPack>? OnSearchCompleted;

    // ==================== 学习成长事件 ====================
    
    /// <summary>学习技能（替代 ExtensionInterface.call("StudySkill")）</summary>
    public static event Action<GameScenario, Person, Skill>? OnSkillLearned;
    
    /// <summary>特技学习成功（替代 ExtensionInterface.call("StudyStuntSuccess")）</summary>
    public static event Action<GameScenario, Person, Stunt>? OnStuntLearnSuccess;
    
    /// <summary>特技学习失败（替代 ExtensionInterface.call("StudyStuntFail")）</summary>
    public static event Action<GameScenario, Person, Stunt>? OnStuntLearnFailed;
    
    /// <summary>称号学习成功（替代 ExtensionInterface.call("StudyTitleSuccess")）</summary>
    public static event Action<GameScenario, Person, Title>? OnTitleLearnSuccess;
    
    /// <summary>称号学习失败（替代 ExtensionInterface.call("StudyTitleFail")）</summary>
    public static event Action<GameScenario, Person, Title>? OnTitleLearnFailed;

    // ==================== 人际关系事件 ====================
    
    /// <summary>发现怀孕（替代 ExtensionInterface.call("FoundPregnant")）</summary>
    public static event Action<GameScenario, Person>? OnPregnancyDiscovered;
    
    /// <summary>宝物没收（替代 ExtensionInterface.call("ConfiscatedTreasure")）</summary>
    public static event Action<GameScenario, Person>? OnTreasureConfiscated;

    // ==================== 其他事件 ====================
    
    /// <summary>被建筑俘虏（替代 ExtensionInterface.call("CapturedByArchitecture")）</summary>
    public static event Action<GameScenario, Person, Architecture>? OnCapturedByArchitecture;
    
    // ==================== 武将行动开始事件（GoFor系列）====================
    
    /// <summary>武将前往说服（替代 ExtensionInterface.call("GoForConvince")）</summary>
    public static event Action<GameScenario, Person, Person>? OnGoForConvince;
    
    /// <summary>武将前往劝降（替代 ExtensionInterface.call("GoForQuanXiang")）</summary>
    public static event Action<GameScenario, Person, Person>? OnGoForQuanXiang;
    
    /// <summary>武将前往破坏（替代 ExtensionInterface.call("GoForDestroy")）</summary>
    public static event Action<GameScenario, Person, Point>? OnGoForDestroy;
    
    /// <summary>武将前往流言（替代 ExtensionInterface.call("GoForGossip")）</summary>
    public static event Action<GameScenario, Person, Point>? OnGoForGossip;
    
    /// <summary>武将前往情报（替代 ExtensionInterface.call("GoForInformation")）</summary>
    public static event Action<GameScenario, Person, Point>? OnGoForInformation;
    
    /// <summary>武将前往劫狱（替代 ExtensionInterface.call("GoForJailBreak")）</summary>
    public static event Action<GameScenario, Person, Point>? OnGoForJailBreak;
    
    /// <summary>武将前往煽动（替代 ExtensionInterface.call("GoForInstigate")）</summary>
    public static event Action<GameScenario, Person, Point>? OnGoForInstigate;
    
    /// <summary>武将前往寻访（替代 ExtensionInterface.call("GoForSearch")）</summary>
    public static event Action<GameScenario, Person>? OnGoForSearch;
    
    /// <summary>武将前往暗杀（替代 ExtensionInterface.call("GoForAssassinate")）</summary>
    public static event Action<GameScenario, Person>? OnGoForAssassinate;
    
    /// <summary>武将前往间谍（替代 ExtensionInterface.call("GoForSpy")）</summary>
    public static event Action<GameScenario, Person, Point>? OnGoForSpy;
    
    /// <summary>武将前往学习技能（替代 ExtensionInterface.call("GoForStudySkill")）</summary>
    public static event Action<GameScenario, Person>? OnGoForStudySkill;
    
    /// <summary>武将前往学习特技（替代 ExtensionInterface.call("GoForStudyStunt")）</summary>
    public static event Action<GameScenario, Person>? OnGoForStudyStunt;
    
    /// <summary>武将前往学习称号（替代 ExtensionInterface.call("GoForStudyTitle")）</summary>
    public static event Action<GameScenario, Person>? OnGoForStudyTitle;

    // ==================== 武将生命周期事件（续）====================
    
    /// <summary>武将被处决（替代 ExtensionInterface.call("Executed")）</summary>
    public static event Action<GameScenario, Person, Faction>? OnPersonExecuted;
    
    /// <summary>武将离开势力（替代 ExtensionInterface.call("LeaveFaction")）</summary>
    public static event Action<GameScenario, Person>? OnPersonLeftFaction;
    
    /// <summary>武将抵达建筑（替代 ExtensionInterface.call("ArrivedAtArchitecture")）</summary>
    public static event Action<GameScenario, Person, Architecture>? OnPersonArrivedAtArchitecture;
    
    /// <summary>创建新武将（替代 ExtensionInterface.call("CreatePerson")）</summary>
    public static event Action<GameScenario, Person>? OnPersonCreated;
    
    /// <summary>创建子女（替代 ExtensionInterface.call("CreateChildren")）</summary>
    public static event Action<GameScenario, Person>? OnChildrenCreated;
    
    /// <summary>纳入后宫（替代 ExtensionInterface.call("TakeToHouGong")）</summary>
    public static event Action<GameScenario, Person, Person>? OnTakeToHouGong;
    
    /// <summary>前往后宫（替代 ExtensionInterface.call("GoForHouGong")）</summary>
    public static event Action<GameScenario, Person, Person>? OnGoForHouGong;

    // ==================== 事件触发器 ====================
    
    internal static void RaiseJailBreakSuccess(GameScenario scenario, Person person, Captive captive) 
        => OnJailBreakSuccess?.Invoke(scenario, person, captive);
    
    internal static void RaiseAssassinateSuccess(GameScenario scenario, Person assassin, Person target) 
        => OnAssassinateSuccess?.Invoke(scenario, assassin, target);
    
    internal static void RaiseConvinceFailed(GameScenario scenario, Person person) 
        => OnConvinceFailed?.Invoke(scenario, person);
    
    internal static void RaiseConvinceSuccess(GameScenario scenario, Person person) 
        => OnConvinceSuccess?.Invoke(scenario, person);
    
    internal static void RaiseDestroySuccess(GameScenario scenario, Person person, int damage) 
        => OnDestroySuccess?.Invoke(scenario, person, damage);
    
    internal static void RaiseDestroyFailed(GameScenario scenario, Person person) 
        => OnDestroyFailed?.Invoke(scenario, person);
    
    internal static void RaiseGossipSuccess(GameScenario scenario, Person person) 
        => OnGossipSuccess?.Invoke(scenario, person);
    
    internal static void RaiseGossipFailed(GameScenario scenario, Person person) 
        => OnGossipFailed?.Invoke(scenario, person);
    
    internal static void RaiseInformationSuccess(GameScenario scenario, Person person, Information info) 
        => OnInformationSuccess?.Invoke(scenario, person, info);
    
    internal static void RaiseInformationFailed(GameScenario scenario, Person person) 
        => OnInformationFailed?.Invoke(scenario, person);
    
    internal static void RaiseInstigateSuccess(GameScenario scenario, Person person, int damage) 
        => OnInstigateSuccess?.Invoke(scenario, person, damage);
    
    internal static void RaiseInstigateFailed(GameScenario scenario, Person person) 
        => OnInstigateFailed?.Invoke(scenario, person);
    
    internal static void RaiseSpySuccess(GameScenario scenario, Person person, int days) 
        => OnSpySuccess?.Invoke(scenario, person, days);
    
    internal static void RaiseSpyFailed(GameScenario scenario, Person person) 
        => OnSpyFailed?.Invoke(scenario, person);
    
    internal static void RaiseSearchCompleted(GameScenario scenario, Person person, SearchResultPack pack) 
        => OnSearchCompleted?.Invoke(scenario, person, pack);
    
    internal static void RaiseSkillLearned(GameScenario scenario, Person person, Skill skill) 
        => OnSkillLearned?.Invoke(scenario, person, skill);
    
    internal static void RaiseStuntLearnSuccess(GameScenario scenario, Person person, Stunt stunt) 
        => OnStuntLearnSuccess?.Invoke(scenario, person, stunt);
    
    internal static void RaiseStuntLearnFailed(GameScenario scenario, Person person, Stunt stunt) 
        => OnStuntLearnFailed?.Invoke(scenario, person, stunt);
    
    internal static void RaiseTitleLearnSuccess(GameScenario scenario, Person person, Title title) 
        => OnTitleLearnSuccess?.Invoke(scenario, person, title);
    
    internal static void RaiseTitleLearnFailed(GameScenario scenario, Person person, Title title) 
        => OnTitleLearnFailed?.Invoke(scenario, person, title);
    
    internal static void RaisePregnancyDiscovered(GameScenario scenario, Person person) 
        => OnPregnancyDiscovered?.Invoke(scenario, person);
    
    internal static void RaiseTreasureConfiscated(GameScenario scenario, Person person) 
        => OnTreasureConfiscated?.Invoke(scenario, person);
    
    internal static void RaiseCapturedByArchitecture(GameScenario scenario, Person person, Architecture arch) 
        => OnCapturedByArchitecture?.Invoke(scenario, person, arch);
    
    // GoFor系列触发器
    internal static void RaiseGoForConvince(GameScenario scenario, Person person, Person target) 
        => OnGoForConvince?.Invoke(scenario, person, target);
    
    internal static void RaiseGoForQuanXiang(GameScenario scenario, Person person, Person target) 
        => OnGoForQuanXiang?.Invoke(scenario, person, target);
    
    internal static void RaiseGoForDestroy(GameScenario scenario, Person person, Point position) 
        => OnGoForDestroy?.Invoke(scenario, person, position);
    
    internal static void RaiseGoForGossip(GameScenario scenario, Person person, Point position) 
        => OnGoForGossip?.Invoke(scenario, person, position);
    
    internal static void RaiseGoForInformation(GameScenario scenario, Person person, Point position) 
        => OnGoForInformation?.Invoke(scenario, person, position);
    
    internal static void RaiseGoForJailBreak(GameScenario scenario, Person person, Point position) 
        => OnGoForJailBreak?.Invoke(scenario, person, position);
    
    internal static void RaiseGoForInstigate(GameScenario scenario, Person person, Point position) 
        => OnGoForInstigate?.Invoke(scenario, person, position);
    
    internal static void RaiseGoForSearch(GameScenario scenario, Person person) 
        => OnGoForSearch?.Invoke(scenario, person);
    
    internal static void RaiseGoForAssassinate(GameScenario scenario, Person person) 
        => OnGoForAssassinate?.Invoke(scenario, person);
    
    internal static void RaiseGoForSpy(GameScenario scenario, Person person, Point position) 
        => OnGoForSpy?.Invoke(scenario, person, position);
    
    internal static void RaiseGoForStudySkill(GameScenario scenario, Person person) 
        => OnGoForStudySkill?.Invoke(scenario, person);
    
    internal static void RaiseGoForStudyStunt(GameScenario scenario, Person person) 
        => OnGoForStudyStunt?.Invoke(scenario, person);
    
    internal static void RaiseGoForStudyTitle(GameScenario scenario, Person person) 
        => OnGoForStudyTitle?.Invoke(scenario, person);
    
    // 生命周期触发器
    internal static void RaisePersonExecuted(GameScenario scenario, Person person, Faction faction) 
        => OnPersonExecuted?.Invoke(scenario, person, faction);
    
    internal static void RaisePersonLeftFaction(GameScenario scenario, Person person) 
        => OnPersonLeftFaction?.Invoke(scenario, person);
    
    internal static void RaisePersonArrivedAtArchitecture(GameScenario scenario, Person person, Architecture arch) 
        => OnPersonArrivedAtArchitecture?.Invoke(scenario, person, arch);
    
    internal static void RaisePersonCreated(GameScenario scenario, Person person) 
        => OnPersonCreated?.Invoke(scenario, person);
    
    internal static void RaiseChildrenCreated(GameScenario scenario, Person child) 
        => OnChildrenCreated?.Invoke(scenario, child);
    
    internal static void RaiseTakeToHouGong(GameScenario scenario, Person person, Person target) 
        => OnTakeToHouGong?.Invoke(scenario, person, target);
    
    internal static void RaiseGoForHouGong(GameScenario scenario, Person person, Person target) 
        => OnGoForHouGong?.Invoke(scenario, person, target);

    /// <summary>
    /// 清理所有事件订阅（由 ScenarioEvents.ClearAllHandlers() 调用）
    /// </summary>
    public static void ClearAllHandlers()
    {
        OnJailBreakSuccess = null;
        OnAssassinateSuccess = null;
        OnConvinceFailed = null;
        OnConvinceSuccess = null;
        OnDestroySuccess = null;
        OnDestroyFailed = null;
        OnGossipSuccess = null;
        OnGossipFailed = null;
        OnInformationSuccess = null;
        OnInformationFailed = null;
        OnInstigateSuccess = null;
        OnInstigateFailed = null;
        OnSpySuccess = null;
        OnSpyFailed = null;
        OnSearchCompleted = null;
        OnSkillLearned = null;
        OnStuntLearnSuccess = null;
        OnStuntLearnFailed = null;
        OnTitleLearnSuccess = null;
        OnTitleLearnFailed = null;
        OnPregnancyDiscovered = null;
        OnTreasureConfiscated = null;
        OnCapturedByArchitecture = null;
        
        // GoFor系列
        OnGoForConvince = null;
        OnGoForQuanXiang = null;
        OnGoForDestroy = null;
        OnGoForGossip = null;
        OnGoForInformation = null;
        OnGoForJailBreak = null;
        OnGoForInstigate = null;
        OnGoForSearch = null;
        OnGoForAssassinate = null;
        OnGoForSpy = null;
        OnGoForStudySkill = null;
        OnGoForStudyStunt = null;
        OnGoForStudyTitle = null;
        
        // 生命周期
        OnPersonExecuted = null;
        OnPersonLeftFaction = null;
        OnPersonArrivedAtArchitecture = null;
        OnPersonCreated = null;
        OnChildrenCreated = null;
        OnTakeToHouGong = null;
        OnGoForHouGong = null;
    }
}
