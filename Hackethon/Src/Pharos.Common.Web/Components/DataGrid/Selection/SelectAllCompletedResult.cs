namespace Pharos.Common.Web.Components.DataGrid.Selection;

internal class SelectAllCompletedResult
{
    public bool IsCancelled { get; }
    public Exception? Error { get; }
    public bool HasError => Error is not null;
    public bool IsSuccess => !IsCancelled && Error is null;

    public SelectAllCompletedResult(bool isCancelled = false, Exception? error = null)
    {
        IsCancelled = isCancelled;
        Error = error;
    }
}