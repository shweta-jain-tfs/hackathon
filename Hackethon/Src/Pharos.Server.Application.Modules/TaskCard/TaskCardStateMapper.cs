using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Pharos.Errors;

namespace Pharos.Server.Application.TaskCard;

public static class TaskCardStateMapper
{
    public static TaskCardState FromClassification(ErrorClassification classification)
    {
        return classification switch
        {
            ErrorClassification.Fatal => TaskCardState.FlashingRed,
            ErrorClassification.Error => TaskCardState.Error,
            ErrorClassification.Warning => TaskCardState.Warning,
            _ => TaskCardState.Information,
        };
    }

    public static TaskCardState FromProtoClassifications(IEnumerable<EnumErrorClassification>? classifications)
    {
        var priority = new[] { EnumErrorClassification.Fatal, EnumErrorClassification.Error, EnumErrorClassification.Warning };
        var found = priority.FirstOrDefault(p => classifications?.Contains(p) == true);
        return found switch
        {
            EnumErrorClassification.Fatal => TaskCardState.FlashingRed,
            EnumErrorClassification.Error => TaskCardState.Error,
            EnumErrorClassification.Warning => TaskCardState.Warning,
            _ => TaskCardState.Information
        };
    }
}
