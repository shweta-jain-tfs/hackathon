using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Queries.GetSystemErrorDetails;

internal record GetSystemErrorDetailsQuery(int SystemErrorId) : IRequest<SystemErrorDetailsDto>;

internal class GetSystemErrorDetailsQueryHandler : IRequestHandler<GetSystemErrorDetailsQuery, SystemErrorDetailsDto>
{
    private readonly ISystemErrorsDbContext _dbContext;
    private readonly IInstrumentManager _instrumentManager;
    private readonly IStringLocalizer<Translations> _loc;

    public GetSystemErrorDetailsQueryHandler(ISystemErrorsDbContext dbContext, IInstrumentManager instrumentManager, IStringLocalizer<Translations> loc)
    {
        _dbContext = dbContext;
        _instrumentManager = instrumentManager;
        _loc = loc;
    }

    public async Task<SystemErrorDetailsDto> Handle(GetSystemErrorDetailsQuery request, CancellationToken cancellationToken)
    {
        var systemError = await _dbContext.SystemErrors.FirstOrDefaultAsync(x => x.Id == request.SystemErrorId, cancellationToken);
        if (systemError is null)
        {
            throw new NotFoundException(nameof(SystemError), request.SystemErrorId);
        }

        return Map(systemError);
    }

    private SystemErrorDetailsDto Map(SystemError systemError)
    {
        var systemLabel = _loc["InstrumentFilter_System"].ToString();

        return new SystemErrorDetailsDto
        {
            Id = systemError.Id,
            Code = systemError.Code,
            Classification = systemError.Classification,
            Parameters = systemError.Parameters,
            PossibleResponses = systemError.PossibleResponses,
            Timestamp = systemError.Timestamp,
            Response = systemError.Response,
            ResponseTimestamp = systemError.ResponseTimestamp,
            InstrumentName = _instrumentManager.FindInstrument(systemError.SerialNumber)?.DisplayName ?? systemError.SerialNumber ?? systemLabel
        };
    }
}