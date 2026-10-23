using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.StepGuide;
/// <summary>
/// A step to use within a <see cref="PhStepGuide"/> component in <see cref="StepGuideMode.Navigation"/> mode.
/// Is clickable and can be used to navigate to this specific step in the guide (handled outside this component).
/// </summary>
public partial class PhStepGuideNavStep
{
    /// <summary>
    /// Step content. Intended to contain the name of the step.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    /// Indicates that the step symbol is filled, e.g. a completed step.
    /// </summary>
    /// <remarks>
    /// If a step is filled, all previous steps are also shown as filled without gaps in the timeline.
    /// </remarks>
    [Parameter] public bool Filled { get; set; }

    /// <summary>
    /// Indicates that the step is the current step in the guide.
    /// This highlights the step.
    /// </summary>
    [Parameter] public bool Current { get; set; }

    /// <summary>
    /// Disable the step, making it unclickable.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Event callback that is invoked when the step is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private string CssClasses => new CssBuilder("ph-step-guide__step")
        .AddClass("ph-step-guide__step--filled", Filled)
        .AddClass("ph-step-guide__step--disabled", Disabled || !OnClick.HasDelegate)
        .Build();

    private string? AriaCurrent => Current ? "step" : null;

    private async Task HandleClick(MouseEventArgs args)
    {
        if (OnClick.HasDelegate && !Disabled)
        {
            await OnClick.InvokeAsync(args);
        }
    }
}
