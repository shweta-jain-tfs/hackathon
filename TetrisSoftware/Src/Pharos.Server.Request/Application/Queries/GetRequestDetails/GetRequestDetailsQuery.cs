using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;

namespace Pharos.Server.Request.Application.Queries.GetRequestDetails;

internal record GetRequestDetailsQuery(int Id, string InstrumentNumber) : IRequest<RequestDetailsDto>;

internal sealed class GetRequestDetailsQueryHandler(
    IAuthorizationService authorizationService,
    IRequestDbContext context,
    RequestMapper requestMapper,
    IMethodsService methodsService,
    IInstrumentManager instrumentManager,
    IOptionsSnapshot<RequestSettings> requestSettings,
    ISender sender,
    ISampleIdFormatter sampleIdFormatter,
    IDateTime dateTime) : IRequestHandler<GetRequestDetailsQuery, RequestDetailsDto>
{
    public async Task<RequestDetailsDto> Handle(GetRequestDetailsQuery request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandlePatientInfo =
            requestSettings.Value.IsPatientInformationEnabled &&
            personalAndHealthPermissions;

        var canHandleRequestorInfo =
            requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        var query = context.Requests.Where(s => s.Id == request.Id);

        query = query
            .Include(x => x.TestOrders)
            .ThenInclude(s => s.ReflexDefinitions)!
            .ThenInclude(s => s.ReflexTests);

        if (canHandlePatientInfo)
        {
            query = query.Include(x => x.Patient);
        }

        if (canHandleRequestorInfo)
        {
            query = query.Include(x => x.Requestor);
        }

        var requestEntity = await query.FirstOrDefaultAsync(cancellationToken);

        if (requestEntity is not null)
        {
            var requestDto = requestMapper.ToRequestDetailsDto(requestEntity);
            requestDto.QueriedAt = dateTime.Now;
            var requestUid = requestEntity.GetRequestUid();
            requestDto.SampleIdDisplay = sampleIdFormatter.Format(requestUid);

            if (!canHandlePatientInfo)
            {
                //don´t display age if user lacks permission or setting is disabled
                requestDto.PatientAgeAtSampling = null;
            }

            if (requestDto.TestOrders != null)
            {
                foreach (var t in requestDto.TestOrders)
                {
                    // Fetch method test data.
                    var methodTestData = await methodsService.GetMethodTest(t.MethodName, t.TestName, x => new
                    {
                        x.Method.FullMethodName,
                        x.FullTestName,
                        x.ReportConcentration,
                        x.ReportCutOff,
                        x.ReportQuotient,
                        x.ReportClass
                    });

                    if (methodTestData != null)
                    {
                        t.FullMethodName = methodTestData.FullMethodName;
                        t.FullTestName = methodTestData.FullTestName;
                        t.ReportConcentration = methodTestData.ReportConcentration;
                        t.ReportCutOff = methodTestData.ReportCutOff;
                        t.ReportQuotient = methodTestData.ReportQuotient != QuotientReportMode.NoReport;
                        t.ReportClass = methodTestData.ReportClass;
                    }
                }

                var testResults = await sender.Send(new GetSampleTestResultsQuery(requestUid), cancellationToken).ConfigureAwait(false);

                // get additional values from test results
                foreach (var testResult in testResults)
                {
                    var testOrder = requestDto.TestOrders.FirstOrDefault(x => x.Uid == testResult.TestOrderId);
                    if (testOrder == null)
                    {
                        continue;
                    }

                    testOrder.Class = testResult.MeanClass;
                    testOrder.Concentration = testResult.MeanConcentrationAsText;
                    testOrder.MeasurementDateTime = testResult.Created;
                    testOrder.AnalyticalRunId = testResult.AnalyticalRunId;
                    testOrder.MeanQuotientAsText = testResult.MeanQuotientAsText;
                    testOrder.MeanCutOff = testResult.MeanCutOff;
                    testOrder.InstrumentName = instrumentManager.FindInstrument(testResult.InstrumentNumber)?.DisplayName ?? testResult.InstrumentNumber;
                }
            }

            return requestDto;
        }

        throw new NotFoundException();
    }
}