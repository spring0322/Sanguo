using System;
using GameManager;
using GameObjects.PersonDetail;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects.AI;

internal enum AssaultPrecheckFailureReason : byte
{
    None = 0,
    MissingSource = 1,
    MissingTarget = 2,
    NoLeader = 3,
    NoEligibleMilitary = 4,
    LowMorale = 5,
    LowFood = 6
}

internal readonly record struct AssaultSortiePrecheckResult(
    bool Passed,
    AssaultPrecheckFailureReason FailureReason,
    int SourceArchitectureId,
    int TargetArchitectureId,
    int BestMobilizableMorale,
    int EligibleMilitaryCount,
    int LeaderableCount,
    int RequiredFoodLock,
    int ReserveFoodFloor,
    int RemainingFoodAfterLock,
    int EstimatedMarchDays)
{
    public int ResolveScoreBias(int targetMorale)
    {
        if (!Passed)
        {
            return 0;
        }

        int moraleBias = Math.Clamp((BestMobilizableMorale - AssaultSortiePrecheckService.MinAssaultMorale) / 4, 0, 10);
        int foodHeadroom = RemainingFoodAfterLock - ReserveFoodFloor;
        int foodBias = RequiredFoodLock > 0
            ? Math.Clamp(foodHeadroom / Math.Max(1, RequiredFoodLock / 2), 0, 10)
            : 0;
        int targetMoralePenalty = Math.Clamp((targetMorale - AssaultSortiePrecheckService.MinAssaultMorale) / 8, 0, 6);
        return moraleBias + foodBias - targetMoralePenalty;
    }
}

internal static class AssaultSortiePrecheckService
{
    internal const int MinAssaultMorale = 60;

    private const int MinLeaderCommand = 40;
    private const float MinEligibleScales = 3f;
    private const int MinFoodReserveDays = 12;
    private const int MinBudgetDays = 6;
    private const int MaxTrackedTroopSlots = 3;

    public static Architecture ResolveSourceArchitecture(Legion legion)
    {
        if (legion == null) return null;

        Architecture troopStart = legion.GetLegionTroopFactionStartArchitecture();
        if (troopStart != null)
        {
            return troopStart;
        }

        if (legion.StartArchitecture != null && legion.StartArchitecture.BelongedFaction == legion.BelongedFaction)
        {
            return legion.StartArchitecture;
        }

        if (legion.CoreTroop != null && !legion.CoreTroop.Destroyed)
        {
            Architecture coreStart = legion.CoreTroop.StartingArchitecture ?? legion.CoreTroop.BelongedArchitecture;
            if (coreStart != null && coreStart.BelongedFaction == legion.BelongedFaction)
            {
                return coreStart;
            }
        }

        return legion.BelongedFaction?.Capital;
    }

    public static AssaultSortiePrecheckResult EvaluateOffensiveSortie(
        Architecture source,
        Architecture target,
        int sortieBudgetPermille,
        int lockedFood)
    {
        if (source == null)
        {
            return CreateFailure(AssaultPrecheckFailureReason.MissingSource, -1, target?.ID ?? -1);
        }

        if (target == null)
        {
            return CreateFailure(AssaultPrecheckFailureReason.MissingTarget, source.ID, -1);
        }

        int leaderableCount = CountLeaderablePersons(source);
        if (leaderableCount <= 0)
        {
            return CreateFailure(AssaultPrecheckFailureReason.NoLeader, source.ID, target.ID);
        }

        int trackedSlots = ResolveTrackedTroopSlots(sortieBudgetPermille, leaderableCount);
        Span<int> topFoodLocks = stackalloc int[MaxTrackedTroopSlots];
        int trackedCount = 0;
        int eligibleMilitaryCount = 0;
        int bestMobilizableMorale = 0;
        int estimatedMarchDays = 0;

        double distance = ResolveDistance(source, target);
        for (int i = 0; i < source.Militaries.Count; i++)
        {
            Military military = source.Militaries[i] as Military;
            if (!IsEligibleMilitary(military))
            {
                continue;
            }

            eligibleMilitaryCount++;
            if (military.Morale > bestMobilizableMorale)
            {
                bestMobilizableMorale = military.Morale;
            }

            if (military.Morale < MinAssaultMorale)
            {
                continue;
            }

            int marchDays = Math.Max(1, military.TransferDays(distance));
            if (marchDays > estimatedMarchDays)
            {
                estimatedMarchDays = marchDays;
            }

            int budgetDays = ResolveBudgetDays(marchDays, military.RationDays);
            int candidateFoodLock = Troop.GetConservativePlanningFoodCostPerDay(military) * budgetDays;
            TryInsertDescending(topFoodLocks, ref trackedCount, trackedSlots, candidateFoodLock);
        }

        if (eligibleMilitaryCount <= 0)
        {
            return CreateFailure(AssaultPrecheckFailureReason.NoEligibleMilitary, source.ID, target.ID, leaderableCount: leaderableCount);
        }

        if (bestMobilizableMorale < MinAssaultMorale)
        {
            return CreateFailure(
                AssaultPrecheckFailureReason.LowMorale,
                source.ID,
                target.ID,
                bestMobilizableMorale,
                eligibleMilitaryCount,
                leaderableCount);
        }

        if (trackedCount <= 0)
        {
            return CreateFailure(
                AssaultPrecheckFailureReason.NoEligibleMilitary,
                source.ID,
                target.ID,
                bestMobilizableMorale,
                eligibleMilitaryCount,
                leaderableCount);
        }

        int requiredFoodLock = 0;
        for (int i = 0; i < trackedCount; i++)
        {
            requiredFoodLock += topFoodLocks[i];
        }

        int normalizedLockedFood = Math.Max(0, lockedFood);
        int reserveFoodFloor = ResolveReserveFoodFloor(source);
        int remainingFoodAfterLock = source.Food - normalizedLockedFood - requiredFoodLock;
        if (remainingFoodAfterLock < reserveFoodFloor)
        {
            return CreateFailure(
                AssaultPrecheckFailureReason.LowFood,
                source.ID,
                target.ID,
                bestMobilizableMorale,
                eligibleMilitaryCount,
                leaderableCount,
                requiredFoodLock,
                reserveFoodFloor,
                remainingFoodAfterLock,
                estimatedMarchDays);
        }

        return new AssaultSortiePrecheckResult(
            true,
            AssaultPrecheckFailureReason.None,
            source.ID,
            target.ID,
            bestMobilizableMorale,
            eligibleMilitaryCount,
            leaderableCount,
            requiredFoodLock,
            reserveFoodFloor,
            remainingFoodAfterLock,
            estimatedMarchDays);
    }

    private static AssaultSortiePrecheckResult CreateFailure(
        AssaultPrecheckFailureReason reason,
        int sourceArchitectureId,
        int targetArchitectureId,
        int bestMobilizableMorale = 0,
        int eligibleMilitaryCount = 0,
        int leaderableCount = 0,
        int requiredFoodLock = 0,
        int reserveFoodFloor = 0,
        int remainingFoodAfterLock = 0,
        int estimatedMarchDays = 0)
    {
        return new AssaultSortiePrecheckResult(
            false,
            reason,
            sourceArchitectureId,
            targetArchitectureId,
            bestMobilizableMorale,
            eligibleMilitaryCount,
            leaderableCount,
            requiredFoodLock,
            reserveFoodFloor,
            remainingFoodAfterLock,
            estimatedMarchDays);
    }

    private static bool IsEligibleMilitary(Military military)
    {
        return military != null &&
               military.Kind != null &&
               military.Kind.Movable &&
               !military.IsTransport &&
               military.Quantity > 0 &&
               military.Scales >= MinEligibleScales &&
               military.InjuryQuantity < military.Kind.MinScale;
    }

    private static int CountLeaderablePersons(Architecture source)
    {
        if (source.Persons == null) return 0;

        int leaderableCount = 0;
        for (int i = 0; i < source.Persons.Count; i++)
        {
            Person person = source.Persons[i] as Person;
            if (person == null) continue;
            if (person.LocationArchitecture != source) continue;
            if (person.LocationTroop != null) continue;
            if (person.Status != PersonStatus.Normal) continue;
            if (person.IsCaptive || person.NvGuan) continue;
            if (person.Command < MinLeaderCommand) continue;
            leaderableCount++;
        }

        return leaderableCount;
    }

    private static int ResolveTrackedTroopSlots(int sortieBudgetPermille, int leaderableCount)
    {
        int desiredSlots = sortieBudgetPermille switch
        {
            >= 700 => 3,
            >= 450 => 2,
            _ => 1
        };

        desiredSlots = Math.Min(desiredSlots, leaderableCount);
        if (desiredSlots < 1)
        {
            desiredSlots = 1;
        }

        return Math.Min(desiredSlots, MaxTrackedTroopSlots);
    }

    private static int ResolveBudgetDays(int marchDays, int rationDays)
    {
        int budgetDays = (int)Math.Ceiling(marchDays * 1.5);
        if (budgetDays < MinBudgetDays)
        {
            budgetDays = MinBudgetDays;
        }

        if (rationDays > 0 && budgetDays > rationDays)
        {
            budgetDays = rationDays;
        }

        if (budgetDays < 1)
        {
            budgetDays = 1;
        }

        return budgetDays;
    }

    private static int ResolveReserveFoodFloor(Architecture source)
    {
        int dailyReserveFood = source.FoodCostPerDayOfAllMilitaries * MinFoodReserveDays;
        int storageReserveFood = source.FoodCeiling / 10;
        return Math.Max(dailyReserveFood, storageReserveFood);
    }

    private static double ResolveDistance(Architecture source, Architecture target)
    {
        GameScenario scenario = Session.Current.Scenario;
        if (source.ArchitectureArea != null && target.ArchitectureArea != null)
        {
            return scenario.GetDistance(source.ArchitectureArea, target.ArchitectureArea);
        }

        return scenario.GetDistance(source.Position, target.Position);
    }

    private static void TryInsertDescending(Span<int> buffer, ref int count, int limit, int value)
    {
        if (limit <= 0 || value <= 0)
        {
            return;
        }

        if (count >= limit && value <= buffer[limit - 1])
        {
            return;
        }

        int insertIndex = count < limit ? count : limit - 1;
        if (count < limit)
        {
            count++;
        }

        while (insertIndex > 0 && buffer[insertIndex - 1] < value)
        {
            if (insertIndex < limit)
            {
                buffer[insertIndex] = buffer[insertIndex - 1];
            }
            insertIndex--;
        }

        buffer[insertIndex] = value;
    }
}
