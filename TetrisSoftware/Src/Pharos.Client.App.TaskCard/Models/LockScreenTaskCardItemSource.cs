namespace Pharos.Client.App.TaskCard.Models;

public interface ILockScreenTaskCardItemSource : IItemsSource<Common.Models.TaskCard>
{
}

public class LockScreenTaskCardItemSource : ILockScreenTaskCardItemSource
{
    private readonly HttpClient _httpClient;

    public LockScreenTaskCardItemSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Common.Models.TaskCard>> GetAsync()
    {
        var client = new TaskCardClient(_httpClient);
        var items = await client.GetTaskCardsForLockScreenAsync();
        return items.ToList();
    }
}