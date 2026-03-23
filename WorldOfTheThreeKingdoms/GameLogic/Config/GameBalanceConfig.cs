using System;

namespace Zhsan.GameLogic.Config
{
    public class GameBalanceConfig
    {
        public AIConfig AI { get; set; } = new AIConfig();
        public ArchitectureConfig Architecture { get; set; } = new ArchitectureConfig();
        public CombatConfig Combat { get; set; } = new CombatConfig();
        public RateConfig Rate { get; set; } = new RateConfig();
        public InternalConfig Internal { get; set; } = new InternalConfig();
    }

    public class AIConfig
    {
        public float RoutewayConsumptionRateMax { get; set; } = 0.7f;
        public float RoutewayConsumptionRateLow { get; set; } = 0.3f;
        public int ArmyScaleNormal { get; set; } = 12; // AISectionArmyScaleNormalCosnt
        public int OffensiveArmyScaleDivisor { get; set; } = 60; // AISectionOffensiveArmyScaleDivisor
        public int ArmyScaleNormalUnit { get; set; } = 5;
    }

    public class ArchitectureConfig
    {
        public int DamageConst { get; set; } = 10; // ArchitectureDamageConst
        public int AreaViewDivisor { get; set; } = 8; // ArchitectureAreaViewDivisor
    }

    public class CombatConfig
    {
        public float FireDamageScale { get; set; } = 0.5f;
        public int DamageConst { get; set; } = 500; // DamageConst
        public float FireDamageDivisor { get; set; } = 200f; // FireDamageDivisor
        public float BaseRateOfQibingDamage { get; set; } = 1.0f;
    }

    public class RateConfig
    {
        public float GlobalDamageRate { get; set; } = 1.0f;
        public float FoodReduceDayRate { get; set; } = 0.001f; // FoodReduceDayRate
    }
    
    public class InternalConfig
    {
        public float SurplusRateUnit { get; set; } = 0.001f; // InternalSurplusRateUnit
    }
}
