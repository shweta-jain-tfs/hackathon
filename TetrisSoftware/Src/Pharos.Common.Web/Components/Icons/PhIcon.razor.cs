using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Icons;

/// <summary>
/// Renders a monochrome SVG icon using the current color unless overridden by the <see cref="Color"/> parameter.
/// </summary>
public partial class PhIcon
{
    [Parameter, EditorRequired] public IconType IconType { get; set; } = IconType.Warning;
    [Parameter, EditorRequired] public IconSize? Size { get; set; }
    [Parameter] public IconColor? Color { get; set; }
    [Parameter] public string? CssClasses { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private Type? _iconComponent;

    protected string CssClassesColor =>
        new CssBuilder("")
            .AddClass("ph-icon--color-alarm", Color == IconColor.Alarm)
            .AddClass("ph-icon--color-success", Color == IconColor.Success)
            .AddClass("ph-icon--color-warning", Color == IconColor.Warning)
            .Build();

    protected string CssClassesSize =>
        new CssBuilder("")
            .AddClass("ph-icon--size-xs", Size == IconSize.Xs)
            .AddClass("ph-icon--size-sm", Size == IconSize.Sm)
            .AddClass("ph-icon--size-md", Size == IconSize.Md)
            .AddClass("ph-icon--size-lg", Size == IconSize.Lg)
            .AddClass("ph-icon--size-xl", Size == IconSize.Xl)
            .AddClass("ph-icon--size-xxl", Size == IconSize.Xxl)
            .Build();

    protected override void OnParametersSet()
    {
        _iconComponent = Type.GetType($"{typeof(PhIcon).Namespace}.Svg.{IconType}");
    }
}