using System.Text;

namespace Pharos.Server.Application.Quality;

/// <summary>
/// CRC-32 checksum calculator using IEEE 802.3 polynomial (0xEDB88320).
/// </summary>
public static class Crc32
{
    /// <summary>
    /// Pre-computed lookup table for CRC-32 calculation using IEEE 802.3 polynomial.
    /// </summary>
    private static readonly uint[] _crcTable = GenerateCrcTable();

    private static uint[] GenerateCrcTable()
    {
        const uint polynomial = 0xEDB88320u; // Reversed representation of 0x04C11DB7
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }
            table[i] = crc;
        }

        return table;
    }

    /// <summary>
    /// Computes the CRC-32 checksum of the provided byte array.
    /// </summary>
    /// <param name="data">The data to compute the checksum for.</param>
    /// <returns>The CRC-32 checksum as a 32-bit unsigned integer.</returns>
    private static uint Compute(byte[] data)
    {
        var crc = 0xFFFFFFFFu;

        foreach (var b in data)
        {
            var tableIndex = (crc ^ b) & 0xFF;
            crc = _crcTable[tableIndex] ^ (crc >> 8);
        }

        return ~crc;
    }

    /// <summary>
    /// Computes the CRC-32 checksum of the provided string using UTF-8 encoding.
    /// </summary>
    /// <param name="data">The string data to compute the checksum for.</param>
    /// <returns>The CRC-32 checksum as a 32-bit unsigned integer.</returns>
    private static uint Compute(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return Compute(bytes);
    }

    /// <summary>
    /// Computes the CRC-32 checksum of the provided string and returns it as an 8-character uppercase hexadecimal string.
    /// </summary>
    /// <param name="data">The string data to compute the checksum for.</param>
    /// <returns>The CRC-32 checksum as an 8-character uppercase hex string.</returns>
    public static string ComputeHex(string data)
    {
        var crc = Compute(data);
        return crc.ToString("X8");
    }
}
