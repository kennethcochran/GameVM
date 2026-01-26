using System.CommandLine;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.CommandLine.Invocation;

namespace GameVM.DevTools;

static class Program
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
        rootCommand.Subcommands.Add(mameCommand);

        var installCommand = new Command("install", "Install the latest MAME version locally");
        installCommand.SetAction(_ => { InstallMameAsync().Wait(); });
        mameCommand.Subcommands.Add(installCommand);

        var pathCommand = new Command("path", "Display the path to the local MAME binary");
        pathCommand.SetAction(_ => { PrintMamePath(); });
        mameCommand.Subcommands.Add(pathCommand);

        var runCommand = new Command("run", "Run a ROM in MAME with GameVM monitoring");
        var romOption = new Option<string>("--rom") { Description = "Path to the ROM file" };
        var scriptOption = new Option<string>("--script") { Description = "Path to the Lua monitoring script" };
        runCommand.Options.Add(romOption);
        runCommand.Options.Add(scriptOption);
        runCommand.SetAction(parseResult => 
        {
            var romPath = parseResult.GetValue(romOption);
            var scriptPath = parseResult.GetValue(scriptOption);
            if (romPath != null && scriptPath != null)
            {
                RunMameAsync(romPath, scriptPath).Wait();
            }
            else
            {
                Console.Error.WriteLine("Both --rom and --script options are required.");
            }
        });
        mameCommand.Subcommands.Add(runCommand);

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && IsFlatpakInstalled()) return "flatpak";

        return null;
    }

    private static bool IsCommandInPath(string command)
    {
        try
        {
            // In CI environments, we can trust the PATH more than in production
            // Use environment variable to override if needed for additional security
            string whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            
            // Allow override of which command location for hardened environments
            var whichPath = Environment.GetEnvironmentVariable("GAMEVM_WHICH_PATH");
            if (!string.IsNullOrEmpty(whichPath) && File.Exists(whichPath))
            {
                whichCmd = whichPath;
            }
            
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
            // Allow override of flatpak command location for hardened environments
            var flatpakCmd = "flatpak";
            var flatpakPath = Environment.GetEnvironmentVariable("GAMEVM_FLATPAK_PATH");
            if (!string.IsNullOrEmpty(flatpakPath) && File.Exists(flatpakPath))
            {
                flatpakCmd = flatpakPath;
            }
            
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = flatpakCmd,
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
                        if (name.Contains("b_x64") && (name.EndsWith(".exe") || name.EndsWith(".zip")))
                    {
                        if (name.Contains("b_x64") && (name.EndsWith(".exe") || name.EndsWith(".zip")))
                        {
                            assetUrl = asset.GetProperty("browser_download_url").GetString();
                            assetName = name;
                            break;
                        }
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                             name.Contains("mac") && name.EndsWith(".zip"))
                    {
                        assetUrl = asset.GetProperty("browser_download_url").GetString();
                        assetName = name;
                        break;
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
            
            // Handle self-extracting .exe (7-Zip SFX) for Windows
            if (tempFile.EndsWith(".exe"))
            {
                // MAME Windows releases are 7-Zip self-extracting archives
                // Use silent extraction: -y (yes to prompts) -gm2 (hide GUI) -o<path> (output directory)
                var extractArgs = $"-y -gm2 -o\"{toolsDir}\"";
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    Arguments = extractArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    Console.WriteLine("Failed to start extraction process.");
                    File.Delete(tempFile);
                    return;
                }

                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Extraction failed with exit code {process.ExitCode}");
                    var error = await process.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"Error: {error}");
                    }
                    File.Delete(tempFile);
                    return;
                }
                
                File.Delete(tempFile);
            }
            else
            {
                // Handle ZIP archives (for macOS/Linux if available)
                await ZipFile.ExtractToDirectoryAsync(tempFile, toolsDir, overwriteFiles: true);
                File.Delete(tempFile);
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
        await process.WaitForExitAsync();

        Console.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine("ERRORS:");
            Console.WriteLine(error);
        }
    }
}
