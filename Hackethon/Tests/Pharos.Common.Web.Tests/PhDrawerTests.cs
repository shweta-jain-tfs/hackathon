using Bunit;
using Pharos.Common.Web.Components.Drawer;

namespace Pharos.Common.Web.Tests
{


    public class PhDrawerTests : BunitContext
    {
        [Fact]
        public void PhDrawer_Should_Render_Correctly()
        {
            // Arrange
            var cut = Render<PhDrawer>(parameters => parameters
                .Add(p => p.Name, "TestDrawer")
                .Add(p => p.DoorState, PhDrawerLockState.ClosedLocked)
                .Add(p => p.IsButtonVisible, true)
                .AddChildContent("<div>Child Content</div>")
            );

            // Act
            var drawer = cut.Find(".ph-drawer");

            // Assert
            Assert.NotNull(drawer);
            Assert.DoesNotContain("ph-drawer__left", drawer.ClassList);
        }

        [Fact]
        public void PhDrawer_Should_Set_CssClass_Based_On_DoorState()
        {
            // Arrange
            var cut = Render<PhDrawer>(parameters => parameters
                .Add(p => p.DoorState, PhDrawerLockState.UnlockInProgress)
                .Add(p => p.IsButtonVisible, false)
                .Add(p => p.HasManageSamplePermission, true)
            );

            // Act
            var led = cut.Find(".ph-drawer__indicator-led");

            // Assert
            Assert.Contains("ph-drawer__indicator-led-animation", led.ClassList);
        }

        [Trait("ProductRequirement", "PR_304")]
        [Trait("ModuleRequirement", "MR_5374")]
        [Fact]
        public void PhDrawer_Should_Set_Button_Visibility_Based_On_DoorState()
        {
            // Arrange
            var cut = Render<PhDrawer>(parameters => parameters
                .Add(p => p.DoorState, PhDrawerLockState.ClosedLocked)
            );

            // Act
            var button = cut.Find(".ph-drawer__action-container");

            // Assert
            Assert.NotNull(button);
        }

        [Fact]
        public void Barcode_Should_Have_Font_Size_20px()
        {
            // Arrange
            using var ctx = new BunitContext();
            var barcode = "1234";
            var sampleTubes = new List<SampleTube>
             {
                 new() { Status = TubeStatus.Empty, SampleId = "S1", Position = 1 },
                 new() { Status = TubeStatus.Empty, SampleId = "S2", Position = 2 }
             };

            // Act
            var component = ctx.Render<PhSampleTray>(parameters => parameters
                .Add(p => p.Barcode, barcode)
                .Add(p => p.SampleTubes, sampleTubes)
                .Add(p => p.BarcodePosition, TrayBarcodePosition.Right)
            );

            var barcodeContainer = component.Find(".ph-tray__barcode-container");
            barcodeContainer.OuterHtml.Should().NotContain("font-size: 20px",
                            "The barcode font-size should be set by a CSS variable, not hardcoded to 20px");

        }
    }
}

