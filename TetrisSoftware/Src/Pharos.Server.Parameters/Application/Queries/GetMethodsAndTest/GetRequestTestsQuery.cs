using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Request;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public abstract class GetTestsQuery : SortedPartialQuery<MethodTestDto>
{
    public string? TestName { get; set; }
    public string? LongTestName { get; set; }
    public string? MethodName { get; set; }

    public List<string>? SelectedMethods { get; set; }
}

public class GetRequestTestsQuery : GetTestsQuery;

internal class GetRequestTestsQueryHandler : IRequestHandler<GetRequestTestsQuery, PartialQueryResult<MethodTestDto>>
{
    private readonly ISender _sender;

    public GetRequestTestsQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<PartialQueryResult<MethodTestDto>> Handle(GetRequestTestsQuery request, CancellationToken cancellationToken)
    {
        var isUsingLabTestNames = await _sender.Send(new GetIsUsingLaboratoryTestNamesQuery(), cancellationToken);

        if (isUsingLabTestNames)
        {
            return await _sender.Send<PartialQueryResult<MethodTestDto>>(new GetRequestTestsWithLaboratoryTestNamesQuery
            {
                TestName = request.TestName,
                Count = request.Count,
                LongTestName = request.LongTestName,
                MethodName = request.MethodName,
                SelectedMethods = request.SelectedMethods,
                StartIndex = request.StartIndex,
                SortByProperty = request.SortByProperty,
                SortByAscending = request.SortByAscending
            }, cancellationToken);
        }

        // at the moment get reflex test is not using laboratory test names
        return await _sender.Send<PartialQueryResult<MethodTestDto>>(new GetReflexTestsQuery
        {
            TestName = request.TestName,
            Count = request.Count,
            LongTestName = request.LongTestName,
            MethodName = request.MethodName,
            SelectedMethods = request.SelectedMethods,
            StartIndex = request.StartIndex,
            SortByProperty = request.SortByProperty,
            SortByAscending = request.SortByAscending
        }, cancellationToken);
    }
}