namespace Pharos.Server.Lis.Astm.Application.Messages;
internal static class AstmRecordType
{
    public const char MessageHeader = 'H';
    public const char PatientInformation = 'P';
    public const char RequestInformation = 'Q';
    public const char TestOrder = 'O';
    public const char Result = 'R';
    public const char Comment = 'C';
    public const char Scientific = 'S';
    public const char ManufacturerInformation = 'M';
    public const char MessageTerminator = 'L';
}
