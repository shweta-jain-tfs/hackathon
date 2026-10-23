namespace Pharos.Client.Common.Models.DataServices;

/// <summary>
/// Represents a data service that is used by a details page.
/// This is a simpler version of the IEntityDataService/IEntityDataSource without the constraints of the IEntity interface.
/// </summary>
/// <typeparam name="TModel">The model that is sent to/from the backend</typeparam>
/// <typeparam name="TKey">The key/id of the model</typeparam>
public interface IDetailsPageDataService<TModel, TKey>
{
    Task<TModel> GetByIdAsync(TKey id);
    Task<TKey> CreateAsync(TModel model);
    Task UpdateAsync(TModel model);
}

public interface IDetailsPageDataService<TModel> : IDetailsPageDataService<TModel, int>;