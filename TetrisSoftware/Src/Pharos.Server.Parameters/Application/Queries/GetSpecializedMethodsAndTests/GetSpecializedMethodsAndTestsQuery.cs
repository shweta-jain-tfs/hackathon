using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetSpecializedMethodsAndTests;

public class GetSpecializedMethodsAndTestsQuery : SortedPartialQuery<SpecializedMethodOrTestDto>
{
    public string? Name { get; set; }
    public List<string> Type { get; set; } = [];
}

internal sealed class GetSpecializedMethodsAndTestsQueryHandler(IParametersDbContext context)
    : IRequestHandler<GetSpecializedMethodsAndTestsQuery, PartialQueryResult<SpecializedMethodOrTestDto>>
{
    public async Task<PartialQueryResult<SpecializedMethodOrTestDto>> Handle(
        GetSpecializedMethodsAndTestsQuery request,
        CancellationToken cancellationToken)
    {
        var filteredItems = ApplyFilters(
                await GetSpecializedMethodsAndTestsAsync(cancellationToken).ConfigureAwait(false),
                request)
            .ToList();

        var items = ApplyDefaultSort(filteredItems)
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        return new PartialQueryResult<SpecializedMethodOrTestDto>(items, request.StartIndex, filteredItems.Count);
    }

    private async Task<List<SpecializedMethodOrTestDto>> GetSpecializedMethodsAndTestsAsync(CancellationToken cancellationToken)
    {
        var items = await GetSpecializedMethodsAsync(cancellationToken).ConfigureAwait(false);

        items.AddRange(await GetRuoTestsAsync(cancellationToken).ConfigureAwait(false));
        items.AddRange(await GetOptionalTestsAsync(cancellationToken).ConfigureAwait(false));

        return items;
    }

    private async Task<List<SpecializedMethodOrTestDto>> GetSpecializedMethodsAsync(CancellationToken cancellationToken)
    {
        var methods = await context.Methods
            .Where(method => method.IsRuo || method.IsSpecialMethod)
            .Select(method => new
            {
                method.Id,
                method.FullMethodName,
                method.IsRuo,
                method.IsSpecializedMethodActivated
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return methods
            .Select(method => new SpecializedMethodOrTestDto
            {
                Id = HashCode.Combine(method.Id, 0),
                MethodId = method.Id,
                MethodTestId = null,
                Name = method.FullMethodName,
                Type = method.IsRuo
                    ? SpecializedMethodOrTestType.RUOMethod
                    : SpecializedMethodOrTestType.OptionalMethod,
                IsSpecializedMethodOrTestActivated = method.IsSpecializedMethodActivated
            })
            .ToList();
    }

    private async Task<List<SpecializedMethodOrTestDto>> GetRuoTestsAsync(CancellationToken cancellationToken)
    {
        var tests = await context.MethodTests
            .Where(test => test.IsRuo && !test.Method.IsRuo)
            .Select(test => new
            {
                test.Id,
                test.MethodId,
                test.FullTestName,
                test.IsSpecializedTestActivated
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tests
            .Select(test => new SpecializedMethodOrTestDto
            {
                Id = HashCode.Combine(test.MethodId, test.Id),
                MethodId = test.MethodId,
                MethodTestId = test.Id,
                Type = SpecializedMethodOrTestType.RUOTest,
                Name = test.FullTestName,
                IsSpecializedMethodOrTestActivated = test.IsSpecializedTestActivated
            })
            .ToList();
    }

    private async Task<List<SpecializedMethodOrTestDto>> GetOptionalTestsAsync(CancellationToken cancellationToken)
    {
        var data = await context.MethodTests
            .Where(test => !string.IsNullOrEmpty(test.SpecialMethodName))
            .Select(test => new
            {
                TestId = test.Id,
                test.MethodId,
                test.Method.FullMethodName,
                test.SpecialMethodName,
                test.IsSpecializedTestActivated
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return data
            .OrderBy(item => item.FullMethodName)
            .ThenBy(item => item.SpecialMethodName)
            .DistinctBy(item => item.FullMethodName)
            .Select(item => new SpecializedMethodOrTestDto
            {
                Id = HashCode.Combine(item.MethodId, item.TestId),
                MethodId = item.MethodId,
                MethodTestId = null,
                Type = SpecializedMethodOrTestType.OptionalTest,
                Name = $"{item.FullMethodName} ({item.SpecialMethodName})",
                IsSpecializedMethodOrTestActivated = item.IsSpecializedTestActivated
            })
            .ToList();
    }

    private static IEnumerable<SpecializedMethodOrTestDto> ApplyFilters(
        IEnumerable<SpecializedMethodOrTestDto> items,
        GetSpecializedMethodsAndTestsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            items = ApplyNameFilter(items, request.Name);
        }

        if (request.Type.Count > 0)
        {
            items = ApplyTypeFilter(items, request.Type);
        }

        return items;
    }

    private static IEnumerable<SpecializedMethodOrTestDto> ApplyNameFilter(
        IEnumerable<SpecializedMethodOrTestDto> items,
        string nameFilter)
    {
        return items.Where(item => item.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<SpecializedMethodOrTestDto> ApplyTypeFilter(
        IEnumerable<SpecializedMethodOrTestDto> items,
        List<string> types)
    {
        var typeNames = types.ToHashSet(StringComparer.Ordinal);
        return items.Where(item => typeNames.Contains(item.Type.ToString()));
    }

    private static IOrderedEnumerable<SpecializedMethodOrTestDto> ApplyDefaultSort(
        IEnumerable<SpecializedMethodOrTestDto> items)
    {
        return items
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Name);
    }
}
