namespace Pharos.Client.Common.Models.DataServices;

public interface IEntityDataService<TModel, TKey> : IEntityDataSource<TModel, TKey>
    where TModel : IEntityModel<TKey>
{
    Task<TKey> CreateAsync(TModel value);
    Task UpdateAsync(TModel value);
    Task DeleteAsync(TKey entityId);
    Task<DeleteResponse> DeleteAsync(TKey[] entityIds);
}

public interface IEntityDataService<TModel> : IEntityDataService<TModel, int>, IEntityDataSource<TModel>
    where TModel : IEntityModel<int>
{
}
