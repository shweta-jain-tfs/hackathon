using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Maintenance.Models;

namespace Pharos.Client.App.Maintenance.Views;

public partial class StepDetailsView
{
    [Parameter] public ActivityStep ViewModel { get; set; } = null!;
}