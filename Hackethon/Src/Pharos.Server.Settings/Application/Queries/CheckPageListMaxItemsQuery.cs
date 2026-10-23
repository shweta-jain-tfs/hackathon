using MediatR;
using Pharos.Server.Settings.Application.Validation;

namespace Pharos.Server.Settings.Application.Queries;

public record CheckPageListMaxItemsQuery(string PageListKey, string RootPageLabel) : IRequest<CheckPageListMaxItemsResult>;

internal sealed class CheckPageListMaxItemsQueryHandler(IPageListMaxItemsValidator validator) 
    : IRequestHandler<CheckPageListMaxItemsQuery, CheckPageListMaxItemsResult>
{
    public async Task<CheckPageListMaxItemsResult> Handle(CheckPageListMaxItemsQuery request, CancellationToken cancellationToken)
    {
        // Delegate to the shared validator
        var validationResult = await validator.ValidateCanAddAsync(
            request.PageListKey,
            request.RootPageLabel,
            cancellationToken).ConfigureAwait(false);

        return new CheckPageListMaxItemsResult(
            validationResult.IsValid,
            validationResult.MaxItems,
            validationResult.CurrentCount);
    }
}

public record CheckPageListMaxItemsResult(bool CanAdd, int? MaxItems, int CurrentCount);

