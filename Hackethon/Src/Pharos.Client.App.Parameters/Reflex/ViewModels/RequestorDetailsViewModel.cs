namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

public class RequestorDetailsViewModel : ViewModelItem<RequestorDetails>
{
    public RequestorDetailsViewModel(RequestorDetails model) : base(model)
    {
    }

    public string RequestorId => Model.RequestorId;
    public string Name => Model.Name;
    public string Address => Model.Address;
    public string ZipCode => Model.ZipCode;
    public string Comment => Model.Comment;
    public string PhoneNumber => Model.PhoneNumber;
}