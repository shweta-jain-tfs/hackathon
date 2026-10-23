using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Sample.Application.Interfaces;

/// <summary>
/// Provides functionality for mapping a sample barcode and a LIS specific sample ID to Pharos sample ID.
/// </summary>
public interface ISampleQcIdProvider
{
    Task<SampleQcTubeKey> GetSampleQcTubeKey(string? barcode);
    Task<SampleQcTubeKey> GetSampleQcTubeKey(string? barcode, string instrumentNumber);
    SampleQcTubeKey GetSampleTubeKeyNoBarcode(string sampleId);
}

public record SampleQcTubeKey(string? Barcode, SampleType SampleType, string Id, string IdSuffix, bool UnreadableBarcode, bool TubeNeeded, string QcLotNumber, int TubeTypeId)
{
    public string TubeId
    {
        get
        {
            if (string.IsNullOrEmpty(Id) || UnreadableBarcode)
            {
                return IdSuffix;
            }

            if (string.IsNullOrEmpty(IdSuffix))
            {
                return Id;
            }

            return $"{Id}_{IdSuffix}";
        }
    }
}