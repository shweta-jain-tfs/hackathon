using System.ComponentModel;
using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Request.Views.RequestDetail;
/// <summary>
/// Represents the view for the patient details.
/// </summary>
public partial class PatientView : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the user information provider.
    /// </summary>
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;

    /// <summary>
    /// Gets or sets the string localizer for translations.
    /// </summary>
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sample ID.
    /// </summary>
    [Parameter] public string? SampleId { get; set; }

    /// <summary>
    /// Gets or sets the patient age at the time of sampling.
    /// </summary>
    [Parameter] public int? PatientAgeAtSampling { get; set; }

    /// <summary>
    /// Gets or sets the view model for the patient.
    /// </summary>
    [Parameter] public PatientViewModel ViewModel { get; set; } = default!;

    /// <summary>
    /// Gets or sets the parent view model (RequestDetailsViewModel) for validation context.
    /// </summary>
    [Parameter] public RequestDetailsViewModel? ParentViewModel { get; set; }

    private List<DropDownOption<Gender>>? _genderTypes = new();

    private bool NotAuthorized { get; set; } = true;

    private bool PatientInputsDisabled => !NotAuthorized && IsEmptyPatientId();

    /// <summary>
    /// Called when the component's parameters are set.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task OnParametersSetAsync()
    {
        NotAuthorized = !await IsAuthorized();
    }

    /// <summary>
    /// Determines whether the user is authorized to manage test orders.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the user is authorized.</returns>
    private async Task<bool> IsAuthorized()
    {
        return await AuthorizationService.IsAuthorized(Permissions.ManageTestOrders);
    }

    /// <summary>
    /// Called when the component is initialized.
    /// </summary>
    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        _genderTypes = Enum.GetValues<Gender>().Select(x => new DropDownOption<Gender>
        {
            Text = x.ToString(),
            Value = x
        }).ToList();
    }

    /// <summary>
    /// Determines whether the patient ID is empty.
    /// </summary>
    /// <returns><c>true</c> if the patient ID is empty; otherwise, <c>false</c>.</returns>
    private bool IsEmptyPatientId()
    {
        return string.IsNullOrEmpty(ViewModel.PatientId);
    }

    /// <summary>
    /// Determines whether the sample ID is empty.
    /// </summary>
    /// <returns><c>true</c> if the sample ID is empty; otherwise, <c>false</c>.</returns>
    private bool IsEmptySampleId()
    {
        return string.IsNullOrWhiteSpace(SampleId);
    }

    /// <summary>
    /// Handles the property changed event of the view model.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    /// <summary>
    /// Disposes the component.
    /// </summary>
    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
