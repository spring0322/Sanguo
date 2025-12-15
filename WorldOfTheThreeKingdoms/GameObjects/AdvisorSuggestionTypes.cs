using System;
using System.Collections.Generic;

namespace GameObjects
{
    /// <summary>
    /// 军师建议类型枚举 - 通用类型
    /// </summary>
    public enum AdvisorSuggestionKind
    {
        None = 0,
        Military = 1,
        Diplomatic = 2,
        Internal = 3,
        Personnel = 4,
        Strategic = 5,
        Emergency = 6,
        Technology = 7,
        Intelligence = 8
    }

    /// <summary>
    /// 军师建议详细信息
    /// </summary>
    public class AdvisorSuggestion
    {
        public SpecificSuggestionKind Kind { get; set; }
        public AdvisorSuggestionKind GeneralKind { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string DetailedAdvice { get; set; }
        public int Priority { get; set; }
        public int Urgency { get; set; }
        public bool IsResolved { get; set; }
        public DateTime GeneratedTime { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }

    public enum SpecificSuggestionKind
    {
        None = 0,
        EnemyAttack,
        PersonRecruit,
        InternalAffairs,
        DiplomaticAction,
        MilitaryExpansion,
        DefensePreparation,
        ResourceManagement,
        TechnologyResearch,
        TroopTraining,
        ArchitectureUpgrade
    }
}