using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.ViewModels.Converters;
public class OptionViewModelFactoryTests
{
    private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer = new();
    private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService = new();

    [Fact]
    public void CreateOptionViewModel_ShouldCreateBoolViewModel_WhenOptionTypeIsBoolean()
    {
        // Arrange
        var option = new Option
        {
            Type = OptionType.Boolean,
            Value = "True"
        };

        // Act
        var result = OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        // Assert
        result.Should().BeOfType<BoolViewModel>();
        ((BoolViewModel)result).Value.Should().BeTrue();
    }

    [Fact]
    public void CreateOptionViewModel_ShouldCreateStringViewModel_WhenOptionTypeIsString()
    {
        // Arrange
        var option = new Option
        {
            Type = OptionType.String,
            Value = "TestString"
        };

        // Act
        var result = OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        // Assert
        result.Should().BeOfType<StringViewModel>();
        ((StringViewModel)result).Value.Should().Be("TestString");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateOptionViewModel_ShouldCopyDisallowedCharactersPattern(bool isObfuscated)
    {
        var option = new Option
        {
            Type = OptionType.String,
            Value = "TestString",
            IsObfuscated = isObfuscated,
            DisallowedCharactersPattern = @"[<>]"
        };

        var result = OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        result.Should().BeAssignableTo<StringViewModel>()
            .Which.DisallowedCharactersPattern.Should().Be(@"[<>]");
    }

    [Fact]
    public void CreateOptionViewModel_ShouldCreateIntegerViewModel_WhenOptionTypeIsInteger()
    {
        // Arrange
        var option = new Option
        {
            Type = OptionType.Integer,
            Value = "123",
            MinValue = "0",
            MaxValue = "200"
        };

        // Act
        var result = OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        // Assert
        result.Should().BeOfType<IntegerViewModel>();
        ((IntegerViewModel)result).Value.Should().Be(123);
        ((IntegerViewModel)result).MinInt.Should().Be(0);
        ((IntegerViewModel)result).MaxInt.Should().Be(200);
    }

    [Fact]
    public void CreateOptionViewModel_ShouldCreateChoicesViewModel_WhenOptionTypeIsChoice()
    {
        // Arrange
        var option = new Option
        {
            Type = OptionType.Choice,
            Value = "Choice1",
            Choices = new List<Choice>
               {
                   new() { Label = "Choice 1", Value = "Choice1" },
                   new() { Label = "Choice 2", Value = "Choice2" }
               }
        };

        // Act
        var result = OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        // Assert
        result.Should().BeOfType<ChoicesViewModel>();
        ((ChoicesViewModel)result).Value.Should().Be("Choice1");
        ((ChoicesViewModel)result).Choices.Should().HaveCount(2);
    }

    [Fact]
    public void CreateOptionViewModel_ShouldCreateDateViewModel_WhenOptionTypeIsDate()
    {
        // Arrange
        var option = new Option
        {
            Type = OptionType.Date,
            Value = "2022-01-01T00:00:00Z"
        };

        // Act
        var result = OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        // Assert
        result.Should().BeOfType<DateViewModel>();
        ((DateViewModel)result).Value.Should().Be(DateTimeOffset.Parse("2022-01-01T00:00:00Z", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CreateOptionViewModel_ShouldCreateDoubleViewModel_WhenOptionTypeIsDouble()
    {
        // Arrange
        var option = new Option
        {
            Type = OptionType.Double,
            Value = "123.45",
            MinValue = "0.0",
            MaxValue = "200.0"
        };

        // Act
        var result = OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        // Assert
        result.Should().BeOfType<DoubleViewModel>();
        ((DoubleViewModel)result).Value.Should().Be(123.45);
        ((DoubleViewModel)result).MinDouble.Should().Be(0.0);
        ((DoubleViewModel)result).MaxDouble.Should().Be(200.0);
    }

    [Fact]
    public void CreateOptionViewModel_ShouldThrowNotImplementedException_WhenOptionTypeIsUnsupported()
    {
        // Arrange
        var option = new Option
        {
            Type = (OptionType)999, // Unsupported type
            Value = "Unsupported"
        };

        // Act  
        Action act = () => OptionViewModelFactory.CreateOptionViewModel(option, _mockLocalizer.Object, _mockDataService.Object);

        // Assert
        act.Should().Throw<NotImplementedException>().WithMessage($"The option type {option.Type} is not supported");
    }
}
