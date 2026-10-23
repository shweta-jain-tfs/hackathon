namespace Pharos.Common.Web.Components.StepGuide;

/// <summary>
/// A step to use within a <see cref="PhStepGuide"/> component in <see cref="StepGuideMode.Wizard"/> mode.
/// Shows a placeholder text instead the step content if the step is not active.
/// </summary>
public partial class PhStepGuideWizardStep
{
    /// <summary>
    /// Step content to show when the step is active.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    /// Show the step content and fill the step symbol.
    /// </summary>
    /// <remarks>
    /// If a step is filled, all previous steps are also shown as filled without gaps in the timeline.
    /// </remarks>
    [Parameter] public bool Active { get; set; }

    /// <summary>
    /// Indicates that the step is the current step.
    /// </summary>
    [Parameter] public bool Current { get; set; }

    /// <summary>
    /// Text to show instead of the step content when the step is not active.
    /// </summary>
    [Parameter] public string? PlaceholderText { get; set; }
}