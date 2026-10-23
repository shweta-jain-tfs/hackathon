using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Queries.GetSpecializedMethodsAndTests;

namespace Pharos.Server.Parameters.Application.Commands.SpecializedMethodsAndTests;

public class SaveSpecializedMethodsAndTestsCommand : IRequest
{
    public List<SaveSpecializedRequestDto> Items { get; set; } = [];
}

public class SaveSpecializedRequestDto
{
    public int MethodId { get; set; }
    public int? MethodTestId { get; set; }
    public SpecializedMethodOrTestType Type { get; set; }
    public bool IsSpecializedMethodOrTestActivated { get; set; }
}

internal sealed class SaveSpecializedMethodsAndTestsAvailabilityCommandHandler(IParametersDbContext context)
    : IRequestHandler<SaveSpecializedMethodsAndTestsCommand>
{
    public async Task Handle(SaveSpecializedMethodsAndTestsCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return;
        }

        await UpdateMethodsAsync(request.Items, cancellationToken).ConfigureAwait(false);
        await UpdateRuoTestsAsync(request.Items, cancellationToken).ConfigureAwait(false);
        await UpdateOptionalTestsAsync(request.Items, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateMethodsAsync(List<SaveSpecializedRequestDto> items, CancellationToken cancellationToken)
    {
        var updates = items
            .Where(IsMethodUpdate)
            .GroupBy(item => item.MethodId)
            .ToDictionary(group => group.Key, group => group.Last().IsSpecializedMethodOrTestActivated);

        if (updates.Count == 0)
        {
            return;
        }

        var methodIds = updates.Keys.ToHashSet();
        var methods = await context.Methods
            .Where(method => methodIds.Contains(method.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var methodTests = await context.MethodTests
            .Include(methodTest => methodTest.Method)
            .Where(methodTest => methodIds.Contains(methodTest.MethodId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var method in methods)
        {
            var isAvailable = updates[method.Id];
            method.IsSpecializedMethodActivated = isAvailable;
            UpdateMethodEnabled(method, isAvailable);
        }

        foreach (var methodTest in methodTests)
        {
            UpdateMethodTestActive(methodTest, updates[methodTest.MethodId]);
        }
    }

    private async Task UpdateRuoTestsAsync(List<SaveSpecializedRequestDto> items, CancellationToken cancellationToken)
    {
        var updates = items
            .Where(item => item.Type == SpecializedMethodOrTestType.RUOTest && item.MethodTestId.HasValue)
            .GroupBy(item => item.MethodTestId!.Value)
            .ToDictionary(group => group.Key, group => group.Last().IsSpecializedMethodOrTestActivated);

        if (updates.Count == 0)
        {
            return;
        }

        var methodTestIds = updates.Keys.ToHashSet();
        var methodTests = await context.MethodTests
            .Include(methodTest => methodTest.Method)
            .Where(methodTest => methodTestIds.Contains(methodTest.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var methodTest in methodTests)
        {
            var isAvailable = updates[methodTest.Id];
            methodTest.IsSpecializedTestActivated = isAvailable;
            UpdateMethodTestActive(methodTest, isAvailable);
        }
    }

    private async Task UpdateOptionalTestsAsync(List<SaveSpecializedRequestDto> items, CancellationToken cancellationToken)
    {
        var updates = items
            .Where(item => item.Type == SpecializedMethodOrTestType.OptionalTest)
            .GroupBy(item => item.MethodId)
            .ToDictionary(group => group.Key, group => group.Last().IsSpecializedMethodOrTestActivated);

        if (updates.Count == 0)
        {
            return;
        }

        var methodIds = updates.Keys.ToHashSet();
        var methodTests = await context.MethodTests
            .Include(methodTest => methodTest.Method)
            .Where(methodTest => methodIds.Contains(methodTest.MethodId)
                && !string.IsNullOrEmpty(methodTest.SpecialMethodName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var methodTest in methodTests)
        {
            var isAvailable = updates[methodTest.MethodId];
            methodTest.IsSpecializedTestActivated = isAvailable;
            UpdateMethodTestActive(methodTest, isAvailable);
        }
    }

    private static void UpdateMethodEnabled(Method method, bool enabled)
    {
        if (method.Enabled == enabled)
        {
            return;
        }

        method.Enabled = enabled;
        method.AddDomainEvent(new MethodEnabledEvent(method.MethodName, method.Enabled));
    }

    private static void UpdateMethodTestActive(MethodTest methodTest, bool active)
    {
        if (methodTest.Active == active)
        {
            return;
        }

        methodTest.Active = active;
        methodTest.AddDomainEvent(new MethodTestActivatedEvent(methodTest.Method.MethodName, methodTest.TestName, methodTest.Active));
    }

    private static bool IsMethodUpdate(SaveSpecializedRequestDto item)
    {
        return item.Type is SpecializedMethodOrTestType.RUOMethod or SpecializedMethodOrTestType.OptionalMethod;
    }
}
