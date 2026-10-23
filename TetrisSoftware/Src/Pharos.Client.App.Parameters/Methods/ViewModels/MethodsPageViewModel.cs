using Pharos.Client.App.Parameters.Methods.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public class MethodsPageViewModel(IMethodDataSource itemSource, IExceptionHandlerService exceptionHandler) : VirtualGridViewModel<Method>(itemSource, exceptionHandler)
{
}