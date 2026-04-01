using System;
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public static class DuelCommitBridge
{
    public static void Complete(MainGameScreen mainGameScreen, DuelRequest request, DuelResult result, Person leftPerson, Person rightPerson)
    {
        if (mainGameScreen == null)
        {
            throw new ArgumentNullException(nameof(mainGameScreen));
        }

        mainGameScreen.dantiaoLayer = null;
        mainGameScreen.duelController = null;

        if (mainGameScreen.cloudLayer != null)
        {
            mainGameScreen.cloudLayer.IsStart = false;
            mainGameScreen.cloudLayer.IsVisible = false;
            mainGameScreen.cloudLayer.Reverse = false;
        }

        TroopDamage damage = request?.Damage;
        if (damage != null)
        {
            damage.ChallengeHappened = true;
            damage.ChallengeStarted = false;
            damage.ChallengeResult = result.LegacyResult;
            damage.ChallengeSourcePerson = leftPerson;
            damage.ChallengeDestinationPerson = rightPerson;
            mainGameScreen.EnableUpdate = true;
            return;
        }

        mainGameScreen.EnableUpdate = true;
        mainGameScreen.ReturnToMainMenu();
    }
}

