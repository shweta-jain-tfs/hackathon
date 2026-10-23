namespace Pharos.Client.Common.Models.DataServices;

public interface IGetByIdDataSource<TModel, in TKey> where TModel : IEntityModel<TKey>
{
    Task<TModel> GetByIdAsync(TKey entityId);
}

public interface IGetByIdDataSource<TModel> : IGetByIdDataSource<TModel, int> where TModel : IEntityModel<int>;