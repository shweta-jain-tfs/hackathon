using Microsoft.Extensions.Localization;

namespace Pharos.Server.Settings.Application.Interfaces;

internal interface ISettingsStringLocalizer : IStringLocalizer
{
    Type ResourceType { get; }
}
