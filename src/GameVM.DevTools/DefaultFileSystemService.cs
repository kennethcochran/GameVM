namespace GameVM.DevTools;

public class DefaultFileSystemService : IFileSystemService
{
    public bool FileExists(string path) => File.Exists(path);
    
    public bool DirectoryExists(string path) => Directory.Exists(path);
    
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    
    public DirectoryInfo GetDirectoryInfo(string path) => new DirectoryInfo(path);
    
    public string GetBaseDirectory() => AppContext.BaseDirectory;
    
    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) 
        => Directory.GetFiles(path, searchPattern, searchOption);
}
