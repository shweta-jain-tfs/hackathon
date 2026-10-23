namespace Pharos.Server.Lis.HL7.Application.Messages;

internal static class HL7SegmentType
{
    public const string MessageHeader = "MSH";
    public const string QueryParameterDefinition = "QPD";
    public const string ResponseControlParameter = "RCP";
    public const string PatientIdentification = "PID";
    public const string ObservationRequest = "OBR";
    public const string SpecimenContainer = "SAC";
    public const string Specimen = "SPM";
    public const string AdditionalTestInformation = "OBX";
    public const string CommonOrder = "ORC";
    public const string Comment = "NTE";
    public const string MessageAcknowledgement = "MSA";
    public const string Error = "ERR";
    public const string QueryAcknowledgement = "QAK";
    public const string EquipmentDetail = "EQU";
    public const string InventoryDetail = "INV";
}
