using System.Net.Http;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.IO.Compression;

namespace GameVM.DevTools;

public interface IMameInstaller
{
    Task InstallAsync();
    Task<bool> IsFlatpakInstalledAsync();
    string GetToolsDirectory();
    string FindProjectRoot(string currentDir);
    string? GetMameExecutable();
    Task RunMameAsync(string romPath, string scriptPath);
}

public class DefaultHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient() => new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
}

public class DefaultConsoleService : IConsoleService
{
    public void WriteLine(string message) => Console.WriteLine(message);
}

public class MameInstaller : IMameInstaller
{
    private const string MameOrg = "mamedev";
    private const string MameRepo = "mame";
    private const string FlatpakId = "org.mamedev.MAME";

    private readonly IConsoleService _consoleService;
    private readonly IProcessService _processService;
    private readonly IAssetFinder _assetFinder;
    private readonly IPlatformService _platformService;
    private readonly IHttpService _httpService;

    public MameInstaller() : this(
        new DefaultHttpClientFactory(), 
        new DefaultConsoleService(), 
        new DefaultProcessService(), 
        new AssetFinder(),
        new DefaultPlatformService(),
        new DefaultHttpService(new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
    )
    {
    }

    public MameInstaller(
        IHttpClientFactory httpClientFactory, 
        IConsoleService consoleService, 
        IProcessService processService, 
        IAssetFinder assetFinder,
        IPlatformService platformService,
        IHttpService httpService)
    {
        _consoleService = consoleService;
        _processService = processService;
        _assetFinder = assetFinder;
        _platformService = platformService;
        _httpService = httpService;
    }

    // Legacy constructor for backward compatibility
    public MameInstaller(
        IHttpClientFactory httpClientFactory, 
        IConsoleService consoleService, 
        IProcessService processService, 
        IAssetFinder assetFinder) : this(
            httpClientFactory, 
            consoleService, 
            processService, 
            assetFinder,
            new DefaultPlatformService(),
            new DefaultHttpService(new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
        )
    {
    }

    public async Task InstallAsync()
    {
        var toolsDir = GetToolsDirectory();
        _consoleService.WriteLine($"Target directory: {toolsDir}");

        try
        {
            if (_platformService.IsLinux())
            {
                _consoleService.WriteLine("Installing MAME from Ubuntu repositories...");
                
                // Try to install MAME from Ubuntu repositories
                var installSuccess = await _processService.RunProcessAsync("sudo", "apt-get update && apt-get install -y mame", redirectOutput: true, createNoWindow: true);
                
                if (installSuccess)
                {
                    _consoleService.WriteLine("MAME installed successfully from Ubuntu repositories");
                    
                    // Verify installation
                    var mamePath = _processService.GetCommandPath("mame");
                    if (!string.IsNullOrEmpty(mamePath))
                    {
                        _consoleService.WriteLine($"MAME available at: {mamePath}");
                        return;
                    }
                }
                else
                {
                    _consoleService.WriteLine("Failed to install MAME from Ubuntu repositories");
                    _consoleService.WriteLine("Falling back to GameVM releases...");
                }
            }

            // Use our own GitHub Releases for unlimited access (fallback for Linux, primary for Windows/macOS)
            var releaseJson = await _httpService.GetStringAsync("https://api.github.com/repos/kennethcochran/GameVM/releases/tags/mame-packages");
            using var doc = JsonDocument.Parse(releaseJson);
            var root = doc.RootElement;

            var assetInfo = _assetFinder.FindSuitableAsset(root);
            if (assetInfo == null)
            {
                _consoleService.WriteLine("Could not find a suitable MAME binary for your platform in GameVM releases.");
                _consoleService.WriteLine("Available binaries may need to be uploaded to https://github.com/kennethcochran/GameVM/releases/new");
                return;
            }

            _consoleService.WriteLine($"Downloading {assetInfo.Name} from GameVM releases...");
            var tempFile = Path.Combine(Path.GetTempPath(), assetInfo.Name);
            var fileBytes = await _httpService.GetByteArrayAsync(assetInfo.Url);
            await File.WriteAllBytesAsync(tempFile, fileBytes);

            _consoleService.WriteLine("Extracting MAME...");
            await ExtractMameAsync(tempFile, toolsDir);

            var exe = GetMameExecutable();
            if (!string.IsNullOrEmpty(exe))
            {
                _consoleService.WriteLine($"MAME installed successfully at: {exe}");
            }
        }
        catch (Exception ex)
        {
            _consoleService.WriteLine($"Error during installation: {ex.Message}");
        }
    }

    public async Task<bool> IsFlatpakInstalledAsync()
    {
        try
        {
            var flatpakPath = _processService.GetCommandPath("flatpak");
            if (string.IsNullOrEmpty(flatpakPath))
            {
                return false;
            }
            
            return await _processService.RunProcessAsync(flatpakPath, $"info {FlatpakId}", redirectOutput: true, createNoWindow: true);
        }
        catch { return false; }
    }

    public string GetToolsDirectory()
    {
        var root = FindProjectRoot(AppContext.BaseDirectory);
        var toolsDir = Path.Combine(root, ".tools", "mame");
        if (!Directory.Exists(toolsDir)) Directory.CreateDirectory(toolsDir);
        return toolsDir;
    }

    public string FindProjectRoot(string currentDir)
    {
        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GameVM.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }

    public string? GetMameExecutable()
    {
        var toolsDir = GetToolsDirectory();
        string binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mame.exe" : "mame";

        if (Directory.Exists(toolsDir))
        {
            var files = Directory.GetFiles(toolsDir, binaryName, SearchOption.AllDirectories);
            if (files.Length > 0) return files[0];
        }

        if (_processService.GetCommandPath(binaryName) != null)
        {
            return binaryName;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && _processService.GetCommandPath("flatpak") != null) 
            return "flatpak";

        return null;
    }

    public async Task RunMameAsync(string romPath, string scriptPath)
    {
        var mameExe = GetMameExecutable();
        if (string.IsNullOrEmpty(mameExe))
        {
            _consoleService.WriteLine("MAME is not installed.");
            return;
        }

        var fullRomPath = Path.GetFullPath(romPath);
        string args;

        if (mameExe == "flatpak")
        {
            args = $"run {FlatpakId} -window -bench 10 a2600 -cart \"{fullRomPath}\"";
            if (!string.IsNullOrEmpty(scriptPath))
            {
                args += $" -autoboot_script \"{Path.GetFullPath(scriptPath)}\"";
            }
        }
        else
        {
            args = $"-window -bench 10 a2600 -cart \"{fullRomPath}\"";
            if (!string.IsNullOrEmpty(scriptPath))
            {
                args += $" -autoboot_script \"{Path.GetFullPath(scriptPath)}\"";
            }
        }

        _consoleService.WriteLine($"Executing: {mameExe} {args}");
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = mameExe,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = (mameExe == "flatpak") ? AppContext.BaseDirectory : Path.GetDirectoryName(mameExe)
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            _consoleService.WriteLine("Failed to start MAME.");
            return;
        }

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        _consoleService.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
        {
            _consoleService.WriteLine("ERRORS:");
            _consoleService.WriteLine(error);
        }
    }

    private async Task ExtractMameAsync(string tempFile, string toolsDir)
    {
        if (tempFile.EndsWith(".exe"))
        {
            var extractArgs = $"-y -gm2 -o\"{toolsDir}\"";
            var success = await _processService.RunProcessAsync(tempFile, extractArgs, redirectOutput: true, createNoWindow: true);
            
            if (!success)
            {
                _consoleService.WriteLine("Extraction failed.");
            }
            File.Delete(tempFile);
        }
        else
        {
            await ZipFile.ExtractToDirectoryAsync(tempFile, toolsDir, overwriteFiles: true);
            File.Delete(tempFile);
        }
    }
}
