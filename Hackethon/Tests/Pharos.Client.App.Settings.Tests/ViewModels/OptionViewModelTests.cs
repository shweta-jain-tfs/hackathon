using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.App.Settings.TestHelpers;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using static Pharos.Client.App.Settings.ViewModels.StringViewModel;

namespace Pharos.Client.App.Settings.ViewModels;

public class OptionViewModelTests
{
    private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer = LocalizerMockFactory.Create();
    private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService = new();

    [Theory]
    [InlineData("test_prefix:15:SampleDefault", "", "default_value", false)]
    [Trait("ModuleRequirement", "MR_3644")]
    public void StringViewModel_ShouldValidate_WhenValueIsRequired(string key, string value, string defaultValue, bool isValidExpected)
    {
        var option = new Option()
        {
            Key = key,
            Required = true,
            DefaultValue = defaultValue
        };
        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = value,
        };
        viewModel.Validate();

        var validator = viewModel.Validator;
        validator.Should().BeOfType<StringViewModelValidator>();
        Assert.Equal(isValidExpected, viewModel.ValidationErrors.Count == 0);
    }

    [Fact]
    public void StringViewModel_ShouldAddValidationError_WhenRequiredValueIsEmpty()
    {
        var option = new Option
        {
            Key = "test_prefix:15:Name",
            Required = true
        };
        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = "",
        };

        viewModel.ValidationErrors.Should().BeEmpty();

        viewModel.Validate();

        viewModel.ValidationErrors.Should().NotBeEmpty();
    }


    [Theory]
    [InlineData("test_prefix:1:AChoiceKey", "", "1", false)]
    [Trait("ModuleRequirement", "MR_3644")]
    public void ChoicesViewModel_ShouldValidate_WhenValueIsRequiredAndNotEqualToDefault(string key, string value, string defaultValue, bool isValidExpected)
    {
        var option = new Option()
        {
            Key = key,
            Required = true,
            DefaultValue = defaultValue
        };
        var viewModel = new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = value,
        };

        var validator = viewModel.Validator;
        validator.Should().BeOfType<OptionViewModelBaseValidator>();
        Assert.Equal(isValidExpected, viewModel.ValidationErrors.Count == 0);
    }

    [Theory]
    [InlineData(3, "test_prefix:3:Name", "value_0", false)]
    [InlineData(3, "test_prefix:3:Name", "value_3", true)]
    [InlineData(3, "other_prefix:0:Name", "value_0", true)]
    public void StringViewModel_ShouldValidate_WhenValueIsUniqueAndNotFound(int numPages, string key, string value, bool isValidExpected)
    {
        var pages = GenerateTestPages(numPages, OptionType.String, "test_prefix", "Name");
        _mockDataService.SetupGet(d => d.KeyItemsPages).Returns(pages);

        var option = new Option()
        {
            Key = key,
            Unique = true
        };
        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = value,
        };
        viewModel.Validate();

        var validator = viewModel.Validator;
        validator.Should().BeOfType<StringViewModelValidator>();
        Assert.Equal(isValidExpected, viewModel.ValidationErrors.Count == 0);
    }

    [Theory]
    [InlineData(5, "test_prefix:23:Id", 0, false)]
    [InlineData(5, "test_prefix:23:Id", 125, true)]
    [InlineData(5, "other_prefix:0:Id", 0, true)]
    public void IntegerViewModel_ShouldValidate_WhenValueIsUniqueAndNotFound(int numPages, string key, int value, bool isValidExpected)
    {
        var pages = GenerateTestPages(numPages, OptionType.Integer, "test_prefix", "Id");
        _mockDataService.SetupGet(d => d.KeyItemsPages).Returns(pages);

        var option = new Option()
        {
            Key = key,
            Unique = true
        };
        var viewModel = new IntegerViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = value,
        };

        var validator = viewModel.Validator;
        validator.Should().BeOfType<IntegerViewModelValidator>();
        Assert.Equal(isValidExpected, viewModel.ValidationErrors.Count == 0);
    }

    [Theory]
    [InlineData(4, "test:prefix:23:AChoiceKey", "0", false)]
    [InlineData(4, "test:prefix:23:AChoiceKey", "11", true)]
    [InlineData(4, "other:prefix:0:AChoiceKey", "0", true)]
    public void ChoicesViewModel_ShouldValidate_WhenValueIsUniqueAndNotFound(int numPages, string key, string value, bool isValidExpected)
    {
        var pages = GenerateTestPages(numPages, OptionType.Choice, "test:prefix", "AChoiceKey");
        _mockDataService.SetupGet(d => d.KeyItemsPages).Returns(pages);

        var option = new Option()
        {
            Key = key,
            Unique = true
        };
        var viewModel = new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = value,
        };

        var validator = viewModel.Validator;
        validator.Should().BeOfType<OptionViewModelBaseValidator>();
        Assert.Equal(isValidExpected, viewModel.ValidationErrors.Count == 0);
    }

    [Theory]
    [InlineData(3, "test_prefix:3:Name", "new_value", "default_value", 0)]
    [InlineData(3, "test_prefix:3:Name", "value_0", "default_value", 1)]
    [InlineData(3, "test_prefix:3:Name", "", "default_value", 1)]
    [InlineData(3, "test_prefix:3:Name", "very_long_value_12345678901234567890", "default_value", 1)]
    [InlineData(3, "test_prefix:3:Name", "", "value_0", 1)]
    [InlineData(3, "test_prefix:3:Name", "", "very_long_value_12345678901234567890", 1)]
    [Trait("ModuleRequirement", "MR_3644")]
    public void StringViewModel_ShouldGenerateValidationError_ForEachFailedRule(int numPages, string key, string value, string defaultValue, int numErrors)
    {
        var pages = GenerateTestPages(numPages, OptionType.String, "test_prefix", "Name");
        _mockDataService.SetupGet(d => d.KeyItemsPages).Returns(pages);

        var option = new Option()
        {
            Key = key,
            Unique = true,
            Required = true,
            DefaultValue = defaultValue
        };
        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            MaxLength = 20,
            Value = value,
        };
        viewModel.Validate();

        var validator = viewModel.Validator;
        validator.Should().BeOfType<StringViewModelValidator>();
        Assert.Equal(numErrors, viewModel.ValidationErrors.Count);
    }

    [Fact]
    public void StringViewModel_ShouldUseLocalizedMessage_WhenAbsoluteDirectoryPathIsInvalid()
    {
        _mockLocalizer
            .Setup(x => x["Validation_Settings_AbsoluteDirectoryPath", It.IsAny<object[]>()])
            .Returns((string key, object[] args) =>
                new LocalizedString(key, $"localized path error for {args[0]}"));

        var option = new Option
        {
            Key = "Result:ExportPath",
            Label = "Path",
            IsAbsoluteDirectoryPath = true,
        };

        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = "tmp"
        };
        viewModel.Validate();

        viewModel.ValidationErrors.Should().ContainSingle("localized path error for Path");
    }

    [Theory]
    [InlineData("valid value", true)]
    [InlineData("invalid<value", false)]
    [InlineData("invalid>value", false)]
    [InlineData("", true)]
    public void StringViewModel_ShouldValidateDisallowedCharacters(string value, bool expectedValid)
    {
        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option { Label = "Name" },
            DisallowedCharactersPattern = @"[<>]",
            Value = value
        };

        viewModel.Validate();

        (viewModel.ValidationErrors.Count == 0).Should().Be(expectedValid);
    }

    [Fact]
    public void StringViewModel_ShouldUseLocalizedMessage_WhenValueContainsDisallowedCharacters()
    {
        _mockLocalizer
            .Setup(x => x["Validation_Settings_DisallowedCharacters", It.IsAny<object[]>()])
            .Returns((string key, object[] args) =>
                new LocalizedString(key, $"localized character error for {args[0]}"));

        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option { Label = "Name" },
            DisallowedCharactersPattern = @"[<>]",
            Value = "invalid<value"
        };

        viewModel.Validate();

        viewModel.ValidationErrors.Should().ContainSingle("localized character error for Name");
    }

    [Theory]
    [InlineData("     test string     ", "test string")]
    public void StringViewModel_ShouldRemoveWhitespaces_WhenValueIsChanged(string inputValue, string expectedValue)
    {
        var viewModel = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Value = inputValue
        };

        Assert.Equal(expectedValue, viewModel.Value);
    }

    private static List<Page> GenerateTestPages(int numPages, OptionType type, string prefix, string property)
    {
        var pages = new List<Page>();

        for (var i = 0; i < numPages; i++)
        {
            var page = new Page()
            {
                Children = new List<SettingsChildItem>()
                {
                    GenerateTestOption(type, prefix, i, property)
                }
            };
            pages.Add(page);
        }

        return pages;
    }

    private static Option GenerateTestOption(OptionType type, string prefix, int id, string property)
    {
        var key = $"{prefix}:{id}:{property}"; ;
        string value;
        switch (type)
        {
            case OptionType.Boolean:
                value = "True";
                break;
            case OptionType.String:
                value = $"value_{id}";
                break;
            case OptionType.Date:
                value = "2022-01-01T00:00:00Z";
                break;
            case OptionType.Integer:
            case OptionType.Double:
            case OptionType.Choice:
            default:
                value = $"{id}";
                break;
        }

        return new Option()
        {
            Key = key,
            Value = value,
        };
    }
}
