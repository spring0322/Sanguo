using System;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public readonly record struct DebateParticipantSeed(
    int PersonId,
    string Name,
    int FactionId,
    int Intelligence,
    int Politics,
    int Glamour,
    int Braveness,
    int Calmness,
    bool IsPlayerControlled = false)
{
    public static DebateParticipantSeed FromPerson(Person person, bool isPlayerControlled = false)
    {
        if (person == null)
        {
            throw new ArgumentNullException(nameof(person));
        }

        return new DebateParticipantSeed(
            person.ID,
            person.Name ?? string.Empty,
            person.BelongedFaction?.ID ?? -1,
            person.Intelligence,
            person.Politics,
            person.Glamour,
            person.Braveness,
            person.Calmness,
            isPlayerControlled);
    }
}

