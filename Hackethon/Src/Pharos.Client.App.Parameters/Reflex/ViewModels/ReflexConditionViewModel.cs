using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.Common.App.Helpers;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

public sealed class ReflexConditionViewModel : ViewModelItem<ReflexCondition>
{
    private readonly IMethodTestDataSource _methodsDataSource;

    public ReflexConditionViewModel(ReflexCondition model, IMethodTestDataSource methodsDataSource, bool canEdit) : base(model)
    {
        _methodsDataSource = methodsDataSource;
        CanEdit = canEdit;

        // Do nothing if we do not have a test
        if (string.IsNullOrEmpty(model.TestName) && string.IsNullOrEmpty(model.MethodName))
        {
            return;
        }

        // Check if the condition data is still valid
        CheckIfDataIsValid();

        // Add options on startup
        AddResultTypeOptions();
        AddCompareTypeOptions(ResultType);
    }

    public bool IsCompleteCondition => ResultType switch
    {
        ResultType.Concentration => ConcentrationCompareValue.HasValue,
        ResultType.ConcentrationRaw => ConcentrationRawCompareValue.HasValue,
        ResultType.Class => !string.IsNullOrEmpty(ClassCompareValue),
        ResultType.CutOff => !string.IsNullOrEmpty(CutOffCompareValue),
        ResultType.Quotient => QuotientCompareValue.HasValue,
        ResultType.RU => RuCompareValue.HasValue,
        _ => false
    };

    public string? TestName
    {
        get => Model.TestName;
        set => SetProperty(Model.TestName, value, v => Model.TestName = v);
    }

    public string? MethodName
    {
        get => Model.MethodName;
        set => SetProperty(Model.MethodName, value, v => Model.MethodName = v);
    }

    public ResultType ResultType
    {
        get => Model.ResultType;
        set
        {
            SetProperty(Model.ResultType, value, v => Model.ResultType = v);
            ResetCompareType();

            if (value > 0 && CompareOptions.Count == 0)
            {
                AddCompareTypeOptions(value);
            }
        }
    }

    /// <summary>
    /// Nullable type to allow empty dropdown
    /// </summary>
    public ResultType? NullableResultType
    {
        get => ResultType == ResultType.Default ? null : ResultType;
        set => ResultType = value ?? ResultType.Default;
    }

    public CompareType CompareType
    {
        get => Model.CompareType;
        set
        {
            SetProperty(Model.CompareType, value, v => Model.CompareType = v);
            ResetUpperLimitValues();
        }
    }

    /// <summary>
    /// Nullable type to allow empty dropdown
    /// </summary>
    public CompareType? NullableCompareType
    {
        get => CompareType == CompareType.Default ? null : CompareType;
        set => CompareType = value ?? CompareType.Default;
    }

    public double? ConcentrationCompareValue
    {
        get => Model.ConcentrationCompareValue;
        set => SetProperty(Model.ConcentrationCompareValue, value, v => Model.ConcentrationCompareValue = v);
    }

    public double? ConcentrationCompareValueUpperLimit
    {
        get => Model.ConcentrationCompareValueUpperLimit;
        set => SetProperty(Model.ConcentrationCompareValueUpperLimit, value, v => Model.ConcentrationCompareValueUpperLimit = v);
    }

    /// <summary>
    /// Tuple property for PhNumberRange binding (Concentration).
    /// </summary>
    public (double? Min, double? Max) ConcentrationCompareRange
    {
        get => (ConcentrationCompareValue, ConcentrationCompareValueUpperLimit);
        set
        {
            ConcentrationCompareValue = value.Min;
            ConcentrationCompareValueUpperLimit = value.Max;
        }
    }

    public double? ConcentrationRawCompareValue
    {
        get => Model.ConcentrationRawCompareValue;
        set => SetProperty(Model.ConcentrationRawCompareValue, value, v => Model.ConcentrationRawCompareValue = v);
    }

    public double? ConcentrationRawCompareValueUpperLimit
    {
        get => Model.ConcentrationRawCompareValueUpperLimit;
        set => SetProperty(Model.ConcentrationRawCompareValueUpperLimit, value, v => Model.ConcentrationRawCompareValueUpperLimit = v);
    }

    /// <summary>
    /// Tuple property for PhNumberRange binding (ConcentrationRaw).
    /// </summary>
    public (double? Min, double? Max) ConcentrationRawCompareRange
    {
        get => (ConcentrationRawCompareValue, ConcentrationRawCompareValueUpperLimit);
        set
        {
            ConcentrationRawCompareValue = value.Min;
            ConcentrationRawCompareValueUpperLimit = value.Max;
        }
    }

    public string? ClassCompareValue
    {
        get => Model.ClassCompareValue;
        set => SetProperty(Model.ClassCompareValue, value, v => Model.ClassCompareValue = v);
    }

    /// <summary>
    /// Tuple property for PhNumberRange binding (Quotient).
    /// </summary>
    public (double? Min, double? Max) QuotientCompareRange
    {
        get => (QuotientCompareValue, QuotientCompareValueUpperLimit);
        set
        {
            QuotientCompareValue = value.Min;
            QuotientCompareValueUpperLimit = value.Max;
        }
    }

    public double? QuotientCompareValue
    {
        get => Model.QuotientCompareValue;
        set => SetProperty(Model.QuotientCompareValue, value, v => Model.QuotientCompareValue = v);
    }

    public double? QuotientCompareValueUpperLimit
    {
        get => Model.QuotientCompareValueUpperLimit;
        set => SetProperty(Model.QuotientCompareValueUpperLimit, value, v => Model.QuotientCompareValueUpperLimit = v);
    }

    public string? CutOffCompareValue
    {
        get => Model.CutOffCompareValue;
        set => SetProperty(Model.CutOffCompareValue, value, v => Model.CutOffCompareValue = v);
    }

    public double? RuCompareValue
    {
        get => Model.RuCompareValue;
        set => SetProperty(Model.RuCompareValue, value, v => Model.RuCompareValue = v);
    }

    public bool ReportingConcentration
    {
        get => Model.ReportingConcentration;
        private set => SetProperty(Model.ReportingConcentration, value, v => Model.ReportingConcentration = v);
    }

    public bool ReportingClass
    {
        get => Model.ReportingClass;
        private set => SetProperty(Model.ReportingClass, value, v => Model.ReportingClass = v);
    }

    public bool ReportingCutOff
    {
        get => Model.ReportingCutOff;
        private set => SetProperty(Model.ReportingCutOff, value, v => Model.ReportingCutOff = v);
    }

    public bool ReportingQuotient
    {
        get => Model.ReportingQuotient;
        private set => SetProperty(Model.ReportingQuotient, value, v => Model.ReportingQuotient = v);
    }

    public List<string> ClassOptions
    {
        get => Model.ClassOptions.ToList();
        private set => SetProperty(Model.ClassOptions, value, v => Model.ClassOptions = v);
    }

    public List<string> CutOffOptions
    {
        get => Model.CutOffOptions.ToList();
        private set => SetProperty(Model.CutOffOptions, value, v => Model.CutOffOptions = v);
    }

    public List<DropDownOption<ResultType?>> ResultOptions { get; private set; } = new();

    public List<DropDownOption<CompareType?>> CompareOptions { get; private set; } = new();

    public List<DropDownOption<string?>> ClassDropDownOptions { get; set; } = new();

    public List<DropDownOption<string?>> CutoffDropDownOptions { get; set; } = new();

    /// <summary>
    /// Current step in the wizard (1-4). Used for validation to only validate fields up to current step.
    /// Step 1: TestName
    /// Step 2: ResultType  
    /// Step 3: CompareType
    /// Step 4: CompareValue
    /// </summary>
    public int CurrentStep
    {
        get
        {
            if (NullableCompareType != null)
            {
                return 4;
            }

            if (NullableResultType != null)
            {
                return 3;
            }

            if (!string.IsNullOrEmpty(TestName))
            {
                return 2;
            }

            return 1;
        }
    }

    /// <summary>
    /// Gets the live formatted condition name for display (e.g., "Test1 Concentration > 5").
    /// Computed from current property values using <see cref="ReflexConditionFormatter"/>.
    /// </summary>
    public string FormattedCondition => ReflexConditionFormatter.Format(
        TestName,
        ResultType,
        CompareType,
        ConcentrationCompareValue,
        ConcentrationCompareValueUpperLimit,
        ConcentrationRawCompareValue,
        ConcentrationRawCompareValueUpperLimit,
        QuotientCompareValue,
        QuotientCompareValueUpperLimit,
        ClassCompareValue,
        CutOffCompareValue,
        RuCompareValue);

    public bool CanEdit { get; }

    public void AddResultTypeOptions()
    {
        if (ReportingConcentration)
        {
            ResultOptions.Add(new DropDownOption<ResultType?>
            {
                Text = ResultType.Concentration.ToLocalizedString(),
                Value = ResultType.Concentration
            });

            ResultOptions.Add(new DropDownOption<ResultType?>
            {
                Text = ResultType.ConcentrationRaw.ToLocalizedString(),
                Value = ResultType.ConcentrationRaw
            });
        }

        if (ReportingClass)
        {
            ResultOptions.Add(new DropDownOption<ResultType?>
            {
                Text = ResultType.Class.ToLocalizedString(),
                Value = ResultType.Class
            });

            // Here we also load and add the class drop down options
            AddClassOptions();
        }

        if (ReportingCutOff)
        {
            ResultOptions.Add(new DropDownOption<ResultType?>
            {
                Text = ResultType.CutOff.ToLocalizedString(),
                Value = ResultType.CutOff
            });

            // Here we also load and add the cut off drop down options
            AddCutOffOptions();
        }

        if (ReportingQuotient)
        {
            ResultOptions.Add(new DropDownOption<ResultType?>
            {
                Text = ResultType.Quotient.ToLocalizedString(),
                Value = ResultType.Quotient
            });  
        }

        // Add the RU option regardless of reporting flags, as it is always available
        ResultOptions.Add(new DropDownOption<ResultType?>
        {
            Text = ResultType.RU.ToLocalizedString(),
            Value = ResultType.RU
        });
    }

    private void AddCutOffOptions()
    {
        CutoffDropDownOptions = CutOffOptions.Select(x => new DropDownOption<string?>
        {
            Text = x,
            Value = x
        }).ToList();
    }

    private void AddClassOptions()
    {
        ClassDropDownOptions = ClassOptions.Select(x => new DropDownOption<string?>
        {
            Text = x,
            Value = x
        }).ToList();
    }

    public void AddCompareTypeOptions(ResultType? resultType)
    {
        if (resultType is null or 0)
        {
            return;
        }

        switch (resultType)
        {
            case ResultType.Concentration or ResultType.ConcentrationRaw:

                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.Above.ToLocalizedString(),
                    Value = CompareType.Above
                });

                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.Below.ToLocalizedString(),
                    Value = CompareType.Below
                });

                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.Between.ToLocalizedString(),
                    Value = CompareType.Between
                });
                break;
            case ResultType.Class:

                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.EqualTo.ToLocalizedString(),
                    Value = CompareType.EqualTo
                });

                break;
            case ResultType.CutOff:

                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.EqualTo.ToLocalizedString(),
                    Value = CompareType.EqualTo
                });

                break;
            case ResultType.Quotient:

                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.Above.ToLocalizedString(),
                    Value = CompareType.Above
                });
                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.Below.ToLocalizedString(),
                    Value = CompareType.Below
                });
                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.Between.ToLocalizedString(),
                    Value = CompareType.Between
                });

                break;
            case ResultType.RU:

                CompareOptions.Add(new DropDownOption<CompareType?>
                {
                    Text = CompareType.Below.ToLocalizedString(),
                    Value = CompareType.Below
                });

                break;        
            default:
                throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null);
        }
    }

    public void ResetResult()
    {
        ResultOptions = new();
        ResultType = ResultType.Default;

        ResetCompareType();
    }

    public void ResetCompareType()
    {
        CompareOptions = new();
        CompareType = CompareType.Default;
        ConcentrationCompareValue = null;
        ConcentrationRawCompareValue = null;
        ClassCompareValue = null;
        QuotientCompareValue = null;
        CutOffCompareValue = null;
        RuCompareValue = null;

        ResetUpperLimitValues();
    }

    public void ResetUpperLimitValues()
    {
        ConcentrationCompareValueUpperLimit = null;
        ConcentrationRawCompareValueUpperLimit = null;
        QuotientCompareValueUpperLimit = null;
    }

    public void SetData(MethodTest data, ParameterInfo parameterInfo)
    {
        TestName = data.TestName;
        MethodName = data.MethodName;
        ReportingConcentration = parameterInfo.ReportConcentration;
        ReportingClass = parameterInfo.ReportClass;
        ReportingCutOff = parameterInfo.ReportCutOff;
        ClassOptions = parameterInfo.ClassOptions.ToList();
        CutOffOptions = parameterInfo.CutOffOptions.ToList();
        ReportingQuotient = parameterInfo.ReportQuotient;
    }

    public void CheckIfDataIsValid()
    {
        var dataNotValid = false;

        switch (ResultType)
        {
            default:
            case ResultType.Default:
                dataNotValid = true;
                break;
            case ResultType.Concentration:
            case ResultType.ConcentrationRaw:
                if (!ReportingConcentration)
                {
                    dataNotValid = true;
                }
                break;
            case ResultType.Class:
                if (!ReportingClass || ClassCompareValue == null || !ClassOptions.Contains(ClassCompareValue))
                {
                    dataNotValid = true;
                }
                break;
            case ResultType.CutOff:
                if (!ReportingCutOff || CutOffCompareValue == null || !CutOffOptions.Contains(CutOffCompareValue))
                {
                    dataNotValid = true;
                }
                break;
            case ResultType.Quotient:
                if (!ReportingQuotient)
                {
                    dataNotValid = true;
                }
                break;
            case ResultType.RU:
                // Always valid, as RU is always available
                break;
        }

        if (dataNotValid)
        {
            ResetResult();
        }
    }

    public async Task ApplyData(MethodTest data)
    {
        var parameterInfo = await _methodsDataSource.GetParameterInfoAsync(data.TestName, data.MethodName);
        ResetResult();
        SetData(data, parameterInfo);
        AddResultTypeOptions();
    }
}