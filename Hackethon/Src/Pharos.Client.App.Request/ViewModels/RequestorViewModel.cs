using Pharos.Client.App.Request.Models;

namespace Pharos.Client.App.Request.ViewModels;

public sealed class RequestorViewModel : ViewModelItem<Requestor>
{
    private readonly IRequestorDataSource _requestorDataSource;

    public RequestorViewModel(Requestor model, IRequestorDataSource requestorDataSource) : base(model)
    {
        _requestorDataSource = requestorDataSource;
    }

    public string RequestorId
    {
        get => Model.RequestorId;
        private set => SetProperty(Model.RequestorId, value, v => Model.RequestorId = v);
    }

    public string Name
    {
        get => Model.Name;
        private set => SetProperty(Model.Name, value, v => Model.Name = v);
    }

    public bool ReflexEnabled
    {
        get => Model.ReflexEnabled;
        private set => SetProperty(Model.ReflexEnabled, value, v => Model.ReflexEnabled = v);
    }

    public async Task<Tuple<IEnumerable<Requestor>, int>> GetRequestors(string searchCriteria)
    {
        return await _requestorDataSource.GetRequestors(searchCriteria);
    }

    public void RequestorSelected(Requestor? requestor)
    {
        requestor ??= _noRequestor;

        RequestorId = requestor.RequestorId;
        Name = requestor.Name;
        ReflexEnabled = requestor.ReflexEnabled;
        Model.Id = requestor.Id;

        OnPropertyChanged(string.Empty);
    }

    private static readonly Requestor _noRequestor = new() {Id = 0, RequestorId = string.Empty, Name = string.Empty};

    public void ClearSelection()
    {
        RequestorSelected(_noRequestor);
    }
}