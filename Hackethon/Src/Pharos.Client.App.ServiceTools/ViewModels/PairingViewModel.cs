using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.ServiceTools.ViewModels;

public class PairingViewModel : LoadingViewModel, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly PairingNotificationProvider _notifications;
    private readonly IExceptionHandlerService _exceptionHandler;
    private readonly IStringLocalizer<Translations> _loc;

    public PairingViewModel(HttpClient httpClient, PairingNotificationProvider notifications, IExceptionHandlerService exceptionHandler, IStringLocalizer<Translations> loc)
    {
        _httpClient = httpClient;
        _notifications = notifications;
        _notifications.PairingModeStateChanged += OnPairingModeStateChanged;
        _notifications.ApprovalRequested += OnApprovalRequested;
        _notifications.InstrumentPaired += OnInstrumentPaired;
        _exceptionHandler = exceptionHandler;
        _loc = loc;
    }

    public void Dispose()
    {
        _notifications.PairingModeStateChanged -= OnPairingModeStateChanged;
        _notifications.ApprovalRequested -= OnApprovalRequested;
        _notifications.InstrumentPaired -= OnInstrumentPaired;
        GC.SuppressFinalize(this);
    }

    public bool IsPairingEnabled
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsPairingEnabled));
        }
    }

    public string? InstrumentToPair
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(InstrumentToPair));
        }
    }

    public string? PairedInstrument
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(PairedInstrument));
        }
    }

    public PairingDecision PendingDecision
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(PendingDecision));
        }
    }

    public PairingStage Stage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PairedInstrument))
            {
                return PairingStage.Done;
            }

            if (!IsPairingEnabled)
            {
                return PairingStage.Off;
            }

            if (PendingDecision != PairingDecision.None)
            {
                return PairingStage.SubmittingApproval;
            }

            if (!string.IsNullOrWhiteSpace(InstrumentToPair))
            {
                return PairingStage.WaitingForApproval;
            }

            return PairingStage.WaitingForInstrument;
        }
    }

    public string StageDisplayText =>
        Stage switch
        {
            PairingStage.Off => _loc["ServiceToolsPage_PairingModal_Timeout"].Value,
            PairingStage.WaitingForInstrument => _loc["ServiceToolsPage_PairingModal_WaitingForInstrument"].Value,
            PairingStage.WaitingForApproval => _loc["ServiceToolsPage_PairingModal_WaitingApproval"].Value,
            PairingStage.SubmittingApproval => _loc["ServiceToolsPage_PairingModal_WaitingForInstrument"].Value,
            PairingStage.Done => _loc["ServiceToolsPage_PairingModal_Success"].Value,
            _ => throw new InvalidOperationException()
        };

    public async Task EnablePairing()
    {
        var client = new PairingClient(_httpClient);
        try
        {
            await client.EnablePairingAsync(true);
            IsPairingEnabled = true;
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);
        }
    }

    public async Task DisablePairing()
    {
        var client = new PairingClient(_httpClient);
        try
        {
            await client.EnablePairingAsync(false);
            IsPairingEnabled = false;
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);
        }
    }

    public async Task CancelPairing()
    {
        ClearPendingPairing();
        await DisablePairing();
    }

    public async Task HandleDialogCancelledAsync()
    {
        switch (Stage)
        {
            case PairingStage.SubmittingApproval:
            case PairingStage.WaitingForInstrument:
            case PairingStage.WaitingForApproval:
                await CancelPairing();
                break;

            case PairingStage.Done:
                DismissDone();
                break;
        }
    }

    public async Task Reject()
    {
        var instrumentNumber = InstrumentToPair;

        if (string.IsNullOrWhiteSpace(instrumentNumber))
        {
            return;
        }

        PendingDecision = PairingDecision.Reject;
        InstrumentToPair = null;
        var client = new PairingClient(_httpClient);
        try
        {
            await client.HandleApprovalAsync(instrumentNumber, false);
            PendingDecision = PairingDecision.None;
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);
            PendingDecision = PairingDecision.None;

            // A notification for the next instrument may arrive while the request is in flight.
            // Only restore this request when it has not been replaced by a newer notification.
            InstrumentToPair ??= instrumentNumber;
        }
    }

    public async Task Pair()
    {
        var instrumentNumber = InstrumentToPair;

        if (string.IsNullOrWhiteSpace(instrumentNumber))
        {
            return;
        }

        PendingDecision = PairingDecision.Pair;
        var client = new PairingClient(_httpClient);
        try
        {
            await client.HandleApprovalAsync(instrumentNumber, true);

            // Keep PendingDecision = Pair and wait for InstrumentPaired.
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);
            PendingDecision = PairingDecision.None;
        }
    }

    public void DismissDone()
    {
        PairedInstrument = null;
    }

    public async Task<string?> GetCurrentInstrumentSerialAsync()
    {
        try
        {
            var client = new CurrentInstrumentClient(_httpClient);
            var instrument = await client.GetInstrumentAsync();
            return instrument.SerialNumber;
        }
        catch
        {
            return null;
        }
    }

    protected override async Task DoLoadAsync()
    {
        var client = new PairingClient(_httpClient);
        IsPairingEnabled = await client.IsPairingEnabledAsync();
    }

    private void ClearPendingPairing()
    {
        PendingDecision = PairingDecision.None;
        InstrumentToPair = null;
    }


    private async Task OnInstrumentPaired(string instrumentNumber)
    {
        PairedInstrument = instrumentNumber;
        ClearPendingPairing();

        await DisablePairing();
    }

    private Task OnApprovalRequested(string instrumentNumber)
    {
        if (!IsPairingEnabled)
        {
            return Task.CompletedTask;
        }

        InstrumentToPair = instrumentNumber;
        PairedInstrument = null;
        return Task.CompletedTask;
    }

    private Task OnPairingModeStateChanged(bool enabled)
    {
        IsPairingEnabled = enabled;
        InstrumentToPair = null;
        if (enabled)
        {
            PairedInstrument = null;
        }

        return Task.CompletedTask;
    }

}

public enum PairingStage
{
    Off,
    WaitingForInstrument,
    WaitingForApproval,
    SubmittingApproval,
    Done
}

public enum PairingDecision
{
    None,
    Pair,
    Reject
}
