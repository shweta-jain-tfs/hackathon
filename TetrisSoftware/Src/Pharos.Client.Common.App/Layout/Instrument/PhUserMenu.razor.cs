namespace Pharos.Client.Common.App.Layout.Instrument;

public partial class PhUserMenu : IDisposable
{
    private const int _infolableThreshold = 25;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public string? Name { get; set; }

    private string? UserNameHoverLabel => !string.IsNullOrEmpty(Name) && Name.Length >= _infolableThreshold ? Name : null;

    private string LogoutUrl { get => $"{PageRoute.Logout}?returnUrl={Uri.EscapeDataString(field ?? "")}"; set; }
    private static string ProfileUri => PageRoute.UserAccount;

    protected override Task OnInitializedAsync()
    {
        LogoutUrl = Navigation.ToBaseRelativePath(Navigation.Uri);
        Navigation.LocationChanged += OnLocationChanged;
        return Task.CompletedTask;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        LogoutUrl = Navigation.ToBaseRelativePath(e.Location);
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
