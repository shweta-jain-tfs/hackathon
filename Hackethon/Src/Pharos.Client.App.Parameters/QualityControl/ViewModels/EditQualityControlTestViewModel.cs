using System.Globalization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class EditQualityControlTestViewModel : ViewModelBase
{
    private bool _applyAcceptanceRange;
    private bool _useAcceptanceRange;
    private bool _useLotQualification;
    private AcceptanceType _acceptanceType;
    private readonly IInstrumentDilutionService _instrumentDilutionService;

    public EditQualityControlTestViewModel(int id,
        bool applyAcceptanceRange,
        double? lowerLimit,
        double? upperLimit,
        int numberOfTests,
        bool useAcceptanceRange,
        int replicates,
        double replicateCvLimit,
        bool useLotQualification,
        int instrumentDilutionFactor,
        double maxCv,
        int numberOfSd,
        int preDilutionFactor,
        int testId,
        string methodName,
        string testName,
        QualityControlType qualityControlType,
        IInstrumentDilutionService instrumentDilutionService) : this(id, applyAcceptanceRange, numberOfTests, useAcceptanceRange, replicates, replicateCvLimit, useLotQualification, instrumentDilutionFactor, maxCv, numberOfSd, preDilutionFactor, qualityControlType, false, instrumentDilutionService)
    {
        LowerLimit = lowerLimit;
        UpperLimit = upperLimit;
        TestId = testId;
        MethodName = methodName;
        TestName = testName;
    }

    public EditQualityControlTestViewModel(int id,
        bool applyAcceptanceRange,
        int numberOfTests,
        bool useAcceptanceRange,
        int replicates,
        double replicateCvLimit,
        bool useLotQualification,
        int instrumentDilutionFactor,
        double maxCv,
        int numberOfSd,
        int preDilutionFactor,
        QualityControlType qualityControlType,
        IInstrumentDilutionService instrumentDilutionService) : this(id, applyAcceptanceRange, numberOfTests, useAcceptanceRange, replicates, replicateCvLimit, useLotQualification, instrumentDilutionFactor, maxCv, numberOfSd, preDilutionFactor, qualityControlType, true, instrumentDilutionService)
    {
    }

    private EditQualityControlTestViewModel(
        int id,
        bool applyAcceptanceRange,
        int numberOfTests,
        bool useAcceptanceRange,
        int replicates,
        double replicateCvLimit,
        bool useLotQualification,
        int instrumentDilutionFactor,
        double maxCv,
        int numberOfSd,
        int preDilutionFactor,
        QualityControlType qualityControlType,
        bool editingPhadiaControl,
        IInstrumentDilutionService instrumentDilutionService)
    {
        Id = id;
        EditingPhadiaControl = editingPhadiaControl;

        InstrumentDilutionFactors = [];
        NumberOfSds =
        [
            new DropDownOption<int>
            {
                Text = "1",
                Value = 1
            },
            new DropDownOption<int>
            {
                Text = "2",
                Value = 2
            },
            new DropDownOption<int>
            {
                Text = "3",
                Value = 3
            }
        ];

        QualityControlTypes = Enum.GetValues<QualityControlType>().Select(x => new DropDownOption<QualityControlType>
        {
            Text = x.ToLocalizedString(),
            Value = x
        }).ToList();

        _instrumentDilutionService = instrumentDilutionService;

        _applyAcceptanceRange = applyAcceptanceRange;
        _useLotQualification = useLotQualification;
        _acceptanceType = useAcceptanceRange ? AcceptanceType.Range : AcceptanceType.Target;

        NumberOfTests = numberOfTests;
        Replicates = replicates;
        ReplicateCvLimit = replicateCvLimit;
        InstrumentDilutionFactor = instrumentDilutionFactor;
        MaxCv = maxCv;
        NumberOfSd = numberOfSd;
        PreDilutionFactor = preDilutionFactor;
        QualityControlType = qualityControlType;
    }

    public static double DefaultMaxCv => 1;
    public static double? DefaultLowerLimit => null;
    public static double? DefaultUpperLimit => null;
    public static int DefaultNumberOfTests => 5;

    public bool EditingPhadiaControl { get; }

    public int Id
    {
        get;
        set => SetField(ref field, value);
    }

    public bool UseAcceptanceRange => _useAcceptanceRange;

    public int InstrumentDilutionFactor
    {
        get;
        set => SetField(ref field, value);
    }

    public int PreDilutionFactor
    {
        get;
        set
        {
            SetField(ref field, value);
            _ = SetPredilutionFactors(MethodName, TestName);
        }
    }

    public string TestName
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    public string MethodName
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    public int TestId
    {
        get;
        private set => SetField(ref field, value);
    }

    public int Replicates
    {
        get;
        set => SetField(ref field, value);
    }

    public double ReplicateCvLimit
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ApplyAcceptanceRange
    {
        get => _applyAcceptanceRange;
        set
        {
            SetField(ref _applyAcceptanceRange, value);
            ValidationNeeded();
        }
    }

    public double MaxCv
    {
        get;
        set => SetField(ref field, value);
    }

    public int NumberOfSd
    {
        get;
        set => SetField(ref field, value);
    }

    public double? LowerLimit
    {
        get;
        set => SetField(ref field, value);
    }

    public double? UpperLimit
    {
        get;
        set => SetField(ref field, value);
    }

    public bool UseLotQualification
    {
        get => _useLotQualification;
        set
        {
            SetField(ref _useLotQualification, value);
            if (!value)
            {
                ValidationNeeded();
            }
        }
    }

    public int NumberOfTests
    {
        get;
        set => SetField(ref field, value);
    }
    
    public QualityControlType QualityControlType
    {
        get;
        set => SetField(ref field, value);
    }

    public int TotalDilution => PreDilutionFactor * InstrumentDilutionFactor;

    public List<DropDownOption<int>> InstrumentDilutionFactors { get; }
    public List<DropDownOption<int>> NumberOfSds { get; }
    public List<DropDownOption<QualityControlType>> QualityControlTypes { get; }

    public AcceptanceType AcceptanceType
    {
        get => _acceptanceType;
        set
        {
            _acceptanceType = value;
            if (_acceptanceType == AcceptanceType.Range)
            {
                SetField(ref _useAcceptanceRange, true);
            }
            else
            {
                SetField(ref _useAcceptanceRange, false);
            }

            ValidationNeeded();
        }
    }

    private void ValidationNeeded()
    {
        ReValidate?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ReValidate;

    private async Task SetPredilutionFactors(string methodName, string testName)
    {
        InstrumentDilutionFactors.Clear();

        if(string.IsNullOrWhiteSpace(methodName) || string.IsNullOrWhiteSpace(testName))
        {
            return;
        }

        var dilutionFactors = await _instrumentDilutionService.GetDilutionFactors(methodName, testName, PreDilutionFactor);

        InstrumentDilutionFactors.AddRange(dilutionFactors.Where(x => x.IsValid).Select(x => new DropDownOption<int>
        {
            Text = x.DilutionFactor.ToString(CultureInfo.InvariantCulture),
            Value = x.DilutionFactor
        }).ToList());

        ValidationNeeded();
    }

    public async Task SetTest(MethodTest test)
    {
        if (EditingPhadiaControl)
        {
            throw new InvalidOperationException("Cannot set test when editing Phadia Quality Control");
        }

        TestId = test.Id;
        MethodName = test.MethodName;
        TestName = test.TestName;

        if (test.UsesInstrumentDilution)
        {
            InstrumentDilutionFactor = test.DilutionFactor;
            PreDilutionFactor = 1;
        }
        else
        {
            InstrumentDilutionFactor = 1;
            PreDilutionFactor = test.DilutionFactor;
        }

        await SetPredilutionFactors(MethodName, TestName);
    }

    public void RemoveTest()
    {
        if (EditingPhadiaControl)
        {
            throw new InvalidOperationException("Cannot remove test when editing Phadia Quality Control");
        }

        TestId = 0;
        MethodName = string.Empty;
        TestName = string.Empty;

        InstrumentDilutionFactors.Clear();
    }

    public async Task ConstructDilutionFactors()
    {
        await SetPredilutionFactors(MethodName, TestName);
    }
}

public enum AcceptanceType
{
    Target,
    Range
}