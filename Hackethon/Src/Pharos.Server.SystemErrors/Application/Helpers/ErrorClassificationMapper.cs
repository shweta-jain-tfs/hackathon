using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.SystemErrors.Application.Helpers;

/// <summary>
/// Maps error classifications to task card states, maintaining consistent severity ordering.
/// </summary>
internal static class ErrorClassificationMapper
{
    /// <summary>
    /// Determines the highest task card state from a collection of error classifications.
    /// </summary>
    public static TaskCardState GetHighestTaskCardState(IEnumerable<ErrorClassification> classifications)
    {
        var set = classifications.ToHashSet();
        
        if (set.Contains(ErrorClassification.Fatal))
        {
            return TaskCardState.FlashingRed;
        }

        if (set.Contains(ErrorClassification.Error))
        {
            return TaskCardState.Error;
        }

        if (set.Contains(ErrorClassification.Warning))
        {
            return TaskCardState.Warning;
        }

        return TaskCardState.Information;
    }
}
