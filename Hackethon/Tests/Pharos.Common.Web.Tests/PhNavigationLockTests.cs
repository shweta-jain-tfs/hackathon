using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pharos.Common.Web.Components.NavigationLock;
using Pharos.Common.Web.Components.Modal;
using System.Reflection;
using Microsoft.AspNetCore.Components.Routing;

namespace Pharos.Common.Web.Tests;

public class PhNavigationLockTests : BunitContext
{
    private readonly Mock<IModalService> _modalServiceMock;
    private readonly Mock<IModalReference> _modalReferenceMock;

    public PhNavigationLockTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        _modalReferenceMock = new Mock<IModalReference>();

        Services.AddSingleton<IModalService>(_modalServiceMock.Object);
    }

    private void SetupModalResult(ModalResult result)
    {
        _modalReferenceMock.Setup(m => m.Result).ReturnsAsync(result);
        _modalServiceMock.Setup(m => m.Show<PhNavigationConfirmation>(It.IsAny<ModalOptions>()))
            .Returns(_modalReferenceMock.Object);
    }

    [Theory]
    [InlineData(null, false, true)]
    [InlineData(null, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]
    public async Task Navigation_Prevention_Based_On_ModalResult(object? data, bool canceled, bool shouldPrevent)
    {
        // Arrange
        SetupModalResult(new ModalResult(data, canceled));

        var cut = Render<PhNavigationLock>(parameters => parameters
            .Add(p => p.Modified, true)
        );

        var instance = cut.Instance;
        var method = typeof(PhNavigationLock).GetMethod("OnBeforeInternalNavigation", BindingFlags.NonPublic | BindingFlags.Instance);

        var context = new LocationChangingContext
        {
            TargetLocation = "http://test",
            IsNavigationIntercepted = true
        };

        // Act
        if (method is null)
        {
            throw new InvalidOperationException("Could not find method 'OnBeforeInternalNavigation'.");
        }
        var result = method.Invoke(instance, new object[] { context });
        if (result is not Task task)
        {
            throw new InvalidOperationException("Method did not return a Task.");
        }
        await task;

        // Assert
        Assert.Equal(shouldPrevent, context.IsNavigationIntercepted);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public async Task Navigation_Is_Allowed_Based_On_ModalResult(object? data, bool canceled, bool shouldPrevent)
    {
        // Arrange
        SetupModalResult(new ModalResult(data, canceled));

        var cut = Render<PhNavigationLock>(parameters => parameters
            .Add(p => p.Modified, true)
        );

        var instance = cut.Instance;
        var method = typeof(PhNavigationLock).GetMethod("OnBeforeInternalNavigation", BindingFlags.NonPublic | BindingFlags.Instance);

        var context = new LocationChangingContext
        {
            TargetLocation = "http://test",
            IsNavigationIntercepted = false
        };

        // Act
        if (method is null)
        {
            throw new InvalidOperationException("Could not find method 'OnBeforeInternalNavigation'.");
        }

        var result = method.Invoke(instance, new object[] { context });
        if (result is not Task task)
        {
            throw new InvalidOperationException("Method did not return a Task.");
        }
        await task;

        // Assert
        Assert.Equal(shouldPrevent, context.IsNavigationIntercepted);
    }
}




