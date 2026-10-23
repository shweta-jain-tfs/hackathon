namespace Pharos.Server.Lis.Domain;

/// <summary>
/// Defines metadata key names used in LIS message delivery tracking.
/// </summary>
public static class LisMessageMetadataKeys
{
    /// <summary>
    /// Sample IDs associated with a test order query message.
    /// </summary>
    public const string SampleIds = "SampleIds";

    /// <summary>
    /// Test order IDs associated with a test result message.
    /// </summary>
    public const string TestOrderIds = "TestOrderIds";

    /// <summary>
    /// Test result ID associated with a quality control test result message.
    /// </summary>
    public const string QcTestResultId = "QcTestResultId";

    /// <summary>
    /// Message control ID associated with a HL7 LIS message.
    /// </summary>
    public const string MessageControlId= "MessageControlId";

    /// <summary>
    /// Trigger event associated with a HL7 LIS message.
    /// </summary>
    public const string TriggerEvent= "TriggerEvent";

    /// <summary>
    /// Processing ID associated with a HL7 LIS message.
    /// </summary>
    public const string ProcessingId= "ProcessingId";
}