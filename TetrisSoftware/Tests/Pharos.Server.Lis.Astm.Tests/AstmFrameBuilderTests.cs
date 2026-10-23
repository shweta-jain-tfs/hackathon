using System.Text;
using Pharos.Server.Lis.Astm.Application.Common;
using Pharos.Server.Lis.Astm.Application.Frames;
using Pharos.Server.Lis.Astm.Application.Interfaces;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmFrameBuilderTests
{
    private readonly string _moqChecksum = "\u0010\u0011";
    private readonly Mock<IFrameChecksumCalculator> _moqChecksumCalculator = new();

    public AstmFrameBuilderTests()
    {
        _moqChecksumCalculator.Setup(c => c.CalculateChecksum(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<byte>())).Returns((0x10, 0x11));
    }

    [Fact]
    public void CreateOneFrameMessage()
    {
        var frameBuilder = new AstmFrameBuilder(false, false, false, _moqChecksumCalculator.Object);

        var message = "".PadLeft(AstmFrame.MaxFrameDataLength, 'M');
        var encoding = Encoding.ASCII;

        var expectedResult = encoding.GetBytes($"\u00021{message}\u0003{_moqChecksum}\r\n");
        var frames = frameBuilder.CreateFramesFromMessage(encoding.GetBytes(message)).ToList();

        frames.Count.Should().Be(1);
        frames.First().GetBytes().Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void DataLongerThan240SplitIntoMultipleFrames()
    {
        var frameBuilder = new AstmFrameBuilder(false, false, false, _moqChecksumCalculator.Object);

        var message1 = "".PadLeft(AstmFrame.MaxFrameDataLength, 'M');
        var message2 = "".PadLeft(AstmFrame.MaxFrameDataLength, 'S');
        var encoding = Encoding.ASCII;

        var expectedFrame1 = encoding.GetBytes($"\u00021{message1}\u0017{_moqChecksum}\r\n");
        var expectedFrame2 = encoding.GetBytes($"\u00022{message2}\u0003{_moqChecksum}\r\n");
        var frames = frameBuilder.CreateFramesFromMessage(encoding.GetBytes(message1 + message2)).ToList();

        frames.Count.Should().Be(2);
        frames.First().GetBytes().Should().BeEquivalentTo(expectedFrame1);
        frames.Last().GetBytes().Should().BeEquivalentTo(expectedFrame2);
    }

    [Fact]
    public void FrameNumberingRollOverAfterNumberSeven()
    {
        var frameBuilder = new AstmFrameBuilder(false, false, false, _moqChecksumCalculator.Object);

        var veryLongMessage = "".PadLeft(2500, 'M');

        var frames = frameBuilder.CreateFramesFromMessage(Encoding.ASCII.GetBytes(veryLongMessage)).ToList();

        frames.Should().HaveCount(11);

        frames[0].FrameNumber.Should().Be(1);
        frames[6].FrameNumber.Should().Be(7);
        frames[7].FrameNumber.Should().Be(0);
        frames[8].FrameNumber.Should().Be(1);
    }

    [Fact]
    public void CreateFrameFromBytesWithoutTerminatorThrows()
    {
        var frameBuilder = new AstmFrameBuilder(false, false, false, _moqChecksumCalculator.Object);
        var rawFrame = Encoding.ASCII.GetBytes("\u00021H|\\^&");

        var act = () => frameBuilder.CreateFrameFromBytes(rawFrame, 1);

        act.Should().Throw<AstmException>();
    }

    [Fact]
    public void ChecksumCalculationTest()
    {
        DoChecksumCalculationTest("A", Encoding.UTF8, false, 55, 53);
        DoChecksumCalculationTest("A", Encoding.UTF8, true, 55, 53);
        DoChecksumCalculationTest("A", Encoding.Unicode, false, 55, 53);
        DoChecksumCalculationTest("A", Encoding.Unicode, true, 52, 52);
        DoChecksumCalculationTest("\u3400", Encoding.Unicode, false, 54, 56);
        DoChecksumCalculationTest("\u3400", Encoding.Unicode, true, 48, 51);
    }

    private static void DoChecksumCalculationTest(string message, Encoding encoding, bool calculateChecksumAsIDM, byte expectedCs0, byte expectedCs1)
    {
        IFrameChecksumCalculator checksumCalculator = calculateChecksumAsIDM ? new IdmChecksumCalculator(encoding) : new StandardChecksumCalculator();

        var frameBuilder = new AstmFrameBuilder(false, false, false, checksumCalculator);
        var frames = frameBuilder.CreateFramesFromMessage(encoding.GetBytes(message));

        var bytes = frames.First().GetBytes();

        bytes[^4].Should().Be(expectedCs0);
        bytes[^3].Should().Be(expectedCs1);
    }
}