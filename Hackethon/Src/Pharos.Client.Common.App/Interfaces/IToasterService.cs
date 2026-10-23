using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.Common.App.Interfaces;

public interface IToasterService
{
    void ShowToast(ToastType type, string text);
}