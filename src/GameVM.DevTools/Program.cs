using System.CommandLine;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GameVM.DevTools;

class Program
{
    private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    private const string MameOrg = "mamedev";
    private const string MameRepo = "mame";
    private const string FlatpakId = "org.mamedev.MAME";

    static async Task<int> Main(string[] args)
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "GameVM-DevTools");

        var rootCommand = new RootCommand("GameVM Developer Tools");

        var mameCommand = new Command("mame", "Manage MAME emulator dependencies");
        rootCommand.AddCommand(mameCommand);

        var installCommand = new Command("install", "Install the latest MAME version locally");
        installCommand.SetHandler(InstallMameAsync);
        mameCommand.AddCommand(installCommand);

        var pathCommand = new Command("path", "Display the path to the local MAME binary");
        pathCommand.SetHandler(PrintMamePath);
        mameCommand.AddCommand(pathCommand);

        var runCommand = new Command("run", "Run a ROM in MAME with GameVM monitoring");
        var romOption = new Option<string>("--rom", "Path to the ROM file") { IsRequired = true };
        var scriptOption = new Option<string>("--script", "Path to the Lua monitoring script");
        runCommand.AddOption(romOption);
        runCommand.AddOption(scriptOption);
        runCommand.SetHandler(RunMameAsync, romOption, scriptOption);
        mameCommand.AddCommand(runCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static string GetToolsDirectory()
    {
        var root = FindProjectRoot(AppContext.BaseDirectory);
        var toolsDir = Path.Combine(root, ".tools", "mame");
        if (!Directory.Exists(toolsDir)) Directory.CreateDirectory(toolsDir);
        return toolsDir;
    }

    private static string FindProjectRoot(string currentDir)
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

    private static string? GetMameExecutable()
    {
        var toolsDir = GetToolsDirectory();
        string binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mame.exe" : "mame";

        // 1. Check local tools directory
        if (Directory.Exists(toolsDir))
        {
            var files = Directory.GetFiles(toolsDir, binaryName, SearchOption.AllDirectories);
            if (files.Length > 0) return files[0];
        }

        // 2. Check system path (native mame)
        if (IsCommandInPath(binaryName))
        {
            return binaryName;
        }

        // 3. Fallback to Flatpak on Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (IsFlatpakInstalled()) return "flatpak";
        }

        return null;
    }

    private static bool IsCommandInPath(string command)
    {
        try
        {
            string whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = whichCmd,
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static bool IsFlatpakInstalled()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "flatpak",
                Arguments = $"info {FlatpakId}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static void PrintMamePath()
    {
        var path = GetMameExecutable();
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("MAME is not installed. Run 'dotnet run --project src/GameVM.DevTools -- mame install' first.");
        }
        else
        {
            Console.WriteLine(path);
        }
    }

    private static async Task InstallMameAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Console.WriteLine("Note: Official Linux binaries are not available on GitHub. Checking for Flatpak...");
            if (IsFlatpakInstalled())
            {
                Console.WriteLine($"MAME is already installed via Flatpak ({FlatpakId}).");
                return;
            }
            Console.WriteLine("Please install MAME via your distribution's package manager or Flatpak.");
            return;
        }

        var toolsDir = GetToolsDirectory();
        Console.WriteLine($"Target directory: {toolsDir}");

        try
        {
            Console.WriteLine("Fetching latest MAME release information...");
            var releaseJson = await HttpClient.GetStringAsync($"https://api.github.com/repos/{MameOrg}/{MameRepo}/releases/latest");
            using var doc = JsonDocument.Parse(releaseJson);
            var root = doc.RootElement;

            string? assetUrl = null;
            string? assetName = null;

            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (string.IsNullOrEmpty(name)) continue;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (name.Contains("b_x64") && (name.EndsWith(".exe") || name.EndsWith(".zip")))
                        {
                            assetUrl = asset.GetProperty("browser_download_url").GetString();
                            assetName = name;
                            break;
                        }
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        if (name.Contains("mac") && name.EndsWith(".zip"))
                        {
                            assetUrl = asset.GetProperty("browser_download_url").GetString();
                            assetName = name;
                            break;
                        }
                    }
                }
            }

            if (assetUrl == null || assetName == null)
            {
                Console.WriteLine("Could not find a suitable MAME binary for your platform.");
                return;
            }

            Console.WriteLine($"Downloading {assetName}...");
            var tempFile = Path.Combine(Path.GetTempPath(), assetName);
            var response = await HttpClient.GetAsync(assetUrl);
            using (var fs = new FileStream(tempFile, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }

            Console.WriteLine("Extracting MAME...");
            // If it's an .exe self-extracting zip, we might need to rename it to .zip for ZipFile to handle it
            string extractionSource = tempFile;
            if (tempFile.EndsWith(".exe"))
            {
                extractionSource = Path.ChangeExtension(tempFile, ".zip");
                if (File.Exists(extractionSource)) File.Delete(extractionSource);
                File.Move(tempFile, extractionSource);
            }

            try
            {
                ZipFile.ExtractToDirectory(extractionSource, toolsDir, overwriteFiles: true);
            }
            finally
            {
                if (File.Exists(extractionSource)) File.Delete(extractionSource);
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }

            var exe = GetMameExecutable();
            if (!string.IsNullOrEmpty(exe))
            {
                Console.WriteLine($"MAME installed successfully at: {exe}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during installation: {ex.Message}");
        }
    }

    private static async Task RunMameAsync(string romPath, string scriptPath)
    {
        var mameExe = GetMameExecutable();
        if (string.IsNullOrEmpty(mameExe))
        {
            Console.WriteLine("MAME is not installed.");
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

        Console.WriteLine($"Executing: {mameExe} {args}");
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
            Console.WriteLine("Failed to start MAME.");
            return;
        }

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        Console.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine("ERRORS:");
            Console.WriteLine(error);
        }
    }
}
