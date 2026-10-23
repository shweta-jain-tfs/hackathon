using System.Globalization;
using Pharos.Server.Result.Domain;


// ReSharper disable StringLiteralTypo

namespace Pharos.Server.Result.Application.Services.ResultExportService;

/// <summary>
/// The result/data section of the ResultExport file.
/// Represents a test order replicate result.
/// </summary>
public record CsvResult
{
    /// <summary>
    /// Sequence number within the list, starting with 1 for the first row.
    /// </summary>
    [CsvField("Seq")]
    public int Sequence { get; set; }

    /// <summary>
    /// Internal identity of the result, unique within this Type of test.
    /// </summary>
    [CsvField("ReplicateId")]
    public string ReplicateId { get; init; } = "";

    /// <summary>
    /// Time of measurement.
    /// Date and time are serialized separately.
    /// This field is required for sorting, but is not directly part of csv output.
    /// </summary>
    public required DateTimeOffset MeasurementDateTime { get; init; }

    [CsvField("MeasDate")]
    public string MeasurementDate => MeasurementDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [CsvField("MeasTime")]
    public string MeasurementTime => MeasurementDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Main status: Measured or Aborted. <see cref="ResultStatus"/>
    /// </summary>
    [CsvField("Status")]
    public required int Status { get; init; }

    /// <summary>
    /// Identity of Calibrator, CC, QC or Sample.
    /// </summary>
    [CsvField("SampleId")]
    public required string SampleId { get; init; }

    /// <summary>
    /// Request identity if type of test is Sample.
    /// </summary>
    [CsvField("Request")]
    public string? RequestId { get; init; }

    /// <summary>
    /// Abbreviated name of method (example "sIgE").
    /// </summary>
    [CsvField("MethodName")]
    public required string MethodName { get; init; }

    /// <summary>
    /// Name of test (example "t17").
    /// </summary>
    [CsvField("TestName")]
    public string TestName { get; init; } = "";

    /// <summary>
    /// Type of test (Calibrator, CC, QC or Sample). <see cref="TestResultType"/>
    /// </summary>
    [CsvField("SampleType")]
    public required int SampleType { get; init; }

    /// <summary>
    /// Lot number of ImmunoCap or EliA Well carrier.
    /// </summary>
    [CsvField("CarrierLotNumber")]
    public string CarrierLotNumber { get; set; } = "";

    /// <summary>
    /// Unique Serial Number of ImmunoCap or EliA Well carrier.
    /// </summary>
    [CsvField("CarrierSerialNumber")]
    public string CarrierSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the ImmunoCap or EliA Well was expired when used.
    /// </summary>
    [CsvField("CarrierExpired")]
    public int CarrierExpired { get; set; }

    /// <summary>
    /// Cartridge lot number of Calibrator, CC or QC used.
    /// </summary>
    /// <remarks>
    /// Applicable only for Type QualityControl, Calibrator or CurveControl results.
    /// </remarks>
    [CsvField("CALCCQCLotNumber")]
    public string CalCcQcLotNumber { get; set; } = "";

    /// <summary>
    /// Cartridge unique serial number of Calibrator, CC or QC used to process the result.
    /// </summary>
    /// <remarks>
    /// Applicable only for Type QualityControl, Calibrator or CurveControl results.
    /// </remarks>
    [CsvField("CALCCQCSerialNumber")]
    public string CalCcQcSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Calibrator, CC or QC was expired when used.
    /// </summary>
    [CsvField("CALCCQCExpired")]
    public int CalCcQcExpired { get; set; }

    /// <summary>
    /// Cartridge lot number of Diluent.
    /// </summary>
    [CsvField("DiluentLotNumber")]
    public string DiluentLotNumber { get; set; } = "";

    /// <summary>
    /// Cartridge unique serial number of Diluent.
    /// </summary>
    [CsvField("DiluentSerialNumber")]
    public string DiluentSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Diluent was expired when used.
    /// </summary>
    [CsvField("DiluentExpired")]
    public int DiluentExpired { get; set; }

    /// <summary>
    /// Cartridge lot number of Conjugate.
    /// </summary>
    [CsvField("ConjugateLotNumber")]
    public string ConjugateLotNumber { get; set; } = "";

    /// <summary>
    /// Cartridge unique seral number of Conjugate.
    /// </summary>
    [CsvField("ConjugateSerialNumber")]
    public string ConjugateSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Conjugate was expired when used.
    /// </summary>
    [CsvField("ConjugateExpired")]
    public int ConjugateExpired { get; set; }

    /// <summary>
    /// Cartridge lot number of Development solution.
    /// </summary>
    [CsvField("DevelopmentLotNumber")]
    public string DevelopmentLotNumber { get; set; } = "";

    /// <summary>
    /// Cartridge unique serial number of Development solution.
    /// </summary>
    [CsvField("DevelopmentSerialNumber")]
    public string DevelopmentSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Development solution was expired when used.
    /// </summary>
    [CsvField("DevelopmentExpired")]
    public int DevelopmentExpired { get; set; }

    /// <summary>
    /// Lot number of Stop solution bottle.
    /// </summary>
    [CsvField("StopLotNumber")]
    public string StopLotNumber { get; set; } = "";

    /// <summary>
    /// Unique serial number of Stop solution bottle.
    /// </summary>
    [CsvField("StopSerialNumber")]
    public string StopSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Stop solution was expired when used.
    /// </summary>
    [CsvField("StopExpired")]
    public int StopExpired { get; set; }

    /// <summary>
    /// Lot number of new Stop solution bottle used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("Stop2LotNumber")]
    public string Stop2LotNumber { get; set; } = "";

    /// <summary>
    /// Unique serial number of new Stop solution bottle used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("Stop2SerialNumber")]
    public string Stop2SerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the new Stop solution bottle was expired when used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("Stop2Expired")]
    public int Stop2Expired { get; set; }

    /// <summary>
    /// Lot number of Wash concentrate bottle used.
    /// </summary>
    [CsvField("WashConcLotNumber")]
    public string WashConcentrateLotNumber { get; set; } = "";

    /// <summary>
    /// Unique serial number of Wash concentrate bottle.
    /// </summary>
    [CsvField("WashConcSerialNumber")]
    public string WashConcentrateSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Wash concentrate bottle was expired when used.
    /// </summary>
    [CsvField("WashConcExpired")]
    public int WashConcentrateExpired { get; set; }

    /// <summary>
    /// Lot number of Wash additive bottle.
    /// </summary>
    [CsvField("WashAddLotNumber")]
    public string WashAdditiveLotNumber { get; set; } = "";

    /// <summary>
    /// Unique serial number of Wash additive bottle.
    /// </summary>
    [CsvField("WashAddSerialNumber")]
    public string WashAdditiveSerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Wash additive bottle was expired when used.
    /// </summary>
    [CsvField("WashAddExpired")]
    public int WashAdditiveExpired { get; set; }

    /// <summary>
    /// Lot number of Wash concentrate bottle when used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("WashConc2LotNumber")]
    public string WashConcentrate2LotNumber { get; set; } = "";

    /// <summary>
    /// Unique serial number of Wash concentrate bottle when used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("WashConc2SerialNumber")]
    public string WashConcentrate2SerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Wash concentrate bottle was expired when used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("WashConc2Expired")]
    public int WashConcentrate2Expired { get; set; }

    /// <summary>
    /// Lot number of Wash additive bottle when used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("WashAdd2LotNumber")]
    public string WashAdditive2LotNumber { get; set; } = "";

    /// <summary>
    /// Unique serial number of Wash additive bottle when used during period of uncertainty due to bottle switching.
    /// </summary>
    [CsvField("WashAdd2SerialNumber")]
    public string WashAdditive2SerialNumber { get; set; } = "";

    /// <summary>
    /// Flag indicating if the Wash additive bottle was expired when used during period of uncertainty during switch of bottle.
    /// </summary>
    [CsvField("WashAdd2Expired")]
    public int WashAdditive2Expired { get; set; }

    /// <summary>
    /// Response in RU (0 if Low RU).
    /// </summary>
    [CsvField("RU")]
    public double Ru { get; init; }

    /// <summary>
    /// Flag indicating if Low RU was encountered. LOW RU is below 5 RU.
    /// </summary>
    [CsvField("Low RU")]
    public int LowRu { get; init; }

    /// <summary>
    /// Concentration, in units.
    /// </summary>
    [CsvField("Conc")]
    public double Concentration { get; init; }

    /// <summary>
    /// Quotient between RU and lowest calibrator (only for sample and QC).
    /// </summary>
    [CsvField("Quot")]
    public double? Quotient { get; init; }

    /// <summary>
    /// Class (only for sample).
    /// </summary>
    [CsvField("Class")]
    public string? SampleClass { get; init; }

    /// <summary>
    /// Cut-off (only for sample).
    /// </summary>
    [CsvField("Cut-off")]
    public string? CutOff { get; init; }

    /// <summary>
    /// Flag indicating if a process error occurred for this ImmunoCap or EliA Well.
    /// </summary>
    [CsvField("Errors")]
    public int Errors { get; init; }

    /// <summary>
    /// Pre-dilution factor for samples and QC (empty for other types).
    /// </summary>
    [CsvField("PreDil")]
    public int? PreDilution { get; init; }

    /// <summary>
    /// Instrument-dilution factor for samples (empty for other types).
    /// </summary>
    [CsvField("InsDil")]
    public int? InstrumentDilution { get; init; }


    /// <summary>
    /// Internal identity of the Analytical Run the result belongs to.
    /// </summary>
    [CsvField("AnalyticalRunId")]
    public string AnalyticalRunId { get; init; } = "";

    /// <summary>
    /// Comment given by operator for this result (in Pharos).
    /// </summary>
    [CsvField("Comment")]
    public string? Comment { get; init; }

    /// <summary>
    /// Measurement value (mV)
    /// </summary>
    [CsvField("FlMeasurementValue")]
    public double FluorometerMeasurementValue { get; init; }

    /// <summary>
    /// Identity of used Reagent blank.
    /// </summary>
    [CsvField("Blank")]
    public int Blank { get; init; }

    /// <summary>
    /// Identity as used by CMR and Request Import, else the field is empty.
    /// </summary>
    [CsvField("CustomRunId")]
    public string? CustomRunId { get; init; }

    /// <summary>
    /// Identity of tests with a CmrIdentity. From a CMR Run or from request added using Request import.
    /// </summary>
    [CsvField("CmrTestId")]
    public string? CmrTestId { get; init; }

    /// <summary>
    /// If calibrator, identity of the curve that it belongs to.
    /// </summary>
    [CsvField("CurveId")]
    public int CurveId { get; init; }

    /// <summary>
    /// Identity of curve the result is calculated against (0 = not calc).
    /// </summary>
    [CsvField("CalcCurveId")]
    public int CalculationCurveId { get; init; }

    /// <summary>
    /// Internal group identity of this group of replicates.
    /// </summary>
    [CsvField("GroupId")]
    public string GroupId { get; init; } = "";

    /// <summary>
    /// Calibrator Original Response in RU (0 if Low RU ). Empty if not edited.
    /// </summary>
    /// <remarks>Only used in <![CDATA[ R&D ]]>.</remarks>
    [CsvField("OrgResp")]
    public double? OriginalResponse { get; init; }

    /// <summary>
    /// Calibrator edited Response, in RU. Empty if not edited.
    /// </summary>
    /// <remarks>Only used in <![CDATA[ R&D ]]>.</remarks>
    [CsvField("EditResp")]
    public double? EditResponse { get; init; }

    /// <summary>
    /// Flag indicating if the calibrator response has been edited.
    /// </summary>
    /// <remarks>Only used in <![CDATA[ R&D ]]>.</remarks>
    [CsvField("RespEdited")]
    public int ResponseEdited { get; init; }

    /// <summary>
    /// Result code for Calibrators, CC, QC, and Curve controls. Enum varies based on test order type.
    /// </summary>
    [CsvField("Judgement")]
    public int? Judgement { get; init; }

    /// <summary>
    /// Error codes occurred during processing of this result (list with error codes separated by \).
    /// </summary>
    [CsvField("ErrorCodes")]
    public string ErrorCodes { get; init; } = "";

    /// <summary>
    /// IRW spoke.
    /// </summary>
    [CsvField("IRWSpoke")]
    public int IrwSpoke { get; init; }

    /// <summary>
    /// IRW row.
    /// </summary>
    [CsvField("IRWRow")]
    public int IrwRow { get; init; }

    /// <summary>
    /// ERW spoke.
    /// </summary>
    [CsvField("ERWSpoke")]
    public int ErwSpoke { get; init; }

    /// <summary>
    /// ERW row.
    /// </summary>
    [CsvField("ERWRow")]
    public int ErwRow { get; init; }
}
