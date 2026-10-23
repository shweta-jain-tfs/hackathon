namespace Pharos.Client.Common.App.Validation;
public record ValidationContextState(IReadOnlyDictionary<string, bool> SectionStates)
{
    public bool IsValid => SectionStates.Values.All(x => x);
}