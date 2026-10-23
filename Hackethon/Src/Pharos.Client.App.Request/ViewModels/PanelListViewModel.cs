using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Request.ViewModels;

public sealed class PanelListViewModel(IEntityDataSource<Panel> dataSource, IExceptionHandlerService exceptionHandler) : VirtualGridViewModel<Panel>(dataSource, exceptionHandler)
{
}