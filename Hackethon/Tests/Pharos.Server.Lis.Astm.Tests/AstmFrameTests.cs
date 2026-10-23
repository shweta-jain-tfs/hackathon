using System.Text;
using Pharos.Server.Lis.Astm.Application.Frames;
using Pharos.Server.Lis.Astm.Application.Interfaces;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmFrameTests
{
    [Fact]
    public void GetBytes()
    {
        var checksumCalculator = new Mock<IFrameChecksumCalculator>();
        checksumCalculator.Setup(c => c.CalculateChecksum(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<byte>())).Returns((0x10, 0x11));

        var message = "My Message";
        var frame = new AstmFrame(2, Encoding.ASCII.GetBytes(message), true, checksumCalculator.Object);
        var expectedResult = Encoding.ASCII.GetBytes($"\u00022{message}\u0017\u0010\u0011\r\n");
        frame.GetBytes().Should().BeEquivalentTo(expectedResult);
    }
}