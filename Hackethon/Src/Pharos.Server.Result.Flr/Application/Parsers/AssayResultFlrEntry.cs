namespace Pharos.Server.Result.Flr.Application.Parsers;

internal enum FlrSampleType
{
    Unspecified = 0,
    Sample = 1,
    QualityControl = 2,
    CurveControl = 3,
    Calibrator = 4
}

internal class AssayResultFlrEntry : FlrEntry
{
    public string CmrTestId { get; set; } = string.Empty;
    public string ReplicateId { get; set; } = string.Empty;

    public string MethodName { get; set; } = string.Empty;


    public string SampleId { get; private set; } = string.Empty;
    public string SampleBarcode { get; private set; } = string.Empty;
    public FlrSampleType SampleType { get; private set; }
    public string TubeId { get; private set; } = string.Empty;
    public int SampleTubeType { get; private set; }
    public bool SampleHighPriority { get; private set; }


    public string TestName { get; set; } = string.Empty;
    public string TestBarcode { get; set; } = string.Empty;
    public string TestUdi { get; private set; } = string.Empty;
    public bool TestExpired { get; set; }
    public string AnalyticalRunId { get; set; } = string.Empty;
    public string CustomRunId { get; set; } = string.Empty;
    public string ConjugateBarcode { get; set; } = string.Empty;
    public int? ConjugateWell { get; set; }
    public bool ConjugateExpired { get; set; }
    public bool DevelopmentExpired { get; set; }
    public bool StopExpired { get; set; }
    public string WashConcentrationBarcode { get; set; } = string.Empty;
    public string WashAddBarcode { get; set; } = string.Empty;
    public int IrwRow { get; private set; }
    public int IrwSpoke { get; private set; }
    public int ErwRow { get; private set; }
    public int ErwSpoke { get; private set; }
    public int ReplicateNumber { get; set; } = 1;
    public int ReplicatesInTestOrder { get; set; } = 1;
    public int TestOrderNumber { get; set; } = 1;
    public int TestOrdersInGroup { get; set; } = 1;


    public int PreDilution { get; private set; }
    public int InstrumentDilution { get; private set; }
    public string DilutionPlateBarcode { get; private set; } = null!;
    public string DiluentBarcode1 { get; set; } = string.Empty;
    public string DiluentBarcode2 { get; set; } = string.Empty;
    public string DiluentBarcode3 { get; set; } = string.Empty;
    public int? DiluentWell1 { get; set; }
    public int? DiluentWell2 { get; set; }
    public int? DiluentWell3 { get; set; }
    public bool DiluentExpired1 { get; set; }
    public bool DiluentExpired2 { get; set; }
    public bool DiluentExpired3 { get; set; }


    public bool IsAborted { get; private set; }


    public AssayResultFlrEntry(FlrMeasurementType measurementType) : base(measurementType)
    {
    }

    protected override void Parse(string[] columnValues, Dictionary<string, FlrEntryColumn> columns)
    {
        base.Parse(columnValues, columns);

        GroupId = columnValues.GetString(columns, FlrEntryColumns.GroupId, canBeEmpty: false);
        ReplicateId = columnValues.GetString(columns, FlrEntryColumns.ReplicateId, canBeEmpty: false);
        CmrTestId = columnValues.GetString(columns, FlrEntryColumns.CmrTestId, canBeEmpty: true);

        MethodName = columnValues.GetString(columns, FlrEntryColumns.MethodName, canBeEmpty: false);

        SampleId = columnValues.GetString(columns, FlrEntryColumns.SampleId, canBeEmpty: false);
        SampleBarcode = columnValues.GetString(columns, FlrEntryColumns.SampleBarcode);
        TubeId = columnValues.GetString(columns, FlrEntryColumns.TubeId);
        SampleType = (FlrSampleType)columnValues.GetInt(columns, FlrEntryColumns.SampleType, canBeEmpty: false);
        SampleTubeType = columnValues.GetInt(columns, FlrEntryColumns.SampleTubeType, defaultValue: 1);
        SampleHighPriority = columnValues.GetBool(columns, FlrEntryColumns.SampleHighPriority);

        TestName = columnValues.GetString(columns, FlrEntryColumns.TestName);
        TestBarcode = columnValues.GetString(columns, FlrEntryColumns.TestBarcode);
        TestUdi = columnValues.GetString(columns, FlrEntryColumns.TestUdi);
        TestExpired = columnValues.GetBool(columns, FlrEntryColumns.TestExpired);
        DevelopmentExpired = columnValues.GetBool(columns, FlrEntryColumns.DevelopmentExpired);
        AnalyticalRunId = columnValues.GetString(columns, FlrEntryColumns.AnalyticalRunId);
        CustomRunId = columnValues.GetString(columns, FlrEntryColumns.CustomRunId);
        ConjugateBarcode = columnValues.GetString(columns, FlrEntryColumns.ConjugateBarcode);
        ConjugateWell = columnValues.GetCartridgeCell(columns, FlrEntryColumns.ConjugateLocation);
        ConjugateExpired = columnValues.GetBool(columns, FlrEntryColumns.ConjugateExpired);
        StopExpired = columnValues.GetBool(columns, FlrEntryColumns.StopExpired);
        WashConcentrationBarcode = columnValues.GetString(columns, FlrEntryColumns.WashConcentrateBarcode);
        WashAddBarcode = columnValues.GetString(columns, FlrEntryColumns.WashAddBarcode);
        (IrwRow, IrwSpoke) = columnValues.GetWheelPosition(columns, FlrEntryColumns.TestPositionIrw, maxRow: 4, maxSpoke: 78);
        (ErwRow, ErwSpoke) = columnValues.GetWheelPosition(columns, FlrEntryColumns.TestPositionErw, maxRow: 2, maxSpoke: 90);

        IsAborted = MeasurementValue == 0;

        PreDilution = columnValues.GetInt(columns, FlrEntryColumns.PreDilution, defaultValue: 1);
        InstrumentDilution = columnValues.GetInt(columns, FlrEntryColumns.InstrumentDilution, defaultValue: 1);
        DilutionPlateBarcode = columnValues.GetString(columns, FlrEntryColumns.DilutionPlateBarcode);
        DiluentBarcode1 = columnValues.GetString(columns, FlrEntryColumns.Dilstep1Barcode);
        DiluentBarcode2 = columnValues.GetString(columns, FlrEntryColumns.Dilstep2Barcode);
        DiluentBarcode3 = columnValues.GetString(columns, FlrEntryColumns.Dilstep3Barcode);
        DiluentWell1 = columnValues.GetCartridgeCell(columns, FlrEntryColumns.Dilstep1DiluentPos);
        DiluentWell2 = columnValues.GetCartridgeCell(columns, FlrEntryColumns.Dilstep2DiluentPos);
        DiluentWell3 = columnValues.GetCartridgeCell(columns, FlrEntryColumns.Dilstep3DiluentPos);
        DiluentExpired1 = columnValues.GetBool(columns, FlrEntryColumns.Dilstep1DiluentExpired);
        DiluentExpired2 = columnValues.GetBool(columns, FlrEntryColumns.Dilstep2DiluentExpired);
        DiluentExpired3 = columnValues.GetBool(columns, FlrEntryColumns.Dilstep3DiluentExpired);
    }
}
