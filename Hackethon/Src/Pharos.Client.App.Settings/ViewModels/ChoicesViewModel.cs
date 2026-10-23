using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class ChoicesViewModel(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService) : OptionViewModelBase(loc, dataService)
{
    public string Value
    {
        get;
        set
        {
            field = value;
            IsModified = ValueAsString != OriginalValue;

            Validate();
            OnPropertyChanged(nameof(Value));
        }
    } = string.Empty;

    public List<DropDownOption<string>>? Choices
    {
        get =>
            field != null
                ?
                [
                    ..field.Select(c => new DropDownOption<string>
                    {
                        Text = c.Text ?? "",
                        Value = c.Value
                    })
                ]
                : [];
        set;
    } = [];

    public override bool ResetToDefault()
    {
        Value = DefaultValue;
        UpdateModelValue();
        return true;
    }

    protected override bool MatchesDependencyValue(string? expectedValue)
    {
        return Value == expectedValue;
    }

    public override string ValueAsString => Value;
}