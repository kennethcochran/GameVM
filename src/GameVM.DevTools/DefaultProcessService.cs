using System.Runtime.InteropServices;

namespace GameVM.DevTools;

public class DefaultProcessService : IProcessService
{
    public async Task<bool> RunProcessAsync(string fileName, string arguments, bool redirectOutput = false, bool createNoWindow = false)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = redirectOutput,
                UseShellExecute = false,
                CreateNoWindow = createNoWindow
            };
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            return false;
        }
        catch { return false; }
    }

    public string? GetCommandPath(string command)
    {
        try
        {
            string whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? @"C:\Windows\System32\where.exe" 
                : "/usr/bin/which";
                
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
            
            if (process?.ExitCode == 0)
            {
                var output = process.StandardOutput.ReadToEnd();
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    return lines[0].Trim();
                }
            }
            return null;
        }
        catch { return null; }
    }
}
