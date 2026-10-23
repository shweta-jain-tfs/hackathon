using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.App.Settings.TestHelpers;

namespace Pharos.Client.App.Settings.ViewModels
{
    public class PageViewModelTests
    {
        private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer;
        private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService = new();
        private readonly PageViewModel _viewModel;

        public PageViewModelTests()
        {
            _mockLocalizer = LocalizerMockFactory.Create();

            _viewModel = new PageViewModel(_mockLocalizer.Object);
        }

        [Fact]
        public void Save_ItemsSaved_IsModifiedSetToFalse()
        {
            // Arrange
            var itemMock1 = new Mock<SettingsViewModelBase>(_mockLocalizer.Object);
            var itemMock2 = new Mock<SettingsViewModelBase>(_mockLocalizer.Object);

            var items = new List<SettingsViewModelBase> { itemMock1.Object, itemMock2.Object };
            _viewModel.Items = items;
            _viewModel.IsModified = true;

            // Act
            var result = _viewModel.Save();

            // Assert
            itemMock1.Verify(m => m.Save(), Times.Once);
            itemMock2.Verify(m => m.Save(), Times.Once);
            Assert.False(_viewModel.IsModified);
            Assert.True(result);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_3642")]
        public void Save_DisabledDependentItemResetToDefault_KeepsDefaultValuesInModel()
        {
            var controllingOption = new Option
            {
                Key = "Lis:LisChannels:0:ImportSettings:PollForRequests",
                Label = "Poll for requests",
                Value = "True",
                DefaultValue = "False"
            };
            var dependentOption = new Option
            {
                Key = "Lis:LisChannels:0:ImportSettings:Frequency",
                Label = "Frequency",
                Value = "10",
                DefaultValue = "5"
            };
            var controllingItem = new BoolViewModel(_mockLocalizer.Object)
            {
                Model = controllingOption,
                OriginalValue = "True",
                Value = true
            };
            var dependentItem = new IntegerViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = dependentOption,
                OriginalValue = "10",
                Value = 10
            };
            _viewModel.Items = [controllingItem, dependentItem];
            _viewModel.IsModified = true;

            controllingItem.ResetToDefault();
            dependentItem.ResetToDefault();
            dependentItem.IsDependencyEnabled = false;

            var result = _viewModel.Save();

            result.Should().BeTrue();
            controllingOption.Value.Should().Be("False");
            dependentOption.Value.Should().Be("5");
        }

        [Fact]
        public void Items_SetItems_PropertyChangedEventSubscribed()
        {
            // Arrange
            var itemMock1 = new Mock<IntegerViewModel>(_mockLocalizer.Object, _mockDataService.Object);
            var itemMock2 = new Mock<StringViewModel>(_mockLocalizer.Object, _mockDataService.Object);

            var items = new List<SettingsViewModelBase> { itemMock1.Object, itemMock2.Object };
            _viewModel.Items = items;

            var propertyChangedRaised1 = false;
            var propertyChangedRaised2 = false;

            itemMock1.Object.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(itemMock1.Object.IsModified))
                {
                    propertyChangedRaised1 = true;
                }
            };

            itemMock2.Object.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(itemMock2.Object.IsModified))
                {
                    propertyChangedRaised2 = true;
                }
            };
            // Act
            itemMock1.Object.Value = 2;
            itemMock2.Object.Value = "New value";

            // Assert
            Assert.True(propertyChangedRaised1);
            Assert.True(itemMock1.Object.IsModified);
            Assert.True(propertyChangedRaised2);
            Assert.True(itemMock2.Object.IsModified);
        }

        [Fact]
        public void ItemOnPropertyChanged_IsModifiedChanged()
        {
            // Arrange
            var itemMock = new Mock<SettingsViewModelBase>(_mockLocalizer.Object);

            var items = new List<SettingsViewModelBase> { itemMock.Object };
            _viewModel.Items = items;

            // Act
            var propertyChangedRaised = false;
            itemMock.Object.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(itemMock.Object.IsModified))
                {
                    propertyChangedRaised = true;
                }
            };

            itemMock.Object.IsModified = true;

            // Assert
            Assert.True(propertyChangedRaised);
            Assert.True(_viewModel.IsModified);
        }

        [Fact]
        public void Items_SetRequiredStringItem_DoesNotValidateUntilPageValidate()
        {
            var option = new Option
            {
                Key = "Lis:LisChannels:0:Name",
                Label = "Name",
                Required = true,
            };
            var item = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = option,
                Value = string.Empty,
            };

            _viewModel.Items = [item];

            item.ValidationErrors.Should().BeEmpty();

            _viewModel.Validate();

            item.ValidationErrors.Should().NotBeEmpty();
            _viewModel.IsValid.Should().BeFalse();
        }

        [Fact]
        public void ClearValidationErrors_ClearsNestedValidationErrors()
        {
            var option = new Option
            {
                Key = "Lis:LisChannels:0:Name",
                Label = "Name",
                Required = true,
            };
            var item = new StringViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = option,
                Value = string.Empty,
            };
            var group = new GroupViewModel(_mockLocalizer.Object)
            {
                Items = [item]
            };
            _viewModel.Items = [group];

            _viewModel.Validate();

            item.ValidationErrors.Should().NotBeEmpty();
            group.IsValid.Should().BeFalse();
            _viewModel.IsValid.Should().BeFalse();

            _viewModel.ClearValidationErrors();

            item.ValidationErrors.Should().BeEmpty();
            item.IsValid.Should().BeTrue();
            group.IsValid.Should().BeTrue();
            _viewModel.IsValid.Should().BeTrue();
        }

        [Trait("ProductRequirement", "PR_1919")]
        [Trait("ModuleRequirement", "MR_3926")]
        [Fact]
        public void ApplyServerValidationErrors_AddsLocalizedErrorsToMatchingNestedOptions()
        {
            var option = new Option
            {
                Key = "Samples:SpecialSampleHandling:SampleIdStartPosition",
                Label = "Start position",
            };
            var item = new IntegerViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = option,
                Value = 1,
            };
            var group = new GroupViewModel(_mockLocalizer.Object)
            {
                Items = [item]
            };
            _viewModel.Items = [group];

            _viewModel.ApplyServerValidationErrors(
            [
                new FailedSettingUpdateError
                {
                    Key = option.Key,
                    Value = item.ValueAsString,
                    Error = SettingUpdateError.SectionsOverlap
                }
            ]);

            item.ValidationErrors.Should().Contain("SettingUpdateError_SectionsOverlap");
            _viewModel.IsValid.Should().BeFalse();
        }

        [Trait("ProductRequirement", "PR_1919")]
        [Trait("ModuleRequirement", "MR_3926")]
        [Fact]
        public void Validate_WhenSpecialSampleHandlingSectionsOverlap_AddsErrorsToRangeOptions()
        {
            var viewModel = CreateSpecialSampleHandlingPage(sampleStart: 1, sampleLength: 4, suffixStart: 4, suffixLength: 2);

            viewModel.Validate();

            GetRangeOptions(viewModel)
                .Should()
                .OnlyContain(option => option.ValidationErrors.Contains("SettingUpdateError_SectionsOverlap"));
            viewModel.IsValid.Should().BeFalse();
        }

        [Trait("ProductRequirement", "PR_1919")]
        [Trait("ModuleRequirement", "MR_3926")]
        [Fact]
        public void Validate_WhenSpecialSampleHandlingRangeIsInvalid_DoesNotAddOverlapErrors()
        {
            var viewModel = CreateSpecialSampleHandlingPage(sampleStart: 0, sampleLength: 4, suffixStart: 1, suffixLength: 2);

            viewModel.Validate();

            GetRangeOptions(viewModel)
                .Should()
                .OnlyContain(option => !option.ValidationErrors.Contains("SettingUpdateError_SectionsOverlap"));
        }

        [Trait("ProductRequirement", "PR_1919")]
        [Trait("ModuleRequirement", "MR_3926")]
        [Fact]
        public void ItemValueChanged_WhenSpecialSampleHandlingSectionsOverlap_AddsErrorsImmediately()
        {
            var viewModel = CreateSpecialSampleHandlingPage(sampleStart: 1, sampleLength: 3, suffixStart: 4, suffixLength: 2);
            viewModel.Validate();

            GetRangeOptions(viewModel)
                .Should()
                .OnlyContain(option => !option.ValidationErrors.Contains("SettingUpdateError_SectionsOverlap"));

            var sampleLength = FindOption<IntegerViewModel>(viewModel, "SampleIdLength");
            sampleLength.Value = 4;

            GetRangeOptions(viewModel)
                .Should()
                .OnlyContain(option => option.ValidationErrors.Contains("SettingUpdateError_SectionsOverlap"));
            viewModel.IsValid.Should().BeFalse();

            sampleLength.Value = 3;

            GetRangeOptions(viewModel)
                .Should()
                .OnlyContain(option => !option.ValidationErrors.Contains("SettingUpdateError_SectionsOverlap"));
            viewModel.IsValid.Should().BeTrue();
        }

        private PageViewModel CreateSpecialSampleHandlingPage(
            int sampleStart,
            int sampleLength,
            int suffixStart,
            int suffixLength)
        {
            var viewModel = new PageViewModel(_mockLocalizer.Object)
            {
                Items =
                [
                    CreateBoolOption("ExtractSampleIdFromBarcode", true),
                    CreateIntegerOption("SampleIdStartPosition", sampleStart),
                    CreateIntegerOption("SampleIdLength", sampleLength),
                    CreateBoolOption("SampleTubesWithSameSampleId", true),
                    CreateBoolOption("ExtractSampleIdSuffixFromBarcode", true),
                    CreateIntegerOption("SampleIdSuffixStartPosition", suffixStart),
                    CreateIntegerOption("SampleIdSuffixLength", suffixLength)
                ]
            };

            AddNonOverlappingRangesValidation(viewModel);
            return viewModel;
        }

        private static void AddNonOverlappingRangesValidation(PageViewModel viewModel)
        {
            var validation = CreateNonOverlappingRangesValidation();
            foreach (var key in GetRangeKeys(validation))
            {
                var option = viewModel.Items
                    .OfType<OptionViewModelBase>()
                    .Single(option => option.Key == key);
                var model = (Option)option.Model!;
                model.NonOverlappingRanges ??= [];
                model.NonOverlappingRanges.Add(validation);
            }
        }

        private static NonOverlappingRangesValidation CreateNonOverlappingRangesValidation()
        {
            return new NonOverlappingRangesValidation
            {
                FirstStartKey = CreateSpecialSampleHandlingKey("SampleIdStartPosition"),
                FirstLengthKey = CreateSpecialSampleHandlingKey("SampleIdLength"),
                SecondStartKey = CreateSpecialSampleHandlingKey("SampleIdSuffixStartPosition"),
                SecondLengthKey = CreateSpecialSampleHandlingKey("SampleIdSuffixLength"),
                Error = nameof(SettingUpdateError.SectionsOverlap),
                EnabledWhen =
                [
                    new Dependency
                    {
                        MainOptionKey = CreateSpecialSampleHandlingKey("ExtractSampleIdFromBarcode"),
                        MainOptionValue = true.ToString(),
                        Type = DependencyType.Disable
                    },
                    new Dependency
                    {
                        MainOptionKey = CreateSpecialSampleHandlingKey("SampleTubesWithSameSampleId"),
                        MainOptionValue = true.ToString(),
                        Type = DependencyType.Disable
                    },
                    new Dependency
                    {
                        MainOptionKey = CreateSpecialSampleHandlingKey("ExtractSampleIdSuffixFromBarcode"),
                        MainOptionValue = true.ToString(),
                        Type = DependencyType.Disable
                    }
                ]
            };
        }

        private static string[] GetRangeKeys(NonOverlappingRangesValidation validation)
        {
            return
            [
                validation.FirstStartKey,
                validation.FirstLengthKey,
                validation.SecondStartKey,
                validation.SecondLengthKey
            ];
        }

        private static IEnumerable<IntegerViewModel> GetRangeOptions(PageViewModel viewModel)
        {
            return
            [
                FindOption<IntegerViewModel>(viewModel, "SampleIdStartPosition"),
                FindOption<IntegerViewModel>(viewModel, "SampleIdLength"),
                FindOption<IntegerViewModel>(viewModel, "SampleIdSuffixStartPosition"),
                FindOption<IntegerViewModel>(viewModel, "SampleIdSuffixLength")
            ];
        }

        private static TOption FindOption<TOption>(PageViewModel viewModel, string keySuffix)
            where TOption : OptionViewModelBase
        {
            return viewModel.Items
                .OfType<TOption>()
                .Single(option => option.Key == CreateSpecialSampleHandlingKey(keySuffix));
        }

        private BoolViewModel CreateBoolOption(string keySuffix, bool value)
        {
            var viewModel = new BoolViewModel(_mockLocalizer.Object)
            {
                Model = new Option
                {
                    Key = CreateSpecialSampleHandlingKey(keySuffix),
                    Label = keySuffix,
                    Type = OptionType.Boolean
                },
                Value = value
            };
            viewModel.OriginalValue = viewModel.ValueAsString;
            viewModel.IsModified = false;
            return viewModel;
        }

        private IntegerViewModel CreateIntegerOption(string keySuffix, int value)
        {
            var viewModel = new IntegerViewModel(_mockLocalizer.Object, _mockDataService.Object)
            {
                Model = new Option
                {
                    Key = CreateSpecialSampleHandlingKey(keySuffix),
                    Label = keySuffix,
                    Type = OptionType.Integer
                },
                MinInt = 1,
                MaxInt = int.MaxValue,
                Value = value
            };
            viewModel.OriginalValue = viewModel.ValueAsString;
            viewModel.IsModified = false;
            return viewModel;
        }

        private static string CreateSpecialSampleHandlingKey(string keySuffix)
        {
            return $"Samples:SpecialSampleHandling:{keySuffix}";
        }
    }
}
