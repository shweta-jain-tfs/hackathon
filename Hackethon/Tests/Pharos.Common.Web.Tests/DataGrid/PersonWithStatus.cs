namespace Pharos.Common.Web.Tests.DataGrid;

public enum WorkflowStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public sealed record PersonWithStatus(int Id, string Name, WorkflowStatus Status);
