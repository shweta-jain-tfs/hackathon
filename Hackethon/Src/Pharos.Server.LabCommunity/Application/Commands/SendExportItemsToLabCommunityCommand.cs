using System.Diagnostics;
using System.Net;
using System.Text.Json;
using LabCommunity.API.Client;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Domain;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Mapping;
using Pharos.Server.LabCommunity.Application.Models;
using Pharos.Server.LabCommunity.Domain;
using ArticleType = Pharos.Server.Domain.Enums.ArticleType;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed record SendExportItemsToLabCommunityCommand(ExportItemType ExportItemType) : IRequest;

internal sealed class SendExportItemsToLabCommunityCommandHandler(
    ILabCommunityDbContext dbContext,
    ILabCommunityExportQueue exportQueue,
    ILabCommunityApiClient client,
    IInstrumentManager instrumentManager,
    IReagentBarcodeService reagentBarcodeService,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot,
    IDateTime dateTime,
    ILogger<SendExportItemsToLabCommunityCommandHandler> logger) : IRequestHandler<SendExportItemsToLabCommunityCommand>
{
    private static readonly TimeSpan _retryDelay = TimeSpan.FromMinutes(5);

    public async Task Handle(SendExportItemsToLabCommunityCommand request, CancellationToken cancellationToken)
    {
        var settings = settingsSnapshot.Value;
        var existingEnrollment = await dbContext.GetActiveEnrollmentAsync(settings.LaboratoryCode, cancellationToken);
        if (existingEnrollment is null)
        {
            logger.LogWarning(
                "Cannot send LabCommunity export items. No active enrollment found.");
            return;
        }

        var now = dateTime.UtcNow;
        var items = await exportQueue.PeekBatchAsync(request.ExportItemType, cancellationToken);
        if (items.Count > 0)
        {
            logger.LogDebug("Found {ExportItemCount} LabCommunity export items ready to send.", items.Count);
        }

        List<ExportItem> itemsToDequeue = [];
        foreach (var item in items)
        {
            item.AttemptCount++;
            item.LastAttemptAtUtc = now;
            var stopwatch = Stopwatch.StartNew();

            logger.LogInformation(
                "Processing LabCommunity export item {ExportItemId} ({ExportType}), attempt {AttemptNumber}.",
                item.Id,
                item.ExportItemType,
                item.AttemptCount);

            try
            {
                switch (item.ExportItemType)
                {
                    case ExportItemType.InstrumentError:
                        await SendInstrumentErrorAsync(item, cancellationToken);
                        break;
                    case ExportItemType.CurveControlResult:
                        await SendCurveControlResultAsync(item, cancellationToken);
                        break;
                    case ExportItemType.QualityControlResult:
                        await SendQualityControlResultAsync(item, cancellationToken);
                        break;
                    case ExportItemType.BlankResult:
                        await SendBlankResultAsync(item, cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(request), item.ExportItemType, null);
                }

                itemsToDequeue.Add(item);

                logger.LogInformation(
                    "LabCommunity export item {ExportItemId} ({ExportType}) completed in {ElapsedMilliseconds} ms.",
                    item.Id,
                    item.ExportItemType,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (ApiException ex) when (ex.StatusCode == (int)HttpStatusCode.UnprocessableEntity)
            {
                itemsToDequeue.Add(item);

                logger.LogWarning(
                    "LabCommunity rejected export item {ExportItemId} ({ExportType}); item will not be retried.",
                    item.Id,
                    item.ExportItemType);
            }
            catch (ApiException ex)
            {
                item.Status = ExportItemStatus.Failed;
                item.NextAttemptAtUtc = now.Add(_retryDelay);
                item.LastError = LimitLastError(ex.Message);

                logger.LogWarning(
                    "LabCommunity export item {ExportItemId} ({ExportType}) failed on attempt {AttemptNumber} with status {StatusCode}: {Error}. Item will be retried at {NextAttemptAtUtc}.",
                    item.Id,
                    item.ExportItemType,
                    item.AttemptCount,
                    ex.StatusCode,
                    ex.Response,
                    item.NextAttemptAtUtc);
            }
            catch (Exception ex)
            {
                item.Status = ExportItemStatus.Failed;
                item.NextAttemptAtUtc = now.Add(_retryDelay);
                item.LastError = LimitLastError(ex.Message);

                logger.LogWarning(
                    ex,
                    "LabCommunity export item {ExportItemId} ({ExportType}) failed on attempt {AttemptNumber}. Item will be retried at {NextAttemptAtUtc}.",
                    item.Id,
                    item.ExportItemType,
                    item.AttemptCount,
                    item.NextAttemptAtUtc);
            }
        }

        if (items.Count > 0)
        {
            await exportQueue.DequeueBatchAsync(itemsToDequeue, cancellationToken);
        }
    }

    private static string LimitLastError(string error)
    {
        return error[..Math.Min(error.Length, Constants.ExportItemLastErrorLength)];
    }

    private async Task SendInstrumentErrorAsync(ExportItem item, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<InstrumentErrorExportPayload>(item.PayloadJson)
                      ?? throw new InvalidOperationException("Instrument error export payload is empty.");

        logger.LogInformation(
            "Sending LabCommunity export item {ExportItemId} as ReportInstrumentError. Instrument serial number = {SerialNumber}, instrument model = {InstrumentModel}, error code = {ErrorCode}, classification = {Classification}.",
            item.Id,
            payload.SerialNumber,
            payload.InstrumentType,
            payload.ErrorCode,
            payload.Classification);

        await client.ReportInstrumentErrorAsync(
            new ReportInstrumentErrorRequest
            {
                InstrumentType = payload.InstrumentType.ToLabCommunityInstrumentType(),
                SerialNumber = payload.SerialNumber,
                ErrorCode = payload.ErrorCode,
                ErrorText = payload.ErrorText,
                ErrorTime = payload.ErrorTime,
                Classification = payload.Classification.ToLabCommunityErrorClassification()
            },
            cancellationToken);
    }

    private async Task SendCurveControlResultAsync(ExportItem item, CancellationToken cancellationToken)
    {
        var result = JsonSerializer.Deserialize<SendCurveControlResultToLabCommunityCommand>(item.PayloadJson)
                     ?? throw new InvalidOperationException("Curve-control result export payload is empty.");
        var instrument = instrumentManager.GetInstrument(result.InstrumentNumber);

        logger.LogInformation(
            "Sending LabCommunity export item {ExportItemId} as ReportCurveControlResult. Instrument serial number = {SerialNumber}, instrument model = {InstrumentModel}, method = {MethodName}, test = {TestName}, curve control = {CurveControlName}.",
            item.Id,
            result.InstrumentNumber,
            instrument.InstrumentType,
            result.MethodName,
            result.TestName,
            result.CurveControlName);

        await client.ReportCurveControlResultAsync(
            new ReportCurveControlResultRequest
            {
                InstrumentType = instrument.InstrumentType.ToLabCommunityInstrumentType(),
                SerialNumber = result.InstrumentNumber,
                MeasurementTime = result.MeasurementTime,
                CurveControlName = result.CurveControlName,
                MethodName = result.MethodName,
                TestName = result.TestName,
                Judgment = result.Judgment.ToLabCommunityCurveControlJudgment(),
                ConjugateLotNumber = result.ConjugateLotNumber,
                SolidPhaseLotNumber = result.SolidPhaseLotNumber,
                CurveControlLotNumber = result.CurveControlLotNumber,
                Replicates = result.Replicates.Select(replicate => new CurveControlReplicateRequest
                {
                    ReplicateNumber = replicate.ReplicateNumber,
                    MeasurementTime = replicate.MeasurementTime,
                    Concentration = replicate.Concentration,
                    DefinedConcentration = result.ExpectedConcentration,
                    Response = replicate.Response,
                    Judgment = replicate.Judgment.ToLabCommunityCurveControlJudgment(),
                    ConjugateLotNumber = replicate.ConjugateLotNumber,
                    SolidPhaseLotNumber = replicate.SolidPhaseLotNumber,
                    CurveControlLotNumber = replicate.CurveControlLotNumber
                }).ToList()
            },
            cancellationToken);
    }

    private async Task SendBlankResultAsync(ExportItem item, CancellationToken cancellationToken)
    {
        var result = JsonSerializer.Deserialize<SendBlankResultToLabCommunityCommand>(item.PayloadJson)
                     ?? throw new InvalidOperationException("Blank result export payload is empty.");
        var instrument = instrumentManager.GetInstrument(result.InstrumentNumber);
        var developmentSolutionLotNumber = result.BlankType == BlankResultType.RinseBlank ? null
            : reagentBarcodeService.GetReagentLotInformation(ArticleType.DevelopmentSolutionCartridge, primaryBarcode: result.DevelopmentSolutionBarcode)?.LotNumber;
        var stopSolutionLotNumber = result.BlankType == BlankResultType.RinseBlank ? null
            : reagentBarcodeService.GetReagentLotInformation(ArticleType.StopSolutionBottle, secondaryBarcode: result.StopSolutionBarcode)?.LotNumber;

        logger.LogInformation(
            "Sending LabCommunity export item {ExportItemId} as ReportBlankResult. Instrument serial number = {SerialNumber}, instrument model = {InstrumentModel}, blank type = {BlankType}, assay technology = {AssayTechnology}.",
            item.Id,
            result.InstrumentNumber,
            instrument.InstrumentType,
            result.BlankType,
            result.BlankType.ToLabCommunityAssayTechnology());

        await client.ReportBlankResultAsync(
            new ReportBlankResultRequest
            {
                InstrumentType = instrument.InstrumentType.ToLabCommunityInstrumentType(),
                SerialNumber = result.InstrumentNumber,
                MeasurementTime = result.MeasurementTime,
                BlankType = result.BlankType.ToLabCommunityBlankType(),
                AssayTechnology = result.BlankType.ToLabCommunityAssayTechnology(),
                Judgment = result.ResultOk ? BlankJudgment.Ok : BlankJudgment.OutOfLimits,
                DevelopmentSolutionLotNumber = developmentSolutionLotNumber,
                StopSolutionLotNumber = stopSolutionLotNumber,
                Replicates = result.Replicates.Select(replicate => new BlankReplicateRequest
                {
                    ReplicateNumber = replicate.ReplicateNumber,
                    MeasurementTime = replicate.MeasurementTime,
                    Voltage = replicate.Voltage,
                    Judgment = result.ResultOk ? BlankJudgment.Ok : BlankJudgment.OutOfLimits,
                    DevelopmentSolutionLotNumber = developmentSolutionLotNumber,
                    StopSolutionLotNumber = stopSolutionLotNumber
                }).ToList()
            },
            cancellationToken);
    }

    private async Task SendQualityControlResultAsync(ExportItem item, CancellationToken cancellationToken)
    {
        var result = JsonSerializer.Deserialize<SendQualityControlResultToLabCommunityCommand>(item.PayloadJson)
                     ?? throw new InvalidOperationException("Quality-control result export payload is empty.");
        var instrument = instrumentManager.GetInstrument(result.InstrumentNumber);

        logger.LogInformation(
            "Sending LabCommunity export item {ExportItemId} as ReportQualityControlResult. Instrument serial number = {SerialNumber}, instrument model = {InstrumentModel}, method = {MethodName}, test = {TestName}, quality control = {QualityControlName}.",
            item.Id,
            result.InstrumentNumber,
            instrument.InstrumentType,
            result.MethodName,
            result.TestName,
            result.QualityControlName);

        await client.ReportQualityControlResultAsync(
            new ReportQualityControlResultRequest
            {
                InstrumentType = instrument.InstrumentType.ToLabCommunityInstrumentType(),
                SerialNumber = result.InstrumentNumber,
                MeasurementTime = result.MeasurementTime,
                QualityControlName = result.QualityControlName,
                MethodName = result.MethodName,
                TestName = result.TestName,
                Judgment = result.Judgment.ToLabCommunityQualityControlJudgment(),
                ConjugateLotNumber = result.ConjugateLotNumber,
                SolidPhaseLotNumber = result.SolidPhaseLotNumber,
                QualityControlLotNumber = result.QualityControlLotNumber,
                RangeMin = result.RangeMin,
                RangeMax = result.RangeMax,
                PercentCvLimit = result.PercentCvLimit,
                Replicates = result.Replicates.Select(replicate => new QualityControlReplicateRequest
                {
                    ReplicateNumber = replicate.ReplicateNumber,
                    MeasurementTime = replicate.MeasurementTime,
                    Concentration = replicate.Concentration,
                    Response = replicate.Response,
                    Judgment = replicate.Judgment.ToLabCommunityQualityControlJudgment(),
                    ConjugateLotNumber = replicate.ConjugateLotNumber,
                    SolidPhaseLotNumber = replicate.SolidPhaseLotNumber,
                    QualityControlLotNumber = replicate.QualityControlLotNumber
                }).ToList()
            },
            cancellationToken);
    }
}