namespace Pharos.Common.Web.Components.Modal;

public interface IModalReference
{
    Guid Id { get; }
    Task<ModalResult> Result { get; }

    event EventHandler Close;
}