namespace Pharos.Server.Settings.Application.Validation;

public class AbsoluteDirectoryWriteAccessValidatorTests
{
    private readonly AbsoluteDirectoryWriteAccessValidator _sut = new();

    [Fact]
    public void Validate_ReturnsSuccess_WhenPathIsWritable()
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), $"pharos-write-access-{Guid.NewGuid():N}");

        try
        {
            // Act
            var result = _sut.Validate(directory);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(directory, result.ExpandedPath);
            Assert.Empty(Directory.GetFiles(directory, ".pharos-write-test-*.tmp"));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenPathCannotBeCreatedAsDirectory()
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), $"pharos-write-access-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "existing-file");
        File.WriteAllText(filePath, string.Empty);

        try
        {
            // Act
            var result = _sut.Validate(filePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(filePath, result.ExpandedPath);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
