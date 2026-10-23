namespace Pharos.Client.Common.App.State.CurrentInstrument.Models;

public interface ICurrentInstrumentDataService
{
    Task<TaskCardInformation> GetTaskCardsInformation();
    Task<Instrument> GetInstrument();
    Task<ProcessStatusSummary> GetProcessStatusSummary();
    Task MuteSound();
    Task<List<int>> GetFatalErrors();
    Task UnlockDrawerAuthorizationResponse();
    Task<ScannedBarcode> GetBarcodeDetails(string barcode);

    /// <summary>
    /// Skips only the next scheduled automatic assay start for the current instrument.
    /// </summary>
    Task SkipNextScheduledAssayStart();
}

internal class CurrentInstrumentDataService : ICurrentInstrumentDataService
{
    private readonly HttpClient _httpClient;

    public CurrentInstrumentDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TaskCardInformation> GetTaskCardsInformation()
    {
        var client = new TaskCardClient(_httpClient);
        var taskCardInfo = await client.GetTaskCardsInformationAsync();
        return taskCardInfo;
    }

    public async Task<Instrument> GetInstrument()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        return await client.GetInstrumentAsync();
    }

    public async Task<ProcessStatusSummary> GetProcessStatusSummary()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        var result = await client.GetProcessStatusSummaryAsync();
        return result;
    }

    public async Task MuteSound()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        await client.MuteSignalTowerAsync();
    }

    public async Task<List<int>> GetFatalErrors()
    {
        var client = new SystemErrorsClient(_httpClient);
        var result = await client.GetFatalSystemErrorsAsync();
        return result.ToList();
    }

    public async Task UnlockDrawerAuthorizationResponse()
    {
        var client = new DrawerAuthorizationClient(_httpClient);
        await client.UnlockDrawerAuthorizationResponseAsync();
    }

    public async Task SkipNextScheduledAssayStart()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        await client.SkipNextScheduledAssayStartAsync();
    }

    public async Task<ScannedBarcode> GetBarcodeDetails(string barcode)
    {
        var client = new CurrentInstrumentClient(_httpClient);
        return await client.GetBarcodeDetailsAsync(barcode);
    }
}