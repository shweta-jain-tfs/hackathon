using MediatR;
using Pharos.Server.Domain.Enums;


namespace Pharos.Server.Application.SystemErrors;

/// <summary>
/// Creates a Pharos-owned entry in the system error log.
/// </summary>
/// <param name="ErrorCode">ErrorCode (one of <see cref="PharosErrorCodes"/>)</param>
/// <param name="Classification">The severity of the error</param>
/// <param name="InstrumentNumber">The serial number of the instrument which caused the error.</param>
/// <param name="Parameters">A list of parameter to render error message template.</param>
/// <param name="AllowMultiple">A value indicating whether Pharos should add multiple errors of this type or keep the latest.</param>
public record CreatePharosErrorCommand(int ErrorCode, ErrorClassification Classification, string? InstrumentNumber = null, List<string>? Parameters = null, bool AllowMultiple = true) : IRequest;

public static class PharosErrorCodes
{
    // Error code for parameter import failure
    public static int ParameterImportFailure => 1000;

    public static int ResultForUnknownMethodAndTestCombination => 1001;

    /// <summary>
    /// Error code for an assay result discarded because its analytical run has ended.
    /// </summary>
    public static int ResultReceivedAfterRunEnd => 1002;

    /// <summary>
    /// Error code for missing request or test order when a result exists.
    /// </summary>
    public static int MissingRequestOrTestOrder => 1010;

    /// <summary>
    /// Error code for duplicate laboratory test names detected when Force Unique Laboratory Test Names setting is enabled.
    /// </summary>
    public static int DuplicateLaboratoryTestNamesDetected => 1020;

    /// <summary>
    /// Error code for when pre-dilution factors are normalized because imported tests have different pre-dilution factors.
    /// </summary>
    public static int PreDilutionFactorsNormalized => 1030;

    /// <summary>
    /// Error code for invalid sample tube type when no default tube is set for Normal or Pediatric tube.
    /// </summary>
    public static int InvalidSampleTubeType => 1031;

    /// <summary>
    /// Error code for when a test is skipped due to illegal or mismatched dilution factors.
    /// </summary>
    public static int TestSkippedDueToIllegalDilutionFactors => 1040;

    /// <summary>
    /// Error code for when an unknown test is received from LIS or the test/method is disabled.
    /// </summary>
    public static int UnknownTestReceivedFromLis => 1050;

    public static int FlrImportFailed => 1051;

    public static int RequestImportFailed => 1052;

    public static int InstrumentIncompatible => 1053;
    public static int InstrumentLimitedCompatibility => 1054;

    public static int LisInvalidMessage => 1060;

    public static int ReflexDefinitionCreated => 1070;
    public static int ReflexDefinitionDeleted => 1071;
    public static int AutomaticFollowUpTestsAdded => 1072;
    public static int ManualFollowUpTestsAdded => 1073;
    public static int ManualFollowUpTestsDiscarded => 1074;

}
