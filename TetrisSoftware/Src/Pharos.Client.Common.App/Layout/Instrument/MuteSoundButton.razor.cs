using Pharos.Client.Common.App.State.CurrentInstrument;

namespace Pharos.Client.Common.App.Layout.Instrument;

public partial class MuteSoundButton : IDisposable
{
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;

    protected override void OnInitialized()
    {
        InstrumentModel.OnCurrentInstrumentChange += RefreshView;
        InstrumentModel.OnSignalTowerChange += RefreshView;
    }

    private async Task MuteSound()
    {
        await InstrumentModel.MuteSound();
    }

    private void RefreshView()
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        InstrumentModel.OnCurrentInstrumentChange -= RefreshView;
        InstrumentModel.OnSignalTowerChange -= RefreshView;
        GC.SuppressFinalize(this);
    }
}