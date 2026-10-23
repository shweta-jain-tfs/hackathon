using Pharos.Common.Web.Components.DataGrid.Selection;

namespace Pharos.Client.App.Request.ViewModels;

public class TestOrderViewModel : ViewModelItem<RequestTest>, IEquatable<TestOrderViewModel>
{
    private readonly IStringLocalizer<Translations> _loc;

    public TestOrderViewModel(RequestTest model, IStringLocalizer<Translations> loc, bool isModified = false) : base(model)
    {
        _loc = loc;
        if (isModified)
        {
            SetAsModified();
        }
    }

    public int TestSortOrder
    {
        get => Model.TestSortOrder;
        set => SetProperty(Model.TestSortOrder, value, v => Model.TestSortOrder = v);
    }

    public int NumberOfReplicates
    {
        get => Model.NumberOfReplicates;
        set => SetProperty(Model.NumberOfReplicates, value, v => Model.NumberOfReplicates = v);
    }

    public int InstrumentDilutionFactor
    {
        get => Model.InstrumentDilutionFactor;
        set => SetProperty(Model.InstrumentDilutionFactor, value, v => Model.InstrumentDilutionFactor = v);
    }

    public bool IsActive
    {
        get => Model.IsActive;
        set => SetProperty(Model.IsActive, value, v => Model.IsActive = v);
    }

    public string TestName => Model.TestName;

    public string LabTestName => Model.LabTestName;

    public TestOrderState State => Model.State;

    public string MethodName => Model.MethodName;

    public int? AnalyticalRunId => Model.AnalyticalRunId;

    [SelectionKey]
    public Guid TestOrderId => Model.Uid;

    public string StatusText => IsActive ? State.ToLocalizedString("") : _loc["RequestDetailPage_TestListView_OnHold"];

    public string Concentration => Model.Concentration;

    public string Class => Model.Class;

    public string MeanCutOff => Model.MeanCutOff;

    public string MeanQuotientAsText => Model.MeanQuotientAsText;

    public string FullTestName => Model.FullTestName;

    public DateTimeOffset? MeasurementDateTime => Model.MeasurementDateTime;

    public string InstrumentName => Model.InstrumentName;

    public string FullMethodName => Model.FullMethodName;

    public bool IsNotFinished => State is TestOrderState.NotStarted or TestOrderState.Scheduled or TestOrderState.UnableToStart or TestOrderState.Started or TestOrderState.Pipetted;

    public IEnumerable<RequestReflexDefinition>? ReflexDefinitions => Model.ReflexDefinitions;

    public bool ReportConcentration => Model.ReportConcentration;

    public bool ReportCutOff => Model.ReportCutOff;

    public bool ReportQuotient => Model.ReportQuotient;

    public bool ReportClass => Model.ReportClass;

    public bool Equals(TestOrderViewModel? other)
    {
        return Model.Equals(other?.Model);
    }

    public override bool Equals(object? obj)
    {
        return obj is TestOrderViewModel other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Model.GetHashCode();
    }

    /// <summary>
    /// Marks test order for reset. When saved, the server will delete and recreate the test order with a new UID.
    /// Status is set to NotStarted and result data is cleared locally for immediate UI feedback.
    /// All test properties (TestName, MethodName, Replicates, ReflexDefinitions, etc.) will be preserved by the server.
    /// </summary>
    public void ResetForClient()
    {
        // Mark as reset so server knows to delete and recreate with new UID
        Model.IsReset = true;

        Model.State = TestOrderState.NotStarted;

        // Clear result data for immediate UI feedback
        Model.Concentration = string.Empty;
        Model.Class = string.Empty;
        Model.MeanCutOff = string.Empty;
        Model.MeanQuotientAsText = string.Empty;
        Model.MeasurementDateTime = null;
        Model.InstrumentName = string.Empty;
        Model.AnalyticalRunId = null;

        // Mark as modified and notify parent ViewModel to enable save button
        SetAsModified();

        // Test properties and ReflexDefinitions remain unchanged - they will be preserved by the server
    }
}