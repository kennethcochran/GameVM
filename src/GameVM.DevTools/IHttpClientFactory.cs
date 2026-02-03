using System.Net.Http;

namespace GameVM.DevTools;

public interface IHttpClientFactory
{
    HttpClient CreateClient();
}
