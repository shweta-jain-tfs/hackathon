namespace Pharos.Server.Request.Application.Models;

internal class ImportedTest
{
    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public int? Replicates { get; set; }
    public int? InstrumentDilutionFactor { get; set; }
    public string CarrierLot { get; set; } = string.Empty;
    public string DiluentLot { get; set; } = string.Empty;
    public string ConjugateLot { get; set; } = string.Empty;
    public string DevelopmentLot { get; set; } = string.Empty;
    public string CalibratorCartridgeLot { get; set; } = string.Empty;
    public string CalibratorCarrierLot { get; set; } = string.Empty;
    public string CalibratorConjugateLot { get; set; } = string.Empty;
    public string CustomTestId { get; set; } = string.Empty;
}
