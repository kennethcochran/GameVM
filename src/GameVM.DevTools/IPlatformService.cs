using System.Runtime.InteropServices;

namespace GameVM.DevTools;

/// <summary>
/// Adapter for platform detection operations
/// </summary>
public interface IPlatformService
{
    bool IsLinux();
    bool IsWindows();
    bool IsMacOS();
}

/// <summary>
/// Default implementation using RuntimeInformation
/// </summary>
public class DefaultPlatformService : IPlatformService
{
    public bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}
