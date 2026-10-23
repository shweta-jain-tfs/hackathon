using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Pharos.Server.Application.Quality;

/// <summary>
/// Parser for proprietary QC Data Matrix barcodes.
/// Format: FNC1(|) + ArticleID(3) + Lot(2) + Version(1) + CRC(8) + Base64/GZip payload
/// </summary>
public sealed class QcDataMatrixBarcode
{
    private const char _fnc1Delimiter = '|';
    private const int _articleIdLength = 3;
    private const int _lotNumberLength = 2;
    private const int _versionLength = 1;
    private const int _checksumLength = 8;
    private const int _payloadStartPosition = 1 + _articleIdLength + _lotNumberLength + _versionLength + _checksumLength; // 15
    private const int _minHeaderLength = _payloadStartPosition;
    private const char _testLimitSeparator = '\\';

    private const char _lessThanOperator = '<';
    private const char _lessThanOrEqualOperator = '(';
    private const char _greaterThanOperator = '>';
    private const char _greaterThanOrEqualOperator = ')';
    private const char _equalOperator = '=';

    private static readonly HashSet<char> _comparisonOperators =
    [
        _lessThanOperator,
        _lessThanOrEqualOperator,
        _greaterThanOperator,
        _greaterThanOrEqualOperator,
        _equalOperator
    ];

    /// <summary>
    /// The 3-character article identifier.
    /// </summary>
    public string ArticleId { get; private init; } = string.Empty;

    /// <summary>
    /// The 2-character lot number.
    /// </summary>
    public string LotNumber { get; private init; } = string.Empty;

    /// <summary>
    /// The version of the QC data matrix encoding.
    /// </summary>
    public string Version { get; private init; } = string.Empty;

    /// <summary>
    /// The list of tests and their limits parsed from the QC data matrix.
    /// </summary>
    public IReadOnlyList<QcCartridgeTestInfo> Tests { get; private init; } = [];

    /// <summary>
    /// Attempts to parse a QC data matrix barcode string.
    /// </summary>
    /// <param name="dataMatrix">The raw data matrix string to parse.</param>
    /// <param name="result">The parsed QcDataMatrixBarcode if successful; otherwise, null.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string dataMatrix, out QcDataMatrixBarcode? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(dataMatrix))
        {
            return false;
        }

        // Check minimum length
        if (dataMatrix.Length < _minHeaderLength)
        {
            return false;
        }

        // Check FNC1 delimiter at start
        if (dataMatrix[0] != _fnc1Delimiter)
        {
            return false;
        }

        // Extract and validate Article ID (positions 1-3)
        var articleId = dataMatrix.Substring(1, _articleIdLength);
        if (!IsAlphanumeric(articleId))
        {
            return false;
        }

        // Extract and validate Lot Number (positions 4-5)
        var lotNumber = dataMatrix.Substring(1 + _articleIdLength, _lotNumberLength);
        if (!IsAlphanumeric(lotNumber))
        {
            return false;
        }

        // Extract and validate Version (position 6)
        var version = dataMatrix.Substring(1 + _articleIdLength + _lotNumberLength, _versionLength);
        if (!IsVersionValid(version))
        {
            return false;
        }

        // Extract CRC checksum (positions 7-14)
        var checksumHex = dataMatrix.Substring(1 + _articleIdLength + _lotNumberLength + _versionLength, _checksumLength);
        if (!IsHexString(checksumHex))
        {
            return false;
        }

        // Extract payload (positions 15+)
        var payload = dataMatrix.Length > _payloadStartPosition
            ? dataMatrix[_payloadStartPosition..]
            : string.Empty;

        // Validate CRC
        var computedCrc = Crc32.ComputeHex(payload);
        var isChecksumValid = string.Equals(checksumHex, computedCrc, StringComparison.OrdinalIgnoreCase);

        if (!isChecksumValid)
        {
            return false;
        }

        // If no payload, return with empty tests
        if (string.IsNullOrEmpty(payload))
        {
            result = new QcDataMatrixBarcode
            {
                ArticleId = articleId,
                LotNumber = lotNumber,
                Version = version,
                Tests = []
            };
            return true;
        }

        // Decode and decompress payload
        IReadOnlyList<QcCartridgeTestInfo> tests;
        try
        {
            var decompressedPayload = DecodeAndDecompressPayload(payload);
            tests = ParseTestLimits(decompressedPayload);
        }
        catch
        {
            return false;
        }

        result = new QcDataMatrixBarcode
        {
            ArticleId = articleId,
            LotNumber = lotNumber,
            Version = version,
            Tests = tests
        };
        return true;
    }

    /// <summary>
    /// Parses a QC data matrix barcode string. Throws an exception if parsing fails.
    /// </summary>
    /// <param name="dataMatrix">The raw data matrix string to parse.</param>
    /// <returns>The parsed QcDataMatrixBarcode.</returns>
    /// <exception cref="ArgumentException">Thrown when the data matrix cannot be parsed.</exception>
    public static QcDataMatrixBarcode Parse(string dataMatrix)
    {
        if (!TryParse(dataMatrix, out var result) || result == null)
        {
            throw new ArgumentException("Invalid QC data matrix format.", nameof(dataMatrix));
        }
        return result;
    }

    private static bool IsAlphanumeric(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsAsciiDigit(c) && !(c >= 'A' && c <= 'Z'))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsVersionValid(string version)
    {
        if (version.Length != 1)
        {
            return false;
        }
        var c = version[0];
        return (c >= '1' && c <= '9') || (c >= 'A' && c <= 'Z');
    }

    private static bool IsHexString(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsAsciiDigit(c) && !(c >= 'A' && c <= 'F') && !(c >= 'a' && c <= 'f'))
            {
                return false;
            }
        }
        return true;
    }

    private static string DecodeAndDecompressPayload(string payload)
    {
        // Decode Base64
        var compressedBytes = Convert.FromBase64String(payload);

        // Decompress GZip
        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        gzipStream.CopyTo(resultStream);

        return Encoding.ASCII.GetString(resultStream.ToArray());
    }

    private static List<QcCartridgeTestInfo> ParseTestLimits(string decompressedPayload)
    {
        if (string.IsNullOrEmpty(decompressedPayload))
        {
            return [];
        }

        var tests = new List<QcCartridgeTestInfo>();
        var span = decompressedPayload.AsSpan();
        var position = 0;

        while (position < span.Length)
        {
            // Skip optional separator (used when next test name starts with a digit)
            if (span[position] == _testLimitSeparator)
            {
                position++;
                if (position >= span.Length)
                {
                    break;
                }
            }

            // Parse test name (2-5 alphanumeric characters until comparison operator)
            var testNameStart = position;
            while (position < span.Length && !_comparisonOperators.Contains(span[position]))
            {
                position++;
            }

            if (position >= span.Length || position == testNameStart)
            {
                break; // No operator found or empty test name
            }

            var testName = span[testNameStart..position].ToString();

            // Validate test name length (2-5 characters)
            if (testName.Length < 2 || testName.Length > 5)
            {
                break;
            }

            // Parse all limits for this test (limits follow immediately after test name)
            // Format: TestName>value<value=value (limits continue until next test name or end)
            var limits = new List<QcCartridgeTestLimit>();

            while (position < span.Length && _comparisonOperators.Contains(span[position]))
            {
                // Parse operator
                var operatorChar = span[position];
                var limitType = ParseLimitType(operatorChar);
                position++;

                // Parse limit value (digits and optional decimal point, 1-5 characters)
                var valueStart = position;
                while (position < span.Length &&
                       (char.IsAsciiDigit(span[position]) || span[position] == '.'))
                {
                    position++;
                }

                if (position == valueStart)
                {
                    break; // No value found
                }

                var valueString = span[valueStart..position].ToString();

                // Validate value length (1-5 characters)
                if (valueString.Length < 1 || valueString.Length > 5)
                {
                    break;
                }

                if (!double.TryParse(valueString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var limitValue))
                {
                    break;
                }

                limits.Add(new QcCartridgeTestLimit
                {
                    LimitType = limitType,
                    LimitValue = limitValue
                });
            }

            if (limits.Count > 0)
            {
                tests.Add(new QcCartridgeTestInfo
                {
                    TestName = testName,
                    Limits = limits
                });
            }
        }

        return tests;
    }

    private static QcCartridgeLimitType ParseLimitType(char operatorChar)
    {
        return operatorChar switch
        {
            _lessThanOperator => QcCartridgeLimitType.LessThan,
            _lessThanOrEqualOperator => QcCartridgeLimitType.LessThanOrEqual,
            _greaterThanOperator => QcCartridgeLimitType.GreaterThan,
            _greaterThanOrEqualOperator => QcCartridgeLimitType.GreaterThanOrEqual,
            _equalOperator => QcCartridgeLimitType.Equal,
            _ => throw new ArgumentException($"Invalid comparison operator: {operatorChar}")
        };
    }
}
