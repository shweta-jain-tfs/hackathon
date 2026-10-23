namespace CustomAction.Wrappers.Interfaces;

public interface IFileSystem
{
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    void DeleteDirectory(string path);
    string[] GetFiles(string path, string searchPattern);
    bool Exists(string path);
}