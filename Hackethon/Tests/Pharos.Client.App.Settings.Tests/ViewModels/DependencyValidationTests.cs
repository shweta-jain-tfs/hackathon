using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.ViewModels;

/// <summary>
/// Tests for dependency-based enable/disable logic in <see cref="SettingsViewModelBase"/>.
/// Uses <c>[Theory]</c>/<c>[InlineData]</c> to cover every setting type that can act as a
/// dependency controller: <see cref="ChoicesViewModel"/>, <see cref="BoolViewModel"/>,
/// <see cref="StringViewModel"/>, <see cref="IntegerViewModel"/>,
/// <see cref="DoubleViewModel"/>, <see cref="DateViewModel"/>, and <see cref="TimeViewModel"/>.
/// </summary>
public class DependencyValidationTests
{
    private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer = new();
    private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService = new();

    // -------------------------------------------------------------------------
    // Factory: creates a controller of any supported type
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="SettingsViewModelBase"/>-derived controller whose
    /// <c>Value</c> property is set to <paramref name="initialValue"/> (using the
    /// string representation each type expects for dependency matching).
    /// </summary>
    private SettingsViewModelBase CreateController(string type, string key, string initialValue)
    {
        return type switch
        {
            "Choices" => new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = new Option { Key = key },
                Value = initialValue
            },
            "Bool" => new BoolViewModel(_mockLocalizer.Object)
            {
                Model = new Option { Key = key },
                Value = bool.Parse(initialValue)
            },
            "String" => new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = new Option { Key = key },
                Value = initialValue
            },
            "Integer" => new IntegerViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = new Option { Key = key },
                Value = int.Parse(initialValue, CultureInfo.InvariantCulture)
            },
            "Double" => new DoubleViewModel(_mockLocalizer.Object)
            {
                Model = new Option { Key = key },
                Value = double.Parse(initialValue, CultureInfo.InvariantCulture)
            },
            "Date" => new DateViewModel(_mockLocalizer.Object)
            {
                Model = new Option { Key = key },
                Value = DateTimeOffset.Parse(initialValue, CultureInfo.InvariantCulture)
            },
            "Time" => new TimeViewModel(_mockLocalizer.Object)
            {
                Model = new Option { Key = key },
                Value = TimeOnly.Parse(initialValue, CultureInfo.InvariantCulture)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown controller type")
        };
    }

    /// <summary>
    /// Updates the <c>Value</c> property on a controller created by <see cref="CreateController"/>.
    /// </summary>
    private static void SetControllerValue(SettingsViewModelBase controller, string type, string newValue)
    {
        switch (type)
        {
            case "Choices": ((ChoicesViewModel)controller).Value = newValue; break;
            case "Bool": ((BoolViewModel)controller).Value = bool.Parse(newValue); break;
            case "String": ((StringViewModel)controller).Value = newValue; break;
            case "Integer": ((IntegerViewModel)controller).Value = int.Parse(newValue, CultureInfo.InvariantCulture); break;
            case "Double": ((DoubleViewModel)controller).Value = double.Parse(newValue, CultureInfo.InvariantCulture); break;
            case "Date": ((DateViewModel)controller).Value = DateTimeOffset.Parse(newValue, CultureInfo.InvariantCulture); break;
            case "Time": ((TimeViewModel)controller).Value = TimeOnly.Parse(newValue, CultureInfo.InvariantCulture); break;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown controller type");
        }
    }

    private ChoicesViewModel CreateDependent(string controllerKey, params string[] activatingValues)
    {
        var dependencies = activatingValues
            .Select(v => new Dependency { MainOptionKey = controllerKey, MainOptionValue = v })
            .ToList();

        return new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option { Key = "dependent:key", Dependency = dependencies }
        };
    }

    // -------------------------------------------------------------------------
    // No dependency — always enabled
    // -------------------------------------------------------------------------

    [Fact]
    public void IsDependencyEnabled_IsTrue_WhenNoDependencyIsDefined()
    {
        var dependent = new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option { Key = "dependent:key" },
            DependencyViewModel = []
        };

        dependent.IsDependencyEnabled.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Single activating value — all controller types
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Choices", "ValueA", "ValueA", true)]
    [InlineData("Choices", "ValueB", "ValueA", false)]
    [InlineData("Bool", "True", "True", true)]
    [InlineData("Bool", "False", "True", false)]
    [InlineData("Bool", "False", "False", true)]
    [InlineData("Bool", "True", "False", false)]
    [InlineData("String", "hello", "hello", true)]
    [InlineData("String", "world", "hello", false)]
    [InlineData("Integer", "42", "42", true)]
    [InlineData("Integer", "99", "42", false)]
    [InlineData("Double", "3.14", "3.14", true)]
    [InlineData("Double", "2.71", "3.14", false)]
    [InlineData("Date", "2025-06-15", "2025-06-15", true)]
    [InlineData("Date", "2025-01-01", "2025-06-15", false)]
    [InlineData("Time", "14:30", "14:30", true)]
    [InlineData("Time", "08:00", "14:30", false)]
    public void IsDependencyEnabled_SingleActivatingValue(
        string type, string controllerValue, string activatingValue, bool expected)
    {
        var controller = CreateController(type, "controller:key", controllerValue);
        var dependent = CreateDependent("controller:key", activatingValue);

        dependent.DependencyViewModel = [controller];

        dependent.IsDependencyEnabled.Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // Multiple activating values (OR semantics) — the bug scenario
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Choices", "ValueA", "ValueA", "ValueB", true)]
    [InlineData("Choices", "ValueB", "ValueA", "ValueB", true)]
    [InlineData("Choices", "ValueX", "ValueA", "ValueB", false)]
    [InlineData("Bool", "True", "True", "False", true)]
    [InlineData("Bool", "False", "True", "False", true)]
    [InlineData("String", "beta", "alpha", "beta", true)]
    [InlineData("String", "gamma", "alpha", "beta", false)]
    [InlineData("Integer", "2", "1", "2", true)]
    [InlineData("Integer", "3", "1", "2", false)]
    [InlineData("Double", "2.71", "3.14", "2.71", true)]
    [InlineData("Double", "1.0", "3.14", "2.71", false)]
    [InlineData("Date", "2025-12-25", "2025-06-15", "2025-12-25", true)]
    [InlineData("Date", "2025-03-01", "2025-06-15", "2025-12-25", false)]
    [InlineData("Time", "08:00", "14:30", "08:00", true)]
    [InlineData("Time", "12:00", "14:30", "08:00", false)]
    public void IsDependencyEnabled_MultipleActivatingValues(
        string type, string controllerValue, string val1, string val2, bool expected)
    {
        var controller = CreateController(type, "controller:key", controllerValue);
        var dependent = CreateDependent("controller:key", val1, val2);

        dependent.DependencyViewModel = [controller];

        dependent.IsDependencyEnabled.Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // Three activating values — verifies third value is also evaluated
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Choices", "ValueC", "ValueA", "ValueB", "ValueC", true)]
    [InlineData("Choices", "ValueX", "ValueA", "ValueB", "ValueC", false)]
    [InlineData("Integer", "3", "1", "2", "3", true)]
    [InlineData("Integer", "4", "1", "2", "3", false)]
    public void IsDependencyEnabled_ThreeActivatingValues(
        string type, string controllerValue, string val1, string val2, string val3, bool expected)
    {
        var controller = CreateController(type, "controller:key", controllerValue);
        var dependent = CreateDependent("controller:key", val1, val2, val3);

        dependent.DependencyViewModel = [controller];

        dependent.IsDependencyEnabled.Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // Reactive update: controller value changes after DependencyViewModel is set
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Choices", "ValueX", "ValueA", "ValueB", "ValueA", true)]
    [InlineData("Choices", "ValueX", "ValueA", "ValueB", "ValueB", true)]
    [InlineData("Choices", "ValueA", "ValueA", "ValueB", "ValueX", false)]
    [InlineData("Bool", "False", "True", "False", "True", true)]
    [InlineData("Bool", "True", "True", "False", "False", true)]
    [InlineData("String", "x", "hello", "world", "world", true)]
    [InlineData("String", "hello", "hello", "world", "x", false)]
    [InlineData("Integer", "0", "1", "2", "2", true)]
    [InlineData("Integer", "1", "1", "2", "0", false)]
    [InlineData("Double", "0", "3.14", "2.71", "2.71", true)]
    [InlineData("Date", "2025-01-01", "2025-06-15", "2025-12-25", "2025-12-25", true)]
    [InlineData("Time", "00:00", "14:30", "08:00", "08:00", true)]
    public void IsDependencyEnabled_ReactiveUpdate(
        string type, string initialValue, string val1, string val2, string newValue, bool expectedAfter)
    {
        var controller = CreateController(type, "controller:key", initialValue);
        var dependent = CreateDependent("controller:key", val1, val2);
        dependent.DependencyViewModel = [controller];

        SetControllerValue(controller, type, newValue);

        dependent.IsDependencyEnabled.Should().Be(expectedAfter);
    }

    // -------------------------------------------------------------------------
    // Multiple independent controllers — AND across controllers (same type)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Choices", "ValueA", "ValueA", "ValueB", "ValueB", true)]
    [InlineData("Choices", "ValueA", "ValueA", "ValueX", "ValueB", false)]
    [InlineData("Bool", "True", "True", "False", "False", true)]
    [InlineData("Bool", "True", "True", "True", "False", false)]
    [InlineData("Integer", "1", "1", "2", "2", true)]
    [InlineData("Integer", "1", "1", "3", "2", false)]
    public void IsDependencyEnabled_TwoControllersSameType_And(
        string type,
        string valueA, string expectedA,
        string valueB, string expectedB,
        bool expected)
    {
        var controllerA = CreateController(type, "ctrl:a", valueA);
        var controllerB = CreateController(type, "ctrl:b", valueB);

        var dependent = new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option
            {
                Key = "dependent:key",
                Dependency =
                [
                    new Dependency { MainOptionKey = "ctrl:a", MainOptionValue = expectedA },
                    new Dependency { MainOptionKey = "ctrl:b", MainOptionValue = expectedB }
                ]
            },
            DependencyViewModel = [controllerA, controllerB]
        };

        dependent.IsDependencyEnabled.Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // Mixed controller types — AND across different types
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Choices", "ValueA", "ValueA", "Bool", "True", "True", true)]
    [InlineData("Choices", "ValueA", "ValueA", "Bool", "False", "True", false)]
    [InlineData("Bool", "True", "True", "Integer", "42", "42", true)]
    [InlineData("Bool", "True", "True", "Integer", "99", "42", false)]
    [InlineData("String", "hello", "hello", "Double", "3.14", "3.14", true)]
    [InlineData("String", "hello", "hello", "Double", "2.71", "3.14", false)]
    [InlineData("Date", "2025-06-15", "2025-06-15", "Time", "14:30", "14:30", true)]
    [InlineData("Date", "2025-06-15", "2025-06-15", "Time", "08:00", "14:30", false)]
    [InlineData("Integer", "42", "42", "Choices", "Alpha", "Alpha", true)]
    [InlineData("Integer", "42", "42", "Choices", "Beta", "Alpha", false)]
    public void IsDependencyEnabled_TwoControllersMixedTypes_And(
        string typeA, string valueA, string expectedA,
        string typeB, string valueB, string expectedB,
        bool expected)
    {
        var controllerA = CreateController(typeA, "ctrl:a", valueA);
        var controllerB = CreateController(typeB, "ctrl:b", valueB);

        var dependent = new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option
            {
                Key = "dependent:key",
                Dependency =
                [
                    new Dependency { MainOptionKey = "ctrl:a", MainOptionValue = expectedA },
                    new Dependency { MainOptionKey = "ctrl:b", MainOptionValue = expectedB }
                ]
            },
            DependencyViewModel = [controllerA, controllerB]
        };

        dependent.IsDependencyEnabled.Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // AND + OR combined: multiple controllers, each with multiple activating values
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Choices", "V2", "Integer", "2", true)]
    [InlineData("Choices", "V1", "Integer", "3", true)]
    [InlineData("Choices", "VX", "Integer", "2", false)]
    [InlineData("Choices", "V1", "Integer", "99", false)]
    public void IsDependencyEnabled_AndOrCombined_MixedTypes(
        string typeA, string valueA,
        string typeB, string valueB,
        bool expected)
    {
        var controllerA = CreateController(typeA, "ctrl:a", valueA);
        var controllerB = CreateController(typeB, "ctrl:b", valueB);

        var dependent = new ChoicesViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option
            {
                Key = "dependent:key",
                Dependency =
                [
                    new Dependency { MainOptionKey = "ctrl:a", MainOptionValue = "V1" },
                    new Dependency { MainOptionKey = "ctrl:a", MainOptionValue = "V2" },
                    new Dependency { MainOptionKey = "ctrl:b", MainOptionValue = "2" },
                    new Dependency { MainOptionKey = "ctrl:b", MainOptionValue = "3" }
                ]
            },
            DependencyViewModel = [controllerA, controllerB]
        };

        dependent.IsDependencyEnabled.Should().Be(expected);
    }
}
