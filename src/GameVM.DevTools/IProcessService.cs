namespace GameVM.DevTools;

public interface IProcessService
{
    Task<bool> RunProcessAsync(string fileName, string arguments, bool redirectOutput = false, bool createNoWindow = false);
    string? GetCommandPath(string command);
}
