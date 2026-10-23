using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class BoolViewModel(IStringLocalizer<Translations> loc) : OptionViewModelBase(loc)
{
    public bool Value
    {
        get;
        set
        {
            field = value;
            IsModified = ValueAsString != OriginalValue;

            Validate();
            OnPropertyChanged(nameof(Value));
        }
    }

    public override bool ResetToDefault()
    {
        Value = bool.Parse(DefaultValue);
        UpdateModelValue();
        return true;
    }

    protected override bool MatchesDependencyValue(string? expectedValue)
    {
        if (bool.TryParse(expectedValue, out var value))
        {
            return Value == value;
        }

        throw new ArgumentOutOfRangeException(nameof(expectedValue), Loc["BoolViewModel_IsDependencyConditionMet"]);

    }

    public override string ValueAsString => Value ? true.ToString() : false.ToString();
}