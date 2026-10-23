using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Mapping;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

internal record GetMethodTestDetailsQuery(int TestId) : IRequest<MethodTestDetailsDto>;

internal sealed class GetMethodTestDetailsQueryHandler(IParametersDbContext context, IDateTime dateTime, IOptionsSnapshot<LisSettings> lisSettings) : IRequestHandler<GetMethodTestDetailsQuery, MethodTestDetailsDto>
{
    public async Task<MethodTestDetailsDto> Handle(GetMethodTestDetailsQuery request, CancellationToken cancellationToken)
    {
        var methodTest = await context.MethodTests
            .AsNoTracking()
            .Include(x => x.Method)
                .ThenInclude(x => x.ConcentrationUnits)
            .Include(x => x.Method)
                .ThenInclude(x => x.ClassTables)
            .Include(x => x.Method)
                .ThenInclude(x => x.ClassTableBelows)
            .Include(x => x.DefaultConcentrationUnit)
            .Include(x => x.Priority)
            .Include(x => x.LabTestNames)
            .Where(x => x.Id == request.TestId)
            .SingleOrDefaultAsync(cancellationToken);

        if (methodTest == null)
        {
            throw new NotFoundException();
        }

        var forceUniqueLaboratoryTestNames = lisSettings.Value.LisChannels
           .Any(x => x.ImportSettings.ForceUniqueLaboratoryTestNames);

        var testDetailsDto = methodTest.ToMethodTestDetailsDto();

        testDetailsDto.QueriedAt = dateTime.Now;
        testDetailsDto.ClassTables = GetClassTables(methodTest.Method);

        if (forceUniqueLaboratoryTestNames)
        {
            var thisTestLabNames = await context.MethodTestLabNames
                .Where(x => x.MethodTestId == request.TestId)
                .Select(x => x.TestName)
                .ToListAsync(cancellationToken);

            testDetailsDto.LaboratoryTestNamesDuplicatedAcrossMethods = await context.MethodTestLabNames
                .Where(x => x.MethodTest.MethodId != testDetailsDto.MethodId &&
                            x.MethodTest.Active &&
                            x.MethodTest.Method.Enabled &&
                            thisTestLabNames.Contains(x.TestName))
                .Select(x => x.TestName)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        return testDetailsDto;
    }

    private static List<MethodClassTableOptionDto> GetClassTables(Method method)
    {
        return method.ClassTables
            .GroupBy(x => x.ClassTableId)
            .Where(g => method.ClassTableBelows.Any(b =>
                b.ClassTableId == g.Key &&
                !string.IsNullOrEmpty(b.TextBelowConcentration)))
            .OrderBy(g => g.Key)
            .Select(g => new MethodClassTableOptionDto
            {
                ClassTableId = g.Key,
                DisplayName = g.Key.ToString()
            })
            .ToList();
    }
}
