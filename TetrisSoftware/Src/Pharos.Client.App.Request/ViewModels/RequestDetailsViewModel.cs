using System.ComponentModel;
using Pharos.Client.App.Request.Models;

namespace Pharos.Client.App.Request.ViewModels;

public sealed class RequestDetailsViewModel : ViewModelItem<RequestDetails>
{
    private bool _testDeleted;
    private int? _requiredVolume;
    private readonly IReflexDefinitionDataSource _reflexDefinitionDataSource;
    private readonly ISampleTubeDataSource _sampleTubeDataSource;
    private readonly IStringLocalizer<Translations> _loc;
    private readonly IVolumeCalculationService _volumeCalculation;
    private readonly List<TubeDefinition> _tubeDefinitions;

    public RequestDetailsViewModel(RequestDetails model,
        List<TubeDefinition> tubeDefinitions,
        int? requiredVolume,
        IRequestorDataSource requestorDataSource,
        ISearchPatientDataSource patientDataSource,
        UiSettings uiSettings,
        ISampleTubeDataSource sampleTubeDataSource,
        IReflexDefinitionDataSource reflexDefinitionDataSource,
        IStringLocalizer<Translations> loc,
        IVolumeCalculationService volumeCalculation) : base(model)
    {
        _reflexDefinitionDataSource = reflexDefinitionDataSource;
        _loc = loc;
        _sampleTubeDataSource = sampleTubeDataSource;
        _volumeCalculation = volumeCalculation;
        UiSettings = uiSettings;
        _tubeDefinitions = tubeDefinitions;

        TubeDefinitions = tubeDefinitions.Select(x => new DropDownOption<int?>
        {
            Text = x.Name,
            Value = x.Id
        }).ToList();

        model.Patient ??= new Patient();
        PatientViewModel = new PatientViewModel(model.Patient, patientDataSource);

        model.Requestor ??= new Requestor();
        RequestorViewModel = new RequestorViewModel(model.Requestor, requestorDataSource);

        model.TestOrders ??= new List<RequestTest>();
        TestOrders = [];
        foreach (var testOrder in model.TestOrders)
        {
            var vm = new TestOrderViewModel(testOrder, _loc);
            TestOrders.Add(vm);
        }

        SampleTubes = [];

        _requiredVolume = requiredVolume;

        RegisterListeners();
    }

    public UiSettings UiSettings { get; }

    public List<TestOrderViewModel> TestOrders { get; }

    public List<SampleTubeHistoryViewModel> SampleTubes { get; }

    public override bool IsModified => base.IsModified || PatientViewModel.IsModified || RequestorViewModel.IsModified || TestOrders.Any(x => x.IsModified) || _testDeleted;

    public bool HighPriority
    {
        get => Model.HighPriority;
        set => SetProperty(Model.HighPriority, value, v => Model.HighPriority = v);
    }

    public string? SampleId
    {
        get => Model.SampleId;
        set
        {
            var changed = SetProperty(Model.SampleId, value, v => Model.SampleId = v);
            if (changed && IsNewEntity)
            {
                RequestId = value;
                RefreshValidationForRequestId();
            }
        }
    }

    public event EventHandler? ValidationNeededForRequestId;

    private void RefreshValidationForRequestId()
    {
        ValidationNeededForRequestId?.Invoke(this, EventArgs.Empty);
    }

    public DateTimeOffset? SampleCollectionDate
    {
        get => Model.SampleCollectionDate;
        set => SetProperty(Model.SampleCollectionDate, value, v => Model.SampleCollectionDate = v);
    }

    public int PreDilutionFactor
    {
        get => Model.PreDilutionFactor;
        set => SetProperty(Model.PreDilutionFactor, value, v => Model.PreDilutionFactor = v);
    }

    public RequestState? RequestState
    {
        get => Model.RequestState;
        set => SetProperty(Model.RequestState, value, v => Model.RequestState = v);
    }

    public string? RequestStateText => Model.RequestState?.ToLocalizedString("Request_Enum");

    public int? SampleTubeType
    {
        get => Model.SampleTubeType;
        set => SetProperty(Model.SampleTubeType, value, v => Model.SampleTubeType = v);
    }

    public SampleVolumeCheck SampleVolumeCheck
    {
        get => Model.SampleVolumeCheck;
        set => SetProperty(Model.SampleVolumeCheck, value, v => Model.SampleVolumeCheck = v);
    }

    public string? RequestId
    {
        get => Model.RequestId;
        set => SetProperty(Model.RequestId, value, v => Model.RequestId = v);
    }

    public DateTimeOffset? RequestDate
    {
        get => Model.RequestDate;
        set => SetProperty(Model.RequestDate, value, v => Model.RequestDate = v);
    }

    public DateTimeOffset? LastModified => Model.LastModified;

    public bool CanEditRequest => RequestState == Common.Models.RequestState.NotStarted;

    public bool IsEmptySampleId => string.IsNullOrWhiteSpace(SampleId) || !SampleRequestIdValidator.IsValidSampleId(SampleId);

    public bool CanEditPredilution => UiSettings.RequestSettings?.AllowEditingOfPredilutionFactor ?? false;

    public int? PatientAgeAtSampling => Model.PatientAgeAtSampling;

    public List<DropDownOption<int?>> TubeDefinitions { get; }

    public PatientViewModel PatientViewModel { get; }

    public RequestorViewModel RequestorViewModel { get; }

    public bool IsNewEntity => Model.Id == 0;

    public string SampleIdDisplay => Model.SampleIdDisplay;

    public string RequiredVolumeDisplay => _requiredVolume.HasValue ? $"{_requiredVolume} µL" : string.Empty;

    public bool SelectedSampleTubeSupportsVolumeCalculation => _tubeDefinitions.FirstOrDefault(td => td.Id == SampleTubeType)?.SupportsVolumeCalculation ?? false;

    public IEnumerable<RequestReflexDefinition> GetFilteredReflexDefinitions(IEnumerable<RequestReflexDefinition>? testOrderReflexDefinitions)
    {
        if (testOrderReflexDefinitions == null || (!string.IsNullOrEmpty(Model.Requestor?.RequestorId) && Model.Requestor?.ReflexEnabled is false))
        {
            return Enumerable.Empty<RequestReflexDefinition>();
        }

        if (!(UiSettings.RequestSettings?.IsRequestorInformationEnabled ?? false))
        {
            return testOrderReflexDefinitions;
        }

        var currentRequestorId = Model.Requestor?.RequestorId;

        return testOrderReflexDefinitions.Where(rd =>
            string.IsNullOrEmpty(rd.RequestorId) || rd.RequestorId == currentRequestorId);
    }

    protected override void OnSetAsUnmodified()
    {
        UnregisterListeners();

        PatientViewModel.SetAsUnmodified();
        RequestorViewModel.SetAsUnmodified();
        foreach (var viewModel in TestOrders)
        {
            viewModel.SetAsUnmodified();
        }

        _testDeleted = false;

        RegisterListeners();
    }

    private void RegisterListeners()
    {
        PatientViewModel.PropertyChanged += SetModifiedFlagOnPropertyChanged;
        RequestorViewModel.PropertyChanged += SetModifiedFlagOnPropertyChanged;
        foreach (var viewModel in TestOrders)
        {
            viewModel.PropertyChanged += SetModifiedFlagOnPropertyChanged;
        }

        // Listen to requestor changes to trigger UI updates for reflex filtering
        RequestorViewModel.PropertyChanged += OnRequestorChanged;
    }

    private void UnregisterListeners(bool dispose = false)
    {
        PatientViewModel.PropertyChanged -= SetModifiedFlagOnPropertyChanged;
        RequestorViewModel.PropertyChanged -= SetModifiedFlagOnPropertyChanged;
        foreach (var viewModel in TestOrders)
        {
            viewModel.PropertyChanged -= SetModifiedFlagOnPropertyChanged;
            if (dispose)
            {
                viewModel.Dispose();
            }
        }

        RequestorViewModel.PropertyChanged -= OnRequestorChanged;

        if (dispose)
        {
            PatientViewModel.Dispose();
            RequestorViewModel.Dispose();
        }
    }

    private void OnRequestorChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When requestor changes, trigger a property change notification to refresh the UI
        // This will cause the test list view to re-evaluate the filtered reflex definitions
        if (e.PropertyName == nameof(RequestorViewModel.RequestorId))
        {
            ModifiedChanged();
        }
    }

    protected override void Disposing()
    {
        base.Disposing();
        UnregisterListeners(true);
    }

    public async Task<bool> AddMethodTestsAsync(HashSet<MethodTest>? selectedItems)
    {
        var showMessage = false;
        if (selectedItems is null)
        {
            return showMessage;
        }

        var requestTests = new List<RequestTest>();

        foreach (var testToAdd in selectedItems)
        {
            var (requestTest, differentPreDilutionFactors) = await ConstructRequestTest(testToAdd);
            showMessage |= differentPreDilutionFactors;
            requestTests.Add(requestTest);
        }

        await AddTests(requestTests);

        return showMessage;
    }

    private async Task AddTests(List<RequestTest> tests)
    {
        var affectingState = false;

        foreach (var requestTest in tests)
        {
            // not allowed to add duplicate test
            var existingTestOrder = Model.TestOrders
                .FirstOrDefault(x => x.TestName == requestTest.TestName && x.MethodName == requestTest.MethodName
                                                                        && x.State is TestOrderState.NotStarted or TestOrderState.Scheduled or TestOrderState.UnableToStart or TestOrderState.Started or TestOrderState.Pipetted);
            if (existingTestOrder == null)
            {
                var sortOrder = TestOrders.Count > 0 ? TestOrders.Max(x => x.TestSortOrder) + 1 : 1;
                requestTest.TestSortOrder = sortOrder;
                Model.TestOrders.Add(requestTest);
                var viewModel = new TestOrderViewModel(requestTest, _loc, true);
                viewModel.PropertyChanged += SetModifiedFlagOnPropertyChanged;
                TestOrders.Add(viewModel);

                affectingState = true;
            }
            else
            {
                //if you are not allowed to create a new test order (e.g. duplicate),
                //then at least tag the existing one with the panel ids
                foreach (var panelId in requestTest.TestPanelIds)
                {
                    if (!existingTestOrder.TestPanelIds.Contains(panelId))
                    {
                        existingTestOrder.TestPanelIds.Add(panelId);
                        affectingState = true;
                    }
                }
            }
        }

        if (affectingState)
        {
            await ReCalculateVolumeNeeded();
            ModifiedChanged();
        }
    }

    public async Task<bool> AddPanelTests(HashSet<Panel>? panels)
    {
        var showMessage = false;
        if (panels is null)
        {
            return showMessage;
        }

        var requestTests = new List<RequestTest>();

        foreach (var panel in panels)
        {
            foreach (var panelTest in panel.PanelTests)
            {
                var testFromMoreThanOnePanel = requestTests.FirstOrDefault(x => x.TestName == panelTest.MethodTest.TestName && x.MethodName == panelTest.MethodTest.MethodName);
                if (testFromMoreThanOnePanel is not null)
                {
                    testFromMoreThanOnePanel.TestPanelIds.Add(panel.Id);
                }
                else
                {
                    var (requestTest, differentPreDilutionFactors) = await ConstructRequestTest(panelTest.MethodTest, panel.Id);
                    showMessage |= differentPreDilutionFactors;
                    requestTests.Add(requestTest);
                }
            }
        }

        await AddTests(requestTests);
        return showMessage;
    }

    private async Task<(RequestTest requestTest, bool differentPreDilutionFactors)> ConstructRequestTest(MethodTest methodTest, int? panelId = null)
    {
        var differentPreDilutionFactors = false;

        var test = new RequestTest
        {
            TestName = methodTest.TestName,
            LabTestName = methodTest.LabTestName,
            MethodName = methodTest.MethodName,
            FullMethodName = methodTest.LongMethodName,
            FullTestName = methodTest.FullTestName,
            NumberOfReplicates = methodTest.Replicates,
            State = TestOrderState.NotStarted,
            TestPanelIds = [],
            ReflexDefinitions = [],
            ReportConcentration = methodTest.ReportConcentration,
            ReportClass = methodTest.ReportClass,
            ReportCutOff = methodTest.ReportCutOff,
            ReportQuotient = methodTest.QuotientReportMode != QuotientReportMode.NoReport,
            IsActive = true
        };

        if (methodTest.UsesInstrumentDilution)
        {
            test.InstrumentDilutionFactor = methodTest.DilutionFactor;
        }
        else
        {
            test.InstrumentDilutionFactor = 1;

            if(PreDilutionFactor != methodTest.DilutionFactor)
            {
                differentPreDilutionFactors = true;
            }
        }

        if (panelId.HasValue)
        {
            test.TestPanelIds.Add(panelId.Value);
        }

        // Fetch reflex definitions from API, returns ALL reflex definitions
        // Requestor filtering will be done on the client side
        var reflexDefinitions = await _reflexDefinitionDataSource.GetReflexDefinitionsForTestAsync(test.TestName, test.MethodName);

        var reflexDefinitionCopies = reflexDefinitions.Select(rd => new RequestReflexDefinition
        {
            Id = rd.Id,
            Name = rd.Name,
            Automatic = rd.Automatic,
            RequestorId = rd.RequestorId,
            ReflexCondition = rd.ReflexCondition != null
                ? new RequestReflexCondition
                {
                    TestName = rd.ReflexCondition.TestName,
                    MethodName = rd.ReflexCondition.MethodName,
                    ResultType = rd.ReflexCondition.ResultType,
                    CompareType = rd.ReflexCondition.CompareType,
                    ConcentrationCompareValue = rd.ReflexCondition.ConcentrationCompareValue,
                    ConcentrationCompareValueUpperLimit = rd.ReflexCondition.ConcentrationCompareValueUpperLimit,
                    ConcentrationRawCompareValue = rd.ReflexCondition.ConcentrationRawCompareValue,
                    ConcentrationRawCompareValueUpperLimit = rd.ReflexCondition.ConcentrationRawCompareValueUpperLimit,
                    ClassCompareValue = rd.ReflexCondition.ClassCompareValue,
                    QuotientCompareValue = rd.ReflexCondition.QuotientCompareValue,
                    QuotientCompareValueUpperLimit = rd.ReflexCondition.QuotientCompareValueUpperLimit,
                    CutOffCompareValue = rd.ReflexCondition.CutOffCompareValue,
                    RuCompareValue = rd.ReflexCondition.RuCompareValue
                }
                : null,
            ReflexTests = rd.ReflexTests != null
                ? rd.ReflexTests.Select(rt => new RequestReflexTest
                {
                    Id = rt.Id,
                    TestName = rt.TestName,
                    MethodName = rt.MethodName,
                    NumberOfReplicates = rt.NumberOfReplicates,
                    InstrumentDilutionFactor = rt.InstrumentDilutionFactor,
                    TestSortOrder = rt.TestSortOrder,
                }).ToList()
                : []
        }).ToList();

        test.ReflexDefinitions = reflexDefinitionCopies;

        return (test, differentPreDilutionFactors);
    }

    public async Task DeleteTest(HashSet<TestOrderViewModel> selectedItems)
    {
        foreach (var test in selectedItems)
        {
            TestOrders.Remove(test);
            Model.TestOrders.Remove(test.Model);
            if (test.Model.Id != 0)
            {
                _testDeleted = true;
            }

            // if the test that is about to get deleted has connections to panels
            // remove those panels from all other testes as well
            if (test.Model.TestPanelIds.Count > 0)
            {
                foreach (var testTestPanelId in test.Model.TestPanelIds)
                {
                    foreach (var testOrder in Model.TestOrders)
                    {
                        testOrder.TestPanelIds.Remove(testTestPanelId);
                    }
                }
            }
        }

        // Sort order might have changed. Re-order from 1
        var testSortOrder = 1;
        foreach (var testOrder in TestOrders.OrderBy(x => x.TestSortOrder))
        {
            testOrder.TestSortOrder = testSortOrder;
            testSortOrder++;
        }

        await ReCalculateVolumeNeeded();

        ModifiedChanged();
    }

    public bool HasReflexTests()
    {
        var requestorFilteringEnabled = UiSettings.RequestSettings?.IsRequestorInformationEnabled ?? false;
        var currentRequestorId = Model.Requestor?.RequestorId;

        return TestOrders.Any(testOrder =>
        {
            // If there are no reflex definitions, return false
            if (testOrder.Model.ReflexDefinitions == null || testOrder.Model.ReflexDefinitions.Count == 0)
            {
                return false;
            }

            // If requestor filtering is not enabled, return true if there are any reflex definitions
            if (!requestorFilteringEnabled)
            {
                return true;
            }

            // If requestor filtering is enabled, return true if there are any reflex definitions that match the current requestor ID or have no requestor ID
            return testOrder.Model.ReflexDefinitions.Any(rd =>
                        string.IsNullOrEmpty(rd.RequestorId) || rd.RequestorId == currentRequestorId);
        });
    }

    public void DeleteReflexForTestOrder(int reflexCopyId)
    {
        var modified = false;

        // Remove from all test orders
        foreach (var testOrderVm in TestOrders)
        {
            var reflexes = testOrderVm.Model.ReflexDefinitions;
            var toRemove = reflexes?.FirstOrDefault(r => r.Id == reflexCopyId);
            if (toRemove == null)
            {
                continue;
            }

            modified |= reflexes?.Remove(toRemove) ?? false;
        }

        if (modified)
        {
            SetAsModified();
        }
    }

    private void SetModifiedFlagOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ModifiedChanged();
    }

    public async Task UnloadSampleTubes()
    {
        await _sampleTubeDataSource.UnloadSampleTubes(Model.Id);
    }

    public void SetSampleTubes(List<SampleTubeHistory> sampleTubes)
    {
        SampleTubes.Clear();

        foreach (var tube in sampleTubes)
        {
            var vm = new SampleTubeHistoryViewModel(tube);
            SampleTubes.Add(vm);
        }

        OnPropertyChanged(nameof(SampleTubes));
    }

    public async Task ReCalculateVolumeNeeded()
    {
        _requiredVolume = await _volumeCalculation.GetNeededSampleVolume(Model.TestOrders);
        OnPropertyChanged(nameof(RequiredVolumeDisplay));
    }
}
