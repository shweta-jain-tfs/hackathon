using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services.ResultExportService;
internal class CsvMapper
{
    /// <summary>
    /// Map a <see cref="ResultExportMeanData"/> item to a <see cref="CsvMean"/> item.
    /// </summary>
    /// <param name="data">Database result item.</param>
    /// <param name="index">Zero based index where the item appears in the original result.</param>
    /// <returns>A mapped item to use for CSV export.</returns>
    public static CsvMean MapMeanDataToCsv(ResultExportMeanData data, int index)
    {
        var identifier = data.ResultType switch
        {
            TestResultType.Sample => data.SampleId,
            TestResultType.Calibrator => data.CalibratorName,
            TestResultType.CurveControl => data.CurveControlName,
            TestResultType.QualityControl => data.QualityControlName,
            _ => "",
        };

        return new CsvMean
        {
            Sequence = index + 1,
            SampleType = (int)data.ResultType,
            GroupId = data.GroupId?.ToString() ?? "",
            SampleId = identifier,
            MethodName = data.MethodName,
            TestName = data.TestName,
            MeanResponse = data.MeanResponse ?? 0,
            MeanConcentration = data.MeanConcentration ?? 0,
            PercentCvResponse = data.PercentCvResponse ?? 0,
            PercentCvConcentration = data.PercentCvConcentration ?? 0,
        };
    }

    /// <summary>
    /// Map a <see cref="ResultExportResultData"/> item to a <see cref="CsvResult"/> item.
    /// </summary>
    /// <param name="data">Database result item.</param>
    /// <param name="index">Zero based index where the item appears in the original result.</param>
    /// <returns>A mapped item to use for CSV export.</returns>
    public static CsvResult MapResultDataToCsv(ResultExportResultData data, int index)
    {
        var csvResult = new CsvResult
        {
            Sequence = index + 1,
            ReplicateId = data.InstrumentReplicateId.ToString(),
            MeasurementDateTime = data.MeasurementDateTime,
            Status = (int)(data.IsAborted ? ResultStatus.Aborted : ResultStatus.Measured),
            SampleId = data.SampleId,
            RequestId = data.RequestId,
            MethodName = data.MethodName,
            TestName = data.TestName,
            SampleType = (int)data.OrderType,
            Ru = data.RU ?? 0,
            LowRu = data.LowRU ? 1 : 0,
            Concentration = data.Concentration ?? 0,
            Quotient = data.Quotient,
            SampleClass = data.Class,
            CutOff = data.CutOff,
            Errors = data.ErrorCodes.Length > 0 ? 1 : 0,
            PreDilution = data.PreDilutionFactor,
            InstrumentDilution = data.InstrumentDilutionFactor,
            AnalyticalRunId = data.InstrumentAnalyticalRunId.ToString(),
            Comment = data.Comment,
            FluorometerMeasurementValue = data.FluorometerValue ?? 0,
            Blank = data.BlankId ?? 0,
            CustomRunId = data.CmrRunId,
            CmrTestId = data.CmrTestId,
            CurveId = data.CurveId ?? 0,
            CalculationCurveId = data.CalculationCurveId ?? 0,
            GroupId = data.GroupId?.ToString() ?? "",
            OriginalResponse = data.CalibratorOriginalResponse,
            EditResponse = data.EditedResponse,
            ResponseEdited = data.EditedResponse.HasValue ? 1 : 0,
            Judgement = data.Judgement,
            ErrorCodes = string.Join('\\', data.ErrorCodes),
            IrwRow = data.IrwRow,
            ErwRow = data.ErwRow,
            IrwSpoke = data.IrwSpoke,
            ErwSpoke = data.ErwSpoke
        };

        // Get reagent info
        var carrierInfo = data.Reagents
            .FirstOrDefault(c => c.ReagentType == ArticleType.ImmunoCapCarrier
                            || c.ReagentType == ArticleType.EliaWellCarrier
                            || c.ReagentType == ArticleType.EliaCalibratorWellCarrier);
        var calCcQcCartridgeInfo = data.Reagents
            .FirstOrDefault(c => c.ReagentType == ArticleType.CurveControlCartridge
                            || c.ReagentType == ArticleType.CalibratorCartridge
                            || c.ReagentType == ArticleType.QualityControlCartridge);
        var diluentInfo = data.Reagents.FirstOrDefault(c => c.ReagentType == ArticleType.DiluentCartridge);
        var conjugateInfo = data.Reagents.FirstOrDefault(c => c.ReagentType == ArticleType.ConjugateCartridge);
        var developmentInfo = data.Reagents.FirstOrDefault(c => c.ReagentType == ArticleType.DevelopmentSolutionCartridge);
        // Reagents list is sorted by serial number in the query
        var stopSolutions = data.Reagents.Where(c => c.ReagentType == ArticleType.StopSolutionBottle).Take(2).ToList();
        var stopInfo = stopSolutions.FirstOrDefault();
        var stop2Info = stopSolutions.Skip(1).FirstOrDefault();
        var washConcentrates = data.Reagents.Where(c => c.ReagentType == ArticleType.WashConcentrateBottle).Take(2).ToList();
        var washConcentrateInfo = washConcentrates.FirstOrDefault();
        var washConcentrate2Info = washConcentrates.Skip(1).FirstOrDefault();
        var washAdditives = data.Reagents.Where(c => c.ReagentType == ArticleType.WashAdditiveBottle).Take(2).ToList();
        var washAdditiveInfo = washAdditives.FirstOrDefault();
        var washAdditive2Info = washAdditives.Skip(1).FirstOrDefault();

        // Fill reagent related fields
        if (carrierInfo != null)
        {
            csvResult.CarrierLotNumber = carrierInfo.LotNumber;
            csvResult.CarrierSerialNumber = carrierInfo.SerialNumber;
            csvResult.CarrierExpired = carrierInfo.Expired ? 1 : 0;
        }

        if (calCcQcCartridgeInfo != null)
        {
            csvResult.CalCcQcLotNumber = calCcQcCartridgeInfo.LotNumber;
            csvResult.CalCcQcSerialNumber = calCcQcCartridgeInfo.SerialNumber;
            csvResult.CalCcQcExpired = calCcQcCartridgeInfo.Expired ? 1 : 0;
        }

        if (diluentInfo != null)
        {
            csvResult.DiluentLotNumber = diluentInfo.LotNumber;
            csvResult.DiluentSerialNumber = diluentInfo.SerialNumber;
            csvResult.DiluentExpired = diluentInfo.Expired ? 1 : 0;
        }

        if (conjugateInfo != null)
        {
            csvResult.ConjugateLotNumber = conjugateInfo.LotNumber;
            csvResult.ConjugateSerialNumber = conjugateInfo.SerialNumber;
            csvResult.ConjugateExpired = conjugateInfo.Expired ? 1 : 0;
        }

        if (developmentInfo != null)
        {
            csvResult.DevelopmentLotNumber = developmentInfo.LotNumber;
            csvResult.DevelopmentSerialNumber = developmentInfo.SerialNumber;
            csvResult.DevelopmentExpired = developmentInfo.Expired ? 1 : 0;
        }

        if (stopInfo != null)
        {
            csvResult.StopLotNumber = stopInfo.LotNumber;
            csvResult.StopSerialNumber = stopInfo.SerialNumber;
            csvResult.StopExpired = stopInfo.Expired ? 1 : 0;
        }

        if (stop2Info != null)
        {
            csvResult.Stop2LotNumber = stop2Info.LotNumber;
            csvResult.Stop2SerialNumber = stop2Info.SerialNumber;
            csvResult.Stop2Expired = stop2Info.Expired ? 1 : 0;
        }

        if (washConcentrateInfo != null)
        {
            csvResult.WashConcentrateLotNumber = washConcentrateInfo.LotNumber;
            csvResult.WashConcentrateSerialNumber = washConcentrateInfo.SerialNumber;
            csvResult.WashConcentrateExpired = washConcentrateInfo.Expired ? 1 : 0;
        }

        if (washAdditiveInfo != null)
        {
            csvResult.WashAdditiveLotNumber = washAdditiveInfo.LotNumber;
            csvResult.WashAdditiveSerialNumber = washAdditiveInfo.SerialNumber;
            csvResult.WashAdditiveExpired = washAdditiveInfo.Expired ? 1 : 0;
        }

        if (washConcentrate2Info != null)
        {
            csvResult.WashConcentrate2LotNumber = washConcentrate2Info.LotNumber;
            csvResult.WashConcentrate2SerialNumber = washConcentrate2Info.SerialNumber;
            csvResult.WashConcentrate2Expired = washConcentrate2Info.Expired ? 1 : 0;
        }

        if (washAdditive2Info != null)
        {
            csvResult.WashAdditive2LotNumber = washAdditive2Info.LotNumber;
            csvResult.WashAdditive2SerialNumber = washAdditive2Info.SerialNumber;
            csvResult.WashAdditive2Expired = washAdditive2Info.Expired ? 1 : 0;
        }

        return csvResult;
    }

}
