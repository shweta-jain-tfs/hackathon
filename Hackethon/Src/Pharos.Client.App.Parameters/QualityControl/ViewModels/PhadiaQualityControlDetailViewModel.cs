using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class PhadiaQualityControlDetailViewModel : ViewModelItem<PhadiaQualityControlDetail>
{
    public PhadiaQualityControlDetailViewModel(PhadiaQualityControlDetail model, List<TubeDefinition> tubeDefinitions) : base(model)
    {
        TubeDefinitions = tubeDefinitions.Select(x => new DropDownOption<int>
        {
            Text = x.Name,
            Value = x.Id
        }).ToList();
    }

    public List<DropDownOption<int>> TubeDefinitions { get; }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, v => Model.Name = v);
    }

    public int TubeTypeId
    {
        get => Model.TubeTypeId;
        set => SetProperty(Model.TubeTypeId, value, v => Model.TubeTypeId = v);
    }

    public bool IsQcInTube => Model.IsQcInTube;

    public List<PhadiaQualityControlTest> Tests => Model.PhadiaQualityControlTests.ToList();

    public void UpdateTest(EditQualityControlTestViewModel viewModel)
    {
        //var existingTest = Model.PhadiaQualityControlTests.FirstOrDefault(t => t.TestId == viewModel.TestId);
        var existingTest = Model.PhadiaQualityControlTests.FirstOrDefault();
        if (existingTest == null)
        {
            return;
        }

        existingTest.Replicates = viewModel.Replicates;
        existingTest.ReplicateCvLimit = viewModel.ReplicateCvLimit;
        existingTest.PreDilutionFactor = viewModel.PreDilutionFactor;
        existingTest.InstrumentDilutionFactor = viewModel.InstrumentDilutionFactor;
        existingTest.ApplyAcceptanceRange = viewModel.ApplyAcceptanceRange;
        existingTest.UseAcceptanceRange = viewModel.UseAcceptanceRange;
        existingTest.MaxCv = viewModel.MaxCv;
        existingTest.NumberOfSd = viewModel.NumberOfSd;
        existingTest.UseLotQualification = viewModel.UseLotQualification;
        existingTest.NumberOfTests = viewModel.NumberOfTests;
        existingTest.QualityControlType = viewModel.QualityControlType;

        SetAsModified();
    }
}