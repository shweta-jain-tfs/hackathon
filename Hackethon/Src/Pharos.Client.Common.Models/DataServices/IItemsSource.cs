namespace Pharos.Client.Common.Models.DataServices;

public interface IItemsSource<TModel>
{
    Task<List<TModel>> GetAsync();
}