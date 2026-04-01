using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelPlaybackScript
{
    private readonly List<DuelPlaybackBeat> beats;

    public IReadOnlyList<DuelPlaybackBeat> Beats => beats;

    public DuelOutcome Outcome { get; private set; } = DuelOutcome.None;

    public DuelPlaybackScript(int initialCapacity = 8)
    {
        beats = new List<DuelPlaybackBeat>(initialCapacity);
    }

    public void Reset(DuelOutcome outcome)
    {
        Outcome = outcome;
        beats.Clear();
    }

    public void Add(in DuelPlaybackBeat beat)
    {
        beats.Add(beat);
    }
}
