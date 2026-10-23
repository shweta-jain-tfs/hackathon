namespace Pharos.Server.Application.Lis;

/// <summary>
/// Information about a single test order sent by LIS.
/// </summary>
public record LisTestOrder
{
    /// <summary>
    /// The unique ID of the test order in Pharos (included here for synchronization purposes).
    /// </summary>
    public Guid TestOrderId { get; set; }

    /// <summary>
    /// The name of an assay method that must be used for the test run. It may or may not be the same
    /// as the Pharos internal method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// The test name (within a method) defined by a laboratory. It may or may not be the same
    /// as the Pharos internal test name.
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// The full (long) test name defined by laboratory.
    /// </summary>
    public string FullTestName { get; set; } = string.Empty;

    /// <summary>
    /// Number of times (integer) the instrument should dilute for this test.
    /// A value of 1 represents “no dilution”.
    /// </summary>
    public int? InstrumentDilutionFactor { get; set; }

    /// <summary>
    /// Number of times the sample was diluted by the lab before this test order.
    /// Null means the imported LIS pre-dilution must be ignored and the method/test default is used.
    /// </summary>
    public int? PreDilutionFactor { get; set; }

    /// <summary>
    /// The requested number of replicates for the test. The result for the test order
    /// will the mean response value across all replicates.
    /// </summary>
    public int? NumberOfReplicates { get; set; }

    /// <summary>
    /// Represents the action for this test intended by the lab (e.g. cancel the test).
    /// </summary>
    public LisTestOrderAction Action { get; set; } = LisTestOrderAction.NewTest;

    /// <summary>
    /// The test result for the order.
    /// </summary>
    public LisSampleTestResult? TestResult { get; set; }

    /// <summary>
    /// Indicates whether concentration should be reported for this method test.
    /// </summary>
    public bool ReportConcentration { get; set; }

    /// <summary>
    /// Indicates whether class should be reported for this method test.
    /// </summary>
    public bool ReportClass { get; set; }

    /// <summary>
    /// Indicates whether cut-off should be reported for this method test.
    /// </summary>
    public bool ReportCutOff { get; set; }

    /// <summary>
    /// Indicates whether quotient should be reported for this method test.
    /// </summary>
    public bool ReportQuotient { get; set; }

    /// <summary>
    /// Indicates whether panel expansion is enabled for this test order.
    /// When true and MethodName is empty, the TestName will be treated as a panel name to be expanded.
    /// </summary>
    public bool PanelExpansionEnabled { get; set; }

    /// <summary>
    /// Indicates automatic panel recognition for test orders incoming from single message only
    /// </summary>
    public bool AutoPanelRecognitionFromOneMessage { get; set; }

    /// <summary>
    /// Indicates automatic panel recognition for test orders incoming from multiple messages
    /// </summary>
    public bool AutoPanelRecognitionFromMultipleMessage { get; set; }

    /// <summary>
    /// Enforce that laboratory test names are unique across all methods
    /// </summary>
    public bool ForceUniqueLaboratoryTestNames { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether requests are prioritized by order number A.
    /// </summary>
    public bool UseRequestPriorityA { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether requests are prioritized by order number B. 
    /// </summary>
    public bool UseRequestPriorityB { get; set; }

    /// <summary>
    /// The names of the panels that this test order belongs to (if any).
    /// Used when exporting to LIS with panel name instead of individual test names.
    /// Multiple panel names can be assigned to a single test order.
    /// </summary>
    public List<string> PanelNames { get; set; } = [];

    /// <summary>
    /// The name of the reflex definition that LIS wants to use for this test.
    /// When specified, only this reflex definition will be attached (others suppressed).
    /// </summary>
    public string ReflexName { get; set; } = string.Empty;

    /// <summary>
    /// Controls whether reflex testing should be suppressed for this test order.
    /// - false (0): Normal reflex processing (attach definitions, evaluate conditions)
    /// - true (1): Suppress all reflex testing for this test order
    /// </summary>
    public bool SuppressReflex { get; set; }

    /// <summary>
    /// The Tray ID where the sample tube is located.
    /// </summary>
    public string? TrayId { get; set; }

    /// <summary>
    /// The position of the sample tube in the tray.
    /// </summary>
    public int? TubePosition { get; set; }
}
