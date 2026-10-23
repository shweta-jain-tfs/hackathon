using FluentValidation.TestHelper;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Queries.GetPatient;

namespace Pharos.Server.Request.Application.Commands;

public class PatientValidatorTests
{
    private readonly PatientValidator _validator;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly DateTimeOffset _fixedNow = new(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public PatientValidatorTests()
    {
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.Now).Returns(_fixedNow);
        _validator = new PatientValidator(_dateTimeMock.Object);
    }

    [Fact]
    public void Should_HaveError_When_BirthDateIsInTheFuture()
    {
        // Arrange
        var futureBirthDate = DateOnly.FromDateTime(_fixedNow.AddDays(1).DateTime);
        var command = new PatientDto
        {
            PatientId = "PID001",
            BirthDate = futureBirthDate
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BirthDate)
            .WithErrorMessage("Birth date cannot be in the future.");
    }

    [Theory]
    [InlineData(0)]   // Today
    [InlineData(-30)] // 30 years ago
    public void Should_NotHaveError_When_BirthDateIsNotInTheFuture(int yearsOffset)
    {
        // Arrange
        var birthDate = DateOnly.FromDateTime(_fixedNow.AddYears(yearsOffset).DateTime);
        var command = new PatientDto
        {
            PatientId = "PID001",
            BirthDate = birthDate
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BirthDate);
    }

    [Fact]
    public void Should_NotHaveError_When_BirthDateIsNull()
    {
        // Arrange
        var command = new PatientDto
        {
            PatientId = "PID001",
            BirthDate = null
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BirthDate);
    }
}
