using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class DateViewModel(IStringLocalizer<Translations> loc) : OptionViewModelBase(loc)
{
    public DateTimeOffset Value
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
        Value = DateTimeOffset.Parse(DefaultValue, CultureInfo.InvariantCulture);
        UpdateModelValue();
        return true;
    }

    protected override bool MatchesDependencyValue(string? expectedValue)
    {
        return ValueAsString == expectedValue;
    }
    public override string ValueAsString => Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}