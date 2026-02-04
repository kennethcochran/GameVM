namespace GameVM.DevTools;

public interface IFileSystemService
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    DirectoryInfo GetDirectoryInfo(string path);
    string GetBaseDirectory();
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
}
