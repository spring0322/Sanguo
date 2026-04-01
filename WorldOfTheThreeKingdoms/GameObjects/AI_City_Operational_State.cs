using System;
using GameManager;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects
{
    internal enum AICityOperationalState
    {
        Recovery,
        Balanced,
        MilitaryBuildUp,
        WartimeOverdraft
    }

    internal readonly record struct AISortieMoraleRecoverySnapshot(
        int EligibleMilitaryCount,
        int SortieReadyMilitaryCount,
        int BestMobilizableMorale,
        int ReserveFoodFloor,
        int AvailableFoodAfterReserve)
    {
        internal bool NeedsTrainingPriority =>
            EligibleMilitaryCount > 0 &&
            SortieReadyMilitaryCount == 0 &&
            AvailableFoodAfterReserve > 0;
    }

    public partial class Architecture
    {
        internal AICityOperationalState GetAICityOperationalState()
        {
            int dominationMargin = this.GetRecruitmentDominationMarginForAI();
            int recoveryMargin = this.GetRecoveryDominationMarginForAI();
            int buildUpMargin = this.GetBuildUpDominationMarginForAI();
            float moraleRatio = this.GetMoraleHealthRatioForAI();
            bool immediateThreat = this.HasHostileTroopsInView() || this.RecentlyAttacked > 0;
            bool needsTroops = this.BelongedFaction != null && this.BelongedFaction.NeedMoreTroops();
            bool supplyReady = this.Population > this.RecruitmentPopulationBoundary &&
                               this.Fund > this.EnoughFund &&
                               this.Food > this.EnoughFood;
            bool canSustainBuildUp = dominationMargin >= buildUpMargin &&
                                     moraleRatio >= 0.55f &&
                                     this.CountLocalActiveOfficersForAI() >= 2 &&
                                     this.EstimateLocalRecoveryCapacityForAI() >= 1.2f;

            if (dominationMargin < 0 || dominationMargin < recoveryMargin || moraleRatio < 0.35f)
            {
                if (this.FrontLine &&
                    immediateThreat &&
                    needsTroops &&
                    dominationMargin >= -Math.Max(4, recoveryMargin / 2) &&
                    moraleRatio >= 0.25f)
                {
                    return AICityOperationalState.WartimeOverdraft;
                }

                return AICityOperationalState.Recovery;
            }

            if (this.FrontLine &&
                immediateThreat &&
                needsTroops &&
                dominationMargin >= -Math.Max(2, recoveryMargin / 3) &&
                moraleRatio >= 0.30f)
            {
                return AICityOperationalState.WartimeOverdraft;
            }

            if (this.FrontLine && needsTroops && supplyReady && canSustainBuildUp)
            {
                return AICityOperationalState.MilitaryBuildUp;
            }

            return AICityOperationalState.Balanced;
        }

        internal float CalculateInternalHealthScoreForAI()
        {
            float score = 0f;
            float saturation = this.InternalAffairSaturationThreshold;

            if (_architectureKind.HasAgriculture && this.Agriculture < this.AgricultureCeiling * saturation)
            {
                score += (this.AgricultureCeiling - this.Agriculture) / 600f;
            }

            if (_architectureKind.HasCommerce && this.Commerce < this.CommerceCeiling * saturation)
            {
                score += (this.CommerceCeiling - this.Commerce) / 600f;
            }

            if (_architectureKind.HasTechnology && this.Technology < this.TechnologyCeiling * saturation)
            {
                score += (this.TechnologyCeiling - this.Technology) / 400f;
            }

            if (_architectureKind.HasDomination && this.Domination < this.DominationCeiling * saturation)
            {
                score += (this.DominationCeiling - this.Domination) / 500f;
            }

            if (_architectureKind.HasMorale && this.Morale < this.MoraleCeiling * saturation)
            {
                score += (this.MoraleCeiling - this.Morale) / 500f;
            }

            if (_architectureKind.HasEndurance && this.Endurance < this.EnduranceCeiling * 0.9f)
            {
                float enduranceScore = (this.EnduranceCeiling - this.Endurance) / 300f;
                if (this.RecentlyAttacked > 0)
                {
                    enduranceScore *= 2f;
                }

                score += enduranceScore;
            }

            if (!this.IsFundEnough)
            {
                score *= 0.8f;
            }

            if (!this.IsFoodEnough)
            {
                score *= 0.8f;
            }

            return score;
        }

        internal float CalculateRecruitmentReadinessScoreForAI()
        {
            if (this.BelongedFaction == null || !_architectureKind.HasPopulation)
            {
                return 0f;
            }

            float score = 0f;

            if (this.Population > this.PopulationCeiling * 0.7f)
            {
                score += 2f;
            }

            if (this.Fund > this.EnoughFund)
            {
                score += 1.5f;
            }

            if (this.Food > this.EnoughFood)
            {
                score += 1.5f;
            }

            if (this.BelongedFaction.NeedMoreTroops())
            {
                score += 2f;
            }

            if (this.FrontLine)
            {
                score += 1.5f;
            }

            if (this.HasHostileTroopsInView() || this.RecentlyAttacked > 0)
            {
                score += 2f;
            }

            return score;
        }

        internal float CalculateCrossCitySupportUrgencyForAI()
        {
            if (this.CanResolveInternalHealthLocallyForAI())
            {
                return 0f;
            }

            int activeOfficers = this.CountLocalActiveOfficersForAI();
            int supportFloor = this.GetStrategicPersonnelFloorForAI();
            float score = this.CalculateInternalHealthScoreForAI() * 2.5f;

            switch (this.GetAICityOperationalState())
            {
                case AICityOperationalState.Recovery:
                    score += 35f;
                    break;
                case AICityOperationalState.MilitaryBuildUp:
                    score += 15f;
                    break;
                case AICityOperationalState.WartimeOverdraft:
                    score += 30f;
                    break;
            }

            if (activeOfficers < supportFloor)
            {
                score += (supportFloor - activeOfficers) * 18f;
            }

            if (this.IsRecentlyOccupied())
            {
                score += 12f;
            }

            return score;
        }

        internal bool CanResolveInternalHealthLocallyForAI()
        {
            int activeOfficers = this.CountLocalActiveOfficersForAI();
            int recoveryOfficers = this.CountLocalRecoveryOfficersForAI(160);
            float recoveryCapacity = this.EstimateLocalRecoveryCapacityForAI();

            return this.GetAICityOperationalState() switch
            {
                AICityOperationalState.Recovery => activeOfficers >= 2 && recoveryOfficers >= 1 && recoveryCapacity >= 1.1f,
                AICityOperationalState.MilitaryBuildUp => activeOfficers >= 2 && recoveryOfficers >= 1,
                AICityOperationalState.WartimeOverdraft => activeOfficers >= 3 && recoveryOfficers >= 1 && recoveryCapacity >= 1.25f,
                _ => activeOfficers >= 1
            };
        }

        internal bool NeedsStrategicPersonnelSupportForAI(int desiredQuota)
        {
            if (desiredQuota <= this.PersonCount)
            {
                return false;
            }

            AICityOperationalState state = this.GetAICityOperationalState();
            bool canResolveLocally = this.CanResolveInternalHealthLocallyForAI();
            int supportFloor = Math.Min(desiredQuota, this.GetStrategicPersonnelFloorForAI());
            if (this.PersonCount < supportFloor)
            {
                return state == AICityOperationalState.WartimeOverdraft || !canResolveLocally;
            }

            return (state == AICityOperationalState.Recovery || state == AICityOperationalState.WartimeOverdraft) &&
                   !canResolveLocally;
        }

        internal int GetStrategicPersonnelFloorForAI()
        {
            int floor = 1;
            AICityOperationalState state = this.GetAICityOperationalState();

            if (state == AICityOperationalState.Recovery)
            {
                floor = 2;
            }
            else if (state == AICityOperationalState.MilitaryBuildUp)
            {
                floor = 2;
            }
            else if (state == AICityOperationalState.WartimeOverdraft)
            {
                floor = 3;
            }

            if (this.FrontLine || this.IsRecentlyOccupied())
            {
                floor = Math.Max(floor, 3);
            }

            if ((this.BelongedFaction != null && this.BelongedFaction.Capital == this) ||
                this.HasHostileTroopsInView() ||
                this.RecentlyAttacked > 0)
            {
                floor = Math.Max(floor, 4);
            }

            return floor;
        }

        internal int GetProtectedPersonnelFloorForAI()
        {
            int floor = this.GetStrategicPersonnelFloorForAI();
            if (this.GetAICityOperationalState() == AICityOperationalState.Balanced)
            {
                floor = Math.Max(1, floor - 1);
            }

            return floor;
        }

        internal bool CanSpareOfficerForStrategicTransferForAI(Person p)
        {
            if (!this.CanUseOfficerForLocalAI(p))
            {
                return false;
            }

            if (this.GetRecoveryAbilityForAI(p) < 160)
            {
                return true;
            }

            if (this.GetAICityOperationalState() == AICityOperationalState.Balanced &&
                this.CanResolveInternalHealthLocallyForAI())
            {
                return true;
            }

            return this.CountLocalRecoveryOfficersForAI(160) > 1;
        }

        internal bool CanExecuteAIRecruitment(bool creatingNewMilitary)
        {
            if (this.BelongedFaction == null || Session.Current == null || Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                return true;
            }

            if (this.NeedsSortieMoraleRecoveryForAI())
            {
                return false;
            }

            int dominationMargin = this.GetRecruitmentDominationMarginForAI();
            float moraleRatio = this.GetMoraleHealthRatioForAI();
            AICityOperationalState state = this.GetAICityOperationalState();

            return state switch
            {
                AICityOperationalState.Recovery => false,
                AICityOperationalState.Balanced => !creatingNewMilitary &&
                                                   dominationMargin >= this.GetBalancedRecruitmentMarginForAI() &&
                                                   moraleRatio >= 0.45f,
                AICityOperationalState.MilitaryBuildUp => dominationMargin >= this.GetBalancedRecruitmentMarginForAI() &&
                                                         moraleRatio >= 0.50f,
                AICityOperationalState.WartimeOverdraft => (this.HasHostileTroopsInView() || this.RecentlyAttacked > 0) &&
                                                          dominationMargin >= -Math.Max(2, this.GetRecoveryDominationMarginForAI() / 2) &&
                                                          moraleRatio >= 0.25f &&
                                                          (!creatingNewMilitary || this.MilitaryCount <= this.PersonCount + 1),
                _ => false
            };
        }

        internal void ApplyOperationalStateWorkBias(float[] workWeights)
        {
            if (workWeights == null || workWeights.Length < 9)
            {
                return;
            }

            if (this.NeedsSortieMoraleRecoveryForAI())
            {
                for (int i = 0; i < 6; i++)
                {
                    if (workWeights[i] > 30f)
                    {
                        workWeights[i] = 30f;
                    }
                }

                workWeights[6] = Math.Max(workWeights[6], 280f);
                workWeights[7] = 0f;
            }

            switch (this.GetAICityOperationalState())
            {
                case AICityOperationalState.Recovery:
                    if (_architectureKind.HasDomination)
                    {
                        workWeights[3] = Math.Max(workWeights[3], 220f);
                    }

                    if (_architectureKind.HasMorale)
                    {
                        workWeights[4] = Math.Max(workWeights[4], 200f);
                    }

                    if (_architectureKind.HasEndurance && this.RecentlyAttacked > 0)
                    {
                        workWeights[5] = Math.Max(workWeights[5], 210f);
                    }

                    workWeights[6] = Math.Min(workWeights[6], this.RecentlyAttacked > 0 ? 120f : 60f);
                    workWeights[7] = 0f;
                    break;

                case AICityOperationalState.Balanced:
                    if (_architectureKind.HasDomination && this.GetRecruitmentDominationMarginForAI() < this.GetBalancedRecruitmentMarginForAI())
                    {
                        workWeights[3] = Math.Max(workWeights[3], 150f);
                    }

                    if (_architectureKind.HasMorale && this.GetMoraleHealthRatioForAI() < 0.55f)
                    {
                        workWeights[4] = Math.Max(workWeights[4], 145f);
                    }

                    if (workWeights[6] > 160f)
                    {
                        workWeights[6] = 160f;
                    }

                    if (workWeights[7] > 135f)
                    {
                        workWeights[7] = 135f;
                    }
                    break;

                case AICityOperationalState.MilitaryBuildUp:
                    workWeights[6] = Math.Max(workWeights[6], 180f);
                    workWeights[7] = Math.Max(workWeights[7], 165f);

                    if (_architectureKind.HasDomination)
                    {
                        workWeights[3] = Math.Max(workWeights[3], 90f);
                    }

                    if (_architectureKind.HasMorale)
                    {
                        workWeights[4] = Math.Max(workWeights[4], 90f);
                    }
                    break;

                case AICityOperationalState.WartimeOverdraft:
                    workWeights[6] = Math.Max(workWeights[6], 210f);
                    workWeights[7] = Math.Max(workWeights[7], 190f);

                    if (_architectureKind.HasEndurance)
                    {
                        workWeights[5] = Math.Max(workWeights[5], 220f);
                    }

                    if (_architectureKind.HasDomination)
                    {
                        workWeights[3] = Math.Max(workWeights[3], 40f);
                    }

                    if (_architectureKind.HasMorale)
                    {
                        workWeights[4] = Math.Max(workWeights[4], 40f);
                    }
                    break;
            }

            if (this.kezhenzai())
            {
                workWeights[8] = Math.Max(workWeights[8], 260f);
            }
        }

        internal int GetRecruitmentDominationMarginForAI()
        {
            return _architectureKind.HasDomination ? this.Domination - Session.Parameters.RecruitmentDomination : int.MaxValue;
        }

        internal float GetMoraleHealthRatioForAI()
        {
            return !_architectureKind.HasMorale || this.MoraleCeiling <= 0
                ? 1f
                : (float)this.Morale / this.MoraleCeiling;
        }

        internal bool NeedsSortieMoraleRecoveryForAI()
        {
            if (this.BelongedFaction == null || Session.Current == null || Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                return false;
            }

            if (!this.IsFoodAbundant || this.RecentlyAttacked > 0)
            {
                return false;
            }

            return this.EvaluateSortieMoraleRecoveryForAI().NeedsTrainingPriority;
        }

        internal AISortieMoraleRecoverySnapshot EvaluateSortieMoraleRecoveryForAI()
        {
            int eligibleMilitaryCount = 0;
            int sortieReadyMilitaryCount = 0;
            int bestMobilizableMorale = 0;

            for (int i = 0; i < this.Militaries.Count; i++)
            {
                Military military = this.Militaries[i] as Military;
                if (!global::GameObjects.AI.AssaultSortiePrecheckService.IsMobilizableMilitary(military))
                {
                    continue;
                }

                eligibleMilitaryCount++;
                if (military.Morale > bestMobilizableMorale)
                {
                    bestMobilizableMorale = military.Morale;
                }

                if (military.Morale >= global::GameObjects.AI.AssaultSortiePrecheckService.MinAssaultMorale)
                {
                    sortieReadyMilitaryCount++;
                }
            }

            int reserveFoodFloor = global::GameObjects.AI.AssaultSortiePrecheckService.ResolveReserveFoodFloor(this);
            int emergencyReserveFoodFloor = this.GetEmergencyFoodReserveFloor();
            if (emergencyReserveFoodFloor > reserveFoodFloor)
            {
                reserveFoodFloor = emergencyReserveFoodFloor;
            }

            return new AISortieMoraleRecoverySnapshot(
                eligibleMilitaryCount,
                sortieReadyMilitaryCount,
                bestMobilizableMorale,
                reserveFoodFloor,
                this.Food - reserveFoodFloor);
        }

        internal int CountLocalActiveOfficersForAI()
        {
            int count = 0;
            if (this.Persons == null)
            {
                return count;
            }

            foreach (GameObject obj in this.Persons)
            {
                Person p = obj as Person;
                if (this.CanUseOfficerForLocalAI(p))
                {
                    count++;
                }
            }

            return count;
        }

        private int GetRecoveryDominationMarginForAI()
        {
            return !_architectureKind.HasDomination || this.DominationCeiling <= 0
                ? 0
                : Math.Max(6, this.DominationCeiling / 15);
        }

        private int GetBalancedRecruitmentMarginForAI()
        {
            return !_architectureKind.HasDomination || this.DominationCeiling <= 0
                ? 0
                : Math.Max(this.GetRecoveryDominationMarginForAI() + 2, 8) + this.GetRecruitmentRiskBufferForAI();
        }

        private int GetBuildUpDominationMarginForAI()
        {
            return !_architectureKind.HasDomination || this.DominationCeiling <= 0
                ? 0
                : Math.Max(this.GetBalancedRecruitmentMarginForAI() + 2, (this.DominationCeiling / 10) + this.GetRecruitmentRiskBufferForAI());
        }

        private int GetRecruitmentRiskBufferForAI()
        {
            if (!_architectureKind.HasPopulation || this.Population >= this.RecruitmentPopulationBoundary)
            {
                return 0;
            }

            return this.Population <= this.RecruitmentPopulationBoundary / 2 ? 5 : 3;
        }

        private int CountLocalRecoveryOfficersForAI(int minimumAbility)
        {
            int count = 0;
            if (this.Persons == null)
            {
                return count;
            }

            foreach (GameObject obj in this.Persons)
            {
                Person p = obj as Person;
                if (!this.CanUseOfficerForLocalAI(p))
                {
                    continue;
                }

                if (this.GetRecoveryAbilityForAI(p) >= minimumAbility)
                {
                    count++;
                }
            }

            return count;
        }

        private float EstimateLocalRecoveryCapacityForAI()
        {
            int best = 0;
            int second = 0;

            if (this.Persons == null)
            {
                return 0f;
            }

            foreach (GameObject obj in this.Persons)
            {
                Person p = obj as Person;
                if (!this.CanUseOfficerForLocalAI(p))
                {
                    continue;
                }

                int ability = this.GetRecoveryAbilityForAI(p);
                if (ability > best)
                {
                    second = best;
                    best = ability;
                }
                else if (ability > second)
                {
                    second = ability;
                }
            }

            return (best + second) / 220f;
        }

        private int GetRecoveryAbilityForAI(Person p)
        {
            int ability = Math.Max(p.DominationAbility, p.MoraleAbility);
            if (this.RecentlyAttacked > 0)
            {
                ability = Math.Max(ability, p.EnduranceAbility);
            }

            return ability;
        }

        private bool CanUseOfficerForLocalAI(Person p)
        {
            return p != null &&
                   p.Alive &&
                   !p.IsCaptive &&
                   p.BelongedCaptive == null &&
                   p.Status == PersonStatus.Normal &&
                   p.LocationArchitecture == this;
        }
    }
}
