using MediatR;

namespace Pharos.Server.Application.Settings;

/// <summary>
/// Generic event that is published when a settings group is changed.
/// </summary>
/// <typeparam name="TSettingsGroup">A settings group class.</typeparam>
/// <remarks>
/// To use this event, create a notification handler for a specific settings group.
/// </remarks>
public record SettingsChangedEvent<TSettingsGroup>(TSettingsGroup OldValue, TSettingsGroup NewValue) : INotification where TSettingsGroup : class;
