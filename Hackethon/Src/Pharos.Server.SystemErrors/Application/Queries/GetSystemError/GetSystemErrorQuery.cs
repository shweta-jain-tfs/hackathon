using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Application.Queries.GetSystemErrors;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Queries.GetSystemError;

internal record GetSystemErrorQuery(int SystemErrorId) : IRequest<SystemErrorSummaryDto>;

internal class GetSystemErrorQueryHandler : IRequestHandler<GetSystemErrorQuery, SystemErrorSummaryDto>
{
    private readonly ISystemErrorsDbContext _dbContext;
    private readonly IInstrumentManager _instrumentManager;

    public GetSystemErrorQueryHandler(ISystemErrorsDbContext dbContext, IInstrumentManager instrumentManager)
    {
        _dbContext = dbContext;
        _instrumentManager = instrumentManager;
    }

    public async Task<SystemErrorSummaryDto> Handle(GetSystemErrorQuery request, CancellationToken cancellationToken)
    {
        var error = await _dbContext.SystemErrors.FirstOrDefaultAsync(x => x.Id == request.SystemErrorId, cancellationToken);
        if (error is null)
        {
            throw new NotFoundException(nameof(SystemError), request.SystemErrorId);
        }

        return new SystemErrorSummaryDto
        {
            Id = error.Id,
            Classification = error.Classification,
            Code = error.Code,
            Timestamp = error.Timestamp,
            Response = error.Response,
            InstrumentName = _instrumentManager.FindInstrument(error.SerialNumber)?.DisplayName ?? error.SerialNumber
        };
    }
}