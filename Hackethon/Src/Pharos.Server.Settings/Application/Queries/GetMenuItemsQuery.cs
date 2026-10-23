using MediatR;
using Pharos.Server.Application.Security;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Queries
{
    internal record GetMenuItemsQuery : IRequest<Dictionary<string, string>>;

    internal class GetMenuItemsQueryHandler : IRequestHandler<GetMenuItemsQuery, Dictionary<string, string>>
    {
        private readonly IAuthorizationService _authService;
        private readonly ISettingsStringLocalizer _localizer;

        public GetMenuItemsQueryHandler(IAuthorizationService authService, ISettingsStringLocalizer localizer)
        {
            _authService = authService;
            _localizer = localizer;
        }

        public async Task<Dictionary<string, string>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
        {
            var menuItems = await SettingsPagesProvider.GetMenuItemsAsync(_localizer, _authService.AuthorizeCurrentUserAsync);
            return menuItems;
        }
    }
}