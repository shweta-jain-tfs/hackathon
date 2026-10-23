namespace Pharos.Common.Web.Components.Modal;

/// <summary>
/// Represents the options for configuring a modal component.
/// </summary>
public class ModalOptions
{
    /// <summary>
    /// Automatically gets or sets the unique identifier for the modal.
    /// </summary>
    internal Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the title of the modal.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the width of the modal.
    /// </summary>
    public ModalSize ModalWidth { get; set; } = ModalSize.Medium;

    /// <summary>
    /// Gets or sets the height of the modal.
    /// </summary>
    public ModalSize ModalHeight { get; set; } = ModalSize.Medium;

    /// <summary>
    /// Gets or sets the status of the modal.
    /// This changes to color of the title bar.
    /// </summary>
    public ModalStatus Status { get; set; }

    /// <summary>
    /// Show or hide the close button in the modal.
    /// </summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Gets or sets optional custom content rendered in the modal header, to the left of the close button.
    /// </summary>
    public RenderFragment? HeaderActions { get; set; }

    /// <summary>
    /// Set if the modal should close on navigation.
    /// Default is true.
    /// </summary>
    public bool CloseOnNavigation { get; set; } = true;

    internal bool CenterText { get; set; }

    /// <summary>
    /// Gets or sets an optional tag for grouping and managing related modals.
    /// Use this to cancel multiple modals of the same category (e.g., "BarcodeDetails").
    /// </summary>
    public string? Tag { get; set; }
}

/// <summary>
/// Represents the possible statuses for a modal component.
/// </summary>
public enum ModalStatus
{
    /// <summary>
    /// The default status.
    /// </summary>
    Default,

    /// <summary>
    /// The informational status.
    /// </summary>
    Info,

    /// <summary>
    /// The warning status.
    /// </summary>
    Warning,

    /// <summary>
    /// The alarm status.
    /// </summary>
    Alarm
}
