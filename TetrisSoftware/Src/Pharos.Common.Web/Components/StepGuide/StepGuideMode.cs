namespace Pharos.Common.Web.Components.StepGuide;

public enum StepGuideMode
{
    /// <summary>
    /// A sequential guide where each step must be completed before moving to the next.
    /// </summary>
    Wizard,

    /// <summary>
    /// A navigation guide where steps can be clicked to navigate directly to them.
    /// </summary>
    Navigation,
}