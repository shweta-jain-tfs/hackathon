namespace Pharos.Client.Common.App.Interfaces;

public interface ITaskCardTranslator
{
    string? GetDescription(TaskCardType taskCardType);
    string GetTitle(TaskCardType taskCardType);
}
