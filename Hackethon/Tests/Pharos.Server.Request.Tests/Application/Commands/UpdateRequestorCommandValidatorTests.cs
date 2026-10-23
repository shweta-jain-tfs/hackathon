using FluentValidation.TestHelper;
using Pharos.Server.Request.Application.Commands.UpdateRequestor;

namespace Pharos.Server.Request.Application.Commands;

public class UpdateRequestorCommandValidatorTests
{
    private readonly UpdateRequestorCommandValidator _validator;

    public UpdateRequestorCommandValidatorTests()
    {
        _validator = new UpdateRequestorCommandValidator();
    }

    [Fact]
    public void Should_HaveError_When_LastModifiedIsMinValue()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "REQ001",
            LastModified = DateTimeOffset.MinValue
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastModified)
              .WithErrorMessage("LastModified for the requestor must be properly set.");
    }

    [Fact]
    public void Should_NotHaveError_When_LastModifiedIsValid()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "REQ001",
            LastModified = DateTimeOffset.Now.AddMinutes(-10)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastModified);
    }

    [Fact]
    public void Should_HaveError_When_RequestorIdIsEmpty()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "",
            LastModified = DateTimeOffset.Now
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RequestorId)
              .WithErrorMessage("Requestor ID is required.");
    }

    [Fact]
    public void Should_HaveError_When_RequestorIdIsNull()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = null!,
            LastModified = DateTimeOffset.Now
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RequestorId)
              .WithErrorMessage("Requestor ID is required.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    public void Should_NotHaveError_When_RequestorIdIsValid(int length)
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = new string('A', length),
            LastModified = DateTimeOffset.Now
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RequestorId);
    }

    [Fact]
    public void Should_HaveError_When_RequestorIdExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = new string('A', 101), // Assuming max length is 100
            LastModified = DateTimeOffset.Now
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RequestorId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Valid Name")]
    public void Should_NotHaveError_When_NameIsNullEmptyOrValid(string? name)
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = name,
            LastModified = DateTimeOffset.Now
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_HaveError_When_NameExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = new string('A', 1001), // Assuming max length is 1000
            LastModified = DateTimeOffset.Now
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123 Test Street")]
    public void Should_NotHaveError_When_AddressIsNullEmptyOrValid(string? address)
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "REQ001",
            Address = address,
            LastModified = DateTimeOffset.Now
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public void Should_PassValidation_When_AllFieldsAreValid()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Updated Requestor",
            Address = "456 Updated Street",
            Comment = "Updated comment",
            ZipCode = "54321",
            OrderingPhysicianCode = "DR002",
            PhoneNumber = "555-5678",
            ReflexEnabled = false,
            ShortcutCode = "UPD",
            LatestSampleDate = DateTimeOffset.Now,
            LastModified = DateTimeOffset.Now.AddMinutes(-30)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_PassValidation_When_OnlyRequiredFieldsAreProvided()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "REQ001",
            LastModified = DateTimeOffset.Now.AddMinutes(-30)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveMultipleErrors_When_MultipleFieldsAreInvalid()
    {
        // Arrange
        var command = new UpdateRequestorCommand
        {
            RequestorId = "", // Invalid: empty
            LastModified = DateTimeOffset.MinValue // Invalid: min value
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RequestorId);
        result.ShouldHaveValidationErrorFor(x => x.LastModified);
    }
}