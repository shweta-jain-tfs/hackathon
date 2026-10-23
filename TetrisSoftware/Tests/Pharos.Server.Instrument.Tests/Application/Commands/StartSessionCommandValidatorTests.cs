using System.Net;
using Pharos.Server.Instrument.Application.Commands.StartSession;

namespace Pharos.Server.Instrument.Application.Commands;

public class StartSessionCommandValidatorTests
{
    private readonly StartSessionCommandValidator _sut;

    public StartSessionCommandValidatorTests()
    {
        _sut = new StartSessionCommandValidator();
    }

    [Fact]
    public void InstrumentSoftwareVersionExceedingProtocolLimit_ShouldFailValidation()
    {
        // Arrange
        var command = new StartSessionCommand(
            IPAddress.Loopback,
            Guid.NewGuid(),
            "E500-123",
            "1.2.3",
            "1234567.8.9",
            5001);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(
            error => error.PropertyName == nameof(StartSessionCommand.InstrumentSoftwareVersion));
    }
}