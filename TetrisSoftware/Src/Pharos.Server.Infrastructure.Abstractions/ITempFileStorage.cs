namespace Pharos.Server.Infrastructure;

/// <summary>
/// The abstraction of a temporary storage for the files uploaded to the Pharos server.
/// </summary>
public interface ITempFileStorage
{
    /// <summary>
    /// Saves a file to the temporary storage and assigns an ID to it.
    /// </summary>
    /// <param name="fileName">The name of the file (without the path). Cannot be empty.</param>
    /// <param name="content">The stream with the file content open for reading.</param>
    /// <returns>The unique ID that is used to access the file content.</returns>
    Task<Guid> SaveFile(string fileName, Stream content);

    /// <summary>
    /// Creates a file in the temporary storage and assigns an ID to it. The file content can be written later using the <see cref="GetFilePath(Guid)"/> method.
    /// </summary>
    /// <param name="fileName">The name of the file (without the path). Cannot be empty.</param>
    /// <param name="folderId">The unique folder ID where the file will be created. If null, the file will be created in the root of the temporary storage.</param>
    /// <returns>The unique ID that is used to access the file content.</returns>
    Task<Guid> CreateFile(string fileName, Guid? folderId = null);

    /// <summary>
    /// Creates a ZIP archive from the contents of the specified folder in the temporary storage and assigns an ID to it.
    /// </summary>
    /// <param name="folderId">The unique folder ID.</param>
    /// <param name="archiveName">The name of the archive file (without the path). Cannot be empty.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The unique ID that is used to access the archive file content.</returns>
    Task<Guid> CreateArchiveFromFolderAsync(Guid folderId, string archiveName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the original name of the file identified (in the storage) by the specified ID.
    /// </summary>
    /// <param name="fileId">The unique file ID.</param>
    /// <returns>The file name, specified for the <see cref="SaveFile(string, Stream)"/> method call.</returns>
    string? GetFileName(Guid fileId);

    /// <summary>
    /// Gets the full path of the file identified (in the storage) by the specified ID.
    /// </summary>
    /// <param name="fileId">The unique file ID.</param>
    /// <returns>The local file path.</returns>
    string? GetFilePath(Guid fileId);

    /// <summary>
    /// Deletes the specified file from the storage.
    /// </summary>
    /// <param name="fileId">The unique file ID.</param>
    void DeleteFile(Guid fileId);

    /// <summary>
    /// Creates a folder with a unique name under the temporary storage.
    /// </summary>
    /// <param name="folderName">The name of the folder (without the path). Cannot be empty.</param>
    /// <returns>The unique ID that is used to access the folder content.</returns>
    Guid CreateFolder(string folderName);

    /// <summary>
    /// Deletes a folder and its contents from the temporary storage.
    /// </summary>
    /// <param name="folderId">The unique folder ID.</param>
    void DeleteFolder(Guid folderId);
}
