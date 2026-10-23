using FluentValidation;
using FluentValidation.Internal;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public abstract class OptionViewModelBase(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase>? dataService = null, IValidator<OptionViewModelBase>? validator = null) : SettingsViewModelBase(loc)
{
    public string OriginalValue { get; set; } = "";

    private Option? ModelAsOption => Model as Option;

    public string DefaultValue => ModelAsOption?.DefaultValue ?? "";

    public bool IsRequired => ModelAsOption?.Required ?? false;

    public bool IsUnique => ModelAsOption?.Unique ?? false;

    public bool IsUniqueCaseInsensitive => ModelAsOption?.UniqueIsCaseInsensitive ?? false;

    public bool IsAbsoluteDirectoryPath => ModelAsOption?.IsAbsoluteDirectoryPath ?? false;

    public abstract string ValueAsString { get; }

    public IValidator<OptionViewModelBase> Validator { get; } = validator
                                                                ?? (dataService != null
                                                                    ? new OptionViewModelBaseValidator(loc, dataService)
                                                                    : new OptionViewModelBaseValidator(loc));

    /// <summary>
    /// By default all rules are validated.
    /// Providing 'options => options.IncludeRuleSets("Name_Of_RuleSet")' you can validate only the rules in that RuleSet
    /// </summary>
    public void Validate(Action<ValidationStrategy<OptionViewModelBase>>? configureOptions = null)
    {
        var result = configureOptions == null
            ? Validator.Validate(this, options => options.IncludeAllRuleSets())
            : Validator.Validate(this, configureOptions);

        ValidationErrors = result.Errors.Select(e => e.ErrorMessage).ToList();
    }

    public override bool Save()
    {
        UpdateModelValue();
        IsModified = false;

        return true;
    }

    protected void UpdateModelValue()
    {
        if (Model is not Option optionModel)
        {
            throw new InvalidOperationException($"Model [{Model?.Key}] is not an Option");
        }

        optionModel.Value = ValueAsString;
    }

    protected override void ValidateSetting()
    {
        Validate();
    }
}

/// <summary>
/// General validator for all option classes.
/// </summary>
public class OptionViewModelBaseValidator : AbstractValidator<OptionViewModelBase>
{
    public OptionViewModelBaseValidator(IStringLocalizer<Translations> loc)
    {
        // Check that Value for Option is not same as DefaultValue if the Option IsRequired
        RuleSet(nameof(OptionViewModelBase.IsRequired), () =>
        {
            RuleFor(p => p)
               .Must(p => !string.IsNullOrWhiteSpace(p.ValueAsString))
               .When(p => p.IsRequired)
               .WithMessage(p => loc["Validation_Settings_ValueRequired", p.Label].Value);
        });

        RuleSet(nameof(OptionViewModelBase.IsAbsoluteDirectoryPath), () =>
        {
            RuleFor(p => p)
                .Must(p => IsValidAbsoluteDirectoryPath(p.ValueAsString))
                .When(p => p.IsAbsoluteDirectoryPath && !string.IsNullOrWhiteSpace(p.ValueAsString))
                .WithMessage(p => loc["Validation_Settings_AbsoluteDirectoryPath", p.Label].Value);
        });
    }

    public OptionViewModelBaseValidator(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService) : this(loc)
    {
        // Verify that the same value isn't already in use when IsUnique is true in Option
        RuleSet(nameof(OptionViewModelBase.IsUnique), () =>
        {
            RuleFor(p => p)
                .Must(p => IsUnique(p, dataService.KeyItemsPages))
                .When(p => p.IsUnique)
                .WithMessage(p => loc["Validation_Settings_ValueNotUnique", p.Label].Value);
        });
    }

    private static bool IsValidAbsoluteDirectoryPath(string path)
    {
        return AbsoluteDirectoryPathFormatValidator.IsValid(path);
    }

    private static bool IsUnique(OptionViewModelBase optionViewModelBase, IEnumerable<Page> pages)
    {
        return optionViewModelBase.Key is null || pages.All(page => VerifyUniqueValue(optionViewModelBase, page));
    }

    private static bool IsSameValueAlreadyUsed(string modelItemKey, string modelItemValue, string pageItemKey, string pageItemValue, bool caseInsensitive)
    {
        if (modelItemKey == pageItemKey)
        {
            return false;
        }

        var comparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(modelItemValue, pageItemValue, comparison))
        {
            return false;
        }

        var modelItemKeySegments = modelItemKey.Split(":");
        var pageItemKeySegments = pageItemKey.Split(":");

        // at least 3 segments are expected, for example Lis:LisChannels:0:Name
        if (modelItemKeySegments.Length < 3 && modelItemKeySegments.Length != pageItemKeySegments.Length)
        {
            return false;
        }

        // property segment, for example Name
        if (modelItemKeySegments[^1] != pageItemKeySegments[^1])
        {
            return false;
        }

        // check all the segments before the id number
        for (var i = 0; i < modelItemKeySegments.Length - 2; i++)
        {
            if (modelItemKeySegments[i] != pageItemKeySegments[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool VerifyUniqueValue(OptionViewModelBase optionViewModelBase, SettingsChildItem item)
    {
        switch (item)
        {
            case Option option:
                {
                    return !IsSameValueAlreadyUsed(optionViewModelBase.Key!, optionViewModelBase.ValueAsString, option.Key, option.Value, optionViewModelBase.IsUniqueCaseInsensitive);
                }
            case Page page:
                {
                    var children = page.Children;
                    if (children.Select(child => VerifyUniqueValue(optionViewModelBase, child)).Any(isUnique => !isUnique))
                    {
                        return false;
                    }

                    break;
                }
            case Group group:
                {
                    var children = group.Children;
                    if (children.Select(child => VerifyUniqueValue(optionViewModelBase, child)).Any(isUnique => !isUnique))
                    {
                        return false;
                    }

                    break;
                }
            case PageList pageList:
                {
                    var pageEntries = pageList.PageEntries;
                    if (pageEntries.Select(entry => VerifyUniqueValue(optionViewModelBase, entry)).Any(isUnique => !isUnique))
                    {
                        return false;
                    }

                    break;
                }
            default:
                return true;
        }

        return true;
    }
    public static class AbsoluteDirectoryPathFormatValidator
    {
        private static readonly char[] _directorySeparators = ['\\', '/'];
        private static readonly char[] _invalidWindowsPathChars = ['<', '>', '"', '|', '?', '*'];

        public static bool IsValid(string? path)
        {
            if (path is null or "")
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return IsWindowsAbsoluteDirectoryPath(path);
        }

        private static bool IsWindowsAbsoluteDirectoryPath(string path)
        {
            return IsDriveRootedPath(path) || IsUncPath(path);
        }

        private static bool IsDriveRootedPath(string path)
        {
            return path.Length >= 3
                && IsAsciiLetter(path[0])
                && path[1] == ':'
                && IsDirectorySeparator(path[2])
                && HasOnlyValidWindowsPathCharacters(path, allowDriveColon: true);
        }

        private static bool IsUncPath(string path)
        {
            if (path.Length < 5
                || !IsDirectorySeparator(path[0])
                || !IsDirectorySeparator(path[1])
                || IsDirectorySeparator(path[2])
                || !HasOnlyValidWindowsPathCharacters(path, allowDriveColon: false))
            {
                return false;
            }

            var parts = path[2..].Split(_directorySeparators, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2;
        }

        private static bool HasOnlyValidWindowsPathCharacters(string path, bool allowDriveColon)
        {
            for (var i = 0; i < path.Length; i++)
            {
                var ch = path[i];

                if (char.IsControl(ch) || Array.IndexOf(_invalidWindowsPathChars, ch) >= 0)
                {
                    return false;
                }

                if (ch == ':' && (!allowDriveColon || i != 1))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsDirectorySeparator(char ch)
        {
            return ch is '\\' or '/';
        }

        private static bool IsAsciiLetter(char ch)
        {
            return ch is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');
        }
    }
}