using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.StepGuide;

/// <summary>
/// A sequence of steps that are filled one by one, like a subway line station map.
/// Use <see cref="PhStepGuideWizardStep"/> for steps in <see cref="StepGuideMode.Wizard"/> mode,
/// use <see cref="PhStepGuideNavStep"/> for steps in <see cref="StepGuideMode.Navigation"/> mode.
/// <see cref="PhStepGuideStep"/> can also be used, but it does not provide any special behavior for the step content.
/// </summary>
public partial class PhStepGuide
{
    /// <summary>
    /// Steps to display in the guide.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    /// Align the step symbol to an input field located in the bottom of the step (e.g. a text box under a label)
    /// instead of centering the step symbol in the middle of the step content.
    /// </summary>
    [Parameter] public bool AlignStepSymbolToInputField { get; set; }

    /// <summary>
    /// Mode of the step guide, either <see cref="StepGuideMode.Wizard"/> or <see cref="StepGuideMode.Navigation"/>.
    /// </summary>
    [Parameter] public StepGuideMode Mode { get; set; } = StepGuideMode.Wizard;

    /// <summary>
    /// Mute the color of the step guide, making it appear disabled.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    private string CssClasses => new CssBuilder("ph-step-guide")
        .AddClass("ph-step-guide--align-to-input-field", AlignStepSymbolToInputField)
        .AddClass("ph-step-guide--disabled", Disabled)
        .AddClass("ph-step-guide--wizard", Mode == StepGuideMode.Wizard)
        .AddClass("ph-step-guide--navigation", Mode == StepGuideMode.Navigation)
        .Build();

}
