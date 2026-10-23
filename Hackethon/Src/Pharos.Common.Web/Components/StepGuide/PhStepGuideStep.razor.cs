using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.StepGuide;

/// <summary>
/// Used inside <see cref="PhStepGuide"/> to represent a step in the guide.
/// </summary>
public partial class PhStepGuideStep
{
    /// <summary>
    /// Step content
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
    /// </summary>
    [Parameter] public bool Current { get; set; }

    private string CssClasses => new CssBuilder("ph-step-guide__step")
        .AddClass("ph-step-guide__step--filled", Filled)
        .Build();

    private string? AriaCurrent => Current ? "step" : null;
}