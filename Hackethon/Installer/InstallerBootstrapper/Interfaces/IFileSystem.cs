using System;

namespace InstallerBootstrapper;

public interface IFileSystem
{
    void CreateDirectory(string path);
    bool DirectoryExists(string path);
    string[] GetFiles(string path, string searchPattern);
    DateTime GetLastWriteUtc(string path);
}