using MediatR;

namespace Pharos.Server.Application.Parameters;

public record GetQualityControlTypeQuery(string QualityControlName) : IRequest<QualityControlTypeDto>;

public record QualityControlTypeDto(bool QualityControlExists, bool IsPhadiaQualityControl, bool IsLabQualityControl, bool IsCartridgeType)
{
    public static QualityControlTypeDto Phadia(bool isCartridgeType)
    {
        return new QualityControlTypeDto(true, true, false, isCartridgeType);
    }

    public static QualityControlTypeDto Lab()
    {
        return new QualityControlTypeDto(true, false, true, false);
    }

    public static QualityControlTypeDto DoNotExist()
    {
        return new QualityControlTypeDto(false, false, false, false);
    }
}