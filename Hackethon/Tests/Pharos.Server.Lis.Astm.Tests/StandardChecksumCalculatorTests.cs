using System.Text;
using Pharos.Server.Lis.Astm.Application.Frames;

namespace Pharos.Server.Lis.Astm.Tests;

public class StandardChecksumCalculatorTests
{
    [Fact]
    public void CalculatesChecksumCorrectly()
    {
        var calculator = new StandardChecksumCalculator();
        var message = "TEST";
        var messageBytes = Encoding.ASCII.GetBytes(message);
        var frameNumber = 1;
        var frameTerminator = (byte)0x03; // ETX

        var (cs0, cs1) = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        // Should produce "74" in ASCII
        cs0.Should().Be((byte)'7');
        cs1.Should().Be((byte)'4');
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public void HandlesFrameNumbers(int frameNumber)
    {
        var calculator = new StandardChecksumCalculator();
        var message = "M";
        var messageBytes = Encoding.ASCII.GetBytes(message);
        var frameTerminator = (byte)0x03;

        var (cs0, cs1) = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        // Should produce valid hex ASCII characters
        cs0.Should().BeInRange((byte)'0', (byte)'F');
        cs1.Should().BeInRange((byte)'0', (byte)'F');
    }

    [Fact]
    public void HandlesDifferentTerminators()
    {
        var calculator = new StandardChecksumCalculator();
        var message = "TEST";
        var messageBytes = Encoding.ASCII.GetBytes(message);
        var frameNumber = 1;

        var checksum1 = calculator.CalculateChecksum(messageBytes, frameNumber, 0x03); // ETX
        var checksum2 = calculator.CalculateChecksum(messageBytes, frameNumber, 0x17); // ETB

        // Different terminators should produce different checksums
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ProducesRepeatableResults()
    {
        var calculator = new StandardChecksumCalculator();
        var message = "REPEATABLE TEST MESSAGE";
        var messageBytes = Encoding.ASCII.GetBytes(message);
        var frameNumber = 3;
        var frameTerminator = (byte)0x03;

        var checksum1 = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);
        var checksum2 = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        checksum1.Should().Be(checksum2, "checksum calculation should be deterministic");
    }
}
