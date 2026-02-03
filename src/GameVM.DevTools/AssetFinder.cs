using System.Text.Json;
using System.Runtime.InteropServices;

namespace GameVM.DevTools;

public class AssetFinder : IAssetFinder
{
    public AssetInfo? FindSuitableAsset(JsonElement root)
    {
        var assets = ExtractAssets(root);
        if (!assets.HasValue)
            return null;

        return FindFirstMatchingAsset(assets.Value);
    }

    private static JsonElement.ArrayEnumerator? ExtractAssets(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assets))
            return null;

        return assets.EnumerateArray();
    }

    private AssetInfo? FindFirstMatchingAsset(JsonElement.ArrayEnumerator assets)
    {
        foreach (var asset in assets)
        {
            var assetInfo = TryCreateAssetInfo(asset);
            if (assetInfo != null && IsAssetSuitableForPlatform(assetInfo.Name))
                return assetInfo;
        }
        return null;
    }

    private AssetInfo? TryCreateAssetInfo(JsonElement asset)
    {
        var name = asset.GetProperty("name").GetString();
        if (string.IsNullOrEmpty(name))
            return null;

        var url = asset.GetProperty("browser_download_url").GetString();
        return new AssetInfo { Name = name, Url = url ?? string.Empty };
    }

    private static bool IsAssetSuitableForPlatform(string assetName)
    {
        return IsWindowsAsset(assetName) || IsMacAsset(assetName);
    }

    private static bool IsWindowsAsset(string assetName)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
               assetName.Contains("b_x64") &&
               (assetName.EndsWith(".exe") || assetName.EndsWith(".zip"));
    }

    private static bool IsMacAsset(string assetName)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
               assetName.Contains("mac") &&
               assetName.EndsWith(".zip");
    }
}
