namespace Pharos.Client.App.Parameters.Methods.Models;

public interface IMethodTestPagedDataSource : IPagedDataSource<MethodTest, int>
{
    Task<DataSourceResult<MethodTest>> GetAsync(DataSourceRequest request, int methodId);
}
