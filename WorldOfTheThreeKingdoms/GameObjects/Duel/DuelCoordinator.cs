using System;
using GameManager;
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public static class DuelCoordinator
{
    private static DuelRequest PendingDemoRequest;

    public static void QueueDemoRequest(int leftPersonId, int rightPersonId)
    {
        PendingDemoRequest = DuelRequest.CreateDemo(leftPersonId, rightPersonId);
    }

    public static void ClearPendingDemoRequest()
    {
        PendingDemoRequest = null;
    }

    public static bool TryStartPendingDemo(MainGameScreen mainGameScreen, out string error)
    {
        DuelRequest request = PendingDemoRequest;
        PendingDemoRequest = null;

        if (request == null)
        {
            error = string.Empty;
            return false;
        }

        return TryStart(mainGameScreen, request, out error);
    }

    public static bool TryStartBattle(Person left, Person right, TroopDamage damage, out string error)
    {
        if (left == null || right == null)
        {
            error = "Challenge persons are null.";
            return false;
        }

        DuelRequest request = DuelRequest.CreateBattle(left, right, damage);
        return TryStart(Session.MainGame?.mainGameScreen, request, out error);
    }

    public static bool TryStart(MainGameScreen mainGameScreen, DuelRequest request, out string error)
    {
        if (mainGameScreen == null)
        {
            error = "MainGameScreen is null.";
            return false;
        }

        if (request == null)
        {
            error = "DuelRequest is null.";
            return false;
        }

        if (mainGameScreen.duelController != null || mainGameScreen.dantiaoLayer != null)
        {
            error = "A duel is already running.";
            return false;
        }

        Person left = ResolvePerson(request.LeftPersonId);
        Person right = ResolvePerson(request.RightPersonId);
        if (left == null || right == null)
        {
            error = $"Failed to resolve duel persons. Left={request.LeftPersonId}, Right={request.RightPersonId}";
            return false;
        }

        try
        {
            DuelController controller = new DuelController(mainGameScreen, request, left, right);
            mainGameScreen.duelController = controller;
            controller.Start();
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            mainGameScreen.duelController = null;
            mainGameScreen.dantiaoLayer = null;
            mainGameScreen.EnableUpdate = true;
            error = ex.Message;
            return false;
        }
    }

    private static Person ResolvePerson(int personId)
    {
        if (personId < 0)
        {
            return null;
        }

        return Session.Current?.Scenario?.Persons?.GetGameObject(personId) as Person;
    }
}
