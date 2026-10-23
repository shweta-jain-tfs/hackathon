namespace Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;

public class UdiBarcodeTests
{
    #region Valid Format Tests for Different Reagent Types

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public void ParseImmunoCapCarriers_AllFields()
    {
        // ImmunoCAP Carriers: (01), (17), (10), (99) - All fields present
        var udi = UdiBarcode.Parse("|01073330660041291722033110CTS19|991234");

        udi.DeviceIdentifier.Should().Be("07333066004129");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2022, 3, 31, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("CTS");
        udi.LotNumber.Should().Be("19");
        udi.SerialNumber.Should().Be("1234");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public void ParseImmunoCapCarriers_WithoutSerial()
    {
        // ImmunoCAP Carriers without serial: (01), (17), (10) - Serial optional
        var udi = UdiBarcode.Parse("|01073330660041291722033110CTS19");

        udi.DeviceIdentifier.Should().Be("07333066004129");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2022, 3, 31, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("CTS");
        udi.LotNumber.Should().Be("19");
        udi.SerialNumber.Should().BeNull();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public void ParseEliAWellCarriers_AllFields()
    {
        // EliA Well Carriers: (01), (17), (10), (99) - 8-character serial number
        var udi = UdiBarcode.Parse("|01234567890123451724063010ELI01|99ABCD5678");

        udi.DeviceIdentifier.Should().Be("23456789012345");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2024, 6, 30, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("ELI");
        udi.LotNumber.Should().Be("01");
        udi.SerialNumber.Should().Be("ABCD5678");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public void ParseEliACalibrationCarriers_AllFields()
    {
        // EliA Calibration Carriers: (01), (17), (10), (99) - 4-character serial number
        var udi = UdiBarcode.Parse("|01987654321098761725121510CAL99|99XY89");

        udi.DeviceIdentifier.Should().Be("98765432109876");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("CAL");
        udi.LotNumber.Should().Be("99");
        udi.SerialNumber.Should().Be("XY89");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5536")]
    public void ParseCartridges_NoSerial()
    {
        // Cartridges: (01), (17), (10) - No serial number field
        var udi = UdiBarcode.Parse("|01510222223333611724042810CAR01");

        udi.DeviceIdentifier.Should().Be("51022222333361");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2024, 4, 28, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("CAR");
        udi.LotNumber.Should().Be("01");
        udi.SerialNumber.Should().BeNull();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5538")]
    public void ParseBottledReagents_AllFields()
    {
        // Bottled Reagents: (01), (17), (10), (99) - Serial = 4 chars
        var udi = UdiBarcode.Parse("|01414141414141411723051510BOT12|991234");

        udi.DeviceIdentifier.Should().Be("41414141414141");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2023, 5, 15, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("BOT");
        udi.LotNumber.Should().Be("12");
        udi.SerialNumber.Should().Be("1234");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    [Trait("ModuleRequirement", "MR_5538")]
    public void ParseValidSerialNumberLengths()
    {
        // Test all valid serial number lengths: 4 and 8 characters
        var testCases = new[]
        {
            (input: "|01073330660041291722033110ABC12|991234", expectedSerial: "1234"), // 4 chars (bottled)
            (input: "|01073330660041291722033110ABC12|99ABCD1234", expectedSerial: "ABCD1234") // 8 chars (carriers)
        };

        foreach (var testCase in testCases)
        {
            var udi = UdiBarcode.Parse(testCase.input);
            udi.SerialNumber.Should().Be(testCase.expectedSerial, $"for input {testCase.input}");
        }
    }

    [Fact]
    public void ParseValidLotBatchField()
    {
        // Test with valid 5-character lot/batch field (3-char Article ID + 2-char lot)
        var udi = UdiBarcode.Parse("|01073330660041291722033110ABC99");

        udi.DeviceIdentifier.Should().Be("07333066004129");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2022, 3, 31, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("ABC");
        udi.LotNumber.Should().Be("99");
        udi.SerialNumber.Should().BeNull();
    }

    #endregion

    #region Optional Field Tests

    [Fact]
    public void ParseWithOptionalLotNumber()
    {
        // Test that lot number is mandatory - should throw exception when missing
        var action = () => UdiBarcode.Parse("|010733306600412917220331|991234");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Lot/batch information (AI 10) not available in UDI.");
    }

    [Fact]
    public void ParseWithOptionalSerialNumber()
    {
        // Test with and without optional serial number
        var udiWithSerial = UdiBarcode.Parse("|01073330660041291722033110LOT12|991234");
        var udiWithoutSerial = UdiBarcode.Parse("|01073330660041291722033110LOT12");

        udiWithSerial.SerialNumber.Should().Be("1234");
        udiWithoutSerial.SerialNumber.Should().BeNull();

        // Both should have same Article ID and Lot Number
        udiWithSerial.ArticleId.Should().Be("LOT");
        udiWithSerial.LotNumber.Should().Be("12");
        udiWithoutSerial.ArticleId.Should().Be("LOT");
        udiWithoutSerial.LotNumber.Should().Be("12");
    }

    #endregion

    #region End of String Tests

    [Fact]
    public void ParseVariableLengthEndOfString_Serial()
    {
        // Serial number at end of string (no closing pipe)
        var udi = UdiBarcode.Parse("|01073330660041291722033110CTS19|991234");

        udi.DeviceIdentifier.Should().Be("07333066004129");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2022, 3, 31, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("CTS");
        udi.LotNumber.Should().Be("19");
        udi.SerialNumber.Should().Be("1234");
    }

    [Fact]
    public void ParseVariableLengthEndOfString_LotNumber()
    {
        // Lot number at end of string (no closing pipe) - must be exactly 5 characters
        var udi = UdiBarcode.Parse("|01073330660041291722033110ABC12");

        udi.DeviceIdentifier.Should().Be("07333066004129");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2022, 3, 31, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("ABC");
        udi.LotNumber.Should().Be("12");
        udi.SerialNumber.Should().BeNull();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ThrowsExceptionForMissingRequiredDeviceIdentifier()
    {
        // Missing Device Identifier (AI 01)
        var action = () => UdiBarcode.Parse("|1722033110CTS19|991234");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Device Identifier (AI 01) is required in UDI.");
    }

    [Fact]
    public void ThrowsExceptionForMissingRequiredLotNumber()
    {
        // Missing Lot/batch Number (AI 10) - mandatory
        var action = () => UdiBarcode.Parse("|010733306600412917220331|991234");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Lot/batch information (AI 10) not available in UDI.");
    }

    [Fact]
    public void ValidatesMandatoryLotNumberWithSpecificException()
    {
        // Test specific exception message for missing lot number
        var testCases = new[]
        {
            "|010733306600412917220331",           // Only device ID and expiry
            "|010733306600412917220331|991234"    // Device ID, expiry, and serial (no lot)
        };

        foreach (var invalidUdiBarcode in testCases)
        {
            var action = () => UdiBarcode.Parse(invalidUdiBarcode);
            action.Should().Throw<ArgumentException>()
                .WithMessage("Lot/batch information (AI 10) not available in UDI.");
        }
    }

    [Fact]
    public void ThrowsExceptionForInvalidDeviceIdentifierLength()
    {
        // Invalid Device Identifier length (not 14 digits)
        var action = () => UdiBarcode.Parse("|0112345678917220331010LOT12");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Unknown Application Identifier: '33' at position 16.");
    }

    [Fact]
    public void ThrowsExceptionForInvalidDeviceIdentifierCharacters()
    {
        // Invalid Device Identifier characters (non-numeric)
        var action = () => UdiBarcode.Parse("|01ABCD6789012345172203311110LOT12");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Device Identifier: 'ABCD6789012345'. Must be exactly 14 digits.");
    }

    [Fact]
    public void ThrowsExceptionForInvalidDateFormat()
    {
        // Invalid date format (not 6 digits)
        var action = () => UdiBarcode.Parse("|0107333066004129172203010LOT12");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Application Identifier: '0L' at position 24. Must be 2 numeric digits.");
    }

    [Fact]
    public void ThrowsExceptionForInvalidDateValue()
    {
        // Invalid date value (month 13)
        var action = () => UdiBarcode.Parse("|01073330660041291722133110LOT12");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid date format: '221331'. Expected format: YYMMDD.");
    }

    [Fact]
    public void ValidateArticleLotFieldShortLength()
    {
        var udi = "|01073330660041291722033110ABC1";

        var action = () => UdiBarcode.Parse(udi);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Lot/Batch: insufficient data at position 26.");
    }

    [Fact]
    public void ValidateArticleLotFieldLengthIsLong()
    {
        var udi = "|01073330660041291722033110ABC123";
        var action = () => UdiBarcode.Parse(udi);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid field format at position 31. Insufficient characters for Application Identifier.");
    }

    [Fact]
    public void ValidateLotBatchFieldCharacters()
    {
        // Invalid lot/batch field characters (only 0-9, A-Z allowed)
        var testCases = new[]
        {
            "|01073330660041291722033110ABC-1",     // Hyphen not allowed
            "|01073330660041291722033110ab123",     // Lowercase not allowed
            "|01073330660041291722033110ABC@1",     // Special character not allowed
        };

        foreach (var invalidUdiBarcode in testCases)
        {
            var action = () => UdiBarcode.Parse(invalidUdiBarcode);
            action.Should().Throw<ArgumentException>()
                .WithMessage("Invalid lot/batch field characters: '*'. Only alphanumeric characters (0-9, A-Z) are allowed.");
        }
    }

    [Fact]
    public void ValidateSerialNumberStrictLengthTooShort()
    {
        var udi = "|01073330660041291722033110LOT12|99ABC";

        var action = () => UdiBarcode.Parse(udi);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid serial number length: 'ABC'. Must be 4 or 8 characters.");
    }

    [Fact]
    public void ValidateSerialNumberStrictLengthTooLong()
    {
        var udi = "|010733306600412917220331|99123456789";

        var action = () => UdiBarcode.Parse(udi);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid serial number length: '123456789'. Must be 4 or 8 characters.");
    }

    [Fact]
    public void ValidateSerialNumberCharacters()
    {
        // Invalid serial number characters (only 0-9, A-Z allowed)
        var testCases = new[]
        {
            "|01073330660041291722033110LOT12|99SER@",       // Special character not allowed
            "|01073330660041291722033110LOT12|99ser1",       // Lowercase not allowed
            "|01073330660041291722033110LOT12|99SER-",       // Hyphen not allowed
        };

        foreach (var invalidUdiBarcode in testCases)
        {
            var action = () => UdiBarcode.Parse(invalidUdiBarcode);
            action.Should().Throw<ArgumentException>()
                .WithMessage("Invalid serial number characters: '*'. Only alphanumeric characters (0-9, A-Z) are allowed.");
        }
    }

    [Fact]
    public void ThrowsExceptionForIncompleteField()
    {
        // Incomplete field (less than 3 characters)
        var action = () => UdiBarcode.Parse("|0107333066004129172203311099");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Lot/Batch: insufficient data at position 26.");
    }

    [Fact]
    public void ThrowsExceptionForInvalidApplicationIdentifier()
    {
        // Invalid Application Identifier (non-numeric)
        var action = () => UdiBarcode.Parse("|0107333066004129172203311010LOT12AB123");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Unknown Application Identifier: '12' at position 31.");
    }

    [Fact]
    public void ThrowsExceptionForInvalidUdiBarcodeFormat()
    {
        // Test various invalid UDI formats
        var testCases = new[]
        {
            "",                    // Empty string
            "   ",                 // Whitespace only
            "0107333066004129",    // Missing pipe delimiter
            "|01",                 // Too short
            "ABC123"               // No pipe delimiter
        };

        foreach (var invalidUdiBarcode in testCases)
        {
            var action = () => UdiBarcode.Parse(invalidUdiBarcode);
            action.Should().Throw<ArgumentException>()
                .WithMessage("Invalid UDI format. (Parameter 'udi')");
        }
    }

    [Fact]
    public void IgnoresUnknownApplicationIdentifiers()
    {
        // Unknown AI codes should be ignored
        var udi = UdiBarcode.Parse("|01073330660041291722033110LOT12|991234");

        udi.DeviceIdentifier.Should().Be("07333066004129");
        udi.ExpiryDate.Should().Be(new DateTimeOffset(2022, 3, 31, 0, 0, 0, TimeSpan.Zero));
        udi.ArticleId.Should().Be("LOT");
        udi.LotNumber.Should().Be("12");
        udi.SerialNumber.Should().Be("1234");
    }

    [Fact]
    public void MandatoryExpiryDate()
    {
        var udiWithExpiryDate = UdiBarcode.Parse("|01073330660041291722033110LOT12|991234");
        var udiWithoutDate = () => UdiBarcode.Parse("|010733306600412910LOT12|991234");

        udiWithExpiryDate.ExpiryDate.Should().Be(new DateTimeOffset(2022, 3, 31, 0, 0, 0, TimeSpan.Zero));
        udiWithExpiryDate.ArticleId.Should().Be("LOT");
        udiWithExpiryDate.LotNumber.Should().Be("12");

        udiWithoutDate.Should().Throw<ArgumentException>()
            .WithMessage("Either Expiry Date (AI 17) should be part of the UDI.");
    }

    #endregion

    #region TryParse Tests

    [Fact]
    public void TryParseReturnsTrue_ForValidUdiBarcode()
    {
        // Test TryParse with valid UDI
        var result = UdiBarcode.TryParse("|01073330660041291722033110CTS19|991234", out var udiData);

        result.Should().BeTrue();
        udiData.Should().NotBeNull();
        udiData!.DeviceIdentifier.Should().Be("07333066004129");
        udiData.ArticleId.Should().Be("CTS");
        udiData.LotNumber.Should().Be("19");
        udiData.SerialNumber.Should().Be("1234");
    }

    [Fact]
    public void TryParseReturnsFalse_ForInvalidUdiBarcode()
    {
        // Test TryParse with invalid UDI
        var result = UdiBarcode.TryParse("invalid-udi-format", out var udiData);

        result.Should().BeFalse();
        udiData.Should().BeNull();
    }

    [Fact]
    public void TryParseReturnsFalse_ForNullOrEmptyUdiBarcode()
    {
        // Test TryParse with null and empty strings
        var result1 = UdiBarcode.TryParse(null, out var udi1);
        var result2 = UdiBarcode.TryParse("", out var udi2);
        var result3 = UdiBarcode.TryParse("   ", out var udi3);

        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
        udi1.Should().BeNull();
        udi2.Should().BeNull();
        udi3.Should().BeNull();
    }

    #endregion
}
