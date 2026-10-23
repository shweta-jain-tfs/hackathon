namespace Pharos.Client.Common.Models.DataServices;

public interface IEntityDataSource<TModel, in TKey> : IPagedDataSource<TModel, TKey>, IGetByIdDataSource<TModel, TKey> where TModel : IEntityModel<TKey>;

public interface IEntityDataSource<TModel> : IEntityDataSource<TModel, int>, IGetByIdDataSource<TModel> where TModel : IEntityModel<int>;

public interface IPagedDataSource<TModel, in TKey> where TModel : IEntityModel<TKey>
{
    /// <summary>
    /// Gets items from the data source.
    /// </summary>
    /// <param name="request"></param>
    /// <returns>A <see cref="ValueTask"/> whose result is a <see cref="DataSourceResult{TModel}"/> upon successful completion.</returns>
    Task<DataSourceResult<TModel>> GetAsync(DataSourceRequest request);
}

public interface IPagedDataSource<TModel> : IPagedDataSource<TModel, int> where TModel : IEntityModel<int>;