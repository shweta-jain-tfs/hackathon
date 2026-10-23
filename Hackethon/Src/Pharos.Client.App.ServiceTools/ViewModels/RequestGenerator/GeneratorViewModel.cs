using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;

public class GeneratorViewModel : LoadingViewModel, IDisposable
{
    public const int MaxHeadingTrailingLength = 7;
    public const int AmountToGenerateMaxNumberOfDigits = 4;
    public const int PreDilutionFactorMaxNumberOfDigits = 4;
    public const int NumberOfDigitsMaxNumberOfDigits = 2;
    public const int Trays_StartOffsetMaxNumberOfDigits = 4;
    public const int SamplesPerTrayMaxNumberOfDigits = 2;
    public const int SampleId_StartOffsetMaxNumberOfDigits = 7;
    public const int ReplicatesMaxNumberOfDigits = 2;
    public const int MinNumberOfDigits = 1;
    public const int MaxNumberOfDigits = 16;
    public const int MinSampleIdStartOffset = 0;
    public const int MaxSampleIdStartOffset = 9999999;
    public const int MinTrayStartOffset = 0;
    public const int MaxTrayStartOffset = 9999;
    public const int MinSamplesPerTray = 1;
    public const int MaxSamplesPerTray = 50;
    public const int MinAmountToGenerate = 1;
    public const int MaxAmountToGenerate = 9999;
    public const int MinPreDilutionFactor = 1;
    public const int MaxPreDilutionFactor = 1000;

    private readonly IRequestGeneratorDataService _dataService;
    private readonly IJobNotificationProvider _jobNotificationProvider;
    private readonly IRequestGeneratorStateService _stateService;
    private readonly IToasterService _toaster;
    private readonly IExceptionHandlerService _exceptionHandler;
    private readonly IStringLocalizer<Translations> _localizer;
    private bool _stateLoaded;
    private string _sampleIdHeadingCharacters = string.Empty;
    private int _sampleIdNumberOfDigits;
    private int _sampleIdStartOffset;
    private string _sampleIdTrailingCharacters = string.Empty;
    private bool _placeSamplesInTrays;
    private int _traysStartOffset;
    private int _samplesPerTray;
    private int _amountToGenerate;
    private int _preDilutionFactor;

    public GeneratorViewModel(
        IRequestGeneratorDataService dataService,
        IJobNotificationProvider jobNotificationProvider,
        IRequestGeneratorStateService stateService,
        TestListViewModel tests,
        IToasterService toaster,
        IExceptionHandlerService exceptionHandler,
        IStringLocalizer<Translations> localizer)
    {
        _dataService = dataService;
        _jobNotificationProvider = jobNotificationProvider;
        _stateService = stateService;
        _toaster = toaster;
        _exceptionHandler = exceptionHandler;
        _localizer = localizer;
        Tests = tests;
        PreviewItems = new List<RequestPreview>();
        InitializeDefaults();
        Tests.PropertyChanged += OnTestsPropertyChanged;
        _jobNotificationProvider.JobCompleted += OnJobCompletedAsync;
    }

    public TestListViewModel Tests { get; }

    public List<RequestPreview> PreviewItems { get; }

    public string HeadingCharacters
    {
        get => _sampleIdHeadingCharacters;
        set => SetPersistedField(ref _sampleIdHeadingCharacters, value);
    }

    public int NumberOfDigits
    {
        get => _sampleIdNumberOfDigits;
        set => SetPersistedField(ref _sampleIdNumberOfDigits, value);
    }

    public int SampleIdStartOffset
    {
        get => _sampleIdStartOffset;
        set => SetPersistedField(ref _sampleIdStartOffset, value);
    }

    public string TrailingCharacters
    {
        get => _sampleIdTrailingCharacters;
        set => SetPersistedField(ref _sampleIdTrailingCharacters, value);
    }

    public bool PlaceSamplesInTrays
    {
        get => _placeSamplesInTrays;
        set => SetPersistedField(ref _placeSamplesInTrays, value);
    }

    public int TrayStartOffset
    {
        get => _traysStartOffset;
        set => SetPersistedField(ref _traysStartOffset, value);
    }

    public int SamplesPerTray
    {
        get => _samplesPerTray;
        set => SetPersistedField(ref _samplesPerTray, value);
    }

    public int AmountToGenerate
    {
        get => _amountToGenerate;
        set => SetPersistedField(ref _amountToGenerate, value);
    }

    public int PreDilutionFactor
    {
        get => _preDilutionFactor;
        set => SetPersistedField(ref _preDilutionFactor, value);
    }

    public bool IsPreviewVisible
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsPreviewVisible));
        }
    }

    public bool GenerateRealTests
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(GenerateRealTests));
            OnPropertyChanged(nameof(CanGenerate));
        }
    }

    public bool IsGenerating
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsGenerating));
            OnPropertyChanged(nameof(CanPreview));
            OnPropertyChanged(nameof(CanGenerate));
        }
    }

    public bool IsGenerationInProgress
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsGenerationInProgress));
            OnPropertyChanged(nameof(CanGenerate));
        }
    }

    public bool CanPreview => Tests.IncludedTestCount > 0 && !IsGenerating;

    public bool CanGenerate => CanPreview && GenerateRealTests && !IsGenerationInProgress;

    private void ApplyState(RequestGeneratorState? config)
    {
        if (config == null)
        {
            return;
        }

        _sampleIdHeadingCharacters = config.SampleIdHeadingCharacters;
        _sampleIdNumberOfDigits = config.SampleIdNumberOfDigits;
        _sampleIdStartOffset = config.SampleIdStartOffset;
        _sampleIdTrailingCharacters = config.SampleIdTrailingCharacters;
        _placeSamplesInTrays = config.PlaceSamplesInTrays;
        _traysStartOffset = config.TraysStartOffset;
        _samplesPerTray = config.SamplesPerTray;
        _amountToGenerate = config.AmountToGenerate;
        _preDilutionFactor = config.PreDilutionFactor;
    }

    private RequestGeneratorState CreateState()
    {
        return new RequestGeneratorState
        {
            SampleIdHeadingCharacters = HeadingCharacters,
            SampleIdNumberOfDigits = NumberOfDigits,
            SampleIdStartOffset = SampleIdStartOffset,
            SampleIdTrailingCharacters = TrailingCharacters,
            PlaceSamplesInTrays = PlaceSamplesInTrays,
            TraysStartOffset = TrayStartOffset,
            SamplesPerTray = SamplesPerTray,
            AmountToGenerate = AmountToGenerate,
            PreDilutionFactor = PreDilutionFactor
        };
    }

    public async Task GenerateAsync()
    {
        if (!CanGenerate)
        {
            return;
        }

        IsGenerating = true;
        try
        {
            var request = CreateRequest();
            await _dataService.GenerateAsync(request);
            IsGenerationInProgress = true;
            _toaster.ShowToast(ToastType.Information, _localizer["RequestGenerator_GenerateStarted"]);
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    public async Task GeneratePreviewAsync()
    {
        if (!CanPreview)
        {
            return;
        }

        IsGenerating = true;
        try
        {
            var preview = await _dataService.PreviewAsync(CreateRequest());
            ShowPreview(preview);
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    protected override async Task DoLoadAsync()
    {
        var savedConfig = await _stateService.GetStateAsync<RequestGeneratorState>(nameof(GeneratorViewModel));
        ApplyState(savedConfig);

        IsGenerationInProgress = await _dataService.GetStatusAsync();

        await Tests.LoadAsync();
        _stateLoaded = true;
    }

    public void Dispose()
    {
        Tests.PropertyChanged -= OnTestsPropertyChanged;
        _jobNotificationProvider.JobCompleted -= OnJobCompletedAsync;
        GC.SuppressFinalize(this);
    }

    private void InitializeDefaults()
    {
        _sampleIdHeadingCharacters = "SID";
        _sampleIdNumberOfDigits = 6;
        _sampleIdStartOffset = 1;
        _sampleIdTrailingCharacters = string.Empty;
        _placeSamplesInTrays = false;
        _traysStartOffset = 1;
        _samplesPerTray = 50;
        _amountToGenerate = 10;
        _preDilutionFactor = 1;
    }

    private GenerateRequests CreateRequest()
    {
        return new GenerateRequests
        {
            SampleIdHeadingCharacters = HeadingCharacters,
            SampleIdNumberOfDigits = NumberOfDigits,
            SampleIdStartOffset = SampleIdStartOffset,
            SampleIdTrailingCharacters = TrailingCharacters,
            PlaceSamplesInTrays = PlaceSamplesInTrays,
            TraysStartOffset = TrayStartOffset,
            SamplesPerTray = SamplesPerTray,
            AmountToGenerate = AmountToGenerate,
            PreDilutionFactor = PreDilutionFactor,
            Tests = GetActiveTests()
        };
    }

    private List<TestDetails> GetActiveTests()
    {
        return Tests.Tests
            .Where(test => test.IsActive)
            .Select(test => new TestDetails
            {
                MethodName = test.MethodName,
                TestName = test.TestName,
                Replicates = test.Replicates,
                InstrumentDilutionFactor = test.InstrumentDilutionFactor
            })
            .ToList();
    }

    private void ShowPreview(List<RequestPreview> previewItems)
    {
        PreviewItems.Clear();
        PreviewItems.AddRange(previewItems);
        IsPreviewVisible = true;
    }

    private void OnTestsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(TestListViewModel.TestCount) or nameof(TestListViewModel.IncludedTestCount)))
        {
            return;
        }

        OnPropertyChanged(nameof(CanPreview));
        OnPropertyChanged(nameof(CanGenerate));
    }

    private async void OnJobCompletedAsync(object? sender, JobCompletedEventArgs e)
    {
        if (e.JobName != JobNames.RequestGeneration)
        {
            return;
        }

        await OnRequestGenerationFinishedAsync(e.IsSuccessful);
    }

    private async Task OnRequestGenerationFinishedAsync(bool isSuccessful)
    {
        IsGenerationInProgress = false;

        if (isSuccessful)
        {
            await GeneratePreviewAsync();
            _toaster.ShowToast(ToastType.Confirmation, _localizer["RequestGenerator_GenerateCompleted"]);
            return;
        }

        _toaster.ShowToast(ToastType.Information, _localizer["RequestGenerator_GenerateFailed"]);
    }

    private void SetPersistedField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!SetField(ref field, value, propertyName))
        {
            return;
        }

        _ = PersistStateAsync();
    }

    private Task PersistStateAsync()
    {
        if (!_stateLoaded)
        {
            return Task.CompletedTask;
        }

        return _stateService.SetStateAsync(nameof(GeneratorViewModel), CreateState());
    }
}
