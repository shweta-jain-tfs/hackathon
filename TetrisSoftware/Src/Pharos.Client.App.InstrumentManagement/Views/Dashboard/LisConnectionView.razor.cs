using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Health;
using Pharos.Common.Web.Components.Modal;
using Pharos.Client.Common.App.Extensions;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class LisConnectionView : IDisposable
{
    [Inject] private LisConnectionViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        await ViewModel.LoadAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    private static HealthStatus GetHealthStatus(LisConnectionState? state)
    {
        if (state == null)
        {
            return HealthStatus.Warning;
        }

        return state switch
        {
            LisConnectionState.Open => HealthStatus.Ok,
            LisConnectionState.Broken => HealthStatus.Alarm,
            LisConnectionState.Closed => HealthStatus.Alarm,
            _ => HealthStatus.Warning
        };
    }

    private string? GetStateText(LisConnectionState? state)
    {
        if (state == null)
        {
            return null;
        }

        return state switch
        {
            LisConnectionState.Open => Loc["ConnectionsPage_Connected"],
            LisConnectionState.Connecting => Loc["ConnectionsPage_Connecting"],
            _ => Loc["ConnectionsPage_Disconnected"]
        };
    }

    private IQueryable<LisStatistics> GetStatistics(int channelId)
    {
        var statistics = ViewModel.Connections.FirstOrDefault(s => s.Id == channelId);

        if (statistics is null)
        {
            return Array.Empty<LisStatistics>().AsQueryable();
        }

        // This is to maintain UI Order in : Queries → Imported → Exported to match the expected UI layout
        return new[]
        {
            new LisStatistics(Loc["ConnectionsPage_Queries"], statistics.Queries.Samples, statistics.Queries.Tests, statistics.Queries.LatestTime?.ToSortableDateTimeString() ?? string.Empty, statistics.Queries.LatestSample),
            new LisStatistics(Loc["ConnectionsPage_Imported"], statistics.Imported.Samples, statistics.Imported.Tests ?? 0, statistics.Imported.LatestTime?.ToSortableDateTimeString() ?? string.Empty, statistics.Imported.LatestSample),
            new LisStatistics(Loc["ConnectionsPage_Exported"], statistics.Exported.Samples, statistics.Exported.Tests ?? 0, statistics.Exported.LatestTime?.ToSortableDateTimeString() ?? string.Empty, statistics.Exported.LatestSample)
        }.AsQueryable();
    }

    private async Task ConfirmResetStatisticsAsync(int channelId)
    {
        var options = new ModalOptions
        {
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Default
        };
        var modal = ModalService.ShowConfirm(
            Loc["ConnectionsPage_ResetConfirmMessage"],
            Loc["ConnectionsPage_ResetCancelButton"],
            Loc["ConnectionsPage_ResetOkButton"],
            options,
            true);
        var result = await modal.Result;

        if (!result.Canceled)
        {
            await ViewModel.ResetStatisticsAsync(channelId);
        }
    }

    private static bool ShowStatusIcon(LisConnectionState? status)
    {
        var isDisconnected = status == null ||
                status == LisConnectionState.Closed ||
                status == LisConnectionState.Broken;
        return isDisconnected;
    }

    private sealed record LisStatistics(string RowTitle, int Samples, int? Tests, string? LatestTime, string? LatestSample);
}