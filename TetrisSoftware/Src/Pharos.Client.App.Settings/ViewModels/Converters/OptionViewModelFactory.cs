using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels.Converters;
public static class OptionViewModelFactory
{
    public static OptionViewModelBase CreateOptionViewModel(Option option, IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService, string? value = null)
    {
        OptionViewModelBase viewModel;
        var optionValue = value ?? option.Value;

        switch (option.Type)
        {
            case OptionType.Boolean:
                viewModel = new BoolViewModel(loc)
                {
                    Value = optionValue == "True",
                    Model = option
                };
                break;
            case OptionType.String:
                if (option.IsObfuscated)
                {
                    viewModel = new PasswordViewModel(loc, dataService)
                    {
                        Value = optionValue,
                        MaxLength = option.MaxLength,
                        DisallowedCharactersPattern = option.DisallowedCharactersPattern,
                        Model = option
                    };
                }
                else
                {
                    viewModel = new StringViewModel(loc, dataService)
                    {
                        Value = optionValue,
                        MaxLength = option.MaxLength,
                        DisallowedCharactersPattern = option.DisallowedCharactersPattern,
                        Model = option
                    };
                }
                break;
            case OptionType.Integer:
                viewModel = new IntegerViewModel(loc, dataService)
                {
                    Value = int.Parse(optionValue, CultureInfo.InvariantCulture),
                    MinInt = int.Parse(option.MinValue, CultureInfo.InvariantCulture),
                    MaxInt = int.Parse(option.MaxValue, CultureInfo.InvariantCulture),
                    UnitResourceName = option.UnitResourceName,
                    Model = option
                };
                break;
            case OptionType.Double:
                viewModel = new DoubleViewModel(loc)
                {
                    Value = double.Parse(optionValue, CultureInfo.InvariantCulture),
                    MinDouble = double.Parse(option.MinValue, CultureInfo.InvariantCulture),
                    MaxDouble = double.Parse(option.MaxValue, CultureInfo.InvariantCulture),
                    UnitResourceName = option.UnitResourceName,
                    Model = option
                };
                break;
            case OptionType.Choice:
                viewModel = new ChoicesViewModel(loc, dataService)
                {
                    Choices = option.Choices.Select(c => new DropDownOption<string>
                    {
                        Text = c.Label,
                        Value = c.Value
                    }).ToList(),
                    Value = optionValue,
                    Model = option
                };
                break;
            case OptionType.Date:
                viewModel = new DateViewModel(loc)
                {
                    Value = DateTimeOffset.Parse(optionValue, CultureInfo.InvariantCulture),
                    Model = option
                };
                break;
            case OptionType.Time:
                viewModel = new TimeViewModel(loc)
                {
                    Value = TimeOnly.Parse(optionValue, CultureInfo.InvariantCulture),
                    Model = option
                };
                break;
            default:
                throw new NotImplementedException($"The option type {option.Type} is not supported");
        }

        viewModel.OriginalValue = option.Value;
        viewModel.Model = option;
        viewModel.IsModified = false;
        return viewModel;
    }
}