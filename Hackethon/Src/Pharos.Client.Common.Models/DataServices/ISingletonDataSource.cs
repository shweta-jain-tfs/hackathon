namespace Pharos.Client.Common.Models.DataServices;

/// <summary>
///     Defines a data source for an entity that has only one "instance" (singleton entity).
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <remarks>
///     This can be, for example, the service that provides information about the current instrument.
/// </remarks>
public interface ISingletonDataSource<TModel>
{
    Task<TModel> GetAsync();
}