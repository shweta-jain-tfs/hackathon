using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Metadata;
using Enum = System.Enum;
using Option = Pharos.Server.Settings.Application.Metadata.Option;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;
internal class ValueConverter : IValueConverter
{
    public Option ValueToOption(PropertyInfo? property, string key, object? value, object? defaultValue, bool required = false, Func<string, string>? localizeFunc = null)
    {
        var option = new Option
        {
            Key = key,
            Required = required
        };

        if (value is bool boolValue)
        {
            option.Type = OptionType.Boolean;
            option.Value = ValueToString(boolValue);
        }
        
        else if (value is DateTimeOffset offset)
        {
            option.Type = OptionType.Date;
            option.Value = ValueToString(offset);
        }
        else if (value is Enum enumValue)
        {
            option.Type = OptionType.Choice;
            option.Value = ValueToString(enumValue);
            foreach (var name in Enum.GetValues(value.GetType()))
            {
                var resourceName = GetDisplayNameLocalEnum((Enum)name);
                var label = localizeFunc?.Invoke(resourceName) ?? resourceName;

                var choice = new Choice
                {
                    Value = name.ToString() ?? "",
                    Label = label
                };

                option.Choices.Add(choice);
            }

            option.IsDisplayModeRadioButtons = property?.GetCustomAttribute<DisplayModeRadioButtons>() != null;

        }
        else if (property?.PropertyType == typeof(int))
        {
            option.Type = OptionType.Integer;
            option.Value = ValueToString(value);
            option.MinValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Min as int? ?? int.MinValue).ToString(CultureInfo.InvariantCulture);
            option.MaxValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Max as int? ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
            option.UnitResourceName = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Unit ?? "");
        }
        else if (property?.PropertyType == typeof(double))
        {
            option.Type = OptionType.Double;
            option.Value = ValueToString(value);
            option.MinValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Min as double? ?? double.MinValue).ToString(CultureInfo.InvariantCulture);
            option.MaxValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Max as double? ?? double.MaxValue).ToString(CultureInfo.InvariantCulture);
            option.UnitResourceName = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Unit ?? "");
        }
        else if (property?.PropertyType == typeof(string))
        {
            // If property is defined as a DynamicDropdown, it is changed to Choice type option
            if (property.CustomAttributes.Any(x => x.AttributeType == typeof(DynamicDropdown)))
            {
                option.Type = OptionType.Choice;
                option.Value = ValueToString(value) ?? throw new ArgumentException($"Dynamic Dropdown should have a default value defined in {property.Name}");

                var dynamicDropdownAttr = property.GetCustomAttribute<DynamicDropdown>();

                // Check if provider-based mode
                if (dynamicDropdownAttr?.ProviderType != null)
                {
                    option.DynamicDropdownProviderType = dynamicDropdownAttr.ProviderType;
                }
                // PageList-based mode
                else
                {
                    option.DynamicDropdownOrigin = dynamicDropdownAttr?.DynamicDropdownOrigin ?? throw new ArgumentException($"DynamicDropdownOrigin should be defined in {property.Name}");
                    option.DynamicDropdownValueTarget = dynamicDropdownAttr?.TargetValue ?? throw new ArgumentException($"TargetValue should be defined in {property.Name}");
                }
            }
            else
            {
                option.Type = OptionType.String;
                option.Value = ValueToString(value) ?? "";
                option.MaxLength = property.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? int.MaxValue;
                option.DisallowedCharactersPattern = property.GetCustomAttribute<DisallowedCharactersAttribute>()?.Pattern;
            }
        }
        else if (property?.PropertyType == typeof(DateTimeOffset?))
        {
            option.Type = OptionType.Date;
            option.Value = ValueToString(value);
        }
        else if (property?.PropertyType == typeof(TimeOnly))
        {
            option.Type = OptionType.Time;
            option.Value = ValueToString(value);
        }
        else
        {
            throw new ArgumentException($"Type [{property?.PropertyType}] of value [{value}] is not supported");
        }

        option.DefaultValue = ValueToString(defaultValue);

        return option;
    }

    public static string GetDisplayNameLocalEnum(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());

        if (field != null)
        {
            if (Attribute.GetCustomAttribute(field, typeof(DisplayNameLocalAttribute)) is DisplayNameLocalAttribute attribute)
            {
                return attribute.ResourceName;
            }
        }
        throw new ArgumentException($"DisplayNameLocalAttribute is missing from enum '{value.GetType().Name}.{value}'");
    }

    public string? ValueToString(object? value)
    {
        return ValueFormattingHelper.ValueToString(value);
    }
}

internal interface IValueConverter
{
    Option ValueToOption(PropertyInfo? property, string key, object? value, object? defaultValue, bool required = false, Func<string, string>? localizeFunc = null);

    string? ValueToString(object? value);
}
