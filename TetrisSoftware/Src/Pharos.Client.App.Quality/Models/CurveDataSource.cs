using System.ComponentModel;

namespace Pharos.Client.App.Quality.Models;

public interface ICurvesDataSource : IEntityDataSource<CurvePresentation>
{
    public string InstrumentNumber { get; set; }
    public string MethodName { get; set; }
    public ICollection<CurvePresentation>? Data { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    Task<ICollection<CurvePresentation>> GetCurrentCurves(string instrumentNumber, string methodName);
} 

internal sealed class CurvesDataSource(HttpClient httpClient) : ICurvesDataSource
{
    public string InstrumentNumber { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public ICollection<CurvePresentation>? Data { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task<ICollection<CurvePresentation>> GetCurrentCurves(string instrumentNumber, string methodName)
    {
        var client = new CurvesClient(httpClient);
        return await client.GetCurrentCurvesAsync(instrumentNumber, methodName);
    }

    public async Task<DataSourceResult<CurvePresentation>> GetAsync(DataSourceRequest request)
    {
        if (!string.IsNullOrEmpty(InstrumentNumber) && !string.IsNullOrEmpty(MethodName))
        {
            var client = new CurvesClient(httpClient);
            var curves = await client.GetCurvesAsync(InstrumentNumber, MethodName);
            Data = curves;
            OnPropertyChanged("GotCurves");

            return new DataSourceResult<CurvePresentation>(curves.ToList(), curves.Count);
        }
        else
        {
            return new DataSourceResult<CurvePresentation>([], 0);
        }
    }

    private void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Task<CurvePresentation> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

}
