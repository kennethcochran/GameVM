using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GameVM.Compiler.Specs.Support;

public static class MameRunner
{
    public static string Run(byte[] rom, string monitorScriptPath)
    {
        var root = FindProjectRoot(AppContext.BaseDirectory);
        var testOutputDir = Path.Combine(root, "test_output");
        if (!Directory.Exists(testOutputDir))
        {
            Directory.CreateDirectory(testOutputDir);
        }
        var tempRomPath = Path.Combine(testOutputDir, "gamevm_test.bin");
        File.WriteAllBytes(tempRomPath, rom);

        var mameExe = GetMameExecutable();
        if (string.IsNullOrEmpty(mameExe))
        {
            throw new Exception("MAME is not installed. Please run 'dotnet run --project src/GameVM.DevTools -- mame install' first.");
        }

        string args;
        if (mameExe == "flatpak")
        {
            args = $"run org.mamedev.MAME -window -bench 10 a2600 -cart \"{tempRomPath}\" -autoboot_script \"{monitorScriptPath}\"";
        }
        else
        {
            args = $"-window -bench 10 a2600 -cart \"{tempRomPath}\" -autoboot_script \"{monitorScriptPath}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = mameExe,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = (mameExe == "flatpak") ? AppContext.BaseDirectory : Path.GetDirectoryName(mameExe)
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new Exception("Failed to start MAME.");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        File.Delete(tempRomPath);

        return output + "\n" + error;
    }

    private static string? GetMameExecutable()
    {
        var root = FindProjectRoot(AppContext.BaseDirectory);
        var toolsDir = Path.Combine(root, ".tools", "mame");
        string binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mame.exe" : "mame";

        if (Directory.Exists(toolsDir))
        {
            var files = Directory.GetFiles(toolsDir, binaryName, SearchOption.AllDirectories);
            if (files.Length > 0) return files[0];
        }

        if (IsCommandInPath(binaryName)) return binaryName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (IsFlatpakInstalled()) return "flatpak";
        }

        return null;
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

    private static bool IsCommandInPath(string command)
    {
        try
        {
            string whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var startInfo = new ProcessStartInfo
            {
                FileName = whichCmd,
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static bool IsFlatpakInstalled()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "flatpak",
                Arguments = "info org.mamedev.MAME",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }
}
