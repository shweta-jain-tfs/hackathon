using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;

public class TimeViewModel(IStringLocalizer<Translations> loc) : OptionViewModelBase(loc)
{
    public TimeOnly Value
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
        Value = TimeOnly.Parse(DefaultValue, CultureInfo.InvariantCulture);
        UpdateModelValue();
        return true;
    }

    protected override bool MatchesDependencyValue(string? expectedValue)
    {
        return ValueAsString == expectedValue;
    }

    public override string ValueAsString => Value.ToString("HH:mm", CultureInfo.InvariantCulture);
}