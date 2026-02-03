using System.CommandLine;
using System.CommandLine.Invocation;

namespace GameVM.DevTools;

static class Program
{
    private static readonly IMameInstaller MameInstaller = new MameInstaller();

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("GameVM Developer Tools");

        var mameCommand = new Command("mame", "Manage MAME emulator dependencies");
        rootCommand.Subcommands.Add(mameCommand);

        var installCommand = new Command("install", "Install the latest MAME version locally");
        installCommand.SetAction(_ => { MameInstaller.InstallAsync().Wait(); });
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

    private static void PrintMamePath()
    {
        var path = MameInstaller.GetMameExecutable();
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("MAME is not installed. Run 'dotnet run --project src/GameVM.DevTools -- mame install' first.");
        }
        else
        {
            Console.WriteLine(path);
        }
    }

    private static async Task RunMameAsync(string romPath, string scriptPath)
    {
        await MameInstaller.RunMameAsync(romPath, scriptPath);
    }
}
