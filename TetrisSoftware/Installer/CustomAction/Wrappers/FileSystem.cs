using System.IO;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.Wrappers;

public class FileSystem : IFileSystem
{
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string content)
    {
        File.WriteAllText(path, content);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public void DeleteDirectory(string path)
    {
        Directory.Delete(path, true);
    }
    public string[] GetFiles(string path, string searchPattern)
    {
        return Directory.GetFiles(path, searchPattern);
    }

    public bool Exists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }
}