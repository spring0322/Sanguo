using System;

namespace Zhsan.GameLogic.Config
{
    public class TroopConfig
    {
        public int NewTroopProtectionTurns { get; set; } = 3;
        
        public float BaseRateOfQibingDamage { get; set; } = 1.0f;
        
        public float DefenceRateOnSubdueBubing { get; set; } = 1.0f;
        public float DefenceRateOnSubdueNubing { get; set; } = 1.0f;
        public float DefenceRateOnSubdueQibing { get; set; } = 1.0f;
        public float DefenceRateOnSubdueQixie { get; set; } = 1.0f;
        public float DefenceRateOnSubdueShuijun { get; set; } = 1.0f;

        public float OffenceRateOnSubdueBubing { get; set; } = 1.0f;
        public float OffenceRateOnSubdueNubing { get; set; } = 1.0f;
        public float OffenceRateOnSubdueQibing { get; set; } = 1.0f;
        public float OffenceRateOnSubdueQixie { get; set; } = 1.0f;
        public float OffenceRateOnSubdueShuijun { get; set; } = 1.0f;

        public float MoraleChangeRateOnOutOfFood { get; set; } = 1.0f;

        public float MultipleOfArmyExperience { get; set; } = 1.0f;
        public float MultipleOfCombatTechniquePoint { get; set; } = 1.0f;
        public float MultipleOfDefenceOnArchitecture { get; set; } = 1.0f;
        public float MultipleOfLeaderExperience { get; set; } = 1.0f;
        public float MultipleOfStratagemTechniquePoint { get; set; } = 1.0f;

        public float RateOfBoost { get; set; } = 1.0f;
        public float RateOfCriticalArchitectureDamage { get; set; } = 1.0f;
        public float RateOfCriticalDamageReceived { get; set; } = 1.0f;
        public float RateOfDefence { get; set; } = 1.0f;
        public float RateOfFireDamage { get; set; } = 1.0f;
        public float RateOfFireProtection { get; set; } = 1.0f;
        public float RateOfGongxin { get; set; } = 1.0f;
        public float RateOfInjuryOnCriticalStrike { get; set; } = 1.0f;
        public float RateOfMovability { get; set; } = 1.0f;
        public float RateOfOffence { get; set; } = 1.0f;
        public float RateOfQibingDamage { get; set; } = 1.0f;

        public float StuntArchitectureDamageRate { get; set; } = 1.0f;
    }
}
