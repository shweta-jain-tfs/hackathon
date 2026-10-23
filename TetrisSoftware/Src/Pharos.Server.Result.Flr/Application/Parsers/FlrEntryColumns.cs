namespace Pharos.Server.Result.Flr.Application.Parsers;

/// <summary>
/// List column names from the [ENTRIES] section header line of the FLR file.
/// This class only includes those names that are used by Pharos.
/// For a complete list of column names, please refer the specification (DocID 807463).
/// </summary>
internal class FlrEntryColumns
{
    // Test order
    public const string GroupId = "GroupId";
    public const string CmrTestId = "CmrTestId";
    public const string ReplicateId = "ReplicateId";

    // Method
    public const string MethodName = "MethodName";
    public const string Technology = "Technology";

    // Sample
    public const string SampleId = "SampleId";
    public const string SampleBarcode = "SampleBarcode";
    public const string TubeId = "TubeId";
    public const string SampleType = "SampleType";
    public const string SampleTubeType = "SampleTubeType";
    public const string SampleHighPriority = "SampleHighPriority";

    // Test
    public const string TestName = "TestName";
    public const string TestBarcode = "TestBarcode";
    public const string TestUdi = "TestUDI";
    public const string TestExpired = "TestExpired";
    public const string TestPositionIrw = "TestPositionIRW";
    public const string TestPositionErw = "TestPositionERW";
    public const string AnalyticalRunId = "AnalyticalRunId";
    public const string CustomRunId = "CustomRunId";
    public const string ConjugateName = "ConjugateName";
    public const string ConjugateBarcode = "ConjugateBarcode";
    public const string ConjugateLocation = "ConjugateLocation";
    public const string ConjugateExpired = "ConjugateExpired";
    public const string DevelopmentBarcode = "DevelopmentBarcode";
    public const string DevelopmentLocation = "DevelopmentLocation";
    public const string DevelopmentExpired = "DevelopmentExpired";
    public const string StopBarcode = "StopBarcode";
    public const string StopExpired = "StopExpired";
    public const string WashConcentrateBarcode = "WashConcBarcode";
    public const string WashAddBarcode = "WashAddBarcode";

    // Result
    public const string MeasurementType = "MeasurementType";
    public const string MeasurementValue = "MeasurementValue";
    public const string MeasurementDateTime = "MeasurementDateTime";
    public const string FluorometerStatus = "FluorometerStatus";

    // Dilution
    public const string PreDilution = "PreDilution";
    public const string InstrumentDilution = "InstrumentDilution";
    public const string DilutionPlateBarcode = "DilutionPlateBarcode";
    public const string Dilstep1Barcode = "Dilstep1Barcode";
    public const string Dilstep2Barcode = "Dilstep2Barcode";
    public const string Dilstep3Barcode = "Dilstep3Barcode";
    public const string Dilstep1DiluentPos = "Dilstep1DiluentPos";
    public const string Dilstep2DiluentPos = "Dilstep2DiluentPos";
    public const string Dilstep3DiluentPos = "Dilstep3DiluentPos";
    public const string Dilstep1DiluentExpired = "Dilstep1DiluentExpired";
    public const string Dilstep2DiluentExpired = "Dilstep2DiluentExpired";
    public const string Dilstep3DiluentExpired = "Dilstep3DiluentExpired";

    /// <summary>
    /// Gets a dictionary containing all supported column names and their mandatory status.
    /// (whether empty value is allowed).
    /// </summary>
    public static readonly (string ColumnName, bool Essential)[] All =
    [
        (GroupId, true),
        (CmrTestId, true),
        (ReplicateId, true),

        (MethodName, true),
        (Technology, true),

        (SampleId, true),
        (SampleBarcode, false),
        (SampleType, true),
        (SampleTubeType, false),
        (TubeId, false),
        (SampleHighPriority, false),

        (TestName, false),
        (TestBarcode, true),
        (TestUdi, false),
        (TestExpired, true),
        (TestPositionIrw, false),
        (TestPositionErw, false),
        (AnalyticalRunId, false),
        (CustomRunId, false),
        (ConjugateName, false),
        (ConjugateBarcode, false),
        (ConjugateLocation, false),
        (ConjugateExpired, false),
        (DevelopmentBarcode, true),
        (DevelopmentLocation, true),
        (DevelopmentExpired, false),
        (StopBarcode, false),
        (StopExpired, false),
        (WashConcentrateBarcode, false),
        (WashAddBarcode, false),

        (MeasurementType, false),
        (MeasurementValue, true),
        (MeasurementDateTime, false),
        (FluorometerStatus, false),

        (PreDilution, false),
        (InstrumentDilution, false),
        (DilutionPlateBarcode, false),
        (Dilstep1Barcode, false),
        (Dilstep2Barcode, false),
        (Dilstep3Barcode, false),
        (Dilstep1DiluentPos, false),
        (Dilstep2DiluentPos, false),
        (Dilstep3DiluentPos, false),
        (Dilstep1DiluentExpired, false),
        (Dilstep2DiluentExpired, false),
        (Dilstep3DiluentExpired, false),
    ];
}
