using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.LoadList;

public record GetCarrierRefillLevelConfigurationsQuery(IReadOnlyCollection<string> InstrumentNumbers)
    : IRequest<IReadOnlyList<CarrierRefillLevelConfigurationDto>>;

public record CarrierRefillLevelConfigurationDto(
    string InstrumentNumber,
    ArticleType ArticleType,
    string Identity,
    int RefillLevel);
