using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class LabQualityControlDetailViewModel : ViewModelItem<LabQualityControlDetail>
{
    private readonly IStringLocalizer<Translations> _loc;

    public LabQualityControlDetailViewModel(LabQualityControlDetail model, List<TubeDefinition> tubeDefinitions, List<QualityControlBarcodeFormatDetail> barcodeFormats, IStringLocalizer<Translations> loc) : base(model)
    {
        _loc = loc;

        TubeDefinitions = tubeDefinitions.Select(x => new DropDownOption<int>
        {
            Text = x.Name,
            Value = x.Id
        }).ToList();

        BarcodeFormats = [];
        SetBarcodeFormats(barcodeFormats);

        //Automatically use the first tube definition on new LabQualityControls
        if (IsNewEntity && TubeDefinitions.Count > 0)
        {
            TubeTypeId = TubeDefinitions.First().Value;
        }
    }

    public List<DropDownOption<int>> TubeDefinitions { get; }
    public List<DropDownOption<int?>> BarcodeFormats { get; }

    public bool IsNewEntity => Model.Id == 0;

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

    public string IdOnBarcode
    {
        get => Model.IdOnBarcode;
        set => SetProperty(Model.IdOnBarcode, value, v => Model.IdOnBarcode = v);
    }

    public int? BarcodeFormatId
    {
        get => Model.BarcodeFormatId;
        set => SetProperty(Model.BarcodeFormatId, value, v => Model.BarcodeFormatId = v);
    }

    public List<LabQualityControlTest> Tests => Model.LabQualityControlTests.ToList();

    public void AddTest(EditQualityControlTestViewModel viewModel)
    {
        if (Model.LabQualityControlTests.Any(x => x.TestId == viewModel.TestId))
        {
            return;
        }

        var test = new LabQualityControlTest
        {
            Id = 0,
            ApplyAcceptanceRange = viewModel.ApplyAcceptanceRange,
            LowerLimit = viewModel.LowerLimit,
            UpperLimit = viewModel.UpperLimit,
            NumberOfTests = viewModel.NumberOfTests,
            UseAcceptanceRange = viewModel.UseAcceptanceRange,
            Replicates = viewModel.Replicates,
            ReplicateCvLimit = viewModel.ReplicateCvLimit,
            UseLotQualification = viewModel.UseLotQualification,
            InstrumentDilutionFactor = viewModel.InstrumentDilutionFactor,
            MaxCv = viewModel.MaxCv,
            NumberOfSd = viewModel.NumberOfSd,
            PreDilutionFactor = viewModel.PreDilutionFactor,
            TestId = viewModel.TestId,
            MethodName = viewModel.MethodName,
            TestName = viewModel.TestName,
            QualityControlType = viewModel.QualityControlType
        };

        Model.LabQualityControlTests.Add(test);
        SetAsModified();
    }

    public void RemoveTests(HashSet<LabQualityControlTest> selectedItems)
    {
        foreach (var test in selectedItems)
        {
            Model.LabQualityControlTests.Remove(test);
        }

        SetAsModified();
    }

    public void UpdateTest(EditQualityControlTestViewModel viewModel)
    {
        var existingTest = Model.LabQualityControlTests.FirstOrDefault(t => t.TestId == viewModel.TestId);
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
        existingTest.UpperLimit = viewModel.UpperLimit;
        existingTest.LowerLimit = viewModel.LowerLimit;
        existingTest.UseLotQualification = viewModel.UseLotQualification;
        existingTest.NumberOfTests = viewModel.NumberOfTests;
        existingTest.QualityControlType = viewModel.QualityControlType;

        SetAsModified();
    }

    public void SetBarcodeFormats(List<QualityControlBarcodeFormatDetail> barcodeFormats)
    {
        BarcodeFormats.Clear();

        BarcodeFormats.AddRange(barcodeFormats.Select(x => new DropDownOption<int?>
        {
            Text = x.Name,
            Value = x.Id
        }));

        BarcodeFormats.Insert(0, new DropDownOption<int?> { Text = _loc["LabQualityControlDetailView_BarcodeFormat_None"], Value = null });
    }
}