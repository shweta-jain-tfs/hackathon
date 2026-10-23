using Microsoft.JSInterop;
using Pharos.Common.Web.Components.Icons;
using SettingsActionKey = Pharos.Client.Common.Models.ActionKey;

namespace Pharos.Client.App.Settings.Actions;

/// <summary>
/// Client-side behavior for a server-provided settings action button.
/// Add one implementation per action key and register it in <see cref="SettingsModule"/>.
/// </summary>
internal interface ISettingsAction
{
    SettingsActionKey ActionKey { get; }
    IconType? StartIconType { get; }
    IReadOnlyCollection<SettingsActionScript> RequiredScripts { get; }
    SettingsActionConfirmation? Confirmation { get; }
    Task<SettingsActionResult> ExecuteAsync(IReadOnlyDictionary<SettingsActionScript, IJSObjectReference> scripts);
}