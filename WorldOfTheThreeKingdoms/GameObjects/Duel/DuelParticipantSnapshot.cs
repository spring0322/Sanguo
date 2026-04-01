using System;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public readonly record struct DuelParticipantSnapshot(
    int PersonId,
    string Name,
    int FactionId,
    int Force,
    int Braveness,
    int Calmness)
{
    public static DuelParticipantSnapshot FromPerson(Person person)
    {
        if (person == null)
        {
            throw new ArgumentNullException(nameof(person));
        }

        return new DuelParticipantSnapshot(
            person.ID,
            person.Name ?? string.Empty,
            person.BelongedFaction?.ID ?? -1,
            person.ChallengeStrength,
            person.Braveness,
            person.Calmness);
    }
}

