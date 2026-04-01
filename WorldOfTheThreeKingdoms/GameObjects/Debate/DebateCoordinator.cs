using System;

namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public static class DebateCoordinator
{
    private static DebateRequest pendingDemoRequest;

    public static void QueueDemoRequest(in DebateParticipantSeed left, in DebateParticipantSeed right, int? seed = null)
    {
        pendingDemoRequest = DebateRequest.CreateDemo(left, right, seed);
    }

    public static void ClearPendingDemoRequest()
    {
        pendingDemoRequest = null;
    }

    public static bool TryTakePendingDemoRequest(out DebateRequest request)
    {
        request = pendingDemoRequest;
        pendingDemoRequest = null;
        return request != null;
    }

    public static bool TryCreateController(DebateRequest request, out DebateController controller, out string error)
    {
        controller = null;

        if (request == null)
        {
            error = "DebateRequest is null.";
            return false;
        }

        if (request.Left.PersonId < 0 || request.Right.PersonId < 0)
        {
            error = $"Invalid debate participants. Left={request.Left.PersonId}, Right={request.Right.PersonId}";
            return false;
        }

        try
        {
            controller = new DebateController(request);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

