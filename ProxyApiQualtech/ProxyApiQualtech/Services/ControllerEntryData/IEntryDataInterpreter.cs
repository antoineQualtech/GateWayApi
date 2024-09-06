using ProxyApiQualtech.Model;

namespace ProxyApiQualtech.Services.ControllerEntryData
{
    public interface IEntryDataInterpreter
    {
        Task<string> QualtechInternalHttpRequester(EntryDataModel entryData);
        HttpClientHandler CreateHandlerToRemoveCert();
        HttpClient QualtechInternalHttpRequestHeadersBuilder(EntryDataModel entryData, HttpClientHandler clientHandler, string bearerToken);
        Task<string> GenerateEpicorApiBearer(string epiEnv);
    }
}
