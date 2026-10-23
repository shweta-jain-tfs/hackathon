namespace Pharos.Server.Lis.Domain;

public enum LisMessageType
{
    Unknown = 0,
    SampleTestResult = 1,
    TestOrderQuery = 2,
    QualityControlTestResult = 3,
    Acknowledgement = 4,
}
