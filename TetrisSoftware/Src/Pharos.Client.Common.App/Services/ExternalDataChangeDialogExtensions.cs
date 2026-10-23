using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.Common.App.Services;

public enum ExternalDataChangeKind
{
    Updated,
    Deleted
}

public static class ExternalDataChangeDialogExtensions
{
    public static IModalReference ShowExternalDataChangeDialog(
        this IModalService modal,
        IStringLocalizer<Translations> loc,
        ExternalDataChangeKind kind,
        string titleEntityName,
        string? changedBy,
        string message,
        string okButton)
    {
        var title = GetTitle(loc, kind, titleEntityName, changedBy);

        return modal.ShowInformation(
            message,
            okButton,
            new ModalOptions
            {
                Title = title,
                ModalWidth = ModalSize.Small,
                ModalHeight = ModalSize.Small,
                Status = ModalStatus.Info,
                ShowCloseButton = false,
                CloseOnNavigation = false
            });
    }

    private static string GetTitle(
        IStringLocalizer<Translations> loc,
        ExternalDataChangeKind kind,
        string titleEntityName,
        string? changedBy)
    {
        if (string.IsNullOrWhiteSpace(changedBy))
        {
            return kind switch
            {
                ExternalDataChangeKind.Deleted => string.Format(loc["ExternalDataChangeDialog_DeletedTitleWithoutUser"], titleEntityName),
                _ => string.Format(loc["ExternalDataChangeDialog_UpdatedTitleWithoutUser"], titleEntityName)
            };
        }

        return kind switch
        {
            ExternalDataChangeKind.Deleted => string.Format(loc["ExternalDataChangeDialog_DeletedTitle"], titleEntityName, changedBy),
            _ => string.Format(loc["ExternalDataChangeDialog_UpdatedTitle"], titleEntityName, changedBy)
        };
    }

}
