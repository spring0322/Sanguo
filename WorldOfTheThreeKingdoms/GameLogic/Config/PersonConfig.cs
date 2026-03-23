using System;

namespace Zhsan.GameLogic.Config
{
    public class PersonConfig
    {
        public float InfluenceRateOfCommand { get; set; } = 1.0f;
        public float InfluenceRateOfStrength { get; set; } = 1.0f;
        public float InfluenceRateOfIntelligence { get; set; } = 1.0f;
        public float InfluenceRateOfPolitics { get; set; } = 1.0f;
        public float InfluenceRateOfGlamour { get; set; } = 1.0f;

        public int MultipleOfAgricultureReputation { get; set; } = 1;
        public int MultipleOfAgricultureTechniquePoint { get; set; } = 1;
        public int MultipleOfCommerceReputation { get; set; } = 1;
        public int MultipleOfCommerceTechniquePoint { get; set; } = 1;
        public int MultipleOfDominationReputation { get; set; } = 1;
        public int MultipleOfDominationTechniquePoint { get; set; } = 1;
        public int MultipleOfEnduranceReputation { get; set; } = 1;
        public int MultipleOfEnduranceTechniquePoint { get; set; } = 1;
        public int MultipleOfMoraleReputation { get; set; } = 1;
        public int MultipleOfMoraleTechniquePoint { get; set; } = 1;
        public int MultipleOfRecruitmentReputation { get; set; } = 1;
        public int MultipleOfRecruitmentTechniquePoint { get; set; } = 1;
        public int MultipleOfTacticsReputation { get; set; } = 1;
        public int MultipleOfTacticsTechniquePoint { get; set; } = 1;
        public int MultipleOfTechnologyReputation { get; set; } = 1;
        public int MultipleOfTechnologyTechniquePoint { get; set; } = 1;
        public int MultipleOfTrainingReputation { get; set; } = 1;
        public int MultipleOfTrainingTechniquePoint { get; set; } = 1;

        public int DayLearnTitleDay { get; set; } = 90;
    }
}
