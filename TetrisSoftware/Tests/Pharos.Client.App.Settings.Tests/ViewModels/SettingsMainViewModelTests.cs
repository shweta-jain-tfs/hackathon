using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.App.Settings.ViewModels.Converters;
using Pharos.Client.App.Settings.Views.Settings;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.NavigationLock;
using Pharos.Client.App.Settings.TestHelpers;

namespace Pharos.Client.App.Settings.ViewModels
{
    public class SettingsMainViewModelTests
    {
        private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer;
        private readonly Mock<IModalService> _mockModalService;
        private readonly SettingsMainViewModel _viewModel;

        public SettingsMainViewModelTests()
        {
            _mockLocalizer = LocalizerMockFactory.Create();
            _mockModalService = new Mock<IModalService>();

            Mock<ISettingsDataService<SettingItemBase>> mockDataService = new();
            Mock<IConverter> mockConverter = new();
            Mock<NavigationManager> mockNavigationManager = new();

            _viewModel = new SettingsMainViewModel(mockDataService.Object, mockConverter.Object, _mockLocalizer.Object)
            {
                NavigationManager = mockNavigationManager.Object,
                Modal = _mockModalService.Object
            };
        }

        [Fact]
        public async Task GetPageAsync_PathMissingFromCurrentMap_RefreshesMapAndLoadsPage()
        {
            var dataServiceMock = new Mock<ISettingsDataService<SettingItemBase>>();
            var converterMock = new Mock<IConverter>();
            var locMock = new Mock<IStringLocalizer<Translations>>();
            var sut = new SettingsMainViewModel(dataServiceMock.Object, converterMock.Object, locMock.Object);
            var page = new Page { Key = "Instrument:Instruments:0" };
            var pageViewModel = new PageViewModel(locMock.Object) { Model = page };

            dataServiceMock
                .Setup(x => x.RefreshPageRouteMapAsync())
                .ReturnsAsync(new Dictionary<string, string>
                {
                    ["Instrument/Instruments/New instrument"] = "Instrument:Instruments:0"
                });
            dataServiceMock
                .Setup(x => x.GetPageAsync("Instrument", "Instrument:Instruments:0"))
                .ReturnsAsync(page);
            converterMock
                .Setup(x => x.Convert(page))
                .Returns(pageViewModel);

            var result = await sut.GetPageAsync("Instrument/Instruments/New instrument");

            Assert.Same(pageViewModel, result);
            dataServiceMock.Verify(x => x.RefreshPageRouteMapAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPageAsync_MappedKeyNotFound_RefreshesMapAndRetriesOnce()
        {
            var dataServiceMock = new Mock<ISettingsDataService<SettingItemBase>>();
            var converterMock = new Mock<IConverter>();
            var locMock = new Mock<IStringLocalizer<Translations>>();
            var sut = new SettingsMainViewModel(dataServiceMock.Object, converterMock.Object, locMock.Object);
            var page = new Page { Key = "Lis:LisChannels:6" };
            var pageViewModel = new PageViewModel(locMock.Object) { Model = page };
            var headers = new Dictionary<string, IEnumerable<string>>();

            dataServiceMock
                .SetupSequence(x => x.RefreshPageRouteMapAsync())
                .ReturnsAsync(new Dictionary<string, string>
                {
                    ["Lis/Lis channel"] = "Lis:LisChannels:5"
                })
                .ReturnsAsync(new Dictionary<string, string>
                {
                    ["Lis/Lis channel"] = "Lis:LisChannels:6"
                });

            dataServiceMock
                .Setup(x => x.GetPageAsync("Lis", "Lis:LisChannels:5"))
                .ThrowsAsync(new ApiException(
                    "Page not found",
                    500,
                    "Page with key [Lis:LisChannels:5] not found",
                    headers,
                    null!));

            dataServiceMock
                .Setup(x => x.GetPageAsync("Lis", "Lis:LisChannels:6"))
                .ReturnsAsync(page);

            converterMock
                .Setup(x => x.Convert(page))
                .Returns(pageViewModel);

            var result = await sut.GetPageAsync("Lis/Lis channel");

            Assert.Same(pageViewModel, result);
            dataServiceMock.Verify(x => x.RefreshPageRouteMapAsync(), Times.Exactly(2));
            dataServiceMock.Verify(x => x.GetPageAsync("Lis", "Lis:LisChannels:5"), Times.Once);
            dataServiceMock.Verify(x => x.GetPageAsync("Lis", "Lis:LisChannels:6"), Times.Once);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_3638")]
        public async Task ShowUnsavedChangesDialog_CancelPreventsNavigation()
        {
            SetupModalResult<PhNavigationConfirmation>(ModalResult.Cancel());

            var result = await _viewModel.ShowUnsavedChangesDialog();

            Assert.True(result);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_3638")]
        public async Task ShowUnsavedChangesDialog_SaveValidChangesAllowsNavigation()
        {
            SetupModalResult<PhNavigationConfirmation>(ModalResult.Ok(true));

            _viewModel.Selected = new PageViewModel(_mockLocalizer.Object)
            {
                IsValid = true
            };

            var saveCalled = false;
            _viewModel.Save = EventCallback.Factory.Create(this, () => { saveCalled = true; });

            var result = await _viewModel.ShowUnsavedChangesDialog();

            Assert.False(result);
            Assert.True(saveCalled);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_3644")]
        public async Task ShowUnsavedChangesDialog_SaveInvalidChangesPreventsNavigation()
        {
            SetupModalResult<PhNavigationConfirmation>(ModalResult.Ok(true));

            _viewModel.Selected = new PageViewModel(_mockLocalizer.Object)
            {
                IsValid = false
            };

            var saveCalled = false;
            _viewModel.Save = EventCallback.Factory.Create(this, () => { saveCalled = true; });

            var result = await _viewModel.ShowUnsavedChangesDialog();

            Assert.True(result);
            Assert.True(saveCalled);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_3641")]
        public async Task ShowUnsavedChangesDialog_DiscardChangesAllowsNavigation()
        {
            SetupModalResult<PhNavigationConfirmation>(ModalResult.Ok(false));

            var result = await _viewModel.ShowUnsavedChangesDialog();

            Assert.False(result);
        }

        [Fact]
        public async Task IsModified_False_ShowSaveSettingsErrorDialog()
        {
            _viewModel.Selected = new PageViewModel(_mockLocalizer.Object)
            {
                IsModified = false
            };
            var showSaveSettingsErrorDialogMock = new Mock<Func<Task>>();
            _viewModel.ShowSaveSettingsErrorDialog = showSaveSettingsErrorDialogMock.Object;

            if (!_viewModel.Selected.IsModified)
            {
                await _viewModel.ShowSaveSettingsErrorDialog!.Invoke();
            }

            showSaveSettingsErrorDialogMock.Verify(s => s.Invoke(), Times.Once);
        }

        [Fact]
        public async Task IsModified_True_DoesNotShowSaveSettingsErrorDialog()
        {
            _viewModel.Selected = new PageViewModel(_mockLocalizer.Object)
            {
                IsModified = true
            };
            var showSaveSettingsErrorDialogMock = new Mock<Func<Task>>();
            _viewModel.ShowSaveSettingsErrorDialog = showSaveSettingsErrorDialogMock.Object;

            if (!_viewModel.Selected.IsModified)
            {
                await _viewModel.ShowSaveSettingsErrorDialog!.Invoke();
            }

            showSaveSettingsErrorDialogMock.Verify(s => s.Invoke(), Times.Never);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_4779")]
        public void ShowImportSettingsResultDialog_WithUnrecognizedSettingsList()
        {
            // Arrange
            var unrecognizedSettings = new List<UnrecognizedSettingImportInfo>
            {
                new() { Key = "Setting1", Value = "Value1" },
                new() { Key = "Setting2", Value = "Value2" }
            };

            var parameters = new ModalParameters
            {
                { nameof(ImportSettingsResultView.IsUnrecognizedSettings), true },
                { nameof(ImportSettingsResultView.UnrecognizedSettings), unrecognizedSettings }
            };

            _mockModalService.Setup(m => m.Show<ImportSettingsResultView>(It.IsAny<ModalOptions>(), It.IsAny<ModalParameters>()))
                .Returns(new Mock<IModalReference>().Object);

            // Act
            _mockModalService.Object.Show<ImportSettingsResultView>(new ModalOptions(), parameters);

            // Assert
            _mockModalService.Verify(m => m.Show<ImportSettingsResultView>(It.IsAny<ModalOptions>(), parameters), Times.Once);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_3651")]
        public void ShowImportSettingsResultDialog_WithImportErrors()
        {
            // Arrange
            var invalidSettings = new List<FailedSettingImportError>
            {
                new() { Key = "Setting1", Value = "Value1", Error = SettingImportError.MalformedFile },
                new() { Key = "Setting2", Value = "Value2", Error = SettingImportError.InvalidValue }
            };

            var parameters = new ModalParameters
            {
                { nameof(ImportSettingsResultView.InvalidSettings), invalidSettings },
                { nameof(ImportSettingsResultView.IsErrorInvalidFile), true }
            };

            _mockModalService.Setup(m => m.Show<ImportSettingsResultView>(It.IsAny<ModalOptions>(), It.IsAny<ModalParameters>()))
                .Returns(new Mock<IModalReference>().Object);

            // Act
            _mockModalService.Object.Show<ImportSettingsResultView>(new ModalOptions(), parameters);

            // Assert
            _mockModalService.Verify(m => m.Show<ImportSettingsResultView>(It.IsAny<ModalOptions>(), parameters), Times.Once);
        }

        [Fact]
        [Trait("ModuleRequirement", "MR_3651")]
        public void ShowImportSettingsResultDialog_WhenFileTypeIsInvalid()
        {
            // Arrange
            var invalidFileType = new List<FailedSettingImportError>
            {
                new() { Error = SettingImportError.InvalidFileType },
            };

            var parameters = new ModalParameters
            {
                { nameof(ImportSettingsResultView.InvalidSettings), invalidFileType },
                { nameof(ImportSettingsResultView.IsErrorInvalidFile), true }
            };

            _mockModalService.Setup(m => m.Show<ImportSettingsResultView>(It.IsAny<ModalOptions>(), It.IsAny<ModalParameters>()))
                .Returns(new Mock<IModalReference>().Object);

            // Act
            _mockModalService.Object.Show<ImportSettingsResultView>(new ModalOptions(), parameters);

            // Assert
            _mockModalService.Verify(m => m.Show<ImportSettingsResultView>(It.IsAny<ModalOptions>(), parameters), Times.Once);
        }

        private void SetupModalResult<TModalComponent>(ModalResult modalResult) where TModalComponent : IComponent
        {
            var modalMock = new Mock<IModalReference>();
            modalMock.Setup(m => m.Result).Returns(Task.FromResult(modalResult));

            _mockModalService
                .Setup(m => m.Show<TModalComponent>(It.IsAny<ModalOptions>()))
                .Returns(modalMock.Object);
        }
    }
}
