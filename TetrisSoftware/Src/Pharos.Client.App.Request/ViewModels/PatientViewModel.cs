using Pharos.Client.App.Request.Models;

namespace Pharos.Client.App.Request.ViewModels;

public sealed class PatientViewModel : ViewModelItem<Patient>
{
    private readonly ISearchPatientDataSource _patientDataSource;

    public PatientViewModel(Patient model, ISearchPatientDataSource patientDataSource) : base(model)
    {
        _patientDataSource = patientDataSource;
    }

    public string PatientId
    {
        get => Model.PatientId;
        set => SetProperty(Model.PatientId, value, v => Model.PatientId = v);
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, v => Model.Name = v);
    }

    public Gender Gender
    {
        get => Model.Gender;
        set => SetProperty(Model.Gender, value, v => Model.Gender = v);
    }

    public DateOnly? BirthDate
    {
        get => Model.BirthDate;

        set => SetProperty(Model.BirthDate, value, v => Model.BirthDate = v);
    }

    public string? PatientNumber
    {
        get => Model.PatientNumber;
        set => SetProperty(Model.PatientNumber, value, v => Model.PatientNumber = v);
    }

    public string? MedicalRecordNumber
    {
        get => Model.MedicalRecordNumber;
        set => SetProperty(Model.MedicalRecordNumber, value, v => Model.MedicalRecordNumber = v);
    }

    public string? Address
    {
        get => Model.Address;
        set => SetProperty(Model.Address, value, v => Model.Address = v);
    }

    public string? ZipCode
    {
        get => Model.ZipCode;
        set => SetProperty(Model.ZipCode, value, v => Model.ZipCode = v);
    }

    public string? Diagnosis
    {
        get => Model.Diagnosis;
        set => SetProperty(Model.Diagnosis, value, v => Model.Diagnosis = v);
    }

    public string? Comment
    {
        get => Model.Comment;
        set => SetProperty(Model.Comment, value, v => Model.Comment = v);
    }

    private async Task LoadPatientFromId(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId))
        {
            return;
        }

        var filters = new List<DataSourceFilter>();
        var filter = new DataSourceFilter(nameof(PatientId), FilterOperator.EqualsTo, patientId);
        filters.Add(filter);

        var dataSourceRequest = new DataSourceRequest
        {
            Count = 1,
            Filters = filters
        };

        var patientDataSourceResult = await _patientDataSource.GetAsync(dataSourceRequest);

        if (patientDataSourceResult.Items.Count != 0)
        {
            var patient = patientDataSourceResult.Items[0];

            CopyValues(patient);
        }
    }

    private void CopyValues(Patient patient)
    {
        Model.PatientId = patient.PatientId;
        Model.Name = patient.Name;
        Model.Gender = patient.Gender;
        Model.BirthDate = patient.BirthDate;
        Model.PatientNumber = patient.PatientNumber;
        Model.MedicalRecordNumber = patient.MedicalRecordNumber;
        Model.Address = patient.Address;
        Model.ZipCode = patient.ZipCode;
        Model.Diagnosis = patient.Diagnosis;
        Model.Comment = patient.Comment;
        
        OnPropertyChanged(string.Empty);
    }

    public async Task PatientIdChanged()
    {
        await LoadPatientFromId(PatientId);
    }
}