using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelPlaybackScriptBuilder
{
    public DuelPlaybackScript Build(DuelPlaybackScript reusableScript, DuelSessionState sessionState, in DuelExchangeResult exchangeResult)
    {
        if (reusableScript == null)
        {
            throw new ArgumentNullException(nameof(reusableScript));
        }

        if (sessionState == null)
        {
            throw new ArgumentNullException(nameof(sessionState));
        }

        reusableScript.Reset(sessionState.Outcome);
        reusableScript.Add(new DuelPlaybackBeat("prepare", 0.15f, true));

        if (exchangeResult.LeftActsFirst)
        {
            reusableScript.Add(new DuelPlaybackBeat("left_attack", 0.25f, true));
            reusableScript.Add(new DuelPlaybackBeat("right_attack", 0.25f, true));
        }
        else
        {
            reusableScript.Add(new DuelPlaybackBeat("right_attack", 0.25f, true));
            reusableScript.Add(new DuelPlaybackBeat("left_attack", 0.25f, true));
        }

        if (sessionState.Outcome != DuelOutcome.None)
        {
            reusableScript.Add(new DuelPlaybackBeat("finish", 0.45f, true));
        }
        else
        {
            reusableScript.Add(new DuelPlaybackBeat("reset", 0.15f, true));
        }

        return reusableScript;
    }
}
