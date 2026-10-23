namespace Pharos.Client.Common.Models.DataServices;

public record DataSourceIdsResult
{
    public ICollection<int> SelectedIds { get; }

    public DataSourceIdsResult(ICollection<int> selectedIds)
    {
        SelectedIds = selectedIds;
    }
}