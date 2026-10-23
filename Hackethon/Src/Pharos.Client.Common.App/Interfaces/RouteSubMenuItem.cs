namespace Pharos.Client.Common.App.Interfaces;

public sealed record RouteSubMenuItem(
    string Uri,
    string Text,
    string? Permission = null);