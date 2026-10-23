using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Components.Popover;

namespace Pharos.Common.Web.Components.DropdownButton;

/// <summary>
/// A button that opens a dropdown menu using a <see cref="PhPopover"/>.
/// Renders a text button or an icon-only button, depending on <see cref="DropdownButtonType"/>.
/// </summary>
public partial class PhDropdownButton : IDisposable
{
    /// <summary>
    /// Determines the visual style and button type.
    /// Non-icon types render a <see cref="PhButton"/>; <see cref="DropdownButtonType.Icon"/> renders a <see cref="PhIconButton"/>.
    /// </summary>
    [Parameter] public DropdownButtonType DropdownButtonType { get; set; } = DropdownButtonType.Secondary;

    /// <summary>
    /// Disables the button and prevents interaction.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Optional custom icon.
    /// For non-icon variants, this is the leading icon; for icon mode, it defines the icon of the <see cref="PhIconButton"/>.
    /// </summary>
    [Parameter] public IconType? IconType { get; set; }

    /// <summary>
    /// The content displayed inside the dropdown panel.
    /// Should consist of <see cref="PhDropdownButtonItem"/> or <see cref="PhDropdownButtonItemLink"/> components.
    /// Clicking an item will close the dropdown.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The button text (ignored for icon-only mode).
    /// </summary>
    [Parameter] public string? Text { get; set; }

    [Parameter] public bool UseInlineFlex { get; set; } = true;

    private bool _isIconButton;
    private bool _isOpen;
    private PhPopover? _popover;

    private string? PopoverCssClasses => UseInlineFlex ? "inline-flex" : null;

    private ButtonType ButtonType { get; set; }

    protected override Task OnParametersSetAsync()
    {
        switch (DropdownButtonType)
        {
            case DropdownButtonType.Primary:
            case DropdownButtonType.Secondary:
            case DropdownButtonType.Text:
                _isIconButton = false;
                ButtonType = DropdownButtonType switch
                {
                    DropdownButtonType.Primary => ButtonType.Primary,
                    DropdownButtonType.Secondary => ButtonType.Secondary,
                    DropdownButtonType.Text => ButtonType.Text,
                    _ => ButtonType.Secondary
                };
                break;
            case DropdownButtonType.Icon:
                _isIconButton = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.CompletedTask;
    }

    private void SetOpenState(bool isOpen)
    {
        _isOpen = isOpen;
    }

    private void ClosePanel()
    {
        _popover?.Hide();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}