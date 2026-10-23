using MediatR;

namespace Pharos.Server.Application.Parameters;

/// <summary>
/// Returns a result that states if the barcode is a quality control or not.
/// </summary>
/// <remarks>The static property 'NotQualityControl' provides a default instance representing that the barcode is not a quality control</remarks>
/// <param name="IsQualityControl">Indicates if this barcode is a quality control</param>
/// <param name="QualityControlIdentity">The quality control name</param>
/// <param name="IsNeeded">Specifies if this quality control is needed by the instrument. Please note that this can only be trusted if the InstrumentNumber is provided in the query</param>
/// <param name="LotNumber">The lot number part of the barcode</param>
/// <param name="TubeTypeId">The tube type identifier</param>
public record QualityControlResultDto(bool IsQualityControl, string QualityControlIdentity, bool IsNeeded, string LotNumber, int TubeTypeId)
{
    public static QualityControlResultDto NotQualityControl => new(false, string.Empty, false, string.Empty, -1);
}

public record IsQualityControlQuery(string Barcode, string InstrumentNumber) : IRequest<QualityControlResultDto>;