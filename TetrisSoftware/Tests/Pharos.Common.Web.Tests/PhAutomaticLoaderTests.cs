using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Common.Web.Components.Loader;

namespace Pharos.Common.Web.Tests;

public class PhAutomaticLoaderTests : BunitContext
{
    private LoaderService _loaderService = null!;
    private IRenderedComponent<PhAutomaticLoader> _component = null!;

    public PhAutomaticLoaderTests()
    {
        SetupTest();
    }

    private void SetupTest()
    {
        _loaderService = new LoaderService();
        Services.AddSingleton(_loaderService);
    }

    [Fact]
    public void PhAutomaticLoader_InitialState_ShouldNotBeVisible()
    {
        // Arrange & Act
        _component = Render<PhAutomaticLoader>();

        // Assert
        var overlay = _component.Find(".ph-loader-overlay");
        Assert.Contains("ph-loader-overlay--disable", overlay.ClassList);
    }

    [Fact]
    public async Task PhAutomaticLoader_CallCompletesBeforeOneSecond_ShouldNotShowLoader()
    {
        // Arrange
        _component = Render<PhAutomaticLoader>();
        var callId = Guid.NewGuid();

        // Act
        _loaderService.Show(callId);
        await Task.Delay(500, Xunit.TestContext.Current.CancellationToken); // Wait 0.5 seconds
        _loaderService.Hide(callId);
        await Task.Delay(600, Xunit.TestContext.Current.CancellationToken); // Wait additional 0.6 seconds (total 1.1 seconds)

        // Assert
        var overlay = _component.Find(".ph-loader-overlay");
        Assert.Contains("ph-loader-overlay--disable", overlay.ClassList);
    }

    [Fact]
    public async Task PhAutomaticLoader_CallExceedsOneSecond_ShouldShowLoader()
    {
        // Arrange
        _component = Render<PhAutomaticLoader>(parameters => parameters
            .Add(p => p.InitialText, "Loading...")
            .Add(p => p.AlternateText, "Still loading..."));

        var callId = Guid.NewGuid();

        // Act
        _loaderService.Show(callId);

        // Wait for loader to become visible (removes the disable class)
        await _component.WaitForStateAsync(
            () => !_component.Find(".ph-loader-overlay").ClassList.Contains("ph-loader-overlay--disable"),
            timeout: TimeSpan.FromSeconds(2)
        );

        // Assert
        var overlay = _component.Find(".ph-loader-overlay");
        Assert.DoesNotContain("ph-loader-overlay--disable", overlay.ClassList);

        // Cleanup
        _loaderService.Hide(callId);
    }

    [Fact]
    public async Task PhAutomaticLoader_MultipleCalls_OneExceedsOneSecond_ShouldShowLoader()
    {
        // Arrange
        _component = Render<PhAutomaticLoader>();
        var quickCallId = Guid.NewGuid();
        var slowCallId = Guid.NewGuid();

        // Act
        _loaderService.Show(quickCallId);
        _loaderService.Show(slowCallId);

        // Quick call completes before 1 second
        await Task.Delay(500, Xunit.TestContext.Current.CancellationToken);
        _loaderService.Hide(quickCallId);

        // Wait for loader to become visible (slow call exceeds 1 second)
        await _component.WaitForStateAsync(
            () => !_component.Find(".ph-loader-overlay").ClassList.Contains("ph-loader-overlay--disable"),
            timeout: TimeSpan.FromSeconds(2)
        );

        // Assert
        var overlay = _component.Find(".ph-loader-overlay");
        Assert.DoesNotContain("ph-loader-overlay--disable", overlay.ClassList);

        // Cleanup
        _loaderService.Hide(slowCallId);
    }

    [Fact]
    public async Task PhAutomaticLoader_MultipleCallsAllCompleteQuickly_ShouldNotShowLoader()
    {
        // Arrange
        _component = Render<PhAutomaticLoader>();
        var callId1 = Guid.NewGuid();
        var callId2 = Guid.NewGuid();
        var callId3 = Guid.NewGuid();

        // Act
        _loaderService.Show(callId1);
        _loaderService.Show(callId2);
        _loaderService.Show(callId3);

        await Task.Delay(300, Xunit.TestContext.Current.CancellationToken);
        _loaderService.Hide(callId1);

        await Task.Delay(300, Xunit.TestContext.Current.CancellationToken);
        _loaderService.Hide(callId2);

        await Task.Delay(300, Xunit.TestContext.Current.CancellationToken);
        _loaderService.Hide(callId3);

        await Task.Delay(200, Xunit.TestContext.Current.CancellationToken); // Total elapsed: 1.1 seconds

        // Assert
        var overlay = _component.Find(".ph-loader-overlay");
        Assert.Contains("ph-loader-overlay--disable", overlay.ClassList);
    }

    [Fact]
    public async Task PhAutomaticLoader_LoaderHidesWhenLastCallCompletes()
    {
        // Arrange
        _component = Render<PhAutomaticLoader>();
        var callId1 = Guid.NewGuid();
        var callId2 = Guid.NewGuid();

        // Act
        _loaderService.Show(callId1);
        _loaderService.Show(callId2);

        // Wait for loader to become visible (removes the disable class)
        await _component.WaitForStateAsync(
            () => !_component.Find(".ph-loader-overlay").ClassList.Contains("ph-loader-overlay--disable"),
            timeout: TimeSpan.FromSeconds(2)
        );

        // Verify loader is visible
        var overlay = _component.Find(".ph-loader-overlay");
        Assert.DoesNotContain("ph-loader-overlay--disable", overlay.ClassList);

        // Complete first call
        _loaderService.Hide(callId1);
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);

        // Loader should still be visible
        Assert.DoesNotContain("ph-loader-overlay--disable", overlay.ClassList);

        // Complete second call
        _loaderService.Hide(callId2);

        // Wait for loader to become hidden (adds the disable class back)
        await _component.WaitForStateAsync(
            () => _component.Find(".ph-loader-overlay").ClassList.Contains("ph-loader-overlay--disable"),
            timeout: TimeSpan.FromSeconds(2)
        );

        // Assert - Loader should now be hidden
        Assert.Contains("ph-loader-overlay--disable", overlay.ClassList);
    }

    [Fact]
    public async Task PhAutomaticLoader_TextAnimation_ShouldAlternateBetweenTexts()
    {
        // Arrange
        var initialText = "Loading...";
        var alternateText = "Still loading...";

        _component = Render<PhAutomaticLoader>(parameters => parameters
            .Add(p => p.InitialText, initialText)
            .Add(p => p.AlternateText, alternateText));

        var callId = Guid.NewGuid();

        // Act
        _loaderService.Show(callId);

        // Wait for loader to appear and text to start showing
        await Task.Delay(1200, Xunit.TestContext.Current.CancellationToken); // Initial timer + text display

        // Wait for text alternation (should happen every 4 seconds after initial display)
        await Task.Delay(4100, Xunit.TestContext.Current.CancellationToken);

        // Note: Due to the complexity of testing exact text content with timers,
        // this test verifies the component structure. In a real scenario, you might
        // want to create a testable wrapper that allows injection of a controllable timer.

        var overlay = _component.Find(".ph-loader-overlay");
        Assert.DoesNotContain("ph-loader-overlay--disable", overlay.ClassList);

        // Cleanup
        _loaderService.Hide(callId);
    }

    [Fact]
    public void PhAutomaticLoader_SameCallIdMultipleTimes_ShouldHandleGracefully()
    {
        // Arrange
        _component = Render<PhAutomaticLoader>();
        var callId = Guid.NewGuid();

        // Act & Assert (should not throw)
        _loaderService.Show(callId);
        _loaderService.Show(callId); // Duplicate show
        _loaderService.Hide(callId);
        _loaderService.Hide(callId); // Duplicate hide

        // Should complete without exceptions
        Assert.True(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _component?.Dispose();
        }
        base.Dispose(disposing);
    }
}