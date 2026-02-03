using System.Text.Json;

namespace GameVM.DevTools;

public interface IAssetFinder
{
    AssetInfo? FindSuitableAsset(JsonElement root);
}
