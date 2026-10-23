using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Queries.GetReflexDefinition;

internal record GetReflexDefinitionQuery(int Id) : IRequest<ReflexDefinitionDto>;

internal sealed class GetReflexDefinitionQueryHandler(IReflexDbContext context, ISender sender, IMethodsService methodsService, IDateTime dateTime, IOptionsSnapshot<RequestSettings> requestSettings, IAuthorizationService authorizationService) : IRequestHandler<GetReflexDefinitionQuery, ReflexDefinitionDto>
{
    public async Task<ReflexDefinitionDto> Handle(GetReflexDefinitionQuery request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        var reflexEntity = await context.ReflexDefinitions
            .Where(r => r.Id == request.Id)
            .Include(x => x.ReflexCondition)
            .Include(x => x.ReflexTests)
            .FirstOrDefaultAsync(cancellationToken);

        var reflexDto = new ReflexDefinitionDto();

        if (reflexEntity is not null)
        {
            reflexDto.Id = reflexEntity.Id;
            reflexDto.Name = reflexEntity.Name;
            reflexDto.Active = reflexEntity.Active;
            reflexDto.HasRequestorEnabled = canHandleRequestorInfo && reflexEntity.RequestorId != null;
            reflexDto.Automatic = reflexEntity.Automatic;
            reflexDto.RequestorId = canHandleRequestorInfo ? reflexEntity.RequestorId : null;
            reflexDto.QueriedAt = dateTime.Now;

            if (reflexEntity.ReflexTests != null)
            {
                reflexDto.ReflexTests = new List<ReflexTestDto>();

                foreach (var reflexTest in reflexEntity.ReflexTests)
                {

                    var reflexTestDto = new ReflexTestDto
                    {
                        Id = reflexTest.Id,
                        TestName = reflexTest.TestName,
                        MethodName = reflexTest.MethodName,
                        NumberOfReplicates = reflexTest.NumberOfReplicates,
                        InstrumentDilutionFactor = reflexTest.InstrumentDilutionFactor,
                        TestSortOrder = reflexTest.TestSortOrder
                    };

                    var reflexTestData = await methodsService.GetMethodTest(reflexTest.MethodName, reflexTest.TestName, x => new
                    {
                        x.Method.FullMethodName,
                        x.FullTestName
                    });

                    if (reflexTestData != null)
                    {
                        reflexTestDto.TestFullName = reflexTestData.FullTestName;
                        reflexTestDto.MethodFullName = reflexTestData.FullMethodName;
                    }

                    reflexDto.ReflexTests.Add(reflexTestDto);
                }
            }

            // Send for more information from parameters
            var parameterInfo = await sender.Send(new GetTestResultInformationQuery(reflexEntity.ReflexCondition.TestName, reflexEntity.ReflexCondition.MethodName), cancellationToken);

            reflexDto.ReflexCondition = new ReflexConditionDto
            {
                TestName = reflexEntity.ReflexCondition.TestName,
                MethodName = reflexEntity.ReflexCondition.MethodName,
                ResultType = reflexEntity.ReflexCondition.ResultType,
                CompareType = reflexEntity.ReflexCondition.CompareType,
                ConcentrationCompareValue = reflexEntity.ReflexCondition.ConcentrationCompareValue,
                ConcentrationCompareValueUpperLimit = reflexEntity.ReflexCondition.ConcentrationCompareValueUpperLimit,
                ConcentrationRawCompareValue = reflexEntity.ReflexCondition.ConcentrationRawCompareValue,
                ConcentrationRawCompareValueUpperLimit = reflexEntity.ReflexCondition.ConcentrationRawCompareValueUpperLimit,
                ClassCompareValue = reflexEntity.ReflexCondition.ClassCompareValue,
                QuotientCompareValue = reflexEntity.ReflexCondition.QuotientCompareValue,
                QuotientCompareValueUpperLimit = reflexEntity.ReflexCondition.QuotientCompareValueUpperLimit,
                CutOffCompareValue = reflexEntity.ReflexCondition.CutOffCompareValue,
                RuCompareValue = reflexEntity.ReflexCondition.RuCompareValue,
                ClassOptions = parameterInfo.ClassOptions,
                CutOffOptions = parameterInfo.CutOffOptions,
                ReportingConcentration = parameterInfo.ReportConcentration,
                ReportingClass = parameterInfo.ReportClass,
                ReportingQuotient = parameterInfo.ReportQuotient,
                ReportingCutOff = parameterInfo.ReportCutOff
            };
        }
        else
        {
            throw new NotFoundException();
        }

        return reflexDto;
    }
}