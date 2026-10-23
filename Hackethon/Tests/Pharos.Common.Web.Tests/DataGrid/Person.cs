namespace Pharos.Common.Web.Tests.DataGrid;

public sealed record Person(int Id, string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}";
}
