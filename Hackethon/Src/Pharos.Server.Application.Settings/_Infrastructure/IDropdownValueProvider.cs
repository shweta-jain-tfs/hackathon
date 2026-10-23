namespace Pharos.Server.Application.Settings;

public interface IDropdownValueProvider
{
    Task<List<DropdownOption>> GetOptionsAsync(CancellationToken cancellationToken = default);
}

public record DropdownOption
{
    public required string Value { get; init; }
    public required string DisplayName { get; init; }
}
