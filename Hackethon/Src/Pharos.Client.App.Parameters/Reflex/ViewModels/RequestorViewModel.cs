using Pharos.Client.App.Parameters.Reflex.Models;

namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

public sealed class RequestorViewModel : ViewModelItem<Requestor>
{
    private readonly IRequestorDataSource _requestorDataSource;

    public RequestorViewModel(Requestor model, IRequestorDataSource requestorDataSource, bool canEditRequestorInfo, bool canViewRequestorInfo) : base(model)
    {
        _requestorDataSource = requestorDataSource;
        CanEditRequestorInfo = canEditRequestorInfo;
        CanViewRequestorInfo = canViewRequestorInfo;
    }

    public string RequestorId
    {
        get => Model.RequestorId;
        set => SetProperty(Model.RequestorId, value, v => Model.RequestorId = v);
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, v => Model.Name = v);
    }

    public bool CanEditRequestorInfo { get; }
    public bool CanViewRequestorInfo { get; }

    public async Task<Tuple<IEnumerable<Requestor>, int>> GetRequestors(string searchCriteria)
    {
        return await _requestorDataSource.GetRequestors(searchCriteria);
    }

    public void SelectRequestor(Requestor? requestor)
    {
        requestor ??= new Requestor { Id = 0, Name = string.Empty, RequestorId = string.Empty };

        RequestorId = requestor.RequestorId;
        Name = requestor.Name;
        Model.Id = requestor.Id;

        OnPropertyChanged(string.Empty);

        OnRequestorSelected(RequestorId);
    }

    public event EventHandler<string>? RequestorSelected;

    private void OnRequestorSelected(string requestorId)
    {
        RequestorSelected?.Invoke(this, requestorId);
    }

    public void ClearSelection()
    {
        SelectRequestor(null);
    }
}