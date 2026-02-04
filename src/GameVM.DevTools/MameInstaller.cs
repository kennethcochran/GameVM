using System.Runtime.InteropServices;

namespace GameVM.DevTools;

public interface IMameInstaller
{
    Task InstallAsync();
    string GetToolsDirectory();
    string FindProjectRoot(string currentDir);
    string? GetMameExecutable();
    Task RunMameAsync(string romPath, string scriptPath);
}

public class DefaultConsoleService : IConsoleService
{
    public void WriteLine(string message) => Console.WriteLine(message);
}

public class MameInstaller : IMameInstaller
{
    private readonly IConsoleService _consoleService;
    private readonly IProcessService _processService;
    private readonly IPlatformService _platformService;
    private readonly IFileSystemService _fileSystemService;

    public MameInstaller() : this(
        new DefaultConsoleService(), 
        new DefaultProcessService(), 
        new DefaultPlatformService(),
        new DefaultFileSystemService()
    )
    {
    }

    public MameInstaller(
        IConsoleService consoleService, 
        IProcessService processService, 
        IPlatformService platformService) : this(
            consoleService, 
            processService,
            platformService,
            new DefaultFileSystemService()
    )
    {
    }

    public MameInstaller(
        IConsoleService consoleService, 
        IProcessService processService, 
        IPlatformService platformService,
        IFileSystemService fileSystemService)
    {
        _consoleService = consoleService;
        _processService = processService;
        _platformService = platformService;
        _fileSystemService = fileSystemService;
    }

    // Legacy constructor for backward compatibility
    public MameInstaller(
        IHttpClientFactory httpClientFactory, 
        IConsoleService consoleService, 
        IProcessService processService, 
        IAssetFinder assetFinder) : this(
            consoleService, 
            processService,
            new DefaultPlatformService()
    )
    {
    }

    // Legacy constructor for backward compatibility
    public MameInstaller(
        IHttpClientFactory httpClientFactory, 
        IConsoleService consoleService, 
        IProcessService processService, 
        IAssetFinder assetFinder,
        IPlatformService platformService,
        IHttpService httpService) : this(
            consoleService, 
            processService,
            platformService
    )
    {
    }

    public async Task InstallAsync()
    {
        var toolsDir = GetToolsDirectory();
        _consoleService.WriteLine($"Target directory: {toolsDir}");

        try
        {
            bool installSuccess = false;
            
            if (_platformService.IsLinux())
            {
                _consoleService.WriteLine("Installing MAME using apt-get (Debian-based Linux)...");
                
                // Check if apt-get is available
                var aptPath = _processService.GetCommandPath("apt-get");
                if (string.IsNullOrEmpty(aptPath))
                {
                    _consoleService.WriteLine("ERROR: apt-get not found. This installer supports Debian-based Linux distributions.");
                    _consoleService.WriteLine("For other Linux distributions, please install MAME using your package manager:");
                    _consoleService.WriteLine("  - Fedora/RHEL: sudo dnf install mame");
                    _consoleService.WriteLine("  - Arch Linux: sudo pacman -S mame");
                    _consoleService.WriteLine("  - openSUSE: sudo zypper install mame");
                    return;
                }
                
                // Install MAME from Debian repositories
                installSuccess = await _processService.RunProcessAsync("sudo", "apt-get update && apt-get install -y mame", redirectOutput: true, createNoWindow: true);
                
                if (installSuccess)
                {
                    _consoleService.WriteLine("MAME installed successfully from Debian repositories");
                    
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
                    _consoleService.WriteLine("Failed to install MAME from Debian repositories");
                    _consoleService.WriteLine("This may indicate an issue with your package sources or permissions.");
                }
            }
            else if (_platformService.IsWindows())
            {
                _consoleService.WriteLine("Installing MAME using Chocolatey...");
                
                // Check if Chocolatey is available
                var chocoPath = _processService.GetCommandPath("choco");
                if (string.IsNullOrEmpty(chocoPath))
                {
                    _consoleService.WriteLine("ERROR: Chocolatey not found. Please install Chocolatey first:");
                    _consoleService.WriteLine("  Run PowerShell as Administrator and execute:");
                    _consoleService.WriteLine("    Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))");
                    return;
                }
                
                // Install MAME using Chocolatey
                installSuccess = await _processService.RunProcessAsync("choco", "install mame -y --no-progress", redirectOutput: true, createNoWindow: true);
                
                if (installSuccess)
                {
                    _consoleService.WriteLine("MAME installed successfully via Chocolatey");
                    
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
                    _consoleService.WriteLine("Failed to install MAME via Chocolatey");
                }
            }
            else if (_platformService.IsMacOS())
            {
                _consoleService.WriteLine("Installing MAME using Homebrew...");
                
                // Check if Homebrew is available
                var brewPath = _processService.GetCommandPath("brew");
                if (string.IsNullOrEmpty(brewPath))
                {
                    _consoleService.WriteLine("ERROR: Homebrew not found. Please install Homebrew first:");
                    _consoleService.WriteLine("  Run: /bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"");
                    return;
                }
                
                // Install MAME using Homebrew
                installSuccess = await _processService.RunProcessAsync("brew", "install mame", redirectOutput: true, createNoWindow: true);
                
                if (installSuccess)
                {
                    _consoleService.WriteLine("MAME installed successfully via Homebrew");
                    
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
                    _consoleService.WriteLine("Failed to install MAME via Homebrew");
                }
            }
            else
            {
                _consoleService.WriteLine("ERROR: Unsupported operating system");
                return;
            }

            // If package manager installation failed, provide guidance
            if (!installSuccess)
            {
                _consoleService.WriteLine("Package manager installation failed. Please install MAME manually:");
                if (_platformService.IsLinux())
                {
                    _consoleService.WriteLine("  sudo apt-get install mame  # For Debian-based systems");
                }
                else if (_platformService.IsWindows())
                {
                    _consoleService.WriteLine("  choco install mame  # Via Chocolatey");
                    _consoleService.WriteLine("  Or download from: https://chocolatey.org/packages/mame");
                }
                else if (_platformService.IsMacOS())
                {
                    _consoleService.WriteLine("  brew install mame  # Via Homebrew");
                    _consoleService.WriteLine("  Or download from: https://formulae.brew.sh/formula/mame");
                }
                return;
            }
        }
        catch (Exception ex)
        {
            _consoleService.WriteLine($"Error during installation: {ex.Message}");
            _consoleService.WriteLine("Please ensure you have the necessary permissions and network access.");
        }
    }

    public string GetToolsDirectory()
    {
        var root = FindProjectRoot(_fileSystemService.GetBaseDirectory());
        var toolsDir = Path.Combine(root, ".tools", "mame");
        if (!_fileSystemService.DirectoryExists(toolsDir)) _fileSystemService.CreateDirectory(toolsDir);
        return toolsDir;
    }

    public string FindProjectRoot(string currentDir)
    {
        var dir = _fileSystemService.GetDirectoryInfo(currentDir);
        while (dir != null)
        {
            if (_fileSystemService.FileExists(Path.Combine(dir.FullName, "GameVM.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return _fileSystemService.GetBaseDirectory();
    }

    public string? GetMameExecutable()
    {
        string binaryName = _platformService.IsWindows() ? "mame.exe" : "mame";

        // First, check if MAME is available via package manager (preferred method)
        if (_processService.GetCommandPath(binaryName) != null)
        {
            return binaryName;
        }

        // Legacy: Check local tools directory (for backward compatibility)
        var toolsDir = GetToolsDirectory();
        if (_fileSystemService.DirectoryExists(toolsDir))
        {
            var files = _fileSystemService.GetFiles(toolsDir, binaryName, SearchOption.AllDirectories);
            if (files.Length > 0) return files[0];
        }

        // Legacy: Check Flatpak (Linux fallback)
        if (_platformService.IsLinux() && _processService.GetCommandPath("flatpak") != null) 
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
        string args = $"-window -bench 10 a2600 -cart \"{fullRomPath}\"";
        
        if (!string.IsNullOrEmpty(scriptPath))
        {
            args += $" -autoboot_script \"{Path.GetFullPath(scriptPath)}\"";
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
            WorkingDirectory = Path.GetDirectoryName(mameExe) ?? AppContext.BaseDirectory
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
}
