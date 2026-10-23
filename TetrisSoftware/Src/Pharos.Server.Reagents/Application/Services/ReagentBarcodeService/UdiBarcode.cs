using System.Globalization;

namespace Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;

/// <summary>
/// Represents a GS1 UDI parser for machine-readable format with pipe separators.
/// Follows GS1 Global Specifications with P2137 System Design Description specifications.
/// </summary>
/// <remarks>
/// Supports machine-readable format. Example: |01510222222333361715070710A213B|991234
/// Format: |[AI01+DeviceId][AI17+ExpiryDate][AI10+ArticleId+LotNumber][|AI99+SerialNumber]?
/// </remarks>
internal sealed record UdiBarcode
{
    #region Constants

    private const int _minUdiLength = 5;
    private const int _maxUdiLength = 43;
    private const int _deviceIdentifierLength = 14;
    private const int _dateFieldLength = 6;
    private const int _articleIdLength = 3;
    private const int _articleLotLength = 5; // 3 (Article ID) + 2 (Lot number)
    private const int _bottledReagentSerialLength = 4;
    private const int _carrierSerialNumberLength = 8;
    private const string _dateTimeFormat = "yyMMdd";
    private const char _fieldDelimiter = '|';

    #endregion

    #region Properties

    /// <summary>Gets the Device Identifier extracted from AI 01.</summary>
    public string DeviceIdentifier { get; init; } = string.Empty;

    /// <summary>Gets the expiry date extracted from AI 17.</summary>
    public DateTimeOffset? ExpiryDate { get; init; }

    /// <summary>Gets the Article ID (3 characters) extracted from the lot/batch field (AI 10).</summary>
    public string ArticleId { get; init; } = string.Empty;

    /// <summary>Gets the lot/batch number (without Article ID) extracted from AI 10.</summary>
    public string LotNumber { get; init; } = string.Empty;

    /// <summary>Gets the serial number extracted from AI 99 (IDD customization).</summary>
    public string? SerialNumber { get; init; }

    #endregion


    /// <summary>
    /// Parses a UDI string in machine-readable format with pipe separators.
    /// </summary>
    /// <param name="udi">The machine-readable UDI string to parse</param>
    /// <returns>A parsed UdiBarcode object</returns>
    /// <exception cref="ArgumentException">Thrown when the UDI string is invalid</exception>
    public static UdiBarcode Parse(string udi)
    {
        ValidateInput(udi);
        return ParseFields(udi);
    }

    /// <summary>
    /// Attempts to parse a UDI string without throwing exceptions.
    /// </summary>
    /// <param name="udi">The UDI string to parse</param>
    /// <param name="result">The parsed UdiBarcode if successful, null otherwise</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    public static bool TryParse(string? udi, out UdiBarcode? result)
    {
        result = null;

        try
        {
            if (string.IsNullOrWhiteSpace(udi))
            {
                return false;
            }

            result = Parse(udi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ValidateInput(string udi)
    {
        if (string.IsNullOrWhiteSpace(udi) ||
            udi.Length < _minUdiLength || udi.Length > _maxUdiLength ||
            !udi.StartsWith(_fieldDelimiter))
        {
            throw new ArgumentException("Invalid UDI format.", nameof(udi));
        }
    }

    private static UdiBarcode ParseFields(string udi)
    {
        var parts = udi.Split(_fieldDelimiter, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("Invalid UDI format: no fields found.", nameof(udi));
        }

        var mainPart = parts[0];
        if (string.IsNullOrEmpty(mainPart))
        {
            throw new ArgumentException("Invalid UDI format: empty content");
        }
        var parseMainContent = ParseMainContent(mainPart);

        var serialNumberPart = parts.Length > 1 ? parts[1] : null;
        string? serialNumber = null;
        if (!string.IsNullOrEmpty(serialNumberPart))
        {
            serialNumber = ParseSerialNumberContent(serialNumberPart);
        }

        ValidateRequiredFields(parseMainContent.deviceIdentifier, parseMainContent.expiryDate, parseMainContent.lotNumber);

        return new UdiBarcode
        {
            DeviceIdentifier = parseMainContent.deviceIdentifier!,
            ExpiryDate = parseMainContent.expiryDate,
            ArticleId = parseMainContent.articleId!,
            LotNumber = parseMainContent.lotNumber!,
            SerialNumber = serialNumber
        };
    }

    private static (string? deviceIdentifier, DateTimeOffset? manufacturingDate, DateTimeOffset? expiryDate, string? articleId, string? lotNumber) ParseMainContent(string data)
    {
        var position = 0;
        string? deviceIdentifier = null;
        DateTimeOffset? manufacturingDate = null;
        DateTimeOffset? expiryDate = null;
        string? articleId = null;
        string? lotNumber = null;

        while (position < data.Length)
        {
            // Need at least 2 characters for AI (Application Identifier)
            if (position + 2 > data.Length)
            {
                throw new ArgumentException($"Invalid field format at position {position}. Insufficient characters for Application Identifier.");
            }

            var ai = data.Substring(position, 2);

            if (!ai.All(char.IsDigit))
            {
                throw new ArgumentException($"Invalid Application Identifier: '{ai}' at position {position}. Must be 2 numeric digits.");
            }

            position += 2; // Move past AI

            switch (ai)
            {
                case "01": // Device Identifier - 14 digits
                    if (position + _deviceIdentifierLength > data.Length)
                    {
                        throw new ArgumentException($"Invalid Device Identifier: insufficient data at position {position}.");
                    }
                    var deviceIdValue = data.Substring(position, _deviceIdentifierLength);
                    deviceIdentifier = ValidateDeviceIdentifier(deviceIdValue);
                    position += _deviceIdentifierLength;
                    break;

                case "17": // Expiry Date - 6 digits
                    if (position + _dateFieldLength > data.Length)
                    {
                        throw new ArgumentException($"Invalid Expiry Date: insufficient data at position {position}.");
                    }
                    var expiryDateValue = data.Substring(position, _dateFieldLength);
                    expiryDate = ParseDate(expiryDateValue, "Expiry Date");
                    position += _dateFieldLength;
                    break;

                case "10": // Lot/Batch Number - 5 characters (3 Article ID + 2 Lot Number)
                    if (position + _articleLotLength > data.Length)
                    {
                        throw new ArgumentException($"Invalid Lot/Batch: insufficient data at position {position}.");
                    }
                    var lotBatchValue = data.Substring(position, _articleLotLength);
                    (articleId, lotNumber) = ParseLotBatchField(lotBatchValue);
                    position += _articleLotLength;
                    break;
                default:
                    // Unknown AI 
                    throw new ArgumentException($"Unknown Application Identifier: '{ai}' at position {position - 2}.");
            }
        }

        return (deviceIdentifier, manufacturingDate, expiryDate, articleId, lotNumber);
    }

    private static string ParseSerialNumberContent(string serialNumberContent)
    {
        if (string.IsNullOrEmpty(serialNumberContent))
        {
            throw new ArgumentException("Serial Number value cannot be empty.");
        }

        var ai = serialNumberContent.Substring(0, 2);
        if (ai != "99")
        {
            throw new ArgumentException($"Invalid serial number Application Identifier: '{ai}'.");
        }

        var serialNumber = serialNumberContent.Substring(2);
        // Serial numbers: 4 chars (bottled reagents) or 8 chars (carriers)
        if (serialNumber.Length is not _bottledReagentSerialLength and
            not _carrierSerialNumberLength)
        {
            throw new ArgumentException($"Invalid serial number length: '{serialNumber}'. Must be {_bottledReagentSerialLength} or {_carrierSerialNumberLength} characters.");
        }

        // Validate characters: only 0-9 and A-Z allowed
        return !serialNumber.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'Z'))
            ? throw new ArgumentException($"Invalid serial number characters: '{serialNumber}'. Only alphanumeric characters (0-9, A-Z) are allowed.")
            : serialNumber;
    }

    #region Field Validation
    private static void ValidateRequiredFields(string? deviceIdentifier, DateTimeOffset? expiryDate, string? lotNumber)
    {
        if (string.IsNullOrEmpty(deviceIdentifier))
        {
            throw new ArgumentException("Device Identifier (AI 01) is required in UDI.");
        }

        if (expiryDate == null)
        {
            throw new ArgumentException("Either Expiry Date (AI 17) should be part of the UDI.");
        }

        if (string.IsNullOrEmpty(lotNumber))
        {
            throw new ArgumentException("Lot/batch information (AI 10) not available in UDI.");
        }
    }

    private static (string articleId, string lotNumber) ParseLotBatchField(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Lot/batch field value cannot be empty.");
        }

        // Article ID (3 chars) + Lot number (2 chars) = exactly 5 characters
        if (value.Length != _articleLotLength)
        {
            throw new ArgumentException($"Invalid lot/batch field length: '{value}'. Must be exactly {_articleLotLength} characters (3-char Article ID + 2-char lot number).");
        }

        // Validate characters: only 0-9 and A-Z allowed
        if (!value.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'Z')))
        {
            throw new ArgumentException($"Invalid lot/batch field characters: '{value}'. Only alphanumeric characters (0-9, A-Z) are allowed.");
        }

        var articleId = value[.._articleIdLength];
        var lotNumber = value[_articleIdLength..];

        return (articleId, lotNumber);
    }

    private static string ValidateDeviceIdentifier(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Device Identifier value cannot be empty.")
            : value.Length != _deviceIdentifierLength || !value.All(char.IsDigit)
            ? throw new ArgumentException($"Invalid Device Identifier: '{value}'. Must be exactly {_deviceIdentifierLength} digits.")
            : value;
    }

    private static DateTimeOffset ParseDate(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} value cannot be empty.");
        }

        if (value.Length != _dateFieldLength || !value.All(char.IsDigit))
        {
            throw new ArgumentException($"Invalid {fieldName}: '{value}'. Must be exactly {_dateFieldLength} digits (YYMMDD).");
        }

        try
        {
            return DateTimeOffset.ParseExact(value, _dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"Invalid date format: '{value}'. Expected format: YYMMDD.", ex);
        }
    }

    #endregion
}