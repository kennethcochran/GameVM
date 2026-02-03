using System.CommandLine;
using System.CommandLine.Invocation;

namespace GameVM.DevTools;

public static class Program
{
    private static readonly IMameInstaller DefaultMameInstaller = new MameInstaller();

    static async Task<int> Main(string[] args)
    {
        return await TestMain(args, DefaultMameInstaller);
    }

    // Test-friendly method that allows dependency injection
    public static async Task<int> TestMain(string[] args, IMameInstaller mameInstaller)
    {
        var rootCommand = new RootCommand("GameVM Developer Tools");

        var mameCommand = new Command("mame", "Manage MAME emulator dependencies");
        rootCommand.Subcommands.Add(mameCommand);

        var installCommand = new Command("install", "Install the latest MAME version locally");
        installCommand.SetAction(_ => { mameInstaller.InstallAsync().Wait(); });
        mameCommand.Subcommands.Add(installCommand);

        var pathCommand = new Command("path", "Display the path to the local MAME binary");
        pathCommand.SetAction(_ => { PrintMamePath(mameInstaller); });
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
                RunMameAsync(mameInstaller, romPath, scriptPath).Wait();
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

    private static void PrintMamePath(IMameInstaller mameInstaller)
    {
        var path = mameInstaller.GetMameExecutable();
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("MAME is not installed. Run 'dotnet run --project src/GameVM.DevTools -- mame install' first.");
        }
        else
        {
            Console.WriteLine(path);
        }
    }

    private static async Task RunMameAsync(IMameInstaller mameInstaller, string romPath, string scriptPath)
    {
        await mameInstaller.RunMameAsync(romPath, scriptPath);
    }
}
