using FluentValidation.TestHelper;
using Pharos.Server.Request.Application.Commands.CreateRequestor;

namespace Pharos.Server.Request.Application.Commands;

public class CreateRequestorCommandValidatorTests
{
    private readonly CreateRequestorCommandValidator _validator;

    public CreateRequestorCommandValidatorTests()
    {
        _validator = new CreateRequestorCommandValidator();
    }

    [Fact]
    public void Should_HaveError_When_RequestorIdIsEmpty()
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = ""
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
        var command = new CreateRequestorCommand
        {
            RequestorId = null!
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
        var command = new CreateRequestorCommand
        {
            RequestorId = new string('A', length)
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
        var command = new CreateRequestorCommand
        {
            RequestorId = new string('A', 101) // Assuming max length is 100
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
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = name
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
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = new string('A', 1001) // Assuming max length is 1000
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
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Address = address
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public void Should_HaveError_When_AddressExceedsMaxLength()
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Address = new string('A', 1001) // Assuming max length is 1000
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Test comment")]
    public void Should_NotHaveError_When_CommentIsNullEmptyOrValid(string? comment)
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Comment = comment
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    public void Should_NotHaveError_When_ZipCodeIsNullEmptyOrValid(string? zipCode)
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            ZipCode = zipCode
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ZipCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DR001")]
    public void Should_NotHaveError_When_OrderingPhysicianCodeIsNullEmptyOrValid(string? code)
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            OrderingPhysicianCode = code
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrderingPhysicianCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("555-1234")]
    public void Should_NotHaveError_When_PhoneNumberIsNullEmptyOrValid(string? phoneNumber)
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            PhoneNumber = phoneNumber
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("TST")]
    public void Should_NotHaveError_When_ShortcutCodeIsNullEmptyOrValid(string? shortcutCode)
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            ShortcutCode = shortcutCode
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ShortcutCode);
    }

    [Fact]
    public void Should_PassValidation_When_AllFieldsAreValid()
    {
        // Arrange
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = "Test Requestor",
            Address = "123 Test Street",
            Comment = "Test comment",
            ZipCode = "12345",
            OrderingPhysicianCode = "DR001",
            PhoneNumber = "555-1234",
            ReflexEnabled = true,
            ShortcutCode = "TST",
            LatestSampleDate = DateTimeOffset.Now,
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
        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001"
            // All other fields are optional and have default values
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}