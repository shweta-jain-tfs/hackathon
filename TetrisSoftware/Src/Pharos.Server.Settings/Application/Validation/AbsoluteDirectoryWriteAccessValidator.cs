namespace Pharos.Server.Settings.Application.Validation;

internal sealed class AbsoluteDirectoryWriteAccessValidator : IAbsoluteDirectoryWriteAccessValidator
{
    public AbsoluteDirectoryWriteAccessValidationResult Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return AbsoluteDirectoryWriteAccessValidationResult.Success();
        }

        string? probeFilePath = null;
        var expandedPath = Environment.ExpandEnvironmentVariables(path);
        if (string.IsNullOrWhiteSpace(expandedPath))
        {
            return AbsoluteDirectoryWriteAccessValidationResult.Failure(
                expandedPath,
                "Path is empty.");
        }
        try
        {
            Directory.CreateDirectory(expandedPath);

            probeFilePath = Path.Combine(
                expandedPath,
                $".pharos-write-test-{Guid.NewGuid():N}.tmp");

            using var stream = new FileStream(
                probeFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            stream.WriteByte(0);

            return AbsoluteDirectoryWriteAccessValidationResult.Success(expandedPath);
        }
        catch (Exception ex) when (IsPathWriteFailure(ex))
        {
            return AbsoluteDirectoryWriteAccessValidationResult.Failure(expandedPath, ex.Message);
        }
        finally
        {
            if (probeFilePath is not null)
            {
                TryDelete(probeFilePath);
            }
        }
    }

    private static bool IsPathWriteFailure(Exception ex)
    {
        return ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Cleanup must not change the validation result.
        }
    }
}
