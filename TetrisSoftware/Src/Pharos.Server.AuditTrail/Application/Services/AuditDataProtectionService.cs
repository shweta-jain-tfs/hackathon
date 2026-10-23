using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Pharos.Server.AuditTrail.Application.Services;

internal class AuditDataProtectionService
{
    private readonly string _filePathHash = Path.Combine(AppContext.BaseDirectory, "secretHash.json");
    private readonly string _filePathSalt = Path.Combine(AppContext.BaseDirectory, "secretSalt.json");
    private readonly JsonSerializerOptions _options;
    private readonly string _saltCache = null!;
    private static readonly Lock _fileLock = new();

    public AuditDataProtectionService()
    {

        _options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        lock (_fileLock) // Ensure thread-safe initialization
        {
            if (!File.Exists(_filePathHash))
            {
                File.WriteAllText(_filePathHash, JsonSerializer.Serialize("{}", _options));
            }

            if (!File.Exists(_filePathSalt))
            {
                CreateSalt();
            }

            _saltCache = LoadSaltCache();
        }
    }

    private void CreateSalt()
    {
        var secSalt = new byte[32];
        RandomNumberGenerator.Fill(secSalt);
        File.WriteAllText(_filePathSalt, JsonSerializer.Serialize(secSalt, _options));
    }

    private string LoadSaltCache()
    {
        var saltBytesJson = File.ReadAllText(_filePathSalt);
        var saltBytes = JsonSerializer.Deserialize<byte[]>(saltBytesJson);
        return Convert.ToBase64String(saltBytes!);
    }

    public virtual string ComputeHash(string? previousHash, string data)
    {
        // estimate the total char count
        var estLength = _saltCache.Length + data.Length + (previousHash?.Length ?? 0);

        // calc max number of bytes needed for UTF8 encoding
        var maxBytes = Encoding.UTF8.GetMaxByteCount(estLength);

        // rent a byte buffer from shared pool to avoid heap allocations
        var buffer = ArrayPool<byte>.Shared.Rent(maxBytes);

        try
        {
            var offset = 0;

            // encoding all inputs into buffer
            offset += Encoding.UTF8.GetBytes(_saltCache, 0, _saltCache.Length, buffer, offset);
            offset += Encoding.UTF8.GetBytes(data, 0, data.Length, buffer, offset);
            offset += Encoding.UTF8.GetBytes(previousHash ?? "", 0, previousHash?.Length ?? 0, buffer, offset);

            // hash exact slice of the buffer that contain data
            var hashBytes = SHA256.HashData(buffer.AsSpan(0, offset));
            return Convert.ToBase64String(hashBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    public virtual void SaveHash(string lHash)
    {
        lock (_fileLock)
        {
            File.WriteAllText(_filePathHash, JsonSerializer.Serialize(lHash, _options));
        }
    }

    public string? LoadHash()
    {
        lock (_fileLock)
        {
            var content = File.ReadAllText(_filePathHash);
            return JsonSerializer.Deserialize<string>(content);
        }
    }
}