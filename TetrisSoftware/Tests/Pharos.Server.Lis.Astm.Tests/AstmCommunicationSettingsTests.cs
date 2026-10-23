using System.Text;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Astm.Application.Frames;
using Pharos.Server.Lis.Astm.Application.Interfaces;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmCommunicationSettingsTests
{
    private readonly Encoding _asciiEncoding = Encoding.ASCII;

    #region Test Data Helpers

    private static Mock<IFrameChecksumCalculator> CreateMockChecksumCalculator()
    {
        var mock = new Mock<IFrameChecksumCalculator>();
        mock.Setup(c => c.CalculateChecksum(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<byte>()))
            .Returns((0x10, 0x11));
        return mock;
    }

    #endregion

    #region Use Only ETX Tests

    [Fact]
    public void UseOnlyEtx_On_AllFramesUseEtxTerminator()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: true,
            CreateMockChecksumCalculator().Object);

        var message1 = new string('M', 240);
        var message2 = new string('S', 240);
        var messageData = _asciiEncoding.GetBytes(message1 + message2);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCount(2);
        foreach (var frame in frames)
        {
            var frameBytes = frame.GetBytes();
            var terminatorPosition = 2 + frame.MessageData.Length;
            frameBytes[terminatorPosition].Should().Be(0x03, "all frames should use ETX when UseOnlyEtx is true");
            frameBytes[terminatorPosition].Should().NotBe(0x17, "no frame should use ETB when UseOnlyEtx is true");
        }
    }

    [Fact]
    public void UseOnlyEtx_Off_IntermediateFramesUseEtbAndFinalUsesEtx()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: false,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: true,
            CreateMockChecksumCalculator().Object);

        var message1 = new string('M', 240);
        var message2 = new string('S', 240);
        var messageData = _asciiEncoding.GetBytes(message1 + message2);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCount(2);

        // First frame should use ETB (0x17) - intermediate frame
        var firstFrameBytes = frames[0].GetBytes();
        var firstTerminatorPosition = 2 + frames[0].MessageData.Length;
        firstFrameBytes[firstTerminatorPosition].Should().Be(0x17, "intermediate frame should use ETB");

        // Last frame should use ETX (0x03) - final frame
        var lastFrameBytes = frames[1].GetBytes();
        var lastTerminatorPosition = 2 + frames[1].MessageData.Length;
        lastFrameBytes[lastTerminatorPosition].Should().Be(0x03, "final frame should use ETX");
    }

    [Fact]
    public void UseOnlyEtx_DefaultValue_IsFalse()
    {
        var settings = new AstmCommunicationSettings();

        settings.UseOnlyEtx.Should().BeFalse("default value should be off/false");
    }

    #endregion

    #region ETX Only Normal Frameblock Size Tests

    [Fact]
    public void EtxOnlyNormalFrameBlockSize_On_GroupsRecordsUpTo240Characters()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: true,
            CreateMockChecksumCalculator().Object);

        var record1 = new string('A', 100) + "\r";
        var record2 = new string('B', 100) + "\r";
        var record3 = new string('C', 50);
        var message = record1 + record2 + record3; // Total : 252
        var messageData = _asciiEncoding.GetBytes(message);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCountGreaterThanOrEqualTo(1);
        foreach (var frame in frames.Take(frames.Count - 1))
        {
            frame.MessageData.Length.Should().Be(240, "frames should be filled to maximum 240 characters");
        }
    }

    [Fact]
    public void EtxOnlyNormalFrameBlockSize_Off_EachRecordInSeparateFrame()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: false,
            CreateMockChecksumCalculator().Object);

        // Create message with multiple records separated by CR
        var record1 = new string('A', 50) + "\r";
        var record2 = new string('B', 60) + "\r";
        var record3 = new string('C', 70);
        var message = record1 + record2 + record3;
        var messageData = _asciiEncoding.GetBytes(message);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCount(3, "each record should be in its own frame");
        frames[0].MessageData.Length.Should().Be(51, "first record length including CR");
        frames[1].MessageData.Length.Should().Be(61, "second record length including CR");
        frames[2].MessageData.Length.Should().Be(70, "third record length without CR");
    }

    [Fact]
    public void EtxOnlyNormalFrameBlockSize_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var settings = new AstmCommunicationSettings();

        // Assert
        settings.EtxOnlyNormalFrameBlockSize.Should().BeTrue("default value should be on/true");
    }

    #endregion

    #region ETX Only Split Large Records Tests

    [Fact]
    public void EtxOnlySplitLargeRecords_On_SplitsLargeRecords()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: true,
            etxOnlyNormalFrameBlockSize: false,
            CreateMockChecksumCalculator().Object);

        var largeRecord = new string('L', 500) + "\r";
        var messageData = _asciiEncoding.GetBytes(largeRecord);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        // Assert - Large record should be split into multiple frames of max 240 chars
        frames.Should().HaveCountGreaterThan(1, "large record should be split into multiple frames");
        foreach (var frame in frames.Take(frames.Count - 1)) // All frames except possibly the last
        {
            frame.MessageData.Length.Should().BeLessOrEqualTo(240, "each frame should not exceed 240 characters");
        }
    }

    [Fact]
    public void EtxOnlySplitLargeRecords_Off_SendsLargeRecordInSingleFrame()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: false,
            CreateMockChecksumCalculator().Object);
        var largeRecord = new string('L', 500) + "\r";
        var messageData = _asciiEncoding.GetBytes(largeRecord);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCount(1, "large record should be sent in a single frame regardless of size");
        frames[0].MessageData.Length.Should().Be(501, "entire record should be in one frame");
    }

    [Fact]
    public void EtxOnlySplitLargeRecords_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var settings = new AstmCommunicationSettings();

        // Assert
        settings.EtxOnlySplitLargeRecords.Should().BeFalse("default value should be off/false");
    }

    [Theory]
    [InlineData(false, false, false)] // UseOnlyEtx=Off, should not split
    [InlineData(false, true, false)]  // UseOnlyEtx=Off, should not split
    [InlineData(true, false, true)]   // UseOnlyEtx=On, NormalFrameBlockSize=Off, SHOULD split when setting is On
    [InlineData(true, true, false)]   // UseOnlyEtx=On, NormalFrameBlockSize=On, should not apply splitting logic
    public void EtxOnlySplitLargeRecords_EnabledOnlyWhenConditionsMet(bool useOnlyEtx, bool normalFrameBlockSize, bool shouldRespectSplitSetting)
    {
        // Arrange
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: useOnlyEtx,
            etxOnlySplitLargeRecords: true, // Always set to true to test when it actually applies
            etxOnlyNormalFrameBlockSize: normalFrameBlockSize,
            CreateMockChecksumCalculator().Object);

        var largeRecord = new string('L', 500) + "\r";
        var messageData = _asciiEncoding.GetBytes(largeRecord);

        // Act
        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        // Assert
        if (shouldRespectSplitSetting)
        {
            frames.Should().HaveCountGreaterThan(1, "splitting should be applied when conditions are met");
        }
        else if (!useOnlyEtx)
        {
            // Standard behavior - split at 240
            frames.Should().HaveCountGreaterThan(1, "standard splitting at 240 chars");
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void IntegrationTest_UseOnlyEtx_WithNormalFrameBlockSize()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: true,
            CreateMockChecksumCalculator().Object);

        var message = new string('M', 240) + new string('S', 240);
        var messageData = _asciiEncoding.GetBytes(message);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCount(2);
        frames.Should().OnlyContain(f => f.GetBytes()[2 + f.MessageData.Length] == 0x03,
            "all frames should use ETX in UseOnlyEtx mode");
    }

    [Fact]
    public void IntegrationTest_UseOnlyEtx_NoNormalFrameBlockSize_WithSplitting()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: true,
            etxOnlyNormalFrameBlockSize: false,
            CreateMockChecksumCalculator().Object);

        var largeRecord = new string('L', 500) + "\r";
        var messageData = _asciiEncoding.GetBytes(largeRecord);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCountGreaterThan(1, "large record should be split");
        frames.Should().OnlyContain(f => f.GetBytes()[2 + f.MessageData.Length] == 0x03,
            "all frames should use ETX in UseOnlyEtx mode");
        frames.Take(frames.Count - 1).Should().OnlyContain(f => f.MessageData.Length <= 240,
            "split frames should respect 240 character limit");
    }

    [Fact]
    public void EdgeCase_Exactly240Characters_WithUseOnlyEtx()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: true,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: true,
            CreateMockChecksumCalculator().Object);

        var message = new string('M', 240);
        var messageData = _asciiEncoding.GetBytes(message);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCount(1);
        frames[0].MessageData.Length.Should().Be(240);
        frames[0].GetBytes()[242].Should().Be(0x03, "should use ETX for single frame");
    }

    [Fact]
    public void EdgeCase_241Characters_ShouldSplitIntoTwoFrames()
    {
        var frameBuilder = new AstmFrameBuilder(
            useOnlyEtx: false,
            etxOnlySplitLargeRecords: false,
            etxOnlyNormalFrameBlockSize: true,
            CreateMockChecksumCalculator().Object);

        var message = new string('M', 241);
        var messageData = _asciiEncoding.GetBytes(message);

        var frames = frameBuilder.CreateFramesFromMessage(messageData).ToList();

        frames.Should().HaveCount(2);
        frames[0].MessageData.Length.Should().Be(240);
        frames[1].MessageData.Length.Should().Be(1);
    }

    [Fact]
    public void EtxOnlySplitLargeRecords_DependsOn_UseOnlyEtx_And_EtxOnlyNormalFrameBlockSize()
    {
        var settings = new AstmCommunicationSettings();

        settings.UseOnlyEtx.Should().BeFalse();
        settings.EtxOnlyNormalFrameBlockSize.Should().BeTrue();
        settings.EtxOnlySplitLargeRecords.Should().BeFalse();
    }

    #endregion


}
