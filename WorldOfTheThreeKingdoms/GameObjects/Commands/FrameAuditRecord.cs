using Microsoft.Xna.Framework;

namespace GameObjects.Commands;

public enum FrameAuditResultCode : byte
{
    Unknown = 0,
    Success = 1,
    RejectedByCellConflict = 2,
    RejectedByEdgeConflict = 3,
    AbortedByQueue = 4,
    ExecutorFailureForwarded = 5,
    MismatchWithoutAuthority = 6,
    DestroyedBeforeCompletion = 7,
    NonSpatialCompleted = 8
}

public readonly record struct FrameAuditRecord(
    int TroopId,
    ExecutionActionKind ActionKind,
    int IssuedTick,
    int CommitTick,
    Point SourcePosition,
    Point ConflictPosition,
    Point ProjectedDestination,
    Point FinalPosition,
    FrameAuditResultCode ResultCode,
    int FailureReasonCode,
    int RelatedObjectId
);
