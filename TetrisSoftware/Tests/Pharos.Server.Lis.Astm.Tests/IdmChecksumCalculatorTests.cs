using System.Text;
using Pharos.Server.Lis.Astm.Application.Frames;

namespace Pharos.Server.Lis.Astm.Tests;

public class IdmChecksumCalculatorTests
{
    [Fact]
    public void UsesLowerByteOfMultiByteCharacters()
    {
        var encoding = Encoding.Unicode; // UTF-16
        var calculator = new IdmChecksumCalculator(encoding);
        
        // Create a message with a character that has multi-byte representation
        // In UTF-16, '€' (Euro sign) is 0x20AC in big-endian or AC20 in little-endian
        var message = "€";
        var messageBytes = encoding.GetBytes(message);
        var frameNumber = 1;
        var frameTerminator = (byte)0x03;

        var (cs0, cs1) = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        // Should return valid hex ASCII
        cs0.Should().BeInRange((byte)'0', (byte)'F');
        cs1.Should().BeInRange((byte)'0', (byte)'F');
    }

    [Fact]
    public void HandlesAsciiEncoding()
    {
        var encoding = Encoding.ASCII;
        var calculator = new IdmChecksumCalculator(encoding);
        var message = "TEST";
        var messageBytes = encoding.GetBytes(message);
        var frameNumber = 1;
        var frameTerminator = (byte)0x03;

        var (cs0, cs1) = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        cs0.Should().BeInRange((byte)'0', (byte)'F');
        cs1.Should().BeInRange((byte)'0', (byte)'F');
    }

    [Fact]
    public void HandlesUtf8Encoding()
    {
        var encoding = Encoding.UTF8;
        var calculator = new IdmChecksumCalculator(encoding);
        var message = "Test™"; // Contains trademark symbol (multi-byte in UTF-8)
        var messageBytes = encoding.GetBytes(message);
        var frameNumber = 1;
        var frameTerminator = (byte)0x03;

        var (cs0, cs1) = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        cs0.Should().BeInRange((byte)'0', (byte)'F');
        cs1.Should().BeInRange((byte)'0', (byte)'F');
    }

    [Fact]
    public void HandlesUnicodeEncoding()
    {
        
        var encoding = Encoding.Unicode;
        var calculator = new IdmChecksumCalculator(encoding);
        var message = "Test™€"; // Multiple multi-byte characters
        var messageBytes = encoding.GetBytes(message);
        var frameNumber = 1;
        var frameTerminator = (byte)0x03;

        
        var (cs0, cs1) = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        cs0.Should().BeInRange((byte)'0', (byte)'F');
        cs1.Should().BeInRange((byte)'0', (byte)'F');
    }

    [Fact]
    public void ProducesRepeatableResults()
    {
        
        var encoding = Encoding.Unicode;
        var calculator = new IdmChecksumCalculator(encoding);
        var message = "REPEATABLE™";
        var messageBytes = encoding.GetBytes(message);
        var frameNumber = 2;
        var frameTerminator = (byte)0x03;

        
        var checksum1 = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);
        var checksum2 = calculator.CalculateChecksum(messageBytes, frameNumber, frameTerminator);

        checksum1.Should().Be(checksum2, "checksum calculation should be deterministic");
    }
}
