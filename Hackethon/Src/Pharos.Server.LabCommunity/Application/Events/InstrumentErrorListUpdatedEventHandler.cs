using System.Globalization;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Domain.Enums;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Models;
using Pharos.Server.LabCommunity.Domain;
using Tetris.Protocol.Pharos.Errors;

namespace Pharos.Server.LabCommunity.Application.Events;

internal sealed class InstrumentErrorListUpdatedEventHandler : INotificationHandler<InstrumentErrorListUpdatedEvent>
{
    private const int _instrumentErrorPayloadVersion = 1;
    private const string _placeholderErrorText = "TODO: Instrument error text.";

    private readonly ILabCommunityExportQueue _exportQueue;
    private readonly ILabCommunityDbContext _dbContext;
    private readonly IOptionsSnapshot<LabCommunitySettings> _settingsSnapshot;
    private readonly IInstrumentManager _instrumentManager;

    public InstrumentErrorListUpdatedEventHandler(
        ILabCommunityExportQueue exportQueue,
        ILabCommunityDbContext dbContext,
        IOptionsSnapshot<LabCommunitySettings> settingsSnapshot,
        IInstrumentManager instrumentManager)
    {
        _exportQueue = exportQueue;
        _dbContext = dbContext;
        _settingsSnapshot = settingsSnapshot;
        _instrumentManager = instrumentManager;
    }

    public async Task Handle(InstrumentErrorListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var settings = _settingsSnapshot.Value;
        if (!settings.Enable)
        {
            return;
        }

        var configuration = await _dbContext.GetConfigurationAsync(settings.LaboratoryCode, cancellationToken);
        if (configuration?.ReportInstrumentErrorsEnabled != true)
        {
            return;
        }

        var instrumentInfo = _instrumentManager.FindInstrument(notification.InstrumentNumber);
        if (instrumentInfo is null)
        {
            return;
        }

        foreach (var error in notification.Message.Errors)
        {
            var payload = new InstrumentErrorExportPayload
            {
                InstrumentType = instrumentInfo.InstrumentType,
                SerialNumber = notification.InstrumentNumber,
                ErrorCode = error.Code.ToString(CultureInfo.InvariantCulture),
                ErrorText = _placeholderErrorText,
                ErrorTime = error.Timestamp.ToDateTimeOffset(),
                Classification = MapClassification(error.Classification)
            };

            await _exportQueue.EnqueueAsync(
                ExportItemType.InstrumentError,
                $"InstrumentError:{notification.InstrumentNumber}:{error.ErrorId}",
                JsonSerializer.Serialize(payload),
                _instrumentErrorPayloadVersion,
                cancellationToken);
        }
    }

    private static ErrorClassification MapClassification(EnumErrorClassification classification)
    {
        return classification switch
        {
            EnumErrorClassification.Fatal => ErrorClassification.Fatal,
            EnumErrorClassification.Error => ErrorClassification.Error,
            EnumErrorClassification.Warning => ErrorClassification.Warning,
            EnumErrorClassification.Information => ErrorClassification.Information,
            _ => ErrorClassification.Error
        };
    }
}